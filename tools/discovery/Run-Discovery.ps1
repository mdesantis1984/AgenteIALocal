Set-StrictMode -Version Latest

# Important: do not abort on failures. We capture errors per step and always exit 0.
$global:ErrorActionPreference = 'Continue'

function Get-RepoRoot {
    try {
        $here = Split-Path -Parent $MyInvocation.MyCommand.Path
        $root = Resolve-Path (Join-Path $here '..\..')
        return $root.Path
    } catch {
        return (Get-Location).Path
    }
}

function Ensure-Dir([string]$Path) {
    try {
        if (-not (Test-Path $Path)) {
            New-Item -ItemType Directory -Path $Path -Force | Out-Null
        }
    } catch {
        # ignore
    }
}

function Write-TextFile([string]$Path, [string[]]$Lines) {
    try {
        $dir = Split-Path -Parent $Path
        if ($dir) { Ensure-Dir $dir }
        $Lines | Out-File -FilePath $Path -Encoding UTF8
    } catch {
        # ignore
    }
}

function Invoke-Step {
    param(
        [Parameter(Mandatory=$true)][string]$Id,
        [Parameter(Mandatory=$true)][string]$Description,
        [Parameter(Mandatory=$true)][string]$OutputPath,
        [Parameter(Mandatory=$true)][scriptblock]$Action
    )

    $step = [ordered]@{
        id = $Id
        description = $Description
        startUtc = [DateTime]::UtcNow.ToString('o')
        endUtc = $null
        durationMs = 0
        exitCode = 0
        error = $null
        output = (Split-Path -Leaf $OutputPath)
        command = $null
    }

    $sw = [System.Diagnostics.Stopwatch]::StartNew()

    try {
        & $Action
        $step.exitCode = 0
    } catch {
        $step.exitCode = 1
        try { $step.error = $_.Exception.Message } catch { }
        try {
            # ensure error is visible in the step log
            ("ERROR: " + ($_.Exception.ToString())) | Out-File -FilePath $OutputPath -Append -Encoding UTF8
        } catch { }
    } finally {
        $sw.Stop()
        $step.endUtc = [DateTime]::UtcNow.ToString('o')
        $step.durationMs = [int]$sw.ElapsedMilliseconds
    }

    return $step
}

function Invoke-ExternalCommandToFile {
    param(
        [Parameter(Mandatory=$true)][string]$FilePath,
        [Parameter(Mandatory=$true)][string]$Arguments,
        [Parameter(Mandatory=$true)][string]$LogPath,
        [Parameter(Mandatory=$false)][string]$WorkingDirectory = $null
    )

    $cmdLine = '"' + $FilePath + '" ' + $Arguments

    # Use cmd.exe redirection to preserve %ERRORLEVEL% reliably across PS 5.1/7.
    # Pass arguments as separate tokens so cmd.exe doesn't choke on Program Files paths.
    $full = $cmdLine + ' 1>"' + $LogPath + '" 2>&1'

    if ([string]::IsNullOrWhiteSpace($WorkingDirectory)) {
        & cmd.exe /c $full
        return $LASTEXITCODE
    }

    pushd $WorkingDirectory
    try {
        & cmd.exe /c $full
        return $LASTEXITCODE
    } finally {
        popd
    }
}

function Find-SolutionPath([string]$RepoRoot) {
    try {
        $candidates = Get-ChildItem -Path $RepoRoot -Filter *.sln -File -ErrorAction SilentlyContinue
        if (-not $candidates -or $candidates.Count -eq 0) {
            return $null
        }

        $preferred = $candidates | Where-Object { $_.Name -ieq 'AgenteIALocal.sln' } | Select-Object -First 1
        if ($preferred) { return $preferred.FullName }

        # fallback: first (sorted)
        return ($candidates | Sort-Object FullName | Select-Object -First 1).FullName
    } catch {
        return $null
    }
}

