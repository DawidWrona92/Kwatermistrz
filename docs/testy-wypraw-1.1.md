# Wyprawy 1.1 — testy przed scaleniem

**Status:** kod zapisany na gałęzi, testy automatyczne i GUI **nieuruchomione**.  
**Środowisko:** Windows, .NET 10 SDK, kopia rzeczywistego katalogu danych. **Nie testuj na jedynej kopii bazy.**

## Testy automatyczne

```powershell
dotnet build Kwatermistrz.slnx
dotnet test tests/Kwatermistrz.Core.Tests/Kwatermistrz.Core.Tests.csproj
```

Nowe przypadki w `OperacjeNaWyprawachTests.cs`: kopia edycji, zmiana daty/roku/repliki, odliczenie i jego cofnięcie, usuwanie/przywracanie, atomowe odrzucenie niespójnej korekty, plik JSON bez flagi historycznej i migracja indeksów wariantów 1.0.0.

## Testy ręczne (checkbox zaznaczamy dopiero po rzeczywistym wykonaniu)

- [ ] **Odczyt starej bazy:** uruchom na kopii prawdziwego `dane.json` z 1.0.0–1.0.3. Porównaj liczby replik, wypraw, zestawów, zdjęcia i powiązania przed/po.
- [ ] **Szybkie dodawanie:** zmień datę w DatePicker, ustaw zestaw i wariant, wybierz replikę, wpisz miejsce; kliknij „Dodaj”. Sprawdź zapis po restarcie aplikacji.
- [ ] **Nowa bez zestawu:** dodaj wyprawę bez wybranego zestawu; upewnij się, że warianty z poprzedniej wyprawy nie zostały skopiowane.
- [ ] **Edycja i anulowanie:** wybierz wyprawę, zmień datę/miejsce/replikę/wariant, anuluj, przejdź na inną kartę i uruchom aplikację ponownie; oryginał musi pozostać bez zmian.
- [ ] **Edycja i zapis:** edytuj wyprawę, kliknij „Zapisz zmiany” i sprawdź wynik na liście, po restarcie oraz w raportach.
- [ ] **Replika wycofana/sprzedana:** otwórz historyczną wyprawę z taką repliką i zapisz edycję komentarza; powiązanie z repliką ma pozostać.
- [ ] **Usuwanie:** sprawdź „Nie” i „Tak” w potwierdzeniu, następnie widok „Tylko usunięte”.
- [ ] **Przywracanie:** przywróć wyprawę; powinna pojawić się na liście aktywnych bez duplikatu.
- [ ] **Dzień zerowy:** dodaj, edytuj, usuń i przywróć wyprawę sprzed daty zerowej. Porównaj liczniki historyczne repliki, munduru i roku przed/po; nie może wystąpić podwójne naliczenie.
- [ ] **Brak liczników:** przy próbie odliczenia z licznika 0 aplikacja musi odmówić, niczego nie zapisać i wyświetlić jasny komunikat.
- [ ] **Stara usunięta wyprawa:** przywróć rekord z bazy, której `OdliczonaOdLicznikow` jest `false`; sprawdź monit i działanie odpowiedzi Tak/Nie/Anuluj.
- [ ] **Filtry i Enter:** Enter w polu filtra lub edycji nie może dodać wyprawy. Sprawdź rok, replikę, zestaw i wyszukiwarkę.
- [ ] **Nawigacja:** powrót do zakładki, skrót Ctrl+N i zmiana aktywnej rekonstrukcji bez crasha.

## Kryterium akceptacji

Dopiero gdy build, testy i wszystkie scenariusze ręczne zakończą się poprawnie, można zaznaczyć **1.1** w `PLAN.md` i rozważyć scalenie PR. Bez tej weryfikacji zmiany pozostają robocze.

**Istniejące ograniczenie:** baza przechowuje tylko flagę odliczenia, a nie pełną migawkę jego dawnych składników. Jeżeli po pierwotnym odliczeniu zmieniono lub usunięto elementy zestawu, dokładne odtworzenie historycznych korekt może wymagać ręcznego uzgodnienia danych. Nie wprowadzamy automatycznej migracji bez potwierdzonej strategii zgodności wstecznej (etap 1.3/1.6).
