# Decyzja 04: wykresy i kontrolki własne

## Kontekst
FR-909: wykres słupkowy wypraw wg lat, poziome słupki udziału wg strony i wg munduru. Bez wykresów kołowych. §10.5: mozaika wypraw (kafelki 14×14 px) i próbnik kamuflażu (kwadrat z paskiem 1–4 barw). §10.8: własne kontrolki tam, gdzie biblioteka ich nie daje.

## Wybór
Wszystkie cztery elementy jako własne kontrolki WPF na `Canvas`/`Grid` z rysowaniem prostokątów — bez biblioteki wykresów. Proste słupki i kafelki nie uzasadniają zależności, a pełna kontrola nad motywem i dostępnością (NFR-08: fokus, czytnik ekranu, kontrast) jest łatwiejsza we własnym kodzie.

## Alternatywy
- **OxyPlot / LiveCharts2 (MIT)**: gotowe wykresy, ale ciężkie, trudniejsze do osadzenia w palecie §10.2 i do spełnienia wymagań dostępności (brak natywnego wsparcia czytnika ekranu). Odrzucone.

## Konsekwencje
- Kontrolki: `MozaikaWypraw`, `ProbnikKamuflazu`, `PasekSpojnosci`, `WykresSlupkowy`.
- Każda kontrolka ma odpowiednik tekstowy/alternatywny opis dla dostępności (NFR-08).
