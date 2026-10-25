using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwatermistrz.App.Services;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.App.ViewModels;

/// <summary>ViewModel ekranu wypraw (FR-801–809): lista pogrupowana wg roku, dodawanie wyprawy, dzień zerowy (FR-807).</summary>
public partial class WyprawyViewModel : ObservableObject
{
    private readonly DaneAplikacji _dane;
    private readonly MagazynJson _magazyn;
    private readonly KalkulatorSerwis _kalkulator;
    private readonly StanAplikacji _stan;

    public WyprawyViewModel(DaneAplikacji dane, MagazynJson magazyn, KalkulatorSerwis kalkulator, StanAplikacji stan)
    {
        _dane = dane;
        _magazyn = magazyn;
        _kalkulator = kalkulator;
        _stan = stan;
    }

    /// <summary>Wiersze listy wypraw (FR-808).</summary>
    public ObservableCollection<WierszWyprawy> Wiersze { get; } = new();

    /// <summary>Wybrana wyprawa (edycja).</summary>
    [ObservableProperty]
    private WierszWyprawy? _wybrana;

    /// <summary>Nowa wyprawa — formularz szybkiego dodawania (FR-802).</summary>
    [ObservableProperty]
    private Wyprawa _nowa = new() { Data = DateOnly.FromDateTime(DateTime.Today) };

    /// <summary>Filtr roku; null = wszystkie.</summary>
    [ObservableProperty]
    private int? _filtrRok;

    /// <summary>Filtr repliki (Id); null = wszystkie.</summary>
    [ObservableProperty]
    private string? _filtrReplikaId;

    /// <summary>Filtr zestawu (Id); null = wszystkie.</summary>
    [ObservableProperty]
    private string? _filtrZestawId;

    /// <summary>Filtr tekstowy (miejsce/komentarz).</summary>
    [ObservableProperty]
    private string _filtrTekst = "";

    /// <summary>Słowniki.</summary>
    public Slowniki Slowniki => _dane.Slowniki;

    /// <summary>Rekonstrukcje.</summary>
    public IReadOnlyList<Rekonstrukcja> Rekonstrukcje => _dane.Rekonstrukcje;

    /// <summary>Zestawy do wyboru (FR-801).</summary>
    public IReadOnlyList<Zestaw> Zestawy => _dane.Zestawy.OrderBy(z => z.Nazwa).ToList();

    /// <summary>Repliki do wyboru jako główna (FR-805 — bez sprzedanych/wycofanych).</summary>
    public IReadOnlyList<Replika> Repliki => _dane.Repliki
        .Where(r => r.Status == StatusWlasnosci.Posiadany || r.Status == StatusWlasnosci.DoSprzedazy)
        .OrderBy(r => r.ModelProducenta)
        .ToList();

    /// <summary>Lata dostępne w filtrze (historyczne + zapisane wyprawy).</summary>
    public IReadOnlyList<int> Lata => _dane.Ustawienia.HistoryczneWyprawyWgRok.Keys
        .Concat(_dane.Wyprawy.Where(w => !w.CzyUsunieta).Select(w => w.Data.Year))
        .Distinct()
        .OrderByDescending(y => y)
        .ToList();

