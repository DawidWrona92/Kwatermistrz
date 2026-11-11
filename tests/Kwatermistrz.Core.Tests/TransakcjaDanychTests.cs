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

    [Fact]
    public void PoImporcie_IstniejaceRekordyISlownikiZachowujaReferencje()
    {
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        var slowniki = dane.Slowniki;
        var role = dane.Slowniki.Role;
        var rola = role[0];
        var ustawienia = dane.Ustawienia;
        var replika = new Replika
        {
            Id = "replika1",
            ModelProducenta = "Przed zmianą",
            LicznikHistorycznyWypraw = 8,
        };
        var zestaw = new Zestaw { Id = "zestaw1", Nazwa = "Stary zestaw" };
        var wyprawa = new Wyprawa { Id = "wyjazd1", Data = new DateOnly(2020, 6, 1) };
        dane.Repliki.Add(replika);
        dane.Zestawy.Add(zestaw);
        dane.Wyprawy.Add(wyprawa);

        TransakcjaDanych.Wykonaj(dane, robocza =>
        {
            robocza.Repliki[0].ModelProducenta = "Po imporcie";
            robocza.Repliki.Add(new Replika { Id = "replika2", ModelProducenta = "Nowa" });
            robocza.Zestawy[0].Nazwa = "Nowy zestaw";
            robocza.Slowniki.Role[0].Aliasy.Add("Alias z importu");
            robocza.Ustawienia.DzienZerowy = new DateOnly(2022, 1, 1);
            return true;
        });

        Assert.Same(replika, dane.Repliki.Single(r => r.Id == replika.Id));
        Assert.Equal("Po imporcie", replika.ModelProducenta);
        Assert.Equal(8, replika.LicznikHistorycznyWypraw);
        Assert.Same(zestaw, dane.Zestawy.Single(z => z.Id == zestaw.Id));
        Assert.Equal("Nowy zestaw", zestaw.Nazwa);
        Assert.Same(wyprawa, dane.Wyprawy.Single());
        Assert.Same(slowniki, dane.Slowniki);
        Assert.Same(role, dane.Slowniki.Role);
        Assert.Same(rola, dane.Slowniki.Role[0]);
        Assert.Contains("Alias z importu", rola.Aliasy);
        Assert.Same(ustawienia, dane.Ustawienia);
        Assert.Equal(new DateOnly(2022, 1, 1), ustawienia.DzienZerowy);
        Assert.Equal(2, dane.Repliki.Count);
    }

    [Fact]
    public void Przywrocenie_UsuwaNieobecneRekordyAleNieZastępujeObiektowWspolnych()
    {
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        var pozostaje = new Przedmiot { Id = "p1", Nazwa = "Oryginalny" };
        var usuniety = new Przedmiot { Id = "p2", Nazwa = "Tylko lokalny" };
        dane.Przedmioty.AddRange(new[] { pozostaje, usuniety });
        var kolekcja = dane.Przedmioty;
        var przywrocone = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        przywrocone.Przedmioty.Add(new Przedmiot { Id = "p1", Nazwa = "Z kopii", LicznikHistoryczny = 19 });
        przywrocone.Przedmioty.Add(new Przedmiot { Id = "p3", Nazwa = "Zapisana w kopii" });

        TransakcjaDanych.Zastap(dane, przywrocone);

        Assert.Same(kolekcja, dane.Przedmioty);
        Assert.Same(pozostaje, dane.Przedmioty.First(p => p.Id == "p1"));
        Assert.Equal("Z kopii", pozostaje.Nazwa);
        Assert.Equal(19, pozostaje.LicznikHistoryczny);
        Assert.DoesNotContain(dane.Przedmioty, p => p.Id == usuniety.Id);
        Assert.Contains(dane.Przedmioty, p => p.Id == "p3");
        Assert.Equal(1, dane.SchemaVersion);
    }

}
