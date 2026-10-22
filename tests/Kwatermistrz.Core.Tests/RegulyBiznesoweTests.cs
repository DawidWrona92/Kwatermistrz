using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Obliczenia;

namespace Kwatermistrz.Core.Tests;

/// <summary>Testy reguł BR-01..BR-11, BR-15 na danych syntetycznych (NFR-09). Wartości końcowe po imporcie sprawdza ImportTesty.</summary>
public class RegulyBiznesoweTests
{
    private static (DaneAplikacji dane, Kalkulator kalk) Przygotuj()
    {
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        return (dane, new Kalkulator(dane));
    }

    private static Replika NowaReplika(string nazwa, int licznikHistoryczny = 0, StatusWlasnosci status = StatusWlasnosci.Posiadany)
        => new() { ModelProducenta = nazwa, LicznikHistorycznyWypraw = licznikHistoryczny, Status = status };

    [Fact]
    public void BR01_WyprawyRepliki_SumaLicznikaHistorycznegoIZapisanych()
    {
        var (dane, kalk) = Przygotuj();
        var replika = NowaReplika("R1", licznikHistoryczny: 5);
        dane.Repliki.Add(replika);
        dane.Wyprawy.Add(new Wyprawa { Data = new DateOnly(2026, 1, 10), ReplikaGlownaId = replika.Id });
        dane.Wyprawy.Add(new Wyprawa { Data = new DateOnly(2026, 2, 1), ReplikaGlownaId = replika.Id });
        dane.Wyprawy.Add(new Wyprawa { Data = new DateOnly(2026, 2, 2) }); // bez repliki

        Assert.Equal(7, kalk.WyprawyRepliki(replika));
    }

    [Fact]
    public void BR02_LacznaLiczbaWypraw_LiczyTezSprzedaneIDoSprzedazy()
    {
        var (dane, kalk) = Przygotuj();
        dane.Repliki.Add(NowaReplika("A", 10));
        dane.Repliki.Add(NowaReplika("B", 1, StatusWlasnosci.DoSprzedazy));
        dane.Repliki.Add(NowaReplika("C", 3, StatusWlasnosci.Sprzedany));

        Assert.Equal(14, kalk.LacznaLiczbaWypraw());
    }

    [Fact]
    public void BR03_ProcentWypraw_SprzedaneWchodzaDoMianownika()
    {
        var (dane, kalk) = Przygotuj();
        dane.Repliki.Add(NowaReplika("A", 19));
        dane.Repliki.Add(NowaReplika("S", 70, StatusWlasnosci.Sprzedany));

        // 19 / 89 = 21,348...% -> 21,3%
        Assert.Equal(21.3m, Math.Round(kalk.ProcentWypraw(19), 1));
    }

    [Fact]
    public void BR04_Koszty_RozbicieNaOryginalnaIDodatki()
    {
        var (dane, kalk) = Przygotuj();
        var replika = NowaReplika("R1");
        dane.Slowniki.RodzajeKosztow.Single(r => r.Semantyka == nameof(SemantykaKosztu.Zakup));
        replika.Koszty.Add(new PozycjaKosztowa { Kwota = 960m, Semantyka = SemantykaKosztu.Zakup });
        replika.Koszty.Add(new PozycjaKosztowa { Kwota = 112m, Semantyka = SemantykaKosztu.Ulepszenie });
        replika.Koszty.Add(new PozycjaKosztowa { Kwota = -100m, Semantyka = SemantykaKosztu.Zwrot });
        dane.Repliki.Add(replika);

        var w = kalk.WierszRepliki(replika);
        Assert.Equal(972m, w.KosztCalkowity);
        Assert.Equal(960m, w.CenaOryginalna);
        Assert.Equal(12m, w.KosztDodatkow);
    }

    [Fact]
    public void BR05_KosztNaWyprawe_BezWyprawPelnyKoszt()
    {
        Assert.Equal(1076m, Kalkulator.KosztNaWyprawe(1076m, 0));
    }

