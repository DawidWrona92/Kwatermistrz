using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwatermistrz.App.Services;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Obliczenia;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.App.ViewModels;

/// <summary>ViewModel ekranu „Broń” (FR-301–305, FR-401–409, FR-501–505, FR-601–602): lista replik, karta z zakładkami, koszty, zadania.</summary>
public partial class ReplikiViewModel : ObservableObject
{
    private readonly DaneAplikacji _dane;
    private readonly MagazynJson _magazyn;
    private readonly KalkulatorSerwis _kalkulator;
    private readonly StanAplikacji _stan;
    private readonly SerwisZdjec _serwisZdjec;

    /// <summary>Serwis zdjęć (FR-211) — dla galerii na karcie.</summary>
    public SerwisZdjec SerwisZdjec => _serwisZdjec;

    public ReplikiViewModel(DaneAplikacji dane, MagazynJson magazyn, KalkulatorSerwis kalkulator, StanAplikacji stan, SerwisZdjec serwisZdjec)
    {
        _dane = dane;
        _magazyn = magazyn;
        _kalkulator = kalkulator;
        _stan = stan;
        _serwisZdjec = serwisZdjec;
    }

    /// <summary>Zapisuje dane bez odświeżania listy — po zmianie samych zdjęć (FR-211, FR-1203).</summary>
    public void ZapiszCicho() => _magazyn.Zapisz(_dane);

    /// <summary>Wiersze listy replik po filtrach (FR-301).</summary>
    public ObservableCollection<WierszRepliki> Wiersze { get; } = new();

    /// <summary>Wybrana replika (karta, FR-302).</summary>
    [ObservableProperty]
    private WierszRepliki? _wybrana;

    /// <summary>Aktywna zakładka karty (FR-302): Przegląd, Karta, Pomiary, Modyfikacje, Koszty, Zadania, Wyprawy.</summary>
    [ObservableProperty]
    private string _zakladka = "Przegląd";

    /// <summary>Filtr tekstowy.</summary>
    [ObservableProperty]
    private string _filtrTekst = "";

    /// <summary>Pokaż sprzedane/wycofane (FR-305; domyślnie ukryte).</summary>
    [ObservableProperty]
    private bool _pokazSprzedane;

    /// <summary>Wybrany filtr strony (Id); null = wszystkie.</summary>
    [ObservableProperty]
    private string? _filtrStronaId;

    /// <summary>Wybrany filtr stanu (Id); null = wszystkie.</summary>
    [ObservableProperty]
    private string? _filtrStanId;

    /// <summary>Tylko repliki z otwartą awarią (FR-301, FR-602).</summary>
    [ObservableProperty]
    private bool _tylkoAwaria;

    /// <summary>Tylko repliki bez testu dłużej niż próg z ustawień (FR-104).</summary>
    [ObservableProperty]
    private bool _tylkoBezTestu;

    /// <summary>Słowniki do filtrów i karty.</summary>
    public Slowniki Slowniki => _dane.Slowniki;

    /// <summary>Rekonstrukcje (FR-1302).</summary>
    public IReadOnlyList<Rekonstrukcja> Rekonstrukcje => _dane.Rekonstrukcje;

    /// <summary>Szablony kart (FR-403, FR-1307).</summary>
    public IReadOnlyList<SzablonKarty> SzablonyKart => _dane.SzablonyKart;

    /// <summary>Liczba wierszy.</summary>
    public int LiczbaWierszy => Wiersze.Count;

