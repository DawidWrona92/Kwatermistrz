# Kwatermistrz – dokumentacja wymagań

Wersja: 0.2 (szkic do akceptacji; uwzględnia odpowiedzi autora z 2026-10-21)
Data: 2026-10-21
Nazwa robocza aplikacji: Kwatermistrz (do zmiany)
Dane źródłowe: `Dane_zrodlowe_Kwatermistrz.xlsx` (wyciąg z `Lista_Broni.numbers` i `Lista_Ekwipunku_Rosyjskiego.numbers`)

---

## 0. Jak korzystać z dokumentu

Dokument opisuje, co aplikacja ma robić i dlaczego. Nie narzuca architektury, struktury danych ani bibliotek. Wybór technologii należy do agenta kodującego (Claude Code, Codex), z wyjątkiem ograniczeń z rozdziału 11.

Priorytety wymagań:

- [M] musi być w wersji 1.0
- [S] powinno być w wersji 1.0, o ile nie wydłuża etapu
- [C] może poczekać na kolejną wersję

Identyfikatory: FR (funkcjonalne), BR (reguły biznesowe i obliczenia), NFR (niefunkcjonalne), UI (interfejs). Agent powinien się do nich odwoływać w commitach, testach i raportach z etapów.

Liczby z rozdziału 8 są testami akceptacyjnymi. Wynik obliczeń w aplikacji musi się z nimi zgadzać co do grosza. Definicje z rozdziału 8 są stałe i nie podlegają konfiguracji użytkownika (NFR-14).

Wszystko, co w rozdziale 14 jest oznaczone jako „do potwierdzenia”, agent zgłasza pytaniem przed implementacją danej funkcji. Nie zgaduje.

Kolejność czytania dla agenta: rozdziały 1–5 (kontekst), 6 (model), 7–9 (wymagania), 10 (wygląd), 11 (ograniczenia), 12 (etapy), 13–14 (dane i pytania), 15 (sposób pracy).

---

## 1. Cel i kontekst

Autor dokumentu zajmuje się rekonstrukcją współczesnych formacji (w arkuszach: strona rosyjska) oraz strzelaniami ASG. Sprzęt i historię wypraw prowadzi dziś w dwóch plikach Numbers, razem 13 arkuszy.

Aplikacja ma zastąpić te arkusze i robić trzy rzeczy:

1. Katalogować sprzęt: repliki ASG oraz wyposażenie (umundurowanie, kamizelki, pasy, ładownice i reszta), z miejscem przechowywania, stanem i zdjęciami.
2. Opisywać, jak sprzęt działa: karta techniczna repliki, pomiary mocy, historia modyfikacji, usterki i rzeczy do zrobienia.
3. Prowadzić dziennik wypraw, z którego automatycznie wynikają liczniki użycia sprzętu i koszt na wyprawę.

Dodatkowo aplikacja pozwala składać z katalogu zestawy (komplety na konkretną wyprawę) i pokazuje statystyki.

Koncepcja jest otwarta. Dane startowe dotyczą współczesnej rekonstrukcji rosyjskiej, ale aplikacja nie może być do niej przywiązana. Autor może w przyszłości zacząć kolejną rekonstrukcję (np. 101. Dywizja Powietrznodesantowa) z zupełnie innymi kategoriami sprzętu i innym rodzajem replik. Program ma to umożliwić bez zmian w kodzie: wszystko, co dziś wynika z arkuszy (kategorie, sloty zestawu, parametry karty technicznej, słowniki, strony), jest konfiguracją, a nie treścią programu. Patrz 7.13 i NFR-13.

Aplikacja ma służyć także kolegom autora, którzy rekonstruują inne jednostki. Każdy używa własnej instalacji z własnymi danymi, a założenia i wyliczenia są wspólne i takie same u wszystkich (NFR-14), żeby wyniki dało się porównywać. Dane z arkuszy autora są dawne i przykładowe: import ma dać punkt wyjścia do przeglądu i aktualizacji, a nie źródło prawdy (rozdział 13). Patrz 7.14.

Problemy arkuszy, które aplikacja ma rozwiązać:

- Wyprawa nie istnieje jako zapis. Są tylko liczniki wpisywane ręcznie, które się rozjeżdżają (przykład: mundur SSO w kamuflażu Spektr ma 2 użycia w arkuszu Mundury i 4 w tabeli umundurowania).
- Koszt repliki to działanie wpisane w komórce, np. `960+112+64+100+301.12+90+1050+620`, bez opisu składników.
- Karta techniczna pokazuje tylko stan bieżący. Nie wiadomo, co i kiedy zmieniono.
- Zestawy to warianty zapisane w jednej komórce (`6b45/6b45+6sh117/6b46`), bez powiązania z katalogiem.
- Powiązania między rzeczami (ładownica na kamizelce) istnieją tylko jako dopisek w komentarzu.
- Brakujące dane to znaczniki `???`, `??`, `Nie dotyczy HPA`, nieodróżnialne od zwykłego tekstu.
- Nazwy nie są ujednolicone (`ATACS`, `A-TACS`, `ATACS-FG`; `LKM` i `WSPARCIE`; magazynki `AK`, `AK 74M`, `AK 12`).

---

## 2. Zakres

W zakresie wersji 1.0:

- aplikacja desktopowa dla jednego użytkownika, działająca w całości lokalnie,
- obsługa wielu rekonstrukcji w jednej aplikacji, bez wiązania programu z jednym rodzajem sprzętu (7.13),
- użycie przez wielu niezależnych użytkowników (autor i koledzy z innych jednostek), każdy z własną instalacją i własnymi danymi, z wymianą konfiguracji przez jawny eksport i import (7.14),
- katalog wyposażenia i replik, karta techniczna, pomiary, modyfikacje, koszty, zadania,
- zestawy i dziennik wypraw,
- liczniki, statystyki i raporty z eksportem,
- jednorazowy import danych z arkuszy oraz późniejszy import powtarzalny,
- kopie zapasowe i przywracanie.

Poza zakresem:

- synchronizacja w chmurze, praca na wielu urządzeniach, wiele kont,
- wersja mobilna i przeglądarkowa (możliwa przyszła aplikacja do szybkiego zapisu wyprawy w terenie),
- integracje z zewnętrznymi serwisami (sklepy, ogłoszenia, mapy),
- rozpoznawanie obrazu i automatyczne opisywanie zdjęć,
- księgowość inna niż koszty sprzętu,
- współdzielenie danych z innymi osobami.

---

## 3. Użytkownik i scenariusze

Użytkownicy: autor (zaawansowany hobbysta, programista, Windows) oraz koledzy rekonstruktorzy innych jednostek, o różnej biegłości technicznej i z innym sprzętem. Każdy ma własną instalację i własne dane. Wprowadzają dane głównie w domu przy komputerze, po wyprawie lub po serwisie. Znają własne nazewnictwo i nie chcą go zmieniać, aplikacja ma je przejąć (u autora: Piwnica, Dom, szturm, DMR, Hop-Up, MOSFET).

Scenariusze, według których będą oceniane wymagania:

- S1 Po wyprawie. Dodaje wyprawę w mniej niż 30 sekund: data, replika główna, zestaw. Liczniki zmieniają się same.
- S2 Przed wyprawą. Wybiera zestaw i widzi listę pakowania z podziałem na miejsca przechowywania (Piwnica, Dom).
- S3 Po serwisie. Zapisuje modyfikację repliki (wymieniona część, koszt). Karta techniczna aktualizuje się, historia zostaje.
- S4 Po pomiarach na chronografie. Dodaje pomiar mocy i zasięgu, widzi datę ostatniego testu i trend.
- S5 Zakup. Dodaje przedmiot z ceną, miejscem, wzorem kamuflażu, zdjęciem, od razu przypina go do kamizelki lub pasa.
- S6 Przegląd. Odpowiada na pytania: czego nie używam, ile kosztuje mnie wyprawa na danej replice, w jakim umundurowaniu chodzę najczęściej.
- S7 Sprzedaż. Oznacza replikę jako sprzedaną. Znika z codziennych list i kosztów bieżących, ale jej wyprawy zostają w statystykach.
- S8 Uzupełnianie luk. Widzi listę pól oznaczonych jako nieznane i uzupełnia je po kolei.
- S9 Pierwsze uruchomienie u kolegi. Dostaje pustą aplikację, tworzy swoją rekonstrukcję, wybiera pusty szablon albo szablon od autora i zaczyna wpisywać własny sprzęt, bez śladów danych autora.
- S10 Wymiana konfiguracji. Autor eksportuje pakiet konfiguracji (kategorie, sloty, szablony karty), kolega go importuje i ma gotowy szkielet pod swoją jednostkę.
- S11 Aktualizacja starych danych. Po imporcie z arkuszy przegląda rekordy oznaczone „z importu, do przeglądu”, poprawia je pojedynczo lub zbiorczo.

---

## 4. Słownik pojęć

Terminy poniżej obowiązują w całej aplikacji (etykiety interfejsu) i w kodzie (odpowiedniki angielskie dobiera agent konsekwentnie).

| Pojęcie | Znaczenie |
|---|---|
| Wyprawa | Pojedynczy wyjazd na strzelanie lub rekonstrukcję. W arkuszach: „strzelanka”. |
| Replika | Replika ASG (karabin, pistolet). W menu: „Bronie”. |
| Przedmiot | Każda rzecz z katalogu wyposażenia, która nie jest repliką. |
| Kategoria | Rodzaj przedmiotu: czapki, hełmy, mundury, ładownice itd. Lista edytowalna. |
| Nośnik | Przedmiot, na którym coś się mocuje: kamizelka, pasoszelki, chest rig, pas, plecak, panel szturmowy. |
| Element mocowany | Przedmiot zamontowany na nośniku, np. ładownica na kamizelce. |
| Komplet | Kilka przedmiotów traktowanych jako jedna pozycja użycia, np. bluza i spodnie. |
| Zestaw | Nazwany układ sprzętu na wyprawę (np. „Gostomel”, „Saper”), złożony ze slotów. |
| Slot | Miejsce w zestawie: mundur, hełm, kamizelka, pas, broń szturmowa itd. |
| Wariant | Jedna z dopuszczalnych zawartości slotu, np. „6b45 + 6sh117”. |
| Karta techniczna | Zestaw parametrów repliki (lufa, gumka hop-up, silnik, MOSFET itd.). |
| Pomiar / test | Wpis o sprawdzeniu repliki: data, energia, zasięg, wynik. |
| Modyfikacja | Zapisana zmiana w replice: co wymieniono, ile kosztowało. |
| Pozycja kosztowa | Pojedynczy wydatek lub zwrot związany z repliką lub przedmiotem. |
| Wzór kamuflażu | Wpis słownikowy: EMR, Multicam, A-TACS, Flecktarn itd. albo inny wzór, kolor lub materiał, z próbnikiem koloru. |
| Strona | Rosja, Polska, NATO (lista edytowalna). Przedmiot lub replika może mieć kilka stron. |
| Rola | Przeznaczenie repliki: szturm, DMR, wsparcie, CQB, boczna. W arkuszach: „Teren”. |
| Napęd | Rodzaj napędu repliki: elektryczna (AEG) albo HPA. |
| Licznik historyczny | Liczba użyć sprzed używania aplikacji, przeniesiona z arkuszy. |
| Dzień zerowy | Data, od której użycia liczy aplikacja; wcześniejsze są w licznikach historycznych. |
| Stan | Ocena replik: Stock, Do zrobienia, Do dokończenia, Ideał, Ideał – sprawdzony w boju, Wyśmienity, W częściach. |
| Rekonstrukcja | Profil odtwarzanej formacji lub epoki (np. „Rosja współczesna”, w przyszłości „101. Dywizja Powietrznodesantowa”). Grupuje przedmioty, repliki, zestawy i wyprawy. |
| Energia docelowa | Idealna, maksymalna energia repliki w J. Pomiar, który ją osiąga, ma oznaczenie „na maksimum” (w arkuszach: serduszko). |
| Pakiet konfiguracji | Plik z konfiguracją rekonstrukcji (kategorie, sloty, szablony karty, słowniki) bez żadnych danych o sprzęcie; służy do wymiany między użytkownikami. |
| Dane przykładowe | Dane z arkuszy autora; dawne, służą do próbnego importu i do testów, nie są częścią aplikacji. |
| Pochodzenie / klasa | Autentyczność i klasa wykonania: Orgo/Reko (faktyczny, oryginalny sprzęt wojskowy do rekonstrukcji, nie reprodukcja ani podróbka), Wojskowe, ASG niskiej jakości (kolumna bez nagłówka w arkuszach). |

---

## 5. Stan wyjściowy: co dziś jest w arkuszach

### 5.1 Lista_Broni.numbers

Arkusz „Lista broni i ekwipunku” zawiera cztery bloki.

Blok replik: 18 replik aktualnych. Kolumny: numer (kolejność wg liczby wypraw), nazwa w postaci „model producenta (odwzorowywany prototyp)”, liczba wypraw, typ magazynka, teren (rola), procent wszystkich wypraw, strona (NATO/UW), koszt całkowity, oryginalna cena, koszt dodatków, koszt na wyprawę. Pod tabelą średnie i sumy.

Blok umundurowania („Szpej”): 20 pozycji (mundur lub komplet, np. „Flecktarn”, „WZ. 93”, „Sumrak ATACS-FG”, „6Sh122”), każda z liczbą wypraw, procentem, kosztem i kosztem na wyprawę. Sumy dla trzech stron (NATO, POLSKA, ROSJA). Dodatkowo trzy wcześniejsze pozycje EMR (8 wypraw, bez kosztu).

