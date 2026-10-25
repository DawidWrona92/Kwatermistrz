using System.Globalization;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Obliczenia;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Kwatermistrz.Core.Eksport;

/// <summary>Wiersz arkusza eksportu: wartości tekstowe (puste = null) + opcjonalna liczba do komórek numerycznych.</summary>
public class WierszEksportu
{
    public List<string?> Komorki { get; set; } = new();
    public bool CzyNaglowek { get; set; }
    public bool CzySuma { get; set; }

    public WierszEksportu(params string?[] komorki)
    {
        Komorki = komorki.ToList();
    }
}

/// <summary>Arkusz eksportu: nazwa + wiersze.</summary>
public class ArkuszEksportu
{
    public string Nazwa { get; set; } = "";
    public List<WierszEksportu> Wiersze { get; set; } = new();
}

/// <summary>
/// Eksport całości do jednego pliku Excel o układzie zbliżonym do arkuszy źródłowych (FR-1106 [M]):
/// arkusze „Broń”, „Wyposażenie”, „Zestawy”, „Wyprawy”, „Zadania”, „Słowniki”.
/// Wartości tekstowe jak w arkuszach; liczby jako tekst w formacie BR-14 (bezpieczne przy dowolnych ustawieniach regionalnych).
/// </summary>
public static class EksporterXlsx
{
    /// <summary>Buduje arkusze eksportu z danych aplikacji.</summary>
    public static List<ArkuszEksportu> BudujArkusze(DaneAplikacji dane)
    {
        var kalk = new Kalkulator(dane);
        return new List<ArkuszEksportu>
        {
            ArkuszBroni(dane, kalk),
            ArkuszWyposazenia(dane, kalk),
            ArkuszZestawow(dane, kalk),
            ArkuszWypraw(dane),
            ArkuszZadan(dane),
            ArkuszSlownikow(dane),
        };
    }

    /// <summary>Zapisuje arkusze do pliku .xlsx (NPOI).</summary>
    public static void Zapisz(List<ArkuszEksportu> arkusze, string sciezka)
    {
        using var strumien = File.Create(sciezka);
        Zapisz(arkusze, strumien);
    }

    /// <summary>Zapisuje arkusze do strumienia (testy, podgląd).</summary>
    public static void Zapisz(List<ArkuszEksportu> arkusze, Stream strumien)
    {
        using var skoroszyt = new XSSFWorkbook();
        foreach (var arkusz in arkusze)
        {
            var nazwa = arkusz.Nazwa.Length > 31 ? arkusz.Nazwa.Substring(0, 31) : arkusz.Nazwa;
            var sheet = skoroszyt.CreateSheet(string.IsNullOrWhiteSpace(nazwa) ? "Arkusz" : nazwa);
            var stylNaglowek = skoroszyt.CreateCellStyle();
            var czcionka = skoroszyt.CreateFont();
            czcionka.IsBold = true;
            stylNaglowek.SetFont(czcionka);
            for (var r = 0; r < arkusz.Wiersze.Count; r++)
            {
                var wiersz = arkusz.Wiersze[r];
                var row = sheet.CreateRow(r);
                for (var c = 0; c < wiersz.Komorki.Count; c++)
                {
                    var komorka = row.CreateCell(c);
                    komorka.SetCellValue(wiersz.Komorki[c] ?? "");
                    if (wiersz.CzyNaglowek)
                        komorka.CellStyle = stylNaglowek;
                }
            }
            for (var c = 0; c < arkusz.Wiersze.Select(w => w.Komorki.Count).DefaultIfEmpty(0).Max(); c++)
                sheet.SetColumnWidth(c, 22 * 256);
        }
        skoroszyt.Write(strumien, leaveOpen: true);
    }

    private static string? Tekst(IEnumerable<string> id, Func<string, string?> nazwa)
    {
        var nazwy = id.Select(nazwa).Where(n => !string.IsNullOrEmpty(n)).ToList();
        return nazwy.Count == 0 ? null : string.Join(", ", nazwy);
    }

