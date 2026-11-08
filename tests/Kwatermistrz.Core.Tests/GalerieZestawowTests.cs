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
        Assert.Equal(new[] { "ref.png", "sylwetka.jpg" }, EksportRekonstrukcji.WypiszZdjecia(archiwum));

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
