# Budowanie Kwatermistrza bez Visual Studio i bez GitHub Actions.
# Wymaga tylko darmowego .NET SDK 10 (Windows x64).
# Uruchom z katalogu repozytorium:
#   powershell -ExecutionPolicy Bypass -File .\scripts\build-windows-local.ps1

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$publish = Join-Path $root 'dist\Kwatermistrz'
$archive = Join-Path $root 'dist\Kwatermistrz-Windows-x64-LOCAL.zip'

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnet) {
    throw "Brak .NET SDK. Zainstaluj .NET SDK 10 (nie Visual Studio): winget install --id Microsoft.DotNet.SDK.10 --exact"
}

$sdks = & dotnet --list-sdks
if (-not ($sdks | Where-Object { $_ -match '^10\.' })) {
    throw "Wymagany jest .NET SDK 10. Polecenie: winget install --id Microsoft.DotNet.SDK.10 --exact"
}

function Sprawdz-Wynik([string]$krok) {
    if ($LASTEXITCODE -ne 0) {
        throw "Niepowodzenie: $krok (exit code $LASTEXITCODE)"
    }
}

Push-Location $root
try {
    Write-Host '1/5 Przywracanie zaleznosci...'
    & dotnet restore Kwatermistrz.slnx
    Sprawdz-Wynik 'restore'

    Write-Host '2/5 Kompilacja Windows Release...'
    & dotnet build Kwatermistrz.slnx --configuration Release --no-restore
    Sprawdz-Wynik 'build'

    Write-Host '3/5 Testy logiki i kompatybilnosci danych...'
    & dotnet test tests/Kwatermistrz.Core.Tests/Kwatermistrz.Core.Tests.csproj --configuration Release --no-build
    Sprawdz-Wynik 'testy Core'

    Write-Host '4/5 Testy uruchomieniowe kontrolek WPF (STA)...'
    & dotnet run --project tests/Kwatermistrz.App.Smoke/Kwatermistrz.App.Smoke.csproj --configuration Release
    Sprawdz-Wynik 'smoke test WPF'

    if (Test-Path -LiteralPath $publish) {
        Remove-Item -LiteralPath $publish -Recurse -Force
    }
    Write-Host '5/5 Publikowanie samodzielnej aplikacji Windows x64...'
    & dotnet publish src/Kwatermistrz.App/Kwatermistrz.App.csproj --configuration Release --runtime win-x64 --self-contained true '-p:PublishSingleFile=false' '-p:PublishTrimmed=false' --output $publish
    Sprawdz-Wynik 'publish'

    if (-not (Test-Path -LiteralPath (Join-Path $publish 'Kwatermistrz.exe'))) {
        throw 'Brak Kwatermistrz.exe po publikacji'
    }

    $instrukcja = @'
Kwatermistrz - kompilacja lokalna, Windows x64
1. Rozpakuj cala zawartosc ZIP do nowego katalogu.
2. Uruchom Kwatermistrz.exe; nie przenos samego EXE.
3. Nie trzeba instalowac .NET Runtime.
UWAGA: przed uruchomieniem zrob kopie calego katalogu %APPDATA%\Kwatermistrz.
To wersja testowa na bazie etapow 1.1-1.3; testy GUI nadal wymagaja odbioru.
'@
    Set-Content -LiteralPath (Join-Path $publish 'PRZECZYTAJ-PRZED-STARTEM.txt') -Value $instrukcja -Encoding UTF8

    if (Test-Path -LiteralPath $archive) {
        Remove-Item -LiteralPath $archive -Force
    }
    Compress-Archive -Path (Join-Path $publish '*') -DestinationPath $archive -CompressionLevel Optimal
    Write-Host ''
    Write-Host "GOTOWE: $archive"
    Write-Host 'Rozpakuj archiwum do osobnego folderu, nie mieszaj z poprzednim buildem.'
}
finally {
    Pop-Location
}
