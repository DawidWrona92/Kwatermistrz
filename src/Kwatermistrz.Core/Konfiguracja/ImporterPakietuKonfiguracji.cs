using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Konfiguracja;

/// <summary>Decyzja o pojedynczej wartości słownika, wzorze lub kategorii z pakietu (FR-1404, FR-1003).</summary>
public enum AkcjaWartosci
{
    /// <summary>Dodaj jako nową wartość (zachowując Id z pakietu, aliasy i semantykę).</summary>
    DodajNowa,
    /// <summary>Scal z istniejącą wartością: nazwa wartości z pakietu i jej aliasy trafiają do aliasów istniejącej (FR-1003).</summary>
    ScalZIstniejaca,
    /// <summary>Nie importuj tej wartości; odniesienia do niej w pakiecie staną się puste.</summary>
    Pominij,
}

/// <summary>Decyzja o wartości słownika/wzoru/kategorii: para z pakietem i opcjonalnie dopasowaniem lokalnym.</summary>
public class DecyzjaWartosc<T> where T : class
{
    public DecyzjaWartosc(T wartoscPakietu, T? istniejaca)
    {
        WartoscPakietu = wartoscPakietu;
        Istniejaca = istniejaca;
        Akcja = istniejaca != null ? AkcjaWartosci.ScalZIstniejaca : AkcjaWartosci.DodajNowa;
    }

    public T WartoscPakietu { get; }
    /// <summary>Lokalna wartość o tej samej nazwie (znormalizowanej) lub aliasie; null, gdy brak dopasowania.</summary>
    public T? Istniejaca { get; set; }
    public AkcjaWartosci Akcja { get; set; }
    public string NazwaPakietu => WartoscPakietu switch
    {
        WartoscSlownika w => w.Nazwa,
        WzorKamuflazu wz => wz.Nazwa,
        Kategoria k => k.Nazwa,
        _ => "",
    };
}

/// <summary>Decyzja o rekonstrukcji z pakietu (FR-1404).</summary>
public enum AkcjaRekonstrukcji
{
    /// <summary>Dodaj jako nową rekonstrukcję (zachowując Id i szablon slotów; strona mapowana na lokalną).</summary>
    DodajNowa,
    /// <summary>Scal szablon slotów z istniejącą rekonstrukcją o tej samej nazwie: dopisz sloty, których jeszcze nie ma.</summary>
    ScalSzablony,
    /// <summary>Nie importuj.</summary>
    Pominij,
}

public class DecyzjaRekonstrukcji
{
    public DecyzjaRekonstrukcji(RekonstrukcjaPakietu pakiet, Rekonstrukcja? istniejaca)
    {
        Pakiet = pakiet;
        Istniejaca = istniejaca;
        Akcja = istniejaca != null ? AkcjaRekonstrukcji.Pominij : AkcjaRekonstrukcji.DodajNowa;
    }

    public RekonstrukcjaPakietu Pakiet { get; }
    public Rekonstrukcja? Istniejaca { get; set; }
    public AkcjaRekonstrukcji Akcja { get; set; }
    /// <summary>Nazwa strony z pakietu (do wyświetlenia w podglądzie).</summary>
    public string? NazwaStrony { get; set; }
}

/// <summary>Decyzja o szablonie karty technicznej z pakietu (FR-1404).</summary>
public enum AkcjaSzablonuKarty
{
    /// <summary>Dodaj jako nowy szablon (napęd mapowany na lokalny).</summary>
    DodajNowy,
    /// <summary>Nie importuj — szablon o tej samej nazwie już istnieje.</summary>
    Pominij,
}

public class DecyzjaSzablonuKarty
{
    public DecyzjaSzablonuKarty(SzablonKarty pakiet, SzablonKarty? istniejacy)
    {
        Pakiet = pakiet;
        Istniejacy = istniejacy;
        Akcja = istniejacy != null ? AkcjaSzablonuKarty.Pominij : AkcjaSzablonuKarty.DodajNowy;
    }

    public SzablonKarty Pakiet { get; }
    public SzablonKarty? Istniejacy { get; set; }
    public AkcjaSzablonuKarty Akcja { get; set; }
    /// <summary>Nazwa napędu z pakietu (pusta dla szablonu wspólnego; do wyświetlenia).</summary>
    public string NazwaNapedu { get; set; } = "";
}

/// <summary>Podgląd importu pakietu konfiguracji: lista decyzji do zatwierdzenia przed zapisem (FR-1404, FR-1102 w wersji pakietowej).</summary>
public class PodgladPakietuKonfiguracji
{
    public PodgladPakietuKonfiguracji(PakietKonfiguracji pakiet)
    {
        Pakiet = pakiet;
    }

