using System.Text.Json;
using Kwatermistrz.Core.Konfiguracja;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.Core.Tests;

/// <summary>Regresje galerii zdjęć własnych i 10 referencji w zestawach.</summary>
public sealed class GalerieZestawowTests : IDisposable
{
    private readonly string _katalog = Path.Combine(Path.GetTempPath(),
        "kwatermistrz-gallery-" + Guid.NewGuid().ToString("N"));

    public GalerieZestawowTests() => Directory.CreateDirectory(_katalog);

    public void Dispose()
    {
        if (Directory.Exists(_katalog))
            Directory.Delete(_katalog, recursive: true);
    }

    [Fact]
    public void StarszyZestaw_ZachowujeDotychczasoweZdjecia_BezNowychPol()
    {
        var staryJson = """{"Nazwa":"Mój stary zestaw","Zdjecia":["zdjecia/moje.jpg"]}""";
        var zestaw = JsonSerializer.Deserialize<Zestaw>(staryJson)!;

        Assert.Equal("zdjecia/moje.jpg", Assert.Single(zestaw.Zdjecia));
        Assert.Empty(zestaw.ZdjeciaReferencyjne);
        Assert.Empty(zestaw.OpisyZdjec);
    }

    [Fact]
    public void ZapisIOdczyt_WspieraObieGalerieIOpisy()
    {
        var magazyn = new MagazynJson(_katalog);
        var dane = magazyn.Wczytaj();
        dane.Zestawy.Add(new Zestaw
        {
            Nazwa = "FSB — lekko",
            Zdjecia = new() { "zdjecia/moje.jpg" },
            ZdjeciaReferencyjne = new() { "zdjecia/ref1.png", "zdjecia/ref2.png" },
            OpisyZdjec = new() { ["zdjecia/ref1.png"] = "Zdjęcie referencyjne — 2022" },
        });
        magazyn.Zapisz(dane);

        var zapisany = Assert.Single(magazyn.Wczytaj().Zestawy);
        Assert.Single(zapisany.Zdjecia);
        Assert.Equal(2, zapisany.ZdjeciaReferencyjne.Count);
        Assert.Equal("Zdjęcie referencyjne — 2022", zapisany.OpisyZdjec["zdjecia/ref1.png"]);
    }

    [Fact]
    public void Kwreko_ImportNieNadpisujeIstniejacychZdjecIMigrujeOpisy()
    {
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        var rekonstrukcja = new Rekonstrukcja { Nazwa = "FSB" };
        dane.Rekonstrukcje.Add(rekonstrukcja);
        dane.Zestawy.Add(new Zestaw
        {
            Nazwa = "Zestaw z referencją",
            RekonstrukcjaId = rekonstrukcja.Id,
            ZdjeciaReferencyjne = new() { "zdjecia/wyglad.png" },
            OpisyZdjec = new() { ["zdjecia/wyglad.png"] = "Wzorcowe ujęcie" },
        });
        var zrodlo = Path.Combine(_katalog, "zrodlo");
        var katalogZdjec = Path.Combine(zrodlo, "zdjecia");
        Directory.CreateDirectory(katalogZdjec);
        File.WriteAllBytes(Path.Combine(katalogZdjec, "wyglad.png"), [1, 2, 3, 4]);
        var archiwum = Path.Combine(_katalog, "referencje.kwreko");
        EksportRekonstrukcji.Eksportuj(dane, rekonstrukcja.Id, zrodlo, archiwum);

        var cel = Path.Combine(_katalog, "import");
        var docelowyKatalog = Path.Combine(cel, "zdjecia");
        Directory.CreateDirectory(docelowyKatalog);
        var istniejace = Path.Combine(docelowyKatalog, "wyglad.png");
        File.WriteAllBytes(istniejace, [7, 8, 9]);

        var import = EksportRekonstrukcji.Wczytaj(archiwum);
        var skopiowane = EksportRekonstrukcji.WyodrebnijZdjecia(archiwum, cel, import);
        Assert.Equal(1, skopiowane);
        Assert.Equal(new byte[] { 7, 8, 9 }, File.ReadAllBytes(istniejace));
        var zestaw = Assert.Single(import.Zestawy);
        var nowaSciezka = Assert.Single(zestaw.ZdjeciaReferencyjne);
        Assert.StartsWith("zdjecia/wyglad-import-", nowaSciezka);
        Assert.Equal("Wzorcowe ujęcie", zestaw.OpisyZdjec[nowaSciezka]);
        Assert.Equal(new byte[] { 1, 2, 3, 4 },
            File.ReadAllBytes(Path.Combine(cel, nowaSciezka.Replace('/', Path.DirectorySeparatorChar))));

        // Ponowny import tej samej treści do nowego katalogu nie powinien
        // dopisywać kolejnego pliku, gdy lokalny już ma identyczne bajty.
        var drugi = EksportRekonstrukcji.Wczytaj(archiwum);
        var cel2 = Path.Combine(_katalog, "identyczne");
        Directory.CreateDirectory(Path.Combine(cel2, "zdjecia"));
        File.WriteAllBytes(Path.Combine(cel2, "zdjecia", "wyglad.png"), [1, 2, 3, 4]);
        Assert.Equal(0, EksportRekonstrukcji.WyodrebnijZdjecia(archiwum, cel2, drugi));
        Assert.Equal("zdjecia/wyglad.png", Assert.Single(drugi.Zestawy[0].ZdjeciaReferencyjne));
    }

