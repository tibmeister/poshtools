param($VSIXPath)

$ToolPath = (Join-Path $PSScriptRoot '..\packages\Microsoft.VSSDK.Vsixsigntool.14.1.24720\tools\vssdk\vsixsigntool.exe')

Set-Location $PSScriptRoot
Start-Process $ToolPath -ArgumentList "sign /f DigiCertNov2016.pfx /sha1 a16105fe72a347d6e5c3bd84e8285fd7c9457ef7 /p $Env:PoshTools_VSIX_CertPassword /t http://timestamp.digicert.com $VSIXPath" -NoNewWindow 
