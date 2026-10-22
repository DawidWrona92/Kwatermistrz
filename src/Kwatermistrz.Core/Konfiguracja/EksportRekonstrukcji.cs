using System.IO.Compression;
using System.Text.Json;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.Core.Konfiguracja;

/// <summary>
/// Eksport i import całej rekonstrukcji (dane + zdjęcia) do jednego pliku .kwreko
/// — do przekazania koledze lub przeniesienia między instalacjami (FR-1405 [C]).
/// Plik to zip: dane.json (format magazynu) + zdjecia/ (pliki zdjęć rekonstrukcji i jej elementów).
/// </summary>
public static class EksportRekonstrukcji
{
    public const string Rozszerzenie = ".kwreko";
    private const string NazwaPlikuDanych = "dane.json";
    private const string KatalogZdjec = "zdjecia/";

    /// <summary>Zapisuje eksport rekonstrukcji do pliku zip.</summary>
    /// <param name="dane">Wszystkie dane aplikacji.</param>
    /// <param name="rekonstrukcjaId">Id eksportowanej rekonstrukcji.</param>
    /// <param name="katalogDanych">Katalog danych aplikacji (źródło zdjęć); może być null — wtedy bez zdjęć.</param>
    /// <param name="sciezka">Docelowy plik .kwreko.</param>
    /// <returns>Liczby: elementów i zdjęć w eksporcie.</returns>
    public static (int Przedmioty, int Repliki, int Zestawy, int Wyprawy, int Zadania, int Zdjecia) Eksportuj(
        DaneAplikacji dane, string rekonstrukcjaId, string? katalogDanych, string sciezka)
    {
        var rekonstrukcja = dane.Rekonstrukcje.FirstOrDefault(r => r.Id == rekonstrukcjaId)
            ?? throw new InvalidOperationException("Nie znaleziono rekonstrukcji.");

        // elementy przypisane do rekonstrukcji + wspólne (bez przypisania)
        var przedmioty = dane.Przedmioty
            .Where(p => p.RekonstrukcjeId.Contains(rekonstrukcjaId) || p.RekonstrukcjeId.Count == 0)
            .ToList();
        var repliki = dane.Repliki
            .Where(r => r.RekonstrukcjeId.Contains(rekonstrukcjaId) || r.RekonstrukcjeId.Count == 0)
            .ToList();
        var zestawy = dane.Zestawy
            .Where(z => z.RekonstrukcjaId == rekonstrukcjaId || z.RekonstrukcjaId == null)
            .ToList();
        var replikiId = repliki.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        var przedmiotyId = przedmioty.Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
        var zestawyId = zestawy.Select(z => z.Id).ToHashSet(StringComparer.Ordinal);
        var wyprawy = dane.Wyprawy
            .Where(w => w.RekonstrukcjaId == rekonstrukcjaId
                || (w.ReplikaGlownaId != null && replikiId.Contains(w.ReplikaGlownaId))
                || (w.ZestawId != null && zestawyId.Contains(w.ZestawId)))
            .ToList();
        var zadania = dane.Zadania
            .Where(z => (z.DotyczyTyp == "Replika" && z.DotyczyId != null && replikiId.Contains(z.DotyczyId))
                || (z.DotyczyTyp == "Przedmiot" && z.DotyczyId != null && przedmiotyId.Contains(z.DotyczyId))
                || (z.DotyczyTyp == "Zestaw" && z.DotyczyId != null && zestawyId.Contains(z.DotyczyId)))
            .ToList();

        // dane eksportu: ta sama rekonstrukcja + powiązane elementy + puste listy pozostałych rekonstrukcji
        var daneEksportu = new DaneAplikacji
        {
            SchemaVersion = dane.SchemaVersion,
            WersjaRegul = dane.WersjaRegul,
            Slowniki = dane.Slowniki, // słowniki współdzielone — odbiorca scala przez pakiet/import (FR-1404)
            Przedmioty = przedmioty,
            Repliki = repliki,
            Zestawy = zestawy,
            Wyprawy = wyprawy,
            Rekonstrukcje = new List<Rekonstrukcja> { rekonstrukcja },
            SzablonyKart = dane.SzablonyKart,
            Zadania = zadania,
            Ustawienia = new Ustawienia(),
            CzyPierwszeUruchomienie = false,
        };

        // zbierz ścieżki zdjęć (względne względem katalogu danych)
        var sciezkiZdjec = new SortedSet<string>(StringComparer.Ordinal);
        void Dodaj(IEnumerable<string>? sciezki)
        {
            if (sciezki == null)
                return;
            foreach (var s in sciezki)
                if (!string.IsNullOrWhiteSpace(s))
                    sciezkiZdjec.Add(s);
        }
        if (rekonstrukcja.Zdjecie != null)
            sciezkiZdjec.Add(rekonstrukcja.Zdjecie);
        foreach (var p in przedmioty) Dodaj(p.Zdjecia);
        foreach (var r in repliki) Dodaj(r.Zdjecia);
        foreach (var z in zestawy) Dodaj(z.Zdjecia);
        foreach (var w in wyprawy) Dodaj(w.Zdjecia);

        var liczbaZdjec = 0;
        using (var zip = ZipFile.Open(sciezka, ZipArchiveMode.Create))
        {
            var wpisDane = zip.CreateEntry(NazwaPlikuDanych);
            using (var strumien = wpisDane.Open())
                MagazynJson.ZapiszDoStrumienia(daneEksportu, strumien);

            if (katalogDanych != null)
            {
                foreach (var sciezkaWzgl in sciezkiZdjec)
                {
                    var pelna = Path.Combine(katalogDanych, sciezkaWzgl.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(pelna))
                        continue;
                    var wpis = zip.CreateEntry(KatalogZdjec + sciezkaWzgl.Replace('\\', '/'));
                    using var zrodlo = File.OpenRead(pelna);
                    using var docel = wpis.Open();
                    zrodlo.CopyTo(docel);
                    liczbaZdjec++;
                }
            }
        }
        return (przedmioty.Count, repliki.Count, zestawy.Count, wyprawy.Count, zadania.Count, liczbaZdjec);
    }