    /// <summary>Odświeża listę (FR-808: grupowanie wg roku, filtry, FR-1303, FR-1304).</summary>
    public void Odswiez()
    {
        var kalk = _kalkulator.Kalkulator();
        var zapytanie = _dane.Wyprawy.Where(w => !w.CzyUsunieta).AsEnumerable();
        if (_stan.AktywnaRekonstrukcjaId != null)
            zapytanie = zapytanie.Where(w => w.RekonstrukcjaId == null || w.RekonstrukcjaId == _stan.AktywnaRekonstrukcjaId);
        if (FiltrRok != null)
            zapytanie = zapytanie.Where(w => w.Data.Year == FiltrRok);
        if (FiltrReplikaId != null)
            zapytanie = zapytanie.Where(w => w.ReplikaGlownaId == FiltrReplikaId);
        if (FiltrZestawId != null)
            zapytanie = zapytanie.Where(w => w.ZestawId == FiltrZestawId);
        var tekst = Normalizacja.Przytnij(FiltrTekst);
        if (tekst != null)
        {
            var szukane = Normalizacja.TekstDoPorownania(tekst);
            zapytanie = zapytanie.Where(w =>
                (w.Miejsce != null && Normalizacja.TekstDoPorownania(w.Miejsce).Contains(szukane)) ||
                (w.Komentarz != null && Normalizacja.TekstDoPorownania(w.Komentarz).Contains(szukane)));
        }
        Wiersze.Clear();
        foreach (var w in zapytanie.OrderByDescending(w => w.Data))
        {
            var replika = w.ReplikaGlownaId != null
                ? _dane.Repliki.FirstOrDefault(r => r.Id == w.ReplikaGlownaId)?.ModelProducenta
                : null;
            var zestaw = w.ZestawId != null
                ? _dane.Zestawy.FirstOrDefault(z => z.Id == w.ZestawId)?.Nazwa
                : null;
            var (mundurId, _) = kalk.MundurWyprawy(w);
            var mundur = mundurId != null
                ? _dane.Przedmioty.FirstOrDefault(p => p.Id == mundurId)?.Nazwa
                : null;
            Wiersze.Add(new WierszWyprawy(w, replika, zestaw, mundur));
        }
        OnPropertyChanged(nameof(Lata));
    }

    partial void OnFiltrRokChanged(int? value) => Odswiez();
    partial void OnFiltrReplikaIdChanged(string? value) => Odswiez();
    partial void OnFiltrZestawIdChanged(string? value) => Odswiez();
    partial void OnFiltrTekstChanged(string value) => Odswiez();

    /// <summary>Wybiera wyprawę do edycji (FR-808) — zapamiętuje wpływ na liczniki historyczne
    /// sprzed edycji (do cofnięcia korekty FR-807 przy zapisie).</summary>
    [RelayCommand]
    private void Wybierz(WierszWyprawy? wiersz)
    {
        Wybrana = wiersz;
        if (wiersz != null)
            _wplywSprzedEdycji[wiersz.Wyprawa.Id] = _kalkulator.Kalkulator().WplywNaLicznikiHistoryczne(wiersz.Wyprawa);
    }

    /// <summary>Ustawia zestaw nowej wyprawy i warianty domyślne/zapamiętane (FR-803, FR-704).</summary>
    public void UstawZestaw(Zestaw? zestaw)
    {
        Nowa.ZestawId = zestaw?.Id;
        Nowa.WybraneWarianty.Clear();
        if (zestaw != null)
        {
            Nowa.RekonstrukcjaId ??= zestaw.RekonstrukcjaId;
            foreach (var slot in zestaw.Sloty)
            {
                // FR-704: wariant ostatnio użyty na wyprawie (po Id), potem domyślny (po Id), potem pierwszy
                var wariantId = zestaw.WariantDomyslnyNaSlot(slot.Id) ?? slot.WariantDomyslnyId;
                var wariant = slot.Warianty.FirstOrDefault(w => w.Id == wariantId) ?? slot.Warianty.FirstOrDefault();
                if (wariant != null)
                    Nowa.WybraneWarianty[slot.Id] = wariant.Id;
            }
            // podpowiedź repliki głównej: pierwszy wariant slotu broni głównej (FR-802)
            var rek = _dane.Rekonstrukcje.FirstOrDefault(r => r.Id == (zestaw.RekonstrukcjaId ?? _stan.AktywnaRekonstrukcjaId));
            var slotyBroni = rek?.SzablonSlotow.Where(s => s.CzySlotBroniGlownej).Select(s => s.Id).ToHashSet(StringComparer.Ordinal) ?? new HashSet<string>(StringComparer.Ordinal);
            foreach (var slot in zestaw.Sloty.Where(s => s.SzablonSlotId != null && slotyBroni.Contains(s.SzablonSlotId)))
            {
                if (!Nowa.WybraneWarianty.TryGetValue(slot.Id, out var wariantId))
                    continue;
                var wariant = slot.Warianty.FirstOrDefault(w => w.Id == wariantId);
                if (wariant == null)
                    continue;
                var pierwszy = wariant.Elementy.FirstOrDefault(e => e.Typ == TypElementu.Replika);
                if (pierwszy != null && Nowa.ReplikaGlownaId == null)
                    Nowa.ReplikaGlownaId = pierwszy.ElementId;
            }
        }
        OnPropertyChanged(nameof(Nowa));
    }