    [Fact]
    public void BR05_KosztNaWyprawe_Dzielenie()
    {
        // 3297,12 / 19 = 173,5326... -> 173,53 przy wyświetlaniu
        Assert.Equal(173.53m, Math.Round(Kalkulator.KosztNaWyprawe(3297.12m, 19), 2));
        Assert.Equal(800m, Kalkulator.KosztNaWyprawe(800m, 1));
    }

    [Fact]
    public void BR06_SrednieKosztow_LiczaSieTylkoPosiadane()
    {
        var (dane, kalk) = Przygotuj();
        var posiadana = NowaReplika("A");
        posiadana.Koszty.Add(new PozycjaKosztowa { Kwota = 1000m, Semantyka = SemantykaKosztu.Zakup });
        var doSprzedazy = NowaReplika("B", status: StatusWlasnosci.DoSprzedazy);
        doSprzedazy.Koszty.Add(new PozycjaKosztowa { Kwota = 5000m, Semantyka = SemantykaKosztu.Zakup });
        var sprzedana = NowaReplika("C", status: StatusWlasnosci.Sprzedany);
        sprzedana.Koszty.Add(new PozycjaKosztowa { Kwota = 9000m, Semantyka = SemantykaKosztu.Zakup });
        dane.Repliki.AddRange(new[] { posiadana, doSprzedazy, sprzedana });

        var tylkoPosiadane = kalk.ReplikiPosiadane().ToList();
        Assert.Single(tylkoPosiadane);
        Assert.Equal(1000m, Kalkulator.Koszty(tylkoPosiadane[0].Koszty).Calkowity);
    }

    [Fact]
    public void BR07_WyprawyWRoku_LiczyHistoryczneIZapisane()
    {
        var (dane, kalk) = Przygotuj();
        dane.Ustawienia.HistoryczneWyprawyWgRok[2025] = 19;
        dane.Ustawienia.HistoryczneWyprawyWgRok[2026] = 13;
        dane.Wyprawy.Add(new Wyprawa { Data = new DateOnly(2026, 5, 1) });
        dane.Wyprawy.Add(new Wyprawa { Data = new DateOnly(2026, 6, 1) });
        dane.Wyprawy.Add(new Wyprawa { Data = new DateOnly(2025, 12, 31) });

        Assert.Equal(15, kalk.WyprawyWRoku(2026));
        Assert.Equal(20, kalk.WyprawyWRoku(2025));
        Assert.Equal(0, kalk.WyprawyWRoku(2024));
    }

    [Fact]
    public void BR08_UzyciaPrzedmiotu_LicznikPlusWyprawyZeSkladu()
    {
        var (dane, kalk) = Przygotuj();
        var mundur = new Przedmiot { Nazwa = "Mundur", LicznikHistoryczny = 7 };
        dane.Przedmioty.Add(mundur);
        var inny = new Przedmiot { Nazwa = "Inny" };
        dane.Przedmioty.Add(inny);

        var zestaw = new Zestaw { Nazwa = "Z1" };
        var slot = new SlotZestawu { Nazwa = "Mundur" };
        slot.Warianty.Add(new WariantSlotu { Elementy = { new ElementWariantu { Typ = TypElementu.Przedmiot, ElementId = mundur.Id } } });
        zestaw.Sloty.Add(slot);
        dane.Zestawy.Add(zestaw);
        var rekonstrukcja = new Rekonstrukcja { Nazwa = "R" };
        rekonstrukcja.SzablonSlotow.Add(new SlotSzablonu { Nazwa = "Mundur", CzySlotMunduru = true });
        dane.Rekonstrukcje.Add(rekonstrukcja);

        dane.Wyprawy.Add(new Wyprawa
        {
            Data = new DateOnly(2026, 3, 1),
            ZestawId = zestaw.Id,
            RekonstrukcjaId = rekonstrukcja.Id,
            WybraneWarianty = new() { [slot.Id] = 0 },
        });

        Assert.Equal(8, kalk.UzyciaPrzedmiotu(mundur));
        Assert.Equal(0, kalk.UzyciaPrzedmiotu(inny));
    }

