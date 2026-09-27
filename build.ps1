$dotnet = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
$log = Join-Path $PSScriptRoot 'build.log'
Set-Location $PSScriptRoot
& $dotnet --version | Out-File $log -Encoding utf8
& $dotnet build DeskBox.sln -c Release 2>&1 | Out-File $log -Append -Encoding utf8
if ($LASTEXITCODE -eq 0) { 'BUILD_OK' | Out-File $log -Append } else { 'BUILD_FAIL' | Out-File $log -Append }
exit $LASTEXITCODE
