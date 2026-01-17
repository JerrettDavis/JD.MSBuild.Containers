#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build script for JD.MSBuild.Containers and samples
.DESCRIPTION
    Orchestrates building the library, publishing to local NuGet feed,
    building samples, and optionally containerizing and running them.
.PARAMETER Target
    Build target: Build, Pack, RestoreSamples, BuildSamples, Containerize, Run, Test, Clean, All
.PARAMETER Configuration
    Build configuration: Debug or Release (default: Release)
.PARAMETER SkipTests
    Skip running tests
.PARAMETER SkipDocker
    Skip Docker operations
.EXAMPLE
    ./build.ps1 -Target All
    ./build.ps1 -Target BuildSamples -Configuration Debug
#>
param(
    [Parameter(Position = 0)]
    [ValidateSet('Build', 'Pack', 'RestoreSamples', 'BuildSamples', 'Containerize', 'Run', 'Test', 'Clean', 'All')]
    [string]$Target = 'All',
    
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [Parameter()]
    [switch]$SkipTests,
    
    [Parameter()]
    [switch]$SkipDocker
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# Paths
$RepoRoot = $PSScriptRoot
$LocalNuGetPath = Join-Path $RepoRoot 'local-nuget'
$SamplesPath = Join-Path $RepoRoot 'samples'
$SolutionFile = Join-Path $RepoRoot 'JD.MSBuild.Containers.sln'

# Colors for output
function Write-Header($Message) {
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
}

function Write-Success($Message) {
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Info($Message) {
    Write-Host "→ $Message" -ForegroundColor Blue
}

function Write-Warning($Message) {
    Write-Host "⚠ $Message" -ForegroundColor Yellow
}

function Write-ErrorMsg($Message) {
    Write-Host "✗ $Message" -ForegroundColor Red
}

# Clean target
function Invoke-Clean {
    Write-Header "Cleaning build artifacts"
    
    Write-Info "Cleaning solution..."
    dotnet clean $SolutionFile --configuration $Configuration
    
    Write-Info "Removing bin/obj directories..."
    Get-ChildItem -Path $RepoRoot -Include bin,obj -Recurse -Directory | Remove-Item -Recurse -Force
    
    Write-Info "Removing local NuGet feed..."
    if (Test-Path $LocalNuGetPath) {
        Remove-Item $LocalNuGetPath -Recurse -Force
    }
    
    Write-Info "Removing generated Dockerfiles..."
    Get-ChildItem -Path $SamplesPath -Filter Dockerfile -Recurse | Remove-Item -Force
    
    Write-Success "Clean completed"
}

# Build library
function Invoke-Build {
    Write-Header "Building JD.MSBuild.Containers"
    
    Write-Info "Restoring packages..."
    dotnet restore $SolutionFile --use-lock-file
    
    Write-Info "Building solution (Configuration: $Configuration)..."
    dotnet build $SolutionFile --configuration $Configuration --no-restore
    
    Write-Success "Build completed"
}

# Run tests
function Invoke-Test {
    if ($SkipTests) {
        Write-Warning "Skipping tests (SkipTests flag set)"
        return
    }
    
    Write-Header "Running tests"
    
    Write-Info "Executing test suite..."
    dotnet test $SolutionFile --configuration $Configuration --no-build --verbosity normal
    
    Write-Success "Tests completed"
}

# Pack library to local NuGet feed
function Invoke-Pack {
    Write-Header "Packing to local NuGet feed"
    
    Write-Info "Creating local NuGet directory..."
    if (-not (Test-Path $LocalNuGetPath)) {
        New-Item -ItemType Directory -Path $LocalNuGetPath | Out-Null
    }
    
    Write-Info "Packing JD.MSBuild.Containers..."
    dotnet pack $SolutionFile --configuration $Configuration --no-build --output $LocalNuGetPath
    
    Write-Info "Packages in local feed:"
    Get-ChildItem -Path $LocalNuGetPath -Filter *.nupkg | ForEach-Object {
        Write-Host "  - $($_.Name)" -ForegroundColor Gray
    }
    
    Write-Success "Pack completed"
}

# Restore samples
function Invoke-RestoreSamples {
    Write-Header "Restoring samples"
    
    $samples = Get-ChildItem -Path $SamplesPath -Filter *.csproj -Recurse
    
    foreach ($sample in $samples) {
        $sampleName = $sample.Directory.Name
        Write-Info "Restoring $sampleName..."
        dotnet restore $sample.FullName
    }
    
    Write-Success "Sample restore completed"
}

# Build samples
function Invoke-BuildSamples {
    Write-Header "Building samples"
    
    $samples = Get-ChildItem -Path $SamplesPath -Filter *.csproj -Recurse
    
    foreach ($sample in $samples) {
        $sampleName = $sample.Directory.Name
        Write-Info "Building $sampleName..."
        dotnet build $sample.FullName --configuration $Configuration
    }
    
    Write-Success "Sample build completed"
}

# Containerize samples
function Invoke-Containerize {
    if ($SkipDocker) {
        Write-Warning "Skipping containerization (SkipDocker flag set)"
        return
    }
    
    Write-Header "Containerizing samples"
    
    # Check Docker availability
    try {
        $dockerVersion = docker --version
        Write-Info "Docker found: $dockerVersion"
    }
    catch {
        Write-ErrorMsg "Docker not found. Please install Docker to containerize samples."
        return
    }
    
    $samples = Get-ChildItem -Path $SamplesPath -Filter *.csproj -Recurse
    
    foreach ($sample in $samples) {
        $sampleName = $sample.Directory.Name
        Write-Info "Publishing and containerizing $sampleName..."
        
        # Publish triggers Docker build when DockerBuildOnPublish=true
        dotnet publish $sample.FullName --configuration $Configuration
        
        # Verify Dockerfile was generated
        $dockerfile = Join-Path $sample.Directory.FullName "Dockerfile"
        if (Test-Path $dockerfile) {
            Write-Success "Dockerfile generated for $sampleName"
        }
        else {
            Write-Warning "Dockerfile not found for $sampleName"
        }
    }
    
    Write-Info "`nListing Docker images:"
    docker images | Select-String -Pattern "sample"
    
    Write-Success "Containerization completed"
}

# Run containerized samples
function Invoke-Run {
    if ($SkipDocker) {
        Write-Warning "Skipping container run (SkipDocker flag set)"
        return
    }
    
    Write-Header "Running containerized samples"
    
    Write-Info "This is a placeholder for integration test execution"
    Write-Info "Container orchestration will be implemented in integration tests"
    
    Write-Success "Run completed"
}

# Main execution
Write-Host @"
╔════════════════════════════════════════════════════════════════╗
║                                                                ║
║            JD.MSBuild.Containers Build Script                 ║
║                                                                ║
╚════════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

Write-Info "Target: $Target"
Write-Info "Configuration: $Configuration"
Write-Info "Skip Tests: $SkipTests"
Write-Info "Skip Docker: $SkipDocker"
Write-Host ""

try {
    switch ($Target) {
        'Clean' {
            Invoke-Clean
        }
        'Build' {
            Invoke-Build
        }
        'Test' {
            Invoke-Build
            Invoke-Test
        }
        'Pack' {
            Invoke-Build
            Invoke-Test
            Invoke-Pack
        }
        'RestoreSamples' {
            Invoke-RestoreSamples
        }
        'BuildSamples' {
            Invoke-RestoreSamples
            Invoke-BuildSamples
        }
        'Containerize' {
            Invoke-RestoreSamples
            Invoke-BuildSamples
            Invoke-Containerize
        }
        'Run' {
            Invoke-Run
        }
        'All' {
            Invoke-Clean
            Invoke-Build
            Invoke-Test
            Invoke-Pack
            Invoke-RestoreSamples
            Invoke-BuildSamples
            Invoke-Containerize
        }
    }
    
    Write-Host "`n"
    Write-Success "Build script completed successfully!"
    exit 0
}
catch {
    Write-Host "`n"
    Write-ErrorMsg "Build script failed: $_"
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    exit 1
}
