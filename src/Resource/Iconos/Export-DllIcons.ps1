[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string[]]$InputPath = @((Get-Location).Path),

    [Parameter(Position = 1)]
    [string]$OutputPath = (Join-Path -Path $PSScriptRoot -ChildPath 'out'),

    [int[]]$Sizes = @(16, 20, 24, 32, 40, 48, 64, 96, 128, 256),

    [switch]$Recurse
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Permitir que -InputPath llegue como un solo string con ';' (tareas de VS Code)
if ($InputPath.Count -eq 1 -and $InputPath[0] -match ';') {
    $InputPath = $InputPath[0].Split(';') | ForEach-Object { $_.Trim() } | Where-Object { $_ }
}

# PowerShell 7+ (PSEdition Core) puede no cargar correctamente DLLs net4x (p.ej. System.Web),
# lo que rompe el render WPF de iconpacks. Re-invocar en Windows PowerShell 5.1 cuando sea posible.
if ($PSVersionTable.PSEdition -eq 'Core') {
    $winPs = Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
    if (Test-Path -LiteralPath $winPs) {
        $argList = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $PSCommandPath)

        $argList += @('-InputPath', ($InputPath -join ';'))
        $argList += @('-OutputPath', $OutputPath)
        if ($Recurse) { $argList += '-Recurse' }

        & $winPs @argList
        exit $LASTEXITCODE
    }
    Write-Warning "Windows PowerShell 5.1 no está disponible; el render WPF puede fallar en PowerShell $($PSVersionTable.PSVersion)."
}