Blok sprzedanych replik: 9 pozycji, razem 31 wypraw. Wyprawy sprzedanych replik wchodzą do łącznej liczby wypraw.

Blok wypraw w czasie: data pierwszej wyprawy (2022-09-01) i liczby wypraw wg lat: 10, 30, 17, 19, 13, razem 89.

Arkusz „Status i części replik” to macierz: kolumny to repliki (23 kolumny, z czego 2 nazwy powtórzone), wiersze to parametry. Parametry: Stan, Body, Lufa, Gumka Hop-Up, Komora Hop-Up, Doszczelnienie, Podkładkowanie, Przewody, Łożyska, Zębatki, Prowadnica sprężyny, Tłok, Głowica tłoka, Cylinder, Głowica cylindra, Dysza, Silnik, MOSFET, Wtyczka, Sprężyna główna (aktualna), Kulki docelowe, Joule na docelowych kulkach i hopie, Zasięg, Testowane działanie (data), Teren, Typ magazynków, a pod spodem dwa wiersze notatek „do zrobienia”. Kolumny są pogrupowane na ROSJA, POLSKA, NATO.

### 5.2 Lista_Ekwipunku_Rosyjskiego.numbers

Dziesięć arkuszy kategorii (łącznie 106 pozycji) i arkusz zestawów.

| Arkusz | Pozycji | Uwagi |
|---|---|---|
| Czapki | 4 | |
| Hełmy | 3 | |
| Szalokominiarki | 4 | |
| Mundury | 15 | część pozycji tworzy komplet ze wspólnym licznikiem (bluza + spodnie) |
| Kamizelki | 8 | |
| Pasoszelki i Chest Rigi | 3 | |
| Pasy | 4 | kolumna bez nagłówka: „Wojskowe”, „ASG Niskiej Jakości” |
| Nakolanniki | 2 | kolumna bez nagłówka: „Orgo/Reko”, „Wojskowe” |
| Plecaki | 3 | w tym 1 panel szturmowy (podsekcja) |
| Ładownice | 60 | komentarz wskazuje nośnik, na którym wisi (np. „6b46”, „Pas MBC”, „A-18 Skanda”) |
| Sety | 15 kolumn | 3 bez nazwy, 9 nazwanych, 3 szkice zawierające tylko pas |

Wspólne kolumny kategorii: Lp, Firma (producent), Nazwa, strona (kolumna bez nagłówka, wszędzie „ROSJA”), Kolor (wzór kamuflażu), Gdzie (Piwnica albo Dom), Liczba użyć (tylko część arkuszy), Komentarze.

Arkusz „Sety” to macierz: kolumny to zestawy, wiersze to sloty (mundur, nakolanniki, kominiarka, rękawice, hełm, czapka, gogle, buty, kamizelka, plecak, pas, broń DMR, broń LAS, broń wsparcia, broń boczna, zdjęcie). W komórkach są warianty rozdzielone ukośnikiem, znak plus łączy elementy użyte razem, `PUSTE` oznacza świadomy brak elementu. Wiersz „Zdjęcie” jest pusty we wszystkich zestawach.

### 5.3 Jak arkusze liczą (wyciągnięte z formuł)

- Koszt repliki to suma składników wpisana jako działanie, np. `960+112+64+100+301.12+90+1050+620`. Pierwszy składnik to zwykle cena zakupu, kolejne to części i usługi. Opisów składników nie ma. W jednym przypadku są składniki ujemne (`626-100-35+76+252+257` dla E&L ELMS: rabat lub sprzedaż części).
- Koszt dodatków = Koszt − Oryginalna cena.
- Liczba wypraw repliki jest wpisywana ręcznie. Łączna liczba wypraw = wyprawy replik aktualnych + wyprawy replik sprzedanych = 58 + 31 = 89. Każda wyprawa jest liczona raz, dla jednej repliki głównej.
- Procent wypraw = wyprawy repliki / 89 (sprzedane są w mianowniku).
- Koszt na wyprawę = koszt / wyprawy; gdy wypraw jest 0, wynik to pełny koszt (efekt zabezpieczenia przed dzieleniem przez zero).
- Średnie i sumy kosztów dotyczą tylko 18 replik aktualnych.
- Wypraw w roku bieżącym nie wpisano; jest to różnica: 89 minus suma lat wcześniejszych.
- W arkuszach wyposażenia „Liczba użyć” jest wpisywana ręcznie; sumy istnieją tylko dla Mundurów (29), Kamizelek (19) i Pasoszelek (4). Według autora liczniki użyć mundurów dawno przestały być aktualne; aktualna jest tabela umundurowania w `Lista_Broni` (sumuje się do 89 wypraw).

### 5.4 Wnioski dla aplikacji

1. Wyprawa musi stać się zapisem (data, replika, zestaw). Liczniki mają być wyliczane, nigdy wpisywane.
2. Koszt musi być zbiorem pozycji z opisem, a suma i podział na „cenę oryginalną” i „dodatki” mają wynikać z rodzaju pozycji.
3. Zmiany części muszą zostawiać historię, a karta techniczna pokazuje stan bieżący.
4. Montaż (ładownica na kamizelce, kamizelka w konfiguracji „Skanda Light”) jest relacją w danych, a nie komentarzem.
5. Zestaw to sloty z wariantami wskazującymi na katalog.
6. Brak danych ma trzy stany: wypełnione, nieznane, nie dotyczy.
7. Wartości powtarzalne (strona, wzór kamuflażu, typ magazynka, miejsce, stan) pochodzą ze słowników z możliwością scalania.
8. Dane z lat sprzed aplikacji zostają przeniesione jako liczniki historyczne. Nie wolno wymyślać z nich pojedynczych wypraw, bo arkusze mają tylko sumy.

---

## 6. Model informacji (pojęciowy)

Opis logiczny, bez struktury bazy. Agent projektuje przechowywanie samodzielnie.

### 6.1 Przedmiot

- kategoria (wymagana), firma (producent), nazwa, wzór kamuflażu, strony (jedna lub więcej)
- miejsce przechowywania (słownik), pochodzenie / klasa (słownik)
- cena zakupu i data zakupu (opcjonalne), pozycje kosztowe (opcjonalne, patrz 6.4)
- status: posiadany, do sprzedaży, sprzedany (data, cena opcjonalne), wycofany
- rekonstrukcje (zero lub więcej; brak przypisania oznacza przedmiot wspólny)
- licznik historyczny użyć, komentarz, zdjęcia
- zamontowany na (jeden nośnik albo brak), przynależność do kompletu (opcjonalnie)
- znacznik „do uzupełnienia” (przedmiot utworzony z samej nazwy w zestawie lub w imporcie)

### 6.2 Replika

- model producenta, odwzorowywany prototyp, strony, role, typ magazynka, napęd, stan, energia docelowa (J), komentarz
- status: posiadana, do sprzedaży, sprzedana (data, cena opcjonalne), wycofana
- rekonstrukcje (zero lub więcej)
- licznik historyczny wypraw (jako replika główna)
- karta techniczna (parametry z trzema stanami wartości)
- pomiary i testy, modyfikacje, pozycje kosztowe, zadania i usterki, zdjęcia

### 6.3 Zestaw, slot, wariant

- zestaw: nazwa (może być pusta, wtedy etykieta z munduru), rekonstrukcja, strona, opis (sezon, scenariusz), status aktywny albo szkic, zdjęcia, wariant domyślny każdego slotu
- slot: nazwa i kolejność; lista slotów jest edytowalna
- wariant slotu: jeden lub kilka elementów (przedmioty albo repliki) połączonych znakiem „+”, albo jawne „puste”

### 6.4 Pozycja kosztowa, modyfikacja, pomiar, zadanie

- pozycja kosztowa: data, opis, kwota (może być ujemna), rodzaj (zakup repliki, ulepszenie, serwis, zwrot/rabat/sprzedaż części, inne), źródło
- modyfikacja: data, opis, zmiany parametrów karty (parametr, wartość przed, wartość po), powiązane pozycje kosztowe
- pomiar / test: data, masa kulki, energia (J), szybkostrzelność (ROF), ustawienie hop-up lub opis, zasięg (m), wynik (sprawna / usterka), komentarz, znacznik „na maksimum”
- zadanie: dotyczy repliki, przedmiotu albo niczego konkretnego; rodzaj (awaria, do zrobienia, do kupienia); opis; status; daty

### 6.5 Wyprawa

- data (wymagana), rekonstrukcja (wynika z zestawu albo aktywnej rekonstrukcji), nazwa lub miejsce (opcjonalne), replika główna (wymagana), zestaw (opcjonalny), skład (konkretne warianty i elementy użyte tego dnia), komentarz, zdjęcia
- wpis historyczny (sprzed dnia zerowego) nie jest wyprawą, tylko licznikiem; patrz FR-806

### 6.6 Rekonstrukcja (profil)

- nazwa (np. „Rosja współczesna”), okres lub era, strona (opcjonalnie), opis, zdjęcie
- szablon slotów zestawu (lista slotów danej rekonstrukcji)
- przypisane przedmioty, repliki i zestawy (wiele do wielu; brak przypisania oznacza element wspólny)
- opcjonalna dokumentacja odwzorowania (FR-712)

### 6.7 Relacje (skrót)

```
Kategoria 1──* Przedmiot *──0..1 Wzór kamuflażu
Przedmiot *──0..1 Nośnik (inny Przedmiot)
Komplet 1──* Przedmiot
Replika 1──* Pozycja kosztowa | Modyfikacja | Pomiar | Zadanie
Zestaw 1──* Slot 1──* Wariant *──* (Przedmiot | Replika)
Wyprawa *──1 Replika główna
Wyprawa *──0..1 Zestaw
Wyprawa 1──* Użyty element (Przedmiot | Replika)
Rekonstrukcja *──* Przedmiot | Replika | Zestaw
Rekonstrukcja 1──* Wyprawa
```

---

## 7. Wymagania funkcjonalne

Zapis: identyfikator, priorytet w nawiasie, treść. Kryteria akceptacji podano tam, gdzie nie wynikają wprost z treści.

### 7.1 Pulpit

- FR-101 [M] Ekran startowy pokazuje: liczbę wypraw łącznie, liczbę w bieżącym roku, datę pierwszej i ostatniej wyprawy, liczbę dni od ostatniej.
- FR-102 [M] Mozaika wypraw: każda wyprawa to kafelek w kolorze wzoru kamuflażu munduru użytego tego dnia, ułożony chronologicznie, w wierszach po latach. Wyprawy bez znanego munduru mają kafelek neutralny. Najechanie pokazuje datę, replikę i zestaw, kliknięcie otwiera wyprawę. Wpisy historyczne (liczniki) są pokazane jako kafelki neutralne z oznaczeniem „bez daty”, w liczbie zgodnej z licznikiem roku.
- FR-103 [M] Przycisk „Dodaj wyprawę” jest widoczny na każdym ekranie, ma skrót klawiaturowy.
- FR-104 [S] Karty skrótowe z liczbą i przejściem do listy: otwarte awarie, otwarte zadania, repliki bez testu dłużej niż próg (domyślnie 180 dni, zmienny w ustawieniach), przedmioty bez żadnego użycia, pola „nieznane” do uzupełnienia.
- FR-105 [S] Ranking pięciu replik z największą liczbą wypraw, z kosztem na wyprawę.

### 7.2 Katalog wyposażenia

