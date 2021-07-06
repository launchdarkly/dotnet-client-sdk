
# Different from the standard build.ps1 for .NET projects because we must use msbuild
# instead of dotnet build. We're only doing this build in order to support building
# documentation, so we only use the .NET Standard 2.0 target and don't need Xamarin.

$ErrorActionPreference = "Stop"

$scriptDir = split-path -parent $MyInvocation.MyCommand.Definition
Import-Module "$scriptDir/circleci/template/helpers.psm1" -Force

ExecuteOrFail { msbuild /restore /p:TargetFramework=netstandard2.0 /p:Configuration=Debug src/LaunchDarkly.ClientSdk/LaunchDarkly.ClientSdk.csproj }
