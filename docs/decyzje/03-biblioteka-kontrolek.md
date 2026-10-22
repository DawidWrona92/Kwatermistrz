# Decyzja 03: biblioteka kontrolek WPF

## Kontekst
§10.8: gotowa, utrzymana biblioteka kontrolek WPF z motywem jasnym/ciemnym i dobrą tabelą danych. Paleta własna (rozdział 10.2), typografia Bahnschrift. NFR-04: płynne przewijanie list 1 000+ wierszy przy 5 000 przedmiotów.

## Wybór
**WPF-UI (wpfui, MIT)** jako baza motywu i kontrolek (przyciski, pola, przełączniki, nawigacja, okna dialogowe) + **wbudowany `DataGrid` WPF** z wirtualizacją (`EnableRowVirtualization`) dla gęstych tabel katalogowych. Motyw jasny/ciemny z palety §10.2 nadpisujemy własnym słownikiem zasobów.

Własne kontrolki (zgodnie z §10.8 — tam, gdzie biblioteka nie daje): mozaika wypraw, próbnik kamuflażu, pasek spójności kamuflażu, wykresy słupkowe (FR-909, bez kołowych).

## Alternatywy rozważone
- **MahApps.Metro (MIT)**: dojrzałe, ale wizualnie przestarzałe względem kierunku §10 (spokojny, neutralny interfejs); słabsze wsparcie .NET 8/9/10.
- **Material Design In XAML (MIT)**: ładne, ale ciężkie, wolniejsze w gęstych tabelach, mniej "techniczny" charakter.
- **ModernWpf (MIT)**: dobry, ale projekt w trybie utrzymania (mało nowych wydań) — ryzyko dla długowieczności.
- **Syncfusion / DevExpress**: płatne licencje komercyjne — wykluczone (§11: tylko MIT/Apache-2.0/BSD).

## Konsekwencje
- WPF-UI wymusza własne okno (`FluentWindow`) — akceptowalne, motyw i tak jest własny.
- DataGrid WPF wymaga własnego stylowania nagłówków (sortowanie, filtry) — planowane jako kontrolka opakowująca.
- Kompilacja WPF na maszynie deweloperskiej bez Windows wymaga `EnableWindowsTargeting=true` — uwzględnione w csproj (testy logiki nie zależą od WPF).
