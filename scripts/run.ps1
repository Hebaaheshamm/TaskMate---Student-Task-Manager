$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
dotnet run --project (Join-Path $root "backend/TaskMate.Api/TaskMate.Api.csproj") --launch-profile http
