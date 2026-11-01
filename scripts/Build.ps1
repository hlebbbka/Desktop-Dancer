param([switch]$FrameworkDependent)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$packageRoot = Join-Path $repoRoot 'dist\DesktopDancer-1.0'
$appRoot = Join-Path $packageRoot 'app'
New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
$selfContained = if ($FrameworkDependent) { 'false' } else { 'true' }
dotnet publish (Join-Path $repoRoot 'MusicDancer\MusicDancer.csproj') -c Release -r win-x64 --self-contained $selfContained -o $appRoot
if ($LASTEXITCODE -ne 0) { throw 'Publish failed / Ошибка сборки' }
Copy-Item -LiteralPath (Join-Path $repoRoot 'MusicDancer\IMG for Desktop Dancer') -Destination $packageRoot -Recurse -Force
Get-ChildItem (Join-Path $repoRoot 'packaging') -File | Copy-Item -Destination $packageRoot -Force
Copy-Item -LiteralPath (Join-Path $repoRoot 'packaging\licenses') -Destination $packageRoot -Recurse -Force
foreach ($name in @('README.md','README.ru.md','THIRD_PARTY_NOTICES.md')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot $name) -Destination $packageRoot -Force
}
Copy-Item -LiteralPath (Join-Path $repoRoot 'MusicDancer\Vendor\WebView2\LICENSE.txt') -Destination (Join-Path $packageRoot 'WebView2-LICENSE.txt') -Force
Copy-Item -LiteralPath (Join-Path $repoRoot 'MusicDancer\Vendor\WebView2\NOTICE.txt') -Destination (Join-Path $packageRoot 'WebView2-NOTICE.txt') -Force
if (Test-Path (Join-Path $repoRoot 'docs')) { Copy-Item -LiteralPath (Join-Path $repoRoot 'docs') -Destination $packageRoot -Recurse -Force }
if (Get-ChildItem $packageRoot -Recurse -File -Filter '*antoha*') { throw 'Excluded image found in release' }
$archive = Join-Path $repoRoot 'dist\DesktopDancer-1.0-win-x64.zip'
Compress-Archive -LiteralPath $packageRoot -DestinationPath $archive -Force
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $repoRoot 'dist\SHA256SUMS.txt'), "$hash  DesktopDancer-1.0-win-x64.zip`n", [Text.UTF8Encoding]::new($false))
Write-Output $archive
