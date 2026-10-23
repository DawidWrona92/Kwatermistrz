using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Import;

/// <summary>Część importera: arkusz „Umundurowanie” (mundury i komplety, BR-09/BR-10, 13.2) oraz arkusz „Zestawy” (FR-701..FR-706, 13.6).</summary>
public partial class ImporterKwatermistrz
{
    private static void ImportujUmundurowanie(KontekstImportu k, List<ArkuszXlsx> arkusze)
    {
        var arkusz = ZnajdzArkusz(arkusze, "Umundurowanie");
        if (arkusz == null)
        {
            k.Podglad.DodajKonflikt(RodzajKonfliktu.DoPotwierdzenia, "Nie znaleziono arkusza „Umundurowanie”", "plik");
            return;
        }
        var naglowek = Naglowek(arkusz);
        if (naglowek == null)
            return;
        int Kolumna(string wzorzec)
        {
            for (var i = 0; i < naglowek.Komorki.Count; i++)
            {
                var c = naglowek.Komorka(i);
                if (c != null && Normalizacja.TekstDoPorownania(c).Contains(wzorzec))
                    return i;
            }
            return -1;
        }
        var kolPozycja = Kolumna("pozycja");
        var kolWyprawy = Kolumna("wyprawy");
        var kolKoszt = Kolumna("koszt");
        var kolFormula = Kolumna("skladniki");
        var kolStrona = Kolumna("strona");

        var kategoriaMundury = IdKategorii(k, "Mundury");
        // kategoria „Mundury” to definicja mundurów (BR-09) — oznaczamy, żeby kontrola spójności nie zgadywała po nazwie
        k.Dane.Slowniki.Kategorie.First(x => x.Id == kategoriaMundury).CzyMundur = true;
        // liczniki z arkusza Mundury są nieaktualne wg autora (13.2) — zerujemy; obowiązują tylko wartości z tabeli umundurowania poniżej
        foreach (var stary in k.Dane.Przedmioty.Where(p => p.KategoriaId == kategoriaMundury))
        {
            stary.LicznikHistoryczny = 0;
            stary.LicznikHistorycznyDoWeryfikacji = true;
        }
        // dopasowujemy tylko do pozycji katalogowych (bez szkiców) — szkic nie może być kandydatem ani wynikiem (13.2)
        var wszystkieMundury = k.Dane.Przedmioty.Where(p => p.KategoriaId == kategoriaMundury && !p.DoUzupelnienia).ToList();
        var licznik = 0;
        var czySekcjaPodsumowan = false;

        foreach (var wiersz in arkusz.Wiersze.SkipWhile(w => w.Numer <= naglowek.Numer))
        {
            var pozycja = Normalizacja.Przytnij(wiersz.Komorka(kolPozycja));
            if (pozycja == null)
                continue;
            var pozycjaNorm = Normalizacja.TekstDoPorownania(pozycja);
            // sekcja „Podsumowania z arkusza” i wszystko po niej pomijamy — aplikacja sama liczy sumy (BR-11)
            if (pozycjaNorm.StartsWith("podsumowania", StringComparison.Ordinal))
            {
                czySekcjaPodsumowan = true;
                continue;
            }
            if (czySekcjaPodsumowan)
                continue;
            // wiersz nagłówkowy grupy („Wcześniejsze umundurowanie…”) bez liczby wypraw — pomijamy
            var wyprawy = CzytnikXlsx.LiczbaCalkowita(wiersz.Komorka(kolWyprawy));
            var stronaTekst = Normalizacja.Przytnij(wiersz.Komorka(kolStrona));
            if (wyprawy == null && stronaTekst == null)
                continue; // wiersz nagłówkowy grupy lub pusty

            // dopasuj do przedmiotu z kategorii Mundury (FR-1103, 13.2) — tekst kandydata zawiera też nazwę i aliasy wzoru (np. „SSO Sumrak” + „Spektr” = „SSO Spektr” z tabeli)
            var dopasowanie = Dopasowanie.Dopasuj(pozycja, wszystkieMundury, p => TekstKandydataMunduru(k, p));
            Przedmiot? mundur;
            if (dopasowanie.Jednoznaczne && dopasowanie.Element != null)
            {
                mundur = dopasowanie.Element;
            }
            else
            {
                // brak odpowiednika — tworzymy przedmiot-szkic (13.6, FR-706)
                mundur = new Przedmiot
                {
                    KategoriaId = kategoriaMundury,
                    Nazwa = pozycja,
                    ZnacznikZImportu = true,
                    DoUzupelnienia = true,
                };
                if (dopasowanie.Niejednoznaczne)
                {
                    var konflikt = k.Podglad.DodajKonflikt(RodzajKonfliktu.Niejednoznaczne,
                        $"Pozycja umundurowania „{pozycja}” pasuje do kilku mundurów — wybierz właściwy",
                        $"Umundurowanie wiersz {wiersz.Numer}");
                    konflikt.Kandydaci = dopasowanie.Kandydaci.Select(p => $"{p.Firma} {p.Nazwa}").ToList();
                    konflikt.DomyslnyWybor = 0;
                    // FR-1102: wybór użytkownika (pominięcie = szkic mimo kandydatów)
                    var wybor = konflikt.Decyzja;
                    if (!konflikt.Pomin && wybor >= 0 && wybor < dopasowanie.Kandydaci.Count)
                        mundur = dopasowanie.Kandydaci[wybor];
                }
                else
                {
                    k.Podglad.DodajKonflikt(RodzajKonfliktu.BrakOdpowiednika,
                        $"Pozycja umundurowania „{pozycja}” nie ma odpowiednika w katalogu — utworzono szkic",
                        $"Umundurowanie wiersz {wiersz.Numer}");
                }
                k.Dane.Przedmioty.Add(mundur);
            }

            // licznik historyczny munduru pochodzi wyłącznie z tabeli umundurowania (13.2, FR-806)
            if (wyprawy != null)
            {
                mundur.LicznikHistoryczny = wyprawy.Value;
                mundur.LicznikHistorycznyDoWeryfikacji = false;
            }

            // koszt: formuła składników lub sama liczba (BR-10: cena góry i dołu kompletu)
            var formula = Normalizacja.Przytnij(wiersz.Komorka(kolFormula));
            var skladniki = Normalizacja.ParsujFormuleKosztu(formula);
            if (skladniki != null)
            {
                foreach (var kwota in skladniki)
                    mundur.Koszty.Add(new PozycjaKosztowa { Kwota = kwota, Opis = "", Semantyka = SemantykaKosztu.Zakup, RodzajId = IdRodzaju(k, SemantykaKosztu.Zakup), Zrodlo = "import" });
            }
            else
            {
                var koszt = CzytnikXlsx.Liczba(wiersz.Komorka(kolKoszt));
                if (koszt != null)
                    mundur.Koszty.Add(new PozycjaKosztowa { Kwota = koszt.Value, Opis = "", Semantyka = SemantykaKosztu.Zakup, RodzajId = IdRodzaju(k, SemantykaKosztu.Zakup), Zrodlo = "import" });
            }

            // strona (Q12: EMR → Rosja, DKWS → Polska; odpowiedź autora 2026-10-22); „brak przypisania w arkuszu” = brak strony
            var stronaZNajleziona = stronaTekst != null && Normalizacja.TekstDoPorownania(stronaTekst) != "brak przypisania w arkuszu";
            if (stronaZNajleziona)
            {
                var w = Model.Slowniki.Znajdz(k.Dane.Slowniki.Strony, stronaTekst!);
                if (w != null && !mundur.StronyId.Contains(w.Id))
                    mundur.StronyId.Add(w.Id);
            }
            else if (Normalizacja.TekstDoPorownania(pozycja).Contains("emr"))
            {
                var rosja = Model.Slowniki.Znajdz(k.Dane.Slowniki.Strony, "Rosja");
                if (rosja != null && !mundur.StronyId.Contains(rosja.Id))
                    mundur.StronyId.Add(rosja.Id);
            }
            else if (Normalizacja.TekstDoPorownania(pozycja).Contains("dkws"))
            {
                var polska = Model.Slowniki.Znajdz(k.Dane.Slowniki.Strony, "Polska");
                if (polska != null && !mundur.StronyId.Contains(polska.Id))
                    mundur.StronyId.Add(polska.Id);
            }
            // DKWS bez strony w arkuszu, ale Q12: DKWS = polskie → powyżej; pozostałe bez strony zostają wspólne

            PrzypiszDoRekonstrukcji(k, mundur.StronyId, mundur.RekonstrukcjeId);
            licznik++;
        }
        k.Podglad.LiczbaRekordow["Pozycje umundurowania"] = licznik;

        // informacja: liczniki z arkusza Mundury nie są importowane (13.2, nieaktualne wg autora)
        k.Podglad.DodajKonflikt(RodzajKonfliktu.Pominiete,
            "Liczniki użyć z arkusza Mundury (suma 29) nie zostały zaimportowane — według autora są nieaktualne; obowiązują liczniki z tabeli umundurowania (suma 89)",
            "Wyposazenie: Mundury");
    }

