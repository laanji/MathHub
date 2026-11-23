$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$localRoot=Join-Path $projectRoot '.local'
$recordPath=Join-Path $localRoot 'server.json'
if(Test-Path $recordPath) {
    $record=Get-Content -Raw -LiteralPath $recordPath | ConvertFrom-Json
    $server=Get-Process -Id $record.ProcessId -ErrorAction SilentlyContinue
    if($server -and $server.StartTime.ToUniversalTime().Ticks -eq $record.StartTicks) {
        $command=Get-CimInstance Win32_Process -Filter "ProcessId=$($server.Id)"
        $expectedDll=Join-Path $projectRoot 'src/MathHub.Api/bin/Debug/net8.0/MathHub.Api.dll'
        if(!$command.CommandLine.Contains($expectedDll)){throw 'Unexpected process command line; refusing to stop.'}
        Stop-Process -Id $server.Id
    }
    Remove-Item -LiteralPath $recordPath
}
$pgCtl=Join-Path $localRoot 'postgresql/pgsql/bin/pg_ctl.exe'
if(Test-Path $pgCtl) {
    & $pgCtl -D (Join-Path $localRoot 'data') status *> $null
    if($LASTEXITCODE -eq 0){& $pgCtl -D (Join-Path $localRoot 'data') -m fast -w stop;if($LASTEXITCODE -ne 0){throw 'PostgreSQL stop failed.'}}
}
Write-Host 'MathHub stopped. Saved data has been preserved.'
