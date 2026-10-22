using System.IO.Compression;
using Kwatermistrz.Core.Konfiguracja;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Obliczenia;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.Core.Tests;

/// <summary>Testy napraw z raportu przegl\u0105du (wydanie 1.0.1): cykl komplet\u00f3w, odczyt magazynu, zip-slip, eksport ustawie\u0144.</summary>
public class Naprawy101Tests
{
    private static (DaneAplikacji dane, Kalkulator kalk) Przygotuj()
    {
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        return (dane, new Kalkulator(dane));
    }

    [Fact]
    public void UzyciaPrzedmiotu_CyklKompletowNieWywolujeStackOverflow()
    {
        var (dane, kalk) = Przygotuj();
        var a = new Przedmiot { Nazwa = "A", LicznikHistoryczny = 3 };
        var b = new Przedmiot { Nazwa = "B", LicznikHistoryczny = 7 };
        a.KompletId = b.Id;
        b.KompletId = a.Id; // cykl A<->B
        dane.Przedmioty.Add(a);
        dane.Przedmioty.Add(b);
        dane.Wyprawy.Add(new Wyprawa { Data = new DateOnly(2026, 1, 1), SkladReczny = new List<ElementWariantu> { new() { Typ = TypElementu.Przedmiot, ElementId = a.Id } } });

        // przed napraw\u0105: StackOverflowException (proces ginie); teraz: sko\u0144czona warto\u015b\u0107
        var uzyciaA = kalk.UzyciaPrzedmiotu(a);
        var uzyciaB = kalk.UzyciaPrzedmiotu(b);
        Assert.True(uzyciaA >= 3);
        Assert.True(uzyciaB >= 7);

        var indeks = new IndeksObliczeniowy(dane);
        Assert.True(indeks.UzyciaPrzedmiotu(a) >= 3);
        Assert.True(indeks.UzyciaPrzedmiotu(b) >= 7);
    }

    [Fact]
    public void CzyTworzyCyklKompletu_WykrywaCyklBezposredniIZagniezdzony()
    {
        var (dane, kalk) = Przygotuj();
        var a = new Przedmiot { Nazwa = "A" };
        var b = new Przedmiot { Nazwa = "B" };
        var c = new Przedmiot { Nazwa = "C" };
        dane.Przedmioty.AddRange(new[] { a, b, c });

        Assert.False(kalk.CzyTworzyCyklKompletu(a.Id, b.Id)); // A -> B: OK
        b.KompletId = a.Id;
        Assert.True(kalk.CzyTworzyCyklKompletu(a.Id, b.Id)); // A -> B -> A: cykl
        Assert.False(kalk.CzyTworzyCyklKompletu(b.Id, c.Id)); // B -> C: OK
        c.KompletId = b.Id;
        Assert.True(kalk.CzyTworzyCyklKompletu(a.Id, c.Id)); // A -> C -> B -> A: cykl
        Assert.True(kalk.CzyTworzyCyklKompletu(c.Id, c.Id)); // w\u0142asny komplet: cykl
    }

    [Fact]
    public void MagazynJson_UszkodzonyGlownyIPustyBak_DajePustyDokumentBezWyjatku()
    {
        var katalog = Path.Combine(Path.GetTempPath(), "kw-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(katalog);
            File.WriteAllText(Path.Combine(katalog, "dane.json"), "{ to nie jest json");
            File.WriteAllText(Path.Combine(katalog, "dane.json.bak"), ""); // pusty .bak

            var magazyn = new MagazynJson(katalog);
            var (dane, kontrola) = magazyn.WczytajZKontrola();

            Assert.False(kontrola.CzyPoprawne);
            Assert.NotNull(kontrola.Komunikat);
            Assert.NotNull(dane); // przed napraw\u0105: wyj\u0105tek przy odczycie pustego .bak
        }
        finally
        {
            Directory.Delete(katalog, recursive: true);
        }
    }