function Write-RepoTreeTop([string]$RepoRoot, [string]$OutFile) {
    try {
        $lines = New-Object System.Collections.Generic.List[string]
        $lines.Add("Root: $RepoRoot")
        $lines.Add('')

        # Depth 3 listing
        $items = Get-ChildItem -Path $RepoRoot -Force -ErrorAction SilentlyContinue
        foreach ($i in ($items | Sort-Object Name)) {
            $lines.Add($i.Name)
            if ($i.PSIsContainer) {
                $lvl2 = Get-ChildItem -Path $i.FullName -Force -ErrorAction SilentlyContinue
                foreach ($j in ($lvl2 | Sort-Object Name)) {
                    $lines.Add('  ' + $j.Name)
                    if ($j.PSIsContainer) {
                        $lvl3 = Get-ChildItem -Path $j.FullName -Force -ErrorAction SilentlyContinue
                        foreach ($k in ($lvl3 | Sort-Object Name)) {
                            $lines.Add('    ' + $k.Name)
                        }
                    }
                }
            }
        }

        Write-TextFile -Path $OutFile -Lines $lines
    } catch {
        Write-TextFile -Path $OutFile -Lines @("Failed to enumerate tree: $($_.Exception.Message)")
    }
}

function Find-MSBuild {
    param([string]$RepoRoot, [string]$OutFile)

    $msbuildPath = $null
    $lines = New-Object System.Collections.Generic.List[string]

    try {
        $vswhere = $null
        try { $vswhere = (Get-Command vswhere -ErrorAction SilentlyContinue).Source } catch { }
        if (-not $vswhere) {
            $defaultVswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
            if (Test-Path $defaultVswhere) { $vswhere = $defaultVswhere }
        }

        if ($vswhere -and (Test-Path $vswhere)) {
            $lines.Add("vswhere: $vswhere")
            try {
                $found = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\\**\\Bin\\MSBuild.exe' 2>$null | Select-Object -First 1
                if ($found -and (Test-Path $found)) {
                    $msbuildPath = $found
                    $lines.Add("MSBuild found via vswhere: $msbuildPath")
                } else {
                    $lines.Add('MSBuild not found via vswhere query.')
                }
            } catch {
                $lines.Add("vswhere query failed: $($_.Exception.Message)")
            }
        } else {
            $lines.Add('vswhere not found.')
        }

        if (-not $msbuildPath) {
            $lines.Add('Trying common Visual Studio MSBuild paths...')
            $roots = @(
                Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio',
                Join-Path ${env:ProgramFiles} 'Microsoft Visual Studio'
            ) | Where-Object { $_ -and (Test-Path $_) }

            foreach ($r in $roots) {
                try {
                    $candidate = Get-ChildItem -Path $r -Recurse -Filter MSBuild.exe -ErrorAction SilentlyContinue |
                        Where-Object { $_.FullName -match '\\MSBuild\\Current\\Bin\\MSBuild\.exe$' } |
                        Sort-Object FullName |
                        Select-Object -First 1

                    if ($candidate) {
                        $msbuildPath = $candidate.FullName
                        $lines.Add("MSBuild found via search: $msbuildPath")
                        break
                    }
                } catch {
                    # ignore
                }
            }
        }

        if (-not $msbuildPath) {
            $lines.Add('MSBuild.exe not found. Will use dotnet build fallback.')
        }

    } catch {
        $lines.Add("MSBuild discovery failed: $($_.Exception.Message)")
    }

    Write-TextFile -Path $OutFile -Lines $lines
    return $msbuildPath
}

$repoRoot = Get-RepoRoot
$timestamp = (Get-Date).ToUniversalTime().ToString('yyyyMMdd-HHmmss')
$outDir = Join-Path $repoRoot (Join-Path 'artifacts\discovery' $timestamp)
Ensure-Dir $outDir

$summaryPath = Join-Path $outDir 'summary.json'

$summary = [ordered]@{
    startUtc = [DateTime]::UtcNow.ToString('o')
    endUtc = $null
    outputDir = $outDir
    repoRoot = $repoRoot
    solution = $null
    msbuildPath = $null
    steps = @()
    overallSuccess = $false
}

