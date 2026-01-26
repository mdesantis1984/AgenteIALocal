<#
.SYNOPSIS
  Descarga banderas PNG desde FlagCDN y las guarda con nombre ISO idioma-país (BCP-47: ll-CC.png), reemplazando siempre.

.DESCRIPTION
  - Obtiene códigos de bandera desde: https://flagcdn.com/en/codes.json
  - Determina el idioma principal por territorio usando CLDR territoryInfo (languagePopulation):
      https://raw.githubusercontent.com/unicode-org/cldr-json/main/cldr-json/cldr-core/supplemental/territoryInfo.json
  - Descarga cada bandera desde: https://flagcdn.com/<Size>/<cc>.png  (cc en minúsculas)
  - Guarda como: <lang>-<CC>.png (lang minúsculas, CC mayúsculas)
  - Reemplaza SIEMPRE.

.PARAMETER ProjectRoot
  Raíz del repo (por defecto "."). Si ejecutas desde /tools, usa "..".

.PARAMETER Size
  Tamaño en formato "32x24", "64x48", "w80", "h40", etc.

.PARAMETER CountryCodes
  Opcional. Si lo pasas, procesa solo esos códigos (2 letras). Ej: -CountryCodes ar,us,ec,es

.PARAMETER NoPlaceholderOnFail
  Si se especifica, NO crea placeholder cuando falla una descarga.

.EXAMPLE
  powershell.exe -ExecutionPolicy Bypass -File .\create-flag-placeholders-all.ps1 -ProjectRoot ..
#>

[CmdletBinding()]
param(
  [string]$ProjectRoot = ".",
  [string]$Size = "h40",
  [string[]]$CountryCodes,
  [switch]$NoPlaceholderOnFail,
  # Si NO se especifica, se omite la descarga cuando ya existe un PNG válido.
  # Útil para evitar re-descargas masivas.
  [switch]$ForceRedownload,
  # Si el PNG existente es muy pequeño (típico placeholder), se reintenta descargar.
  # Default 1024 bytes.
  [int]$MinBytesToKeep = 1024
)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Permite que el usuario pase tamaños con el signo de multiplicación Unicode (×)
# (ej: "36×24"), normalizándolo a "36x24" para la URL de FlagCDN.
$Size = $Size.Replace('×','x')

# TLS 1.2 (mejora compatibilidad en Windows PowerShell 5.1)
try { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 } catch { }

$languageRoot = Join-Path $ProjectRoot "src\AgenteIALocalVSIX\Languages"
$flagsDir     = Join-Path $languageRoot "flags\img"
if (!(Test-Path $flagsDir)) { New-Item -Path $flagsDir -ItemType Directory -Force | Out-Null }

function Invoke-WebRequestCompat {
  param(
    [Parameter(Mandatory=$true)][string]$Uri,
    [string]$OutFile
  )
  $inv = @{
    Uri               = $Uri
    Headers           = @{"User-Agent"="Mozilla/5.0"}
    MaximumRedirection= 5
    ErrorAction       = "Stop"
  }
  if ($PSVersionTable.PSVersion.Major -lt 6) { $inv.UseBasicParsing = $true }
  if ($OutFile) { $inv.OutFile = $OutFile }
  return Invoke-WebRequest @inv
}

function Test-PngFile([string]$path) {
  if (!(Test-Path $path)) { return $false }
  try {
    $bytes = [System.IO.File]::ReadAllBytes($path)
    if ($bytes.Length -lt 16) { return $false }
    $sig = @(0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A)
    for ($i=0; $i -lt $sig.Count; $i++) {
      if ($bytes[$i] -ne $sig[$i]) { return $false }
    }
    return $true
  } catch { return $false }
}

function Get-SizeWH([string]$size) {
  # Soporta "36x24" (y variantes). Si no matchea, devuelve 32x24.
  $m = [regex]::Match($size, '^(\d{1,4})x(\d{1,4})$')
  if ($m.Success) {
    return @([int]$m.Groups[1].Value, [int]$m.Groups[2].Value)
  }
  return @(32,24)
}

function Create-Placeholder([string]$tag, [string]$outFile, [string]$size) {
  Add-Type -AssemblyName System.Drawing
  $wh = Get-SizeWH $size
  $width  = $wh[0]
  $height = $wh[1]

  $bmp = New-Object System.Drawing.Bitmap($width, $height)
  $g   = [System.Drawing.Graphics]::FromImage($bmp)
  try {
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighSpeed
    $g.Clear([System.Drawing.Color]::FromArgb(255, 210, 210, 210))

    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 120, 120, 120), 1)
    $g.DrawRectangle($pen, 0, 0, $width-1, $height-1)
    $pen.Dispose()

    $text = $tag
    if ($text.Length -gt 7) { $text = $text.Substring(0,7) }

    $font     = New-Object System.Drawing.Font("Arial", 7, [System.Drawing.FontStyle]::Bold)
    $textSize = $g.MeasureString($text, $font)
    $x = [Math]::Max(0, ($width  - $textSize.Width)  / 2)
    $y = [Math]::Max(0, ($height - $textSize.Height) / 2)
    $g.DrawString($text, $font, [System.Drawing.Brushes]::Black, $x, $y)
    $font.Dispose()
  } finally {
    $g.Dispose()
    $bmp.Save($outFile, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
  }
}

function Get-FlagCdnCodes {
  $codesUrl = "https://flagcdn.com/en/codes.json"
  $resp = Invoke-WebRequestCompat -Uri $codesUrl
  $obj = $resp.Content | ConvertFrom-Json
  return ($obj.PSObject.Properties | ForEach-Object { $_.Name.ToLowerInvariant() }) | Sort-Object -Unique
}