    [Fact]
    public void BR09_MundurWyprawy_PierwszyElementSlotuMunduruPrzyMieszance()
    {
        var (dane, kalk) = Przygotuj();
        var gorny = new Przedmiot { Nazwa = "Góra" };
        var dol = new Przedmiot { Nazwa = "Dół" };
        dane.Przedmioty.AddRange(new[] { gorny, dol });
        var zestaw = new Zestaw { Nazwa = "Z1" };
        var slot = new SlotZestawu { Nazwa = "Mundur" };
        slot.Warianty.Add(new WariantSlotu
        {
            Elementy =
            {
                new ElementWariantu { Typ = TypElementu.Przedmiot, ElementId = gorny.Id },
                new ElementWariantu { Typ = TypElementu.Przedmiot, ElementId = dol.Id },
            }
        });
        zestaw.Sloty.Add(slot);
        dane.Zestawy.Add(zestaw);
        var rekonstrukcja = new Rekonstrukcja { Nazwa = "R" };
        rekonstrukcja.SzablonSlotow.Add(new SlotSzablonu { Nazwa = "Mundur", CzySlotMunduru = true });
        dane.Rekonstrukcje.Add(rekonstrukcja);

        var wyprawa = new Wyprawa
        {
            Data = new DateOnly(2026, 3, 1),
            ZestawId = zestaw.Id,
            RekonstrukcjaId = rekonstrukcja.Id,
            WybraneWarianty = new() { [slot.Id] = 0 },
        };
        var (mundurId, czyMieszanka) = kalk.MundurWyprawy(wyprawa);

        Assert.Equal(gorny.Id, mundurId);
        Assert.True(czyMieszanka);
    }

    [Fact]
    public void BR13_KwotyZachowujaDokladnosc_DoGrosza()
    {
        // suma 0,1 + 0,2 w decimal daje dokładnie 0,3
        var pozycje = new List<PozycjaKosztowa>
        {
            new() { Kwota = 0.1m },
            new() { Kwota = 0.2m },
        };
        Assert.Equal(0.3m, Kalkulator.Koszty(pozycje).Calkowity);
    }

    [Fact]
    public void BR14_Formaty_WalutaProcentData()
    {
        Assert.Equal("3 297,12 zł", Formatowanie.Waluta(3297.12m));
        Assert.Equal("21,3%", Formatowanie.Procent(21.348314606741572881m));
        Assert.Equal("01.09.2022", Formatowanie.Data(new DateOnly(2022, 9, 1)));
        Assert.Equal("-100,00 zł", Formatowanie.Waluta(-100m));
    }

    [Fact]
    public void BR15_DataPierwszejWyprawy_UstawienieLubNajwczesniejszaZapisana()
    {
        var (dane, kalk) = Przygotuj();
        dane.Ustawienia.DataPierwszejWyprawy = new DateOnly(2022, 9, 1);
        Assert.Equal(new DateOnly(2022, 9, 1), kalk.DataPierwszejWyprawy());

        dane.Wyprawy.Add(new Wyprawa { Data = new DateOnly(2021, 5, 1) });
        Assert.Equal(new DateOnly(2021, 5, 1), kalk.DataPierwszejWyprawy());
    }

    [Fact]
    public void BR16_FiltrRekonstrukcji_ElementWspolnyBezPrzypisania()
    {
        Assert.True(Kalkulator.WidocznyDlaRekonstrukcji(new List<string>(), "r1"));
        Assert.True(Kalkulator.WidocznyDlaRekonstrukcji(new List<string> { "r1" }, "r1"));
        Assert.False(Kalkulator.WidocznyDlaRekonstrukcji(new List<string> { "r2" }, "r1"));
        Assert.True(Kalkulator.WidocznyDlaRekonstrukcji(new List<string> { "r2" }, null));
    }

