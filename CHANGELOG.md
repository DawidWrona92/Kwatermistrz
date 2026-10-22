# Changelog

Wszystkie istotne zmiany w projekcie. Format: [Keep a Changelog](https://keepachangelog.com/pl/1.1.0/), wersjonowanie semantyczne.

## [Nieopublikowane]

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