# Step: env
$envOut = Join-Path $outDir 'env.txt'
$summary.steps += Invoke-Step -Id 'env' -Description 'Capture environment, PowerShell, OS, user, culture' -OutputPath $envOut -Action {
    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add('=== Discovery Environment ===')
    $lines.Add("UtcNow: " + [DateTime]::UtcNow.ToString('o'))
    $lines.Add("LocalNow: " + (Get-Date).ToString('o'))
    $lines.Add('')
    $lines.Add("RepoRoot: $repoRoot")
    $lines.Add("OutputDir: $outDir")
    $lines.Add('')

    try { $lines.Add("PSVersion: " + $PSVersionTable.PSVersion.ToString()) } catch { }
    try { $lines.Add("PSEdition: " + $PSVersionTable.PSEdition) } catch { }
    try { $lines.Add("CLRVersion: " + $PSVersionTable.CLRVersion) } catch { }
    try { $lines.Add("OS: " + [Environment]::OSVersion.VersionString) } catch { }
    try { $lines.Add("MachineName: " + $env:COMPUTERNAME) } catch { }
    try { $lines.Add("User: " + $env:USERNAME) } catch { }
    try { $lines.Add("Culture: " + [System.Globalization.CultureInfo]::CurrentCulture.Name) } catch { }
    try { $lines.Add("UICulture: " + [System.Globalization.CultureInfo]::CurrentUICulture.Name) } catch { }

    $lines.Add('')
    $lines.Add('=== Key Environment Variables ===')

    $keys = @(
        'PATH','PATHEXT','TEMP','TMP','PROCESSOR_ARCHITECTURE',
        'DOTNET_ROOT','DOTNET_ROOT(x86)','DOTNET_CLI_HOME','DOTNET_MULTILEVEL_LOOKUP','DOTNET_SKIP_FIRST_TIME_EXPERIENCE','DOTNET_NOLOGO',
        'MSBUILD_EXE_PATH','MSBuildSDKsPath','MSBuildExtensionsPath','MSBuildExtensionsPath32','MSBuildExtensionsPath64',
        'VSINSTALLDIR','VisualStudioVersion','VSCMD_VER','VSCMD_ARG_TGT_ARCH','VSCMD_ARG_HOST_ARCH','DevEnvDir'
    )

    foreach ($k in $keys) {
        try {
            $v = [Environment]::GetEnvironmentVariable($k)
            if ($null -ne $v -and $v -ne '') {
                $lines.Add($k + '=' + $v)
            }
        } catch { }
    }

    $lines.Add('')
    $lines.Add('=== Dotnet / MSBuild / VS commands (PATH scan) ===')
    foreach ($cmd in @('dotnet','msbuild','devenv','vswhere','git')) {
        try {
            $c = Get-Command $cmd -ErrorAction SilentlyContinue
            if ($c) { $lines.Add($cmd + ' -> ' + $c.Source) } else { $lines.Add($cmd + ' -> (not found)') }
        } catch { }
    }

    Write-TextFile -Path $envOut -Lines $lines
}

# Step: dotnet
$dotnetInfo = Join-Path $outDir 'dotnet_info.txt'
$dotnetSdks = Join-Path $outDir 'dotnet_sdks.txt'
$dotnetRuntimes = Join-Path $outDir 'dotnet_runtimes.txt'
$summary.steps += Invoke-Step -Id 'dotnet' -Description 'dotnet --info / --list-sdks / --list-runtimes' -OutputPath $dotnetInfo -Action {
    $dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue)
    if (-not $dotnet) {
        Write-TextFile -Path $dotnetInfo -Lines @('dotnet not found in PATH')
        Write-TextFile -Path $dotnetSdks -Lines @('dotnet not found in PATH')
        Write-TextFile -Path $dotnetRuntimes -Lines @('dotnet not found in PATH')
        throw "dotnet not found"
    }

    $ec1 = Invoke-ExternalCommandToFile -FilePath $dotnet.Source -Arguments '--info' -LogPath $dotnetInfo -WorkingDirectory $repoRoot
    $ec2 = Invoke-ExternalCommandToFile -FilePath $dotnet.Source -Arguments '--list-sdks' -LogPath $dotnetSdks -WorkingDirectory $repoRoot
    $ec3 = Invoke-ExternalCommandToFile -FilePath $dotnet.Source -Arguments '--list-runtimes' -LogPath $dotnetRuntimes -WorkingDirectory $repoRoot

    # If any of these fail, mark step as failing (but do not abort script)
    if (($ec1 -ne 0) -or ($ec2 -ne 0) -or ($ec3 -ne 0)) {
        throw "dotnet diagnostics had non-zero exit code(s): info=$ec1 sdks=$ec2 runtimes=$ec3"
    }
}

