using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.Core.Tests;

public class TransakcjaDanychTests
{
    [Fact]
    public void WyjatekImportu_NieZmieniaDanychAniHistorycznychLicznikow()
    {
        var dane = new DaneAplikacji();
        var replika = new Replika { Id = "r1", ModelProducenta = "R1", LicznikHistorycznyWypraw = 7 };
        dane.Repliki.Add(replika);
        dane.Ustawienia.HistoryczneWyprawyWgRok[2022] = 11;
        var listaReplik = dane.Repliki;
        var listaWypraw = dane.Wyprawy;
        var schemat = dane.SchemaVersion;

        Assert.Throws<InvalidOperationException>(() =>
            TransakcjaDanych.Wykonaj<int>(dane, robocza =>
            {
                robocza.Repliki[0].LicznikHistorycznyWypraw = 999;
                robocza.Ustawienia.HistoryczneWyprawyWgRok[2022] = 0;
                robocza.Repliki.Add(new Replika { ModelProducenta = "Nowa" });
                robocza.Wyprawy.Add(new Wyprawa { Data = new DateOnly(2024, 6, 6) });
                throw new InvalidOperationException("Nieprawidłowy plik");
            }));
        Assert.Same(listaReplik, dane.Repliki);
        Assert.Same(listaWypraw, dane.Wyprawy);
        Assert.Same(replika, Assert.Single(dane.Repliki));
        Assert.Equal(7, replika.LicznikHistorycznyWypraw);
        Assert.Equal(11, dane.Ustawienia.HistoryczneWyprawyWgRok[2022]);
        Assert.Empty(dane.Wyprawy);
        Assert.Equal(schemat, dane.SchemaVersion);
    }

    [Fact]
    public void SukcesImportu_ZachowujeTozsamoscDanychIListOrazIdHistorycznychRekordow()
    {
        var dane = new DaneAplikacji();
        dane.Repliki.Add(new Replika { Id = "stara", ModelProducenta = "AK" });
        dane.Wyprawy.Add(new Wyprawa { Id = "stara-wyprawa", Data = new DateOnly(2020, 1, 1) });
        dane.Ustawienia.HistoryczneWyprawyWgRok[2020] = 4;
        var listaReplik = dane.Repliki;
        var listaWypraw = dane.Wyprawy;

        var liczba = TransakcjaDanych.Wykonaj(dane, robocza =>
        {
            robocza.Repliki.Add(new Replika { Id = "nowa", ModelProducenta = "G36" });
            return robocza.Repliki.Count;
        });

        Assert.Equal(2, liczba);
        Assert.Same(listaReplik, dane.Repliki);
        Assert.Same(listaWypraw, dane.Wyprawy);
        Assert.Contains(dane.Repliki, r => r.Id == "stara");
        Assert.Contains(dane.Wyprawy, w => w.Id == "stara-wyprawa");
        Assert.Equal(4, dane.Ustawienia.HistoryczneWyprawyWgRok[2020]);
        Assert.Equal(1, dane.SchemaVersion);

        using var zapis = new MemoryStream();
        MagazynJson.ZapiszDoStrumienia(dane, zapis);
        zapis.Position = 0;
        var poWczytaniu = MagazynJson.WczytajZeStrumienia(zapis);
        Assert.Equal(2, poWczytaniu.Repliki.Count);
        Assert.Contains(poWczytaniu.Wyprawy, w => w.Id == "stara-wyprawa");
    }
}