- FR-201 [M] Nawigacja po kategoriach z licznikami pozycji. Kategorie startowe: Czapki, Hełmy, Szalokominiarki, Mundury, Kamizelki, Pasoszelki i chest rigi, Pasy, Nakolanniki, Plecaki, Panele szturmowe, Ładownice, a dodatkowo Rękawice, Gogle, Buty (występują w zestawach, nie mają jeszcze arkuszy).
- FR-202 [M] Użytkownik dodaje, zmienia nazwę i usuwa kategorie (usunięcie tylko pustej). Kategorię można oznaczyć jako „nośnik” (kamizelki, pasoszelki, pasy, plecaki, panele szturmowe).
- FR-203 [M] Lista przedmiotów kategorii jako gęsta tabela: próbnik wzoru, firma, nazwa, wzór, strona, miejsce, użycia, komentarz. Sortowanie po każdej kolumnie, filtry (strona, wzór, miejsce, status, pochodzenie), wyszukiwanie tekstowe, wybór widocznych kolumn zapamiętywany.
- FR-204 [M] Karta przedmiotu zawiera pola z 6.1, w tym przypisanie do rekonstrukcji (FR-1302). Zmiany zapisują się po zatwierdzeniu formularza; walidacja wymaga tylko kategorii i nazwy lub firmy.
- FR-205 [M] „Dodaj N takich samych” oraz „Duplikuj”: tworzą osobne rekordy, bo każda sztuka ma własne miejsce montażu i własną historię użycia. Podstawa: 60 ładownic, wiele identycznych pozycji typu „Technikom, 2 x AK, EMR”.
- FR-206 [M] Komplet: przedmioty (np. bluza i spodnie) można połączyć w komplet. Komplet jest wybierany w zestawie jako jedna pozycja i ma wspólny licznik użyć, ale każdy element zachowuje własną kartę i cenę.
- FR-207 [M] Montaż: przedmiot może być zamontowany na jednym nośniku (lista nośników filtrowana do kategorii oznaczonych jako nośnik). Karta nośnika pokazuje listę zamontowanych elementów, z możliwością dodania i zdjęcia elementu jednym działaniem. Przeniesienie elementu na inny nośnik nadpisuje poprzedni montaż.
- FR-208 [S] Konfiguracje nośnika: nośnik może mieć nazwane konfiguracje (np. „Skanda Light”, „Skanda Heavy”), każda jako lista elementów z możliwością odwołania się do niej jako do wariantu w zestawie.
- FR-209 [M] Status przedmiotu: posiadany, do sprzedaży, sprzedany (data, cena opcjonalne), wycofany. „Do sprzedaży” zostaje widoczne z oznaczeniem. Sprzedane i wycofane są domyślnie ukryte (przełącznik „Pokaż sprzedane”), zostają w statystykach historycznych.
- FR-210 [M] Pole „Pochodzenie / klasa” ze słownikiem edytowalnym; wartości startowe w Załączniku B. „Orgo/Reko” oznacza faktyczny, oryginalny sprzęt wojskowy do rekonstrukcji, a nie reprodukcję ani podróbkę.
- FR-211 [M] Zdjęcia: wiele na przedmiot, miniatura w liście [S], powiększenie, zdjęcie główne.
- FR-212 [S] Widok „Gdzie to jest”: przedmioty pogrupowane wg miejsca przechowywania, z grupową zmianą miejsca dla zaznaczonych pozycji.
- FR-213 [S] Widok „Nieużywane”: przedmioty o zerowym łącznym użyciu (historyczne + z wypraw), z sortowaniem po cenie.
- FR-214 [C] Pola własne definiowane per kategoria (np. pojemność ładownicy, rozmiar).
- FR-215 [C] Kategorie ilościowe (magazynki, akumulatory): jedna pozycja z liczbą sztuk zamiast osobnych rekordów, z typem magazynka ze słownika. Powód: w arkuszu pojawia się potrzeba zakupu magazynków, a same magazynki nie są katalogowane.
- FR-216 [M] Edycja zbiorcza: zaznaczone pozycje (przedmioty, repliki, zestawy) można zmienić jednym poleceniem w polach wspólnych: kategoria, miejsce, rekonstrukcja, strona, wzór, pochodzenie / klasa, status. Powód: dane z importu są dawne i wymagają masowych poprawek.

### 7.3 Bronie (repliki)

- FR-301 [M] Lista replik: miejsce w rankingu wg wypraw, nazwa w postaci „model (prototyp)”, strona, rola, typ magazynka, napęd, stan, wyprawy, procent wypraw, koszt całkowity, cena oryginalna, dodatki, koszt na wyprawę, data ostatniego testu. Sortowanie po każdej kolumnie, filtry (strona, rola, stan, napęd, otwarta awaria), grupowanie wg strony.
- FR-302 [M] Karta repliki z zakładkami: Przegląd, Karta techniczna, Pomiary, Modyfikacje i koszty, Zadania, Wyprawy, Zdjęcia.
- FR-303 [M] Pola podstawowe: model producenta, odwzorowywany prototyp (osobne pola, wyświetlane razem), strony (wiele), role (wiele), typ magazynka, napęd, stan, energia docelowa (J), rekonstrukcje (wiele), komentarz.
- FR-304 [M] Przegląd repliki pokazuje: stan, wyprawy i procent, koszt całkowity z podziałem na cenę oryginalną i dodatki, koszt na wyprawę, ostatni pomiar (z oznaczeniem „na maksimum”), datę ostatniego testu, otwarte zadania, ostatnie trzy wyprawy.
- FR-305 [M] Status własności repliki: posiadana, do sprzedaży, sprzedana (data i cena opcjonalne), wycofana. Replika „do sprzedaży” (np. rozebrana, czekająca na uporządkowanie) zostaje na liście z oznaczeniem, zachowuje zadania i wyprawy, ale jest wyłączona ze średnich i sum kosztów bieżących. Replika sprzedana znika z domyślnej listy i z doboru przy dodawaniu wypraw (FR-805). Wyprawy replik „do sprzedaży” i sprzedanych zostają w statystykach (BR-02, BR-06).
- FR-306 [S] Aplikacja podpowiada stan „Ideał – sprawdzony w boju”, gdy po ostatniej modyfikacji odbyła się co najmniej jedna wyprawa na tej replice. Podpowiedź wymaga potwierdzenia, nie zmienia stanu sama (do potwierdzenia, Q11).
- FR-307 [M] Przy dodawaniu repliki typ magazynka, rola, strona i napęd wybiera się ze słowników; nowe wartości można dodać w locie.

### 7.4 Karta techniczna i pomiary

- FR-401 [M] Karta techniczna ma parametry w kolejności z arkusza: Body, Lufa, Gumka Hop-Up, Komora Hop-Up, Doszczelnienie, Podkładkowanie, Przewody, Łożyska, Zębatki, Prowadnica sprężyny, Tłok, Głowica tłoka, Cylinder, Głowica cylindra, Dysza, Silnik, MOSFET, Wtyczka, Sprężyna główna (aktualna), Kulki docelowe.
- FR-402 [M] Lista parametrów jest edytowalna: dodawanie, zmiana nazwy, zmiana kolejności, ukrywanie. Proponowane parametry dodatkowe, domyślnie ukryte: Długość lufy, Szybkostrzelność (ROF), Akumulator.
- FR-403 [M] Szablon karty zależy od napędu i jest edytowalny. Dla HPA parametry mechanizmu elektrycznego (zębatki, łożyska, tłok, cylinder, silnik itd.) mają stan „nie dotyczy”, co odpowiada „Nie dotyczy HPA” z arkusza. Użytkownik definiuje własne napędy i szablony kart (FR-1307).
- FR-404 [M] Każde pole ma trzy stany: wypełnione, nieznane, nie dotyczy. „Nieznane” zastępuje `???`, `??`, `?` i jest wyraźnie wyróżnione w interfejsie. Pole puste, którego nikt nie oznaczył, jest czwartym, neutralnym stanem „nie uzupełniono”.
- FR-405 [M] Wartości tekstowe pól są podpowiadane z wcześniej użytych w tym samym parametrze, żeby ograniczyć rozjazdy zapisu („Specna Arms” kontra „Speca Arms”).
- FR-406 [S] Porównanie kart dwóch lub więcej replik obok siebie, z podświetleniem różnic.
- FR-407 [S] „Kopiuj kartę z innej repliki” jako punkt wyjścia dla nowej repliki.
- FR-408 [M] Pomiar / test: data (wymagana), masa kulki, energia w J, szybkostrzelność (ROF), opis ustawienia hop-up, zasięg w m, wynik (sprawna / usterka), komentarz, znacznik „na maksimum” (w arkuszu serduszko: pomiar osiągnął idealną, maksymalną wartość energii dla tej repliki). Wszystkie pola poza datą opcjonalne, żeby dało się zapisać sam test działania bez pomiaru.
- FR-409 [M] Dla repliki aplikacja pokazuje „Testowane działanie” jako datę najnowszego wpisu pomiaru lub testu oraz ostatnie wartości energii i zasięgu.
- FR-410 [S] Wykres energii w czasie dla repliki z linią energii docelowej.
- FR-411 [S] Replika ma energię docelową (maksymalną) w J. Aplikacja podpowiada znacznik „na maksimum”, gdy energia pomiaru jest równa docelowej lub ją osiąga w granicach tolerancji z ustawień (domyślnie 0,05 J), i ostrzega, gdy pomiar przekracza energię docelową o więcej niż tolerancja. Przy imporcie pomiar z serduszkiem ustawia energię docelową repliki na zmierzoną wartość, oznacza pomiar „na maksimum” i zapisuje go jako ostatni.

### 7.5 Koszty i modyfikacje

- FR-501 [M] Pozycje kosztowe dla repliki i dla przedmiotu: data, opis, kwota w PLN (także ujemna), rodzaj, źródło. Rodzaje startowe: Zakup repliki, Ulepszenie, Serwis, Zwrot / rabat / sprzedaż części, Inne.
- FR-502 [M] Koszt całkowity = suma wszystkich pozycji. Cena oryginalna = suma pozycji rodzaju „Zakup repliki”. Koszt dodatków = koszt całkowity − cena oryginalna.
- FR-503 [M] Przedmiot ma uproszczone pole „cena zakupu” (tworzy jedną pozycję kosztową). Dodatkowe pozycje można dopisać [S].
- FR-504 [M] Modyfikacja łączy: datę, opis, listę zmian parametrów karty (parametr, wartość przed, wartość po) oraz powiązane pozycje kosztowe. Zapis modyfikacji aktualizuje kartę techniczną i dopisuje wpis do historii.
- FR-505 [M] Historia modyfikacji repliki jako oś czasu, od najnowszej.
- FR-506 [S] Szybka poprawka pola karty bez wpisu do historii (literówka), z opcją „zapisz jako modyfikację”.
- FR-507 [C] Podgląd karty technicznej z wybranego dnia (odtworzenie z historii modyfikacji).
- FR-508 [M] Przy imporcie składniki z formuł kosztu (np. `960+112+64`) stają się pozycjami bez opisu („Pozycja z arkusza 1 z 8” itd.). Pierwsza ma rodzaj „Zakup repliki”, kolejne „Ulepszenie”, ujemne „Zwrot / rabat / sprzedaż części”. Widok „Do uzupełnienia” prowadzi użytkownika po pozycjach bez opisu.

### 7.6 Zadania i usterki

- FR-601 [M] Zadanie: dotyczy repliki, przedmiotu lub niczego konkretnego; rodzaj (awaria, do zrobienia, do kupienia); opis; status (otwarte, zamknięte); daty utworzenia i zamknięcia.
- FR-602 [M] Lista wszystkich otwartych zadań z filtrami (rodzaj, replika, kategoria). Replika z otwartą awarią jest oznaczona na liście replik.
- FR-603 [S] Zamknięcie zadania „do kupienia” proponuje utworzenie pozycji kosztowej, zamknięcie awarii proponuje utworzenie modyfikacji.
- FR-604 [M] Dwa wiersze notatek z arkusza stają się zadaniami. Rodzaj jest podpowiadany słowami kluczowymi (Awaria, Złamana → awaria; Kupić → do kupienia; reszta → do zrobienia) i poprawiany w podglądzie importu.

### 7.7 Zestawy

- FR-701 [M] Zestaw: nazwa (w imporcie może być pusta, wtedy aplikacja podpowiada etykietę z nazwy munduru), rekonstrukcja, strona, opis (sezon, scenariusz), status (aktywny, szkic), zdjęcia.
- FR-702 [M] Sloty pochodzą z szablonu rekonstrukcji (FR-1301). Sloty startowe rekonstrukcji z importu: Mundur, Nakolanniki, Kominiarka, Rękawice, Hełm, Czapka, Gogle, Buty, Kamizelka, Plecak, Pas, Broń DMR, Broń szturmowa (w arkuszu „LAS”, Q3), Broń wsparcia, Broń boczna. Lista slotów jest edytowalna (np. dodanie „Radio”). Szablon oznacza, które sloty są slotami munduru (domyślnie Mundur) i które slotami broni głównej (domyślnie Broń szturmowa, Broń DMR, Broń wsparcia); od tych oznaczeń zależą FR-802 i BR-09.
- FR-703 [M] Slot ma zero lub więcej wariantów. Wariant to jeden lub kilka elementów z katalogu (przedmioty albo repliki) albo jawne „puste”. Slot bez wariantów oznacza „nieokreślony”, a „puste” oznacza „celowo bez elementu” (odpowiednik `PUSTE`).
- FR-704 [M] Każdy slot ma wariant domyślny. Aplikacja zapamiętuje także wariant ostatnio użyty na wyprawie dla danego zestawu.
- FR-705 [M] Pasek spójności kamuflażu: próbniki wzorów elementów wybranego wariantu w jednej linii, z nazwami wzorów po najechaniu. Ma charakter informacyjny, bez blokad i ostrzeżeń.
- FR-706 [M] Jeśli użytkownik wpisze element, którego nie ma w katalogu (np. „Rękawice Ratnik”, „Gogle 6b50”, „Grach”), aplikacja tworzy przedmiot lub replikę ze znacznikiem „do uzupełnienia” i podpowiada w widoku „Do uzupełnienia”.
- FR-707 [S] Lista pakowania: dla wybranego zestawu i wariantów aplikacja generuje listę elementów pogrupowaną wg miejsca przechowywania (Piwnica, Dom), z odhaczaniem w aplikacji, wydrukiem i eksportem. Elementy zamontowane na nośniku są pokazane pod nośnikiem.
- FR-708 [S] Koszt zestawu = suma cen elementów wariantów domyślnych. Zakres od minimum do maksimum dla wszystkich wariantów [C].
- FR-709 [M] Karta zestawu pokazuje liczbę wypraw, procent wszystkich wypraw i datę ostatniego użycia.
- FR-710 [S] Kopiowanie zestawu jako punkt wyjścia dla nowego.
- FR-711 [M] Ostrzeżenie (bez blokady), gdy wariant zawiera element sprzedany lub wycofany.
- FR-712 [C] Dokumentacja odwzorowania zestawu: formacja i okres lub wydarzenie, które zestaw ma odtwarzać (np. nazwy „Gostomel”, „SMO”), źródła, zdjęcia referencyjne obok zdjęć własnych, uwagi o poprawności poszczególnych elementów.

### 7.8 Wyprawy