    [Fact]
    public void Normalizacja_FormulyKosztu_ZMinusamiIUjemnymi()
    {
        Assert.Equal(new List<decimal> { 960m, 112m, 64m }, Normalizacja.ParsujFormuleKosztu("960+112+64"));
        Assert.Equal(new List<decimal> { 626m, -100m, -35m, 76m, 252m, 257m }, Normalizacja.ParsujFormuleKosztu("626-100-35+76+252+257"));
        Assert.Equal(new List<decimal> { 301.12m }, Normalizacja.ParsujFormuleKosztu("301.12"));
        Assert.Null(Normalizacja.ParsujFormuleKosztu(""));
        Assert.Null(Normalizacja.ParsujFormuleKosztu(null));
        Assert.Null(Normalizacja.ParsujFormuleKosztu("brak"));
    }

    [Fact]
    public void Normalizacja_Znaczniki_13_4_13_5()
    {
        Assert.True(Normalizacja.CzyZnacznikNieznane("??"));
        Assert.True(Normalizacja.CzyZnacznikNieznane("???"));
        Assert.False(Normalizacja.CzyZnacznikNieznane("brak"));
        Assert.True(Normalizacja.CzyZnacznikNieDotyczy("Nie dotyczy HPA"));
        Assert.True(Normalizacja.CzyWartoscBrak("brak"));
        Assert.Equal("PKM", Normalizacja.Przytnij("PKM "));
        Assert.Equal("Ideał", Normalizacja.Przytnij("Ideał "));
    }
}

/// <summary>Testy pulpitowe FR-104/105: karty skrótowe i ranking replik.</summary>
public class PulpitTests
{
    private static (DaneAplikacji dane, Kalkulator kalk) Przygotuj()
    {
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        return (dane, new Kalkulator(dane));
    }

    [Fact]
    public void FR104_OtwarteAwaria_LiczyTylkoZadaniaSemantykiAwaria()
    {
        var (dane, kalk) = Przygotuj();
        dane.Zadania.Add(new Zadanie { Semantyka = SemantykaZadania.Awaria, Status = StatusZadania.Otwarte });
        dane.Zadania.Add(new Zadanie { Semantyka = SemantykaZadania.Awaria, Status = StatusZadania.Zamkniete });
        dane.Zadania.Add(new Zadanie { Semantyka = SemantykaZadania.DoZrobienia, Status = StatusZadania.Otwarte });

        Assert.Equal(1, kalk.OtwarteAwaria());
        Assert.Equal(2, kalk.OtwarteZadania());
    }

    [Fact]
    public void FR104_ReplikiBezTestu_LiczyStarszeNizProgIRozneBezPomiarow()
    {
        var (dane, kalk) = Przygotuj();
        var dzis = DateOnly.FromDateTime(DateTime.Today);
        var stara = new Replika { ModelProducenta = "Stara" };
        stara.Pomiary.Add(new Pomiar { Data = dzis.AddDays(-200) });
        var swieza = new Replika { ModelProducenta = "Świeża" };
        swieza.Pomiary.Add(new Pomiar { Data = dzis.AddDays(-10) });
        var bezTestu = new Replika { ModelProducenta = "Bez testu" };
        var sprzedana = new Replika { ModelProducenta = "Sprzedana", Status = StatusWlasnosci.Sprzedany };
        dane.Repliki.AddRange(new[] { stara, swieza, bezTestu, sprzedana });

        var wynik = kalk.ReplikiBezTestu(180);

        Assert.Equal(2, wynik.Count);
        Assert.Contains(wynik, r => r.ModelProducenta == "Stara");
        Assert.Contains(wynik, r => r.ModelProducenta == "Bez testu");
        Assert.DoesNotContain(wynik, r => r.ModelProducenta == "Świeża");
        Assert.DoesNotContain(wynik, r => r.ModelProducenta == "Sprzedana");
    }

