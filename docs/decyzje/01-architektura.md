# Decyzja 01: architektura

## Kontekst
Aplikacja desktopowa WPF, jedna osoba, dane lokalne. Wymagania: logika obliczeń (rozdział 8) i importu muszą być testowalne bez interfejsu (NFR-09, §11 "Jakość"), brak funkcji spoza dokumentu.

## Wybór
Trzy projekty:

- `src/Kwatermistrz.Core` — domena, reguły biznesowe (BR-01..BR-16), magazyn danych, importer Excela. Bez zależności od WPF. Wszystko tu jest testowalne jednostkowo.
- `src/Kwatermistrz.App` — WPF (net10.0-windows), MVVM z CommunityToolkit.Mvvm, biblioteka kontrolek WPF-UI. Warstwa prezentacji nie zawiera logiki obliczeń.
- `tests/Kwatermistrz.Core.Tests` — xUnit, testy reguł BR i test akceptacyjny importu na danych z `docs/dane-startowe/` (NFR-09).

## Alternatywy
- Jednolity projekt WPF: odrzucony, miesza logikę z UI i utrudnia testy.
- Osobny projekt importera: odrzucony, import jest częścią domeny (podgląd importu pracuje na modelu domenowym).

## Konsekwencje
- Core nie zna WPF; komunikacja tylko przez modele i serwisy.
- Wersjonowanie schematu danych w Core (pole `schemaVersion`), migracje przy starcie (NFR-06).
- Każda zmiana reguły BR wymaga aktualizacji testów w `tests/`.
