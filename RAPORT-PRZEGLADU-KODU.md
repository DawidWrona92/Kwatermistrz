# Kwatermistrz — raport z przeglądu kodu

Zakres: cały kod w `src/` (Kwatermistrz.Core i Kwatermistrz.App, ok. 10 tys. linii); testy tylko pobieżnie.
Metoda: czytanie kodu. Środowisko nie miało `dotnet`, więc nic nie zostało zbudowane ani uruchomione — przy kilku punktach zaznaczono, że są prawdopodobne, a nie potwierdzone. W kodzie nic nie zmieniano.

## Wysokie — utrata lub uszkodzenie danych, crash

1. **Cykl w kompletach wywala aplikację przy starcie.** `Kalkulator.cs:129-134` i `IndeksObliczeniowy.cs:72-74` rekurencyjnie idą po `KompletId`, sprawdzając tylko `KompletId != Id`. `WyposazenieViewModel.DolaczDoKompletu` (`:280`) nie pilnuje cykli. A→B i B→A daje StackOverflow, którego nie da się złapać. Pulpit liczy `PrzedmiotyBezUzycia` przy starcie, więc aplikacja wpada w pętlę crashy po każdym uruchomieniu.

2. **Uszkodzony `.bak` też wywala start.** `MagazynJson.cs:83-84`: odczyt kopii w bloku `catch` nie jest w `try`; przy dwóch złych plikach wyjątek idzie do `OnStartup`. Dodatkowo `catch (Exception)` przy `:78` łapie wszystko (także np. nieznaną wartość enuma) i po cichu ładuje starszą kopię lub pusty dokument, a następny zapis to utrwala.

3. **Dwie instancje aplikacji nadpisują sobie dane.** Brak mutexa single-instance (`App.xaml.cs`); `lock` w magazynie działa tylko w obrębie procesu.

4. **Brak globalnej obsługi wyjątków.** Nie ma `DispatcherUnhandledException`; każdy wyjątek w `RelayCommand` zabija aplikację (np. `IOException` z `Zapisz`, `EksportujCsv` w `RaportyViewModel.cs:520` bez `try`, `UtworzKopie`).

5. **Import z arkusza ignoruje wybrane konflikty.** `ImporterKwatermistrz.Zastosuj` (`ImporterKwatermistrz.cs:34-47`) nigdzie nie używa `Konflikty` / `Decyzja` / `WyborUzytkownika`. Ekran „rozstrzygnij konflikty” (wybór nośnika ładownicy, kandydatów zestawów) nic nie robi.

6. **Import z arkusza daje osierocone Id słowników.** `Zastosuj` kopiuje rekordy z `podglad.Dane`, ale nie słowniki ani `SzablonyKart`. Id wartości startowych to losowe Guidy (`StartoweSlowniki.Utworz`), więc `_dane.Slowniki` ma inne Id niż podgląd. Po imporcie `StronyId`, `MiejsceId`, `KategoriaId`, `RodzajId` kosztów, `NapedId`, `StanId`, `SzablonKartyId` i klucze `KartaTechniczna` wskazują w próżnię. Ponadto import dopisuje trzy rekonstrukcje (Rosja/Polska/NATO) bez sprawdzania, czy już istnieją, a drugi import podwaja wszystkie rekordy.

7. **Import `.kwreko` dubluje rekonstrukcję i rekordy** (`MainWindowViewModel.cs:131-147`):
   - `ImporterPakietuKonfiguracji.Zastosuj` dodaje rekonstrukcję (akcja domyślna), a zaraz potem `_dane.Rekonstrukcje.AddRange(...)` dodaje ją drugi raz z tym samym Id.
   - Przedmioty, repliki, zestawy, wyprawy dodawane są z oryginalnymi Id. Ponowny import tego samego pliku (albo pliku wyeksportowanego z tej instalacji) daje duplikaty Id.
   - `IndeksObliczeniowy` robi `ToDictionary(p => p.Id)` (`:22`) → wyjątek przy duplikatach → wywala Wyposażenie, Broń i Pulpit.
   - Mapy Id (`mapySlownikow`, `mapaKategorii`) są liczone w `Zastosuj` i wyrzucane; zaimportowane rekordy zachowują Id słowników z pliku i przy scalaniu z lokalnymi wartościami tracą stronę, kategorię, miejsce.
   - Komunikat „konflikty rozstrzygnij na ekranie Pakiety” jest nieprawdziwy — `Zastosuj` leci od razu na domyślnych decyzjach.

