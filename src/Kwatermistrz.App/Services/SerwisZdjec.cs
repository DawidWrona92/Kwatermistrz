using System.IO;
using System.Windows.Media.Imaging;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.App.Services;

/// <summary>Serwis zdjęć (FR-211): zapis plików do katalogu zdjęć, miniatury, powiększenie. Ścieżki w modelu są względne („zdjecia/…”).</summary>
public class SerwisZdjec
{
    private readonly MagazynJson _magazyn;

    public SerwisZdjec(MagazynJson magazyn)
    {
        _magazyn = magazyn;
    }

    /// <summary>Katalog bezwzględny zdjęć.</summary>
    public string KatalogZdjec => Path.Combine(_magazyn.KatalogDanych, "zdjecia");

    /// <summary>Pełna ścieżka bezwzględna dla ścieżki względnej z modelu; null, gdy brak lub plik nie istnieje.</summary>
    public string? PelnaSciezka(string? sciezkaWzgledna)
    {
        if (string.IsNullOrWhiteSpace(sciezkaWzgledna))
            return null;
        var pelna = Path.GetFullPath(Path.Combine(_magazyn.KatalogDanych, sciezkaWzgledna));
        return File.Exists(pelna) ? pelna : null;
    }

    /// <summary>Wersja statyczna dla konwerterów: ścieżka względna względem katalogu danych aplikacji (App.KatalogDanych).</summary>
    public static string? PelnaSciezkaBezwzgledna(string sciezkaWzgledna)
    {
        if (string.IsNullOrWhiteSpace(sciezkaWzgledna))
            return null;
        var pelna = Path.GetFullPath(Path.Combine(App.KatalogDanych, sciezkaWzgledna));
        return File.Exists(pelna) ? pelna : null;
    }

    /// <summary>Kopiuje wybrany plik do katalogu zdjęć i zwraca ścieżkę względną do zapisu w modelu. Duplikaty nazw dostają sufiks.</summary>
    public string ZapiszDoKatalogu(string sciezkaZrodlowa)
    {
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
        File.Copy(sciezkaZrodlowa, Path.Combine(KatalogZdjec, docelowa));
        return "zdjecia/" + docelowa;
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

    /// <summary>Usuwa plik zdjęcia z katalogu (po usunięciu z modelu). Brak pliku nie jest błędem.</summary>
    public void UsunPlik(string? sciezkaWzgledna)
    {
        var pelna = PelnaSciezka(sciezkaWzgledna);
        if (pelna != null)
        {
            try { File.Delete(pelna); } catch (IOException) { /* plik zajęty — zostaje */ }
        }
    }
}
