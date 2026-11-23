param([string]$BinariesPath = '')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$localRoot = Join-Path $projectRoot '.local'
New-Item -ItemType Directory -Force $localRoot | Out-Null
$pgRoot = Join-Path $localRoot 'postgresql/pgsql'
if (!(Test-Path (Join-Path $pgRoot 'bin/pg_ctl.exe'))) {
    if ($BinariesPath) {
        New-Item -ItemType Directory -Force $pgRoot | Out-Null
        foreach ($part in @('bin','lib','share')) { Copy-Item -LiteralPath (Join-Path $BinariesPath $part) -Destination $pgRoot -Recurse }
    } else {
        Write-Host 'Downloading portable PostgreSQL 16.16 (first launch only)...'
        $archive = Join-Path $localRoot 'postgresql16.zip'
        Invoke-WebRequest 'https://sbp.enterprisedb.com/getfile.jsp?fileid=1260731' -OutFile $archive
        if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne 'ADD1DE261B150E6923266ABEE293F05050795E73F5356158AEE4221A2170C073') { throw 'PostgreSQL archive checksum mismatch. Download was not extracted.' }
        $extractRoot = Join-Path $localRoot 'postgresql'
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $zip = [IO.Compression.ZipFile]::OpenRead($archive)
        try {
            foreach ($entry in $zip.Entries) {
                if ($entry.FullName -notmatch '^pgsql/(bin|lib|share)/' -or !$entry.Name) { continue }
                $target = [IO.Path]::GetFullPath((Join-Path $extractRoot $entry.FullName))
                if (!$target.StartsWith([IO.Path]::GetFullPath($extractRoot) + [IO.Path]::DirectorySeparatorChar)) { throw 'Invalid archive entry' }
                New-Item -ItemType Directory -Force (Split-Path $target -Parent) | Out-Null
                [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,$target,$true)
            }
        } finally { $zip.Dispose() }
        Remove-Item -LiteralPath $archive
    }
}
function Invoke-Pg([string]$Name,[string[]]$Arguments) {
    & (Join-Path $pgRoot "bin/$Name.exe") @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Name failed ($LASTEXITCODE)" }
}
function New-Secret { $bytes=New-Object byte[] 32; $rng=[Security.Cryptography.RandomNumberGenerator]::Create();try{$rng.GetBytes($bytes)}finally{$rng.Dispose()};return ([BitConverter]::ToString($bytes)).Replace('-','').ToLowerInvariant() }
$configPath = Join-Path $localRoot 'appsettings.local.json'
if (!(Test-Path $configPath)) {
    $dbPassword = New-Secret
    $bootstrapPassword = New-Secret
    $config = @{
        ConnectionStrings=@{ MathHub="Host=127.0.0.1;Port=55432;Database=mathhub;Username=mathhub;Password=$dbPassword" }
        Jwt=@{ Key=(New-Secret); Issuer='MathHub'; Audience='MathHub.Web' }
        Development=@{ AdminEmail='admin@mathhub.local'; AdminPassword=(New-Secret); BootstrapPassword=$bootstrapPassword; DatabasePassword=$dbPassword }
    }
    $config | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $configPath -Encoding utf8
}
$config = Get-Content -Raw -LiteralPath $configPath | ConvertFrom-Json
$dataRoot = Join-Path $localRoot 'data'
if (!(Test-Path (Join-Path $dataRoot 'PG_VERSION'))) {
    $pwFile = Join-Path $localRoot 'bootstrap-password.tmp'
    try {
        [IO.File]::WriteAllText($pwFile,$config.Development.BootstrapPassword)
        Invoke-Pg 'initdb' @('-D',$dataRoot,'-U','postgres',"--pwfile=$pwFile",'--auth=scram-sha-256','--encoding=UTF8','--locale=C')
    } finally { if(Test-Path $pwFile){Remove-Item -LiteralPath $pwFile} }
}
& (Join-Path $pgRoot 'bin/pg_ctl.exe') -D $dataRoot status *> $null
if ($LASTEXITCODE -ne 0) {
    Invoke-Pg 'pg_ctl' @('-D',$dataRoot,'-l',(Join-Path $localRoot 'postgres.log'),'-o','-h 127.0.0.1 -p 55432','-w','start')
}
$previousPgPassword = $env:PGPASSWORD
try {
    $env:PGPASSWORD=$config.Development.BootstrapPassword
    $roleExists = & (Join-Path $pgRoot 'bin/psql.exe') -h 127.0.0.1 -p 55432 -U postgres -d postgres -tAc "SELECT 1 FROM pg_roles WHERE rolname='mathhub'"
    if ($LASTEXITCODE -ne 0) { throw 'Cannot connect to the private PostgreSQL cluster.' }
    if ($roleExists -ne '1') { Invoke-Pg 'psql' @('-h','127.0.0.1','-p','55432','-U','postgres','-d','postgres','-v','ON_ERROR_STOP=1','-c',"CREATE ROLE mathhub LOGIN PASSWORD '$($config.Development.DatabasePassword)'") }
    $dbExists = & (Join-Path $pgRoot 'bin/psql.exe') -h 127.0.0.1 -p 55432 -U postgres -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='mathhub'"
    if ($dbExists -ne '1') { Invoke-Pg 'createdb' @('-h','127.0.0.1','-p','55432','-U','postgres','-O','mathhub','mathhub') }
} finally { $env:PGPASSWORD=$previousPgPassword }
Write-Host 'MathHub PostgreSQL 16 is ready on 127.0.0.1:55432. Local credentials: .local/appsettings.local.json'