8. **Zip-slip przy imporcie zdjęć.** `EksportRekonstrukcji.WyodrebnijZdjecia` (`:139-147`) składa ścieżkę z nazwy wpisu w zipie przez `Path.Combine` bez walidacji. Plik `.kwreko` jest przeznaczony do przekazywania innym osobom, więc wpis `zdjecia/../../x` lub ścieżka absolutna zapisze plik poza katalogiem danych.

9. **Ścieżki zdjęć z importowanych danych nie są walidowane.** `SerwisZdjec.PelnaSciezka` / `UsunPlik` (`:26-32`, `:150-157`) ufają `Zdjecia[]`. Dane z `..\..\coś` pozwalają usunąć dowolny plik, gdy użytkownik usunie „zdjęcie” z galerii; eksport czyta te same ścieżki do zipa.

10. **Zastosowanie szablonu konfiguracji niszczy słowniki.** `RekonstrukcjeViewModel.ZastosujSzablon` (`:40-41`) podmienia `_dane.Slowniki` na świeże, czyści `SzablonyKart` i remapuje tylko kategorie. Osierocone zostają Id: stron, miejsc, ról, napędów, stanów, pochodzenia, wzorów, rodzajów kosztów i zadań. Po zniknięciu `SzablonyKart` karty techniczne wszystkich replik przestają się wyświetlać. Własne wartości i aliasy użytkownika przepadają — bez potwierdzenia i bez kopii. Komentarz „dane o sprzęcie zostają” tego nie odzwierciedla.

11. **Wyprawy trzymają indeks wariantu, więc edycja zestawu przepisuje historię.** `Wyprawa.WybraneWarianty` mapuje slotId → **indeks** wariantu. `UsunWariant` / `UsunSlot` (`ZestawyViewModel.cs:171-200`) przesuwają indeksy lub usuwają slot; użycia przedmiotów, mundury i raporty wszystkich dawnych wypraw zmieniają się po cichu. Ta sama przyczyna: `OstatnieWarianty`, `IndeksWariantuDomyslnego` (nie jest korygowany po usunięciu), `SpakowaneKlucze`. Komentarz przy `UsunSlot` („tylko gdy nie ma wariantów”) nie ma pokrycia w kodzie.

12. **Import ogólny: nagłówek jako przedmiot + czytanie kolumny 0 dla niezmapowanych pól.**
    - `ImporterOgolny.PrzygotujPodglad` robi `Skip(wierszNaglowka)` z domyślnym 0, więc wiersz nagłówka też jest importowany; `SugerujMapowanie` z tym samym 0 traktuje wiersz 1 jako nagłówek. `ImportViewModel` nigdy nie podaje tego parametru.
    - `Pole()` (`:81-85`) robi `FirstOrDefault(...).Key` na słowniku. Gdy żadna kolumna nie jest zmapowana na dane pole, `Key` = domyślne 0, a warunek `kol >= 0` jest zawsze prawdziwy → `Komentarz`, `Strona`, `Wzór`, `Miejsce` itd. dostają tekst z kolumny 0.

13. **Raport „Umundurowanie” liczy źle** (`RaportyViewModel.cs:305-325`):
    - Kontener kompletu ma `KompletId == null`, więc trafia do pierwszej pętli jako samodzielny mundur i drugi raz do grupy „(komplet)” — komplet jest wylistowany dwa razy.
    - `uzycia = elementy.Sum(UzyciaPrzedmiotu)`, ale wszystkie elementy kompletu zwracają wspólny licznik → użycia mnożone przez liczbę elementów.
    - `koszt = Sum(KosztMunduru(e))`, a dla części `KosztMunduru` zwraca koszt całego kompletu → koszt też się mnoży.
    - Skutek: zawyżone wyprawy, procenty i sumy.

14. **Repliki znikają z listy pakowania i kosztu zestawu.** `Kalkulator.RozwinNaPrzedmioty` (`:141-156`) nie obsługuje `TypElementu.Replika` (zwraca pustą listę). `ElementyListyPakowania` przepuszcza wszystko przez to rozwinięcie, więc broń nigdy nie trafia na listę pakowania (FR-707), a `KosztZestawu` nie liczy replik (martwa gałąź `Replika` w `:269`). `ZakresKosztuZestawu` repliki wlicza, więc koszt domyślny może wypaść poza zakres min–max.

