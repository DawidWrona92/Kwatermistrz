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
            // ustawienia z licznikami historycznymi wg rok (bez osobistych: motyw, okno, ostatnie filtry)
            Ustawienia = new Ustawienia
            {
                DzienZerowy = dane.Ustawienia.DzienZerowy,
                HistoryczneWyprawyWgRok = dane.Ustawienia.HistoryczneWyprawyWgRok,
                DataPierwszejWyprawy = dane.Ustawienia.DataPierwszejWyprawy,
                MaksymalnyBokZdjecia = dane.Ustawienia.MaksymalnyBokZdjecia,
                ZachowajOryginalZdjecia = dane.Ustawienia.ZachowajOryginalZdjecia,
                OstatniaRekonstrukcjaId = rekonstrukcja.Id,
            },
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
        foreach (var z in zestawy)
        {
            Dodaj(z.Zdjecia);
            Dodaj(z.ZdjeciaReferencyjne);
        }
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

    /// <summary>
    /// Wyodrębnia zdjęcia z eksportu; istniejących lokalnych plików nie nadpisuje.
    /// Jeśli zawartość zdjęcia o tej samej ścieżce jest inna, nadaje unikatową
    /// nazwę i aktualizuje referencje w importowanym modelu (przed scaleniem).
    /// </summary>
    public static int WyodrebnijZdjecia(string sciezka, string katalogDanych, DaneAplikacji? daneImportu = null)
    {
        var liczba = 0;
        var katalogPelny = Path.GetFullPath(katalogDanych);
        var mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var zip = ZipFile.OpenRead(sciezka);
        foreach (var wpis in zip.Entries.Where(e =>
            e.FullName.StartsWith(KatalogZdjec, StringComparison.Ordinal)))
        {
            if (string.IsNullOrWhiteSpace(wpis.Name))
                continue;
            var wzgledna = wpis.FullName[KatalogZdjec.Length..];
            var normalizowana = wzgledna.Replace('\\', '/');
            var pelna = Path.GetFullPath(Path.Combine(katalogPelny,
                normalizowana.Replace('/', Path.DirectorySeparatorChar)));
            // Zip-slip / absolute paths / katalog danych: nic poza root.
            if (!pelna.StartsWith(katalogPelny + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
                continue;

            if (File.Exists(pelna))
            {
                // Identyczna zawartość nie wymaga ponownego zapisu.
                using var zrodloPorownanie = wpis.Open();
                using var lokalny = File.OpenRead(pelna);
                var hashNowy = System.Security.Cryptography.SHA256.HashData(zrodloPorownanie);
                var hashLokalny = System.Security.Cryptography.SHA256.HashData(lokalny);
                if (hashNowy.SequenceEqual(hashLokalny))
                    continue;
                if (daneImportu == null)
                    continue; // Stare API bez mapowania nie nadpisuje lokalnych plików.
                var folder = Path.GetDirectoryName(pelna)!;
                var baza = Path.GetFileNameWithoutExtension(pelna);
                var ext = Path.GetExtension(pelna);
                string nowa;
                do
                {
                    nowa = Path.Combine(folder, baza + "-import-" +
                        Guid.NewGuid().ToString("N")[..10] + ext);
                } while (File.Exists(nowa));
                mapa[normalizowana] = Path.GetRelativePath(katalogPelny, nowa)
                    .Replace('\\', '/');
                pelna = nowa;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(pelna)!);
            using var zrodlo = wpis.Open();
            using var docel = new FileStream(pelna, FileMode.CreateNew, FileAccess.Write);
            zrodlo.CopyTo(docel);
            liczba++;
        }
        if (daneImportu != null && mapa.Count > 0)
            PrzemapujZdjecia(daneImportu, mapa);
        return liczba;
    }

    private static void PrzemapujZdjecia(DaneAplikacji dane, Dictionary<string, string> mapa)
    {
        string Przemapuj(string sciezka)
            => mapa.TryGetValue(sciezka.Replace('\\', '/'), out var nowa) ? nowa : sciezka;
        void Lista(List<string> zdjecia)
        {
            for (var i = 0; i < zdjecia.Count; i++)
                zdjecia[i] = Przemapuj(zdjecia[i]);
        }
        foreach (var p in dane.Przedmioty)
            Lista(p.Zdjecia);
        foreach (var r in dane.Repliki)
            Lista(r.Zdjecia);
        foreach (var w in dane.Wyprawy)
            Lista(w.Zdjecia);
        foreach (var z in dane.Zestawy)
        {
            Lista(z.Zdjecia);
            Lista(z.ZdjeciaReferencyjne);
            z.OpisyZdjec = z.OpisyZdjec.ToDictionary(
                para => Przemapuj(para.Key), para => para.Value, StringComparer.OrdinalIgnoreCase);
        }
        foreach (var r in dane.Rekonstrukcje)
            if (r.Zdjecie != null)
                r.Zdjecie = Przemapuj(r.Zdjecie);
    }

}
