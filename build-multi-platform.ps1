#!/usr/bin/env pwsh

# Multi-Platform Build Script for MultiNode Network
# This script builds the solution for Windows and Linux (Raspberry Pi) platforms

param(
    [string]$Configuration = "Release",
    [switch]$Clean,
    [switch]$Publish
)

Write-Host "=== MultiNode Network Multi-Platform Build Script ===" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow

# Set error action preference
$ErrorActionPreference = "Stop"

# Function to build for a specific runtime
function Build-ForRuntime {
    param(
        [string]$Runtime,
        [string]$Framework,
        [string]$ProjectPath
    )
    
    Write-Host "Building $ProjectPath for $Runtime ($Framework)..." -ForegroundColor Cyan
    
    $buildArgs = @(
        "build",
        $ProjectPath,
        "--configuration", $Configuration,
        "--framework", $Framework,
        "--runtime", $Runtime,
        "--verbosity", "minimal"
    )
    
    if ($Clean) {
        $buildArgs += "--no-restore"
    }
    
    $result = & dotnet $buildArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed for $Runtime" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Build completed for $Runtime" -ForegroundColor Green
}

# Function to publish for a specific runtime
function Publish-ForRuntime {
    param(
        [string]$Runtime,
        [string]$Framework,
        [string]$ProjectPath
    )
    
    Write-Host "Publishing $ProjectPath for $Runtime ($Framework)..." -ForegroundColor Cyan
    
    $outputPath = "publish/$Runtime"
    
    $publishArgs = @(
        "publish",
        $ProjectPath,
        "--configuration", $Configuration,
        "--framework", $Framework,
        "--runtime", $Runtime,
        "--output", $outputPath,
        "--self-contained",
        "--verbosity", "minimal"
    )
    
    if ($Clean) {
        $publishArgs += "--no-restore"
    }
    
    $result = & dotnet $publishArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Publish failed for $Runtime" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Publish completed for $Runtime at $outputPath" -ForegroundColor Green
}

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning solution..." -ForegroundColor Yellow
    & dotnet clean --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Clean failed" -ForegroundColor Red
        exit 1
    }
}

# Restore packages
Write-Host "Restoring packages..." -ForegroundColor Yellow
& dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Restore failed" -ForegroundColor Red
    exit 1
}

# Define platforms to build for
$platforms = @(
    @{ Runtime = "win-x64"; Framework = "net9.0-windows" },
    @{ Runtime = "linux-x64"; Framework = "net9.0" },
    @{ Runtime = "linux-arm64"; Framework = "net9.0" }
)

# Build projects
$projects = @(
    "src/MultiNodeNetwork.Master/MultiNodeNetwork.Master.csproj",
    "src/MultiNodeNetwork.Repeater/MultiNodeNetwork.Repeater.csproj"
)

foreach ($project in $projects) {
    foreach ($platform in $platforms) {
        if ($Publish) {
            Publish-ForRuntime -Runtime $platform.Runtime -Framework $platform.Framework -ProjectPath $project
        } else {
            Build-ForRuntime -Runtime $platform.Runtime -Framework $platform.Framework -ProjectPath $project
        }
    }
}

# Build library projects (no runtime-specific builds needed)
Write-Host "Building library projects..." -ForegroundColor Yellow
& dotnet build "src/MultiNodeNetwork.Core/MultiNodeNetwork.Core.csproj" --configuration $Configuration
& dotnet build "src/MultiNodeNetwork.Common/MultiNodeNetwork.Common.csproj" --configuration $Configuration
& dotnet build "tests/MultiNodeNetwork.Tests/MultiNodeNetwork.Tests.csproj" --configuration $Configuration

Write-Host "=== Multi-Platform Build Completed Successfully ===" -ForegroundColor Green

if ($Publish) {
    Write-Host "Published artifacts available in the 'publish' directory:" -ForegroundColor Yellow
    Get-ChildItem "publish" -Directory | ForEach-Object {
        Write-Host "  - $($_.Name)" -ForegroundColor Cyan
    }
} 