---
name: programowanie-kwatermistrz
description: Lekcje z pracy nad Kwatermistrzem (C#, WPF, .NET 10) — wzorce i pułapki, których trzeba unikać: migracje JSON, edycja plików skryptami, TreatWarningsAsErrors, import CSV/XLSX, publikacja WPF, testy przed wydaniem. Używaj przy każdej pracy nad tą aplikacją.
---

# Programowanie Kwatermistrza — lekcje i wzorce

Zebrane podczas budowy aplikacji i naprawiania 51 błędów z raportu przeglądu kodu (wydanie 1.0.1). Czytaj przed większą zmianą; każda sekcja kończy się regułą „nie rób tak".

## 1. Migracje danych JSON — pre-pass na `JsonNode`, nigdy shadow properties

**Pułapka (złamaliśmy to sami):** dodanie do modelu właściwości „cienia" z `[JsonPropertyName]` kolidującym z istniejącym kluczem (`IndeksWariantuDomyslnego` vs `WariantDomyslnyId`) psuje serializację — System.Text.Json zapisuje oba klucze lub gubi dane.

**Wzorzec (stosowany w `MagazynJson.DeserializujZMigracja`):** migrację rób jako pre-pass na `JsonNode`/`JsonObject` **przed** `Deserializer.Deserialize`:

```csharp
var node = JsonNode.Parse(json) as JsonObject;
PrzepiszStareIndeksyWariantow(node); // przypisz Id wariantom, przepisz int->Id
var dane = node.Deserialize<DaneAplikacji>(Opcje);
```

- Wszystkie miejsca odczytu (Wczytaj, WczytajZKontrola, WczytajZPliku) idą przez `DeserializujZMigracja`.
- Po migracji dopisz test roundtrip: zapis → odczyt → dane równe; zapis nie zawiera starego klucza.
- Nigdy nie „doklejaj" właściwości do modelu tylko po to, żeby odczytać stare dane.

## 2. Edycja plików C# przez skrypty (Python/bash heredoc) — literały `\n`

**Pułapka (3 razy się zdarzyło):** zamiana w Pythonie `"\n"` na tekst z rzeczywistym znakiem nowej linii w środku stringa/charu w C# (`'\n'` jako dosłowny newline) — plik przestaje się kompilować. To samo dotyczy `\r`, `\t`, `\\`.

**Reguła:** w skrypcie zamieniającym wstawiaj sekwencje jako `\\n` (podwójnie escapowane), żeby w pliku C# zostało `\n`. Po każdej masowej edycji skryptem od razu `dotnet build`.

## 3. `TreatWarningsAsErrors` — reguły CA to błędy kompilacji

Projekt ma włączone `TreatWarningsAsErrors` i analizatory .NET. Typowe pułapki:

- **CA1859** — parametr/zmienna deklarowana jako interfejs (`IReadOnlyDictionary`, `IEnumerable`), gdy wystarczy typ konkretny (`Dictionary`, `List`). Kompilator traktuje to jako błąd.
- **CA1865** — `string.EndsWith('?')` z charem zamiast `"?"` (albo odwrotnie tam, gdzie trzeba char).
- **CS0103/CS1061** — po dopisaniu pola do modelu (`SlotZestawu.SzablonSlotId` jako `string?`) wszystkie konstruktory i użycia muszą być zgodne z typem (nullable!).

**Reguła:** po każdej zmianie modelu uruchom pełny `dotnet build Kwatermistrz.slnx` — nie tylko testy.

## 4. Import z Excela/CSV — cztery klasyczne błędy

- **CSV to nie „linia = wiersz”.** Pola w cudzysłowach mogą zawierać nowe linie — czytaj całość i parsuj zgodnie z RFC 4180 (stan: w cudzysłowie / poza, `""` = escapowany cudzysłów). Patrz `CzytnikCsv.ParsujCalkowicie` w `ImporterOgolny.cs`.
- **Kodowanie:** polskie CSV z Excela bywa w Windows-1250, nie UTF-8. Wykrywaj BOM; bez BOM zakładaj cp1250. UTF-8 bez BOM może dać „�" zamiast polskich znaków.
- **Formuły Excela (CSV injection):** wartość zaczynająca się od `=`, `+`, `-`, `@` (lub tab) w komórce, którą otworzy użytkownik w Excelu, wykona się jako formuła. Przy eksporcie CSV poprzedzaj takie wartości apostrofem `'`. Stosuj w każdym eksporterze CSV (`Csv()` helper).
- **Liczby z doubli:** suma składników kosztu z Excela (`110.49000000000001`) nigdy nie będzie równa `decimal` — porównuj z tolerancją (`Math.Abs(a - b) > 0.005m`), nie `!=`.

## 5. Dopasowanie nazw — po granicach słów, nie `Contains`

`Contains` łapie podłańcuchy: „pas" trafia w „pasoszelki", „m4" w „m43”. Do heurystyk importu używaj dopasowania po granicach słów (`Dopasowanie.SlowoWTekscie` — granica = znak niebędący literą/cyfrą). To samo przy usuwaniu słów sezonowych („lato” nie wycinaj z „latem" w środku słowa — wycinaj całe słowo).