    /// <summary>Nazwy slotów w kolejności kolumn arkusza „Zestawy” (FR-702).</summary>
    private static readonly string[] SlotyZestawow =
    {
        "Mundur", "Nakolanniki", "Kominiarka", "Rękawice", "Hełm", "Czapka", "Gogle", "Buty",
        "Kamizelka", "Plecak", "Pas", "Broń DMR", "Broń szturmowa", "Broń wsparcia", "Broń boczna",
    };

    private static void ImportujZestawy(KontekstImportu k, List<ArkuszXlsx> arkusze)
    {
        var arkusz = ZnajdzArkusz(arkusze, "Zestawy");
        if (arkusz == null)
        {
            k.Podglad.DodajKonflikt(RodzajKonfliktu.DoPotwierdzenia, "Nie znaleziono arkusza „Zestawy”", "plik");
            return;
        }
        var naglowek = Naglowek(arkusz);
        if (naglowek == null)
            return;
        int Kolumna(string wzorzec)
        {
            for (var i = 0; i < naglowek.Komorki.Count; i++)
            {
                var c = naglowek.Komorka(i);
                if (c != null && Normalizacja.TekstDoPorownania(c).Contains(wzorzec))
                    return i;
            }
            return -1;
        }
        var kolNazwa = Kolumna("nazwa zestawu");
        var kolStrona = Kolumna("strona");
        // kolumny slotów zaczynają się od „Mundur” (indeks 3 w arkuszu)
        var pierwszySlot = Kolumna("mundur");

        var licznik = 0;
        foreach (var wiersz in arkusz.Wiersze.SkipWhile(w => w.Numer <= naglowek.Numer))
        {
            var nazwa = Normalizacja.Przytnij(wiersz.Komorka(kolNazwa));
            var stronaTekst = Normalizacja.Przytnij(wiersz.Komorka(kolStrona));
            if (nazwa == null && stronaTekst == null)
                continue;

            var zestaw = new Zestaw
            {
                Nazwa = nazwa ?? "",
                CzySzkic = nazwa == null || NazwaJestSzkicem(nazwa),
                ZnacznikZImportu = true,
            };
            if (stronaTekst != null)
            {
                var w = Model.Slowniki.Znajdz(k.Dane.Slowniki.Strony, stronaTekst);
                if (w != null)
                    zestaw.StronaId = w.Id;
            }
            // rekonstrukcja zestawu: z jego strony (Q16)
            if (zestaw.StronaId != null)
            {
                var strona = k.Dane.Slowniki.Strony.First(s => s.Id == zestaw.StronaId);
                if (k.RekonstrukcjeWgStrony.TryGetValue(strona.Nazwa, out var rek))
                    zestaw.RekonstrukcjaId = rek.Id;
            }

            // sloty: kolumny od „Mundur” w kolejności SlotyZestawow; SzablonSlotId wiąże slot
            // z szablonem rekonstrukcji po nazwie (BR-09 rozpoznaje mundur po Id slotu szablonu)
            var szablonSloty = zestaw.RekonstrukcjaId != null
                ? k.Dane.Rekonstrukcje.FirstOrDefault(r => r.Id == zestaw.RekonstrukcjaId)?.SzablonSlotow
                : null;
            for (var s = 0; s < SlotyZestawow.Length; s++)
            {
                var komorka = Normalizacja.Przytnij(wiersz.Komorka(pierwszySlot + s));
                var szablonSlot = szablonSloty?.FirstOrDefault(x =>
                    Normalizacja.TekstDoPorownania(x.Nazwa) == Normalizacja.TekstDoPorownania(SlotyZestawow[s]));
                var slot = new SlotZestawu { Nazwa = SlotyZestawow[s], Kolejnosc = s, SzablonSlotId = szablonSlot?.Id };
                if (komorka != null)
                {
                    var warianty = komorka.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    var indeksDomyslny = 0;
                    for (var w = 0; w < warianty.Length; w++)
                    {
                        var wariantTekst = warianty[w].Trim();
                        if (Normalizacja.TekstDoPorownania(wariantTekst) == "puste")
                        {
                            slot.Warianty.Add(new WariantSlotu { CzyPuste = true });
                            continue;
                        }
                        var elementy = wariantTekst.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        var wariant = new WariantSlotu();
                        foreach (var elementTekst in elementy)
                        {
                            var element = RozwiazElement(k, elementTekst, SlotyZestawow[s], wiersz.Numer);
                            if (element != null)
                                wariant.Elementy.Add(element);
                        }
                        // slot „Mundur”: wariant z kilku przedmiotów = komplet (FR-206, BR-09) — pierwszy element staje się kontenerem kompletu, pozostałe dostają KompletId (bez osobnego rekordu-kontenera, 15 pozycji w arkuszu)
                        var przedmiotyWVariancie = wariant.Elementy.Where(e => e.Typ == TypElementu.Przedmiot).ToList();
                        if (SlotyZestawow[s] == "Mundur" && przedmiotyWVariancie.Count > 1)
                        {
                            var kontener = k.Dane.Przedmioty.First(p2 => p2.Id == przedmiotyWVariancie[0].ElementId);
                            foreach (var el in przedmiotyWVariancie.Skip(1))
                            {
                                var przedmiot = k.Dane.Przedmioty.FirstOrDefault(p2 => p2.Id == el.ElementId);
                                if (przedmiot != null && !przedmiot.DoUzupelnienia)
                                    przedmiot.KompletId = kontener.Id;
                            }
                            wariant.Elementy.RemoveAll(e => e.Typ == TypElementu.Przedmiot && e.ElementId != kontener.Id);
                            wariant.Elementy.Add(new ElementWariantu { Typ = TypElementu.Komplet, ElementId = kontener.Id });
                        }
                        slot.Warianty.Add(wariant);
                        if (w == 0)
                            indeksDomyslny = slot.Warianty.Count - 1;
                    }
                    if (slot.Warianty.Count > 0)
                        slot.WariantDomyslnyId = slot.Warianty[indeksDomyslny].Id;
                }
                zestaw.Sloty.Add(slot);
            }
            k.Dane.Zestawy.Add(zestaw);
            licznik++;
        }
        k.Podglad.LiczbaRekordow["Zestawy"] = licznik;
    }