    /// <summary>Odświeża listę replik (FR-301: ranking wg wypraw, filtry, aktywna rekonstrukcja FR-1303).</summary>
    public void Odswiez()
    {
        // indeks obliczeniowy budowany raz na odświeżenie — O(R·W) zamiast O(R²·W) przy NFR-04
        var indeks = new IndeksObliczeniowy(_dane);
        var zapytanie = _dane.Repliki.AsEnumerable();
        if (!PokazSprzedane)
            zapytanie = zapytanie.Where(r => r.Status != StatusWlasnosci.Sprzedany && r.Status != StatusWlasnosci.Wycofany);
        if (_stan.AktywnaRekonstrukcjaId != null)
            zapytanie = zapytanie.Where(r => r.RekonstrukcjeId.Count == 0 || r.RekonstrukcjeId.Contains(_stan.AktywnaRekonstrukcjaId));
        if (FiltrStronaId != null)
            zapytanie = zapytanie.Where(r => r.StronyId.Contains(FiltrStronaId));
        if (FiltrStanId != null)
            zapytanie = zapytanie.Where(r => r.StanId == FiltrStanId);
        var tekst = Normalizacja.Przytnij(FiltrTekst);
        if (tekst != null)
        {
            var szukane = Normalizacja.TekstDoPorownania(tekst);
            zapytanie = zapytanie.Where(r =>
                Normalizacja.TekstDoPorownania(r.ModelProducenta).Contains(szukane) ||
                (r.Prototyp != null && Normalizacja.TekstDoPorownania(r.Prototyp).Contains(szukane)) ||
                (r.Komentarz != null && Normalizacja.TekstDoPorownania(r.Komentarz).Contains(szukane)));
        }

        var lista = zapytanie.ToList();
        var wiersze = lista
            .Select(r => BudujWiersz(indeks, r))
            .OrderByDescending(w => w.Wyprawy)
            .ThenBy(w => w.NazwaPelna, StringComparer.CurrentCulture)
            .ToList();
        if (TylkoAwaria)
            wiersze = wiersze.Where(w => w.CzyOtwartaAwaria).ToList();
        if (TylkoBezTestu)
        {
            var prog = DateOnly.FromDateTime(DateTime.Today).AddDays(-_dane.Ustawienia.DniBezTestu);
            wiersze = wiersze.Where(w => w.DataOstatniegoTestu == null || w.DataOstatniegoTestu < prog).ToList();
        }
        for (var i = 0; i < wiersze.Count; i++)
            wiersze[i].MiejsceWRankingu = i + 1;

        Wiersze.Clear();
        foreach (var w in wiersze)
            Wiersze.Add(w);
        OnPropertyChanged(nameof(LiczbaWierszy));
    }

    partial void OnFiltrTekstChanged(string value) => Odswiez();
    partial void OnPokazSprzedaneChanged(bool value) => Odswiez();
    partial void OnFiltrStronaIdChanged(string? value) => Odswiez();
    partial void OnFiltrStanIdChanged(string? value) => Odswiez();
    partial void OnTylkoAwariaChanged(bool value) => Odswiez();
    partial void OnTylkoBezTestuChanged(bool value) => Odswiez();

    private WierszRepliki BudujWiersz(IndeksObliczeniowy indeks, Replika r)
    {
        var wyprawy = indeks.WyprawyRepliki(r);
        var (calkowity, oryginalna, dodatki) = Kalkulator.Koszty(r.Koszty);
        var otwarteZadania = _dane.Zadania
            .Where(z => z.DotyczyId == r.Id && z.Status == StatusZadania.Otwarte)
            .ToList();
        var czyAwaria = otwarteZadania.Any(z =>
            z.Semantyka == SemantykaZadania.Awaria ||
            (z.Semantyka == SemantykaZadania.DoZrobienia &&
             _dane.Slowniki.RodzajeZadan.FirstOrDefault(rz => rz.Id == z.RodzajId)?.Semantyka == nameof(SemantykaZadania.Awaria)));
        var ostatnieWyprawy = _dane.Wyprawy
            .Where(w => !w.CzyUsunieta && w.ReplikaGlownaId == r.Id)
            .OrderByDescending(w => w.Data)
            .Take(3)
            .ToList();
        return new WierszRepliki(r, new WierszReplikiObliczenia
        {
            ReplikaId = r.Id,
            Status = r.Status,
            Wyprawy = wyprawy,
            ProcentWypraw = indeks.ProcentWypraw(wyprawy),
            KosztCalkowity = calkowity,
            CenaOryginalna = oryginalna,
            KosztDodatkow = dodatki,
            KosztNaWyprawe = Kalkulator.KosztNaWyprawe(calkowity, wyprawy),
            DataOstatniegoTestu = r.Pomiary.Count > 0 ? r.Pomiary.Max(p => p.Data) : null,
            CzyOtwartaAwaria = czyAwaria,
            OtwarteZadania = otwarteZadania.Count,
            OstatnieWyprawy = ostatnieWyprawy,
        });
    }

    /// <summary>Wybiera replikę — otwiera kartę (FR-302).</summary>
    [RelayCommand]
    private void Wybierz(WierszRepliki? wiersz)
    {
        Wybrana = wiersz;
        Zakladka = "Przegląd";
        if (wiersz != null)
        {
            wiersz.Replika.ZnacznikZImportu = false;
            _magazyn.Zapisz(_dane);
        }
    }

