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

## Struktura repozytorium

```
src/Kwatermistrz.Core/    model domenowy, magazyn JSON, kalkulator BR, importer xlsx
src/Kwatermistrz.App/     aplikacja WPF (interfejs)
tests/Kwatermistrz.Core.Tests/  testy jednostkowe i akceptacyjne
docs/wymagania.md        specyfikacja (wersja 0.2)
docs/dane-startowe/      plik źródłowy xlsx do importu
docs/decyzje/            notatki decyzji technologicznych
```
