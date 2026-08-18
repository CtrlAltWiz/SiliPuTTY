param(
    [string]$Runtime = "win-x64",
    [string]$CertificateThumbprint = ""
)
$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$publishPath = Join-Path $projectRoot "artifacts\SillyPutty-$Runtime"
$zipPath = Join-Path $projectRoot "artifacts\SillyPutty-0.2.0-alpha-$Runtime.zip"
dotnet publish (Join-Path $projectRoot "SillyPutty.csproj") -c Release -r $Runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $publishPath
$exePath = Join-Path $publishPath "SillyPutty.exe"
if ($CertificateThumbprint) {
    $signTool = Get-Command signtool.exe -ErrorAction Stop
    & $signTool.Source sign /sha1 $CertificateThumbprint /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 $exePath
}
Compress-Archive -Path (Join-Path $publishPath "*") -DestinationPath $zipPath -Force
Write-Output "Package created: $zipPath"
if (-not $CertificateThumbprint) { Write-Warning "Package is unsigned. Supply a trusted code-signing certificate thumbprint for public releases." }