    /// <summary>Dodaje nową replikę (FR-303).</summary>
    [RelayCommand]
    private void Dodaj()
    {
        var replika = new Replika();
        _dane.Repliki.Add(replika);
        Odswiez();
        Wybrana = Wiersze.FirstOrDefault(w => w.Id == replika.Id);
        Zakladka = "Przegląd";
    }

    /// <summary>FR-407: „Kopiuj kartę z innej repliki” — kopiuje wartości karty technicznej (bez pomiarów, modyfikacji, kosztów i zadań) jako punkt wyjścia dla nowej repliki.</summary>
    [RelayCommand]
    private void KopiujKarteZInnej(WierszRepliki? zrodlo)
    {
        if (zrodlo == null)
            return;
        var replika = new Replika();
        var z = zrodlo.Replika;
        foreach (var (parametrId, wartosc) in z.KartaTechniczna)
            replika.KartaTechniczna[parametrId] = new WartoscPolaKarty { Tekst = wartosc.Tekst, Stan = wartosc.Stan };
        if (z.SzablonKartyId != null)
            replika.SzablonKartyId = z.SzablonKartyId;
        replika.NapedId = z.NapedId;
        replika.TypMagazynkaId = z.TypMagazynkaId;
        _dane.Repliki.Add(replika);
        _magazyn.Zapisz(_dane);
        Odswiez();
        Wybrana = Wiersze.FirstOrDefault(w => w.Id == replika.Id);
        Zakladka = "Karta";
    }

    /// <summary>FR-406: wiersze porównania kart — zaznaczone repliki + parametry ich kart.</summary>
    public IReadOnlyList<WierszPorownaniaKart> PorownanieKart => BudujPorownanie();

    /// <summary>FR-406: lista replik zaznaczonych do porównania (maks. 4).</summary>
    public IReadOnlyList<WierszRepliki> ZaznaczoneDoPorownania =>
        Wiersze.Where(w => w.ZaznaczonaDoPorownania).Take(4).ToList();

    /// <summary>Czy pokazuje panel porównania (FR-406).</summary>
    [ObservableProperty]
    private bool _pokazPorownanie;

    /// <summary>FR-406: przełącza panel porównania kart.</summary>
    [RelayCommand]
    private void PrzelaczPorownanie() => PokazPorownanie = !PokazPorownanie;

    partial void OnPokazPorownanieChanged(bool value) => OnPropertyChanged(nameof(PorownanieKart));

    private List<WierszPorownaniaKart> BudujPorownanie()
    {
        var wynik = new List<WierszPorownaniaKart>();
        var wybrane = ZaznaczoneDoPorownania;
        if (wybrane.Count < 2)
            return wynik;
        // parametry: unia nazw z szablonów, zachowując kolejność pierwszego szablonu
        var parametry = new List<ParametrKarty>();
        var widziane = new HashSet<string>();
        foreach (var w in wybrane)
        {
            var szablon = SzablonDla(w.Replika);
            if (szablon == null)
                continue;
            foreach (var p in szablon.Parametry.OrderBy(x => x.Kolejnosc))
                if (widziane.Add(p.Id))
                    parametry.Add(p);
        }
        foreach (var p in parametry)
        {
            var wartosci = wybrane.Select(w =>
            {
                w.Replika.KartaTechniczna.TryGetValue(p.Id, out var wartosc);
                return wartosc?.Tekst;
            }).ToList();
            wynik.Add(new WierszPorownaniaKart
            {
                Parametr = p.Nazwa,
                Wartosci = wartosci,
                CzyRoznica = wartosci.Distinct().Count() > 1,
            });
        }
        return wynik;
    }

    private SzablonKarty? SzablonDla(Replika r)
    {
        if (r.SzablonKartyId != null)
            return _dane.SzablonyKart.FirstOrDefault(s => s.Id == r.SzablonKartyId);
        if (r.NapedId != null)
            return _dane.SzablonyKart.FirstOrDefault(s => s.NapedId == r.NapedId)
                ?? _dane.SzablonyKart.FirstOrDefault(s => s.NapedId == null);
        return _dane.SzablonyKart.FirstOrDefault(s => s.NapedId == null);
    }

    /// <summary>Zapisuje kartę repliki po zatwierdzeniu (FR-1203).</summary>
    [RelayCommand]
    private void ZapiszKarte()
    {
        if (Wybrana == null)
            return;
        Wybrana.Replika.ZnacznikZImportu = false;
        _magazyn.Zapisz(_dane);
        Odswiez();
    }

