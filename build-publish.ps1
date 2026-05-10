$ErrorActionPreference = "Stop"

# --- Adjust if needed ---
$RepoRoot = $PSScriptRoot
$PublishDir = "D:\SteamLibrary\steamapps\common\RimWorld\Mods\DyzePathogenics"
$ProjectPath = Join-Path $RepoRoot "Sources\DyzePathogenics\DyzePathogenics.csproj"

Write-Host "Building Release DLL..."
dotnet build $ProjectPath -c Release

Write-Host "Preparing publish folder..."
if (Test-Path $PublishDir) {
    Remove-Item $PublishDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null

$foldersToCopy = @(
    "About",
    "Assemblies",
    "Defs",
    "Languages",
    "Patches",
    "Textures"
)

foreach ($folder in $foldersToCopy) {
    $source = Join-Path $RepoRoot $folder
    $target = Join-Path $PublishDir $folder

    if (Test-Path $source) {
        robocopy $source $target /E /NFL /NDL /NJH /NJS /NC /NS `
            /XF "0Harmony.dll" "Assembly-CSharp.dll" "UnityEngine*.dll" `
            /XD "bin" "obj" ".git" ".vs" ".idea" | Out-Null

        if ($LASTEXITCODE -gt 7) {
            throw "Robocopy failed for $folder with exit code $LASTEXITCODE"
        }
    }
}

$filesToCopy = @(
    "README.md",
    "LICENSE.txt",
    "CHANGELOG.md"
)

foreach ($file in $filesToCopy) {
    $source = Join-Path $RepoRoot $file
    if (Test-Path $source) {
        Copy-Item $source $PublishDir -Force
    }
}

Write-Host "Checking forbidden files..."
$forbidden = Get-ChildItem $PublishDir -Recurse -File |
    Where-Object {
        $_.Name -eq "0Harmony.dll" -or
        $_.Name -eq "Assembly-CSharp.dll" -or
        $_.Name -like "UnityEngine*.dll"
    }

if ($forbidden) {
    $forbidden | ForEach-Object { Write-Host $_.FullName }
    throw "Forbidden DLLs found in publish folder."
}

Write-Host "Publish folder ready:"
Write-Host $PublishDir