    private static ArkuszEksportu ArkuszBroni(DaneAplikacji dane, Kalkulator kalk)
    {
        var arkusz = new ArkuszEksportu { Nazwa = "Broń" };
        arkusz.Wiersze.Add(Naglowek("Miejsce", "Nazwa", "Wyprawy", "Procent", "Typ magazynka", "Rola", "Strona", "Koszt", "Cena oryginalna", "Dodatki", "Koszt na wyprawę"));
        var wiersze = dane.Repliki
            .Select(r => (Replika: r, Wiersz: kalk.WierszRepliki(r)))
            .OrderByDescending(x => x.Wiersz.Wyprawy)
            .ThenBy(x => x.Replika.ModelProducenta, StringComparer.CurrentCulture);
        var miejsce = 0;
        decimal sumaKoszt = 0, sumaOryg = 0, sumaDodatki = 0;
        foreach (var (replika, w) in wiersze)
        {
            miejsce++;
            arkusz.Wiersze.Add(new WierszEksportu(
                miejsce.ToString(CultureInfo.InvariantCulture),
                replika.Prototyp != null ? $"{replika.ModelProducenta} ({replika.Prototyp})" : replika.ModelProducenta,
                w.Wyprawy.ToString(CultureInfo.InvariantCulture),
                Formatowanie.Procent(w.ProcentWypraw),
                Tekst(replika.TypMagazynkaId != null ? new[] { replika.TypMagazynkaId } : Array.Empty<string>(), id => Slowniki.Znajdz(dane.Slowniki.TypyMagazynkow, id)?.Nazwa),
                Tekst(replika.RoleId, id => Slowniki.Znajdz(dane.Slowniki.Role, id)?.Nazwa),
                Tekst(replika.StronyId, id => Slowniki.Znajdz(dane.Slowniki.Strony, id)?.Nazwa),
                Formatowanie.Waluta(w.KosztCalkowity),
                Formatowanie.Waluta(w.CenaOryginalna),
                Formatowanie.Waluta(w.KosztDodatkow),
                Formatowanie.Waluta(w.KosztNaWyprawe)));
            if (replika.Status == StatusWlasnosci.Posiadany)
            {
                sumaKoszt += w.KosztCalkowity;
                sumaOryg += w.CenaOryginalna;
                sumaDodatki += w.KosztDodatkow;
            }
        }
        arkusz.Wiersze.Add(Suma("Suma (posiadane)", "", "", "", "", "",
            Formatowanie.Waluta(sumaKoszt), Formatowanie.Waluta(sumaOryg), Formatowanie.Waluta(sumaDodatki), ""));
        return arkusz;
    }

    private static ArkuszEksportu ArkuszWyposazenia(DaneAplikacji dane, Kalkulator kalk)
    {
        var arkusz = new ArkuszEksportu { Nazwa = "Wyposażenie" };
        arkusz.Wiersze.Add(Naglowek("Kategoria", "Firma", "Nazwa", "Strona", "Wzór", "Miejsce", "Pochodzenie", "Liczba użyć", "Komentarz"));
        foreach (var p in dane.Przedmioty.OrderBy(p => dane.Slowniki.Kategorie.FirstOrDefault(k => k.Id == p.KategoriaId)?.Kolejnosc ?? int.MaxValue).ThenBy(p => p.Nazwa, StringComparer.CurrentCulture))
        {
            arkusz.Wiersze.Add(new WierszEksportu(
                dane.Slowniki.Kategorie.FirstOrDefault(k => k.Id == p.KategoriaId)?.Nazwa,
                p.Firma,
                p.Nazwa,
                Tekst(p.StronyId, id => Slowniki.Znajdz(dane.Slowniki.Strony, id)?.Nazwa),
                p.WzorId != null ? dane.Slowniki.Wzory.FirstOrDefault(w => w.Id == p.WzorId)?.Nazwa : null,
                p.MiejsceId != null ? Slowniki.Znajdz(dane.Slowniki.Miejsca, p.MiejsceId)?.Nazwa : null,
                p.PochodzenieId != null ? Slowniki.Znajdz(dane.Slowniki.Pochodzenie, p.PochodzenieId)?.Nazwa : null,
                kalk.UzyciaPrzedmiotu(p).ToString(CultureInfo.InvariantCulture),
                p.Komentarz));
        }
        return arkusz;
    }

