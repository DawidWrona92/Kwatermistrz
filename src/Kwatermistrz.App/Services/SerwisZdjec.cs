using System.IO;
using System.Windows.Media.Imaging;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.App.Services;

/// <summary>Serwis zdjęć (FR-211): zapis plików do katalogu zdjęć, miniatury, powiększenie. Ścieżki w modelu są względne („zdjecia/…”).</summary>
public class SerwisZdjec
{
    private readonly MagazynJson _magazyn;
    private DaneAplikacji? _dane;

    public SerwisZdjec(MagazynJson magazyn)
    {
        _magazyn = magazyn;
    }

    /// <summary>Ustawia dane aplikacji — używane do odczytu ustawień NFR-11 (limit rozmiaru zdjęć).</summary>
    public void UstawDane(DaneAplikacji dane) => _dane = dane;

    /// <summary>Katalog bezwzględny zdjęć.</summary>
    public string KatalogZdjec => Path.Combine(_magazyn.KatalogDanych, "zdjecia");

    /// <summary>Pełna ścieżka bezwzględna dla ścieżki względnej z modelu; null, gdy brak, plik nie istnieje lub ścieżka wychodzi poza katalog danych (dane z importu mogą zawierać „../”).</summary>
    public string? PelnaSciezka(string? sciezkaWzgledna)
        => PelnaSciezkaWalidowana(sciezkaWzgledna, _magazyn.KatalogDanych);

    /// <summary>Wersja statyczna dla konwerterów (brak dostępu do instancji): ścieżka względna względem katalogu danych aplikacji (App.KatalogDanych — ten sam katalog co _magazyn.KatalogDanych; obie ścieżki mają wspólną walidację).</summary>
    public static string? PelnaSciezkaBezwzgledna(string sciezkaWzgledna)
        => PelnaSciezkaWalidowana(sciezkaWzgledna, App.KatalogDanych);