    /// <summary>FR-807: kandydaci do odjęcia od liczników historycznych przy wyprawie z datą ≤ dzień zerowy (do monitu).</summary>
    public IReadOnlyList<(string Rodzaj, string Id, string Opis)> KandydaciOdejmowania =>
        _kalkulator.Kalkulator().WplywNaLicznikiHistoryczne(Nowa);

    /// <summary>Czy nowa wyprawa ma datę ≤ dzień zerowy (FR-807 — aplikacja pyta o odjęcie od liczników).</summary>
    public bool CzyPrzedDniemZerowym
    {
        get
        {
            var dzienZerowy = _dane.Ustawienia.DzienZerowy;
            return dzienZerowy != null && Nowa.Data != default && Nowa.Data <= dzienZerowy.Value;
        }
    }

    /// <summary>FR-807: odpowiedź na monit o odjęcie od liczników historycznych; domyślnie „tak”.</summary>
    [ObservableProperty]
    private bool _odejmijOdLicznikow = true;

    partial void OnNowaChanged(Wyprawa value)
    {
        OnPropertyChanged(nameof(CzyPrzedDniemZerowym));
        OnPropertyChanged(nameof(KandydaciOdejmowania));
        // po dodaniu wyprawy formularz wraca z tym samym zestawem — wybierz warianty od razu,
        // żeby kolejna wyprawa nie została z pustym składem (FR-704)
        if (value.ZestawId != null)
            UstawZestaw(_dane.Zestawy.FirstOrDefault(z => z.Id == value.ZestawId));
    }

    /// <summary>FR-807: korekta licznika historycznego jest odwracalna — usunięcie/przywrócenie
    /// lub edycja daty wyprawy sprzed dnia zerowego cofa/powtarza odjęcie (delta +1/-1).</summary>
    private void ZmienLicznikHistoryczny(string rodzaj, string id, int delta)
    {
        switch (rodzaj)
        {
            case "Replika":
                var replika = _dane.Repliki.FirstOrDefault(r => r.Id == id);
                if (replika != null && replika.LicznikHistorycznyWypraw + delta >= 0)
                    replika.LicznikHistorycznyWypraw += delta;
                break;
            case "Mundur":
                var mundur = _dane.Przedmioty.FirstOrDefault(p => p.Id == id);
                if (mundur != null && mundur.LicznikHistoryczny + delta >= 0)
                    mundur.LicznikHistoryczny += delta;
                break;
            case "Rok":
                if (int.TryParse(id, out var rok) && _dane.Ustawienia.HistoryczneWyprawyWgRok.TryGetValue(rok, out var liczba) && liczba + delta >= 0)
                    _dane.Ustawienia.HistoryczneWyprawyWgRok[rok] = liczba + delta;
                break;
        }
    }

    /// <summary>FR-807: odlicza wyprawę od liczników historycznych i zaznacza korektę jako aktywną (flaga w danych).</summary>
    private void OdliczOdLicznikow(Wyprawa wyprawa)
    {
        foreach (var (rodzaj, id, _) in _kalkulator.Kalkulator().WplywNaLicznikiHistoryczne(wyprawa))
            ZmienLicznikHistoryczny(rodzaj, id, -1);
        wyprawa.OdliczonaOdLicznikow = true;
    }