    private static ArkuszEksportu ArkuszZestawow(DaneAplikacji dane, Kalkulator kalk)
    {
        var arkusz = new ArkuszEksportu { Nazwa = "Zestawy" };
        arkusz.Wiersze.Add(Naglowek("Nazwa", "Slot", "Wariant", "Elementy", "Koszt zestawu", "Zakres kosztu"));
        foreach (var z in dane.Zestawy.OrderBy(z => z.Nazwa, StringComparer.CurrentCulture))
        {
            for (var s = 0; s < z.Sloty.Count; s++)
            {
                var slot = z.Sloty[s];
                for (var w = 0; w < slot.Warianty.Count; w++)
                {
                    var wariant = slot.Warianty[w];
                    var elementy = wariant.CzyPuste
                        ? "PUSTE"
                        : string.Join(", ", wariant.Elementy.Select(e => NazwaElementu(dane, e)));
                    arkusz.Wiersze.Add(new WierszEksportu(
                        s == 0 && w == 0 ? z.Nazwa : null,
                        slot.Nazwa,
                        $"Wariant {w + 1}{(slot.WariantDomyslnyId != null && slot.Warianty[w].Id == slot.WariantDomyslnyId ? " (domyślny)" : "")}",
                        elementy,
                        s == 0 && w == 0 ? Formatowanie.Waluta(kalk.KosztZestawu(z)) : null,
                        s == 0 && w == 0 ? ZakresTekst(kalk.ZakresKosztuZestawu(z)) : null));
                }
            }
        }
        return arkusz;
    }

    private static string NazwaElementu(DaneAplikacji dane, ElementWariantu e)
    {
        if (e.NazwaSzkicu != null)
            return e.NazwaSzkicu;
        return e.Typ switch
        {
            TypElementu.Przedmiot => dane.Przedmioty.FirstOrDefault(p => p.Id == e.ElementId)?.Nazwa ?? "?",
            TypElementu.Replika => dane.Repliki.FirstOrDefault(r => r.Id == e.ElementId)?.ModelProducenta ?? "?",
            TypElementu.Komplet => dane.Przedmioty.FirstOrDefault(p => p.Id == e.ElementId)?.Nazwa ?? "?",
            TypElementu.KonfiguracjaNosnika => dane.Przedmioty
                .Where(p => p.Konfiguracje.Count > 0)
                .SelectMany(p => p.Konfiguracje.Select(k => (Nosnik: p, Konfiguracja: k)))
                .FirstOrDefault(x => x.Konfiguracja.Id == e.ElementId) is { } para
                    ? $"{para.Nosnik.Nazwa} — {para.Konfiguracja.Nazwa}"
                    : "?",
            _ => "?",
        };
    }

    private static string ZakresTekst((decimal Minimum, decimal Maksimum) zakres)
        => zakres.Minimum == zakres.Maksimum
            ? Formatowanie.Waluta(zakres.Minimum)
            : $"{Formatowanie.Waluta(zakres.Minimum)} – {Formatowanie.Waluta(zakres.Maksimum)}";

    private static ArkuszEksportu ArkuszWypraw(DaneAplikacji dane)
    {
        var arkusz = new ArkuszEksportu { Nazwa = "Wyprawy" };
        arkusz.Wiersze.Add(Naglowek("Data", "Miejsce", "Replika główna", "Zestaw", "Komentarz"));
        foreach (var w in dane.Wyprawy.Where(w => !w.CzyUsunieta).OrderBy(w => w.Data))
        {
            arkusz.Wiersze.Add(new WierszEksportu(
                w.Data.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                w.Miejsce,
                w.ReplikaGlownaId != null ? dane.Repliki.FirstOrDefault(r => r.Id == w.ReplikaGlownaId)?.ModelProducenta : null,
                w.ZestawId != null ? dane.Zestawy.FirstOrDefault(z => z.Id == w.ZestawId)?.Nazwa : null,
                w.Komentarz));
        }
        return arkusz;
    }