    /// <summary>Wczytuje eksport rekonstrukcji z pliku .kwreko; nic nie zapisuje do danych aplikacji.</summary>
    public static DaneAplikacji Wczytaj(string sciezka)
    {
        using var zip = ZipFile.OpenRead(sciezka);
        var wpisDane = zip.GetEntry(NazwaPlikuDanych) ?? throw new InvalidOperationException("Brak dane.json w pliku eksportu.");
        using var strumien = wpisDane.Open();
        return MagazynJson.WczytajZeStrumienia(strumien);
    }

    /// <summary>Wypisuje ścieżki zdjęć zapisane w pliku eksportu (względne, jak w katalogu danych).</summary>
    public static List<string> WypiszZdjecia(string sciezka)
    {
        using var zip = ZipFile.OpenRead(sciezka);
        return zip.Entries
            .Where(e => e.FullName.StartsWith(KatalogZdjec, StringComparison.Ordinal))
            .Select(e => e.FullName[KatalogZdjec.Length..])
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Wyodrębnia zdjęcia z eksportu do katalogu danych aplikacji (zwraca liczbę plików).</summary>
    public static int WyodrebnijZdjecia(string sciezka, string katalogDanych)
    {
        var liczba = 0;
        using var zip = ZipFile.OpenRead(sciezka);
        foreach (var wpis in zip.Entries.Where(e => e.FullName.StartsWith(KatalogZdjec, StringComparison.Ordinal)))
        {
            var wzgledna = wpis.FullName[KatalogZdjec.Length..];
            var pelna = Path.Combine(katalogDanych, wzgledna.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(pelna)!);
            using var zrodlo = wpis.Open();
            using var docel = File.Create(pelna);
            zrodlo.CopyTo(docel);
            liczba++;
        }
        return liczba;
    }
}
