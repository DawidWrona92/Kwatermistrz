# Kwatermistrz

Aplikacja desktopowa (Windows, C#, WPF) do katalogowania sprzętu rekonstrukcyjnego i replik ASG oraz śledzenia, jak ten sprzęt jest używany. Zastępuje dwa arkusze w Numbers (lista broni i lista wyposażenia).

Pełna specyfikacja: [`docs/wymagania.md`](docs/wymagania.md). Decyzje technologiczne: [`docs/decyzje/`](docs/decyzje/). Zmiany: [`CHANGELOG.md`](CHANGELOG.md).

## Właściwości

- Działa **offline**, bez kont. Dane lokalne, w otwartym formacie JSON.
- Ogólna — nie zakłada żadnej formacji. Rekonstrukcje, kategorie, sloty zestawów i szablony kart konfiguruje użytkownik.
- Reguły obliczeń są stałe we wszystkich instalacjach (NFR-14), więc wyniki da się porównywać między kolegami z innych jednostek.
- Interfejs po polsku, spokojny i neutralny; próbki kamuflaży jako jedyny element kolorystyczny.

## Uruchomienie (dewelopersko)

Wymagania: .NET 10 SDK, Windows (projekt WPF; `EnableWindowsTargeting` pozwala też kompilować na innych systemach).

```bash
# całość (Core + App + testy)
dotnet build Kwatermistrz.slnx

# testy (Core, bez WPF)
dotnet test tests/Kwatermistrz.Core.Tests/Kwatermistrz.Core.Tests.csproj

# aplikacja (WPF — tylko Windows)
dotnet run --project src/Kwatermistrz.App/Kwatermistrz.App.csproj
```

Dane użytkownika: `%AppData%\Kwatermistrz\dane.json` (+ katalog `zdjecia/`, kopie w `kopie/`). Przy pierwszym uruchomieniu kreator pyta o nazwę pierwszej rekonstrukcji i stronę; dane autora można zaimportować z pliku xlsx przez ekran **Import** (podgląd rekordów i konfliktów przed zatwierdzeniem).

Interfejs: lewa szyna nawigacji, u góry wyszukiwanie (Ctrl+F), przełącznik rekonstrukcji i „Dodaj wyprawę” (Ctrl+N), po prawej panel karty. Motyw jasny/ciemny/systemowy w Ustawieniach.

## Instalacja dla użytkowników (bez admina)

Publikacja zgodnie z [decyzją 06](docs/decyzje/06-instalacja.md): self-contained jako **katalog** w zipie z instrukcją — rozpakować i uruchomić `Kwatermistrz.exe`, bez instalatora i bez uprawnień administratora (NFR-10). Katalog, nie pojedynczy plik: wersja 1.0.0 single-file nie uruchamiała się na części maszyn (od 1.0.1 publikujemy folder). Aktualizacja = podmiana folderu aplikacji; dane w `%AppData%\Kwatermistrz` zostają (NFR-06).

```powershell
# wydanie paczki dist\Kwatermistrz-<wersja>.zip (Windows, .NET 10 SDK)
powershell -File scripts\publikuj-katalog.ps1 -Wersja 1.0.1
```

Paczka nie zawiera danych autora (NFR-15). Instrukcja dla kolegów: [`docs/instalacja/INSTRUKCJA.txt`](docs/instalacja/INSTRUKCJA.txt).

## Struktura repozytorium

```
src/Kwatermistrz.Core/    model domenowy, magazyn JSON, kalkulator BR, importer xlsx
src/Kwatermistrz.App/     aplikacja WPF (interfejs)
tests/Kwatermistrz.Core.Tests/  testy jednostkowe i akceptacyjne
scripts/publikuj-katalog.ps1  wydanie paczki zip — katalog (NFR-10, od 1.0.1)
scripts/publikuj.ps1      wydanie paczki zip — single-file (1.0.0, niezalecane)
docs/wymagania.md        specyfikacja (wersja 0.2)
docs/dane-startowe/      plik źródłowy xlsx do importu
docs/decyzje/            notatki decyzji technologicznych
docs/instalacja/         instrukcja dla użytkowników (trafia do paczki)
```