function Get-CldrTerritoryInfo {
  $cldrUrl = "https://raw.githubusercontent.com/unicode-org/cldr-json/main/cldr-json/cldr-core/supplemental/territoryInfo.json"
  $resp = Invoke-WebRequestCompat -Uri $cldrUrl
  return ($resp.Content | ConvertFrom-Json).supplemental.territoryInfo
}

function Normalize-LanguageCode([string]$langKey) {
  if ([string]::IsNullOrWhiteSpace($langKey)) { return $null }
  $k = $langKey.Trim()

  # CLDR a veces usa "_" para subtags (ej: zh_Hant). Nos quedamos con el language subtag.
  $k = $k.Replace('_','-')
  $k = $k.Split('-')[0]

  if ($k -match '^[A-Za-z]{2,3}$') { return $k.ToLowerInvariant() }
  return $null
}

function Get-PrimaryLanguageForTerritory($territoryInfo, [string]$cc) {
  $CC = $cc.ToUpperInvariant()
  # Importante: paréntesis para evitar precedencia incorrecta de -not
  if (-not ($territoryInfo.PSObject.Properties.Name -contains $CC)) { return "en" }

  $ti = $territoryInfo.$CC
  if ($null -eq $ti -or $null -eq $ti.languagePopulation) { return "en" }

  $bestLang = $null
  $bestPct = -1.0

  foreach ($p in $ti.languagePopulation.PSObject.Properties) {
    $lang = Normalize-LanguageCode $p.Name
    if ($null -eq $lang) { continue }
    if ($lang -eq "und") { continue }

    $lp = $p.Value
    if ($null -eq $lp -or $null -eq $lp._populationPercent) { continue }

    $pct = 0.0
    try { $pct = [double]$lp._populationPercent } catch { continue }

    if ($pct -gt $bestPct) {
      $bestPct = $pct
      $bestLang = $lang
    }
  }

  if ([string]::IsNullOrWhiteSpace($bestLang)) { return "en" }
  return $bestLang
}

function Download-Flag([string]$cc, [string]$outFile, [string]$size) {
  $url = "https://flagcdn.com/$size/$($cc.ToLowerInvariant()).png"
  try {
    # Reintentos simples para reducir fallos transitorios / rate limit.
    for ($i=0; $i -lt 3; $i++) {
      try {
        Invoke-WebRequestCompat -Uri $url -OutFile $outFile | Out-Null
        if (Test-PngFile $outFile) { return $true }
      } catch {
        # continúa
      }
      Start-Sleep -Milliseconds (250 * ($i + 1))
    }
    return $false
  } catch {
    return $false
  }
}

# Resolver códigos a procesar
$codes =
  if ($CountryCodes -and $CountryCodes.Count -gt 0) {
    $CountryCodes | ForEach-Object { $_.Trim().ToLowerInvariant() } | Where-Object { $_ -match '^[a-z]{2}$' } | Sort-Object -Unique
  } else {
    Get-FlagCdnCodes
  }

$territoryInfo = Get-CldrTerritoryInfo

Write-Host "Flags dir: $flagsDir" -ForegroundColor Cyan
Write-Host "Size:      $Size" -ForegroundColor Cyan
Write-Host "Codes:     $($codes.Count)" -ForegroundColor Cyan
Write-Host ""

$ok = 0
$failed = 0
$placeholders = 0

foreach ($cc in $codes) {
  $lang = Get-PrimaryLanguageForTerritory -territoryInfo $territoryInfo -cc $cc
  $fileName = "{0}-{1}.png" -f $lang.ToLowerInvariant(), $cc.ToUpperInvariant()
  $outFile = Join-Path $flagsDir $fileName

  # Por defecto: NO re-descargar si ya existe un PNG válido y no parece placeholder.
  # (Muchos placeholders quedan muy pequeños; si el archivo es < MinBytesToKeep se reintenta descargar.)
  if (-not $ForceRedownload -and (Test-PngFile $outFile)) {
    $len = (Get-Item -LiteralPath $outFile).Length
    if ($len -ge $MinBytesToKeep) {
      Write-Host ("  = {0} -> {1} (ya existe, omitido)" -f $cc, $fileName) -ForegroundColor DarkGray
      continue
    }
    Write-Host ("  ~ {0} -> {1} (parece placeholder: {2} bytes, reintentando descarga)" -f $cc, $fileName, $len) -ForegroundColor DarkYellow
  }

  # Si vamos a descargar/placeholder, limpiamos el destino
  if (Test-Path $outFile) { Remove-Item -Path $outFile -Force -ErrorAction SilentlyContinue }

  $downloaded = Download-Flag -cc $cc -outFile $outFile -size $Size
  if ($downloaded) {
    Write-Host ("  ✓ {0} -> {1}" -f $cc, $fileName) -ForegroundColor Green
    $ok++
    continue
  }

  $failed++
  if (-not $NoPlaceholderOnFail) {
    Create-Placeholder -tag ($lang + "-" + $cc.ToUpperInvariant()) -outFile $outFile -size $Size
    Write-Host ("  ! {0} -> {1} (falló descarga, placeholder creado)" -f $cc, $fileName) -ForegroundColor Yellow
    $placeholders++
  } else {
    Write-Host ("  x {0} -> {1} (falló descarga)" -f $cc, $fileName) -ForegroundColor Red
  }
}

Write-Host ""
Write-Host "Resumen:" -ForegroundColor Cyan
Write-Host "  Descargadas OK:  $ok" -ForegroundColor Green
Write-Host "  Fallidas:        $failed" -ForegroundColor Yellow
Write-Host "  Placeholders:    $placeholders" -ForegroundColor Yellow
