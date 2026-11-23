param([switch]$NoBrowser)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$localRoot=Join-Path $projectRoot '.local'
function Wait-Api {
    for($attempt=0;$attempt -lt 60;$attempt++) {
        try { $health=Invoke-RestMethod 'http://127.0.0.1:5080/health' -TimeoutSec 2; if($health.database -eq 'PostgreSQL'){return} } catch {}
        Start-Sleep -Milliseconds 300
    }
    throw 'MathHub API did not become ready. Check .local/api.error.log and .local/api.log.'
}
$recordPath=Join-Path $localRoot 'server.json'
if(Test-Path $recordPath) {
    $record=Get-Content -Raw -LiteralPath $recordPath | ConvertFrom-Json
    $existing=Get-Process -Id $record.ProcessId -ErrorAction SilentlyContinue
    if($existing -and $existing.StartTime.ToUniversalTime().Ticks -eq $record.StartTicks) {
        Wait-Api
        if(!$NoBrowser){Start-Process 'http://127.0.0.1:5080'}
        Write-Host 'MathHub is already running: http://127.0.0.1:5080'
        return
    }
}
& (Join-Path $PSScriptRoot 'Initialize-Local.ps1')
Push-Location $projectRoot
try {
    Write-Host 'Building MathHub...'
    & dotnet build MathHub.sln --configuration Debug --nologo -v quiet -m:1
    if($LASTEXITCODE -ne 0){throw 'Backend build failed.'}
    Push-Location (Join-Path $projectRoot 'src/MathHub.Web')
    try {
        $npm=(Get-Command npm.cmd -ErrorAction Stop).Source
        & $npm ci --no-fund --no-audit
        if($LASTEXITCODE -ne 0){throw 'npm ci failed.'}
        & $npm run build
        if($LASTEXITCODE -ne 0){throw 'Frontend build failed.'}
    } finally {Pop-Location}
} finally {Pop-Location}
$apiDll=Join-Path $projectRoot 'src/MathHub.Api/bin/Debug/net8.0/MathHub.Api.dll'
$contentRoot=Join-Path $projectRoot 'src/MathHub.Api'
$previousEnvironment=$env:ASPNETCORE_ENVIRONMENT
try {
    $env:ASPNETCORE_ENVIRONMENT='Development'
    $server=Start-Process -FilePath (Get-Command dotnet).Source -ArgumentList @("`"$apiDll`"",'--urls','http://127.0.0.1:5080','--contentRoot',"`"$contentRoot`"") -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $localRoot 'api.log') -RedirectStandardError (Join-Path $localRoot 'api.error.log')
    @{ProcessId=$server.Id;StartTicks=$server.StartTime.ToUniversalTime().Ticks} | ConvertTo-Json | Set-Content -LiteralPath $recordPath -Encoding utf8
} finally {$env:ASPNETCORE_ENVIRONMENT=$previousEnvironment}
Wait-Api
if(!$NoBrowser){Start-Process 'http://127.0.0.1:5080'}
Write-Host 'MathHub is ready: http://127.0.0.1:5080'
Write-Host 'To stop: run Stop-MathHub.cmd. Accounts and results stay in .local/data.'