- FR-801 [M] Wyprawa: data (wymagana), nazwa lub miejsce (opcjonalne, z podpowiedziami z wcześniejszych), replika główna (wymagana, chyba że szablon rekonstrukcji tego nie wymaga), zestaw (opcjonalny), skład, komentarz, zdjęcia [S].
- FR-802 [M] Szybkie dodawanie w jednym oknie: data (domyślnie dziś), zestaw (domyślnie ostatnio użyty), replika główna (podpowiedź z zestawu: pierwszy wariant slotu oznaczonego jako broń główna), warianty slotów jako przełączniki, komentarz. Zapis klawiszem Enter. Kryterium: wyprawa z domyślnym składem dodana w nie więcej niż 5 kliknięciach lub naciśnięciach klawiszy po otwarciu okna.
- FR-803 [M] Po wyborze zestawu skład ustawia się na warianty zapamiętane (FR-704). Zapisany skład jest podstawą liczenia użyć każdego elementu (BR-08).
- FR-804 [M] Wyprawa bez zestawu jest dopuszczalna. Skład można wtedy podać ręcznie albo pominąć (wtedy liczy się tylko replika główna).
- FR-805 [M] Sprzedane i wycofane elementy nie są oferowane w wyborze repliki i składu dla dat późniejszych niż data sprzedaży lub wycofania. Dla wcześniejszych dat są dostępne (uzupełnianie historii).
- FR-806 [M] Liczniki historyczne (sprzed używania aplikacji), przenoszone z arkuszy: liczba wypraw dla każdej repliki, liczba wypraw dla każdego munduru lub kompletu z tabeli umundurowania (liczniki z arkusza Mundury są według autora nieaktualne i nie są importowane), liczba użyć dla pozostałych kategorii z kolumny „Liczba użyć” (oznaczona „do weryfikacji”), liczby wypraw wg lat. Są to niezależne sumy; aplikacja nie tworzy z nich sztucznych wypraw. Liczniki są widoczne i edytowalne w kartach, ale osobno od wyliczanych.
- FR-807 [M] Dzień zerowy: data ustawiana przy imporcie (domyślnie dzień importu). Wyprawy dodane z datą późniejszą niż dzień zerowy zwiększają liczniki. Wyprawę z datą do dnia zerowego włącznie aplikacja traktuje jako uzupełnienie historii i pyta, czy odjąć ją od liczników historycznych (replika, mundur, rok); odpowiedź domyślna „tak”. Zapobiega to podwójnemu liczeniu.
- FR-808 [M] Lista wypraw pogrupowana wg roku, z sumą rocznych wypraw (liczniki historyczne + zapisane). Filtry: replika, zestaw, mundur, strona, rok, miejsce. Edycja i usuwanie z możliwością cofnięcia.
- FR-809 [M] Edycja lub usunięcie wyprawy aktualizuje wszystkie liczniki natychmiast. Żaden licznik nie jest zapisywany jako liczba wpisana ręcznie poza licznikami historycznymi z FR-806.
- FR-810 [C] Repliki dodatkowe w składzie wyprawy (np. zapasowa) z osobnym licznikiem „użycia jako dodatkowa”. Statystyki wypraw replik liczą wyłącznie replikę główną (Q5).
- FR-811 [C] Koszty wyjazdu (wpisowe, dojazd, amunicja) jako osobna kategoria kosztów wyprawy; nie ma ich w arkuszach.

### 7.9 Statystyki i raporty

- FR-901 [M] Raport „Bronie”: odpowiednik pierwszej tabeli arkusza. Kolumny: miejsce, nazwa, wyprawy, procent, typ magazynka, rola, strona, koszt, cena oryginalna, dodatki, koszt na wyprawę. Wiersz średnich i wiersz sum dla replik posiadanych. Przełącznik „Uwzględnij sprzedane” (wtedy sprzedane pokazują tylko wyprawy i procent).
- FR-902 [M] Raport „Umundurowanie”: wiersz to mundur lub komplet; kolumny: strona, wzór, wyprawy, procent, koszt, koszt na wyprawę. Koszt munduru to cena góry i dołu (komplet), bez dodatków. Sumy wg strony (NATO, Polska, Rosja) i łączna. Możliwość grupowania wg wzoru kamuflażu [S].
- FR-903 [M] Raport „Wyprawy wg lat”: liczba wypraw w roku (liczniki historyczne + zapisane), suma narastająca, data pierwszej wyprawy, średnia na rok i na miesiąc od pierwszej wyprawy.
- FR-904 [M] Raport „Użycie wyposażenia”: przedmiot, kategoria, użycia (historyczne + z wypraw), cena, koszt na użycie. Sortowanie po zerowych użyciach.
- FR-905 [S] Raport „Koszty”: wydatki wg roku, kategorii i rodzaju (zakup, ulepszenie), wartość posiadanej kolekcji wg cen zakupu.
- FR-906 [S] Raport „Zestawy”: wyprawy, procent, ostatnie użycie, koszt zestawu, koszt na wyprawę.
- FR-907 [S] Raport „Stan replik”: rozkład stanów, repliki bez testu powyżej progu, otwarte awarie.
- FR-908 [M] Każdy raport eksportuje się do Excela (.xlsx) i CSV; wydruk [S].
- FR-909 [S] Wykresy: słupkowy wypraw wg lat, poziome słupki udziału wg strony i wg munduru. Bez wykresów kołowych.
- FR-910 [M] Ekran „Kontrola spójności”: porównuje łączną liczbę wypraw liczoną wg replik, wg lat i wg mundurów, wskazuje różnice i liczbę wypraw bez przypisanej repliki lub munduru. Po imporcie początkowym wszystkie trzy sumy wynoszą 89.
- FR-911 [M] „Jak liczymy”: przy każdym wskaźniku w raportach (procent wypraw, koszt na wyprawę, koszt dodatków, użycia i inne) dostępny jest krótki opis definicji zgodny z rozdziałem 8, w tym przypadki szczególne (np. koszt na wyprawę przy zerowej liczbie wypraw). Cel: ten sam wskaźnik znaczy to samo u każdego użytkownika.

### 7.10 Słowniki i ustawienia

- FR-1001 [M] Edytowalne słowniki: rekonstrukcje, strony, wzory i kolory (kamuflaże, materiały), miejsca przechowywania, role replik, typy magazynków, napędy, stany replik, pochodzenie / klasa, rodzaje pozycji kosztowych, rodzaje zadań, kategorie, sloty zestawów. Wartości startowe w Załączniku B.
- FR-1002 [M] Zmiana nazwy wartości słownika aktualizuje ją wszędzie. Usunięcie używanej wartości wymaga wskazania zastępnika.
- FR-1003 [M] Scalanie wartości (np. „ATACS” i „A-TACS”) z przeniesieniem wszystkich odniesień. Wartość scalona zostaje zapamiętana jako alias, żeby import rozpoznawał ją w przyszłości.
- FR-1004 [M] Wzór kamuflażu ma próbnik: jeden kolor albo pasek 3–4 kolorów, opcjonalnie mały obraz. Domyślne próbniki aplikacja nadaje przybliżonymi barwami charakterystycznymi dla wzoru, użytkownik je poprawia.
- FR-1005 [M] Strona jest cechą przedmiotu, repliki i zestawu, a nie wzoru kamuflażu (wzór Multicam występuje w sprzęcie rosyjskim i NATO).
- FR-1006 [S] Ustawienia: motyw (jasny, ciemny, systemowy), folder danych i zdjęć, próg „brak testu” w dniach, kolejność sortowania stanów replik.
- FR-1007 [C] Limity energii (Q9).

### 7.11 Import, eksport, kopie zapasowe

- FR-1101 [M] Import początkowy z pliku Excel (.xlsx) w układzie `Dane_zrodlowe_Kwatermistrz.xlsx`. Aplikacja nie czyta plików .numbers; arkusze eksportuje się z Numbers do Excela, a wyciąg dołączony do dokumentacji jest już gotowy. Dane z arkuszy autora są dawne i przykładowe: import ma dać punkt wyjścia do przeglądu i aktualizacji (FR-1110).
- FR-1102 [M] Import działa na podglądzie: przed zapisem pokazuje, ile rekordów każdego typu powstanie, listę konfliktów i wątpliwości oraz pozwala je rozstrzygnąć. Do zatwierdzenia nic nie jest zapisywane.
- FR-1103 [M] Dopasowania proponowane przez import, każde do zatwierdzenia lub zmiany:
  - repliki z karty technicznej do listy replik (po nazwie, z tolerancją na różnice zapisu; duplikaty z 13.3),
  - elementy zestawów do przedmiotów katalogu,
  - ładownice do nośników na podstawie komentarza (np. „6b46”, „Pas MBC”, „6sh117 Zjuratkul”); gdy komentarz pasuje do kilku nośników, import prosi o wskazanie,
  - pozycje tabeli umundurowania do przedmiotów z kategorii Mundury (np. „Gorka 3 Splav” do „Splav Górka 3”),
  - wartości do słowników (z aliasami, np. „LKM” do „Wsparcie”, „WSS/Wał” do „VSS/VAL”),
  - przypisanie do rekonstrukcji: domyślnie powstaje po jednej rekonstrukcji na stronę występującą w danych (Rosja, Polska, NATO), a pozycja trafia do rekonstrukcji swojej strony; replika o dwóch stronach (np. „POLSKA/ROSJA”) trafia do obu; nazwy i podział użytkownik zmienia w podglądzie (Q16).
- FR-1104 [S] Import jest powtarzalny: ponowne wczytanie tego samego pliku nie tworzy duplikatów, tylko proponuje aktualizację zmienionych rekordów.
- FR-1105 [M] Raport z importu jest zapisywany i dostępny później: co zaimportowano, co pominięto, co czeka na decyzję (widok „Do uzupełnienia”).
- FR-1106 [M] Eksport całości do jednego pliku Excel o układzie zbliżonym do arkuszy źródłowych oraz eksport dowolnej listy i raportu do CSV.
- FR-1107 [M] Kopia zapasowa jednym poleceniem do pliku (dane i zdjęcia) i przywracanie z pliku. Przed każdym importem i przed migracją po aktualizacji aplikacja robi kopię automatycznie.
- FR-1108 [S] Automatyczna kopia przy zamknięciu do wskazanego folderu, z zachowaniem ostatnich 10.
- FR-1109 [C] Przypomnienie, gdy ostatnia kopia jest starsza niż 30 dni.
- FR-1110 [M] Każdy zaimportowany rekord dostaje znacznik „z importu, do przeglądu”. Widok „Do uzupełnienia” (FR-1204) prowadzi przez przegląd, a znacznik znika po zatwierdzeniu rekordu lub po jego edycji. Liczniki historyczne z importu są edytowalne i oznaczone „do weryfikacji”, dopóki użytkownik ich nie potwierdzi.

### 7.12 Wspólne

- FR-1201 [M] Globalne wyszukiwanie (skrót Ctrl+K): przedmioty, repliki, zestawy, wyprawy, zadania; wyniki pogrupowane wg typu, otwierane Enterem.
- FR-1202 [M] Cofnięcie po usunięciu (komunikat z przyciskiem „Cofnij”) oraz Kosz z możliwością przywrócenia przez co najmniej 30 dni.
- FR-1203 [M] Brak ogólnego przycisku „Zapisz wszystko”. Zmiany zapisują się po zatwierdzeniu formularza; formularz z niezapisanymi zmianami sygnalizuje to i pyta przed zamknięciem.
- FR-1204 [S] Widok „Do uzupełnienia”: jedna lista rzeczy wymagających decyzji lub danych (pola nieznane, pozycje kosztowe bez opisu, przedmioty „do uzupełnienia”, konflikty z importu).
- FR-1205 [S] Na każdej karcie widoczne „ostatnia zmiana: data i godzina”.
- FR-1206 [S] Ostatnio otwarte pozycje w menu głównym.

### 7.13 Rekonstrukcje i ogólność

Wymagania wynikają z decyzji autora, że koncepcja jest otwarta: aplikacja ma obsłużyć także zupełnie inną rekonstrukcję (np. 101. Dywizja Powietrznodesantowa) z innymi kategoriami sprzętu i innym rodzajem replik.

- FR-1301 [M] Rekonstrukcja (profil): nazwa, okres lub era, strona (opcjonalnie), opis, zdjęcie, szablon slotów zestawu. Po imporcie początkowym istnieją rekonstrukcje utworzone według FR-1103. Użytkownik dodaje, zmienia nazwę, scala i usuwa (puste) rekonstrukcje bez ograniczeń.
- FR-1302 [M] Przedmioty, repliki i zestawy można przypisać do zera, jednej lub wielu rekonstrukcji. Brak przypisania oznacza element wspólny, widoczny w każdej rekonstrukcji (np. rękawice używane w kilku).
- FR-1303 [M] Przełącznik aktywnej rekonstrukcji w pasku górnym, z opcją „Wszystkie”. Filtruje katalogi, listy, pulpit, mozaikę, raporty, zestawy i wyprawy, nie zmieniając danych. Wybór jest zapamiętywany.
- FR-1304 [M] Wyprawa należy do jednej rekonstrukcji (domyślnie z zestawu, a bez zestawu z aktywnej). Łączna liczba wypraw i kontrola spójności (FR-910) liczą wszystkie rekonstrukcje razem.
- FR-1305 [M] Zasada ogólności: w programie nie ma na stałe żadnych treści specyficznych dla sprzętu rosyjskiego ani airsoftowego. Kategorie, nośniki, sloty, parametry karty technicznej, napędy, role, stany, strony, pochodzenie, wzory i kolory są danymi konfiguracyjnymi. Wartości z arkuszy są wyłącznie danymi startowymi z importu.
- FR-1306 [M] Nową rekonstrukcję można utworzyć od zera (puste kategorie i sloty do dodania) albo „na wzór” istniejącej: kopiuje kategorie, sloty i szablony karty, nie kopiuje przedmiotów, replik ani wypraw.
- FR-1307 [M] Użytkownik definiuje własne napędy replik i szablony karty technicznej (np. dla repliki sprężynowej lub gazowej z innej epoki), z dowolnymi parametrami i ich kolejnością. Replika bez szablonu pasującego do napędu dostaje szablon pusty do uzupełnienia.
- FR-1308 [C] Dokumentacja odwzorowania na poziomie rekonstrukcji (formacja, okres, źródła, zdjęcia referencyjne), patrz FR-712.