    [Fact]
    public void MagazynJson_UszkodzonyGlownyIDobryBak_OdczytujeBak()
    {
        var katalog = Path.Combine(Path.GetTempPath(), "kw-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var magazyn = new MagazynJson(katalog);
            var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
            dane.Repliki.Add(new Replika { ModelProducenta = "R1" });
            magazyn.Zapisz(dane);
            magazyn.Zapisz(dane); // drugi zapis tworzy dane.json.bak
            File.WriteAllText(Path.Combine(katalog, "dane.json"), "zepsute");

            var (wczytane, kontrola) = new MagazynJson(katalog).WczytajZKontrola();

            Assert.False(kontrola.CzyPoprawne);
            Assert.Single(wczytane.Repliki);
        }
        finally
        {
            Directory.Delete(katalog, recursive: true);
        }
    }

    [Fact]
    public void WyodrebnijZdjecia_PomijaWpisySpozaKataloguDanych()
    {
        var katalog = Path.Combine(Path.GetTempPath(), "kw-test-" + Guid.NewGuid().ToString("N"));
        var plik = Path.Combine(Path.GetTempPath(), "kw-test-" + Guid.NewGuid().ToString("N") + ".kwreko");
        try
        {
            Directory.CreateDirectory(katalog);
            var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
            using (var zip = ZipFile.Open(plik, ZipArchiveMode.Create))
            {
                var wpis = zip.CreateEntry("dane.json");
                using (var str = wpis.Open())
                    MagazynJson.ZapiszDoStrumienia(dane, str);
                // konwencja eksportu: wpis = "zdjecia/" + ścieżka z modelu ("zdjecia/foto.jpg")
                var zlosliwy = zip.CreateEntry("zdjecia/../../evil.txt");
                using (var str = zlosliwy.Open())
                using (var pisarz = new StreamWriter(str))
                    pisarz.Write("x");
                var dobry = zip.CreateEntry("zdjecia/zdjecia/foto.jpg");
                using (var str = dobry.Open())
                using (var pisarz = new StreamWriter(str))
                    pisarz.Write("foto");
            }

            var liczba = EksportRekonstrukcji.WyodrebnijZdjecia(plik, katalog);

            Assert.Equal(1, liczba); // tylko dobry wpis
            Assert.True(File.Exists(Path.Combine(katalog, "zdjecia", "foto.jpg")));
            Assert.False(File.Exists(Path.Combine(katalog, "..", "evil.txt")));
            Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(katalog)!, "evil.txt")));
        }
        finally
        {
            if (Directory.Exists(katalog)) Directory.Delete(katalog, recursive: true);
            if (File.Exists(plik)) File.Delete(plik);
            var evil = Path.Combine(Path.GetTempPath(), "evil.txt");
            if (File.Exists(evil)) File.Delete(evil);
        }
    }

    [Fact]
    public void EksportRekonstrukcji_ZabieraUstawieniaZLicznikamiHistorycznymi()
    {
        var katalog = Path.Combine(Path.GetTempPath(), "kw-test-" + Guid.NewGuid().ToString("N"));
        var plik = Path.Combine(Path.GetTempPath(), "kw-test-" + Guid.NewGuid().ToString("N") + ".kwreko");
        try
        {
            Directory.CreateDirectory(katalog);
            var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
            dane.Rekonstrukcje.Add(new Rekonstrukcja { Nazwa = "Test" });
            dane.Ustawienia.HistoryczneWyprawyWgRok[2024] = 12;
            dane.Ustawienia.DzienZerowy = new DateOnly(2025, 1, 1);
            dane.Ustawienia.Motyw = Motyw.Ciemny; // osobiste \u2014 nie powinno wędrować

            EksportRekonstrukcji.Eksportuj(dane, dane.Rekonstrukcje[0].Id, katalog, plik);
            var wczytane = EksportRekonstrukcji.Wczytaj(plik);

            Assert.Equal(12, wczytane.Ustawienia.HistoryczneWyprawyWgRok[2024]);
            Assert.Equal(new DateOnly(2025, 1, 1), wczytane.Ustawienia.DzienZerowy);
            Assert.Equal(Motyw.Systemowy, wczytane.Ustawienia.Motyw); // domyślny, nie osobisty
        }
        finally
        {
            if (Directory.Exists(katalog)) Directory.Delete(katalog, recursive: true);
            if (File.Exists(plik)) File.Delete(plik);
        }
    }
}
