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
powershell -File scripts\publikuj-katalog.ps1
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

## Diagnostyka uruchomienia i walidacja Windows

Jeśli aplikacja się nie uruchamia, sprawdź `%AppData%\Kwatermistrz\error.log`
(znajdują się tam pełne wyjątki, także z parsowania `App.xaml`).
**Nie usuwaj `%AppData%\Kwatermistrz`** — zawiera dane, zdjęcia i kopie.

Przed wypuszczeniem nowej wersji uruchom CI `Windows build, tests and XAML smoke test`.
CI kompiluje .NET 10, uruchamia testy Core, sprawdza pokazanie okna,
wszystkie widoki i nawigację WPF w trybie `--smoke-test`,
a następnie tworzy przenośny ZIP jako artefakt GitHub Actions.
Test automatyczny **nie zastępuje ręcznego sprawdzenia** na docelowym Windows
z rzeczywistymi danymi.

Skrypt `scripts/publikuj-katalog.ps1` czyści katalog `dist/publish`
i pobiera numer wersji automatycznie z `Kwatermistrz.App.csproj`.

## Zdjęcia zestawów i referencje

W zakładce **Zestawy** wybierz zestaw. Na karcie są dwie osobne galerie:

- **Moje zdjęcia w stroju** — Twoje zdjęcia sylwetki, bez limitu liczby plików. Wykorzystuje istniejące pole `Zestaw.Zdjecia`, więc stare zdjęcia są zachowywane.
- **Zdjęcia referencyjne** — do 10 zdjęć wzorcowych dla danego zestawu; licznik pokazuje postęp od 0 do 10.

Kliknij **+**, aby wybrać kilka plików naraz, albo przeciągnij pliki z Eksploratora do galerii.
Obsługiwane formaty: JPG, JPEG, PNG, BMP. Kliknięcie miniatury otwiera powiększenie.
Prawym przyciskiem myszy na miniaturze można ustawić zdjęcie główne,
zmienić opcjonalny opis (maks. 500 znaków) lub usunąć zdjęcie z galerii.

**Bezpieczeństwo:** usuwanie zdjęcia z galerii wymaga potwierdzenia i nie
usuwa od razu fizycznego pliku z katalogu `%APPDATA%\\Kwatermistrz\\zdjecia`.
To chroni inne zestawy i istniejące kopie danych. Dopiero osobna, świadoma
operacja porządkowania osieroconych plików mogłaby je trwale usuwać.
Zdjęcia są kopiowane do katalogu aplikacji, zmniejszane według ustawień,
a referencje i opisy zapisywane w `dane.json`.

Eksport rekonstrukcji do `.kwreko` uwzględnia obie galerie wraz z opisami.
Przy imporcie kolizja z istniejącym plikiem o tej samej nazwie nie nadpisuje zdjęcia:
różna zawartość otrzymuje nową nazwę i aktualizowane są ścieżki importowanego zestawu.

## Układ ekranu Zestawy

Lista zestawów i edytor mają elastyczne szerokości. Przeciągnij pionowy
rozdzielacz pomiędzy nimi w lewo lub prawo, aby ustawić większą szerokość listy
lub karty (np. podczas pracy ze zdjęciami referencyjnymi). Po maksymalizacji
okna obie części wykorzystują dodatkowe miejsce. Lista zestawów ma własny
pionowy i poziomy pasek przewijania; nie trzeba powiększać całego okna,
aby zobaczyć pozostałe kolumny.

Po wejściu do ekranu zaznaczany jest pierwszy widoczny zestaw, a wybrany
rekord pozostaje zaznaczony po odświeżeniu lub edycji. Gdy filtr nie zwraca
wyników, wyświetlany jest komunikat wskazujący wybór lub utworzenie zestawu.

## Automatyczne dopasowanie szerokości kolumn

W tabelach Raportów, Broni, Wyposażenia, Wypraw, Zestawów, Słowników,
Pakietów i Do uzupełnienia kolumny z danymi dzielą wolną szerokość okna
proporcjonalnie. Szersze pola tekstowe (nazwa, opis, komentarz) otrzymują
większy udział niż liczniki, procenty czy daty. Dla wąskich okien kolumny
mają minimalne szerokości, a nadmiar można przewijać poziomo.

Możesz nadal przeciągnąć granicę nagłówka, żeby ręcznie zmienić szerokość
kolumny, lub kliknąć tę granicę dwukrotnie, żeby dopasować ją do treści.
Przy pierwszym otwarciu i przy zmianie szerokości okna kolumny zachowują
proporcjonalny układ. Kolumny akcji i ikon nie zajmują niepotrzebnie miejsca.

## Wyświetlanie i dopasowanie kolumn tabel

Tabele automatycznie wypełniają dostępną szerokość już po otwarciu ekranu, bez konieczności przeciągania czy maksymalizowania okna. Kolumny z danymi dzielą dodatkowe miejsce według wag, ale zachowują minimalne szerokości. Jeśli okno jest zbyt wąskie, pojawia się przewijanie poziome. Po ręcznej zmianie szerokości kolumny aplikacja zachowuje wybraną przez użytkownika szerokość podczas dalszego zmieniania rozmiaru okna.

W Wyposażeniu checkboxy do edycji zbiorczej mieszczą się w kolumnie wyboru. Kolumna „Miejsce” prezentuje nazwę ze słownika zamiast wewnętrznego identyfikatora.