    private static ArkuszEksportu ArkuszZadan(DaneAplikacji dane)
    {
        var arkusz = new ArkuszEksportu { Nazwa = "Zadania" };
        arkusz.Wiersze.Add(Naglowek("Dotyczy", "Rodzaj", "Opis", "Status", "Data utworzenia", "Data zamknięcia"));
        foreach (var z in dane.Zadania.OrderByDescending(z => z.DataUtworzenia))
        {
            arkusz.Wiersze.Add(new WierszEksportu(
                NazwaDotyczy(dane, z),
                z.RodzajId != null ? Slowniki.Znajdz(dane.Slowniki.RodzajeZadan, z.RodzajId)?.Nazwa : null,
                z.Opis,
                z.Status == StatusZadania.Otwarte ? "Otwarte" : "Zamknięte",
                z.DataUtworzenia.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                z.DataZamkniecia?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)));
        }
        return arkusz;
    }

    private static string? NazwaDotyczy(DaneAplikacji dane, Zadanie z)
    {
        if (z.DotyczyId == null)
            return null;
        return z.DotyczyTyp switch
        {
            "Replika" => dane.Repliki.FirstOrDefault(r => r.Id == z.DotyczyId)?.ModelProducenta,
            "Przedmiot" => dane.Przedmioty.FirstOrDefault(p => p.Id == z.DotyczyId)?.Nazwa,
            "Zestaw" => dane.Zestawy.FirstOrDefault(x => x.Id == z.DotyczyId)?.Nazwa,
            _ => z.DotyczyId,
        };
    }

    private static ArkuszEksportu ArkuszSlownikow(DaneAplikacji dane)
    {
        var arkusz = new ArkuszEksportu { Nazwa = "Słowniki" };
        arkusz.Wiersze.Add(Naglowek("Słownik", "Nazwa", "Aliasy"));
        void Dodaj(string nazwaSlownika, IEnumerable<WartoscSlownika> wartosci)
        {
            foreach (var w in wartosci.OrderBy(w => w.Nazwa, StringComparer.CurrentCulture))
                arkusz.Wiersze.Add(new WierszEksportu(nazwaSlownika, w.Nazwa, w.Aliasy.Count > 0 ? string.Join(", ", w.Aliasy) : null));
        }
        Dodaj("Strony", dane.Slowniki.Strony);
        Dodaj("Miejsca", dane.Slowniki.Miejsca);
        Dodaj("Role", dane.Slowniki.Role);
        Dodaj("Typy magazynków", dane.Slowniki.TypyMagazynkow);
        Dodaj("Napędy", dane.Slowniki.Napedy);
        Dodaj("Stany replik", dane.Slowniki.StanyReplik);
        Dodaj("Pochodzenie", dane.Slowniki.Pochodzenie);
        Dodaj("Rodzaje kosztów", dane.Slowniki.RodzajeKosztow);
        Dodaj("Rodzaje zadań", dane.Slowniki.RodzajeZadan);
        foreach (var k in dane.Slowniki.Kategorie.OrderBy(k => k.Kolejnosc))
            arkusz.Wiersze.Add(new WierszEksportu("Kategorie", k.Nazwa, k.CzyNosnik ? "nośnik" : null));
        foreach (var w in dane.Slowniki.Wzory.OrderBy(w => w.Nazwa, StringComparer.CurrentCulture))
            arkusz.Wiersze.Add(new WierszEksportu("Wzory", w.Nazwa, w.Aliasy.Count > 0 ? string.Join(", ", w.Aliasy) : null));
        return arkusz;
    }

    private static WierszEksportu Naglowek(params string?[] komorki)
        => new(komorki) { CzyNaglowek = true };

    private static WierszEksportu Suma(params string?[] komorki)
        => new(komorki) { CzySuma = true };
}