# Asegurar carpeta de salida y ruta de log de error
if (-not (Test-Path -LiteralPath $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath | Out-Null
}
$errorLogPath = Join-Path $OutputPath 'last-error.txt'

function Resolve-DllFiles {
    param(
        [Parameter(Mandatory)]
        [string[]]$Path,
        [switch]$Recurse
    )

    $all = New-Object 'System.Collections.Generic.List[System.IO.FileInfo]'
    foreach ($p in $Path) {
        $resolved = Resolve-Path -LiteralPath $p -ErrorAction Stop
        $item = Get-Item -LiteralPath $resolved.Path -ErrorAction Stop

        if ($item.PSIsContainer) {
            $params = @{ LiteralPath = $item.FullName; Filter = '*.dll'; File = $true }
            if ($Recurse) { $params.Recurse = $true }
            foreach ($f in (Get-ChildItem @params)) {
                $all.Add($f) | Out-Null
            }
            continue
        }

        if ($item.Extension -ne '.dll') {
            throw "El archivo no es una DLL: $($item.FullName)"
        }
        $all.Add([System.IO.FileInfo]$item) | Out-Null
    }

    return $all | Sort-Object FullName -Unique
}

function Invoke-InStaThread {
    param(
        [Parameter(Mandatory)][scriptblock]$ScriptBlock,
        [object[]]$ArgumentList = @()
    )

    if ([System.Threading.Thread]::CurrentThread.GetApartmentState() -eq [System.Threading.ApartmentState]::STA) {
        return & $ScriptBlock @ArgumentList
    }

    $completed = New-Object System.Threading.ManualResetEvent($false)
    $boxed = [pscustomobject]@{ Result = $null; Error = $null }

    $threadStart = [System.Threading.ParameterizedThreadStart]{
        param($state)
        try {
            $state.Result = & $ScriptBlock @ArgumentList
        } catch {
            $state.Error = $_
        } finally {
            [void]$completed.Set()
        }
    }

    $thread = New-Object System.Threading.Thread($threadStart)
    $thread.IsBackground = $true
    $thread.SetApartmentState([System.Threading.ApartmentState]::STA)
    $thread.Start($boxed)

    [void]$completed.WaitOne()
    if ($boxed.Error) { throw $boxed.Error }
    return $boxed.Result
}

function Sanitize-FileName {
    param([Parameter(Mandatory)][string]$Name)

    $safe = [regex]::Replace($Name, '[^A-Za-z0-9._-]+', '_')
    if ($safe.Length -gt 80) { $safe = $safe.Substring(0, 80) }
    if ([string]::IsNullOrWhiteSpace($safe)) { $safe = 'icon' }
    return $safe
}

function Try-Export-PackIconsFromAssembly {
    param(
        [Parameter(Mandatory)][string]$DllPath,
        [Parameter(Mandatory)][string]$DllBase,
        [Parameter(Mandatory)][string]$ImagesRoot,
        [Parameter(Mandatory)][int[]]$Sizes,
        [Parameter(Mandatory)][int]$PreviewSize
    )

    return Invoke-InStaThread -ArgumentList @($DllPath, $DllBase, $ImagesRoot, $Sizes, $PreviewSize) -ScriptBlock {
        param($DllPath, $DllBase, $ImagesRoot, $Sizes, $PreviewSize)

        Add-Type -AssemblyName WindowsBase
        Add-Type -AssemblyName PresentationCore
        Add-Type -AssemblyName PresentationFramework

        if (-not [System.Windows.Application]::Current) {
            $app = New-Object System.Windows.Application
            $app.ShutdownMode = [System.Windows.ShutdownMode]::OnExplicitShutdown
        }

        $asmName = [Reflection.AssemblyName]::GetAssemblyName($DllPath).Name

        # Renderizar SOLO tamaño medio para iconpacks (evita explosión de PNGs)
        $sizesToRender = @($PreviewSize)

        [Reflection.Assembly]::LoadFrom($DllPath) | Out-Null

        # Intentar cargar dependencias típicas desde el mismo directorio (para NuGet)
        $dllDir = [IO.Path]::GetDirectoryName($DllPath)
        $coreDep = Join-Path $dllDir 'MahApps.Metro.IconPacks.Core.dll'
        if (Test-Path -LiteralPath $coreDep) {
            try { [Reflection.Assembly]::LoadFrom($coreDep) | Out-Null } catch { }
        }

        function Save-FrameworkElementPng {
            param(
                [Parameter(Mandatory)][System.Windows.FrameworkElement]$Element,
                [Parameter(Mandatory)][int]$Size,
                [Parameter(Mandatory)][string]$DestinationPng
            )

            # Muchos controles (p.ej. PackIcon) dependen de una plantilla y de estar en un árbol visual.
            # Renderizamos un contenedor (Grid) que aloja el elemento y forzamos layout + render.
            $root = New-Object System.Windows.Controls.Grid
            $root.Width = $Size
            $root.Height = $Size
            $root.Background = [System.Windows.Media.Brushes]::White

            $Element.Width = $Size
            $Element.Height = $Size

            if ($Element -is [System.Windows.Controls.Control]) {
                try { $Element.SetValue([System.Windows.Controls.Control]::ForegroundProperty, [System.Windows.Media.Brushes]::Black) } catch { }
                try { $Element.SetValue([System.Windows.Controls.Control]::BackgroundProperty, [System.Windows.Media.Brushes]::Transparent) } catch { }
            }

            [void]$root.Children.Add($Element)

            try { $root.ApplyTemplate() } catch { }
            try { $Element.ApplyTemplate() } catch { }

            $root.Measure([System.Windows.Size]::new($Size, $Size))
            $root.Arrange([System.Windows.Rect]::new(0, 0, $Size, $Size))
            $root.UpdateLayout()

            # Asegurar que se procese la cola de render
            try {
                $null = $root.Dispatcher.Invoke([Action]{ }, [System.Windows.Threading.DispatcherPriority]::Render)
            } catch { }

            $rtb = New-Object System.Windows.Media.Imaging.RenderTargetBitmap($Size, $Size, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
            $rtb.Render($root)

            $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
            [void]$encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rtb))

            $destDir = Split-Path -Path $DestinationPng -Parent
            if (-not (Test-Path -LiteralPath $destDir)) {
                New-Item -ItemType Directory -Path $destDir | Out-Null
            }

            $fs = [System.IO.File]::Open($DestinationPng, [System.IO.FileMode]::Create)
            try { $encoder.Save($fs) } finally { $fs.Dispose() }
        }

        function Sanitize-FileNameLocal {
            param([Parameter(Mandatory)][string]$Name)
            $safe = [regex]::Replace($Name, '[^A-Za-z0-9._-]+', '_')
            if ($safe.Length -gt 80) { $safe = $safe.Substring(0, 80) }
            if ([string]::IsNullOrWhiteSpace($safe)) { $safe = 'icon' }
            return $safe
        }

        function Try-ExportPack {
            param(
                [Parameter(Mandatory)][string]$ControlTypeName,
                [Parameter(Mandatory)][string]$EnumTypeName
            )

            function Find-LoadedType {
                param([Parameter(Mandatory)][string]$FullName)
                foreach ($a in [AppDomain]::CurrentDomain.GetAssemblies()) {
                    try {
                        $t = $a.GetType($FullName, $false, $false)
                        if ($t) { return $t }
                    } catch {
                        continue
                    }
                }
                return $null
            }

            $controlType = Find-LoadedType -FullName $ControlTypeName
            $enumType = Find-LoadedType -FullName $EnumTypeName
            if (-not $controlType -or -not $enumType -or -not $enumType.IsEnum) {
                return @()
            }

            $kindProp = $controlType.GetProperty('Kind')
            if (-not $kindProp) { return @() }

            $fgProp = $controlType.GetProperty('Foreground')

            $names = [Enum]::GetNames($enumType)
            $localEntries = New-Object 'System.Collections.Generic.List[object]'

            for ($i = 0; $i -lt $names.Length; $i++) {
                $name = $names[$i]
                $safeName = Sanitize-FileNameLocal -Name $name
                $fileBase = ('k{0:D5}_{1}' -f $i, $safeName)

                $styleLinks = New-Object 'System.Collections.Generic.List[string]'
                $previewRel = $null

                foreach ($size in $sizesToRender) {
                    $pngName = "$fileBase.png"
                    $pngDirAbs = Join-Path (Join-Path $ImagesRoot $DllBase) ("s$size")
                    $pngAbs = Join-Path $pngDirAbs $pngName

                    if (-not (Test-Path -LiteralPath $pngAbs)) {
                        try {
                            $element = [System.Windows.FrameworkElement][Activator]::CreateInstance($controlType)
                            $kindValue = [Enum]::Parse($enumType, $name)
                            $kindProp.SetValue($element, $kindValue)

                            if ($fgProp) {
                                try { $fgProp.SetValue($element, [System.Windows.Media.Brushes]::Black) } catch { }
                            }

                            Save-FrameworkElementPng -Element $element -Size $size -DestinationPng $pngAbs
                        } catch {
                            # Saltar iconos que no puedan renderizarse
                            continue
                        }
                    }

                    $rel = (Join-Path -Path (Join-Path -Path ('images\' + $DllBase) -ChildPath ("s$size")) -ChildPath $pngName)
                    $rel = $rel.Replace('\', '/')
                    [void]$styleLinks.Add("[$size]($rel)")
                    if ($size -eq $PreviewSize) { $previewRel = $rel }
                }

                if (-not $previewRel -and $styleLinks.Count -gt 0) {
                    $m = [regex]::Match($styleLinks[0], '\\(([^)]+)\\)')
                    if ($m.Success) { $previewRel = $m.Groups[1].Value }
                }

                if ($previewRel) {
                    $localEntries.Add([pscustomobject]@{
                        Title = "$DllBase::$name"
                        PreviewRelPath = $previewRel
                        StyleLinks = $styleLinks.ToArray()
                    }) | Out-Null
                }
            }

            return $localEntries
        }

        # Packs soportados (por DLL)
        $allEntries = New-Object 'System.Collections.Generic.List[object]'

        foreach ($e in (Try-ExportPack -ControlTypeName 'MaterialDesignThemes.Wpf.PackIcon' -EnumTypeName 'MaterialDesignThemes.Wpf.PackIconKind')) {
            $allEntries.Add($e) | Out-Null
        }
        foreach ($e in (Try-ExportPack -ControlTypeName 'MahApps.Metro.IconPacks.PackIconMaterial' -EnumTypeName 'MahApps.Metro.IconPacks.PackIconMaterialKind')) {
            $allEntries.Add($e) | Out-Null
        }

        return $allEntries
    }
}

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

namespace Win32
{
    public static class NativeMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint PrivateExtractIcons(
            string szFileName,
            int nIconIndex,
            int cxIcon,
            int cyIcon,
            IntPtr[] phicon,
            uint[] piconid,
            uint nIcons,
            uint flags);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern uint ExtractIconEx(
            string lpszFile,
            int nIconIndex,
            IntPtr[] phiconLarge,
            IntPtr[] phiconSmall,
            uint nIcons);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr hIcon);
    }
}
'@

Add-Type -AssemblyName System.Drawing

function Get-IconCountFromDll {
    param([Parameter(Mandatory)][string]$DllPath)

    # ExtractIconEx con nIconIndex = -1 devuelve el número de iconos.
    try {
        return [int][Win32.NativeMethods]::ExtractIconEx($DllPath, -1, $null, $null, 0)
    } catch {
        throw "No se pudo obtener el conteo de iconos de '$DllPath': $($_.Exception.Message)"
    }
}

function Export-IconPng {
    param(
        [Parameter(Mandatory)][string]$DllPath,
        [Parameter(Mandatory)][int]$IconIndex,
        [Parameter(Mandatory)][int]$Size,
        [Parameter(Mandatory)][string]$DestinationPng
    )

    $hIcons = New-Object IntPtr[] 1
    $iconIds = New-Object UInt32[] 1

    $extracted = [Win32.NativeMethods]::PrivateExtractIcons($DllPath, $IconIndex, $Size, $Size, $hIcons, $iconIds, 1, 0)
    if ($extracted -lt 1 -or $hIcons[0] -eq [IntPtr]::Zero) {
        return $false
    }

    try {
        $icon = [System.Drawing.Icon]::FromHandle($hIcons[0])
        $iconClone = [System.Drawing.Icon]$icon.Clone()
        $bmp = $iconClone.ToBitmap()

        $destDir = Split-Path -Path $DestinationPng -Parent
        if (-not (Test-Path -LiteralPath $destDir)) {
            New-Item -ItemType Directory -Path $destDir | Out-Null
        }

        $bmp.Save($DestinationPng, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        $iconClone.Dispose()
        $icon.Dispose()

        return $true
    } finally {
        [void][Win32.NativeMethods]::DestroyIcon($hIcons[0])
    }
}

function Write-IconsMarkdown {
    param(
        [Parameter(Mandatory)][string]$MdPath,
        [Parameter(Mandatory)][System.Collections.Generic.List[object]]$Entries,
        [Parameter(Mandatory)][int]$Columns,
        [Parameter(Mandatory)][int]$PreviewSize
    )

    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('# Galería de iconos')
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("Generado: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
    [void]$sb.AppendLine()

    $colCount = [Math]::Max(1, $Columns)

    $cells = @()
    $skipped = 0
    foreach ($e in $Entries) {
        if (-not $e -or -not $e.PSObject -or -not $e.PSObject.Properties) {
            $skipped++
            continue
        }

        $pPreview = $e.PSObject.Properties['PreviewRelPath']
        $pTitle = $e.PSObject.Properties['Title']
        $pStyles = $e.PSObject.Properties['StyleLinks']

        if (-not $pPreview -or -not $pTitle -or -not $pStyles) {
            $skipped++
            continue
        }

        $previewRel = ([string]$pPreview.Value).Replace('\\', '/')
        $title = [string]$pTitle.Value
        $styleLinks = ((@($pStyles.Value) | ForEach-Object { $_.ToString().Replace('\\', '/') }) -join ' ')

        $cell = "<img src=`"$previewRel`" width=`"$PreviewSize`" height=`"$PreviewSize`" /><br/><sub>$title</sub><br/><sub>$styleLinks</sub>"
        $cells += $cell
    }

    if ($skipped -gt 0) {
        Write-Warning "Se omitieron $skipped entradas inválidas al generar el Markdown."
    }

    # Tabla Markdown simple
    for ($i = 0; $i -lt $cells.Count; $i += $colCount) {
        $row = $cells[$i..([Math]::Min($i + $colCount - 1, $cells.Count - 1))]

        # header spacer para que GitHub renderice
        $header = ('|' + (('   |') * $row.Count))
        $sep = ('|' + (('---|') * $row.Count))

        [void]$sb.AppendLine($header)
        [void]$sb.AppendLine($sep)
        [void]$sb.AppendLine('|' + ($row -join '|') + '|')
        [void]$sb.AppendLine()
    }

    $dir = Split-Path -Path $MdPath -Parent
    if (-not (Test-Path -LiteralPath $dir)) {
        New-Item -ItemType Directory -Path $dir | Out-Null
    }

    [System.IO.File]::WriteAllText($MdPath, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
}

try {
    if (Test-Path -LiteralPath $errorLogPath) {
        Remove-Item -LiteralPath $errorLogPath -Force -ErrorAction SilentlyContinue
    }

    $dllFiles = @(Resolve-DllFiles -Path $InputPath -Recurse:$Recurse)
    if (-not $dllFiles -or $dllFiles.Count -eq 0) {
        throw "No se encontraron DLLs en '$($InputPath -join '; ')'."
    }

    $imagesRoot = Join-Path $OutputPath 'images'
    $mdPath = Join-Path $OutputPath 'icons.md'

    $entries = New-Object 'System.Collections.Generic.List[object]'

    # Preview: tamaño “medio”. Elegimos 64 si está en Sizes, si no, el más cercano.
    $previewSize = 64
    if ($Sizes -notcontains $previewSize) {
        $previewSize = ($Sizes | Sort-Object { [Math]::Abs($_ - 64) } | Select-Object -First 1)
    }

    foreach ($dll in $dllFiles) {
        $dllPath = $dll.FullName
        $dllBase = [IO.Path]::GetFileNameWithoutExtension($dll.Name)

        Write-Host "Procesando: $dllPath" -ForegroundColor Cyan

        $iconCount = 0
        try { $iconCount = Get-IconCountFromDll -DllPath $dllPath } catch { $iconCount = 0 }

        if ($iconCount -le 0) {
            # No hay icon resources nativos: intentar iconpacks .NET (WPF)
            $packEntries = @()
            try {
                $packEntries = Try-Export-PackIconsFromAssembly -DllPath $dllPath -DllBase $dllBase -ImagesRoot $imagesRoot -Sizes $Sizes -PreviewSize $previewSize
            } catch {
                Write-Warning "No se pudieron exportar iconos WPF desde '$dllPath': $($_.Exception.Message)"
                $packEntries = @()
            }

            # A veces pueden colarse valores extra por el pipeline; quedarnos solo con entradas válidas.
            $packEntries = @(
                $packEntries | Where-Object {
                    $_ -and $_.PSObject -and
                    $_.PSObject.Properties['Title'] -and
                    $_.PSObject.Properties['PreviewRelPath'] -and
                    $_.PSObject.Properties['StyleLinks']
                }
            )

            if ($packEntries -and $packEntries.Count -gt 0) {
                foreach ($pe in $packEntries) { $entries.Add($pe) | Out-Null }
            } else {
                Write-Warning "Sin iconos (nativos o WPF): $dllPath"
            }
            continue
        }

        $dllOutDir = Join-Path $imagesRoot $dllBase

        for ($idx = 0; $idx -lt $iconCount; $idx++) {
            $styleLinks = New-Object 'System.Collections.Generic.List[string]'
            $previewPng = $null

            foreach ($size in $Sizes) {
                $pngName = ('idx{0:D4}_s{1}.png' -f $idx, $size)
                $pngPath = Join-Path $dllOutDir $pngName

                $ok = $true
                if (-not (Test-Path -LiteralPath $pngPath)) {
                    $ok = Export-IconPng -DllPath $dllPath -IconIndex $idx -Size $size -DestinationPng $pngPath
                }

                if ($ok) {
                    $rel = (Join-Path -Path ('images\' + $dllBase) -ChildPath $pngName)
                    $rel = $rel.Replace('\', '/')
                    [void]$styleLinks.Add("[$size]($rel)")

                    if ($size -eq $previewSize) {
                        $previewPng = $rel
                    }
                }
            }

            if (-not $previewPng) {
                # fallback al primer estilo que exista
                if ($styleLinks.Count -gt 0) {
                    $first = $styleLinks[0]
                    $m = [regex]::Match($first, '\\(([^)]+)\\)')
                    if ($m.Success) { $previewPng = $m.Groups[1].Value }
                }
            }

            if (-not $previewPng) {
                continue
            }

            $entries.Add([pscustomobject]@{
                Title = "$dllBase#$idx"
                PreviewRelPath = $previewPng
                StyleLinks = $styleLinks.ToArray()
            }) | Out-Null
        }
    }

    if ($entries.Count -eq 0) {
        throw 'No se pudieron extraer iconos (0 entradas).'
    }

    Write-IconsMarkdown -MdPath $mdPath -Entries $entries -Columns 5 -PreviewSize $previewSize
    Write-Host "\nOK: $mdPath" -ForegroundColor Green
    Write-Host "Imágenes en: $imagesRoot" -ForegroundColor Green
} catch {
    'ERROR' | Out-File -FilePath $errorLogPath -Encoding UTF8
    $_ | Format-List * -Force | Out-String -Width 400 | Out-File -FilePath $errorLogPath -Append -Encoding UTF8
    if ($_.Exception) {
        'EXCEPTION' | Out-File -FilePath $errorLogPath -Append -Encoding UTF8
        $_.Exception | Format-List * -Force | Out-String -Width 400 | Out-File -FilePath $errorLogPath -Append -Encoding UTF8
    }
    throw
}
