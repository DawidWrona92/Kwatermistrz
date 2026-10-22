# Publikacja Kwatermistrza (NFR-10, decyzja 06): self-contained, pojedynczy plik exe, zip z instrukcją.
# Wymaga: Windows + .NET 10 SDK. Uruchomienie:  powershell -File scripts\publikuj.ps1 [-Wersja 1.0.0]
# Wynik:  dist\Kwatermistrz-<wersja>.zip  (gotowy do rozdania kolegom — bez danych autora, NFR-15)
param(
    [string]$Wersja = "1.0.0"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$publikacja = Join-Path $root "dist\publish"
$zip = Join-Path $root "dist\Kwatermistrz-$Wersja.zip"
$staging = Join-Path $root "dist\staging"

Write-Host "== Kwatermistrz $Wersja — publikacja self-contained single-file ==" -ForegroundColor Cyan

# wersja trafia do metadanych pliku exe (widoczna w oknie właściwości i w O programie)
& dotnet publish "$root\src\Kwatermistrz.App\Kwatermistrz.App.csproj" `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=embedded -p:DebugSymbols=false `
    -p:Version=$Wersja -p:AssemblyVersion=$Wersja -p:FileVersion=$Wersja `
    -o $publikacja
if ($LASTEXITCODE -ne 0) { throw "dotnet publish nieudane" }
# w paczce single-file zostaje tylko exe — pliki .pdb zależności (np. libSkiaSharp.pdb z NPOI) są zbędne
Get-ChildItem $publikacja -Filter *.pdb -ErrorAction SilentlyContinue | Remove-Item -Force

# pakowanie: exe + instrukcja + plik README (bez danych — NFR-15)
Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $staging | Out-Null
Copy-Item "$publikacja\Kwatermistrz.exe" $staging
Copy-Item "$root\docs\instalacja\INSTRUKCJA.txt" $staging

Remove-Item $zip -Force -ErrorAction SilentlyContinue
Compress-Archive -Path "$staging\*" -DestinationPath $zip

$rozmiar = [math]::Round((Get-Item $zip).Length / 1MB, 1)
Write-Host "== Gotowe: $zip ($rozmiar MB) ==" -ForegroundColor Green
Write-Host "Rozdaj kolegom zip. Dane każdy ma u siebie w %AppData%\Kwatermistrz — aktualizacja to podmiana exe."