    public PakietKonfiguracji Pakiet { get; }
    public List<(RodzajSlownika Rodzaj, List<DecyzjaWartosc<WartoscSlownika>> Decyzje)> Slowniki { get; } = new();
    public List<DecyzjaWartosc<WzorKamuflazu>> Wzory { get; } = new();
    public List<DecyzjaWartosc<Kategoria>> Kategorie { get; } = new();
    public List<DecyzjaRekonstrukcji> Rekonstrukcje { get; } = new();
    public List<DecyzjaSzablonuKarty> SzablonyKart { get; } = new();

    public int LiczbaWartosciDoDodania =>
        Slowniki.Sum(s => s.Decyzje.Count(d => d.Akcja == AkcjaWartosci.DodajNowa))
        + Wzory.Count(d => d.Akcja == AkcjaWartosci.DodajNowa)
        + Kategorie.Count(d => d.Akcja == AkcjaWartosci.DodajNowa);

    public int LiczbaWartosciDoScalenia =>
        Slowniki.Sum(s => s.Decyzje.Count(d => d.Akcja == AkcjaWartosci.ScalZIstniejaca))
        + Wzory.Count(d => d.Akcja == AkcjaWartosci.ScalZIstniejaca)
        + Kategorie.Count(d => d.Akcja == AkcjaWartosci.ScalZIstniejaca);

    public int LiczbaRekonstrukcjiDoDodania => Rekonstrukcje.Count(d => d.Akcja == AkcjaRekonstrukcji.DodajNowa);
    public int LiczbaSzablonowDoDodania => SzablonyKart.Count(d => d.Akcja == AkcjaSzablonuKarty.DodajNowy);
}

/// <summary>Import pakietu konfiguracji: podgląd z decyzjami (FR-1404) i zastosowanie zatwierdzonych decyzji. Bez przedmiotów, replik, wypraw, zdjęć i cen.</summary>
public static class ImporterPakietuKonfiguracji
{
    /// <summary>Buduje podgląd: dla każdej wartości, wzoru, kategorii, rekonstrukcji i szablonu karty proponuje domyślną decyzję (FR-1102, FR-1003).</summary>
    public static PodgladPakietuKonfiguracji PrzygotujPodglad(DaneAplikacji dane, PakietKonfiguracji pakiet)
    {
        var podglad = new PodgladPakietuKonfiguracji(pakiet);
        var nazwyStron = pakiet.Slowniki
            .Where(w => w.Slownik == "Strony")
            .ToDictionary(w => w.Wartosc.Id, w => w.Wartosc.Nazwa);
        var nazwyNapedow = pakiet.Slowniki
            .Where(w => w.Slownik == "Napedy")
            .ToDictionary(w => w.Wartosc.Id, w => w.Wartosc.Nazwa);

        var grupowane = pakiet.Slowniki.GroupBy(w => w.Slownik);
        foreach (var rodzaj in RodzajeSlownikow.Wszystkie)
        {
            var lokalne = rodzaj.Pobierz(dane.Slowniki);
            var zPakietu = grupowane.FirstOrDefault(g => g.Key == rodzaj.Klucz)?.ToList() ?? new List<WartoscSlownikaPakietu>();
            var decyzje = zPakietu
                .Select(w => new DecyzjaWartosc<WartoscSlownika>(w.Wartosc, Slowniki.Znajdz(lokalne, w.Wartosc.Nazwa)))
                .ToList();
            podglad.Slowniki.Add((rodzaj, decyzje));
        }

        podglad.Wzory.AddRange(pakiet.Wzory.Select(w =>
            new DecyzjaWartosc<WzorKamuflazu>(w, dane.Slowniki.ZnajdzWzor(w.Nazwa))));

        podglad.Kategorie.AddRange(pakiet.Kategorie.Select(k =>
            new DecyzjaWartosc<Kategoria>(k, ZnajdzKategorie(dane.Slowniki.Kategorie, k.Nazwa))));

        foreach (var r in pakiet.Rekonstrukcje)
        {
            var istniejaca = dane.Rekonstrukcje.FirstOrDefault(x =>
                Normalizacja.TekstDoPorownania(x.Nazwa) == Normalizacja.TekstDoPorownania(r.Nazwa));
            var decyzja = new DecyzjaRekonstrukcji(r, istniejaca)
            {
                NazwaStrony = r.StronaId != null && nazwyStron.TryGetValue(r.StronaId, out var ns) ? ns : null,
            };
            podglad.Rekonstrukcje.Add(decyzja);
        }

        foreach (var s in pakiet.SzablonyKart)
        {
            var istniejacy = dane.SzablonyKart.FirstOrDefault(x =>
                Normalizacja.TekstDoPorownania(x.Nazwa) == Normalizacja.TekstDoPorownania(s.Nazwa));
            var decyzja = new DecyzjaSzablonuKarty(s, istniejacy)
            {
                NazwaNapedu = s.NapedId != null && nazwyNapedow.TryGetValue(s.NapedId, out var nn) ? nn : "",
            };
            podglad.SzablonyKart.Add(decyzja);
        }

        return podglad;
    }

