# Changelog

Wszystkie istotne zmiany w projekcie. Format: [Keep a Changelog](https://keepachangelog.com/pl/1.1.0/), wersjonowanie semantyczne.

## [1.0.1] — naprawy po przeglądzie kodu (wszystkie 51 punktów raportu)

Publikacja jako **folder** (nie pojedynczy plik) — wersja 1.0.0 single-file nie uruchamiała się na części maszyn. Zip zawiera cały katalog aplikacji + instrukcję; aktualizacja to rozpakowanie obok starej wersji (dane zostają w `%AppData%\Kwatermistrz`).

### Wysokie — crash / utrata danych
- Naprawiony crash przy starcie przy cyklu w kompletach (A→B→A) — wykrywanie cyklu przed dołączeniem i w kalkulatorze.
- Uszkodzony plik `.bak` nie wywala już startu; odczyt kopi w osobnym `try`.
- Jedna instancja aplikacji (mutex) — dwie instancje nie nadpisują sobie danych.
- Globalna obsługa wyjątkęw (`DispatcherUnhandledException`) — log w `error.log` + komunikat zamiast cichego zamknięcia.
- Import z arkusza respektuje decyzje o konfliktach; nie tworzy osieroconych Id słowników; drugi import nie dubluje rekordów ani rekonstrukcji.
- Import `.kwreko` nie dubluje rekonstrukcji; ponowny import nie daje duplikatów Id (indeks odporny na duplikaty).
- Zip-slip przy imporcie zdjęć — ścieżki z zip — walidacja (nic spoza katalogu danych).
- Ścieżki zdjęć z importowanych danych walidowane przed odczytem/usunięciem (`..` odrzucone).
- Zastosowanie szablonu konfiguracji nie niszczy słowników ani szablonów kart; scalanie zamiast podmiany.
- Wyprawy trzymają Id wariantu (nie indeks) — edycja zestawu nie przepisuje historii; migracja starych indeksów przy wczytaniu.
- Import ogólny: wiersz nagłówka nie jest importowany; niezmapowane pola nie czytają kolumny 0.
- Raport „Umundurowanie": komplet nie jest liczony dwa razy; użycia i koszt nie są mnożone przez liczbę elementów.
- Repliki trafiają na listę pakowania i do kosztu zestawu (FR-707).
- Typ magazynka z arkusza „Repliki” ustawia się poprawnie.
- Kreator nie dubluje szablonów kart.
- Usuwanie replik/przedmiotów czyści wszystkie referencje (wyprawy, zestawy, zadania, komplety, konfiguracje) i prosi o potwierdzenie.

### Średnie — złe wyniki, niespójności
- `Semantyka` kosztu i zadania ustawiana z wybranego rodzaju (zakup repliki liczy się do „Ceny oryginalnej”; awarie spójne na pulpicie i liście).
- Jeden sposób liczenia użyć (przez `IndeksObliczeniowy`) — ten sam przedmiot w dwóch slotach daje ten sam wynik wszędzie.
- Odliczanie od liczników historycznych (FR-807) jest odwracalne — usunięcie/przywrócenie/edycja wyprawy cofa lub powtarza korektę.
- „Średnia na miesiąc” w raporcie lat liczona od początku roku, nie „od pierwszej wyprawy do dziś”.
- „Średnia” wypraw w raporcie Broń z ułamkiem (nie dzielenie całkowitoliczbowe).
- Kopia zapasowa przy imporcie wykonywana **przed** importem (FR-1107).
- Przywracanie z kopii: kopia przed nadpisaniem, wracają też zdjęcia, wszystkie ekrany się odświeżają.
- Kopie zapasowe z retencją (maks. 20) i bez kolizji nazw w tej samej sekundzie.
- Usunięcie rekonstrukcji czyści `OstatniaRekonstrukcjaId`.
- Wyszukiwanie zadań otwiera ekran z zaznaczoną repliką/przedmiotem (RekordId = Id rekordu, nie zadania).
- „Do uzupełnienia → Potwierdź” czyści tylko własną flagę pozycji (nie wszystkie naraz).
- Samo kliknięcie rekordu nie zdejmuje znacznika „z importu” (tylko zapis karty).
- Duplikowanie/kopiowanie przedmiotu kopiuje też rekonstrukcje, datę zakupu i status.
- Eksport rekonstrukcji pakuje dokładnie jej dane (wspólne elementy raz, bez wypraw innych rekonstrukcji; wchodzą też ustawienia i liczniki historyczne).
- Porównanie sum kosztów z tolerancją 0.005 (szum doubli z Excela).
- Motyw „Systemowy” respektuje faktyczny motyw systemu; aliasy kolorów wykresów ustawiane też przy starcie.
- Mapowanie kolumn w imporcie odświeża UI.
- Listy (`Wartosci`, `Rekonstrukcje`, `RekonstrukcjeLista`) zwracają kopie albo są `ObservableCollection` — WPF odświeża.
- Nowa wyprawa z wybranym zestawem od razu ma wybrane warianty (ustawiane przy dodaniu, nie tylko przy zmianie combo).
- Kontrola spójności: komunikat OK bez sztywnych liczb, `wgReplik` liczone raz, mundury rozpoznawane po fladze kategorii (z fallbackiem nazwy).

### Niskie — kruchość, drobiazgi
- Dopasowanie importu po granicach słów („pas” ≠ „pasoszelki”, „m4” ≠ „m43”); usuwanie słów sezonowych też po granicach.
- Importer wyposażenia: nieznane kategoria/strona/miejsce/klasa → konflikt (nie cicho); kolumna „klasa” po nagłówku; realna heurystyka szkicu.
- `CzytnikXlsx`: `XSSFWorkbook` w `using`; daty z fallbackiem invariant-culture.
- CSV: pełny parser RFC 4180 (pola wieloliniowe), wykrywanie kodowania UTF-8/Windows-1250; magazyn mapowań odporny na uszkodzony JSON.
- Eksport CSV zabezpieczony przed formułami Excela; raport „Stan replik” pokazuje datę ostatniego testu.
- Eksport listy pakowania nie połyka wyjątków — błąd widać na pasku stanu.
- `SynchronizujCenaZakupu` aktualizuje pozycję z importu zamiast dodawać drugą.
- Mundur wyprawy rozpoznawany po Id slotu szablonu, nie po nazwie.
- Dialog zdjęć bez .webp (WPF go nie dekoduje); przy zmniejszaniu nieznane rozszerzenie → JPEG z korektą nazwy; usuwanie zdjęcia usuwa też oryginał.
- Przejście z pulpitu ustawia flagi filtru jednym odświeżeniem; flagi czyszczone przy nawigacji menu.
- Ranking: spójny remis po nazwie na pulpicie, liście Broń i w raporcie.
- Kosmetyka: `Komunikat` wielką literą, usunięte martwe `_ = kalk;`, unikalność nazwy przy zmianie wartości słownika, `PoleImportu` jako string w JSON.

## [1.0.0] — pierwsza wersja do codziennego użytku

Pełna specyfikacja: `docs/wymagania.md`. Wszystkie wymagania [M] i [S] z wyjątkiem odłożonych (patrz „Odłożone” na końcu sekcji Etap 6); wymagania [C] poza 1.0.

### Etap 6 — Eksport/import i szablony (FR-1106, FR-1403, FR-1405, FR-1406), monit dnia zerowego (FR-807)

#### Dodane
- **Eksport całości do Excela** (FR-1106): jeden plik .xlsx o układzie zbliżonym do arkuszy źródłowych — arkusze Broń (z wierszem sumy posiadanych, BR-04/06), Wyposażenie, Zestawy (sloty, warianty, koszty), Wyprawy, Zadania, Słowniki. Przycisk „Eksport xlsx” w pasku głównym. Test: budowa arkuszy + zapis/odczyt przez NPOI.
- **Eksport/import rekonstrukcji** (FR-1405): plik `.kwreko` (zip: dane.json + zdjecia/) — przekazanie koledze lub przeniesienie między instalacjami. Eksport wybiera rekonstrukcję i pakuje jej przedmioty, repliki, zestawy, wyprawy, zadania (wraz z elementami wspólnymi) oraz zdjęcia. Import scala konfigurację przez podgląd pakietu (FR-1404) i wyodrębnia zdjęcia; przed importem kopia zapasowa (FR-1107).
- **Import ogólny z mapowaniem kolumn** (FR-1403): dowolny plik xlsx lub csv — użytkownik wskazuje, która kolumna to które pole (nazwa, firma, kategoria, strona, wzór, miejsce, pochodzenie, liczba użyć, komentarz), opcjonalnie kategorię docelową. Sugestia mapowania po nagłówkach; mapowania zapisywalne i wielokrotnego użytku (plik mapowania-kolumn.json). Podgląd wierszy z ostrzeżeniami (nieznane wartości słowników), checkbox „Pomiń” per wiersz, znacznik „z importu, do przeglądu” (FR-1110), liczniki użyć oznaczone „do weryfikacji” (FR-806).
- **Szablony konfiguracji** (FR-1406): „Pusta (minimalna)” i „Przykład — współczesna Rosja” (kategorie i sloty z rozdziału 5, wzory kamuflaży z próbkami) — bez danych o sprzęcie (NFR-15). Przyciski na ekranie Rekonstrukcje; zastosowanie przemapowuje kategorie przedmiotów po nazwie. Kreator pierwszego uruchomienia (FR-1401) oferuje teraz trzy punkty startu: pusta aplikacja, szablon konfiguracji, import z arkusza (FR-1101).
- **Monit dnia zerowego** (FR-807): wyprawa z datą ≤ dzień zerowy wywołuje pytanie o odjęcie od liczników historycznych (replika, mundur, rok) — domyślna odpowiedź „tak”, zapobiega podwójnemu liczeniu. Kalkulator: `WplywNaLicznikiHistoryczne`.
- **NFR-11**: zdjęcia przy zapisie zmniejszane do dłuższego boku 2048 px (ustawienie w Ustawieniach, 0 = wyłączone), oryginał opcjonalnie zachowywany w podkatalogu `oryginaly/`.
- **FR-1205**: „Ostatnia zmiana: data i godzina” w stopce kart przedmiotu i repliki.

#### Odłożone (poza 1.0.0)
- Wydruk raportów i listy pakowania (FR-908 wydruk [S]), ostatnio otwarte pozycje w menu (FR-1206 [S]), zapamiętywanie filtrów między ekranami (NFR-12 reszta), automatyczna kopia przy zamknięciu (FR-1108), przypomnienie o kopii (FR-1109 [C]), wymagania [C] (FR-810, FR-811, FR-1007).

### Etap 5j — Wydajność, dostępność, instalacja (NFR-04, NFR-08, NFR-10)

#### Dodane
- **NFR-04 Wydajność**: nowy `IndeksObliczeniowy` (Core) — raz na odświeżenie listy rozwiazuje przecięcia wypraw z replikami i przedmiotami do słowników; listy replik i wyposażenia budują się w O(R·W)/O(P·W) zamiast O(R²·W)/O(P²·W). Ranking na pulpicie korzysta z tego samego indeksu. Testy: zgodność indeksu z `Kalkulator` (BR-01/02/03/08, komplety, konfiguracje nośnika) oraz budżety czasowe przy skali 5 000 przedmiotów / 2 000 wypraw / 5 000 zdjęć (budowa indeksu < 300 ms, zapis+odczyt JSON < 3 s).
- **NFR-08 Dostępność**: tekst ostry przy powiększeniu (`TextFormattingMode=Display`, `UseLayoutRounding`, `SnapsToDevicePixels` na głównym oknie); widoczny fokus klawiatury (ramka 2 px w kolorze akcentu) dla przycisków i pól WPF-UI; szare teksty pomocnicze przez `TextFillColorSecondaryBrush` zamiast `Opacity` (kontrast AA też w motywie ciemnym); próbka kamuflażu zawsze z nazwą obok i tooltipem; miniatury zdjęć i przycisk „+” opisane dla czytnika ekranu (`AutomationProperties.Name`); wiersze z różnicami w porównaniu kart oznaczone też tekstem „różnica”, nie tylko kolorem.
- **NFR-10 Instalacja**: `scripts/publikuj.ps1` — publikacja self-contained single-file (`Kwatermistrz.exe`, win-x64) + zip z instrukcją; paczka bez danych autora (NFR-15). Instrukcja po polsku dla osób bez wiedzy technicznej: `docs/instalacja/INSTRUKCJA.txt` (instalacja, aktualizacja = podmiana exe, gdzie są dane, współdzielenie konfiguracji). W Ustawieniach pokazana wersja aplikacji obok wersji reguł (NFR-14). Zweryfikowana publikacja win-x64 (~170 MB exe, bez plików .pdb).
- `.gitignore`: katalog `dist/` (wyniki publikacji).

### Etap 5i — Porównanie i kopiowanie kart (FR-406, FR-407), konfiguracje nośnika (FR-208)

#### Dodane
- **Porównanie kart replik** (FR-406): checkbox „Por.” w liście broni, przycisk „Porównaj zaznaczone” (maks. 4 repliki), panel z wierszami parametrów karty — wartości obok siebie, wiersze z różnicami podświetlone.
- **Kopiowanie karty z innej repliki** (FR-407): przycisk w kolumnie „Akcje” tworzy nową replikę z wartościami karty technicznej, szablonem karty, napędem i typem magazynka źródłowej (bez pomiarów, modyfikacji, kosztów i zadań) i otwiera jej kartę do edycji.
- **Konfiguracje nośnika** (FR-208): na karcie przedmiotu, którego kategoria jest oznaczona jako nośnik, sekcja „Konfiguracje nośnika” — nazwane listy elementów („Zapisz z zamontowanych”, „Dodaj pustą”, zastosuj, usuń). Konfigurację można dodać jako element wariantu slotu zestawu (typ „Konfiguracja”).
- Kalkulator: `RozwinNaPrzedmioty` — rozwinięcie elementu na konkretne przedmioty (przedmiot / komplet / konfiguracja nośnika); lista pakowania, skład wyprawy i koszt zestawu uwzględniają konfiguracje nośnika.

### Etap 5e — Zdjęcia (FR-211)

#### Dodane
- **Galeria zdjęć** na karcie przedmiotu i karcie repliki (FR-211 [M]): wiele zdjęć, miniatury, przycisk „+” (wybór wielu plików), powiększenie w osobnym oknie (Escape zamyka), menu kontekstowe: powiększ / ustaw jako główne / usuń. Pierwsze zdjęcie to **zdjęcie główne** (ramka).
- Pliki kopiowane do katalogu `zdjecia/` w katalogu danych; ścieżki w modelu względne; kopia zapasowa obejmuje zdjęcia (FR-1107); zmiana zdjęć zapisuje dane natychmiast (FR-1203).
- **Miniatura w liście wyposażenia** (FR-211 [S]) — pierwsze zdjęcie w kolumnie „Zdjęcie”.
- `SerwisZdjec` (ścieżki, kopia do katalogu, miniatury z `DecodePixelWidth`), `GaleriaZdjec` (kontrolka), `PowiekszenieZdjeciaWindow`, `KonwerterMiniatury`.

### Etap 5h — Wykresy (FR-909)

#### Dodane
- Raport **Wykresy** (FR-909), trzy kontrolki rysowane w czystym WPF (bez nowych zależności):
  - **WykresPionowy** — słupkowy wykres wypraw wg lat (BR-07), z wartościami nad słupkami i etykietami lat.
  - **WykresPoziomy** — poziome słupki udziału wypraw wg strony (pierwsza strona repliki głównej) i wg munduru (BR-09), z wartością i procentem.
- Kolory wykresów (`Stal`, `Linia`, `Sygnal`, `Poprawny`, `Uwaga`) jako aliasy w palecie, podążają za motywem jasnym/ciemnym (FR-1006).

### Etap 5g — Raporty Koszty / Zestawy / Stan replik (FR-905, FR-906, FR-907)

#### Dodane
- Raport **Koszty** (FR-905): wydatki wg roku, wg kategorii i wg rodzaju (nazwa ze słownika) + wartość posiadanej kolekcji wg cen zakupu (przedmioty i repliki osobno, BR-04 „Zakup”), wszystko z filtrem aktywnej rekonstrukcji.
- Raport **Zestawy** (FR-906): wyprawy, procent, ostatnie użycie, koszt zestawu (FR-708), koszt na wyprawę (BR-05); wiersz sumy.
- Raport **Stan replik** (FR-907): rozkład stanów, repliki bez testu powyżej progu (FR-104), otwarte awarie z opisem.
- Eksport CSV dla trzech nowych raportów (FR-908).

### Etap 5f — Lista pakowania (FR-707) i koszt zestawu (FR-708)

#### Dodane
- **Lista pakowania** na karcie zestawu (FR-707): elementy wariantów domyślnych (komplety rozwinięte na części, „puste” pominięte), pogrupowane wg miejsca przechowywania (Piwnica, Dom), z checkboxami „spakowane” zapamiętywanymi w zestawie; elementy zamontowane na nośniku oznaczone („na: …”); eksport listy do CSV.
- **Koszt zestawu** (FR-708): suma cen elementów (pozycje „Zakup”, BR-04) wariantów domyślnych; zakres min–max po wszystkich wariantach [C].
- Core `Kalkulator`: `ElementyListyPakowania`, `KosztZestawu`, `ZakresKosztuZestawu`; model `Zestaw.SpakowaneKlucze`.
- 3 testy FR-707/708.

#### Uwagi
- Wydruk listy pakowania (FR-707) — do realizacji razem z wydrukami raportów.

### Etap 5c/5d — Podpowiedzi pól karty (FR-405) i cena zakupu przedmiotu (FR-503)

#### Dodane
- **Podpowiedzi wartości pól karty technicznej** (FR-405): pola tekstowe zamienione na edytowalne ComboBox z wcześniejszymi wartościami tego parametru u innych replik (ogranicza rozjazdy zapisu, np. „Specna Arms” kontra „Speca Arms”).
- **Cena zakupu przedmiotu tworzy pozycję kosztową** (FR-503): `Przedmiot.SynchronizujCenaZakupu` — przy zapisie karty tworzy/aktualizuje pozycję „Zakup” (semantyka stała BR-04), przy wyczyszczeniu pola usuwa własną pozycję; ręczne pozycje zakupu zostają nietknięte. Synchronizacja także przy duplikowaniu i „Dodaj N takich samych”.
- Karta przedmiotu pokazuje listę pozycji kosztowych (data, opis, kwota).
- 4 testy FR-503.

### Etap 5b — Pulpit: karty skrótowe i ranking (FR-104, FR-105)

#### Dodane
- **Karty skrótowe na pulpicie** (FR-104) z liczbą i przejściem do listy: otwarte awarie, otwarte zadania, repliki bez testu (próg z ustawień, FR-1006), przedmioty bez użyć, pola „nieznane” kart technicznych.
- **Ranking pięciu replik** z największą liczbą wypraw (FR-105) — nazwa, liczba wypraw, koszt na wyprawę (BR-05); klik otwiera kartę repliki.
- Przejścia ustawiają filtr na ekranie docelowym: „Tylko z otwartą awarią” / „Bez testu” (Broń), „Bez użyć” (Wyposażenie), „Tylko zadania” (Do uzupełnienia).
- Widok **Do uzupełnienia** (FR-1204) pokazuje teraz także otwarte zadania (FR-601) z nawigacją do karty repliki/przedmiotu.
- Core `Kalkulator`: `OtwarteAwaria`, `OtwarteZadania`, `ReplikiBezTestu`, `PrzedmiotyBezUzycia`, `PolaNieznane`, `RankingReplik`.
- 5 testów pulpitowych (FR-104/105).

### Etap 5a — Pakiety konfiguracji (FR-1404, część FR-1003)

#### Dodane
- **Pakiet konfiguracji** `.kwpakiet` (JSON): eksport konfiguracji rekonstrukcji — słowniki (z aliasami i semantyką), wzory kamuflażu, kategorie (z oznaczeniem nośnika), rekonstrukcje z szablonami slotów, szablony kart technicznej. **Bez przedmiotów, replik, zestawów, wypraw, zdjęć i cen** (FR-1404, FR-1402).
- **Import pakietu z podglądem** (FR-1102 w wersji pakietowej): lista wszystkich wartości/wzorów/kategorii/rekonstrukcji/szablonów z domyślnymi decyzjami — dodaj nową / scal z istniejącą / pomiń; przed zastosowaniem automatyczna kopia zapasowa (FR-1107).
- **Scalanie słowników (FR-1003)**: nazwa i aliasy wartości z pakietu trafiają do aliasów istniejącej wartości lokalnej, dzięki czemu import rozpoznaje je w przyszłości; semantyka (rodzaje kosztów/zadań) uzupełniana, gdy lokalnie pusta.
- Mapowanie Id przy imporcie: strona rekonstrukcji i napęd szablonu karty wskazują na lokalne wartości po scaleniu.
- Idempotentność: powtórny import tego samego pakietu nie tworzy duplikatów.
- Nowy ekran **Pakiety** w stopce nawigacji; `PakietyViewModel`, `WierszDecyzji`; Core: `PakietKonfiguracji`, `ImporterPakietuKonfiguracji`, `RodzajeSlownikow`.
- Testy: 10 nowych (roundtrip eksport/import, brak danych o sprzęcie w pakiecie, domyślne scalanie, alias przy kolejnym imporcie, mapowanie napędu, scalanie szablonów slotów, idempotentność, błędny typ/nowsza wersja pakietu).

#### Uwagi
- FR-1406 (dołączone szablony konfiguracji) i FR-1405 (eksport całej rekonstrukcji) — pozostałe pozycje Etapu 5.

### Etap 4 — Raporty (FR-901–904, FR-908 CSV, FR-910, FR-911, FR-1106 CSV, FR-1201, FR-1204)

#### Dodane
- Ekran **Raporty** z wyborem raportu:
  - **Broń** (FR-901): miejsce, nazwa, wyprawy, %, typ magazynka, rola, strona, koszt/cena oryginalna/dodatki/koszt na wyprawę; wiersz średnich i wiersz sum dla replik posiadanych (BR-06); przełącznik „Uwzględnij sprzedane”.
  - **Umundurowanie** (FR-902): mundur lub komplet, strona, wzór, wyprawy, %, koszt (BR-10: góra+dół kompletu bez dodatków), koszt na wyprawę; wiersz łączny.
  - **Wyprawy wg lat** (FR-903): liczba wypraw w roku (liczniki historyczne + zapisane, BR-07), suma narastająca, średnia na miesiąc od pierwszej wyprawy (BR-15).
  - **Użycie wyposażenia** (FR-904): przedmiot, kategoria, użycia (BR-08), cena, koszt na użycie; sortowanie — najpierw pozycje bez użyć.
  - **Kontrola spójności** (FR-910): trzy sumy wg replik/lat/mundurów, różnice, liczba wypraw bez repliki/munduru.
- „Jak liczymy” (FR-911) — opis definicji wskaźników pod wyborem raportu.
- Eksport wybranego raportu do **CSV** (FR-908 część; Excel .xlsx — Etap 5).
- Ekran **Do uzupełnienia** (FR-1204): jedna lista — przedmioty/repliki z importu (FR-1110), przedmioty „do uzupełnienia” (FR-706), pola karty „nieznane” (FR-404), pozycje kosztowe bez opisu (FR-508), liczniki historyczne do weryfikacji (FR-806); potwierdzenie usuwa znaczniki.
- **Globalne wyszukiwanie** (FR-1201): wyniki pod polem w górnym pasku, pogrupowane wg typu (przedmioty, repliki, zestawy, wyprawy, zadania), Enter/click otwiera ekran i wybiera rekord.
- `RaportyViewModel`, `DoUzupelnieniaViewModel`, `SzukanieViewModel` + wiersze raportów.

#### Uwagi
- FR-908 eksport do Excela (.xlsx), FR-1106 eksport całości, FR-905–907, FR-909 (wykresy) — Etap 5 ([S]).
- Wszystkie ekrany nawigacji mają teraz realne strony (nie ma już placeholderów).

### Etap 3 — Wyprawy i zestawy (FR-701–705, FR-709, FR-801–805, FR-808–809, FR-101–103, FR-910 część)

#### Dodane
- Ekran **Zestawy** (FR-701–705, FR-709): lista zestawów z liczbą wypraw, % i datą ostatniego użycia; filtry (tekst, tylko szkice), aktywna rekonstrukcja (BR-16). Karta zestawu: nazwa (z podpowiedzią z nazwy munduru, gdy pusta), rekonstrukcja, strona, status szkic.
- Edytor slotów i wariantów (FR-702, FR-703): dodawanie/usuwanie slotów, wariantów (lista elementów + jawne „puste”), wybór domyślnego wariantu (FR-704), wybór elementów spośród przedmiotów i replik (FR-805 — bez sprzedanych/wycofanych).
- Ekran **Wyprawy** (FR-801–809): pasek szybkiego dodawania (FR-802) — data (domyślnie dziś), zestaw, replika główna (podpowiedź z zestawu: pierwszy wariant slotu broni głównej), miejsce, komentarz; Enter = zapis. Po wyborze zestawu skład ustawia się na warianty zapamiętane/domyślne (FR-803, FR-704) i zapamiętuje ostatnio użyte warianty.
- Lista wypraw (FR-808): sortowana od najnowszej, filtry (rok, replika, zestaw, tekst), kolumny: data, miejsce, replika główna, zestaw, mundur, komentarz; usuwanie (oznacza `CzyUsunieta`) i przywracanie — liczniki aktualizują się natychmiast (FR-809). Aktywna rekonstrukcja filtruje listę (FR-1303/1304).
- `ZestawyViewModel`, `WyprawyViewModel`, `WierszZestawu`, `WierszWyprawy`.
- Przycisk „Dodaj wyprawę” w górnym pasku (FR-103) przenosi teraz do ekranu Wyprawy z fokusem na formularz.

#### Uwagi
- FR-706 (szkice elementów przy wpisywaniu w zestawie), FR-707 (lista pakowania), FR-708 (koszt zestawu) — Etap 5.
- FR-807 (pytanie o odjęcie od liczników przy wyprawie przed dniem zerowym) — uproszczone: zapis bez pytania; pełna wersja w Etapie 5.
- Kontrola spójności (FR-910) jako osobny ekran — Etap 4 (Raporty).

### Etap 2 — Repliki (FR-301–305, FR-401–405, FR-408–409, FR-501–505, FR-601–602, FR-604)

#### Dodane
- Ekran **Broń** (FR-301): gęsta tabela replik — miejsce w rankingu wg wypraw, nazwa „model (prototyp)”, strona, rola, typ magazynka, napęd, stan, wyprawy, % wypraw, koszt całkowity/cena oryginalna/dodatki/koszt na wyprawę, ostatni test, oznaczenie otwartej awarii. Filtry: tekst, strona, stan, „tylko awaria”, „pokaż sprzedane/wycofane” (FR-305), aktywna rekonstrukcja (FR-1303).
- **Karta repliki** (FR-302) w panelu po prawej z zakładkami:
  - **Przegląd** (FR-304): stan, wyprawy i %, koszty z podziałem, koszt na wyprawę, ostatni pomiar z „na maksimum”, data ostatniego testu, otwarte zadania, ostatnie 3 wyprawy + pola podstawowe (FR-303).
  - **Karta techniczna** (FR-401, FR-404): parametry wg szablonu przypisanego do repliki (FR-1307), każde pole z wyborem stanu (wypełnione/nieznane/nie dotyczy/nie uzupełnione).
  - **Pomiary** (FR-408): data, energia, ROF, zasięg, wynik testu, „na maksimum”.
  - **Modyfikacje i koszty** (FR-501, FR-504, FR-505): pozycje kosztowe (data, opis, kwota, rodzaj) i modyfikacje ze zmianami parametrów przed/po.
  - **Zadania** (FR-601–602): dodawanie, zamykanie, usuwanie; lista zadań repliki.
  - **Wyprawy**: lista wypraw repliki (BR-02).
- `ReplikiViewModel`, `WierszRepliki` + `WierszReplikiObliczenia` (otwarta awaria, otwarte zadania, ostatnie wyprawy), `WierszParametruKarty`.
- Konwerter `KonwerterBoolToVisibility`; poprawki bindingów kart (DataContext przez UserControl).

#### Uwagi
- FR-405 (podpowiedzi wartości pól) i FR-406/407 ([S]) — Etap 5.
- FR-503: „cena zakupu” przedmiotu zostaje uproszczonym polem (pozycje kosztowe przedmiotu — Etap 5).

### Etap 1 — Wyposażenie (FR-201–207, FR-209, FR-216, FR-1110 część wyposażenia)

#### Dodane
- Ekran **Wyposażenie**: lewa kolumna kategorii z licznikami (FR-201), gęsta tabela przedmiotów (firma, nazwa, miejsce, użycia, status, komentarz) z filtrem tekstowym i przełącznikiem „pokaż sprzedane/wycofane” (FR-203, FR-209), wybór kategorii filtruje listę, aktywna rekonstrukcja też (FR-1303).
- **Karta przedmiotu** (FR-204) w panelu po prawej: nazwa, firma, kategoria, wzór, miejsce, pochodzenie, status, cena/data zakupu, komentarz; zapis po zatwierdzeniu (FR-1203).
- **Komplety** (FR-206): dołącz/odepnij przedmiot do kompletu; wspólny licznik użyć liczony w Core (BR-08).
- **Montaż** (FR-207): wybór nośnika z listy kategorii „nośnik”, zdejmij; nadpisuje poprzedni montaż.
- **Duplikuj / Dodaj N takich samych** (FR-205) — osobne rekordy z własną historią.
- **Edycja zbiorcza** (FR-216): zaznaczone pozycje zmieniają miejsce/wzór/pochodzenie/strony/status jednym poleceniem.
- `WyposazenieViewModel` + `KategorieViewModel` (FR-202: dodaj/zmień nazwę/usuń tylko pustą kategorię, oznacz „nośnik”), wiersz `WierszPrzedmiotu`, konwerter `KonwerterLicznikaKategorii`.
- Znacznik „z importu, do przeglądu” znika przy otwarciu karty lub zapisie (FR-1110).

#### Uwagi
- FR-208 (konfiguracje nośnika) i FR-211 (zdjęcia) — Etap 5 ([S]/rozszerzenia).
- FR-210 (słownik pochodzenia) edytowalny już w ekranie Słowniki.

### Etap 0 — Fundament (aplikacja WPF, FR-1001–1003, FR-1006, FR-1107, FR-1202–1203, FR-1301–1306, FR-1401–1402, FR-1407, NFR-03/05/13–15)

#### Dodane
- Projekt `src/Kwatermistrz.App` (WPF, WPF-UI 4.0.3 + CommunityToolkit.Mvvm): okno główne `FluentWindow` z układem §10.4 — lewa szyna nawigacji (Pulpit, Wyprawy, Broń, Wyposażenie, Zestawy, Raporty, Do uzupełnienia; stopka: Słowniki, Import, Ustawienia), górny pasek (wyszukiwanie Ctrl+F, przełącznik rekonstrukcji FR-1303, przycisk „Dodaj wyprawę” Ctrl+N FR-103), środek, prawy panel karty ~420 px zamykany.
- Ekrany: Pulpit (liczby FR-101 + mozaika wypraw FR-102 w kontrolce `MozaikaWypraw`), Słowniki (FR-1001 edycja wartości, FR-1002 zmiana nazwy, usuwanie tylko gdy nieużywana), Rekonstrukcje (FR-1301/1306: pusta/na wzór, edycja szablonu slotów), Ustawienia (FR-1006 motyw jasny/ciemny/systemowy przełączany na żywo, próg „bez testu”, katalog danych; FR-1107 kopia jednym poleceniem + przywracanie z pliku), Import (FR-1101/1102: wybór xlsx, podgląd rekordów i konfliktów z wyborem kandydata, zastosowanie z automatyczną kopią przed importem).
- Kreator pierwszego uruchomienia (FR-1401) — w tym etapie tylko wariant „pusta aplikacja”: nazwa pierwszej rekonstrukcji + strona, startowe sloty z Załącznika B.
- Motywy: słowniki `Themes/Paleta.xaml` (paleta §10.2 jasny/ciemny) i `Themes/Style.xaml` (typografia §10.3: Bahnschrift z zapasową Segoe UI, style tytułów/liczb/kart); `ApplicationThemeManager` przełącza motyw WPF-UI, a przycisk zmiany motywu działa od razu (FR-1006).
- Kontrolki własne: `ProbkaKamuflazu` (siatka 2×2 kolorów wzoru, §10.5), `MozaikaWypraw` (wiersze kafelków 18×18 po latach, kolor = pierwszy kolor wzoru munduru, neutralne dla historycznych, klik otwiera wyprawę).
- Serwisy: `StanAplikacji` (aktywna rekonstrukcja zapamiętywana w ustawieniach, ekran, wyszukiwanie), `KalkulatorSerwis`. DI przez `Microsoft.Extensions.DependencyInjection`.
- Ikona aplikacji `Assets/app.ico` (4 rozmiary, kolor Stal).
- `MagazynJson.WczytajZPliku` — wczytywanie danych z konkretnego pliku (przywracanie z kopii, FR-1107) + 2 testy (45 zielonych).
- Projekt App dodany do `Kwatermistrz.slnx`.

#### Zmienione
- `MozaikaWypraw` dziedziczy po `ContentControl` (rysuje kafelki jako zawartość).

#### Uwagi
- Ekrany Wyprawy/Broń/Wyposażenie/Zestawy/Raporty/Do uzupełnienia to na razie strony zastępcze — powstaną w Etapach 1–4.
- Kreator: warianty „szablon” i „import” zaplanowane na późniejszy etap (FR-1401 część).

### Etap 0 — Fundament (Core + import)

#### Dodane
- Model domenowy: przedmioty, repliki, zestawy (sloty/warianty/elementy), wyprawy, rekonstrukcje, szablony kart, pozycje kosztowe, modyfikacje, pomiary, zadania, ustawienia (`src/Kwatermistrz.Core/Model/`).
- Słowniki startowe z Załącznika B (strony, miejsca, role, typy magazynków, napędy, stany, pochodzenie, rodzaje kosztów/zadań, kategorie, wzory kamuflaży z próbkami kolorów).
- Magazyn JSON: zapis atomowy (tmp → .bak → dane.json), wczytywanie z kontrolą i awaryjnym odczytem kopii `.bak`, kopia zapasowa (`Storage/MagazynJson.cs`).
- Kalkulator reguł biznesowych BR-01..BR-16: wyprawy replik, procent wypraw, koszty (BR-04/05), statystyki roczne, kontrola spójności trzech sum (FR-910) (`Obliczenia/Kalkulator.cs`).
- Formatowanie BR-14: waluta „3 297,12 zł”, procent „21,3%”, data „01.09.2022” (`Obliczenia/Formatowanie.cs`).
- Importer pliku źródłowego xlsx (NPOI) z podglądem i konfliktami (FR-1101/1102, FR-1103, FR-1110, FR-508, FR-806, rozdział 13): repliki, karta techniczna (HPA, pomiary, zadania, duplikaty), wyposażenie (106 przedmiotów, montaż ładowania), umundurowanie (liczniki tylko z tabeli, suma 89), zestawy (15, komplety), rekonstrukcje i lata.
- Testy: 18 akceptacyjnych wartości z rozdziału 8.2 (NFR-09) + 18 jednostkowych reguł BR + 7 magazynu JSON — 43 zielone.
- Decyzje technologiczne w `docs/decyzje/` (01–06).

### Zmienione
- `Normalizacja.TekstDoPorownania`: zamiana ł→l (FormD nie dekomponuje litery ł).

## [0.0.1] — fundament repozytorium

- Inicjalizacja repo: katalogi `src/`, `tests/`, `docs/`, rozwiązanie `Kwatermistrz.slnx`, projekty Core i testów.
