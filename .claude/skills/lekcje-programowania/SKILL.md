---
name: lekcje-programowania
description: Ogólne lekcje z programowania aplikacji desktopowych (C#/.NET/WPF, ale wzorce działają też gdzie indziej) — migracje danych, edycja plików skryptami, ostrzeżenia jak błędy, import CSV/XLSX, odświeżanie UI, bezpieczeństwo ścieżek, publikacja, testy przed wydaniem. Używaj przy każdej większej zmianie w projekcie.
---

# Lekcje z programowania — wzorce i pułapki

Zebrane przy pracy nad aplikacjami desktopowymi i naprawianiu błędów z przeglądów kodu. Wszystko jest ogólne — konkretne nazwy klas i funkcji zastąpiono wzorcami (`obiekt_1`, `funkcja_1`, nazwy bez spacji). Czytaj przed większą zmianą; każda sekcja kończy się regułą „nie rób tak".

## 1. Migracje danych w formacie JSON — pre-pass na węźle, nigdy właściwości „cienia"

**Pułapka:** dodanie do modelu właściwości tylko do odczytu starych danych, z atrybutem nazwy kolidującym z istniejącym kluczem — serializator zapisuje wtedy oba klucze lub gubi dane.

**Wzorzec:** migrację rób jako pre-pass na drzewie węzłów (`JsonNode`/`JsonObject` w .NET), **przed** deserializacją do modelu:

```csharp
var wezel = JsonNode.Parse(json) as JsonObject;
funkcja_migracji_przepisuje_stare_wartosci(wezel); // np. int -> identyfikator
var obiekt_1 = wezel.Deserialize<TypModelu>(opcje_serializacji);
```

- Wszystkie ścieżki odczytu idą przez jedną funkcję z migracją.
- Po migracji test roundtrip: zapis → odczyt → dane równe; zapis nie zawiera starego klucza.
- **Nie rób tak:** nie doklejaj właściwości do modelu tylko po to, żeby odczytać stare dane.

## 2. Edycja plików kodu przez skrypty (Python/bash) — literały `\n`

**Pułapka:** w skrypcie zamieniającym pojawia się znak nowej linii w środku literału znakowego/stringu w kodzie docelowym (`'\n'` zapisane jako dosłowny znak końca linii) — plik przestaje się kompilować. To samo dotyczy `\r`, `\t`, `\\`.

**Reguła:** w skrypcie wstawiaj sekwencje podwójnie escapowane (`\\n`), żeby w pliku docelowym zostało `\n`. Po każdej masowej edycji skryptem od razu kompiluj projekt.

## 3. Ostrzeżenia traktowane jak błędy — reguły analizatorów kompilują się na czerwono

Gdy projekt ma włączone `TreatWarningsAsErrors` + analizatory, drobne reguły są błędami kompilacji. Typowe w .NET:

- deklarowanie parametru/zmiennej jako interfejs (`IReadOnlyDictionary`, `IEnumerable`), gdy wystarczy typ konkretny (`Dictionary`, `List`) — używaj najwęższego wystarczającego typu;
- metody rozszerzeń stringów: wersja na `char` vs na `string` — wybieraj zgodnie z literałem (`"?"` vs `'?'`);
- po dopisaniu pola do modelu (szczególnie nullable, np. `string?`) — wszystkie konstruktory i użycia muszą być zgodne z typem.

**Reguła:** po każdej zmianie modelu uruchamiaj pełną kompilację rozwiązania — nie tylko testy.

## 4. Import z arkuszy kalkulacyjnych / CSV — cztery klasyczne błędy

- **CSV to nie „linia = wiersz".** Pola w cudzysłowach mogą zawierać znaki nowej linii — czytaj całą zawartość i parsuj zgodnie z RFC 4180 (stan: w cudzysłowie / poza; `""` to escapowany cudzysłów wewnątrz pola).
- **Kodowanie.** Pliki CSV z Excela lokalizowane na PL bywają w Windows-1250, nie UTF-8. Wykrywaj BOM; bez BOM zakładaj kodowanie lokalne. Błędne kodowanie zamienia polskie znaki na „�".
- **Formuły arkusza (CSV injection).** Wartość zaczynająca się od `=`, `+`, `-`, `@` (lub znak tabulacji), otwarta w Excelu, wykona się jako formuła. Przy eksporcie CSV poprzedzaj takie wartości apostrofem `'` — w każdym eksporterze.
- **Liczby zmiennoprzecinkowe.** Suma wartości pochodzących z doubli arkusza (`110.49000000000001`) nigdy nie będzie równa wartości `decimal` — porównuj z tolerancją (`Math.Abs(a - b) > 0.005m`), nigdy przez `!=`.

## 5. Dopasowanie nazw — po granicach słów, nie przez podłańcuch

`Contains` łapie podłańcuchy: „pas" trafia w „pasoszelki", „m4" w „m43". Do heurystyk dopasowania używaj granic słów (granica = znak niebędący literą ani cyfrą). To samo przy usuwaniu zbędnych słów: usuwaj całe słowo, nie jego fragment wewnątrz innego słowa.

## 6. UI: przypisanie tej samej instancji listy nie odświeża kontrolki

Gdy `ItemsSource` wskazuje na `List<T>`, a zmienisz zawartość tej samej listy i zgłosisz zmianę właściwości — kontrolka się nie odświeży. Wzorce:

- kolekcje ekranu: kolekcja obserwowalna (`ObservableCollection<T>`) + `Clear()`/`Add()`;
- listy przeliczane od zera: zwracaj nową instancję (`.ToList()`) + zgłoszenie zmiany;
- przy masowej zmianie filtrów, gdzie każda flaga osobno przebudowuje dane: para funkcji `zawies_odswiezanie()` / `wznow_odswiezanie()` — jedno odświeżenie na końcu.

## 7. Bezpieczeństwo ścieżek — waliduj wszystko, co przyszło z zewnątrz

Dane mogą przyjść z importu (archiwum zip, plik konfiguracyjny, CSV, stary plik danych). Każda ścieżka względna z zewnątrz:

- rozwiąż do pełnej (`Path.GetFullPath(Path.Combine(katalog_bazowy, sciezka))`) i odrzuć, gdy wynik nie leży wewnątrz katalogu bazowego (blokuje `..\..\`, ścieżki absolutne i zip-slip przy rozpakowywaniu archiwum);
- tę samą walidację stosuj przed **odczytem, zapisem i usunięciem** pliku — jedna funkcja, trzy użycia.

## 8. Usuwanie rekordu = czyszczenie referencji

Usunięcie rekordu bez czyszczenia zostawia „wiszące" identyfikatory w innych rekordach. Skutek: ciche spadki sum w raportach i fałszywe różnice w kontroli spójności.

**Wzorzec:** wydzielona klasa/usługa czyszcząca (`funkcja_usun_referencje_obiektu`), wywoływana z każdej ścieżki usuwania, zawsze z potwierdzeniem od użytkownika.

## 9. Grafy w modelu — cykle i odwracalność operacji

- **Cykle** (obiekt_1 → obiekt_2 → obiekt_1 przez pole „rodzic/komplet") przy rekursji bez zbioru odwiedzonych kończą się przepełnieniem stosu, którego nie da się złapać. Zawsze śledź odwiedzone identyfikatory; przy tworzeniu krawędzi odrzucaj cykl.
- **Korekty danych muszą być odwracalne** (np. odjęcie wartości od licznika historycznego po zdarzeniu). Przechowuj flagę „korekta zastosowana" w samych danych (przeżyje restart) i trzymaj w warstwie prezentacji migawkę stanu sprzed edycji (bo kontrolki modyfikują pola przed zapisem). Usunięcie cofa korektę, przywrócenie powtarza, edycja przelicza.

## 10. Publikacja aplikacji desktopowej — katalog zamiast pojedynczego pliku

Self-contained **single-file** bywa problematyczny na części maszyn (starsze środowiska, problemy z rozpakowaniem natywnych bibliotek). Bezpieczniejszy wybór: publikacja jako **katalog** (`PublishSingleFile=false`, self-contained), spakowany do zip — użytkownik rozpakowuje i uruchamia główny plik wykonywalny.

```bash
dotnet publish sciezka/do/projektu.csproj \
  -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=false -o katalog_wyjsciowy
# potem zip katalogu katalog_wyjsciowego -> paczka_wydania.zip
```

Przed wydaniem: kompilacja + testy zielone, podniesiona wersja w pliku projektu, wpis w changelogu, commit, tag `v<wersja>`, push.

## 11. Testy przed każdym wydaniem — nawet gdy „trzeba szybko"

Testy jednostkowe/akceptacyjne łapią regresje w regułach biznesowych w sekundę. Przy każdej serii napraw: najpierw test czerwony (pokazuje błąd), potem poprawka, potem zielony.

## 12. Konwencje projektu (żeby kod był spójny)

- Identyfikatory, komentarze i komunikaty w języku projektu (tu: polskim); dokumentacja XML na publicznych członkach.
- Ostrzeżenia traktowane jak błędy — kompilacja musi być zielona bez ostrzeżeń.
- Zapis danych atomowy (plik tymczasowy + podmiana + kopia `.bak`); kopie zapasowe z retencją (limit liczby, unikalne nazwy — unikaj kolizji w tej samej sekundzie).
- Warstwa prezentacji: MVVM z generatorami kodu (`[ObservableProperty]`, `[RelayCommand]`, `partial void OnXChanged`).
- Reguły biznesowe stałe we wszystkich instalacjach — zmiana reguły oznacza zmianę wersji reguł.
- Commituj często, po każdej skończonej jednostce pracy; komunikaty commitów w języku projektu.