    /// <summary>FR-807: cofa korektę liczników historycznych (usunięcie/przywrócenie/edycja wyprawy).
    /// <paramref name="wplyw"/> to wpływ sprzed zmiany — przy edycji bierące pola mogą być już zmienione przez binding.</summary>
    private void CofnijOdliczenie(Wyprawa wyprawa, IReadOnlyList<(string Rodzaj, string Id, string Opis)>? wplyw = null)
    {
        if (!wyprawa.OdliczonaOdLicznikow)
            return;
        wyprawa.OdliczonaOdLicznikow = false;
        foreach (var (rodzaj, id, _) in wplyw ?? _kalkulator.Kalkulator().WplywNaLicznikiHistoryczne(wyprawa))
            ZmienLicznikHistoryczny(rodzaj, id, +1);
    }

    /// <summary>Migawka wpływu na liczniki historyczne przy otwarciu edycji wyprawy (FR-807).</summary>
    private readonly Dictionary<string, List<(string Rodzaj, string Id, string Opis)>> _wplywSprzedEdycji = new(StringComparer.Ordinal);

    /// <summary>Szybkie dodanie wyprawy (FR-802): Enter, domyślna data dziś, zestaw ostatni (FR-803).</summary>
    [RelayCommand]
    private void DodajWyprawe()
    {
        if (Nowa.Data == default)
            Nowa.Data = DateOnly.FromDateTime(DateTime.Today);
        if (Nowa.RekonstrukcjaId == null)
            Nowa.RekonstrukcjaId = _stan.AktywnaRekonstrukcjaId;
        // FR-807: wyprawa z datą ≤ dzień zerowy — użytkownik decyduje o odjęciu od liczników historycznych
        if (CzyPrzedDniemZerowym && KandydaciOdejmowania.Count > 0)
        {
            var tresc = string.Join("\n", KandydaciOdejmowania.Select(k => "• " + k.Opis));
            var wynik = System.Windows.MessageBox.Show(
                $"Ta wyprawa ma datę sprzed dnia zerowego ({_dane.Ustawienia.DzienZerowy:dd.MM.yyyy}).\n" +
                "Liczy się jako uzupełnienie historii — żeby nie liczyć jej podwójnie, aplikacja może odjąć ją od liczników historycznych:\n" +
                tresc + "\n\nOdjąć od liczników historycznych?",
                "Dzień zerowy (FR-807)",
                System.Windows.MessageBoxButton.YesNoCancel,
                System.Windows.MessageBoxImage.Question);
            if (wynik == System.Windows.MessageBoxResult.Cancel)
                return;
            OdejmijOdLicznikow = wynik == System.Windows.MessageBoxResult.Yes;
            if (OdejmijOdLicznikow)
                OdliczOdLicznikow(Nowa);
            OdejmijOdLicznikow = true;
        }
        _dane.Wyprawy.Add(Nowa);
        _magazyn.ZapiszDebounce(_dane);
        // zapamiętaj warianty jako ostatnio użyte (FR-704) — po Id wariantu
        if (Nowa.ZestawId != null)
        {
            var zestaw = _dane.Zestawy.FirstOrDefault(z => z.Id == Nowa.ZestawId);
            if (zestaw != null)
                foreach (var (slotId, wariantId) in Nowa.WybraneWarianty)
                    zestaw.OstatnieWarianty[slotId] = wariantId;
        }
        Nowa = new Wyprawa { Data = DateOnly.FromDateTime(DateTime.Today), ZestawId = Nowa.ZestawId };
        OnPropertyChanged(nameof(Nowa));
        Odswiez();
    }

