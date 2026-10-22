using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.Core.Tests;

/// <summary>Testy magazynu: zapis atomowy, kontrola integralności, kopia zapasowa (NFR-03, NFR-05, FR-1107).</summary>
public class MagazynJsonTests : IDisposable
{
    private readonly string _katalog;

    public MagazynJsonTests()
    {
        _katalog = Path.Combine(Path.GetTempPath(), "kwatermistrz-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_katalog);
    }

    public void Dispose()
    {
        if (Directory.Exists(_katalog))
            Directory.Delete(_katalog, recursive: true);
    }

    [Fact]
    public void Wczytaj_PierwszyRaz_DajePustaAplikacjeZeSlownikamiStartowymi()
    {
        var magazyn = new MagazynJson(_katalog);
        var dane = magazyn.Wczytaj();

        Assert.True(dane.CzyPierwszeUruchomienie);
        Assert.Equal(3, dane.Slowniki.Strony.Count);
        Assert.Equal(14, dane.Slowniki.Kategorie.Count);
        Assert.Equal(2, dane.SzablonyKart.Count);
        Assert.Empty(dane.Repliki);
        Assert.Empty(dane.Przedmioty);
    }

    [Fact]
    public void Zapisz_Wczytaj_Roundtrip_ZPolskimiZnakami()
    {
        var magazyn = new MagazynJson(_katalog);
        var dane = magazyn.Wczytaj();
        dane.Przedmioty.Add(new Przedmiot { Nazwa = "Kamizelka 6Sh117 „Zjuratkul”", Komentarz = "Półprecyzyjna 6,01" });
        dane.Wyprawy.Add(new Wyprawa { Data = new DateOnly(2026, 7, 19), Miejsce = "Gostomel" });
        dane.Ustawienia.DataPierwszejWyprawy = new DateOnly(2022, 9, 1);
        magazyn.Zapisz(dane);

        var wczytane = magazyn.Wczytaj();
        Assert.Single(wczytane.Przedmioty);
        Assert.Equal("Kamizelka 6Sh117 „Zjuratkul”", wczytane.Przedmioty[0].Nazwa);
        Assert.Equal(new DateOnly(2022, 9, 1), wczytane.Ustawienia.DataPierwszejWyprawy);
    }

    [Fact]
    public void Zapisz_PoAwariiProcesu_PlikTymczasowyNieZostajeGlownym()
    {
        var magazyn = new MagazynJson(_katalog);
        magazyn.Zapisz(magazyn.Wczytaj());

        // symulacja przerwanego zapisu: zostaje tylko plik .tmp
        File.WriteAllText(Path.Combine(_katalog, "dane.json.tmp"), "{ nieprawidłowy");

        var (dane, kontrola) = magazyn.WczytajZKontrola();
        Assert.True(kontrola.CzyPoprawne);
        Assert.NotNull(dane);
    }

    [Fact]
    public void WczytajZKontrola_UszkodzonyPlik_OdczytujeKopieBak()
    {
        var magazyn = new MagazynJson(_katalog);
        var dane = magazyn.Wczytaj();
        dane.Przedmioty.Add(new Przedmiot { Nazwa = "Test" });
        magazyn.Zapisz(dane);
        magazyn.Zapisz(dane); // drugi zapis tworzy .bak

        File.WriteAllText(magazyn.SciezkaPliku, "{ uszkodzony json");

        var (wczytane, kontrola) = magazyn.WczytajZKontrola();
        Assert.False(kontrola.CzyPoprawne);
        Assert.Single(wczytane.Przedmioty);
    }

    [Fact]
    public void UtworzKopieZapasowa_KopiujeDaneIZdjecia()
    {
        var magazyn = new MagazynJson(_katalog);
        var dane = magazyn.Wczytaj();
        dane.Przedmioty.Add(new Przedmiot { Nazwa = "X", Zdjecia = new() { "a.jpg" } });
        magazyn.Zapisz(dane);
        Directory.CreateDirectory(Path.Combine(_katalog, "zdjecia"));
        File.WriteAllText(Path.Combine(_katalog, "zdjecia", "a.jpg"), "foto");

        var sciezkaKopii = magazyn.UtworzKopieZapasowa();

        Assert.True(File.Exists(Path.Combine(sciezkaKopii, "dane.json")));
        Assert.True(File.Exists(Path.Combine(sciezkaKopii, "zdjecia", "a.jpg")));
    }

    [Fact]
    public void WczytajZPliku_PoprawnyPlik_ZwracaDane()
    {
        var magazyn = new MagazynJson(_katalog);
        var dane = magazyn.Wczytaj();
        dane.Przedmioty.Add(new Przedmiot { Nazwa = "Przywraczany", Firma = "Test" });
        magazyn.Zapisz(dane);

        var wczytane = MagazynJson.WczytajZPliku(magazyn.SciezkaPliku);

        Assert.Single(wczytane.Przedmioty);
        Assert.Equal("Przywraczany", wczytane.Przedmioty[0].Nazwa);
    }

    [Fact]
    public void WczytajZPliku_NieistniejacyPlik_RzucaWyjatek()
    {
        Assert.Throws<FileNotFoundException>(() =>
            MagazynJson.WczytajZPliku(Path.Combine(_katalog, "nie-ma.json")));
    }
}
