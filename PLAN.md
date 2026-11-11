# Plan rozwoju Kwatermistrza

**Aktualna wersja:** 1.0.3  
**Aktualny etap:** 1 — Poprawki  
**Następne zadanie:** wspólny odbiór 1.1–1.3 na Windows, następnie 1.4 — Nawigacja i zakładki  
**Stan prac:** poprawki z audytu i etapów **1.1 Wyprawy, 1.2 Import, 1.3 Jedno źródło prawdy** są w roboczym [PR #5](https://github.com/DawidWrona92/Kwatermistrz/pull/5); automatyczny CI Windows i ręczne testy całego pakietu na kopii bazy warunkują scalenie.

> Ten plik jest główną listą zadań projektu. Aktualizujemy go po zakończeniu każdego punktu.

## Etap 1 — Poprawki

- [ ] **1.1 Wyprawy** — poprawić dodawanie, wybór, edycję, usuwanie i przywracanie; zapewnić poprawne przeliczanie liczników historycznych i brak podwójnego naliczania. *Kod przygotowany; do wykonania: [testy i akceptacja na Windows](docs/testy-wypraw-1.1.md).*
- [ ] **1.2 Import** — sprawdzić import XLSX, ogólny, pakiety i `.kwreko`; naprawić podgląd, komunikaty, rozwiązywanie konfliktów oraz odświeżanie danych po imporcie. *Kod i testy regresyjne przygotowane; do akceptacji: [checklista 1.2](docs/testy-import-1.2.md). Import podstawowego XLSX do istniejącej bazy nadal wymaga ręcznej kontroli potencjalnych duplikatów.*
- [ ] **1.3 Jedno źródło prawdy** — ujednolicić stan JSON, ekranów i obliczeń. *Kod przygotowany: transakcje zachowujące obiekty po ID, powiadamianie o zmianach, odświeżanie ekranów, ochrona edycji, migawka odliczeń historycznych (opcjonalne pole JSON), zabezpieczenie uszkodzonych plików. Do odbioru: [testy 1.3](docs/testy-jedno-zrodlo-prawdy-1.3.md).*
- [ ] **1.4 Nawigacja i zakładki** — sprawdzić przełączanie ekranów, wybór rekordów, filtry, panel karty i skróty klawiszowe; zweryfikować poprawki z PR #5.
- [ ] **1.5 Przyciski i spójność UX** — ujednolicić „Dodaj”, „Edytuj”, „Zapisz”, „Usuń”, „Otwórz” i „Potwierdź”; poprawić niejasne lub nieaktywne akcje, zaznaczanie oraz menu kontekstowe.
- [ ] **1.6 Integralność i kopie danych** — zabezpieczyć odzyskiwanie po uszkodzeniu JSON, zweryfikować backup/przywracanie zdjęć oraz referencje między przedmiotami, replikami, zestawami i wyprawami.
- [ ] **1.7 Testy i stabilizacja** — uruchomić build, testy jednostkowe i regresyjne oraz ręczne testy wszystkich zakładek na Windows; usunąć wykryte błędy.

**Koniec etapu 1:** aplikacja nie zawiesza się przy typowych operacjach, zachowuje dane, wszystkie podstawowe akcje działają przewidywalnie, a testy przechodzą.

## Etap 2 — Ujednolicenie interfejsu

- [ ] **2.1 Tabele i formularze** — wspólne zasady kolumn, filtrowania, sortowania, zaznaczania, walidacji i komunikatów.
- [ ] **2.2 Wygląd i dostępność** — poprawić ciemny/jasny motyw, czytelność, rozmiary kontrolek oraz zachowanie przy mniejszych oknach.
- [ ] **2.3 Obsługa użytkownika** — dopracować stany puste, błędy, podpowiedzi i potwierdzenia operacji.

**Koniec etapu 2:** podobne czynności wykonuje się w taki sam sposób w każdej zakładce.

## Etap 3 — Rozwój funkcji

- [ ] **3.1 Zdjęcia w zestawach** — zdjęcia własnej sylwetki oraz galeria zdjęć referencyjnych.
- [ ] **3.2 Praca z katalogiem** — usprawnić wyszukiwanie, powiązania sprzętu z zestawami i podgląd szczegółów.
- [ ] **3.3 Raporty i eksport** — zweryfikować zgodność raportów z danymi oraz dopracować eksporty.

**Koniec etapu 3:** nowe funkcje są spójne z resztą aplikacji i objęte testami.

## Zasady prowadzenia prac

1. Pracujemy kolejno, zaczynając od **1.1**. Nie przeskakujemy nad błędami wpływającymi na dane.
2. Jedno zadanie = jeden jasno opisany zakres zmian, testy i commit (lub osobny PR, gdy zakres jest większy).
3. Zaznaczamy `[x]` **dopiero po sprawdzeniu działania**; sam napisany kod nie oznacza ukończonego zadania.
4. Po każdym zadaniu aktualizujemy **Następne zadanie** i ten plik. Nowe błędy dopisujemy do odpowiedniego etapu.
5. **Backward compatibility bazy jest obowiązkowa.** Każda nowa wersja musi poprawnie czytać istniejące pliki `dane.json` (w tym starsze formaty), zachować ich identyfikatory, historię, zdjęcia, powiązania i liczniki. Nie wolno po cichu wyzerować, nadpisać ani przeliczyć danych historycznych.
6. Zmiana schematu lub reguł obliczeń wymaga testów zgodności na starej bazie, scenariusza migracji, kopii zapasowej przed migracją oraz możliwości odzyskania danych. **SchemaVersion pozostaje 1.** Od 1.3 nowe korekty mogą zapisywać *opcjonalne* `SkladnikiKorekty`; starsze bazy bez tego pola pozostają czytelne, a brak migawki nie jest automatycznie rekonstruowany.

**Szczegóły aktualnego audytu:** [docs/audyt-2026-11-11.md](docs/audyt-2026-11-11.md).