    /// <summary>„Oldgen zimno/ciepło” bez innych slotów to szkice zawierające tylko pas (13.6).</summary>
    /// <summary>Czy nazwa zestawu to szkic (FR-706): jawne znaczniki „?”/„TODO”/pusta nazwa po myślnikach.</summary>
    private static bool NazwaJestSzkicem(string nazwa)
    {
        var t = Normalizacja.TekstDoPorownania(nazwa);
        return t is "?" || t == "todo" || t == "tbd" || t.EndsWith('?');
    }

    /// <summary>Tekst kandydata munduru do dopasowania (13.2): firma, nazwa (z aliasami w nawiasach, np. „6b46 „BNZ””), nazwa i aliasy wzoru.</summary>
    private static string TekstKandydataMunduru(KontekstImportu k, Przedmiot p)
    {
        var wzor = p.WzorId != null ? k.Dane.Slowniki.Wzory.FirstOrDefault(w2 => w2.Id == p.WzorId) : null;
        var aliasyWzroru = wzor != null ? string.Join(" ", wzor.Aliasy) : "";
        var aliasyNazwy = "";
        var idx = p.Nazwa.IndexOf('„');
        if (idx >= 0)
            aliasyNazwy = p.Nazwa[idx..].Replace("„", " ").Replace("”", " ");
        return $"{p.Firma} {p.Nazwa} {aliasyNazwy} {wzor?.Nazwa} {aliasyWzroru}";
    }