    [Fact]
    public void Kwreko_EksportIImport_ZawieraWlasneIReferencyjnePlikiIOpisy()
    {
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        var rekonstrukcja = new Rekonstrukcja { Nazwa = "Wschód 2022" };
        dane.Rekonstrukcje.Add(rekonstrukcja);
        var zestaw = new Zestaw
        {
            RekonstrukcjaId = rekonstrukcja.Id,
            Nazwa = "Sylwetka A",
            Zdjecia = new() { "zdjecia/sylwetka.jpg" },
            ZdjeciaReferencyjne = new() { "zdjecia/ref.png", "zdjecia/sylwetka.jpg" },
            OpisyZdjec = new() { ["zdjecia/ref.png"] = "Opis reference" },
        };
        dane.Zestawy.Add(zestaw);
        var obrazy = Path.Combine(_katalog, "zdjecia");
        Directory.CreateDirectory(obrazy);
        File.WriteAllBytes(Path.Combine(obrazy, "sylwetka.jpg"), [1, 2, 3, 4]);
        File.WriteAllBytes(Path.Combine(obrazy, "ref.png"), [5, 6, 7, 8]);
        var archiwum = Path.Combine(_katalog, "sylwetka.kwreko");

        var wynik = EksportRekonstrukcji.Eksportuj(dane, rekonstrukcja.Id, _katalog, archiwum);
        Assert.Equal(1, wynik.Zestawy);
        Assert.Equal(2, wynik.Zdjecia); // duplikat w obu galeriach pakujemy tylko raz
        Assert.Equal(new[] { "zdjecia/ref.png", "zdjecia/sylwetka.jpg" }, EksportRekonstrukcji.WypiszZdjecia(archiwum));

        var import = EksportRekonstrukcji.Wczytaj(archiwum);
        var odtworzony = Assert.Single(import.Zestawy);
        Assert.Single(odtworzony.Zdjecia);
        Assert.Equal(2, odtworzony.ZdjeciaReferencyjne.Count);
        Assert.Equal("Opis reference", odtworzony.OpisyZdjec["zdjecia/ref.png"]);

        var cel = Path.Combine(_katalog, "przywrocone");
        var wypakowane = EksportRekonstrukcji.WyodrebnijZdjecia(archiwum, cel);
        Assert.Equal(2, wypakowane);
        Assert.Equal(new byte[] { 5, 6, 7, 8 }, File.ReadAllBytes(Path.Combine(cel, "zdjecia", "ref.png")));
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, File.ReadAllBytes(Path.Combine(cel, "zdjecia", "sylwetka.jpg")));
    }
}
