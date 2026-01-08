# Discovery (build/restore/VSIX diagnostics)

This folder contains a **reproducible discovery package** to collect diagnostics about `restore`, `build`, MSBuild/VS installation, and repo state.

## Requirements

- Windows
- PowerShell **Windows PowerShell 5.1** or **PowerShell 7**
- Tools available in `PATH` (as applicable): `git`, `dotnet`, `MSBuild.exe` and/or `devenv`, `vswhere` (optional)

No additional dependencies are required.

## Run

From the **repo root**:

```powershell
powershell -ExecutionPolicy Bypass -File tools/discovery/Run-Discovery.ps1
```

PowerShell 7:

```powershell
pwsh -File tools/discovery/Run-Discovery.ps1
```

## Output

A new timestamped directory is created under:

- `artifacts/discovery/{yyyyMMdd-HHmmss}/`

Minimum expected files include (see `summary.json` for the full list and step results):

- `summary.json`
- `env.txt`
- `dotnet_info.txt`, `dotnet_sdks.txt`, `dotnet_runtimes.txt`
- `git_status.txt`, `git_last5.txt`
- `msbuild_locator.txt`
- `restore.log`
- `build_full_diagnostic.log`
- `msbuild.binlog` (only when `MSBuild.exe` is found)

The script **never fails the process**: it always exits with code `0`. Any failures are recorded per-step in `summary.json`.

## Zip latest output

To create a zip for the **latest** discovery run:

```powershell
powershell -ExecutionPolicy Bypass -File tools/discovery/Zip-LatestDiscovery.ps1
```

This creates:

- `artifacts/discovery/{latestTimestamp}.zip`

## Attach artifacts

Attach the generated zip file (or the entire timestamp folder) when reporting issues.

- Share `summary.json` and `build_full_diagnostic.log` at minimum.
- If present, include `msbuild.binlog` (opens in MSBuild Structured Log Viewer).