    private static string? PelnaSciezkaWalidowana(string? sciezkaWzgledna, string katalogDanych)
    {
        if (string.IsNullOrWhiteSpace(sciezkaWzgledna))
            return null;
        var katalogPelny = Path.GetFullPath(katalogDanych);
        var pelna = Path.GetFullPath(Path.Combine(katalogPelny, sciezkaWzgledna));
        // strażnik: ścieżka spoza katalogu danych odrzucamy (nie czytamy, nie usuwamy)
        if (!pelna.StartsWith(katalogPelny + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            return null;
        return File.Exists(pelna) ? pelna : null;
    }

    /// <summary>Kopiuje wybrany plik do katalogu zdjęć i zwraca ścieżkę względną do zapisu w modelu. Duplikaty nazw dostają sufiks. NFR-11: przy ustawionym limicie dłuższy bok jest zmniejszany do 2048 px (oryginał opcjonalnie zachowany).</summary>
    public string ZapiszDoKatalogu(string sciezkaZrodlowa, int? maksymalnyBok = null, bool? zachowajOryginal = null)
    {
        // NFR-11: domyślnie wartości z ustawień aplikacji
        maksymalnyBok ??= _dane?.Ustawienia.MaksymalnyBokZdjecia ?? 2048;
        zachowajOryginal ??= _dane?.Ustawienia.ZachowajOryginalZdjecia ?? false;
        var limit = maksymalnyBok.Value;
        var zachowaj = zachowajOryginal.Value;
    
        Directory.CreateDirectory(KatalogZdjec);
        var nazwa = Path.GetFileName(sciezkaZrodlowa);
        var bezRozszerzenia = Path.GetFileNameWithoutExtension(nazwa);
        var rozszerzenie = Path.GetExtension(nazwa);
        var docelowa = nazwa;
        var i = 1;
        while (File.Exists(Path.Combine(KatalogZdjec, docelowa)))
        {
            docelowa = $"{bezRozszerzenia}-{i}{rozszerzenie}";
            i++;
        }
        var pelnaDocelowa = Path.Combine(KatalogZdjec, docelowa);
        if (limit > 0 && CzyObrazWiekszyNizLimit(sciezkaZrodlowa, limit))
        {
            if (zachowaj)
            {
                var katalogOryginalow = Path.Combine(KatalogZdjec, "oryginaly");
                Directory.CreateDirectory(katalogOryginalow);
                File.Copy(sciezkaZrodlowa, Path.Combine(katalogOryginalow, docelowa), overwrite: true);
            }
            docelowa = ZapiszZmniejszone(sciezkaZrodlowa, pelnaDocelowa, limit);
        }
        else
        {
            File.Copy(sciezkaZrodlowa, pelnaDocelowa);
        }
        return "zdjecia/" + docelowa;
    }

    private static bool CzyObrazWiekszyNizLimit(string sciezka, int limit)
    {
        try
        {
            using var strumien = File.OpenRead(sciezka);
            var obraz = new BitmapImage();
            obraz.BeginInit();
            obraz.CacheOption = BitmapCacheOption.OnLoad;
            obraz.StreamSource = strumien;
            obraz.EndInit();
            return Math.Max(obraz.PixelWidth, obraz.PixelHeight) > limit;
        }
        catch (Exception)
        {
            return false; // nie obraz lub uszkodzony — kopiujemy jak jest
        }
    }

    /// <summary>Zapisuje zmniejszony obraz; zwraca nazwę pliku docelowego (przy nieznanym rozszerzeniu — .jpg).</summary>
    private static string ZapiszZmniejszone(string sciezkaZrodlowa, string pelnaDocelowa, int maksymalnyBok)
    {
        using var strumien = File.OpenRead(sciezkaZrodlowa);
        var dekorator = new TransformedBitmap();
        var obraz = new BitmapImage();
        obraz.BeginInit();
        obraz.CacheOption = BitmapCacheOption.OnLoad;
        obraz.StreamSource = strumien;
        obraz.EndInit();
        var skala = Math.Min(1.0, (double)maksymalnyBok / Math.Max(obraz.PixelWidth, obraz.PixelHeight));
        dekorator.BeginInit();
        dekorator.Source = obraz;
        dekorator.Transform = new System.Windows.Media.ScaleTransform(skala, skala);
        dekorator.EndInit();

        BitmapEncoder enkoder = Path.GetExtension(pelnaDocelowa).ToLowerInvariant() switch
        {
            ".png" => new PngBitmapEncoder(),
            ".bmp" => new BmpBitmapEncoder(),
            _ => new JpegBitmapEncoder { QualityLevel = 90 },
        };
        enkoder.Frames.Add(BitmapFrame.Create(dekorator));
        // nieznane rozszerzenie (np. .webp, .gif — dialog ich nie dopuszcza, dane mogą zawierać takie ścieżki z zewnątrz) → zapisujemy jako JPEG i korygujemy nazwę pliku
        if (enkoder is JpegBitmapEncoder && !pelnaDocelowa.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            && !pelnaDocelowa.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            pelnaDocelowa = Path.ChangeExtension(pelnaDocelowa, ".jpg");
        }
        using var wyjscie = File.Create(pelnaDocelowa);
        enkoder.Save(wyjscie);
        return Path.GetFileName(pelnaDocelowa);
    }

    /// <summary>Wczytuje obraz jako zamrożony BitmapImage (do miniatur i podglądu).</summary>
    public static BitmapImage? WczytajObraz(string pelnaSciezka, int? docelowaSzerokosc = null)
    {
        try
        {
            var obraz = new BitmapImage();
            obraz.BeginInit();
            obraz.CacheOption = BitmapCacheOption.OnLoad;
            obraz.UriSource = new Uri(pelnaSciezka, UriKind.Absolute);
            if (docelowaSzerokosc is > 0)
            {
                obraz.DecodePixelWidth = docelowaSzerokosc.Value;
            }
            obraz.EndInit();
            obraz.Freeze();
            return obraz;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Usuwa plik zdjęcia z katalogu (po usunięciu z modelu) — wraz z ewentualną kopią w podkatalogu oryginaly/ (NFR-11). Brak pliku nie jest błędem.</summary>
    public void UsunPlik(string? sciezkaWzgledna)
    {
        var pelna = PelnaSciezka(sciezkaWzgledna);
        if (pelna != null)
        {
            try { File.Delete(pelna); } catch (IOException) { /* plik zajęty — zostaje */ }
            var oryginal = Path.Combine(KatalogZdjec, "oryginaly", Path.GetFileName(pelna));
            if (File.Exists(oryginal))
            {
                try { File.Delete(oryginal); } catch (IOException) { /* plik zajęty — zostaje */ }
            }
        }
    }
}