    /// <summary>Stosuje zatwierdzone decyzje do danych: słowniki (z aliasami przy scalaniu, FR-1003), wzory, kategorie, rekonstrukcje z szablonami slotów i szablony kart. Mapuje Id wartości z pakietu na lokalne. Zmiany trafiają do danych; zapis wykonuje wywołujący.</summary>
    public static void Zastosuj(DaneAplikacji dane, PodgladPakietuKonfiguracji podglad)
    {
        var mapySlownikow = new Dictionary<string, Dictionary<string, string>>();

        foreach (var (rodzaj, decyzje) in podglad.Slowniki)
        {
            var lokalne = rodzaj.Pobierz(dane.Slowniki);
            var mapa = new Dictionary<string, string>();
            foreach (var d in decyzje)
            {
                var idPakietu = d.WartoscPakietu.Id;
                switch (d.Akcja)
                {
                    case AkcjaWartosci.DodajNowa:
                        var kopia = new WartoscSlownika
                        {
                            Id = idPakietu,
                            Nazwa = d.WartoscPakietu.Nazwa,
                            Aliasy = new List<string>(d.WartoscPakietu.Aliasy),
                            Semantyka = d.WartoscPakietu.Semantyka,
                        };
                        lokalne.Add(kopia);
                        mapa[idPakietu] = kopia.Id;
                        break;
                    case AkcjaWartosci.ScalZIstniejaca when d.Istniejaca != null:
                        DodajAliasy(d.Istniejaca, d.WartoscPakietu.Nazwa, d.WartoscPakietu.Aliasy);
                        if (d.Istniejaca.Semantyka.Length == 0)
                            d.Istniejaca.Semantyka = d.WartoscPakietu.Semantyka;
                        mapa[idPakietu] = d.Istniejaca.Id;
                        break;
                    case AkcjaWartosci.ScalZIstniejaca:
                        // użytkownik wybrał scalanie, ale nie wskazał istniejącej — traktuj jak dodanie nowej
                        lokalne.Add(new WartoscSlownika
                        {
                            Id = idPakietu,
                            Nazwa = d.WartoscPakietu.Nazwa,
                            Aliasy = new List<string>(d.WartoscPakietu.Aliasy),
                            Semantyka = d.WartoscPakietu.Semantyka,
                        });
                        mapa[idPakietu] = idPakietu;
                        break;
                }
            }
            mapySlownikow[rodzaj.Klucz] = mapa;
        }

        var mapaWzorow = new Dictionary<string, string>();
        foreach (var d in podglad.Wzory)
        {
            switch (d.Akcja)
            {
                case AkcjaWartosci.DodajNowa:
                    dane.Slowniki.Wzory.Add(new WzorKamuflazu
                    {
                        Id = d.WartoscPakietu.Id,
                        Nazwa = d.WartoscPakietu.Nazwa,
                        Aliasy = new List<string>(d.WartoscPakietu.Aliasy),
                        KoloryProbki = new List<string>(d.WartoscPakietu.KoloryProbki),
                        CzyKolorJednolity = d.WartoscPakietu.CzyKolorJednolity,
                    });
                    mapaWzorow[d.WartoscPakietu.Id] = d.WartoscPakietu.Id;
                    break;
                case AkcjaWartosci.ScalZIstniejaca when d.Istniejaca != null:
                    DodajAliasy(d.Istniejaca, d.WartoscPakietu.Nazwa, d.WartoscPakietu.Aliasy);
                    mapaWzorow[d.WartoscPakietu.Id] = d.Istniejaca.Id;
                    break;
            }
        }

        var mapaKategorii = new Dictionary<string, string>();
        foreach (var d in podglad.Kategorie)
        {
            switch (d.Akcja)
            {
                case AkcjaWartosci.DodajNowa:
                    dane.Slowniki.Kategorie.Add(new Kategoria
                    {
                        Id = d.WartoscPakietu.Id,
                        Nazwa = d.WartoscPakietu.Nazwa,
                        CzyNosnik = d.WartoscPakietu.CzyNosnik,
                        Kolejnosc = d.WartoscPakietu.Kolejnosc,
                    });
                    mapaKategorii[d.WartoscPakietu.Id] = d.WartoscPakietu.Id;
                    break;
                case AkcjaWartosci.ScalZIstniejaca when d.Istniejaca != null:
                    mapaKategorii[d.WartoscPakietu.Id] = d.Istniejaca.Id;
                    break;
            }
        }

        foreach (var d in podglad.Rekonstrukcje)
        {
            switch (d.Akcja)
            {
                case AkcjaRekonstrukcji.DodajNowa:
                    dane.Rekonstrukcje.Add(new Rekonstrukcja
                    {
                        Id = d.Pakiet.Id,
                        Nazwa = d.Pakiet.Nazwa,
                        Okres = d.Pakiet.Okres,
                        StronaId = Mapuj(mapySlownikow, "Strony", d.Pakiet.StronaId),
                        Opis = d.Pakiet.Opis,
                        SzablonSlotow = d.Pakiet.SzablonSlotow.Select(s => new SlotSzablonu
                        {
                            Id = s.Id,
                            Nazwa = s.Nazwa,
                            Kolejnosc = s.Kolejnosc,
                            CzySlotMunduru = s.CzySlotMunduru,
                            CzySlotBroniGlownej = s.CzySlotBroniGlownej,
                        }).ToList(),
                    });
                    break;
                case AkcjaRekonstrukcji.ScalSzablony when d.Istniejaca != null:
                    var nazwyIstniejace = d.Istniejaca.SzablonSlotow
                        .Select(s => Normalizacja.TekstDoPorownania(s.Nazwa))
                        .ToHashSet();
                    var kolejnosc = d.Istniejaca.SzablonSlotow.Count > 0
                        ? d.Istniejaca.SzablonSlotow.Max(s => s.Kolejnosc) + 1
                        : 0;
                    foreach (var s in d.Pakiet.SzablonSlotow.OrderBy(x => x.Kolejnosc))
                    {
                        if (nazwyIstniejace.Add(Normalizacja.TekstDoPorownania(s.Nazwa)))
                        {
                            d.Istniejaca.SzablonSlotow.Add(new SlotSzablonu
                            {
                                Nazwa = s.Nazwa,
                                Kolejnosc = kolejnosc++,
                                CzySlotMunduru = s.CzySlotMunduru,
                                CzySlotBroniGlownej = s.CzySlotBroniGlownej,
                            });
                        }
                    }
                    break;
            }
        }

        foreach (var d in podglad.SzablonyKart)
        {
            if (d.Akcja != AkcjaSzablonuKarty.DodajNowy)
                continue;
            dane.SzablonyKart.Add(new SzablonKarty
            {
                Id = d.Pakiet.Id,
                Nazwa = d.Pakiet.Nazwa,
                NapedId = Mapuj(mapySlownikow, "Napedy", d.Pakiet.NapedId),
                Parametry = d.Pakiet.Parametry.Select(p => new ParametrKarty
                {
                    Id = p.Id,
                    Nazwa = p.Nazwa,
                    Kolejnosc = p.Kolejnosc,
                    Ukryty = p.Ukryty,
                }).ToList(),
            });
        }
    }

