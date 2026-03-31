param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SkipRestore
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Ensure-Dotnet {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw ".NET SDK غير موجود. ثبّت .NET SDK 8 أولاً ثم أعد المحاولة."
    }
}

function Remove-DirectoryIfExists {
    param([string]$PathValue)
    if (Test-Path -LiteralPath $PathValue) {
        Remove-Item -LiteralPath $PathValue -Recurse -Force
    }
}

function Copy-DeltaFiles {
    param(
        [string]$PublishDir,
        [string]$DeltaDir
    )

    $includePatterns = @(
        "Lumina.Host.exe",
        "Lumina.Host.dll",
        "Lumina.Host.pdb",
        "Lumina.Host.deps.json",
        "Lumina.Host.runtimeconfig.json",
        "Lumina.*.dll",
        "Lumina.*.pdb",
        "NLua.dll",
        "KeraLua.dll",
        "Microsoft.Extensions.*.dll",
        "System.Speech.dll"
    )

    foreach ($pattern in $includePatterns) {
        $matches = Get-ChildItem -Path $PublishDir -Filter $pattern -Recurse -File -ErrorAction SilentlyContinue
        foreach ($match in $matches) {
            $relativePath = $match.FullName.Substring($PublishDir.Length).TrimStart("\")
            $destinationPath = Join-Path $DeltaDir $relativePath
            $destinationDirectory = Split-Path -Parent $destinationPath
            if (-not (Test-Path -LiteralPath $destinationDirectory)) {
                New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
            }

            Copy-Item -LiteralPath $match.FullName -Destination $destinationPath -Force
        }
    }

    foreach ($directoryName in @("scripts", "config")) {
        $sourceDirectory = Join-Path $PublishDir $directoryName
        if (-not (Test-Path -LiteralPath $sourceDirectory)) {
            continue
        }

        $destinationDirectory = Join-Path $DeltaDir $directoryName
        Copy-Item -LiteralPath $sourceDirectory -Destination $destinationDirectory -Recurse -Force
    }
}

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -LiteralPath $repoRoot

$solutionPath = Join-Path $repoRoot "Lumina.sln"
$projectPath = Join-Path $repoRoot "src\Lumina.Host\Lumina.Host.csproj"
$artifactsRoot = Join-Path $repoRoot "artifacts"
$publishDir = Join-Path $artifactsRoot "Lumina.Host"
$deltaDir = Join-Path $artifactsRoot "Lumina.Host.Delta"
$deltaZipPath = Join-Path $artifactsRoot "lumina-windows-delta.zip"

Write-Step "Checking prerequisites"
Ensure-Dotnet
if (-not (Test-Path -LiteralPath $solutionPath)) {
    throw "لم يتم العثور على Lumina.sln في المسار: $solutionPath"
}
if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "لم يتم العثور على Lumina.Host.csproj في المسار: $projectPath"
}

if (-not $SkipRestore) {
    Write-Step "Restoring solution packages"
    dotnet restore $solutionPath
}

Write-Step "Building solution ($Configuration)"
dotnet build $solutionPath --configuration $Configuration --no-restore

Write-Step "Publishing Lumina.Host ($Runtime, self-contained)"
Remove-DirectoryIfExists -PathValue $publishDir
dotnet publish $projectPath --configuration $Configuration --runtime $Runtime --self-contained true --output $publishDir

Write-Step "Preparing delta package"
Remove-DirectoryIfExists -PathValue $deltaDir
New-Item -ItemType Directory -Path $deltaDir -Force | Out-Null
Copy-DeltaFiles -PublishDir $publishDir -DeltaDir $deltaDir

Write-Step "Creating delta zip"
if (Test-Path -LiteralPath $deltaZipPath) {
    Remove-Item -LiteralPath $deltaZipPath -Force
}
Compress-Archive -Path (Join-Path $deltaDir "*") -DestinationPath $deltaZipPath -Force

$zipSizeMb = [math]::Round(((Get-Item -LiteralPath $deltaZipPath).Length / 1MB), 2)
Write-Host ""
Write-Host "Build completed successfully." -ForegroundColor Green
Write-Host "Publish folder: $publishDir"
Write-Host "Delta folder  : $deltaDir"
Write-Host "Delta zip     : $deltaZipPath ($zipSizeMb MB)"