    /// <summary>Usuwa replikę (FR-1202).</summary>
    [RelayCommand]
    private void Usun(WierszRepliki? wiersz)
    {
        if (wiersz == null)
            return;
        _dane.Repliki.Remove(wiersz.Replika);
        _magazyn.Zapisz(_dane);
        Odswiez();
    }

    /// <summary>Dodaje pozycję kosztową do wybranej repliki (FR-501).</summary>
    [RelayCommand]
    private void DodajKoszt()
    {
        if (Wybrana == null)
            return;
        var rodzajKosztu = _dane.Slowniki.RodzajeKosztow.FirstOrDefault();
        Wybrana.Replika.Koszty.Add(new PozycjaKosztowa
        {
            Data = DateOnly.FromDateTime(DateTime.Today),
            RodzajId = rodzajKosztu?.Id ?? "",
            // semantyka z wybranego rodzaju (BR-04: cena oryginalna liczy się tylko z Zakupu)
            Semantyka = Enum.TryParse<SemantykaKosztu>(rodzajKosztu?.Semantyka, out var sem) ? sem : SemantykaKosztu.Inne,
        });
        _magazyn.Zapisz(_dane);
        Odswiez();
    }

    /// <summary>Usuwa pozycję kosztową (FR-501).</summary>
    [RelayCommand]
    private void UsunKoszt(PozycjaKosztowa? koszt)
    {
        if (Wybrana == null || koszt == null)
            return;
        Wybrana.Replika.Koszty.Remove(koszt);
        _magazyn.Zapisz(_dane);
        Odswiez();
    }

    /// <summary>Dodaje pomiar/test (FR-408).</summary>
    [RelayCommand]
    private void DodajPomiar()
    {
        if (Wybrana == null)
            return;
        Wybrana.Replika.Pomiary.Add(new Pomiar { Data = DateOnly.FromDateTime(DateTime.Today) });
        _magazyn.Zapisz(_dane);
        Odswiez();
    }

    /// <summary>Usuwa pomiar (FR-408).</summary>
    [RelayCommand]
    private void UsunPomiar(Pomiar? pomiar)
    {
        if (Wybrana == null || pomiar == null)
            return;
        Wybrana.Replika.Pomiary.Remove(pomiar);
        _magazyn.Zapisz(_dane);
        Odswiez();
    }

    /// <summary>Dodaje modyfikację (FR-504).</summary>
    [RelayCommand]
    private void DodajModyfikacje()
    {
        if (Wybrana == null)
            return;
        Wybrana.Replika.Modyfikacje.Add(new Modyfikacja { Data = DateOnly.FromDateTime(DateTime.Today) });
        _magazyn.Zapisz(_dane);
        Odswiez();
    }

    /// <summary>Usuwa modyfikację (FR-504/505).</summary>
    [RelayCommand]
    private void UsunModyfikacje(Modyfikacja? modyfikacja)
    {
        if (Wybrana == null || modyfikacja == null)
            return;
        Wybrana.Replika.Modyfikacje.Remove(modyfikacja);
        _magazyn.Zapisz(_dane);
        Odswiez();
    }

    /// <summary>Dodaje zadanie do wybranej repliki (FR-601).</summary>
    [RelayCommand]
    private void DodajZadanie()
    {
        if (Wybrana == null)
            return;
        var rodzajZadania = _dane.Slowniki.RodzajeZadan.FirstOrDefault();
        _dane.Zadania.Add(new Zadanie
        {
            DotyczyTyp = "Replika",
            DotyczyId = Wybrana.Id,
            RodzajId = rodzajZadania?.Id ?? "",
            // semantyka z wybranego rodzaju (raporty liczą awarie po Semantyce)
            Semantyka = Enum.TryParse<SemantykaZadania>(rodzajZadania?.Semantyka, out var semZ) ? semZ : SemantykaZadania.DoZrobienia,
            DataUtworzenia = DateOnly.FromDateTime(DateTime.Today),
        });
        _magazyn.Zapisz(_dane);
        Odswiez();
    }

    /// <summary>Zamyka zadanie (FR-601).</summary>
    [RelayCommand]
    private void ZamknijZadanie(Zadanie? zadanie)
    {
        if (zadanie == null)
            return;
        zadanie.Status = StatusZadania.Zamkniete;
        zadanie.DataZamkniecia = DateOnly.FromDateTime(DateTime.Today);
        _magazyn.Zapisz(_dane);
        Odswiez();
    }