    [Fact]
    public void FR104_PrzedmiotyBezUzycia_LiczyPosiadaneBezUzyc()
    {
        var (dane, kalk) = Przygotuj();
        var replika = new Replika { ModelProducenta = "R1" };
        dane.Repliki.Add(replika);
        var uzyty = new Przedmiot { Nazwa = "Użyty" };
        var nieuzyty = new Przedmiot { Nazwa = "Nieuzyty" };
        var historyczny = new Przedmiot { Nazwa = "Historyczny", LicznikHistoryczny = 3 };
        var sprzedany = new Przedmiot { Nazwa = "Sprzedany", Status = StatusWlasnosci.Sprzedany };
        dane.Przedmioty.AddRange(new[] { uzyty, nieuzyty, historyczny, sprzedany });
        // „użyty” ma użycia tylko przez skład ręczny wyprawy
        dane.Wyprawy.Add(new Wyprawa
        {
            Data = new DateOnly(2026, 1, 1),
            ReplikaGlownaId = replika.Id,
            SkladReczny = new() { new ElementWariantu { Typ = TypElementu.Przedmiot, ElementId = uzyty.Id } },
        });

        Assert.Equal(1, kalk.PrzedmiotyBezUzycia());
    }

    [Fact]
    public void FR104_PolaNieznane_LiczyPolaStanuNieznane()
    {
        var (dane, kalk) = Przygotuj();
        var r = new Replika { ModelProducenta = "R1" };
        r.KartaTechniczna["p1"] = new WartoscPolaKarty { Tekst = "?", Stan = StanWartosciPola.Nieznane };
        r.KartaTechniczna["p2"] = new WartoscPolaKarty { Tekst = "OK", Stan = StanWartosciPola.Wypelnione };
        r.KartaTechniczna["p3"] = new WartoscPolaKarty { Stan = StanWartosciPola.Nieznane };
        dane.Repliki.Add(r);

        Assert.Equal(2, kalk.PolaNieznane());
    }

    [Fact]
    public void FR105_RankingReplik_SortujePoWyprawachIWylaczaSprzedane()
    {
        var (dane, kalk) = Przygotuj();
        var r1 = new Replika { ModelProducenta = "R1", LicznikHistorycznyWypraw = 10 };
        r1.Koszty.Add(new PozycjaKosztowa { Kwota = 1000m, Semantyka = SemantykaKosztu.Zakup });
        var r2 = new Replika { ModelProducenta = "R2", LicznikHistorycznyWypraw = 5 };
        var r3 = new Replika { ModelProducenta = "R3", LicznikHistorycznyWypraw = 20, Status = StatusWlasnosci.Sprzedany };
        var r4 = new Replika { ModelProducenta = "R4", LicznikHistorycznyWypraw = 3 };
        var r5 = new Replika { ModelProducenta = "R5", LicznikHistorycznyWypraw = 1 };
        var r6 = new Replika { ModelProducenta = "R6", LicznikHistorycznyWypraw = 0 };
        dane.Repliki.AddRange(new[] { r1, r2, r3, r4, r5, r6 });

        var ranking = kalk.RankingReplik(5);

        Assert.Equal(5, ranking.Count);
        Assert.Equal(r1.Id, ranking[0].ReplikaId);
        Assert.Equal(1000m / 10m, ranking[0].KosztNaWyprawe);
        Assert.Equal(r2.Id, ranking[1].ReplikaId);
        Assert.Equal(r4.Id, ranking[2].ReplikaId);
        Assert.DoesNotContain(ranking, w => w.ReplikaId == r3.Id);
    }
}

/// <summary>Testy FR-503: pole „cena zakupu” przedmiotu synchronizuje pozycję kosztową.</summary>
public class Fr503Tests
{
    private static (DaneAplikacji dane, Slowniki slowniki) Przygotuj()
    {
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        return (dane, dane.Slowniki);
    }

