# Odbiór 1.3 — jedno źródło prawdy

**Status:** kod 1.3 wdrożony na roboczej gałęzi. Testy xUnit/CI to nie testy GUI. Weryfikację ręczną wykonać **razem z 1.1 i 1.2**, na KOPII rzeczywistego katalogu danych.
**Kompatybilność:** `SchemaVersion` pozostaje `1`. Starsze pliki JSON nadal są czytane. Nowe odliczenia historyczne otrzymują tylko **opcjonalne** pole `SkladnikiKorekty`; brak pola w starszej bazie jest poprawny.

## Automatyczny odbiór

- [ ] Ostatni commit ma zielony GitHub Actions na Windows: build Release WPF, testy Core, publikacja Windows x64.
- [ ] `TransakcjaDanychTests`: po imporcie zachowane są referencje do dokumentu, list, istniejących replik, zestawów, słowników i ustawień.
- [ ] `TransakcjaDanychTests`: duplikat ID w późniejszej kolekcji nie zatwierdza żadnej wcześniejszej zmiany.
- [ ] `MagazynJsonTests`: powiadomienie o zmianie jest jedno na modyfikację, a sam okresowy flush nie odświeża drugi raz.
- [ ] `MigawkaHistorycznejKorektyTests`: rzeczywisty mundur i replika odliczone historycznie pozostają tymi samymi celami po zmianie bieżącego zestawu i po ponownym odczycie JSON.
- [ ] `MagazynJsonTests`: uszkodzenie obu plików blokuje zapis, a odzysk z .bak nie niszczy dobrej kopii.

## Ręczne testy na Windows (wspólna runda 1.1–1.3)

- [ ] **Kopia danych:** skopiuj cały `%APPDATA%\Kwatermistrz` (w tym zdjęcia i kopie). Nie uruchamiaj wersji testowej na jedynej kopii.
- [ ] **Lista i karta:** otwórz replikę, przejdź do innych zakładek, wróć. Edycja wybranego rekordu, zdjęcia i liczników pozostają przypisane do tego samego ID.
- [ ] **Wpisy na pulpicie:** dodaj wyprawę, wróć na Pulpit; liczba wypraw i ranking odpowiadają zapisowi. Zweryfikuj też Raporty i Do uzupełnienia.
- [ ] **Aktualizacja między zakładkami:** zmień status/nazwę/koszty repliki lub przedmiotu. Otwórz raport, zestawy, wyszukiwarkę i pulpit — nie mogą trzymać starych wartości.
- [ ] **Wpis w edytorze:** zacznij wpisywać dłuższy komentarz do wyprawy lub karty. Nie może znikać fokus ani resetować się pole po każdym znaku.
- [ ] **Szkic wyprawy:** rozpocznij edycję i wywołaj odświeżenie listy przy zachowaniu tego samego zaznaczenia. Niezapisany szkic nie może zniknąć; Anuluj przywraca pierwotną treść.
- [ ] **Import pakietu / .kwreko:** po zatwierdzeniu przejdź między kategoriami, replikami, zestawami i raportami. Wszystko korzysta z tej samej bazy. Ręczny wybór rekordu nie może pozostać na kopii sprzed importu.
- [ ] **Przywracanie:** po dodaniu nowych wpisów przywróć starszy, poprawny JSON. Pozycji nieobecnych w kopii nie powinno już być; wspólne rekordy i ich powiązania muszą odpowiadać kopii.
- [ ] **Rekonstrukcja aktywna:** przywróć kopię bez wcześniej aktywnej rekonstrukcji. Widoki nie mogą być puste wskutek filtru do nieistniejącego ID.
- [ ] **Nowa korekta historyczna:** dodaj wyprawę przed dniem zerowym z odliczeniem. Zmień obecny mundur w przypisanym zestawie; edycja opisu, usunięcie i przywrócenie wyprawy muszą korygować pierwotny mundur (z migawki).
- [ ] **Stara korekta:** otwórz historyczną wyprawę bez `SkladnikiKorekty`. Przy próbie zmiany jej składu, usunięcia lub przywrócenia aplikacja ostrzega o braku migawki. Sama zmiana komentarza nie zmienia liczników.
- [ ] **Zgodność JSON:** uruchom na kopii danych wersji 1.0.0–1.0.3. Nie może być automatycznego wyzerowania, zmiany ID, usunięcia zdjęć lub podwójnego odliczenia.
- [ ] **Nieczytelna baza:** TYLKO w osobnym, sztucznym środowisku testowym symuluj zepsute `dane.json` i `.bak`. Program musi odmówić startu i niczego nie nadpisać.
- [ ] **Restart:** zamknij program, uruchom ponownie, porównaj rekordy, daty, liczniki i powiązania.

## Granice naprawy

- Starsze wyprawy z korektą zapisaną wyłącznie jako flaga bool **nie mają historycznej migawki**. Nie można jej odtworzyć z pewnością po późniejszych zmianach zestawu. Przy ryzykownych operacjach jest ostrzeżenie; potrzebne ręczne uzgodnienie liczników.
- Nowa migawka utrwala dokładnie składniki odliczenia liczników, ale nie jest pełnym, niezmiennym zdjęciem wszystkich elementów wyprawy. Raporty pokazujące skład mogą nadal korzystać z aktualnej konfiguracji zestawu.
- Bez rzeczywistych testów WPF i kopii własnej bazy nie oznaczamy 1.1–1.3 jako odebranych.