### 7.14 Użycie przez wielu niezależnych użytkowników

Z aplikacji korzystają autor i koledzy rekonstruujący inne jednostki. Każdy ma własną instalację i własne dane. Konfiguracja jest elastyczna, ale reguły obliczeń są wspólne (NFR-14).

- FR-1401 [M] Kreator pierwszego uruchomienia: nazwa pierwszej rekonstrukcji i jej strona oraz wybór punktu startu: pusta aplikacja, szablon konfiguracji (FR-1404, FR-1406) albo import z arkusza (FR-1101, FR-1403). Aplikacja startowa nie zawiera żadnych danych autora.
- FR-1402 [M] Każdy użytkownik ma własne dane lokalnie. Brak kont i synchronizacji. Wymiana wyłącznie przez jawny eksport i import plików (FR-1106, FR-1404, FR-1405).
- FR-1403 [S] Import ogólny z mapowaniem kolumn: dla pliku Excel lub CSV w dowolnym układzie użytkownik wskazuje, która kolumna odpowiada któremu polu (firma, nazwa, wzór, miejsce itd.) i do jakiej kategorii trafiają wiersze. Mapowanie można zapisać i użyć ponownie. Podgląd (FR-1102) i znacznik „z importu, do przeglądu” (FR-1110) działają tak samo jak przy imporcie z układu autora.
- FR-1404 [M] Pakiet konfiguracji: eksport i import pliku z konfiguracją rekonstrukcji (kategorie, oznaczenia nośników, sloty i ich oznaczenia, szablony karty technicznej, słowniki). Bez przedmiotów, replik, wypraw, zdjęć i cen. Import pakietu pokazuje podgląd i pozwala scalić słowniki z istniejącymi (FR-1003).
- FR-1405 [C] Eksport i import całej rekonstrukcji (dane i zdjęcia) do jednego pliku, do przekazania koledze lub przeniesienia między instalacjami.
- FR-1406 [S] Dołączone szablony konfiguracji: „Pusta” (minimalna) oraz przykład według układu autora (kategorie i sloty z rozdziału 5), bez żadnych danych o sprzęcie.
- FR-1407 [M] Dane przykładowe z arkuszy autora nie wchodzą do instalatora. Są w repozytorium jako plik do ręcznego importu i do testów (Q17).

---

## 8. Reguły biznesowe i obliczenia

### 8.1 Reguły

- BR-01 Wyprawy repliki = licznik historyczny repliki + liczba zapisanych wypraw po dniu zerowym, w których jest repliką główną.
- BR-02 Łączna liczba wypraw = suma wypraw wszystkich replik, także przeznaczonych do sprzedaży i sprzedanych. Po imporcie: 58 (posiadane) + 1 (do sprzedaży, SA-G12) + 30 (sprzedane) = 89.
- BR-03 Procent wypraw repliki = jej wyprawy / łączna liczba wypraw. Wyprawy sprzedanych replik są w mianowniku.
- BR-04 Koszt całkowity, cena oryginalna i koszt dodatków wynikają z pozycji kosztowych (FR-502).
- BR-05 Koszt na wyprawę = koszt całkowity / wyprawy. Gdy wypraw jest 0, wartość jest równa kosztowi całkowitemu, a interfejs oznacza ją „bez wypraw”.
- BR-06 Średnie i sumy kosztów dotyczą tylko replik o statusie „posiadana”. Repliki „do sprzedaży” i sprzedane są pomijane w kosztach, ale zachowują swoje wyprawy w BR-02 i BR-03.
- BR-07 Wyprawy w roku R = licznik historyczny roku R + zapisane wyprawy z datą późniejszą niż dzień zerowy i z roku R. Rok bieżący nie jest już różnicą do sumy, jak w arkuszu.
- BR-08 Użycia przedmiotu = licznik historyczny + liczba zapisanych wypraw po dniu zerowym, w których przedmiot jest w składzie. Elementy kompletu mają wspólną liczbę użyć.
- BR-09 Mundur wyprawy to elementy ze slotów oznaczonych w szablonie rekonstrukcji jako sloty munduru (domyślnie Mundur), traktowane jako komplet. Wyprawa zalicza się do tego munduru. Gdy slot zawiera kilka niezależnych elementów (mieszanka), wyprawa zalicza się do pierwszego z nich i jest oznaczona jako „mieszanka” (do potwierdzenia, Q4).
- BR-10 Koszt pozycji raportu umundurowania = suma cen munduru, czyli góry i dołu (komplet), bez dodatków, chyba że dane stanowią inaczej (rozstrzygnięte przez autora). Koszt na wyprawę według BR-05.
- BR-11 Sumy według strony obejmują wszystkie pozycje z daną stroną. Pozycje bez strony trafiają do osobnego wiersza „bez strony”. Nie wolno powtórzyć błędów arkusza opisanych w 13.1.
- BR-12 Wartości w stanie „nieznane” nie są traktowane jak zero. Średnie liczy się z wartości znanych.
- BR-13 Kwoty nie mogą tracić dokładności (rozliczenie co do grosza). Zaokrągla się tylko przy wyświetlaniu.
- BR-14 Formaty: waluta PLN, zapis `3 297,12 zł`; procenty z jednym miejscem po przecinku, `21,3%`; daty `01.09.2022`; sortowanie tekstu zgodne z polskim alfabetem.
- BR-15 Data pierwszej wyprawy to 01.09.2022 z importu (edytowalna w ustawieniach) albo wcześniejsza data zapisanej wyprawy.
- BR-16 Filtr aktywnej rekonstrukcji ogranicza widoki i raporty do jej danych oraz elementów wspólnych. Kontrola spójności (FR-910) i łączna liczba wypraw liczą wszystkie rekonstrukcje.

### 8.2 Dane testowe po imporcie początkowym

Wartości do sprawdzenia automatycznymi testami (NFR-09). Źródło: arkusz „Lista broni i ekwipunku”. Dane odzwierciedlają dawny stan arkuszy i służą do testów obliczeń oraz importu; nie muszą odpowiadać aktualnemu stanowi kolekcji autora.

Repliki:

| Replika | Wyprawy | Procent | Koszt | Cena oryg. | Dodatki | Koszt na wyprawę |
|---|---|---|---|---|---|---|
| SA-J01 Edge 2 (AK-74M Alfa) | 19 | 21,3% | 3 297,12 zł | 960,00 zł | 2 337,12 zł | 173,53 zł |
| SA-H20 (HK 416) | 10 | 11,2% | 2 189,00 zł | 988,00 zł | 1 201,00 zł | 218,90 zł |
| AT-AK12-PE (AK-12) | 8 | 9,0% | 1 732,78 zł | 1 682,78 zł | 50,00 zł | 216,60 zł |
| PKM | 3 | 3,4% | 3 015,00 zł | 3 015,00 zł | 0,00 zł | 1 005,00 zł |
| E&L ELMS (AKMS) | 0 | 0,0% | 1 076,00 zł | 491,00 zł | 585,00 zł | 1 076,00 zł |
| Custom (AKS-74U Alfa) | 1 | 1,1% | 800,00 zł | 800,00 zł | 0,00 zł | 800,00 zł |

Wartości zbiorcze replik:

| Wielkość | Wartość |
|---|---|
| Repliki posiadane / do sprzedaży / sprzedane | 18 / 1 / 8 |
| Wyprawy: posiadane / do sprzedaży / sprzedane / łącznie | 58 / 1 / 30 / 89 |
| Suma kosztów replik posiadanych | 27 769,07 zł |
| Suma cen oryginalnych | 19 752,95 zł |
| Średni koszt repliki | 1 542,73 zł |
| Średnia cena oryginalna | 1 097,39 zł |
| Średni koszt dodatków | 445,34 zł |
| Średni koszt na wyprawę (średnia z replik) | 883,36 zł |

Wyprawy w czasie:

| Wielkość | Wartość |
|---|---|
| Pierwsza wyprawa | 01.09.2022 |
| 2022 / 2023 / 2024 / 2025 / 2026 | 10 / 30 / 17 / 19 / 13 |
| Razem | 89 |

Umundurowanie (tabela „Szpej”):

| Pozycja | Wyprawy | Koszt | Koszt na wyprawę |
|---|---|---|---|
| Flecktarn | 5 | 250,00 zł | 50,00 zł |
| WZ. 93 | 16 | 160,00 zł | 10,00 zł |
| Palma | 2 | 700,00 zł | 350,00 zł |
| Combat Bars EMR/VKBO 3.1 (komplet) | 1 | 570,00 zł (220 + 350) | 570,00 zł |
| WZ. 2019 124T | 0 | 175,00 zł | 175,00 zł |
| DKWS | 0 | brak ceny | 0,00 zł |

Sumy wg strony:

| Strona | Wyprawy | Koszt | Koszt na wyprawę | Uwaga |
|---|---|---|---|---|
| NATO | 23 | 944,00 zł | 41,04 zł | |
| POLSKA | 17 | 518,09 zł | 30,48 zł | |
| ROSJA | 41 | 6 251,89 zł | 152,49 zł | arkusz podaje 5 851,89 zł i 142,73 zł, bo pomija ostatni wiersz (Gorka 5 ATACS-FG, 400,00 zł); aplikacja ma pokazać wartość poprawną |
| Wcześniejsze EMR (Chiński, Liuks, Ręczny Wiosna/Jesień) | 8 | 0,00 zł | | bez przypisanej strony w arkuszu |
| Razem | 89 | 7 713,98 zł | 86,67 zł | |

Wyposażenie po imporcie: 106 przedmiotów w kategoriach z arkuszy (Czapki 4, Hełmy 3, Szalokominiarki 4, Mundury 15, Kamizelki 8, Pasoszelki 3, Pasy 4, Nakolanniki 2, Plecaki 3, Ładownice 60). Dochodzą przedmioty-szkice utworzone z tabeli umundurowania i zestawów, które nie mają odpowiednika; ich liczba zależy od decyzji w podglądzie importu. Liczniki historyczne użyć: mundury z tabeli umundurowania (liczniki z arkusza Mundury, suma 29, nie są importowane, bo są nieaktualne); Kamizelki 19 i Pasoszelki 4 z kolumny „Liczba użyć”, oznaczone „do weryfikacji”.

Zestawy: 15 (3 bez nazwy, 9 nazwanych, 3 szkice zawierające tylko pas). Zadania: 18 (niepuste komórki w dwóch wierszach notatek: 12 w pierwszym, 6 w drugim; dwa z nich dotyczą SA-G12 (G36C), która jest w częściach i przeznaczona do sprzedaży, patrz 13.3).

---

## 9. Wymagania niefunkcjonalne

- NFR-01 Platforma: Windows 10 22H2 i Windows 11, 64-bit. Ekrany od 1366×768 do 4K, skalowanie systemowe 100–200%.
- NFR-02 Praca w całości offline. Aplikacja nie wysyła żadnych danych i nie zbiera telemetrii.
- NFR-03 Dane leżą lokalnie w jednym miejscu (plik danych i folder zdjęć) w otwartym formacie, który da się odczytać poza aplikacją standardowymi narzędziami. Przeniesienie na inny komputer polega na skopiowaniu folderu.
- NFR-04 Wydajność przy 5 000 przedmiotów, 2 000 wypraw i 5 000 zdjęć: start poniżej 3 s, wyszukiwanie poniżej 300 ms, płynne przewijanie list po 1 000 wierszy.
- NFR-05 Trwałość: po zatwierdzeniu zmiany nie można jej stracić przy awarii procesu lub zaniku zasilania. Przy starcie kontrola integralności danych. Przed importem i migracją automatyczna kopia.
- NFR-06 Aktualizacja aplikacji nie niszczy danych. Starsze dane są migrowane, a kopia sprzed migracji zostaje.
- NFR-07 Interfejs w języku polskim, bez mieszania z angielskim w etykietach.
- NFR-08 Dostępność: pełna obsługa klawiaturą, widoczny fokus, kontrast zgodny z WCAG 2.1 AA, praca przy powiększonym tekście. Znaczenie nigdy nie wynika wyłącznie z koloru: próbnik wzoru zawsze ma obok nazwę, stan zawsze ma tekst.
- NFR-09 Reguły z rozdziału 8 pokryte automatycznymi testami na danych z załącznika; test importu sprawdza wartości z 8.2.
- NFR-10 Instalacja i aktualizacja bez uprawnień administratora (instalator użytkownika albo wersja przenośna), możliwa dla osób bez wiedzy technicznej [M].
- NFR-11 Zdjęcia są zmniejszane przy zapisie do rozsądnego rozmiaru (dłuższy bok do 2048 px) z miniaturami; zachowanie oryginału opcjonalne [S].
- NFR-12 Aplikacja zapamiętuje rozmiar i położenie okna, układ kolumn, ostatnio wybrane filtry i ostatnio otwarty ekran.
- NFR-13 Ogólność: dodanie nowej rekonstrukcji z innymi kategoriami, slotami, napędami i szablonami karty technicznej odbywa się wyłącznie w aplikacji, bez zmian w kodzie i bez nowej wersji programu. Test akceptacyjny: utworzenie rekonstrukcji „101. Dywizja Powietrznodesantowa” z własnymi kategoriami (np. „Hełm M1”, „Kurtka”), innymi slotami i repliką o napędzie sprężynowym, a następnie dodanie zestawu i wyprawy.
- NFR-14 Spójność obliczeń: definicje wskaźników i reguły z rozdziału 8 są takie same w każdej instalacji i zmieniają się wyłącznie z nową wersją aplikacji. Nie są konfigurowalne. Konfigurowalne są dane (kategorie, słowniki, sloty, szablony) oraz progi i tolerancje z ustawień. Numer wersji reguł jest widoczny w oknie „O programie” i w każdym eksporcie raportu.
- NFR-15 Dane autora z arkuszy nie są częścią programu ani instalatora. Pierwsze uruchomienie daje pustą aplikację (FR-1401).

