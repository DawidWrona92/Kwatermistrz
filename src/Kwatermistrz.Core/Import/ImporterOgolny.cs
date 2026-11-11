using System.Globalization;
using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Import;

/// <summary>Pola, do których można zmapować kolumnę przy imporcie ogólnym (FR-1403).</summary>
public enum PoleImportu
{
    /// <summary>Kolumna nieprzypisana (ignorowana przy imporcie).</summary>
    Puste,
    Nazwa,
    Firma,
    Kategoria,
    Strona,
    Wzor,
    Miejsce,
    Pochodzenie,
    LiczbaUzyc,
    Komentarz,
}

/// <summary>Mapowanie kolumn źródła na pola importu + kategoria docelowa (FR-1403). Zapisywalne i wielokrotnego użytku.</summary>
public class MapowanieKolumn
{
    /// <summary>Nazwa mapowania (np. „Mój arkusz 2025”).</summary>
    public string Nazwa { get; set; } = "";
    /// <summary>Indeks kolumny źródłowej (0-based) → pole.</summary>
    public Dictionary<int, PoleImportu> Kolumny { get; set; } = new();
    /// <summary>Id kategorii docelowej; puste = kategoria z kolumny „Kategoria” wiersza.</summary>
    public string? KategoriaId { get; set; }
    /// <summary>Czy importować wiersze, których nazwa jest pusta (same dodatkowe pola).</summary>
    public bool TylkoPelneWiersze { get; set; } = true;

    /// <summary>Kopia mapowania (do edycji w podglądzie).</summary>
    public MapowanieKolumn Kopia() => new()
    {
        Nazwa = Nazwa,
        Kolumny = new Dictionary<int, PoleImportu>(Kolumny),
        KategoriaId = KategoriaId,
        TylkoPelneWiersze = TylkoPelneWiersze,
    };
}

/// <summary>Wiersz podglądu importu ogólnego: proponowany przedmiot + surowe wartości (FR-1102, FR-1403).</summary>
public class WierszPodgladuOgolnego
{
    public int NumerWiersza { get; set; }
    public Przedmiot Przedmiot { get; set; } = new();
    public string KategoriaNazwa { get; set; } = "";
    public string? Ostrzezenie { get; set; }
    public bool Pomin { get; set; }
}