## 6. WPF: `ItemsSource` nie odświeża tej samej instancji listy

Jeśli `ItemsSource` jest ustawiony na `List<T>` i zmienisz zawartość tej samej listy + `OnPropertyChanged`, kontrolka się nie odświeży. Wzorce w projekcie:

- Kolekcje ekranów: `ObservableCollection<T>` + `Clear()`/`Add()`.
- Listy „przeliczane od zera": zwracaj `.ToList()` (nowa instancja) + `OnPropertyChanged`.
- Przy masowych zmianach filtrów (każda flaga osobno przebudowuje indeks): `ZawiesOdswiezanieFiltru()` / `WznowOdswiezanieFiltru()` — jedno odświeżenie na końcu.

## 7. Bezpieczeństwo ścieżek — waliduj wszystko z zewnątrz

Dane mogą przyjść z importu (plik `.kwreko`, `.kwpakiet`, CSV, stare dane.json). Każda ścieżka względna z zewnątrz:

- rozwiąż `Path.GetFullPath(Path.Combine(katalogBazowy, sciezka))` i odrzuć, gdy wynik nie zaczyna się od `katalogBazowy + Path.DirectorySeparatorChar` (blokuje `..\..\`, ścieżki absolutne, zip-slip przy rozpakowywaniu),
- tę samą walidację stosuj przed **odczytem, zapisem i usunięciem** pliku (patrz `SerwisZdjec.PelnaSciezkaWalidowana`, `EksportRekonstrukcji.WyodrebnijZdjecia`).

## 8. Usuwanie rekordu = czyszczenie referencji

Usunięcie repliki/przedmiotu bez czyszczenia zostawia „wiszące" Id: `Wyprawa.ReplikaGlownaId`, elementy zestawów, `Zadania.DotyczyId`, `KompletId`, `ZamontowanyNaId`, `Konfiguracje.ElementyId`. Skutek: ciche spadki sum w raportach i fałszywe różnice w kontroli spójności.

**Wzorzec:** `CzyscicielReferencji.UsunReferencjeRepliki` / `UsunReferencjePrzedmiotu` (Core) — wywoływane z `Usun` w ViewModelu, zawsze z potwierdzeniem (MessageBox YesNo).

## 9. Grafy w modelu — cykle i odwracalność operacji

- **Cykle w kompletach** (A→B→A przez `KompletId`) przy rekursji bez zbioru odwiedzonych = StackOverflow, którego nie da się złapać. Zawsze śledź odwiedzone Id; przy dołączaniu do kompletu odrzuć cykl.
- **Korekty liczników muszą być odwracalne** (FR-807: odliczanie wyprawy od liczników historycznych). Przechowuj flagę `Wyprawa.OdliczonaOdLicznikow` w danych (przeżyje restart) i trzymaj w ViewModelu migawkę wpływu sprzed edycji (`_wplywSprzedEdycji` — bo bindingi mutują pola przed `Zapisz`). Usunięcie cofa, przywrócenie powtarza, edycja przelicza.

## 10. Publikacja WPF — katalog, nie single-file (dla tej aplikacji)

Wydanie 1.0.0 jako self-contained **single-file** nie uruchomiło się na maszynie użytkownika (stare VS, brak debuggera). Od 1.0.1 publikujemy jako **katalog** (`PublishSingleFile=false`, self-contained, win-x64), zipujemy katalog — użytkownik rozpakowuje i uruchamia `Kwatermistrz.exe`.

```bash
dotnet publish src/Kwatermistrz.App/Kwatermistrz.App.csproj \
  -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=false -o dist/1.0.1
# potem zip katalogu dist/1.0.1 -> releases/Kwatermistrz-1.0.1.zip
```

Przed wydaniem: `dotnet build` + `dotnet test` zielone, bump wersji w csproj, wpis w CHANGELOG, commit, tag `v<wersja>`, push.

## 11. Testy przed każdym wydaniem — nawet gdy „shipujemy szybko"

Testy jednostkowe/akceptacyjne (xUnit, `tests/Kwatermistrz.Core.Tests`) łapią regresje w regułach biznesowych (BR-01..BR-10, FR-503, FR-807) w sekundę. Przy każdej fali napraw: najpierw test czerwony (pokazuje błąd), potem fix, potem zielony. W wydaniu 1.0.1: 94 testy.

## 12. Konwencje repo (żeby kod był spójny)

- Identyfikatory, komentarze i komunikaty **po polsku**; XML doc na publicznych członkach.
- `TreatWarningsAsErrors` — build musi być zielony bez ostrzeżeń.
- Zapis danych atomowy (tmp + move + `.bak`) w `MagazynJson`; kopie zapasowe z retencją (maks. 20, nazwy unikalne — unikaj kolizji sekundowych).
- ViewModele: CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`, `partial void OnXChanged`).
- Reguły biznesowe są stałe we wszystkich instalacjach (NFR-14) — zmiana reguły = zmiana `WersjaRegul`.
- Commituj często, po każdej skończonej fali; komunikaty po polsku.
