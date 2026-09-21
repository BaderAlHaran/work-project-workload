# Rebuilds "build\أوراق العمل.exe" using the C# compiler that ships with Windows (.NET Framework 4.x).
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$csc  = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$out  = Join-Path $root 'build\أوراق العمل.exe'
New-Item -ItemType Directory -Force (Join-Path $root 'build') | Out-Null
& $csc /nologo /target:winexe /optimize+ /codepage:65001 "/win32icon:$root\src\app.ico" `
    /r:Microsoft.VisualBasic.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll `
    "/out:$out" "$root\src\*.cs"
if ($LASTEXITCODE -ne 0) { throw "Build failed" }
Write-Host "Built $out"
