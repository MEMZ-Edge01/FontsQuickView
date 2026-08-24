[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$')]
    [string]$Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $repositoryRoot 'FontsQuickView.csproj'
$releaseRoot = Join-Path $repositoryRoot "artifacts\v$Version"
$publishDirectory = Join-Path $releaseRoot 'FontsQuickView-win-x64'
$publishOutputPath = $publishDirectory + [IO.Path]::DirectorySeparatorChar
$archivePath = Join-Path $releaseRoot 'FontsQuickView-win-x64.zip'

if (Test-Path -LiteralPath $releaseRoot)
{
    throw "Release output already exists: $releaseRoot"
}

New-Item -ItemType Directory -Path $releaseRoot | Out-Null

# WinUI's dotnet publish target can omit the app's XBF/PRI files with some SDK
# combinations. A self-contained build output contains the complete unpackaged app.
& dotnet build $projectPath `
    -c Release `
    -r win-x64 `
    -p:Platform=x64 `
    -p:Version=$Version `
    --self-contained true `
    -p:WindowsAppSDKSelfContained=true `
    -p:OutputPath=$publishOutputPath `
    -p:AppendTargetFrameworkToOutputPath=false `
    -p:AppendRuntimeIdentifierToOutputPath=false

if ($LASTEXITCODE -ne 0)
{
    throw "Release build failed with exit code $LASTEXITCODE."
}

$requiredFiles = @(
    'FontsQuickView.exe',
    'App.xbf',
    'MainWindow.xbf',
    'FontsQuickView.pri'
)

foreach ($requiredFile in $requiredFiles)
{
    $requiredPath = Join-Path $publishDirectory $requiredFile
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf))
    {
        throw "Release output is incomplete; missing $requiredFile."
    }
}

$forbiddenScreenshot = Join-Path $publishDirectory 'screenshot.png'
if (Test-Path -LiteralPath $forbiddenScreenshot)
{
    throw 'Release output contains the local development screenshot.'
}

$applicationSymbols = Join-Path $publishDirectory 'FontsQuickView.pdb'
if (Test-Path -LiteralPath $applicationSymbols)
{
    Remove-Item -LiteralPath $applicationSymbols
}

Compress-Archive `
    -Path (Join-Path $publishDirectory '*') `
    -DestinationPath $archivePath `
    -CompressionLevel Optimal

$publishedFiles = Get-ChildItem -LiteralPath $publishDirectory -Recurse -File
$archive = Get-Item -LiteralPath $archivePath
$archiveHash = Get-FileHash -LiteralPath $archivePath -Algorithm SHA256

[pscustomobject]@{
    Version = $Version
    FileCount = $publishedFiles.Count
    UncompressedMiB = [math]::Round((($publishedFiles | Measure-Object -Property Length -Sum).Sum / 1MB), 2)
    ArchiveMiB = [math]::Round(($archive.Length / 1MB), 2)
    SHA256 = $archiveHash.Hash
    Archive = $archive.FullName
}