    /// <summary>Słowa sezonowe i potoczne z nazw w arkuszu Zestawy (13.6) — nie biorą udziału w dopasowaniu ani nie zmieniają sensu („Mundur letni WKBO” = WKBO Mundur Polowy).</summary>
    private static readonly HashSet<string> SlowaNeutralne = new(StringComparer.Ordinal)
        { "letni", "letnia", "zimny", "zimno", "zimowa", "ciepły", "ciepło", "ciepla", "lato", "zima" };

    /// <summary>Usuwa wystąpienia słowa jako całych słów (granice słowa) — „lato” nie rusza „latawiec”.</summary>
    private static string UsunSlowoNaGranicy(string tekst, string slowo)
    {
        var start = 0;
        while ((start = tekst.IndexOf(slowo, start, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var przedOk = start == 0 || !char.IsLetterOrDigit(tekst[start - 1]);
            var poOk = start + slowo.Length >= tekst.Length || !char.IsLetterOrDigit(tekst[start + slowo.Length]);
            if (przedOk && poOk)
            {
                tekst = tekst.Remove(start, slowo.Length).Insert(start, " ");
                start++;
            }
            else
                start += slowo.Length;
        }
        return tekst;
    }

    /// <summary>Rozwiązuje nazwę elementu w komórce wariantu na element katalogu: replika albo przedmiot z kategorii slotu; brak = szkic (FR-706, 13.6). Dopasowanie po słowach kluczowych z tolerancją n-1 (jak przy mundurach); szkice nie są kandydatami.</summary>
    private static ElementWariantu? RozwiazElement(KontekstImportu k, string tekst, string nazwaSlotu, int numerWiersza)
    {
        // aliasy potoczne (13.6): ochraniacze = nakolanniki, „Smiersz” = pas
        var tekstDopasowany = tekst
            .Replace("ochraniacze", "nakolanniki", StringComparison.OrdinalIgnoreCase)
            .Replace("smiersz", "pas", StringComparison.OrdinalIgnoreCase);
        // bez słów sezonowych (na granicach słów — „letnia” nie rusza „letniej”, ale też usuwamy osobno)
        foreach (var slowo in SlowaNeutralne)
            tekstDopasowany = UsunSlowoNaGranicy(tekstDopasowany, slowo);

        // sloty broni → tylko repliki
        var kategoriaSlotu = KategoriaDlaSlotu(k, nazwaSlotu);
        if (kategoriaSlotu == null)
        {
            var dopasowanieRepliki0 = Dopasowanie.Dopasuj(tekstDopasowany, k.Dane.Repliki, r =>
                r.Prototyp != null ? $"{r.ModelProducenta} {r.Prototyp}" : r.ModelProducenta);
            if (dopasowanieRepliki0.Jednoznaczne && dopasowanieRepliki0.Element != null)
                return new ElementWariantu { Typ = TypElementu.Replika, ElementId = dopasowanieRepliki0.Element.Id };
            return TworzSzkicElementu(k, tekst, nazwaSlotu, numerWiersza,
                dopasowanieRepliki0.Kandydaci.Select(r => r.Prototyp != null ? $"{r.ModelProducenta} {r.Prototyp}" : r.ModelProducenta).ToList());
        }

        // przedmioty tylko z kategorii slotu, bez szkiców
        var kandydaciPrzedmiotow = k.Dane.Przedmioty
            .Where(p => p.KategoriaId == kategoriaSlotu && !p.DoUzupelnienia)
            .ToList();
        var dopasowaniePrzedmiotu = Dopasowanie.Dopasuj(tekstDopasowany, kandydaciPrzedmiotow, p => $"{p.Firma} {p.Nazwa}");
        if (dopasowaniePrzedmiotu.Jednoznaczne && dopasowaniePrzedmiotu.Element != null)
            return new ElementWariantu
            {
                Typ = dopasowaniePrzedmiotu.Element.KompletId != null || k.Dane.Przedmioty.Any(x => x.KompletId == dopasowaniePrzedmiotu.Element.Id)
                    ? TypElementu.Komplet
                    : TypElementu.Przedmiot,
                ElementId = dopasowaniePrzedmiotu.Element.Id,
            };

        // slot „Mundur”: kilka elementów po „/” w jednym wariancie to komplet (BR-09); pojedynczy — zwykły przedmiot
        return TworzSzkicElementu(k, tekst, nazwaSlotu, numerWiersza,
            dopasowaniePrzedmiotu.Kandydaci.Select(p => $"{p.Firma} {p.Nazwa}").ToList());
    }

    /// <summary>Tworzy szkic elementu (FR-706) z konfliktem do decyzji; decyzja użytkownika (wybór kandydata / pominij) jest stosowana od razu. Zwraca null, gdy użytkownik wybrał „pomiń”.</summary>
    private static ElementWariantu? TworzSzkicElementu(KontekstImportu k, string tekst, string nazwaSlotu, int numerWiersza, List<string> kandydaci)
    {
        var konflikt = k.Podglad.DodajKonflikt(
            kandydaci.Count > 1 ? RodzajKonfliktu.Niejednoznaczne : RodzajKonfliktu.BrakOdpowiednika,
            kandydaci.Count > 1
                ? $"Element „{tekst}” (slot „{nazwaSlotu}”) pasuje do kilku pozycji — wybierz właściwą"
                : $"Element „{tekst}” (slot „{nazwaSlotu}”) nie ma odpowiednika w katalogu — utworzono szkic do uzupełnienia",
            $"Zestawy wiersz {numerWiersza}");
        konflikt.Kandydaci = kandydaci;
        konflikt.DomyslnyWybor = 0;
        // FR-1102: decyzja z ekranu konfliktów — pominij albo wybór kandydata zamiast szkicu
        if (konflikt.Pomin)
            return null;
        if (konflikt.WyborUzytkownika is int wybor && wybor >= 0 && wybor < kandydaci.Count && kandydaci.Count > 1)
        {
            var wybrany = kandydaci[wybor];
            var replika = k.Dane.Repliki.FirstOrDefault(r =>
                (r.Prototyp != null ? $"{r.ModelProducenta} {r.Prototyp}" : r.ModelProducenta) == wybrany);
            if (replika != null)
                return new ElementWariantu { Typ = TypElementu.Replika, ElementId = replika.Id };
            var przedmiot = k.Dane.Przedmioty.FirstOrDefault(p => $"{p.Firma} {p.Nazwa}" == wybrany);
            if (przedmiot != null)
                return new ElementWariantu
                {
                    Typ = przedmiot.KompletId != null || k.Dane.Przedmioty.Any(x => x.KompletId == przedmiot.Id)
                        ? TypElementu.Komplet
                        : TypElementu.Przedmiot,
                    ElementId = przedmiot.Id,
                };
        }
        var szkic = new Przedmiot
        {
            KategoriaId = IdKategoriiDlaSlotu(k, nazwaSlotu),
            Nazwa = tekst,
            ZnacznikZImportu = true,
            DoUzupelnienia = true,
        };
        k.Dane.Przedmioty.Add(szkic);
        return new ElementWariantu { Typ = TypElementu.Przedmiot, ElementId = szkic.Id, NazwaSzkicu = tekst };
    }

    /// <summary>Id kategorii dla slotu (FR-201); null dla slotów broni (tam tylko repliki).</summary>
    private static string? KategoriaDlaSlotu(KontekstImportu k, string nazwaSlotu)
    {
        var mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Mundur"] = "Mundury",
            ["Nakolanniki"] = "Nakolanniki",
            ["Kominiarka"] = "Szalokominiarki",
            ["Rękawice"] = "Rękawice",
            ["Hełm"] = "Hełmy",
            ["Czapka"] = "Czapki",
            ["Gogle"] = "Gogle",
            ["Buty"] = "Buty",
            ["Kamizelka"] = "Kamizelki",
            ["Plecak"] = "Plecaki",
            ["Pas"] = "Pasy",
        };
        if (!mapa.TryGetValue(nazwaSlotu, out var kat))
            return null;
        return k.Dane.Slowniki.Kategorie.FirstOrDefault(x => Normalizacja.TekstDoPorownania(x.Nazwa) == Normalizacja.TekstDoPorownania(kat))?.Id
            ?? IdKategorii(k, kat);
    }

    /// <summary>Kategoria szkicu na podstawie nazwy slotu (FR-201); sloty broni → Ładownice jako zaślepka (szkic zostanie poprawiony ręcznie).</summary>
    private static string IdKategoriiDlaSlotu(KontekstImportu k, string nazwaSlotu)
        => KategoriaDlaSlotu(k, nazwaSlotu) ?? IdKategorii(k, "Ładownice");
}
