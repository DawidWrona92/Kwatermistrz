# Import 1.2 — lista odbioru

**Status:** poprawki zapisane, przed scaleniem wymagane testy na Windows i kopii prawdziwej bazy.
**Kontrakt:** SchemaVersion = 1, zachowane stare ID, powiązania, zdjęcia, daty oraz liczniki historyczne.

## Kompilacja i regresja

- [ ] GitHub Actions: build WPF .NET 10 i wszystkie testy Core kończą się sukcesem na ostatnim commicie.
- [ ] Nowe testy: TransakcjaDanychTests, TransakcyjnyImportPakietuTests, ImportHistorycznychLicznikowTests, ScalaczRekordowZgodnoscTests, ImportZdjecRekonstrukcjiTests, CzytnikCsvTests.

## Testy ręczne — zawsze na kopii danych

- [ ] Przed testem skopiuj cały katalog danych (plik dane.json, kopie, zdjęcia). Zanotuj liczbę rekordów, historyczne liczniki i dzień zerowy.
- [ ] Import podstawowego XLSX: podgląd nie zamraża okna; przycisk zastosowania jest zablokowany przed podglądem.
- [ ] XLSX do niepustej bazy: użytkownik widzi ostrzeżenie przed duplikatami; stary dzień zerowy i zapisane sumy lat nie są nadpisywane.
- [ ] Import ogólny: oba panele nie nakładają się, możliwe jest przewijanie i odczyt komunikatów.
- [ ] Pierwszy niepusty wiersz XLSX jako nagłówek; przy pustym pliku czytelny błąd zamiast wyjątku.
- [ ] Mapowanie ogólne wymaga pola Nazwa lub Firma; nie pozwala przypisać kilku kolumn do jednego pola.
- [ ] Zmiana mapowania lub kategorii unieważnia dawny podgląd; zaznaczenie Pomiń wyłącza wiersz z importu.
- [ ] CSV: cytowane średniki/przecinki, cudzysłowy, wielowierszowe komentarze, plik Windows-1250, niedomknięte cudzysłowy.
- [ ] Import pakietów: można wybrać Scal tylko dla znalezionego lokalnego rekordu; Dodaj/Pomiń zawsze są czytelne.
- [ ] Scalenie pakietu: istniejące aliasy, kategorie, sloty i ID nie znikają; nieudany import nie modyfikuje oryginału.
- [ ] Import .kwreko: dialog potwierdzenia, automatyczny backup, zachowanie lokalnych ról, kategorii, napędów i liczników.
- [ ] Dwukrotny import tego samego .kwreko nie dubluje rekordów o tym samym ID.
- [ ] Zdjęcie istniejące pod identyczną ścieżką nie jest nadpisywane; nowe zdjęcia pojawiają się po imporcie.
- [ ] Po imporcie bez restartu odświeżają się Pulpit, Repliki, Wyposażenie, Słowniki, Kategorie, Rekonstrukcje, Zestawy, Wyprawy i Raporty.
- [ ] Restart aplikacji: JSON pozostaje czytelny i wszystkie historyczne powiązania istnieją.

## Otwarte kwestie

- Import podstawowego XLSX do już zapełnionej bazy może utworzyć logiczne duplikaty pod nowymi ID; jest ostrzeżenie. Automatyczne dopasowanie wymaga osobnego zadania.
- Pełne odzyskiwanie po uszkodzeniu jednocześnie dane.json i dane.json.bak jest w etapie 1.6.
- Zadanie 1.2 oznaczamy jako skończone dopiero po zielonym CI i ręcznej akceptacji.
