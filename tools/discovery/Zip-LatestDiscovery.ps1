Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    try {
        $here = Split-Path -Parent $MyInvocation.MyCommand.Path
        $root = Resolve-Path (Join-Path $here '..\..')
        return $root.Path
    } catch {
        return (Get-Location).Path
    }
}

$repoRoot = Get-RepoRoot
$discoveryRoot = Join-Path $repoRoot 'artifacts\discovery'

if (-not (Test-Path $discoveryRoot)) {
    Write-Host "Discovery folder not found: $discoveryRoot"
    exit 0
}

$latest = Get-ChildItem -Path $discoveryRoot -Directory -ErrorAction SilentlyContinue |
    Sort-Object Name -Descending |
    Select-Object -First 1

if (-not $latest) {
    Write-Host "No discovery runs found under: $discoveryRoot"
    exit 0
}

$zipPath = Join-Path $discoveryRoot ($latest.Name + '.zip')

try {
    if (Test-Path $zipPath) {
        Remove-Item -Force $zipPath -ErrorAction SilentlyContinue
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($latest.FullName, $zipPath, [System.IO.Compression.CompressionLevel]::Optimal, $false)

    Write-Host "Created: $zipPath"
} catch {
    Write-Host "Zip creation failed: $($_.Exception.Message)"
}

exit 0
