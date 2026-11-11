using System.Text;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Obliczenia;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.Core.Tests;

public class OperacjeNaWyprawachTests
{
    private static (DaneAplikacji Dane, Replika R1, Replika R2) Przyklad()
    {
        var dane = new DaneAplikacji();
        var r1 = new Replika { ModelProducenta = "R1", LicznikHistorycznyWypraw = 3 };
        var r2 = new Replika { ModelProducenta = "R2", LicznikHistorycznyWypraw = 2 };
        dane.Repliki.AddRange(new[] { r1, r2 });
        dane.Ustawienia.DzienZerowy = new DateOnly(2022, 12, 31);
        dane.Ustawienia.HistoryczneWyprawyWgRok[2021] = 5;
        dane.Ustawienia.HistoryczneWyprawyWgRok[2022] = 4;
        return (dane, r1, r2);
    }

    [Fact]
    public void Szkic_JestOdizolowanyOdDanychZapisanych()
    {
        var zrodlo = new Wyprawa
        {
            Data = new DateOnly(2022, 7, 1), Miejsce = "Stare",
            WybraneWarianty = new Dictionary<string, string> { ["slot"] = "wariant1" },
            SkladReczny = new List<ElementWariantu> { new() { Typ = TypElementu.Przedmiot, ElementId = "p1" } },
            Zdjecia = new List<string> { "zdjecia/1.jpg" },
        };
        var szkic = OperacjeNaWyprawach.Kopia(zrodlo);
        szkic.Miejsce = "Nowe";
        szkic.WybraneWarianty["slot"] = "wariant2";
        szkic.SkladReczny[0].ElementId = "p2";
        szkic.Zdjecia.Clear();
        Assert.Equal("Stare", zrodlo.Miejsce);
        Assert.Equal("wariant1", zrodlo.WybraneWarianty["slot"]);
        Assert.Equal("p1", zrodlo.SkladReczny[0].ElementId);
        Assert.Single(zrodlo.Zdjecia);
    }

    [Fact]
    public void EdycjaWyprawyHistorycznej_ZmieniaWylacznieWlasciweLiczniki()
    {
        var (dane, r1, r2) = Przyklad();
        // Wcześniej odliczono 1 z R1 i z 2021. Baza już zawiera pomniejszone wartości.
        var oryginal = new Wyprawa
        {
            Data = new DateOnly(2021, 6, 1), ReplikaGlownaId = r1.Id,
            OdliczonaOdLicznikow = true,
        };
        dane.Wyprawy.Add(oryginal);
        var operacje = new OperacjeNaWyprawach(dane);
        var szkic = OperacjeNaWyprawach.Kopia(oryginal);
        szkic.Data = new DateOnly(2022, 8, 1);
        szkic.ReplikaGlownaId = r2.Id;

        Assert.True(operacje.SprobujZapisac(oryginal, szkic, out var blad), blad);
        Assert.Equal(4, r1.LicznikHistorycznyWypraw);
        Assert.Equal(1, r2.LicznikHistorycznyWypraw);
        Assert.Equal(6, dane.Ustawienia.HistoryczneWyprawyWgRok[2021]);
        Assert.Equal(3, dane.Ustawienia.HistoryczneWyprawyWgRok[2022]);
        Assert.True(oryginal.OdliczonaOdLicznikow);
        Assert.Equal(new DateOnly(2022, 8, 1), oryginal.Data);
    }

    [Fact]
    public void EdycjaHistorycznejBezKorekty_NieWlaczaJejSamoczynnie()
    {
        var (dane, r1, _) = Przyklad();
        var oryginal = new Wyprawa { Data = new DateOnly(2021, 1, 1), ReplikaGlownaId = r1.Id };
        dane.Wyprawy.Add(oryginal);
        var szkic = OperacjeNaWyprawach.Kopia(oryginal);
        szkic.Data = new DateOnly(2022, 5, 1);

        Assert.True(new OperacjeNaWyprawach(dane).SprobujZapisac(oryginal, szkic, out var blad), blad);
        Assert.False(oryginal.OdliczonaOdLicznikow);
        Assert.Equal(3, r1.LicznikHistorycznyWypraw);
        Assert.Equal(5, dane.Ustawienia.HistoryczneWyprawyWgRok[2021]);
        Assert.Equal(4, dane.Ustawienia.HistoryczneWyprawyWgRok[2022]);
    }

    [Fact]
    public void PrzeniesienieZaDzienZerowy_ZwracaWczesniejszaKorekte()
    {
        var (dane, r1, _) = Przyklad();
        var oryginal = new Wyprawa
        {
            Data = new DateOnly(2021, 4, 1), ReplikaGlownaId = r1.Id,
            OdliczonaOdLicznikow = true,
        };
        dane.Wyprawy.Add(oryginal);
        var szkic = OperacjeNaWyprawach.Kopia(oryginal);
        szkic.Data = new DateOnly(2023, 4, 1);

        Assert.True(new OperacjeNaWyprawach(dane).SprobujZapisac(oryginal, szkic, out var blad), blad);
        Assert.False(oryginal.OdliczonaOdLicznikow);
        Assert.Equal(4, r1.LicznikHistorycznyWypraw);
        Assert.Equal(6, dane.Ustawienia.HistoryczneWyprawyWgRok[2021]);
    }

