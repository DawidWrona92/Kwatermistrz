# Decyzja 05: biblioteka do plików Excel

## Kontekst
FR-1101/FR-1403: import z .xlsx (i CSV). FR-1106/FR-908: eksport raportów do .xlsx i CSV. Bez czytania .numbers (użytkownik eksportuje z Numbers sam).

## Wybór
**NPOI (Apache-2.0)** — odczyt i zapis .xlsx, działa na .NET 10, dojrzały, bez zależności systemowych. CSV przez własny writer/reader (prosty format, własna implementacja z cudzysłowami i separatorem `;` zgodnym z polskim Excelem).

## Alternatywy
- **EPPlus**: od wersji 5 licencja Polyform Noncommercial — płatna dla użytku komercyjnego, niezgodna z §11. Odrzucony.
- **ClosedXML (MIT)**: wygodniejsze API, oparte na DocumentFormat.OpenXml; cięższy. Rozważany, ale NPOI wystarczy i ma mniejszy ślad.

## Konsekwencje
- Import czyta arkusze po nazwie (Załącznik A), z tolerancją na nazwy kolumn (mapowanie w podglądzie FR-1403 w etapie 5).
- Eksport raportów: jeden arkusz na raport, formatowanie walut/daty zgodne z BR-14.