    [Fact]
    public void CenaZakupu_TworzyPozycjeZakupu()
    {
        var (dane, slowniki) = Przygotuj();
        var p = new Przedmiot { Nazwa = "Gorka", CenaZakupu = 450m, DataZakupu = new DateOnly(2025, 3, 1) };

        p.SynchronizujCenaZakupu(slowniki, p.DataZakupu!.Value);

        var pozycja = Assert.Single(p.Koszty);
        Assert.Equal(450m, pozycja.Kwota);
        Assert.Equal(SemantykaKosztu.Zakup, pozycja.Semantyka);
        Assert.Equal(new DateOnly(2025, 3, 1), pozycja.Data);
        Assert.Equal(slowniki.RodzajeKosztow.First(r => r.Semantyka == nameof(SemantykaKosztu.Zakup)).Id, pozycja.RodzajId);
    }

    [Fact]
    public void ZmianaCeny_AktualizujeIstniejacaPozycje()
    {
        var (dane, slowniki) = Przygotuj();
        var p = new Przedmiot { Nazwa = "Gorka", CenaZakupu = 450m };
        p.SynchronizujCenaZakupu(slowniki, DateOnly.FromDateTime(DateTime.Today));
        p.CenaZakupu = 500m;

        p.SynchronizujCenaZakupu(slowniki, DateOnly.FromDateTime(DateTime.Today));

        var pozycja = Assert.Single(p.Koszty);
        Assert.Equal(500m, pozycja.Kwota);
    }

    [Fact]
    public void WyczyszczenieCeny_UsuwaPozycjeZakupu()
    {
        var (dane, slowniki) = Przygotuj();
        var p = new Przedmiot { Nazwa = "Gorka", CenaZakupu = 450m };
        p.SynchronizujCenaZakupu(slowniki, DateOnly.FromDateTime(DateTime.Today));
        p.CenaZakupu = null;

        p.SynchronizujCenaZakupu(slowniki, DateOnly.FromDateTime(DateTime.Today));

        Assert.Empty(p.Koszty);
    }

    [Fact]
    public void PozycjaRecznaZZakupem_NieJestNadpisywana()
    {
        var (dane, slowniki) = Przygotuj();
        var p = new Przedmiot { Nazwa = "Gorka", CenaZakupu = 450m };
        p.Koszty.Add(new PozycjaKosztowa { Opis = "Zakup z faktury", Kwota = 400m, Semantyka = SemantykaKosztu.Zakup });

        p.SynchronizujCenaZakupu(slowniki, DateOnly.FromDateTime(DateTime.Today));

        Assert.Equal(2, p.Koszty.Count);
        Assert.Contains(p.Koszty, k => k.Opis == "Zakup z faktury" && k.Kwota == 400m);
        Assert.Contains(p.Koszty, k => k.Opis == "Cena zakupu" && k.Kwota == 450m);
    }
}

/// <summary>Testy FR-707/FR-708: lista pakowania i koszt zestawu (min/max).</summary>
public class Fr707708Tests
{
    private static (DaneAplikacji dane, Kalkulator kalk) Przygotuj()
    {
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        return (dane, new Kalkulator(dane));
    }

