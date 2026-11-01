$ErrorActionPreference = 'Stop'
$appPath = Join-Path $PSScriptRoot 'app\MusicDancer.exe'
$gpuKey = 'HKCU:\Software\Microsoft\DirectX\UserGpuPreferences'
if (!(Test-Path -LiteralPath $gpuKey)) { New-Item -Path $gpuKey -Force | Out-Null }
New-ItemProperty -LiteralPath $gpuKey -Name $appPath -Value 'GpuPreference=2;' -PropertyType String -Force | Out-Null
Start-Process -FilePath $appPath
