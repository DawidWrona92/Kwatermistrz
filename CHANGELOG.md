# Changelog

Wszystkie istotne zmiany w projekcie. Format: [Keep a Changelog](https://keepachangelog.com/pl/1.1.0/), wersjonowanie semantyczne.

## [Nieopublikowane]

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