/// <summary>
/// Import ogólny z mapowaniem kolumn: plik Excel/CSV w dowolnym układzie → przedmioty katalogu
/// (FR-1403 [S]). Mapowanie wskazuje, która kolumna to które pole; podgląd i znacznik
/// „z importu, do przeglądu” działają jak przy imporcie z układu autora (FR-1102, FR-1110).
/// </summary>
public static class ImporterOgolny
{
    /// <summary>Czyta plik (xlsx lub csv) do arkuszy komórek-tekstowych.</summary>
    public static List<ArkuszXlsx> WczytajPlik(string sciezka)
    {
        if (sciezka.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return new List<ArkuszXlsx> { CzytnikCsv.Wczytaj(sciezka) };
        return CzytnikXlsx.Wczytaj(sciezka);
    }

    /// <summary>Buduje podgląd importu pierwszego arkusza wg mapowania; nic nie zapisuje (FR-1102).</summary>
    public static List<WierszPodgladuOgolnego> PrzygotujPodglad(
        DaneAplikacji dane, ArkuszXlsx arkusz, MapowanieKolumn mapowanie, int wierszNaglowka = 0)
    {
        var wynik = new List<WierszPodgladuOgolnego>();
        var slowniki = dane.Slowniki;
        var kategorieJakoWartosci = slowniki.Kategorie
            .Select(k => new WartoscSlownika { Id = k.Id, Nazwa = k.Nazwa })
            .ToList();
        var wiersze = arkusz.Wiersze.Skip(wierszNaglowka);
        foreach (var wiersz in wiersze)
        {
            string? Pole(PoleImportu pole)
            {
                // brak mapowania = brak wartości; wcześniej FirstOrDefault().Key dawało 0 i kolumna 0 leciała do każdego niezmapowanego pola
                foreach (var (kol, p) in mapowanie.Kolumny)
                    if (p == pole)
                        return Normalizacja.Przytnij(wiersz.Komorka(kol));
                return null;
            }
            var nazwa = Pole(PoleImportu.Nazwa) ?? Pole(PoleImportu.Firma);
            if (mapowanie.TylkoPelneWiersze && nazwa == null)
                continue;

            var przedmiot = new Przedmiot
            {
                Nazwa = nazwa ?? "",
                Firma = Pole(PoleImportu.Firma),
                Komentarz = Pole(PoleImportu.Komentarz),
                ZnacznikZImportu = true,
            };

            var ostrzezenie = new List<string>();

            var katTekst = Pole(PoleImportu.Kategoria);
            if (mapowanie.KategoriaId != null)
                przedmiot.KategoriaId = mapowanie.KategoriaId;
            else if (katTekst != null)
            {
                var kat = Slowniki.Znajdz(kategorieJakoWartosci, katTekst);
                if (kat != null)
                    przedmiot.KategoriaId = kat.Id;
                else
                {
                    przedmiot.KategoriaId = slowniki.Kategorie.First().Id;
                    ostrzezenie.Add($"Kategoria „{katTekst}” nieznana — trafi do „{slowniki.Kategorie.First().Nazwa}”");
                }
            }
            else
                przedmiot.KategoriaId = slowniki.Kategorie.First().Id;

            var strona = Pole(PoleImportu.Strona);
            if (strona != null)
            {
                var w = Slowniki.Znajdz(slowniki.Strony, strona);
                if (w != null)
                    przedmiot.StronyId.Add(w.Id);
                else
                    ostrzezenie.Add($"Strona „{strona}” nieznana — pominięta");
            }

            var wzor = Pole(PoleImportu.Wzor);
            if (wzor != null)
            {
                var wz = slowniki.ZnajdzWzor(wzor);
                if (wz != null)
                    przedmiot.WzorId = wz.Id;
                else
                    ostrzezenie.Add($"Wzór „{wzor}” nieznany — pominięty");
            }

            var miejsce = Pole(PoleImportu.Miejsce);
            if (miejsce != null)
            {
                var m = Slowniki.Znajdz(slowniki.Miejsca, miejsce);
                if (m != null)
                    przedmiot.MiejsceId = m.Id;
                else
                    ostrzezenie.Add($"Miejsce „{miejsce}” nieznane — pominięte");
            }

            var pochodzenie = Pole(PoleImportu.Pochodzenie);
            if (pochodzenie != null)
            {
                var p = Slowniki.Znajdz(slowniki.Pochodzenie, pochodzenie);
                if (p != null)
                    przedmiot.PochodzenieId = p.Id;
                else
                    ostrzezenie.Add($"Pochodzenie „{pochodzenie}” nieznane — pominięte");
            }

            var uzycia = Pole(PoleImportu.LiczbaUzyc);
            if (uzycia != null)
            {
                if (CzytnikXlsx.LiczbaCalkowita(uzycia) is int liczba && liczba > 0)
                {
                    przedmiot.LicznikHistoryczny = liczba;
                    przedmiot.LicznikHistorycznyDoWeryfikacji = true; // FR-806/FR-1110: do weryfikacji
                }
                else
                    ostrzezenie.Add($"Liczba użyć „{uzycia}” nierozpoznana — pominięta");
            }

            wynik.Add(new WierszPodgladuOgolnego
            {
                NumerWiersza = wiersz.Numer,
                Przedmiot = przedmiot,
                KategoriaNazwa = slowniki.Kategorie.FirstOrDefault(k => k.Id == przedmiot.KategoriaId)?.Nazwa ?? "",
                Ostrzezenie = ostrzezenie.Count > 0 ? string.Join("; ", ostrzezenie) : null,
            });
        }
        return wynik;
    }

    /// <summary>Dodaje zatwierdzone wiersze podglądu do danych (znacznik „z importu” zostaje, FR-1110).</summary>
    public static int Zastosuj(DaneAplikacji dane, IEnumerable<WierszPodgladuOgolnego> wiersze)
    {
        var liczba = 0;
        foreach (var w in wiersze.Where(w => !w.Pomin))
        {
            dane.Przedmioty.Add(w.Przedmiot);
            liczba++;
        }
        return liczba;
    }

    /// <summary>Proponuje mapowanie kolumn po nagłówkach (automatyczna sugestia, użytkownik koryguje).</summary>
    public static MapowanieKolumn SugerujMapowanie(ArkuszXlsx arkusz, int wierszNaglowka = 0)
    {
        var mapowanie = new MapowanieKolumn { Nazwa = arkusz.Nazwa };
        var naglowek = arkusz.Wiersze.FirstOrDefault(w => w.Numer == wierszNaglowka + 1);
        if (naglowek == null)
            return mapowanie;
        for (var i = 0; i < naglowek.Komorki.Count; i++)
        {
            var tekst = naglowek.Komorka(i);
            if (tekst == null)
                continue;
            var z = Normalizacja.TekstDoPorownania(tekst);
            var pole = z switch
            {
                _ when z.Contains("nazwa") || z.Contains("model") || z.Contains("przedmiot") => PoleImportu.Nazwa,
                _ when z.Contains("firma") || z.Contains("producent") || z.Contains("marka") => PoleImportu.Firma,
                _ when z.Contains("kategoria") => PoleImportu.Kategoria,
                _ when z.Contains("strona") => PoleImportu.Strona,
                _ when z.Contains("wzor") || z.Contains("kamuflaz") || z.Contains("kolor") => PoleImportu.Wzor,
                _ when z.Contains("miejsce") || z.Contains("gdzie") || z.Contains("lokalizacja") => PoleImportu.Miejsce,
                _ when z.Contains("pochodz") || z.Contains("klasa") || z.Contains("oryg") => PoleImportu.Pochodzenie,
                _ when z.Contains("uzy") || z.Contains("liczb") => PoleImportu.LiczbaUzyc,
                _ when z.Contains("komentarz") || z.Contains("uwag") || z.Contains("opis") => PoleImportu.Komentarz,
                _ => PoleImportu.Puste,
            };
            if (pole != PoleImportu.Puste)
                mapowanie.Kolumny[i] = pole;
        }
        return mapowanie;
    }
}

/// <summary>Czytnik plików CSV (separator ; lub , wykrywany po pierwszej linii; cudzysłów jak w RFC 4180).</summary>
public static class CzytnikCsv
{
    public static ArkuszXlsx Wczytaj(string sciezka)
    {
        var bajty = File.ReadAllBytes(sciezka);
        var tekst = WykryjKodowanie(bajty);
        // Podział rekordów musi uwzględniać pola wielowierszowe i separatory w cudzysłowach.
        return WczytajZPamieci(ParsujCalkowicie(tekst));
    }

