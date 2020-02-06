
# Different from the standard build.ps1 for .NET projects because we must use msbuild instead of dotnet build

$ErrorActionPreference = "Stop"

$scriptDir = split-path -parent $MyInvocation.MyCommand.Definition
Import-Module "$scriptDir/circleci/template/helpers.psm1" -Force

ExecuteOrFail { msbuild /restore /p:TargetFramework=netstandard2.0 /p:Configuration=Debug src/LaunchDarkly.XamarinSdk/LaunchDarkly.XamarinSdk.csproj }
