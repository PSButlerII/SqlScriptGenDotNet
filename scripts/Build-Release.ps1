[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string] $Version
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repositoryRoot 'src\SqlScriptGen.Cli\SqlScriptGen.Cli.csproj'
$releaseRoot = Join-Path $repositoryRoot "artifacts\release\$Version"
$winPublish = Join-Path $releaseRoot 'win-x64'
$linuxPublish = Join-Path $releaseRoot 'linux-x64'
$winPackage = Join-Path $releaseRoot "SqlScriptGenDotNet-$Version-win-x64.zip"
$linuxPackage = Join-Path $releaseRoot "SqlScriptGenDotNet-$Version-linux-x64.tar.gz"

if (Test-Path -LiteralPath $releaseRoot) {
    throw "Release output already exists: $releaseRoot. Move or remove that exact version directory before rebuilding."
}

New-Item -ItemType Directory -Path $winPublish, $linuxPublish | Out-Null

dotnet publish $project --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false --output $winPublish
if ($LASTEXITCODE -ne 0) { throw 'Windows publish failed.' }

dotnet publish $project --configuration Release --runtime linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false --output $linuxPublish
if ($LASTEXITCODE -ne 0) { throw 'Linux publish failed.' }

foreach ($directory in @($winPublish, $linuxPublish)) {
    $xmlDocumentation = Join-Path $directory 'SqlScriptGen.Core.xml'
    if (Test-Path -LiteralPath $xmlDocumentation) {
        Remove-Item -LiteralPath $xmlDocumentation
    }
    Copy-Item -LiteralPath (Join-Path $repositoryRoot 'README.md') -Destination $directory
    Copy-Item -LiteralPath (Join-Path $repositoryRoot 'LICENSE') -Destination $directory
    Copy-Item -LiteralPath (Join-Path $repositoryRoot 'docs\release-usage.md') -Destination (Join-Path $directory 'USAGE.md')
}

Compress-Archive -Path (Join-Path $winPublish '*') -DestinationPath $winPackage -CompressionLevel Optimal

Push-Location $linuxPublish
try {
    git init --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Temporary Linux package index creation failed.' }
    git add LICENSE README.md USAGE.md sqlscriptgen
    git update-index --chmod=+x sqlscriptgen
    $tree = git write-tree
    if ($LASTEXITCODE -ne 0) { throw 'Temporary Linux package tree creation failed.' }
    git archive --format=tar.gz --output=$linuxPackage $tree
    if ($LASTEXITCODE -ne 0) { throw 'Linux archive creation failed.' }
}
finally {
    Pop-Location
    $temporaryGitDirectory = Join-Path $linuxPublish '.git'
    if (Test-Path -LiteralPath $temporaryGitDirectory) {
        Remove-Item -LiteralPath $temporaryGitDirectory -Recurse -Force
    }
}

$archives = @($winPackage, $linuxPackage)
$checksumLines = foreach ($archive in $archives) {
    $hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $(Split-Path -Leaf $archive)"
}
$checksumLines | Set-Content -LiteralPath (Join-Path $releaseRoot 'SHA256SUMS.txt') -Encoding ascii

Write-Output "Release artifacts created in $releaseRoot"