    /// <summary>UTF-8 (z/bez BOM) oraz starsze polskie pliki CP1250.</summary>
    internal static string WykryjKodowanie(byte[] bajty)
    {
        if (bajty.Length >= 3 && bajty[0] == 0xEF && bajty[1] == 0xBB && bajty[2] == 0xBF)
            return System.Text.Encoding.UTF8.GetString(bajty, 3, bajty.Length - 3);
        var jakoUtf8 = System.Text.Encoding.UTF8.GetString(bajty);
        if (!jakoUtf8.Contains('�'))
            return jakoUtf8;
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        return System.Text.Encoding.GetEncoding(1250).GetString(bajty);
    }

    /// <summary>
    /// Dzieli CSV na kompletne rekordy, zachowując oryginalne cudzysłowy.
    /// Inaczej separator w "nazwie; z przecinkiem" po podziale staje się dodatkową kolumną.
    /// </summary>
    internal static List<string> ParsujCalkowicie(string tekst)
    {
        var wynik = new List<string>();
        var rekord = new System.Text.StringBuilder();
        var cytat = false;
        for (var i = 0; i < tekst.Length; i++)
        {
            var znak = tekst[i];
            if (znak == '"')
            {
                rekord.Append('"');
                if (cytat && i + 1 < tekst.Length && tekst[i + 1] == '"')
                {
                    rekord.Append('"');
                    i++;
                }
                else
                    cytat = !cytat;
            }
            else if (!cytat && (znak == '\r' || znak == '\n'))
            {
                if (znak == '\r' && i + 1 < tekst.Length && tekst[i + 1] == '\n')
                    i++;
                wynik.Add(rekord.ToString());
                rekord.Clear();
            }
            else
                rekord.Append(znak);
        }
        if (cytat)
            throw new InvalidDataException("Plik CSV zawiera niedomknięte cudzysłowy.");
        if (rekord.Length > 0)
            wynik.Add(rekord.ToString());
        return wynik;
    }

    public static ArkuszXlsx WczytajZPamieci(IEnumerable<string> linie)
    {
        var rekordy = linie.ToList();
        var pierwszy = rekordy.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r)) ?? "";
        var separator = WykryjSeparator(pierwszy);
        var arkusz = new ArkuszXlsx { Nazwa = "CSV" };
        for (var i = 0; i < rekordy.Count; i++)
            arkusz.Wiersze.Add(new WierszXlsx
            {
                Numer = i + 1,
                Komorki = ParsujLinie(rekordy[i], separator),
            });
        return arkusz;
    }

    private static char WykryjSeparator(string pierwszy)
    {
        var przecinki = 0;
        var sredniki = 0;
        var cytat = false;
        for (var i = 0; i < pierwszy.Length; i++)
        {
            var znak = pierwszy[i];
            if (znak == '"')
            {
                if (cytat && i + 1 < pierwszy.Length && pierwszy[i + 1] == '"')
                    i++;
                else
                    cytat = !cytat;
            }
            else if (!cytat)
            {
                if (znak == ',')
                    przecinki++;
                else if (znak == ';')
                    sredniki++;
            }
        }
        return przecinki > sredniki ? ',' : ';';
    }

    private static List<string?> ParsujLinie(string rekord, char separator)
    {
        var wynik = new List<string?>();
        var pole = new System.Text.StringBuilder();
        var cytat = false;
        for (var i = 0; i < rekord.Length; i++)
        {
            var znak = rekord[i];
            if (znak == '"')
            {
                if (cytat && i + 1 < rekord.Length && rekord[i + 1] == '"')
                {
                    pole.Append('"');
                    i++;
                }
                else
                    cytat = !cytat;
            }
            else if (!cytat && znak == separator)
            {
                wynik.Add(pole.Length == 0 ? null : pole.ToString());
                pole.Clear();
            }
            else
                pole.Append(znak);
        }
        wynik.Add(pole.Length == 0 ? null : pole.ToString());
        return wynik;
    }
}