---

## 10. Kierunek wizualny i interakcje

### 10.1 Założenia

To narzędzie do codziennego wpisywania i przeglądania gęstych danych, a nie strona prezentacyjna. Interfejs ma być spokojny, czytelny i szybki w obsłudze. Kolor w aplikacji pochodzi z samego sprzętu, czyli z próbników wzorów kamuflażu, a nie z dekoracji. Jedynym wyrazistym elementem jest mozaika wypraw na pulpicie. Reszta jest zdyscyplinowana i neutralna.

Przegląd kierunku względem tematu: pierwszy pomysł (oliwka i czerń, wersaliki jak w instrukcji wojskowej, kolorowe stemple „SPRZEDANE”) został odrzucony. Jest to odpowiedź domyślna dla każdej aplikacji o tematyce wojskowej, nie wynika z danych użytkownika, a wersaliki i stemple męczą w gęstych tabelach. Zastąpiła go paleta neutralna, w której jedyne nasycone kolory to próbniki kamuflaży i kolor sygnałowy awarii.

### 10.2 Paleta

| Nazwa | Motyw jasny | Motyw ciemny | Zastosowanie |
|---|---|---|---|
| Płótno | #ECEDEA | #1E2221 | tło okna |
| Karta | #F7F7F5 | #272C2A | panele, karty, wiersze tabel |
| Tusz | #222725 | #E4E7E3 | tekst podstawowy |
| Linia | #CDD0CA | #3A403D | obramowania, separatory |
| Stal | #2F4A6B | #8DB0D8 | akcje główne, zaznaczenie, fokus |
| Sygnał | #A83A2B | #E08A7A | awaria, błąd, operacje destrukcyjne |

Kolory pomocnicze stanu, używane zawsze razem z tekstem: poprawny #4A7A52 / #8CC495, uwaga #9A6B00 / #E0B34A. Próbniki wzorów kamuflażu są jedynymi dodatkowymi kolorami. Tryb systemowy przełącza motyw automatycznie, użytkownik może to nadpisać (FR-1006).

### 10.3 Typografia

Jedna rodzina: Bahnschrift (wbudowana w Windows 10 i 11, zmienna grubość i szerokość, charakter techniczny, pasuje do oznaczeń sprzętu). Gdy jej brakuje, Segoe UI. Agent sprawdza poprawne wyświetlanie polskich znaków oraz cyfr tabelarycznych w kolumnach kwot i wypraw.

| Rola | Krój | Rozmiar |
|---|---|---|
| Tytuł ekranu | Bahnschrift SemiCondensed SemiBold | 22 px |
| Nagłówek sekcji | Bahnschrift SemiCondensed SemiBold | 16 px |
| Liczby na pulpicie | Bahnschrift SemiCondensed Bold | 32 px |
| Tabele i formularze | Bahnschrift Regular | 13 px |
| Teksty długie (komentarze, notatki) | Bahnschrift Regular | 14 px, linia do 80 znaków |

Etykiety piszemy zwykłym zapisem zdaniowym, bez wersalików. Liczby i kwoty w tabelach wyrównane do prawej.

### 10.4 Układ okna

Lewa szyna nawigacji (zwijana do ikon): Pulpit, Wyprawy, Bronie, Wyposażenie (rozwijane kategorie z licznikami), Zestawy, Raporty, Do uzupełnienia, Ustawienia. Na górze wyszukiwanie globalne, przełącznik rekonstrukcji (FR-1303) i przycisk „Dodaj wyprawę”. Pośrodku lista lub raport. Z prawej panel karty wybranej pozycji (szerokość około 420 px, zamykany), tak aby przeglądanie listy i edycja odbywały się bez zmiany ekranu. Wyrównanie: tekst do lewej, kwoty i liczby do prawej.

Pulpit:

```
┌─────────────┬────────────────────────────────────────────────────────────────┐
│ Kwatermistrz│ Szukaj…  Ctrl+K     [ Rosja współczesna ▾ ]  [ Dodaj wyprawę ] │
│             ├────────────────────────────────────────────────────────────────┤
│ Pulpit      │ 89 wypraw    13 w 2026    ostatnia: dd.MM.rrrr                  │
│ Wyprawy     │                                                                │
│ Bronie    18│ 2026  ■■■■■■■■■■■■■                                        13  │
│ Wyposaż. 106│ 2025  ■■■■■■■■■■■■■■■■■■■                                  19  │
│  Czapki    4│ 2024  ■■■■■■■■■■■■■■■■■                                    17  │
│  Hełmy     3│ 2023  ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■                       30  │
│  …          │ 2022  ■■■■■■■■■■                                           10  │
│ Zestawy   15│       kafelek = wyprawa, kolor = wzór użytego munduru          │
│ Raporty     │                                                                │
│ Do uzupełn. │ Awarie n    Zadania n    Bez testu >180 dni n    Nieznane n    │
└─────────────┴────────────────────────────────────────────────────────────────┘
```

Lista replik z kartą:

```
┌ Bronie ─────────────────────────────────────────┬ SA-J01 Edge 2 (AK-74M Alfa) ──┐
│ Szukaj…   Strona ▾   Rola ▾   Stan ▾   Awaria ▾ │ Ideał – sprawdzony w boju     │
│  #  Nazwa                 Wyprawy    %   Koszt/wyp│ Przegląd Karta Pomiary Zadania │
│  1  SA-J01 Edge 2 (…)         19  21,3%  173,53 zł│ Wyprawy      19 (21,3%)        │
│  2  SA-H20 (HK 416)           10  11,2%  218,90 zł│ Koszt   3 297,12 zł            │
│  3  CM.057T (SWD-M)            9  10,1%  173,33 zł│   cena oryg.   960,00 zł       │
│  …                                              │   dodatki    2 337,12 zł       │
│  Średnia                          …    883,36 zł │ Ostatni test       19.07.2026   │
└─────────────────────────────────────────────────┴───────────────────────────────┘
```

Dodawanie wyprawy (okno z przełącznikami wariantów):

```
┌ Dodaj wyprawę ─────────────────────────────────────────┐
│ Data       [dd.MM.rrrr]            Miejsce [__________] │
│ Zestaw     [Gostomel ▾]                                 │
│ Replika    [SA-J01 Edge 2 (AK-74M Alfa) ▾]              │
│ Mundur     (■ Demisezonka)                              │
│ Kamizelka  (Skanda Light) ( 6b45 ) ( 6b46 )             │
│ Plecak     (6sh117 25L) ( ANA MC )                      │
│ Pas        (Pas taktyczny MC MBC)                       │
│ Komentarz  [____________________________________]      │
│                          [ Anuluj ]  [ Dodaj wyprawę ]  │
└─────────────────────────────────────────────────────────┘
```

Edytor zestawu:

```
Zestaw  SMO/Modern Commercial        Strona  Rosja        Stan  ( ) szkic  (•) aktywny
Spójność kamuflażu   ■ A-TACS  ■ Multicam  ■ EMR
Slot        Warianty                                                  ★ = domyślny
Mundur      ★ ATACS G3   |  ATACS Bars  |  Aride Savanna                      [ + ]
Kamizelka   ★ Skanda Light  |  Skanda Heavy  |  BNZ                            [ + ]
Pas         ★ Pas taktyczny MC MBC  |  Pas taktyczny Wartech                  [ + ]
Broń DMR    ★ SWD                                                             [ + ]
```

### 10.5 Element wyróżniający: próbnik kamuflażu i mozaika wypraw

Próbnik wzoru to mały kwadrat z paskiem 1–4 barw wzoru (14 px w tabelach, 24 px na karcie). Zawsze występuje razem z nazwą wzoru, poza mozaiką.

Mozaika wypraw na pulpicie: kafelek 14×14 px, odstęp 3 px, rząd na rok (najnowszy u góry), etykieta roku po lewej i suma po prawej. Kolor kafelka to próbnik wzoru munduru z wyprawy. Wyprawa bez munduru ma kafelek pusty z obrysem. Wpisy z liczników historycznych mają kafelek w kolorze Linia z przekątną kreską (znaczy „bez daty”). Legenda pod mozaiką. Mozaika jest klikalna i dostępna z klawiatury.

### 10.6 Interakcja

- Skróty: Ctrl+N dodaj wyprawę, Ctrl+K wyszukiwanie, Ctrl+E edytuj, F2 zmień nazwę, Delete usuń (z cofnięciem), Enter zatwierdź, Esc anuluj.
- Tabele: obsługa klawiaturą, zaznaczanie wielu wierszy, menu kontekstowe, zapamiętane kolumny i sortowanie.
- Brak ozdobnego ruchu. Dopuszczalne wyłącznie reakcje na działanie użytkownika: krótkie podświetlenie nowo dodanego wiersza lub kafelka (około 600 ms), rozwinięcie panelu. Aplikacja respektuje systemowe ograniczanie animacji.
- Potwierdzenie pytaniem tylko dla operacji nieodwracalnych (opróżnienie Kosza, nadpisanie danych przywróceniem kopii). Pozostałe usunięcia są natychmiastowe z przyciskiem „Cofnij”.

### 10.7 Teksty w interfejsie

- Zapis zdaniowy, tryb rozkazujący na przyciskach: „Dodaj wyprawę”, „Zapisz zmiany”, „Anuluj”, „Usuń”, „Cofnij”. Ta sama akcja nazywa się zawsze tak samo w całym przepływie (przycisk „Dodaj wyprawę” daje komunikat „Dodano wyprawę”).
- Błędy mówią, co się stało i co zrobić, bez przeprosin: „Nie zapisano wyprawy. Wybierz replikę główną.”
- Puste ekrany zachęcają do działania: „Nie ma jeszcze wypraw. Dodaj pierwszą albo zaimportuj dane z arkuszy.”
- Brak wyników: „Nic nie znaleziono dla „…”. Sprawdź pisownię albo zmień filtry.”
- Pola: „Nieznane” w kolorze Stal z ikoną, „Nie dotyczy” w kolorze przygaszonym. Bez emotikonów, bez wykrzykników.
- Nazewnictwo spójne ze słownikiem z rozdziału 4.

### 10.8 Biblioteka kontrolek

Aplikacja używa gotowej, utrzymywanej biblioteki kontrolek WPF o nowoczesnym wyglądzie, z obsługą motywu jasnego i ciemnego i dobrą tabelą danych. Agent sprawdza aktualność, licencję i stan utrzymania przed wyborem (do rozważenia: WPF UI, MahApps.Metro, ModernWpf, Material Design In XAML) i opisuje wybór w notatce decyzji. Własne kontrolki powstają tylko tam, gdzie biblioteka ich nie daje: mozaika wypraw, próbnik kamuflażu, pasek spójności oraz wykresy, jeśli wybrana biblioteka wykresów nie wystarczy.

---

## 11. Ograniczenia technologiczne i konwencje

Obowiązkowe:

- język C#, interfejs WPF, aplikacja desktopowa dla Windows (NFR-01), aktualna wersja .NET z długim wsparciem dostępna w momencie startu (agent weryfikuje),
- praca lokalna i offline, bez serwera (NFR-02),
- dane w otwartym formacie możliwym do odczytu poza aplikacją (NFR-03).

Wybory pozostawione agentowi, do opisania w krótkich notatkach decyzji w `docs/decyzje/` (jedna strona na decyzję: kontekst, wybór, alternatywy, konsekwencje): architektura, sposób przechowywania danych, biblioteka kontrolek, biblioteka wykresów, biblioteka do plików Excel, sposób instalacji.

Jakość:

- automatyczne testy obliczeń (rozdział 8), importu (8.2) i migracji danych,
- logika obliczeń i importu oddzielona od okien, aby testować ją bez uruchamiania interfejsu,
- brak ostrzeżeń kompilatora w nowym kodzie, włączona analiza statyczna,
- licencje zależności umożliwiające bezpłatne użycie (MIT, Apache-2.0, BSD); lista zależności z licencjami w repozytorium.

Konwencje kodu (propozycja do potwierdzenia, Q14):

