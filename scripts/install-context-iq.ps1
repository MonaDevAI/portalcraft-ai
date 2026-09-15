param(
    [Parameter(Mandatory = $true)]
    [string]$TargetRepository,

    [string]$Branch = 'main',
    [int]$SinceDays = 30,
    [string]$ProductName = 'Service Workspace',
    [string]$AssistantName = 'Workspace Assistant',
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$tool = Join-Path $root 'tools\ContextIq.RepoTool'
$repository = (& git -C $TargetRepository rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0) {
    throw "TargetRepository is not a Git repository: $TargetRepository"
}

$output = Join-Path $repository '.context-iq'
$generateArguments = @(
    'run',
    '--project', $tool,
    '--',
    'generate', $repository,
    '--output', $output
)
if ($Force) {
    $generateArguments += '--force'
}

& dotnet @generateArguments
if ($LASTEXITCODE -ne 0) {
    throw 'Context IQ repository generation failed.'
}

$scenarioOutput = Join-Path $output 'pr-scenarios'
$scenarioArguments = @(
    'run',
    '--project', $tool,
    '--',
    'pr-scenarios', $repository,
    '--branch', $Branch,
    '--since-days', $SinceDays,
    '--output', $scenarioOutput
)
if ($Force) {
    $scenarioArguments += '--force'
}

& dotnet @scenarioArguments
if ($LASTEXITCODE -ne 0) {
    throw 'Context IQ PR scenario generation failed.'
}

$environmentFile = Join-Path $output 'client\.env.context-iq'
@"
VITE_PRODUCT_NAME=$ProductName
VITE_ASSISTANT_NAME=$AssistantName
"@ | Set-Content -Path $environmentFile -Encoding utf8

Write-Host "Context IQ onboarding generated at $output"
Write-Host 'Review the manifest, generated source, authorization boundaries, and PR scenarios before integration.'
Write-Host "Preview the dashboard with:"
Write-Host "dotnet run --project `"$tool`" -- serve `"$output`""