# Step: git
$gitStatus = Join-Path $outDir 'git_status.txt'
$gitLast5 = Join-Path $outDir 'git_last5.txt'
$summary.steps += Invoke-Step -Id 'git' -Description 'git status / last 5 commits' -OutputPath $gitStatus -Action {
    $git = (Get-Command git -ErrorAction SilentlyContinue)
    if (-not $git) {
        Write-TextFile -Path $gitStatus -Lines @('git not found in PATH')
        Write-TextFile -Path $gitLast5 -Lines @('git not found in PATH')
        throw "git not found"
    }

    # status
    $ecS = Invoke-ExternalCommandToFile -FilePath $git.Source -Arguments 'status --porcelain=v1' -LogPath $gitStatus -WorkingDirectory $repoRoot

    # last commits
    $ecL = Invoke-ExternalCommandToFile -FilePath $git.Source -Arguments 'log -5 --oneline --decorate' -LogPath $gitLast5 -WorkingDirectory $repoRoot

    if (($ecS -ne 0) -or ($ecL -ne 0)) {
        throw "git step had non-zero exit code(s): status=$ecS log=$ecL"
    }
}

# Step: repo
$solutionProjects = Join-Path $outDir 'solution_projects.txt'
$repoTreeTop = Join-Path $outDir 'repo_tree_top.txt'
$summary.steps += Invoke-Step -Id 'repo' -Description 'Locate solution/projects and write top tree' -OutputPath $solutionProjects -Action {
    $sln = Find-SolutionPath -RepoRoot $repoRoot
    $summary.solution = $sln

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("Solution: " + ($(if ($sln) { $sln } else { '(not found)' })))
    $lines.Add('')

    try {
        $csprojs = Get-ChildItem -Path (Join-Path $repoRoot 'src') -Recurse -Filter *.csproj -File -ErrorAction SilentlyContinue |
            Sort-Object FullName
        $lines.Add('Projects under src:')
        foreach ($p in $csprojs) {
            $rel = $p.FullName.Substring($repoRoot.Length).TrimStart('\','/')
            $lines.Add(' - ' + $rel)
        }
    } catch {
        $lines.Add("Failed to enumerate csproj: $($_.Exception.Message)")
    }

    Write-TextFile -Path $solutionProjects -Lines $lines
    Write-RepoTreeTop -RepoRoot $repoRoot -OutFile $repoTreeTop
}

# Step: msbuild_find
$msbuildLocator = Join-Path $outDir 'msbuild_locator.txt'
$summary.steps += Invoke-Step -Id 'msbuild_find' -Description 'Locate MSBuild.exe via vswhere/common paths' -OutputPath $msbuildLocator -Action {
    $msbuild = Find-MSBuild -RepoRoot $repoRoot -OutFile $msbuildLocator
    $summary.msbuildPath = $msbuild
}

# Step: restore
$restoreLog = Join-Path $outDir 'restore.log'
$summary.steps += Invoke-Step -Id 'restore' -Description 'dotnet restore on selected solution' -OutputPath $restoreLog -Action {
    $sln = $summary.solution
    $dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue)

    if (-not $dotnet) {
        Write-TextFile -Path $restoreLog -Lines @('dotnet not found in PATH')
        throw "dotnet not found"
    }

    if ([string]::IsNullOrWhiteSpace($sln) -or (-not (Test-Path $sln))) {
        Write-TextFile -Path $restoreLog -Lines @('Solution not found; skipping restore.')
        throw "solution not found"
    }

    $args = 'restore ' + ('"' + $sln + '"') + ' -v diag'
    $ec = Invoke-ExternalCommandToFile -FilePath $dotnet.Source -Arguments $args -LogPath $restoreLog -WorkingDirectory $repoRoot

    if ($ec -ne 0) {
        throw "dotnet restore failed with exit code $ec"
    }
}