    private static string? Mapuj(Dictionary<string, Dictionary<string, string>> mapy, string slownik, string? idPakietu)
    {
        if (idPakietu == null)
            return null;
        return mapy.TryGetValue(slownik, out var mapa) && mapa.TryGetValue(idPakietu, out var lokalny)
            ? lokalny
            : null;
    }

    private static void DodajAliasy(WartoscSlownika istniejaca, string nazwaPakietu, List<string> aliasyPakietu)
    {
        DodajAlias(istniejaca, nazwaPakietu);
        foreach (var alias in aliasyPakietu)
            DodajAlias(istniejaca, alias);
    }

    private static void DodajAliasy(WzorKamuflazu istniejacy, string nazwaPakietu, List<string> aliasyPakietu)
    {
        DodajAlias(istniejacy, nazwaPakietu);
        foreach (var alias in aliasyPakietu)
            DodajAlias(istniejacy, alias);
    }

    private static void DodajAlias(WartoscSlownika istniejaca, string alias)
    {
        if (Normalizacja.TekstDoPorownania(alias) == Normalizacja.TekstDoPorownania(istniejaca.Nazwa))
            return;
        if (!istniejaca.Aliasy.Any(a => Normalizacja.TekstDoPorownania(a) == Normalizacja.TekstDoPorownania(alias)))
            istniejaca.Aliasy.Add(alias);
    }

    private static void DodajAlias(WzorKamuflazu istniejacy, string alias)
    {
        if (Normalizacja.TekstDoPorownania(alias) == Normalizacja.TekstDoPorownania(istniejacy.Nazwa))
            return;
        if (!istniejacy.Aliasy.Any(a => Normalizacja.TekstDoPorownania(a) == Normalizacja.TekstDoPorownania(alias)))
            istniejacy.Aliasy.Add(alias);
    }

    private static Kategoria? ZnajdzKategorie(List<Kategoria> kategorie, string nazwa)
        => kategorie.FirstOrDefault(k => Normalizacja.TekstDoPorownania(k.Nazwa) == Normalizacja.TekstDoPorownania(nazwa));
}
