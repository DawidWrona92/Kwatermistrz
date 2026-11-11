using Kwatermistrz.Core.Import;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Obliczenia;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.Core.Tests;

public sealed class RejestrWyprawTests
{
    private static DaneAplikacji Nowe()
    {
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        dane.Ustawienia.HistoryczneWyprawyWgRok[2022] = 3;
        dane.Ustawienia.HistoryczneWyprawyWgRok[2023] = 2;
        dane.Repliki.Add(new Replika { ModelProducenta = "AK", LicznikHistorycznyWypraw = 4 });
        dane.Repliki.Add(new Replika { ModelProducenta = "SWD", LicznikHistorycznyWypraw = 1 });
        return dane;
    }

    [Fact]
    public void ImportowaneAgregaty_PokazujaBezimienneWyprawyBezWymyslaniaDat()
    {
        var dane = Nowe();
        var wpisy = RejestrWypraw.Nierozpoznane(dane);
        Assert.Equal(5, wpisy.Count);
        Assert.Equal("Wyprawa_1", wpisy[0].Nazwa);
        Assert.Equal("Wyprawa_5", wpisy[4].Nazwa);
        Assert.Equal(2022, wpisy[0].Rok);
        Assert.Equal(2023, wpisy[4].Rok);
        Assert.Empty(dane.Wyprawy); // Nie materializujemy fikcyjnych wydarzeń w JSON.
        Assert.Equal(5, new Kalkulator(dane).LacznaLiczbaWypraw());
        Assert.Equal(5, new IndeksObliczeniowy(dane).LacznaLiczbaWypraw);
        Assert.Equal(3, new Kalkulator(dane).WyprawyWRoku(2022));
    }

    [Fact]
    public void RozpoznanieSamejDaty_NiePodwajaSumyMimoNieznanejRepliki()
    {
        var dane = Nowe();
        var id = RejestrWypraw.Nierozpoznane(dane)[0].Id;
        var w = RejestrWypraw.Rozpoznaj(dane, id, new DateOnly(2022, 10, 5), null, null);
        Assert.Equal(5, new Kalkulator(dane).LacznaLiczbaWypraw());
        Assert.Equal(5, new IndeksObliczeniowy(dane).LacznaLiczbaWypraw);
        Assert.Equal(3, new Kalkulator(dane).WyprawyWRoku(2022));
        Assert.Equal(2, dane.Ustawienia.HistoryczneWyprawyWgRok[2022]);
        Assert.Equal(4, dane.Repliki[0].LicznikHistorycznyWypraw);
        Assert.Null(w.ReplikaGlownaId);
        Assert.True(w.CzyHistoryczna);
        Assert.Equal(0, RejestrWypraw.RozbieznoscLicznikow(dane));
    }

    [Fact]
    public void RozpoznanieReplikiIZestawu_PrzenosiUzycieZLicznikow_ZamiastDublowac()
    {
        var dane = Nowe();
        var przedmiot = new Przedmiot { Nazwa = "Mundur", LicznikHistoryczny = 2 };
        dane.Przedmioty.Add(przedmiot);
        var wariant = new WariantSlotu
        {
            Elementy = new() { new ElementWariantu { Typ = TypElementu.Przedmiot, ElementId = przedmiot.Id } },
        };
        var slot = new SlotZestawu { Warianty = new() { wariant }, WariantDomyslnyId = wariant.Id };
        var zestaw = new Zestaw { Nazwa = "Zestaw", Sloty = new() { slot } };
        dane.Zestawy.Add(zestaw);

        var id = RejestrWypraw.Nierozpoznane(dane)[0].Id;
        var eventHistoryczny = RejestrWypraw.Rozpoznaj(dane, id, null, dane.Repliki[0].Id, zestaw.Id);
        Assert.Equal(default, eventHistoryczny.Data);
        Assert.Equal(2022, eventHistoryczny.RokHistoryczny);
        Assert.Equal(3, dane.Repliki[0].LicznikHistorycznyWypraw);
        Assert.Equal(1, przedmiot.LicznikHistoryczny);
        Assert.Single(eventHistoryczny.WybraneWarianty);
        Assert.Equal(5, new Kalkulator(dane).LacznaLiczbaWypraw());
        Assert.Equal(4, new Kalkulator(dane).WyprawyRepliki(dane.Repliki[0]));
        Assert.Equal(2, new Kalkulator(dane).UzyciaPrzedmiotu(przedmiot));
        Assert.Equal(2, new IndeksObliczeniowy(dane).UzyciaPrzedmiotu(przedmiot));

        RejestrWypraw.CofnijRozpoznanie(dane, eventHistoryczny);
        Assert.True(eventHistoryczny.CzyUsunieta);
        Assert.Equal(4, dane.Repliki[0].LicznikHistorycznyWypraw);
        Assert.Equal(2, przedmiot.LicznikHistoryczny);
        Assert.Equal(3, dane.Ustawienia.HistoryczneWyprawyWgRok[2022]);
        Assert.Equal(5, new Kalkulator(dane).LacznaLiczbaWypraw());
    }