- interfejs po polsku, identyfikatory w kodzie po angielsku, opisowe, bez nazw jednoliterowych,
- komentarze w kodzie po polsku, krótkie, informacyjne, bez kropki na końcu i bez emotikonów,
- publiczne metody z krótkim opisem w komentarzu dokumentacyjnym XML, napisanym potocznie i zwięźle.

Repozytorium: git, katalogi `src/`, `tests/`, `docs/` (ten dokument jako `docs/wymagania.md`, `docs/dane-startowe/Dane_zrodlowe_Kwatermistrz.xlsx`, `docs/decyzje/`), plik `CHANGELOG.md` i `README.md` z opisem uruchomienia.

---

## 12. Plan etapów

Każdy etap kończy się działającą aplikacją i raportem agenta: które identyfikatory wymagań zrealizowano, które testy przechodzą, co odbiega od dokumentu.

| Etap | Zakres | Wynik dla użytkownika | Kryterium zakończenia |
|---|---|---|---|
| 0 Fundament | Szkielet okna, nawigacja, motywy, słowniki z wartościami startowymi, rekonstrukcje z przełącznikiem i konfigurowalne kategorie, sloty i szablony karty, zapis i odczyt danych, kopia zapasowa i przywracanie, ustawienia, kreator pierwszego uruchomienia (w tym etapie tylko wariant pusty; pozostałe warianty dochodzą z FR-1101, FR-1403, FR-1404) | Aplikacja uruchamia się pusta, słowniki i rekonstrukcje da się edytować, kopia działa | FR-1001–1003, FR-1006, FR-1107, FR-1202–1203, FR-1301–1306, FR-1401–1402, FR-1407, NFR-03, NFR-05, NFR-13–15 |
| 1 Wyposażenie | Katalog, kategorie, karta przedmiotu, komplety, montaż na nośnikach, zdjęcia, miejsca, edycja zbiorcza, import wyposażenia z podglądem i znacznikiem „z importu, do przeglądu” | Cały katalog z arkuszy wyposażenia w aplikacji, ładownice przypięte do nośników | FR-201–211, FR-216, FR-1101–1103 (część wyposażenia), FR-1110, 106 przedmiotów zaimportowanych |
| 2 Repliki | Lista replik, karta techniczna z trzema stanami, pomiary, pozycje kosztowe, modyfikacje, zadania, import replik i karty technicznej | Wszystkie repliki z kartą, kosztami rozbitymi na pozycje i otwartymi zadaniami | FR-301–305, FR-401–409, FR-501–505, FR-508, FR-601–602, FR-604; liczby z 8.2 dla replik |
| 3 Wyprawy i zestawy | Zestawy ze slotami i wariantami, dodawanie wypraw, liczniki historyczne i dzień zerowy, pulpit z mozaiką | Dodanie wyprawy w mniej niż 30 s, liczniki wyliczane, zestawy z 15 kolumn arkusza zaimportowane | FR-701–709, FR-801–809, FR-101–103, FR-910 (89 = 89 = 89) |
| 4 Raporty | Raporty z 7.9, kontrola spójności, eksport do Excela i CSV, wyszukiwanie globalne, widok „Do uzupełnienia” | Odpowiedniki obu arkuszy plus nowe zestawienia | FR-901–904, FR-908, FR-911, FR-1106, FR-1201, FR-1204; wszystkie liczby z 8.2 |
| 5 Dopracowanie | Lista pakowania, porównanie kart, wykresy, wydruki, instalator, import z mapowaniem kolumn, pakiety konfiguracji i szablony, przegląd dostępności i wydajności | Aplikacja gotowa do codziennego użytku przez autora i kolegów | wszystkie wymagania [S], FR-1404, NFR-04, NFR-08, NFR-10; kolega bez wiedzy technicznej instaluje aplikację i konfiguruje swoją rekonstrukcję bez pomocy |

Wymagania [C] nie wchodzą do wersji 1.0, o ile użytkownik nie zdecyduje inaczej.

---

## 13. Uwagi o danych źródłowych (do rozstrzygnięcia przy imporcie)

Wszystko poniżej wynika z analizy obu plików. Import ma te sytuacje wykrywać i pokazywać w podglądzie (FR-1102), a nie poprawiać po cichu. Według autora dane z arkuszy są dawne i przykładowe, wymagają wielu zmian i aktualizacji, więc każdy zaimportowany rekord dostaje znacznik „z importu, do przeglądu” (FR-1110), a liczby w 8.2 sprawdzają poprawność importu, nie aktualność danych.

### 13.1 Błędy w obliczeniach arkusza

- Suma kosztu „ROSJA” w tabeli umundurowania obejmuje wiersze Warstwa 5 WKBO do KZS i pomija ostatni wiersz: Gorka 5 ATACS-FG, czyli model munduru („5” to numer modelu), cena 400,00 zł. Suma ogólna ten wiersz zawiera. Efekt: 5 851,89 zł zamiast 6 251,89 zł (BR-11).
- Pozycja DKWS (0 wypraw, bez ceny) nie należy do żadnej z trzech stron. Wcześniejsze trzy pozycje EMR (8 wypraw) są w sumie ogólnej, ale też bez strony.
- Wypraw w roku bieżącym nie wpisano, jest to różnica do sumy 89 (BR-07).
- Etykiety „Do końca roku 2022” i „Do końca roku 2023” sugerują sumy narastające, ale liczby 10, 30, 17, 19, 13 sumują się do 89 tylko jako wartości roczne. Aplikacja traktuje je jako roczne (założenie A3).

### 13.2 Rozbieżności liczników użyć między plikami

Liczby użyć w arkuszu Mundury i w tabeli umundurowania opisują te same mundury, ale nie zawsze się zgadzają. Dopasowanie nazw jest przypuszczalne i wymaga potwierdzenia w podglądzie.

| Mundur w arkuszu Mundury | Pozycja w tabeli umundurowania | Mundury | Tabela |
|---|---|---|---|
| SSO Sumrak, Spektr | SSO Spektr | 2 | 4 |
| SSO KLMK, Palma | Palma | 0 | 2 |
| Bars Sumrak, Izlom/Skol | Sumak Izlom | 1 | 2 |
| ANA Combat Pants + Ars Arma Combat Shirt, ATACS (komplet) | Combat Ana/Ars Arma ATACS-FG | 5 | 7 |
| Bars Combat Shirt EMR + VKBO 3.1 Spodnie (komplet) | Combat Bars EMR/VKBO 3.1 | 0 | 1 |
| brak odpowiednika | Gorka 5 ATACS-FG | – | 4 |

Zgodne (przypuszczalnie te same): WKBO Demisezonka i Warstwa 5 WKBO (7), Bars Sumrak ATACS i Sumrak ATACS-FG (6), VSO Partizan i Partizan SS Lieto (4), Splav Górka 3 i Gorka 3 Splav (2), WKBO 6Sh122 (1), SRVV Triarius (1), ZSRR KZS (0).

Według autora liczniki użyć mundurów w arkuszu Mundury dawno przestały być aktualne. Reguła: licznik historyczny munduru pochodzi wyłącznie z tabeli umundurowania (sumuje się do 89 wypraw). Wartości z arkusza Mundury nie są importowane jako liczniki; podgląd importu pokazuje je tylko informacyjnie. Dla pozostałych kategorii (Kamizelki, Pasoszelki, Pasy, Czapki, Hełmy) licznik historyczny to wartość z kolumny „Liczba użyć”, oznaczona „do weryfikacji”, bo powstała tą samą ręczną metodą.

### 13.3 Duplikaty i konflikty w karcie technicznej

- SA-J02 Edge (AK-74 Wood) jest w dwóch kolumnach. Jedna prawie pusta (stan „Do dokończenia”, `??` w polach pomiarowych), druga pełna (stan „Wyśmienity”).
- RPKS jest w dwóch kolumnach: jedna prawie pusta (stan „Stock”), druga z częściami „Oryginalne” (stan „Do zrobienia”).
- LCT AK-74: kolumna „LCT AK-74 (AK-74M)” wygląda na szkic (stan „Do dokończenia”, `??`), a „LCT AK-74 (AK-74)” ma dane. Lista replik zawiera jedną pozycję „LCT AK-74 (AK-74)”.
- „Custom (AK-74M Plum)” w karcie vs „Custom (AK-74 Plum)” na liście replik.
- „CXP-APE (MASADA ACR)” w karcie z danymi; lista replik ma „CXP-APE-L (MASADA ACR)” i „CXP-APE (Bushmaster ACR)”, a ich kolumny w karcie są bez danych.
- SA-G12 (G36C): według autora nadal jest w jego posiadaniu, leży w częściach i jest przeznaczona do sprzedaży po uporządkowaniu. Arkusz ma ją na liście sprzedanych (1 wyprawa) i w karcie technicznej z dwiema notatkami awarii (złamana głowica, złamana dysza). Import ustawia status „do sprzedaży” i stan „W częściach”, zachowuje 1 wyprawę i oba zadania.

Import proponuje scalenie po nazwie, pokazuje warianty obok siebie i pozwala wybrać kolumnę główną oraz dołożyć pola z drugiej.

### 13.4 Niespójne słownictwo

- Rola (teren): `SZTURM`, `SZTURM/DMR`, `DMR`, `LKM` w pierwszym arkuszu oraz `WSPARCIE`, `SZTURM/CQB`, `DMR/SZTURM/CQB` w drugim. `LKM` i `WSPARCIE` oznaczają to samo.
- Typ magazynka: `AK`, `AK 74M`, `AK 74`, `AK 12`, `AK-47`, `AKM`, `RPK`, `RPK-74/Drum`, `VSS/VAL` i `WSS/Wał`, `PP-19 Vityaz` i `PP Vityaz`, `SWD`, `PKM`, `STANAG`, `Beryl`.
- Wzory kamuflażu: `ATACS`, `A-TACS`, `ATACS-FG`; `Spektr` i `Spectre`; `Surpat Aride Savanna`, `SURPAT SAVANNA`, `Aride`; `EMR`, `Chiński EMR`, `Liuks EMR`, `Ręczny EMR Wiosna/Jesień`. `Olive` i `Green` to kolory jednolite, nie wzory.
- Spacje na końcu wartości (`PKM `, `KZS `, `Ideał `), niewidoczny znak po „Sprawdzony w boju”. Import przycina i normalizuje.
- Brak części: `brak` (np. gumka hop-up w SA-G12) jest wartością (części nie ma) i nie może zostać potraktowany jako „nieznane”.

### 13.5 Znaczniki braków w karcie technicznej

Karta ma 23 kolumny po 26 parametrów. Spośród 598 pól: 114 pustych, 30 znaczników `?`/`??`/`???` (nieznane) i 27 „Nie dotyczy HPA”. Mapowanie na trzy stany z FR-404: `?`, `??`, `???` na nieznane; „Nie dotyczy HPA” na nie dotyczy i ustawienie napędu HPA dla repliki (w danych: VSS Wintorez i SA-H20 (HK 416)); puste na „nie uzupełniono”.

### 13.6 Elementy zestawów bez odpowiednika w katalogu

Import tworzy przedmioty-szkice ze znacznikiem „do uzupełnienia” i proponuje dopasowania:

- bez odpowiednika: Rękawice (Ratnik, Mechanix), Gogle (6b50), Buty (Ratnik czarne, Pentagon), Broń boczna (Grach), Plecak „6sh117 25L”, wpis „giurza” w wariancie munduru „Vkbo 3.1+giurza” (do zidentyfikowania),
- do potwierdzenia: Hełm „Tor-L” w zestawach i „Tor H” w katalogu (Q6); „Skanda Light” i „Skanda Heavy” jako konfiguracje przedmiotu „A-18 Skanda” (FR-208); „ANA Mk2” jako pasoszelki „ANA MK 2”; „Pas taktyczny MC MBC” i „Pas taktyczny Wartech” jako pozycje arkusza Pasy; „Minimap” jako panel szturmowy Ars Arma,
- prawdopodobna literówka: wpisy „Smiersz” i „Śmierdź” w wierszu Pas trzech szkiców zestawów („Oldgen zimno”, dwa razy „Oldgen ciepło”); katalog ma pasoszelki „Smiersz Newgen”.

### 13.7 Montaż ładownic na nośnikach (kolumna Komentarze)

| Komentarz | Pozycje | Proponowany nośnik |
|---|---|---|
| `6b46` | 1–7 | Technikom 6b46 „BNZ” lub Technikom 6b46 Old Gen (niejednoznaczne, import prosi o wybór) |
| `Pas MBC` | 8–14 | MBC, Pas uniwersalny z szelkami |
| `6sh117 Zjuratkul` | 15–23 | Zjuratkul, Kamizelka 6Sh117 |
| `6sh117 Technikom` | 24–39 | Technikom, Kamizelka 6Sh117 |
| `A-18 Skanda` | 40–47 | Demych/ARS ARMA, A-18 Skanda |
| `Na 6b45` | 48–52 | Technikom 6B45 |
| `Pas SWD` | 53–60 | Wartech, Pas uniwersalny z szelkami (komentarz „SWD”) |

Razem 7 + 7 + 9 + 16 + 8 + 5 + 8 = 60 ładownic.

### 13.8 Kolumna bez nagłówka z klasą wykonania

W arkuszach Pasy, Nakolanniki i Plecaki ostatnia kolumna nie ma nagłówka. Wartości: „Wojskowe”, „ASG Niskiej Jakości”, „Orgo/Reko”. Trafiają do pola „Pochodzenie / klasa” (FR-210). Według autora „Orgo/Reko” oznacza faktyczny, oryginalny sprzęt wojskowy do rekonstrukcji, a nie podróbkę ani reprodukcję. Różnica między „Wojskowe” a „Orgo/Reko” do potwierdzenia (Q15).