    /// <summary>Zapisuje edycję wybranej wyprawy (FR-808, FR-809). Przy zmianie daty/repliki/zestawu
    /// korekta liczników historycznych (FR-807) jest przeliczana: stary wpływ cofany, nowy stosowany.</summary>
    [RelayCommand]
    private void ZapiszWybrana()
    {
        if (Wybrana == null)
            return;
        var wyprawa = Wybrana.Wyprawa;
        // korekta sprzed edycji jest cofana po migawce z momentu otwarcia edycji (pola mogą być już zmienione)
        _wplywSprzedEdycji.TryGetValue(wyprawa.Id, out var staryWplyw);
        CofnijOdliczenie(wyprawa, staryWplyw);
        if (CzyPrzedDniemZerowymDla(wyprawa))
            OdliczOdLicznikow(wyprawa);
        _magazyn.ZapiszDebounce(_dane);
        Odswiez();
    }

    private bool CzyPrzedDniemZerowymDla(Wyprawa wyprawa)
    {
        var dzienZerowy = _dane.Ustawienia.DzienZerowy;
        return dzienZerowy != null && wyprawa.Data != default && wyprawa.Data <= dzienZerowy.Value
            && !wyprawa.CzyUsunieta;
    }

    /// <summary>Usuwa wyprawę (oznacza jako usuniętą; FR-808/809 — liczniki aktualizują się od razu).
    /// Korekta liczników historycznych (FR-807) jest cofana — przywrócenie wyprawy ją powtórzy.</summary>
    [RelayCommand]
    private void Usun(WierszWyprawy? wiersz)
    {
        if (wiersz == null)
            return;
        wiersz.Wyprawa.CzyUsunieta = true;
        CofnijOdliczenie(wiersz.Wyprawa);
        _magazyn.ZapiszDebounce(_dane);
        Odswiez();
    }

    /// <summary>Przywraca usuniętą wyprawę (cofnięcie, FR-808) — powtarza korektę liczników historycznych.</summary>
    [RelayCommand]
    private void Przywroc(WierszWyprawy? wiersz)
    {
        if (wiersz == null || !wiersz.Wyprawa.CzyUsunieta)
            return;
        wiersz.Wyprawa.CzyUsunieta = false;
        if (CzyPrzedDniemZerowymDla(wiersz.Wyprawa))
            OdliczOdLicznikow(wiersz.Wyprawa);
        _magazyn.ZapiszDebounce(_dane);
        Odswiez();
    }

    /// <summary>Nazwa repliki po Id.</summary>
    public string NazwaRepliki(string? id) =>
        id == null ? "" : _dane.Repliki.FirstOrDefault(r => r.Id == id)?.ModelProducenta ?? "";

    /// <summary>Nazwa zestawu po Id.</summary>
    public string NazwaZestawu(string? id) =>
        id == null ? "" : _dane.Zestawy.FirstOrDefault(z => z.Id == id)?.Nazwa ?? "";

    /// <summary>Nazwa rekonstrukcji po Id.</summary>
    public string NazwaRekonstrukcji(string? id) =>
        id == null ? "" : _dane.Rekonstrukcje.FirstOrDefault(r => r.Id == id)?.Nazwa ?? "";

    /// <summary>Nazwa przedmiotu po Id.</summary>
    public string NazwaPrzedmiotu(string? id) =>
        id == null ? "" : _dane.Przedmioty.FirstOrDefault(p => p.Id == id)?.Nazwa ?? "";

    /// <summary>Opis elementu wariantu (FR-703).</summary>
    public string NazwaElementu(ElementWariantu element) => element.Typ switch
    {
        TypElementu.Przedmiot => NazwaPrzedmiotu(element.ElementId),
        TypElementu.Replika => NazwaRepliki(element.ElementId),
        TypElementu.Komplet => NazwaPrzedmiotu(element.ElementId),
        _ => "?",
    };

    /// <summary>Sloty zestawu nowej wyprawy (do przełączników wariantów, FR-802).</summary>
    public IReadOnlyList<SlotZestawu> SlotyNowejWyprawy =>
        Nowa.ZestawId != null
            ? _dane.Zestawy.FirstOrDefault(z => z.Id == Nowa.ZestawId)?.Sloty.OrderBy(s => s.Kolejnosc).ToList() ?? new List<SlotZestawu>()
            : new List<SlotZestawu>();
}