    [Fact]
    public void CzescDanychMoznaUzupelnicPozniej_BezPowtornegoOdejmowaniaRoku()
    {
        var dane = Nowe();
        var w = RejestrWypraw.Rozpoznaj(dane, RejestrWypraw.Nierozpoznane(dane)[0].Id,
            new DateOnly(2022, 11, 11), null, null);
        RejestrWypraw.Dopelnij(dane, w, new DateOnly(2022, 11, 11), dane.Repliki[1].Id, null);
        Assert.Equal(2, dane.Ustawienia.HistoryczneWyprawyWgRok[2022]);
        Assert.Equal(0, dane.Repliki[1].LicznikHistorycznyWypraw);
        Assert.Equal(5, new Kalkulator(dane).LacznaLiczbaWypraw());
        Assert.Equal(0, RejestrWypraw.RozbieznoscLicznikow(dane));
    }

    [Fact]
    public void ZlaDataLubBrakHistorycznegoUzycia_NieZmieniaDanych()
    {
        var dane = Nowe();
        dane.Repliki.Add(new Replika { ModelProducenta = "Nowa", LicznikHistorycznyWypraw = 0 });
        var id = RejestrWypraw.Nierozpoznane(dane)[0].Id;
        Assert.Throws<InvalidOperationException>(() => RejestrWypraw.Rozpoznaj(
            dane, id, new DateOnly(2024, 1, 1), null, null));
        Assert.Throws<InvalidOperationException>(() => RejestrWypraw.Rozpoznaj(
            dane, id, null, dane.Repliki[^1].Id, null));
        Assert.Empty(dane.Wyprawy);
        Assert.Equal(3, dane.Ustawienia.HistoryczneWyprawyWgRok[2022]);
        Assert.Equal(5, new Kalkulator(dane).LacznaLiczbaWypraw());
    }

    [Fact]
    public void Json_ChroniNieznaneDatyIRokPoPonownymWczytaniu()
    {
        var katalog = Path.Combine(Path.GetTempPath(), "kw-history-" + Guid.NewGuid().ToString("N"));
        try
        {
            var magazyn = new MagazynJson(katalog);
            var dane = Nowe();
            RejestrWypraw.Rozpoznaj(dane, RejestrWypraw.Nierozpoznane(dane)[0].Id,
                null, dane.Repliki[0].Id, null);
            magazyn.Zapisz(dane);
            var wczytane = magazyn.Wczytaj();
            var w = Assert.Single(wczytane.Wyprawy);
            Assert.Equal(default, w.Data);
            Assert.Equal(2022, w.RokHistoryczny);
            Assert.Equal("Wyprawa_1", w.NazwaHistoryczna);
            Assert.Equal(5, new Kalkulator(wczytane).LacznaLiczbaWypraw());
            Assert.Equal(3, new Kalkulator(wczytane).WyprawyWRoku(2022));
        }
        finally
        {
            if (Directory.Exists(katalog))
                Directory.Delete(katalog, true);
        }
    }

    [Fact]
    public void ImportXlsx_89HistorycznychWypraw_JedenRejestrDlaLatIReplik()
    {
        var sciezka = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "dane-startowe", "Dane_zrodlowe_Kwatermistrz.xlsx"));
        var podglad = ImporterKwatermistrz.PrzygotujPodglad(sciezka, new DateOnly(2026, 10, 22));
        var dane = podglad.Dane!;
        var historia = RejestrWypraw.Nierozpoznane(dane);
        Assert.Equal(89, historia.Count);
        Assert.Equal(89, new Kalkulator(dane).LacznaLiczbaWypraw());
        Assert.Equal(89, new IndeksObliczeniowy(dane).LacznaLiczbaWypraw);
        Assert.Equal(0, RejestrWypraw.RozbieznoscLicznikow(dane));
        Assert.Equal(10, historia.Count(w => w.Rok == 2022));
        Assert.Equal(30, historia.Count(w => w.Rok == 2023));
    }
}