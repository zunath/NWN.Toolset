<#
.SYNOPSIS
  Restores, builds, tests and packs the Nwn.* NuGet packages. Never pushes anything.

.DESCRIPTION
  Steps: restore, Release build, all four test projects (any failure aborts), then pack
  Nwn.Formats, Nwn.Authoring, Nwn.Preview and Nwn.Toolset.Avalonia (.nupkg and .snupkg) into
  the output directory. Existing package files are never overwritten. Finally prints the
  SHA-256 of each file and the exact `dotnet nuget push` commands for the owner to run.

  Environment variables are passed through to the test run untouched. The corpus tests read
  XENOMECH_TEST_CONTENT_ROOT and SWLOR_TEST_HAKS_ROOT when they are set.

.PARAMETER Version
  Package version. Defaults to NwnToolsetVersion from Directory.Build.props.

.PARAMETER OutputDirectory
  Where the packages go. Defaults to artifacts/release/<version> under the repository root.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools/Pack-Packages.ps1
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot 'NWN.Toolset.sln'
$packages = @('Nwn.Formats', 'Nwn.Authoring', 'Nwn.Preview', 'Nwn.Toolset.Avalonia')
$testProjects = @(
    'tests/Nwn.Formats.Tests/Nwn.Formats.Tests.csproj',
    'tests/Nwn.Authoring.Tests/Nwn.Authoring.Tests.csproj',
    'tests/Nwn.Preview.Tests/Nwn.Preview.Tests.csproj',
    'tests/Nwn.Toolset.Avalonia.Tests/Nwn.Toolset.Avalonia.Tests.csproj'
)

function Invoke-Dotnet {
    param([string[]]$Arguments)
    Write-Host ("dotnet " + ($Arguments -join ' '))
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments[0]) failed with exit code $LASTEXITCODE."
    }
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $propsText = Get-Content -LiteralPath (Join-Path $repoRoot 'Directory.Build.props') -Raw
    $match = [regex]::Match($propsText, '<NwnToolsetVersion[^>]*>([^<]+)</NwnToolsetVersion>')
    if ($match.Success) { $Version = $match.Groups[1].Value.Trim() }
}
if ([string]::IsNullOrWhiteSpace($Version)) {
    throw 'Could not determine the version. Pass -Version.'
}
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot (Join-Path 'artifacts\release' $Version)
}
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)

# Refuse to overwrite any existing package file before doing any work.
$expected = @()
foreach ($id in $packages) {
    $expected += (Join-Path $OutputDirectory "$id.$Version.nupkg")
    $expected += (Join-Path $OutputDirectory "$id.$Version.snupkg")
}
$existing = @($expected | Where-Object { Test-Path -LiteralPath $_ })
if ($existing.Count -gt 0) {
    throw ("Refusing to overwrite existing package files:`n  " + ($existing -join "`n  "))
}

if (-not $env:XENOMECH_TEST_CONTENT_ROOT -or -not $env:SWLOR_TEST_HAKS_ROOT) {
    Write-Warning 'XENOMECH_TEST_CONTENT_ROOT and/or SWLOR_TEST_HAKS_ROOT is not set; corpus tests may skip or fail.'
}

Write-Host "Version: $Version"
Write-Host "Output:  $OutputDirectory"

$versionArg = "-p:Version=$Version"
Push-Location $repoRoot
try {
    Invoke-Dotnet @('restore', $solution)
    Invoke-Dotnet @('build', $solution, '-c', 'Release', '--no-restore', $versionArg)

    foreach ($project in $testProjects) {
        Invoke-Dotnet @('test', (Join-Path $repoRoot $project), '-c', 'Release', '--no-build', $versionArg)
    }

    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
    foreach ($id in $packages) {
        $project = Join-Path $repoRoot "src\$id\$id.csproj"
        Invoke-Dotnet @('pack', $project, '-c', 'Release', '--no-build', $versionArg, '-o', $OutputDirectory)
    }
}
finally {
    Pop-Location
}

$missing = @($expected | Where-Object { -not (Test-Path -LiteralPath $_) })
if ($missing.Count -gt 0) {
    throw ("Expected package files were not produced:`n  " + ($missing -join "`n  "))
}

Write-Host ''
Write-Host 'SHA-256:'
foreach ($file in $expected) {
    $hash = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash
    Write-Host ("  {0}  {1}" -f $hash, (Split-Path -Leaf $file))
}

Write-Host ''
Write-Host 'Nothing was pushed. To publish, set NUGET_API_KEY to your scoped key and run:'
foreach ($id in $packages) {
    # The .snupkg is pushed automatically alongside its .nupkg.
    $nupkg = Join-Path $OutputDirectory "$id.$Version.nupkg"
    Write-Host ('  dotnet nuget push "{0}" --source https://api.nuget.org/v3/index.json --api-key $env:NUGET_API_KEY' -f $nupkg)
}
Write-Host 'Push in this order so dependencies exist first: Formats, Authoring, Preview, Toolset.Avalonia.'
