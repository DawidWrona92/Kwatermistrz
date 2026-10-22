using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Konfiguracja;

/// <summary>
/// Scalanie konfiguracji szablonu (FR-1406) z istniej\u0105cymi s\u0142ownikami:
/// warto\u015bci i wzory dopasowywane po nazwie/aliasie (FR-1003), brakuj\u0105ce dopisywane,
/// istniej\u0105ce nigdy nie nadpisywane. Zwraca mapy Id szablonu \u2192 Id lokalne.
/// </summary>
public static class MergerSlownikow
{
    /// <summary>Wynik scalenia: mapy Id (klucz rodzaju \u2192 mapa Id; klucze "Kategorie"/"Wzory" bezpo\u015brednio) oraz liczba dopisanych warto\u015bci.</summary>
    public class WynikScalenia
    {
        public Dictionary<string, Dictionary<string, string>> Mapy { get; } = new(StringComparer.Ordinal);
        public int Dopisane { get; internal set; }
        /// <summary>Mapuje Id warto\u015bci s\u0142ownika z szablonu na lokalne; nieznane zwraca bez zmiany.</summary>
        public string? MapujSlownik(string rodzaj, string? id)
            => id != null && Mapy.TryGetValue(rodzaj, out var mapa) && mapa.TryGetValue(id, out var lokalny) ? lokalny : id;
        /// <summary>Mapuje Id kategorii; nieznane zwraca bez zmiany.</summary>
        public string? MapujKategoria(string? id)
            => id != null && Mapy.TryGetValue("Kategorie", out var mapa) && mapa.TryGetValue(id, out var lokalny) ? lokalny : id;
        /// <summary>Mapuje Id wzoru; nieznane zwraca bez zmiany.</summary>
        public string? MapujWzor(string? id)
            => id != null && Mapy.TryGetValue("Wzory", out var mapa) && mapa.TryGetValue(id, out var lokalny) ? lokalny : id;
    }

    /// <summary>Scala s\u0142owniki szablonu z lokalnymi: dopisuje brakuj\u0105ce, istniej\u0105ce scala aliasami, zwraca mapy Id.</summary>
    public static WynikScalenia ScalSlowniki(Slowniki lokalne, Slowniki szablon)
    {
        var wynik = new WynikScalenia();
        wynik.Dopisane += ScalWartosci(lokalne.Strony, szablon.Strony, wynik.Mapy, "Strony");
        wynik.Dopisane += ScalWartosci(lokalne.Miejsca, szablon.Miejsca, wynik.Mapy, "Miejsca");
        wynik.Dopisane += ScalWartosci(lokalne.Role, szablon.Role, wynik.Mapy, "Role");
        wynik.Dopisane += ScalWartosci(lokalne.TypyMagazynkow, szablon.TypyMagazynkow, wynik.Mapy, "TypyMagazynkow");
        wynik.Dopisane += ScalWartosci(lokalne.Napedy, szablon.Napedy, wynik.Mapy, "Napedy");
        wynik.Dopisane += ScalWartosci(lokalne.StanyReplik, szablon.StanyReplik, wynik.Mapy, "StanyReplik");
        wynik.Dopisane += ScalWartosci(lokalne.Pochodzenie, szablon.Pochodzenie, wynik.Mapy, "Pochodzenie");
        wynik.Dopisane += ScalWartosci(lokalne.RodzajeKosztow, szablon.RodzajeKosztow, wynik.Mapy, "RodzajeKosztow");
        wynik.Dopisane += ScalWartosci(lokalne.RodzajeZadan, szablon.RodzajeZadan, wynik.Mapy, "RodzajeZadan");

        // wzory
        var mapaWzorow = new Dictionary<string, string>(StringComparer.Ordinal);
        wynik.Mapy["Wzory"] = mapaWzorow;
        foreach (var wzor in szablon.Wzory)
        {
            var istniejacy = lokalne.ZnajdzWzor(wzor.Nazwa);
            if (istniejacy != null)
            {
                DodajAlias(istniejacy.Aliasy, istniejacy.Nazwa, new[] { wzor.Nazwa }.Concat(wzor.Aliasy));
                mapaWzorow[wzor.Id] = istniejacy.Id;
                continue;
            }
            lokalne.Wzory.Add(new WzorKamuflazu
            {
                Id = wzor.Id,
                Nazwa = wzor.Nazwa,
                Aliasy = new List<string>(wzor.Aliasy),
                KoloryProbki = new List<string>(wzor.KoloryProbki),
                CzyKolorJednolity = wzor.CzyKolorJednolity,
            });
            mapaWzorow[wzor.Id] = wzor.Id;
            wynik.Dopisane++;
        }

        // kategorie
        var mapaKategorii = new Dictionary<string, string>(StringComparer.Ordinal);
        wynik.Mapy["Kategorie"] = mapaKategorii;
        foreach (var kategoria in szablon.Kategorie)
        {
            var istniejaca = lokalne.Kategorie.FirstOrDefault(k =>
                Normalizacja.TekstDoPorownania(k.Nazwa) == Normalizacja.TekstDoPorownania(kategoria.Nazwa));
            if (istniejaca != null)
            {
                // oznaczenie „mundur” z szablonu przechodzi na istniejącą kategorię (definicja BR-09)
                if (kategoria.CzyMundur)
                    istniejaca.CzyMundur = true;
                mapaKategorii[kategoria.Id] = istniejaca.Id;
                continue;
            }
            lokalne.Kategorie.Add(new Kategoria
            {
                Id = kategoria.Id,
                Nazwa = kategoria.Nazwa,
                CzyNosnik = kategoria.CzyNosnik,
                CzyMundur = kategoria.CzyMundur,
                Kolejnosc = kategoria.Kolejnosc,
            });
            mapaKategorii[kategoria.Id] = kategoria.Id;
            wynik.Dopisane++;
        }

        return wynik;
    }

    private static int ScalWartosci(List<WartoscSlownika> lokalne, List<WartoscSlownika> zSzablonu,
        Dictionary<string, Dictionary<string, string>> mapy, string klucz)
    {
        var mapa = new Dictionary<string, string>(StringComparer.Ordinal);
        mapy[klucz] = mapa;
        var dopisane = 0;
        foreach (var wartosc in zSzablonu)
        {
            var istniejaca = Slowniki.Znajdz(lokalne, wartosc.Nazwa);
            if (istniejaca != null)
            {
                DodajAlias(istniejaca.Aliasy, istniejaca.Nazwa, new[] { wartosc.Nazwa }.Concat(wartosc.Aliasy));
                if (istniejaca.Semantyka.Length == 0)
                    istniejaca.Semantyka = wartosc.Semantyka;
                mapa[wartosc.Id] = istniejaca.Id;
                continue;
            }
            lokalne.Add(new WartoscSlownika
            {
                Id = wartosc.Id,
                Nazwa = wartosc.Nazwa,
                Aliasy = new List<string>(wartosc.Aliasy),
                Semantyka = wartosc.Semantyka,
            });
            mapa[wartosc.Id] = wartosc.Id;
            dopisane++;
        }
        return dopisane;
    }

    private static void DodajAlias(List<string> aliasy, string nazwaIstniejacej, IEnumerable<string> nowe)
    {
        foreach (var alias in nowe)
        {
            if (Normalizacja.TekstDoPorownania(alias) == Normalizacja.TekstDoPorownania(nazwaIstniejacej))
                continue;
            if (!aliasy.Any(a => Normalizacja.TekstDoPorownania(a) == Normalizacja.TekstDoPorownania(alias)))
                aliasy.Add(alias);
        }
    }
}