15. **Typ magazynka z arkusza „Repliki” nigdy się nie ustawia.** `ImporterRepliki.cs:115`: warunek `!replika.TypMagazynkaId?.Contains(w.Id) == true` dla `null` daje `false`, więc przypisanie nie zachodzi. Pole wypełnia się tylko z `Karta_techniczna` i tylko dla replik z kartą. Logika jest też błędna sama w sobie (`Contains` to szukanie podłańcucha w Id; model ma jedno pole, a kod zakłada wiele).

16. **Kreator dubluje szablony kart.** `KreatorViewModel.cs:93` robi `_dane.SzablonyKart.AddRange(...)`, a `NoweDane()` już dodało AEG i HPA, przy czym `_dane.Slowniki` jest podmieniane. Powstają cztery szablony; stare wskazują nieistniejące Id napędów, a import kart wybiera `FirstOrDefault(Nazwa == "AEG")`, czyli stary.

17. **Usuwanie replik i przedmiotów zostawia wiszące referencje.** `ReplikiViewModel.Usun` (`:288`), `WyposazenieViewModel.Usun` (`:269`), bez potwierdzenia. Zostają: `ReplikaGlownaId` w wyprawach, elementy zestawów, `DotyczyId` zadań, `KompletId`, `ZamontowanyNaId`, `ElementyId` konfiguracji. Wyprawy z wiszącym `ReplikaGlownaId` nie wchodzą do żadnej sumy w BR-02, więc łączna liczba wypraw spada, a `KontrolaSpojnosci` pokazuje fałszywą różnicę. Zadania usuniętej repliki zostają otwarte i liczą się jako awarie.

## Średnie — błędne wyniki, niespójności

18. **`Semantyka` kosztu i zadania nie jest ustawiana z wybranego rodzaju.** `DodajKoszt` (`ReplikiViewModel.cs:303`) i dodawanie zadania ustawiają tylko `RodzajId`; `Semantyka` zostaje domyślna (`Inne`, `DoZrobienia`). W warstwie UI nie znaleziono kodu mapującego rodzaj na semantykę, a komentarz przy `PozycjaKosztowa.Semantyka` obiecuje takie mapowanie „przy zapisie”. Ręcznie dodany „Zakup repliki” nie wchodzi do „Ceny oryginalnej”. `Kalkulator.OtwarteAwaria` liczy tylko po `Semantyka`, a `ReplikiViewModel` ma fallback po `RodzajId` — Pulpit i lista replik mogą pokazywać różne liczby awarii. `SlownikiViewModel.Dodaj` ma komentarz o nadawaniu semantyki, ale jej nie nadaje.

19. **Dwa liczniki użyć dają różne wyniki.** `Kalkulator.UzyciaPrzedmiotu` liczy wyprawę raz; `IndeksObliczeniowy` zwiększa licznik za każde wystąpienie. Ten sam przedmiot w dwóch slotach (albo Komplet + jego część) daje 2 w liście Wyposażenia i 1 w raportach, eksporcie i na Pulpicie.

20. **Odejmowanie od liczników historycznych (FR-807) jest nieodwracalne.** Usunięcie, przywrócenie lub edycja daty wyprawy sprzed dnia zerowego nie cofa ani nie powtarza korekty (`WyprawyViewModel.cs:180-204`, `:258-276`). Liczniki się rozjeżdżają.

21. **Zła „Średnia na miesiąc” w raporcie lat.** `RaportyViewModel.cs:370` dzieli sumę narastającą do danego roku przez liczbę miesięcy od pierwszej wyprawy do **dziś**.

22. **„Średnia” wypraw w raporcie Broń jest dzielona całkowitoliczbowo** (`:277`) i obcina ułamki.

23. **`ImportViewModel.ZastosujOgolny` robi kopię po imporcie, nie przed** (`:275-278`), wbrew FR-1107 i wbrew `Zastosuj`. Kopia zawiera już zaimportowane dane.

24. **Przywracanie z kopii jest niepełne i ryzykowne** (`UstawieniaViewModel.cs:143-176`): brak kopii przed nadpisaniem; wczytywany jest tylko `dane.json`, zdjęcia z kopii nie wracają; pozostałe ViewModele (listy, `StanAplikacji`) nie są odświeżane.

25. **Kopie zapasowe rosną bez ograniczeń.** Każdy import/pakiet kopiuje wszystkie zdjęcia, bez retencji. Dwie kopie w tej samej sekundzie zderzają się w `File.Copy` bez `overwrite` (katalog z dokładnością do sekundy, `MagazynJson.cs:142`) → wyjątek (patrz pkt 4).