    [Fact]
    public void UsunieciePrzywrocenie_ZachowujeDecyzjeOHistorycznejKorekcie()
    {
        var (dane, r1, _) = Przyklad();
        var wyprawa = new Wyprawa
        {
            Data = new DateOnly(2021, 5, 1), ReplikaGlownaId = r1.Id,
            OdliczonaOdLicznikow = true,
        };
        dane.Wyprawy.Add(wyprawa);
        var operacje = new OperacjeNaWyprawach(dane);

        Assert.True(operacje.SprobujUsun(wyprawa, out var blad), blad);
        Assert.True(wyprawa.CzyUsunieta);
        Assert.True(wyprawa.OdliczonaOdLicznikow);
        Assert.Equal(4, r1.LicznikHistorycznyWypraw);
        Assert.Equal(6, dane.Ustawienia.HistoryczneWyprawyWgRok[2021]);

        Assert.True(operacje.SprobujPrzywroc(wyprawa, false, out blad), blad);
        Assert.False(wyprawa.CzyUsunieta);
        Assert.True(wyprawa.OdliczonaOdLicznikow);
        Assert.Equal(3, r1.LicznikHistorycznyWypraw);
        Assert.Equal(5, dane.Ustawienia.HistoryczneWyprawyWgRok[2021]);
    }

    [Fact]
    public void BrakLicznika_HistorycznaKorektaJestAtomowa()
    {
        var (dane, r1, _) = Przyklad();
        r1.LicznikHistorycznyWypraw = 0;
        var nowa = new Wyprawa { Data = new DateOnly(2021, 1, 1), ReplikaGlownaId = r1.Id };
        var operacje = new OperacjeNaWyprawach(dane);

        Assert.False(operacje.SprobujDodac(nowa, true, out var blad));
        Assert.NotEmpty(blad);
        Assert.Empty(dane.Wyprawy);
        Assert.Equal(0, r1.LicznikHistorycznyWypraw);
        Assert.Equal(5, dane.Ustawienia.HistoryczneWyprawyWgRok[2021]);
        Assert.False(nowa.OdliczonaOdLicznikow);
    }

    [Fact]
    public void StaraBazaBezFlagiKorekty_JestCzytelnaINiePrzeliczanaBezZgody()
    {
        const string json = """
            {
              "SchemaVersion": 1,
              "Repliki": [{ "Id": "stara-replika", "ModelProducenta": "AK", "LicznikHistorycznyWypraw": 7 }],
              "Wyprawy": [{ "Id": "stara-wyprawa", "Data": "2020-04-12", "ReplikaGlownaId": "stara-replika", "Miejsce": "Hostomel" }],
              "Ustawienia": { "DzienZerowy": "2022-12-31", "HistoryczneWyprawyWgRok": { "2020": 12 } }
            }
            """;
        using var strumien = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var dane = MagazynJson.WczytajZeStrumienia(strumien);
        var operacje = new OperacjeNaWyprawach(dane);
        var wyprawa = Assert.Single(dane.Wyprawy);

        Assert.False(wyprawa.OdliczonaOdLicznikow);
        var szkic = OperacjeNaWyprawach.Kopia(wyprawa);
        szkic.Miejsce = "Nowa nazwa";
        Assert.True(operacje.SprobujZapisac(wyprawa, szkic, out var blad), blad);
        Assert.Equal(7, Assert.Single(dane.Repliki).LicznikHistorycznyWypraw);
        Assert.Equal(12, dane.Ustawienia.HistoryczneWyprawyWgRok[2020]);
        Assert.Equal("stara-wyprawa", wyprawa.Id);
        Assert.Equal(1, dane.SchemaVersion);

        using var zapis = new MemoryStream();
        MagazynJson.ZapiszDoStrumienia(dane, zapis);
        zapis.Position = 0;
        var ponownie = MagazynJson.WczytajZeStrumienia(zapis);
        Assert.Equal("Nowa nazwa", Assert.Single(ponownie.Wyprawy).Miejsce);
        Assert.Equal(12, ponownie.Ustawienia.HistoryczneWyprawyWgRok[2020]);
    }

    [Fact]
    public void StaraUsunietaBezFlagi_PowrotBezOdliczeniaNieModyfikujeHistorii()
    {
        var (dane, r1, _) = Przyklad();
        var wyprawa = new Wyprawa
        {
            Data = new DateOnly(2021, 1, 1), ReplikaGlownaId = r1.Id,
            CzyUsunieta = true,
        };
        dane.Wyprawy.Add(wyprawa);
        Assert.True(new OperacjeNaWyprawach(dane).SprobujPrzywroc(wyprawa, false, out var blad), blad);
        Assert.Equal(3, r1.LicznikHistorycznyWypraw);
        Assert.Equal(5, dane.Ustawienia.HistoryczneWyprawyWgRok[2021]);
    }
}
