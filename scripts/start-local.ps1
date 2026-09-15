$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

$api = Start-Process dotnet -ArgumentList @(
    'run',
    '--project',
    (Join-Path $root 'server\ContextIq.Api')
) -PassThru

$client = Start-Process npm -WorkingDirectory (Join-Path $root 'client') -ArgumentList @(
    'run',
    'dev'
) -PassThru

Write-Host "Context IQ API PID: $($api.Id) - http://localhost:5080"
Write-Host "Context IQ client PID: $($client.Id) - http://localhost:5173"
Write-Host 'Stop the two reported process IDs when finished.'