26. **Nieaktualne Id aktywnej rekonstrukcji.** `OstatniaRekonstrukcjaId` wskazujące usuniętą rekonstrukcję zostaje w stanie (`MainWindowViewModel.cs:53`). Nagłówek pokazuje „Wszystkie”, ale listy są filtrowane po nieistniejącym Id.

27. **Wyszukiwanie zadań nie otwiera repliki.** `SzukanieViewModel.cs:54` ustawia `RekordId` na Id **zadania**, a `MainWindow.OtworzWynik` szuka po nim repliki — nic nie zostaje zaznaczone. Podobnie wyniki ukryte filtrem „sprzedane” lub aktywną rekonstrukcją otwierają ekran bez zaznaczenia.

28. **„Do uzupełnienia” → Potwierdź czyści wszystkie flagi naraz** (`DoUzupelnieniaViewModel.cs:122-139`): `ZnacznikZImportu`, `DoUzupelnienia`, `LicznikHistorycznyDoWeryfikacji`. Potwierdzenie samego licznika zamyka też szkic bez danych. Dla „Pole nieznane” potwierdzenie nic nie zmienia, a wpis wraca po odświeżeniu.

29. **Samo kliknięcie rekordu zdejmuje znacznik „z importu, do przeglądu”.** `ReplikiViewModel.Wybierz` robi to i zapisuje cały JSON (`:174-178`) — przeglądanie kasuje flagę bez akceptacji, a każde kliknięcie to synchroniczny zapis.

30. **Duplikowanie przedmiotu gubi pola.** `Duplikuj` / `DodajWiele` nie kopiują `RekonstrukcjeId`, `DataZakupu`, statusu. Kopia staje się „wspólna dla wszystkich rekonstrukcji”, a koszt zakupu dostaje dzisiejszą datę, co zaburza raport kosztów wg roku.

31. **Eksport rekonstrukcji bierze za dużo i za mało** (`EksportRekonstrukcji.cs:32-53`). Elementy „wspólne” i wyprawy powiązane przez wspólne repliki trafiają do paczki razem z danymi innych rekonstrukcji. Jednocześnie elementy innych rekonstrukcji, do których odwołują się zestawy, nie są dołączone (wiszące referencje). `Ustawienia` jest puste, więc roczne liczniki historyczne nie wędrują z eksportem.

32. **Porównanie sum kosztów na `decimal` bez tolerancji.** `ImporterRepliki.cs:173` (`skladniki.Sum() != kosztCalkowity`) porównuje liczby pochodzące z doubli Excela; szum typu `110.49000000000001` powoduje wpadnięcie w gałąź „formuła niezgodna” i utratę podziału zakup/ulepszenie.

33. **Paleta wykresów dla trybu „Systemowy”.** `UstawieniaViewModel.ZastosujMotyw` (`:113-129`) wybiera ciemną paletę dla wszystkiego poza `Motyw.Jasny`, także gdy system jest jasny. Przy starcie aplikacji aliasy kolorów w ogóle nie są ustawiane (`App.xaml.cs`). Może to łamać kontrast (NFR-08).

34. **Mapowanie kolumn w imporcie ogólnym nie odświeża UI.** `WierszMapowaniaKolumny` to zwykły POCO bez `INotifyPropertyChanged`; „Wczytaj mapowanie” (`ImportViewModel.cs:322-324`) zmienia dane, ale kontrolki zostają ze starym wyborem.

35. **Listy bez odświeżania (do sprawdzenia w XAML).** `SlownikiViewModel.Wartosci` i `RekonstrukcjeViewModel.Rekonstrukcje` zwracają tę samą instancję `List<T>`, a potem robią `OnPropertyChanged`; w WPF `ItemsSource` ustawiony na ten sam obiekt zwykle się nie odświeża. `MainWindowViewModel.RekonstrukcjeLista` nie jest nigdzie powiadamiane po dodaniu rekonstrukcji. Szablony XAML nie były czytane pod tym kątem — to hipoteza.

36. **Wyprawa po dodaniu może nie mieć wybranych wariantów (prawdopodobne).** `DodajWyprawe` (`WyprawyViewModel.cs:241`) tworzy nową `Wyprawa` z samym `ZestawId`; `UstawZestaw` uzupełniający `WybraneWarianty` jest wołany tylko z `SelectionChanged` combo (`WyprawyPage.xaml.cs:30`). Następna wyprawa z tym samym zestawem może mieć pusty skład → zero użyć i brak munduru.

