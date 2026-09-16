$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

$api = Start-Process dotnet -ArgumentList @(
    'run',
    '--project',
    (Join-Path $root 'server\PortalCraft.Api')
) -PassThru

$client = Start-Process npm -WorkingDirectory (Join-Path $root 'client') -ArgumentList @(
    'run',
    'dev'
) -PassThru

Write-Host "PortalCraft AI API PID: $($api.Id) - http://localhost:5080"
Write-Host "PortalCraft AI client PID: $($client.Id) - http://localhost:5173"
Write-Host 'Stop the two reported process IDs when finished.'
