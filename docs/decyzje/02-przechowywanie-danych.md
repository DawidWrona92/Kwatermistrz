# Decyzja 02: przechowywanie danych

## Kontekst
NFR-03: dane lokalnie w jednym miejscu, w otwartym formacie czytelnym poza aplikacją, przenoszenie przez kopiowanie folderu. NFR-05: trwałość — zapis atomowy, kontrola integralności przy starcie, automatyczna kopia przed importem i migracją.

## Wybór
Jeden plik `dane.json` (System.Text.Json, wcięcie dla czytelności, kodowanie UTF-8, `schemaVersion` na poziomie dokumentu) plus folder `zdjecia/` na zdjęcia i miniatury. Całość w katalogu danych, domyślnie `%AppData%\Kwatermistrz\` (Windows), zmienny w ustawieniach (FR-1006).

Zapis atomowy: zapis do `dane.json.tmp`, fsync, zamiana przez `File.Move(tmp, dane.json, overwrite: true)` — po zatwierdzeniu zmiany nie da się jej stracić przy awarii procesu (NFR-05). Przy starcie: kontrola integralności (deserializacja + walidacja referencji), a przy niepowodzeniu próba odczytu ostatniej kopii `.bak`.

Przed importem i migracją automatyczna kopia do `kopie/YYYY-MM-DD-HHmm/` (FR-1107). Ręczna kopia jednym poleceniem: zip katalogu danych (FR-1107).

## Alternatywy
- SQLite (Microsoft.Data.Sqlite): mocniejsze dla relacji, ale format binarny zamknięty w jednym pliku — trudniejszy do odczytu "standardowymi narzędziami" i do przeglądu różnic. Odrzucony na rzecz jawnego JSON.
- Osobne pliki JSON per encja: więcej ruchu przy zapisie atomowym, ryzyko niespójności między plikami. Odrzucony.
- Folder z wieloma plikami (jedna encja = jeden plik): skaluje się lepiej przy 5 000 przedmiotów, ale komplikuje transakcyjność. Odrzucony dla v1; migracja w przyszłości możliwa (otwarty format).

## Konsekwencje
- Przy 5 000 przedmiotów / 2 000 wyprawach plik ma kilka MB — mieści się w pamięci, start < 3 s (NFR-04) bez indeksów.
- Zapisujemy cały dokument przy każdej zmianie — przy takim wolumenie to milisekundy.
- JSON zachowuje kolejność kolekcji; sortowanie prezentacyjne robimy w warstwie widoku.