    /// <summary>Usuwa zadanie (FR-601).</summary>
    [RelayCommand]
    private void UsunZadanie(Zadanie? zadanie)
    {
        if (zadanie == null)
            return;
        _dane.Zadania.Remove(zadanie);
        _magazyn.Zapisz(_dane);
        Odswiez();
    }

    /// <summary>Zadania dotyczące wybranej repliki (FR-602).</summary>
    public IReadOnlyList<Zadanie> ZadaniaWybranej =>
        Wybrana == null
            ? new List<Zadanie>()
            : _dane.Zadania.Where(z => z.DotyczyId == Wybrana.Id).OrderByDescending(z => z.DataUtworzenia).ToList();

    /// <summary>Wszystkie otwarte zadania (FR-602).</summary>
    public IReadOnlyList<Zadanie> OtwarteZadaniaWszystkie =>
        _dane.Zadania.Where(z => z.Status == StatusZadania.Otwarte).OrderByDescending(z => z.DataUtworzenia).ToList();

    /// <summary>Wyprawy wybranej repliki (BR-02).</summary>
    public IReadOnlyList<Wyprawa> WyprawyWybranej =>
        Wybrana == null
            ? new List<Wyprawa>()
            : _dane.Wyprawy.Where(w => !w.CzyUsunieta && w.ReplikaGlownaId == Wybrana.Id).OrderByDescending(w => w.Data).ToList();

    /// <summary>Szablon karty przypisany do wybranej repliki (FR-403, FR-1307).</summary>
    public SzablonKarty? SzablonWybranej
    {
        get
        {
            if (Wybrana == null)
                return null;
            var r = Wybrana.Replika;
            if (r.SzablonKartyId != null)
                return _dane.SzablonyKart.FirstOrDefault(s => s.Id == r.SzablonKartyId);
            if (r.NapedId != null)
                return _dane.SzablonyKart.FirstOrDefault(s => s.NapedId == r.NapedId)
                    ?? _dane.SzablonyKart.FirstOrDefault(s => s.NapedId == null);
            return _dane.SzablonyKart.FirstOrDefault(s => s.NapedId == null);
        }
    }

    /// <summary>Parametry karty wybranej repliki z wartościami (FR-401, FR-404).</summary>
    public IReadOnlyList<WierszParametruKarty> ParametryKartyWybranej
    {
        get
        {
            var szablon = SzablonWybranej;
            if (Wybrana == null || szablon == null)
                return new List<WierszParametruKarty>();
            return szablon.Parametry
                .OrderBy(p => p.Kolejnosc)
                .Select(p => new WierszParametruKarty(p, Wybrana.Replika, PodpowiedziParametru(p.Id)))
                .ToList();
        }
    }

    /// <summary>Wartości podpowiadane dla parametru (FR-405): wcześniejsze wartości tego parametru u innych replik.</summary>
    public IReadOnlyList<string> PodpowiedziParametru(string parametrId)
    {
        if (Wybrana == null)
            return new List<string>();
        return _dane.Repliki
            .Where(r => r.Id != Wybrana.Id)
            .Select(r => r.KartaTechniczna.TryGetValue(parametrId, out var w) ? w.Tekst : null)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .OrderBy(t => t)
            .Cast<string>()
            .ToList();
    }
}

/// <summary>Wiersz parametru karty technicznej z trzema stanami (FR-404).</summary>
public class WierszParametruKarty
{
    public WierszParametruKarty(ParametrKarty parametr, Replika replika, IReadOnlyList<string>? podpowiedzi = null)
    {
        Parametr = parametr;
        replika.KartaTechniczna.TryGetValue(parametr.Id, out var wartosc);
        Wartosc = wartosc ?? new WartoscPolaKarty();
        Podpowiedzi = podpowiedzi ?? new List<string>();
    }

    public ParametrKarty Parametr { get; }
    public WartoscPolaKarty Wartosc { get; }
    public string Nazwa => Parametr.Nazwa;
    public bool Ukryty => Parametr.Ukryty;
    /// <summary>Podpowiedzi wartości dla tego parametra (FR-405) — wcześniejsze wartości u innych replik.</summary>
    public IReadOnlyList<string> Podpowiedzi { get; }
}

/// <summary>Wiersz porównania kart (FR-406): jeden parametr, wartości kolejnych replik, czy są różnice.</summary>
public class WierszPorownaniaKart
{
    public string Parametr { get; set; } = "";
    public List<string?> Wartosci { get; set; } = new();
    /// <summary>Prawda, gdy wartości się różnią — wiersz podświetlony.</summary>
    public bool CzyRoznica { get; set; }
}