37. **Kontrola spójności.** Komunikat OK ma na sztywno „(89 = 89 = 89 po imporcie)” (`RaportyViewModel.cs:514`) i wyświetla się dla każdego zbioru danych. `Kalkulator.KontrolaSpojnosci` liczy `wgReplik` dwa razy (`:385`, `:391`), a mundury rozpoznaje po nazwie kategorii zawierającej „mundur” (`:403`).

## Niskie — kruchość, drobiazgi

38. `Dopasuj` / `SlowaKluczowe` dopasowują słowa jako podłańcuchy („pas” trafia w „pasoszelki”, „m4” w „m43”); heurystyki ujednoznaczniające są zaszyte na sztywno (`ImporterWyposazenie.cs:160`, `:188`).
39. `RozwiazElement` (`ImporterUmundurowanieZestawy.cs:297-298`) usuwa słowa sezonowe zwykłym `Replace`, bez granic słów.
40. `ImporterWyposazenie`: nieznane kategoria/strona/miejsce/klasa są ciche (konflikt tylko dla wzoru); nieznana kategoria ląduje w pierwszej; zmienna `kategoria` (`:63`) jest martwa. `NazwaJestSzkicem` to stub zwracający `false`.
41. `kolKlasa = naglowek.Komorki.Count - 1` (`ImporterWyposazenie.cs:40`) opiera się na tym, że `CzytnikXlsx` dodaje dodatkową pustą komórkę (`c <= LastCellNum`); działa, dopóki układ pliku się nie zmieni.
42. `CzytnikXlsx.Data` ma fallback `DateOnly.TryParse` zależny od kultury (niejednoznaczne `03/04/2022`); `Liczba` czyta `1,234` jako 1.234; `XSSFWorkbook` nie jest `Dispose`'owany.
43. `CzytnikCsv` czyta linia po linii (pola z cudzysłowem i nowymi liniami się psują), zakłada UTF-8 (polskie CSV z Excela bywa w Windows-1250). `MagazynMapowan.Wczytaj` nie obsługuje uszkodzonego JSON-a.
44. Eksport CSV nie zabezpiecza przed formułami Excela (`=`, `+`, `-`, `@` na początku). W raporcie „Stan replik” `Liczba = -1` jest kodowane jako „ostatni test” bez daty (`:488`, `:566`).
45. `EksportujListePakowania` połyka każdy wyjątek (`ZestawyViewModel.cs:422`) — użytkownik nie dowie się o błędzie.
46. `SynchronizujCenaZakupu` rozpoznaje własną pozycję po tekście `Opis == "Cena zakupu"`; edycja ceny przy zaimportowanych kosztach (opis pusty) dokłada drugi zakup zamiast aktualizować pierwszy.
47. `Kalkulator.MundurWyprawy` dopasowuje sloty munduru po nazwie slotu zamiast po Id — zmiana nazwy slotu w szablonie po cichu odpina mundur.
48. `SerwisZdjec`: dialog dopuszcza `.webp`, którego WPF nie dekoduje; przy zmniejszaniu nieznane rozszerzenia dostają JPEG pod złym rozszerzeniem; `UsunPlik` nie usuwa kopii z `oryginaly/`; istnieją dwie wersje `PelnaSciezka` o różnych korzeniach.
49. `MainWindow.PrzejdzZPulpitu` ustawia dwie flagi filtrów, z których każda woła `Odswiez()`, a potem woła je jeszcze raz (trzy przebudowy `IndeksObliczeniowy`). Flagi `TylkoAwaria` / `TylkoBezTestu` zostają włączone po powrocie przez menu.
50. Niespójne sortowanie remisów w rankingu replik: Pulpit i Broń używają `ThenBy`, raport Broń nie — „Miejsce” może się różnić między ekranami.
51. Kosmetyka: `WynikKontroli.komunikat` z małej litery, `_ = kalk;` w `BudujRaportKosztow`, `SlownikiViewModel.ZapiszWartosc` bierze nową nazwę z pola „nowa wartość” bez sprawdzenia unikalności, `PoleImportu` zapisywane w JSON jako liczby.

## Proponowana kolejność napraw

**1, 2, 7, 8, 10, 6, 11, 5, 13, 14.** Pierwsze dwa to crashe bez możliwości odzysku, kolejne trzy — utrata lub psucie danych, a 6, 5, 13 i 14 dają bezpośrednio złe liczby w raportach i na listach.