    [Fact]
    public void FR707_ListaPakowania_RozwijaKompletIWybieraWariantyDomyslne()
    {
        var (dane, kalk) = Przygotuj();
        var gora = new Przedmiot { Nazwa = "Góra", Koszty = new() { new PozycjaKosztowa { Kwota = 300m, Semantyka = SemantykaKosztu.Zakup } } };
        var dol = new Przedmiot { Nazwa = "Dół", Koszty = new() { new PozycjaKosztowa { Kwota = 200m, Semantyka = SemantykaKosztu.Zakup } } };
        var kompletId = "komplet-1";
        gora.KompletId = kompletId;
        dol.KompletId = kompletId;
        var replika = new Replika { ModelProducenta = "AK" };
        dane.Przedmioty.AddRange(new[] { gora, dol });
        dane.Repliki.Add(replika);
        var zestaw = new Zestaw { Nazwa = "Zestaw" };
        var slotMundur = new SlotZestawu { Nazwa = "Mundur" };
        slotMundur.Warianty.Add(new WariantSlotu { Elementy = new() { new ElementWariantu { Typ = TypElementu.Komplet, ElementId = kompletId } } });
        slotMundur.Warianty.Add(new WariantSlotu { Elementy = new() { new ElementWariantu { Typ = TypElementu.Przedmiot, ElementId = gora.Id } } });
        slotMundur.IndeksWariantuDomyslnego = 0;
        var slotBron = new SlotZestawu { Nazwa = "Broń" };
        slotBron.Warianty.Add(new WariantSlotu { CzyPuste = true });
        slotBron.Warianty.Add(new WariantSlotu { Elementy = new() { new ElementWariantu { Typ = TypElementu.Replika, ElementId = replika.Id } } });
        zestaw.Sloty.AddRange(new[] { slotMundur, slotBron });
        dane.Zestawy.Add(zestaw);

        var lista = kalk.ElementyListyPakowania(zestaw);

        // komplet rozwinięty na dwa elementy (góra+dół), slot broni domyślnie „puste” → pominięty
        Assert.Equal(2, lista.Count);
        Assert.Contains(lista, x => x.Element.ElementId == gora.Id);
        Assert.Contains(lista, x => x.Element.ElementId == dol.Id);
    }

    [Fact]
    public void FR708_KosztZestawu_SumujeCenyWariantowDomyslnych()
    {
        var (dane, kalk) = Przygotuj();
        var p1 = new Przedmiot { Nazwa = "P1", Koszty = new() { new PozycjaKosztowa { Kwota = 300m, Semantyka = SemantykaKosztu.Zakup } } };
        var p2 = new Przedmiot { Nazwa = "P2", Koszty = new() { new PozycjaKosztowa { Kwota = 100m, Semantyka = SemantykaKosztu.Zakup }, new PozycjaKosztowa { Kwota = 50m, Semantyka = SemantykaKosztu.Ulepszenie } } };
        dane.Przedmioty.AddRange(new[] { p1, p2 });
        var zestaw = new Zestaw { Nazwa = "Z" };
        var slot = new SlotZestawu { Nazwa = "S" };
        slot.Warianty.Add(new WariantSlotu { Elementy = new() { new ElementWariantu { Typ = TypElementu.Przedmiot, ElementId = p1.Id } } });
        slot.Warianty.Add(new WariantSlotu { Elementy = new() { new ElementWariantu { Typ = TypElementu.Przedmiot, ElementId = p2.Id } } });
        slot.IndeksWariantuDomyslnego = 1;
        zestaw.Sloty.Add(slot);

        Assert.Equal(100m, kalk.KosztZestawu(zestaw));
    }

    [Fact]
    public void FR708_ZakresKosztu_MinMaxPoWariantach()
    {
        var (dane, kalk) = Przygotuj();
        var p1 = new Przedmiot { Nazwa = "P1", Koszty = new() { new PozycjaKosztowa { Kwota = 300m, Semantyka = SemantykaKosztu.Zakup } } };
        var p2 = new Przedmiot { Nazwa = "P2", Koszty = new() { new PozycjaKosztowa { Kwota = 100m, Semantyka = SemantykaKosztu.Zakup } } };
        dane.Przedmioty.AddRange(new[] { p1, p2 });
        var zestaw = new Zestaw { Nazwa = "Z" };
        var slot = new SlotZestawu { Nazwa = "S" };
        slot.Warianty.Add(new WariantSlotu { Elementy = new() { new ElementWariantu { Typ = TypElementu.Przedmiot, ElementId = p1.Id } } });
        slot.Warianty.Add(new WariantSlotu { CzyPuste = true });
        slot.Warianty.Add(new WariantSlotu { Elementy = new() { new ElementWariantu { Typ = TypElementu.Przedmiot, ElementId = p2.Id } } });
        zestaw.Sloty.Add(slot);

        var (min, max) = kalk.ZakresKosztuZestawu(zestaw);

        Assert.Equal(0m, min);
        Assert.Equal(300m, max);
    }
}