---

## 14. Założenia i decyzje do potwierdzenia

### 14.1 Założenia przyjęte w dokumencie

- A1 „Wyprawa” to „strzelanka” z arkuszy.
- A2 Każda wyprawa ma jedną replikę główną. Suma wypraw wg replik równa się liczbie wypraw.
- A3 Liczby 10, 30, 17, 19, 13 to wyprawy w kolejnych latach, a nie sumy narastające.
- A4 Strona jest cechą przedmiotu, repliki lub zestawu, nie wzoru kamuflażu.
- A5 Jedna osoba, jeden komputer z Windows, waluta PLN.
- A6 Tabela „Szpej” dotyczy mundurów i kompletów, nie samych wzorów, więc raport umundurowania jest po mundurach (grupowanie po wzorze jako dodatek).
- A7 Dzień zerowy to dzień importu.
- A8 Dane z lat sprzed aplikacji istnieją tylko jako sumy i tak są przenoszone (liczniki historyczne), bez tworzenia sztucznych wypraw.
- A9 Replika „do sprzedaży” jest wyłączona ze średnich i sum kosztów bieżących, ale zachowuje wyprawy i zadania.
- A10 Aplikacja jest ogólna: dane z arkuszy to tylko jedna z możliwych rekonstrukcji (7.13, NFR-13).
- A11 Dane z arkuszy to dawne dane przykładowe; po imporcie wymagają przeglądu i aktualizacji.
- A12 Z aplikacji korzysta wielu niezależnych użytkowników (autor i koledzy z innych jednostek), każdy z własnymi danymi. Reguły obliczeń są wspólne (NFR-14).

### 14.2 Pytania do autora

Odpowiedzi autora z 2026-10-21 są wpisane. Agent zadaje tylko pytania o statusie „otwarte” i przyjmuje podaną odpowiedź domyślną, jeśli autor jej nie zmieni.

| Nr | Pytanie | Odpowiedź autora lub domyślna | Status |
|---|---|---|---|
| Q1 | Co znaczy „Orgo/Reko” w kolumnie bez nagłówka? | Faktyczny, oryginalny sprzęt wojskowy do rekonstrukcji, nie podróbka ani reprodukcja | rozstrzygnięte |
| Q2 | Co znaczy serduszko przy pomiarach energii? | Idealna i maksymalna wartość energii: znacznik „na maksimum” i energia docelowa na replice (FR-411) | rozstrzygnięte |
| Q3 | Co znaczy „LAS” w slocie „Broń LAS”? | Slot „Broń szturmowa (LAS)” | otwarte |
| Q4 | Co wchodzi w koszt umundurowania? Jak liczyć mieszankę wzorów? | Sam mundur, czyli góra i dół, chyba że dane stanowią inaczej. Mieszanka: zaliczana do pierwszego elementu | koszt rozstrzygnięty, mieszanka domyślnie |
| Q5 | Czy liczyć repliki dodatkowe i boczne w wyprawach? | Nie; liczy się replika główna, boczna jest w składzie zestawu | otwarte |
| Q6 | „Tor-L” w zestawach i „Tor H” w katalogu to ten sam hełm? | Ten sam, literówka w zestawach | otwarte |
| Q7 | SA-G12 (G36C): sprzedana czy posiadana? | Posiadana, w częściach, do sprzedania po uporządkowaniu | rozstrzygnięte |
| Q8 | Która kolumna karty jest właściwa dla SA-J02 Edge, RPKS i LCT AK-74? | Ta z pełniejszymi danymi | otwarte |
| Q9 | Czy wartości energii 1,9 / 2,2 / 2,6 J są limitami z ostrzeżeniem? | To idealne, maksymalne wartości docelowe konkretnych replik (serduszko). Energia docelowa na replice, ostrzeżenie po przekroczeniu (FR-411) | rozstrzygnięte |
| Q10 | Czy nazwa „Kwatermistrz” zostaje? | Zostaje | otwarte |
| Q11 | Czy „Wyśmienity” jest wyżej niż „Ideał – sprawdzony w boju”? Czy podpowiadać „sprawdzony w boju”? | Kolejność jak w arkuszu, bez podpowiedzi | otwarte |
| Q12 | Do której strony zaliczyć wcześniejsze EMR (8 wypraw) i DKWS? | EMR do Rosji, DKWS bez strony | otwarte |
| Q13 | Czy katalog ma obejmować sprzęt NATO i polski oraz inne rekonstrukcje? | Tak: aplikacja jest ogólna i obsługuje wiele rekonstrukcji (7.13) | rozstrzygnięte |
| Q14 | Czy konwencje kodu z rozdziału 11 (komentarze po polsku, opisy XML) obowiązują? | Tak | otwarte |
| Q15 | Czym „Wojskowe” różni się od „Orgo/Reko” w kolumnie klasy wykonania? | Osobne wartości słownika, bez interpretacji | otwarte |
| Q16 | Jak nazwać rekonstrukcje tworzone przy imporcie? | Po jednej na stronę (Rosja, Polska, NATO), do zmiany w podglądzie importu | otwarte |
| Q17 | Czy dołączać dane przykładowe z arkuszy do instalatora dla kolegów? | Nie; dane autora tylko w repozytorium, do ręcznego importu i testów | otwarte |
| Q18 | Na jakich zasadach aplikacja jest udostępniana kolegom (licencja, czy kod jest publiczny)? | Użytek prywatny w gronie autora i kolegów, kod niepubliczny; zależności muszą pozwalać na redystrybucję | otwarte |

---

## 15. Instrukcja dla agenta kodującego

### 15.1 Sposób pracy

1. Przeczytaj cały dokument i plik danych. Zacznij od pytań o statusie „otwarte” z 14.2: zadaj je jednym zestawem z odpowiedziami domyślnymi i nie wstrzymuj prac, które od nich nie zależą.
2. Zapisz notatki decyzji technologicznych (rozdział 11) przed pierwszym kodem.
3. Pracuj etapami z rozdziału 12. Nie zaczynaj następnego etapu przed spełnieniem kryterium poprzedniego.
4. W każdym etapie najpierw napisz testy akceptacyjne z rozdziału 8 dla danego zakresu, potem implementację.
5. Nie dodawaj funkcji spoza dokumentu. Pomysły zapisuj na liście „Propozycje” w `docs/`, do decyzji autora.
6. Po każdym etapie: raport (spełnione identyfikatory, wynik testów, odchylenia od dokumentu, otwarte pytania), aktualizacja `CHANGELOG.md`, opis uruchomienia w `README.md`.
7. Dane użytkownika są cenne. Żadnej operacji nieodwracalnej bez kopii zapasowej. Import zawsze przez podgląd.
8. Gdy znaczenie danych z arkuszy jest niejasne, zapytaj. Nie zgaduj.
9. Interfejs buduj według rozdziału 10, łącznie z tekstami.

### 15.2 Prompt startowy do wklejenia w Claude Code lub Codex

```
Zbuduj aplikację desktopową dla Windows w C# i WPF według dokumentacji w docs/wymagania.md.
Dane startowe są w docs/dane-startowe/Dane_zrodlowe_Kwatermistrz.xlsx.

Najpierw przeczytaj całą dokumentację i plik danych. Następnie:
1. Zadaj mi wszystkie pytania o statusie „otwarte” z rozdziału 14.2 jednym zestawem, z proponowanymi odpowiedziami domyślnymi.
2. Zapisz decyzje technologiczne w docs/decyzje/ (architektura, przechowywanie danych, biblioteka kontrolek, wykresy, pliki Excel).
3. Przedstaw plan Etapu 0 i Etapu 1 i zacznij od testów z rozdziału 8.

Pracuj etapami z rozdziału 12. Po każdym etapie uruchom testy, zaktualizuj CHANGELOG.md i wypisz, które identyfikatory wymagań są spełnione oraz co odbiega od dokumentu.
Aplikacja ma służyć też kolegom z innych jednostek: pierwsze uruchomienie jest puste, dane autora tylko do importu, a reguły obliczeń są stałe (rozdział 7.14, NFR-14).
Nie wpisuj na stałe do programu żadnych treści rosyjskich ani airsoftowych (rozdział 7.13): to dane konfiguracyjne.
Nie dodawaj funkcji spoza dokumentu. Interfejs po polsku, zgodnie z rozdziałem 10.
```

---

## Załącznik A. Zawartość pliku danych

`Dane_zrodlowe_Kwatermistrz.xlsx` to wyciąg 1:1 z obu plików Numbers, z ujednoliconym układem. Nic nie jest w nim poprawiane. Według autora to dawne dane przykładowe.

| Arkusz | Zawartość | Użycie w imporcie |
|---|---|---|
| README | opis pliku | – |
| Repliki | 18 replik posiadanych: wyprawy, magazynek, rola, strona, koszt, cena oryginalna, dodatki, składniki kosztu z formuł | repliki, liczniki historyczne, pozycje kosztowe (FR-508) |
| Repliki_sprzedane | 9 replik sprzedanych z liczbą wypraw | repliki sprzedane, liczniki historyczne |
| Umundurowanie | 20 pozycji „Szpej”, 3 wcześniejsze EMR, podsumowania wg stron z arkusza | mundury i komplety, liczniki historyczne, test sum |
| Wyprawy_wg_lat | data pierwszej wyprawy i liczby wypraw w latach | liczniki roczne, data pierwszej wyprawy |
| Karta_techniczna | jedna replika na wiersz (transpozycja macierzy), 20 parametrów, test, rola, magazynki, dwie notatki | karty techniczne, pomiary, zadania |
| Wyposazenie | 106 przedmiotów ze wszystkich kategorii: kategoria, podkategoria, firma, nazwa, strona, wzór, miejsce, liczba użyć, komentarz, kolumna bez nagłówka | przedmioty, montaż, liczniki historyczne |
| Zestawy | 15 zestawów, jeden na wiersz, komórki slotów w oryginalnym zapisie wariantów | zestawy, sloty, warianty |

## Załącznik B. Słowniki startowe

- Strony: Rosja, Polska, NATO.
- Role replik: Szturm, DMR, Wsparcie, CQB, Boczna. Aliasy: LKM → Wsparcie.
- Napęd: elektryczna (AEG), HPA. Użytkownik dodaje własne (np. sprężynowa, gazowa, CO2) wraz z szablonem karty technicznej.
- Stany replik: Stock, Do zrobienia, Do dokończenia, Ideał, Ideał – sprawdzony w boju, Wyśmienity, W częściach.
- Statusy własności (repliki i przedmioty): posiadana, do sprzedaży, sprzedana, wycofana.
- Miejsca przechowywania: Piwnica, Dom.
- Pochodzenie / klasa: Orgo/Reko (faktyczny, oryginalny sprzęt wojskowy do rekonstrukcji), Wojskowe, ASG niskiej jakości.
- Rekonstrukcje: tworzone przy imporcie po jednej na stronę (Rosja, Polska, NATO), nazwy zmienia użytkownik (Q16). Dalsze dodaje użytkownik.
- Rodzaje pozycji kosztowych: Zakup repliki, Ulepszenie, Serwis, Zwrot / rabat / sprzedaż części, Inne.
- Rodzaje zadań: Awaria, Do zrobienia, Do kupienia.
- Kategorie: Czapki, Hełmy, Szalokominiarki, Mundury, Kamizelki (nośnik), Pasoszelki i chest rigi (nośnik), Pasy (nośnik), Nakolanniki, Plecaki (nośnik), Panele szturmowe (nośnik), Ładownice, Rękawice, Gogle, Buty.
- Sloty zestawu: Mundur, Nakolanniki, Kominiarka, Rękawice, Hełm, Czapka, Gogle, Buty, Kamizelka, Plecak, Pas, Broń DMR, Broń szturmowa, Broń wsparcia, Broń boczna.
- Wzory kamuflażu występujące w danych: EMR, EMR Wiosna/Jesień, Chiński EMR, Liuks EMR, Multicam, Multicam Tropic G2, Multicam G3, A-TACS (alias ATACS), A-TACS FG (alias ATACS-FG), Spektr (alias Spectre, Spektr SKWO), VSR-98, Woodland, KZS, Palma, Izlom / Skol, Surpat Aride Savanna (aliasy Aride, Surpat Savanna), SS Lieto (alias Gaszony), Flecktarn, WZ.93, WZ.2019 124T, WZ.2019 124P, DKWS, Olive i Green (kolory jednolite).
- Typy magazynków: wartości z arkuszy z 13.4; ujednolicenie w podglądzie importu.

---

## Historia wersji dokumentu

| Wersja | Data | Zmiany |
|---|---|---|
| 0.1 | 2026-10-21 | Pierwszy szkic |
| 0.2 | 2026-10-21 | Odpowiedzi autora: Orgo/Reko (oryginalny sprzęt wojskowy), serduszko (idealna, maksymalna energia, energia docelowa na replice), G36C (posiadana, w częściach, do sprzedaży), nieaktualne liczniki mundurów, koszt munduru = góra i dół. Nowe: rekonstrukcje i zasada ogólności (7.13, NFR-13), użycie przez wielu niezależnych użytkowników (7.14, NFR-14, NFR-15), dane z arkuszy jako przykładowe (FR-1110), edycja zbiorcza (FR-216), „Jak liczymy” (FR-911), status „do sprzedaży”, stan „W częściach”, pytania Q15–Q18 |