# Step: build
$buildLog = Join-Path $outDir 'build_full_diagnostic.log'
$binlog = Join-Path $outDir 'msbuild.binlog'
$summary.steps += Invoke-Step -Id 'build' -Description 'Build (MSBuild.exe preferred, otherwise dotnet build) with diagnostic logs/binlog' -OutputPath $buildLog -Action {
    $sln = $summary.solution
    if ([string]::IsNullOrWhiteSpace($sln) -or (-not (Test-Path $sln))) {
        Write-TextFile -Path $buildLog -Lines @('Solution not found; skipping build.')
        throw "solution not found"
    }

    $msbuild = $summary.msbuildPath
    if (-not [string]::IsNullOrWhiteSpace($msbuild) -and (Test-Path $msbuild)) {
        $args = ('"' + $sln + '"') + ' /t:Build /p:Configuration=Debug /v:diag /bl:' + ('"' + $binlog + '"')
        $ec = Invoke-ExternalCommandToFile -FilePath $msbuild -Arguments $args -LogPath $buildLog -WorkingDirectory $repoRoot
        if ($ec -ne 0) {
            throw "MSBuild failed with exit code $ec"
        }
    } else {
        $dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue)
        if (-not $dotnet) {
            Write-TextFile -Path $buildLog -Lines @('dotnet not found in PATH')
            throw "dotnet not found"
        }
        $args = 'build ' + ('"' + $sln + '"') + ' -c Debug -v diag'
        $ec = Invoke-ExternalCommandToFile -FilePath $dotnet.Source -Arguments $args -LogPath $buildLog -WorkingDirectory $repoRoot
        if ($ec -ne 0) {
            throw "dotnet build failed with exit code $ec"
        }
    }
}

# Step: copy_known_logs
$copiedLogsDir = Join-Path $outDir 'copied_logs'
$copyLogOut = Join-Path $outDir 'copied_logs\copied_logs_manifest.txt'
$summary.steps += Invoke-Step -Id 'copy_known_logs' -Description 'Copy known log folders (best-effort)' -OutputPath $copyLogOut -Action {
    Ensure-Dir $copiedLogsDir

    $candidates = @(
        Join-Path $repoRoot 'artifacts\build',
        Join-Path $repoRoot 'artifacts\logs',
        Join-Path $repoRoot 'logs',
        Join-Path $repoRoot 'binlogs',
        Join-Path $repoRoot 'artifacts\binlogs'
    )

    $manifest = New-Object System.Collections.Generic.List[string]
    $manifest.Add('Copied logs manifest')
    $manifest.Add("UtcNow: " + [DateTime]::UtcNow.ToString('o'))

    $copiedAny = $false

    foreach ($src in $candidates) {
        if (-not $src) { continue }
        if (-not (Test-Path $src)) { continue }

        try {
            $name = Split-Path -Leaf $src
            $dst = Join-Path $copiedLogsDir $name
            Ensure-Dir $dst

            Copy-Item -Path (Join-Path $src '*') -Destination $dst -Recurse -Force -ErrorAction SilentlyContinue
            $manifest.Add("Copied: $src -> $dst")
            $copiedAny = $true
        } catch {
            $manifest.Add("Copy failed: $src :: $($_.Exception.Message)")
        }
    }

    if (-not $copiedAny) {
        $manifest.Add('No known log folders found.')
    }

    Write-TextFile -Path $copyLogOut -Lines $manifest
}

# Final step: summary
$summaryEnd = [DateTime]::UtcNow
$summary.endUtc = $summaryEnd.ToString('o')

try {
    $restoreStep = $summary.steps | Where-Object { $_.id -eq 'restore' } | Select-Object -First 1
    $buildStep = $summary.steps | Where-Object { $_.id -eq 'build' } | Select-Object -First 1

    $restoreOk = ($restoreStep -and $restoreStep.exitCode -eq 0)
    $buildOk = ($buildStep -and $buildStep.exitCode -eq 0)

    $summary.overallSuccess = ($restoreOk -and $buildOk)
} catch {
    $summary.overallSuccess = $false
}

# Ensure minimal files exist (best-effort). Some are produced by steps already.
try {
    $minFiles = @(
        'summary.json',
        'dotnet_info.txt','dotnet_sdks.txt','dotnet_runtimes.txt',
        'git_status.txt','git_last5.txt',
        'env.txt',
        'msbuild_locator.txt',
        'restore.log',
        'build_full_diagnostic.log',
        'msbuild.binlog',
        'solution_projects.txt',
        'repo_tree_top.txt'
    )

    foreach ($f in $minFiles) {
        $p = Join-Path $outDir $f
        if (-not (Test-Path $p)) {
            try { Write-TextFile -Path $p -Lines @('(not produced)') } catch { }
        }
    }
} catch { }

try {
    $json = $summary | ConvertTo-Json -Depth 8
    $json | Out-File -FilePath $summaryPath -Encoding UTF8
} catch {
    try {
        '{"error":"failed to write summary"}' | Out-File -FilePath $summaryPath -Encoding UTF8
    } catch { }
}

# Always exit 0
exit 0
