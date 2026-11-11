using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwatermistrz.App.Services;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Obliczenia;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.App.ViewModels;

/// <summary>ViewModel ekranu wypraw (FR-801–809): lista pogrupowana wg roku, dodawanie wyprawy, dzień zerowy (FR-807).</summary>
public partial class WyprawyViewModel : ObservableObject
{
    private readonly DaneAplikacji _dane;
    private readonly MagazynJson _magazyn;
    private readonly KalkulatorSerwis _kalkulator;
    private readonly StanAplikacji _stan;
    private readonly OperacjeNaWyprawach _operacje;

    public WyprawyViewModel(DaneAplikacji dane, MagazynJson magazyn, KalkulatorSerwis kalkulator, StanAplikacji stan)
    {
        _dane = dane;
        _magazyn = magazyn;
        _kalkulator = kalkulator;
        _stan = stan;
        _operacje = new OperacjeNaWyprawach(dane);
    }

    /// <summary>Wiersze listy wypraw (FR-808).</summary>
    public ObservableCollection<WierszWyprawy> Wiersze { get; } = new();

    /// <summary>Wybrana wyprawa (edycja).</summary>
    [ObservableProperty]
    private WierszWyprawy? _wybrana;

    /// <summary>Oddzielna kopia edycji: anulowanie nie zmienia danych na dysku.</summary>
    [ObservableProperty]
    private Wyprawa? _edytowana;

    [ObservableProperty]
    private bool _pokazUsuniete;

    [ObservableProperty]
    private string? _zestawEdycjiId;

    public ObservableCollection<WyborWariantuWyprawy> WariantyEdycji { get; } = new();
    public ObservableCollection<WyborWariantuWyprawy> WariantyNowej { get; } = new();

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

    /// <summary>W edycji pokazujemy też sprzedane/wycofane repliki, by nie gubić relacji historycznych.</summary>
    public IReadOnlyList<Replika> ReplikiHistoryczne => _dane.Repliki
        .OrderBy(r => r.ModelProducenta).ToList();

    /// <summary>Lata dostępne w filtrze (historyczne + zapisane wyprawy).</summary>
    public IReadOnlyList<int> Lata => _dane.Ustawienia.HistoryczneWyprawyWgRok.Keys
        .Concat(_dane.Wyprawy.Select(w => w.Data.Year))
        .Distinct()
        .OrderByDescending(y => y)
        .ToList();

    /// <summary>Odświeża listę (FR-808: grupowanie wg roku, filtry, FR-1303, FR-1304).</summary>
    public void Odswiez()
    {
        var kalk = _kalkulator.Kalkulator();
        var zapytanie = _dane.Wyprawy.Where(w => w.CzyUsunieta == PokazUsuniete).AsEnumerable();
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
        var wybranaId = Wybrana?.Id;
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
        Wybrana = Wiersze.FirstOrDefault(w => w.Id == wybranaId);
        OnPropertyChanged(nameof(Lata));
    }

    partial void OnFiltrRokChanged(int? value) => Odswiez();
    partial void OnFiltrReplikaIdChanged(string? value) => Odswiez();
    partial void OnFiltrZestawIdChanged(string? value) => Odswiez();
    partial void OnFiltrTekstChanged(string value) => Odswiez();
    partial void OnPokazUsunieteChanged(bool value) => Odswiez();

    /// <summary>Odświeżona lista może zawierać nowy wiersz tej samej wyprawy.
    /// Nie wyrzucaj wtedy szkicu edycji (dopóki zmienia się tylko widok).</summary>
    partial void OnWybranaChanged(WierszWyprawy? oldValue, WierszWyprawy? newValue)
    {
        if (oldValue?.Id == newValue?.Id
            && ReferenceEquals(oldValue?.Wyprawa, newValue?.Wyprawa))
            return;
        Edytowana = newValue != null && !newValue.CzyUsunieta
            ? OperacjeNaWyprawach.Kopia(newValue.Wyprawa) : null;
        ZestawEdycjiId = Edytowana?.ZestawId;
        PrzygotujWariantyEdycji();
    }

    /// <summary>Po imporcie lub przywróceniu zawartości bazy odrzuca stary szkic.</summary>
    public void PrzeladujPoWymianieDanych()
    {
        Odswiez();
        AnulujEdycje();
        PrzygotujWariantyNowej();
    }

    partial void OnZestawEdycjiIdChanged(string? value)
    {
        if (Edytowana == null || Edytowana.ZestawId == value)
            return;
        Edytowana.ZestawId = value;
        Edytowana.WybraneWarianty.Clear();
        var zestaw = _dane.Zestawy.FirstOrDefault(z => z.Id == value);
        if (zestaw != null)
        {
            foreach (var slot in zestaw.Sloty)
            {
                var wariant = slot.Warianty.FirstOrDefault(w => w.Id == slot.WariantDomyslnyId)
                    ?? slot.Warianty.FirstOrDefault();
                if (wariant != null)
                    Edytowana.WybraneWarianty[slot.Id] = wariant.Id;
            }
        }
        PrzygotujWariantyEdycji();
    }

    private void PrzygotujWariantyNowej()
    {
        WariantyNowej.Clear();
        var nowa = Nowa;
        var zestaw = _dane.Zestawy.FirstOrDefault(z => z.Id == nowa.ZestawId);
        if (zestaw == null)
            return;
        foreach (var slot in zestaw.Sloty.OrderBy(s => s.Kolejnosc))
        {
            var opcje = slot.Warianty.Select(w => new OpcjaWariantuWyprawy(
                w.Id, w.CzyPuste ? "Puste" : w.Elementy.Count == 0 ? "(brak elementów)" :
                    string.Join(" + ", w.Elementy.Select(NazwaElementu)))).ToList();
            nowa.WybraneWarianty.TryGetValue(slot.Id, out var wybrany);
            var wybor = new WyborWariantuWyprawy(slot.Id, slot.Nazwa, opcje, wybrany);
            wybor.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(WyborWariantuWyprawy.WybranyWariantId)
                    || !ReferenceEquals(Nowa, nowa) || nowa.ZestawId != zestaw.Id)
                    return;
                if (wybor.WybranyWariantId is { } wariantId)
                    nowa.WybraneWarianty[wybor.SlotId] = wariantId;
                else
                    nowa.WybraneWarianty.Remove(wybor.SlotId);
            };
            WariantyNowej.Add(wybor);
        }
    }

    private void PrzygotujWariantyEdycji()
    {
        WariantyEdycji.Clear();
        if (Edytowana?.ZestawId == null)
            return;
        var zestaw = _dane.Zestawy.FirstOrDefault(z => z.Id == Edytowana.ZestawId);
        if (zestaw == null)
            return;
        foreach (var slot in zestaw.Sloty.OrderBy(s => s.Kolejnosc))
        {
            var opcje = slot.Warianty.Select(w => new OpcjaWariantuWyprawy(
                w.Id, w.CzyPuste ? "Puste" : w.Elementy.Count == 0 ? "(brak elementów)" :
                    string.Join(" + ", w.Elementy.Select(NazwaElementu)))).ToList();
            var wybrany = Edytowana.WybraneWarianty.TryGetValue(slot.Id, out var id)
                && slot.Warianty.Any(w => w.Id == id)
                ? id : slot.Warianty.FirstOrDefault(w => w.Id == slot.WariantDomyslnyId)?.Id
                    ?? slot.Warianty.FirstOrDefault()?.Id;
            var szkic = Edytowana;
            var wybor = new WyborWariantuWyprawy(slot.Id, slot.Nazwa, opcje, wybrany);
            wybor.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(WyborWariantuWyprawy.WybranyWariantId)
                    || !ReferenceEquals(Edytowana, szkic) || szkic.ZestawId != zestaw.Id)
                    return;
                if (wybor.WybranyWariantId is { } wariantId)
                    szkic.WybraneWarianty[wybor.SlotId] = wariantId;
                else
                    szkic.WybraneWarianty.Remove(wybor.SlotId);
            };
            WariantyEdycji.Add(wybor);
        }
    }

    /// <summary>Zaznaczenie w tabeli również tworzy migawkę przez OnWybranaChanged.</summary>
    [RelayCommand]
    private void Wybierz(WierszWyprawy? wiersz) => Wybrana = wiersz;

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
        PrzygotujWariantyNowej();
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

    partial void OnNowaChanged(Wyprawa value)
    {
        OnPropertyChanged(nameof(CzyPrzedDniemZerowym));
        OnPropertyChanged(nameof(KandydaciOdejmowania));
        // po dodaniu wyprawy formularz wraca z tym samym zestawem — wybierz warianty od razu,
        // żeby kolejna wyprawa nie została z pustym składem (FR-704)
        if (value.ZestawId != null)
            UstawZestaw(_dane.Zestawy.FirstOrDefault(z => z.Id == value.ZestawId));
        else
            WariantyNowej.Clear();
    }

    private static void PokazBlad(string blad) =>
        System.Windows.MessageBox.Show(blad, "Wyprawy",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);

    /// <summary>Dodawanie z jawną decyzją o historycznej korekcie.</summary>
    [RelayCommand]
    private void DodajWyprawe()
    {
        if (Nowa.Data == default)
        {
            PokazBlad("Wybierz datę wyprawy.");
            return;
        }
        Nowa.RekonstrukcjaId ??= _stan.AktywnaRekonstrukcjaId;
        var odlicz = false;
        if (CzyPrzedDniemZerowym && KandydaciOdejmowania.Count > 0)
        {
            var tresc = string.Join("\n", KandydaciOdejmowania.Select(k => "• " + k.Opis));
            var wybor = System.Windows.MessageBox.Show(
                $"Ta wyprawa jest sprzed dnia zerowego ({_dane.Ustawienia.DzienZerowy:dd.MM.yyyy}).\n" +
                "Czy odjąć poniższe pozycje od liczników historycznych, aby nie liczyć ich podwójnie?\n" +
                tresc + "\n\nTak — odlicz, Nie — dodaj bez korekty, Anuluj — przerwij.",
                "Wyprawy — korekta historyczna",
                System.Windows.MessageBoxButton.YesNoCancel, System.Windows.MessageBoxImage.Question);
            if (wybor == System.Windows.MessageBoxResult.Cancel)
                return;
            odlicz = wybor == System.Windows.MessageBoxResult.Yes;
        }
        var nowa = Nowa;
        if (!_operacje.SprobujDodac(nowa, odlicz, out var blad))
        {
            PokazBlad(blad);
            return;
        }
        if (nowa.ZestawId != null)
        {
            var zestaw = _dane.Zestawy.FirstOrDefault(z => z.Id == nowa.ZestawId);
            if (zestaw != null)
                foreach (var (slotId, wariantId) in nowa.WybraneWarianty)
                    zestaw.OstatnieWarianty[slotId] = wariantId;
        }
        _magazyn.ZapiszDebounce(_dane);
        Nowa = new Wyprawa { Data = DateOnly.FromDateTime(DateTime.Today), ZestawId = nowa.ZestawId };
        Odswiez();
        Wybrana = Wiersze.FirstOrDefault(w => w.Id == nowa.Id);
    }

    /// <summary>Zapisuje szkic; w przypadku błędu oryginał zostaje bez zmian.</summary>
    [RelayCommand]
    private void ZapiszWybrana()
    {
        if (Wybrana == null || Edytowana == null)
            return;
        if (!_operacje.SprobujZapisac(Wybrana.Wyprawa, Edytowana, out var blad))
        {
            PokazBlad(blad);
            return;
        }
        _magazyn.ZapiszDebounce(_dane);
        Odswiez();
    }

    /// <summary>Porzuca niezapisany szkic bez modyfikacji źródła prawdy.</summary>
    [RelayCommand]
    private void AnulujEdycje()
    {
        Edytowana = Wybrana != null && !Wybrana.CzyUsunieta
            ? OperacjeNaWyprawach.Kopia(Wybrana.Wyprawa) : null;
        ZestawEdycjiId = Edytowana?.ZestawId;
        PrzygotujWariantyEdycji();
    }

    [RelayCommand]
    private void Usun(WierszWyprawy? wiersz)
    {
        if (wiersz == null || wiersz.CzyUsunieta)
            return;
        var wybor = System.Windows.MessageBox.Show(
            $"Usunąć wyprawę z dnia {wiersz.Wyprawa.Data:dd.MM.yyyy}?\nMożna ją później przywrócić.",
            "Wyprawy", System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (wybor != System.Windows.MessageBoxResult.Yes)
            return;
        if (!_operacje.SprobujUsun(wiersz.Wyprawa, out var blad))
        {
            PokazBlad(blad);
            return;
        }
        _magazyn.ZapiszDebounce(_dane);
        Odswiez();
    }

    [RelayCommand]
    private void Przywroc(WierszWyprawy? wiersz)
    {
        if (wiersz == null || !wiersz.CzyUsunieta)
            return;
        var odlicz = false;
        // Stare wersje zerowały flagę przy usuwaniu: nie odtwarzamy jej z domysłów.
        if (!wiersz.Wyprawa.OdliczonaOdLicznikow
            && _operacje.CzyHistoryczna(wiersz.Wyprawa)
            && _kalkulator.Kalkulator().WplywNaLicznikiHistoryczne(wiersz.Wyprawa).Count > 0)
        {
            var wybor = System.Windows.MessageBox.Show(
                "Ta wyprawa jest sprzed dnia zerowego, ale nie ma zapisanej informacji o korekcie.\n" +
                "Czy odjąć ją od liczników historycznych?\n" +
                "Tak — odlicz; Nie — przywróć bez korekty; Anuluj — przerwij.",
                "Wyprawy — przywracanie historii",
                System.Windows.MessageBoxButton.YesNoCancel, System.Windows.MessageBoxImage.Question);
            if (wybor == System.Windows.MessageBoxResult.Cancel)
                return;
            odlicz = wybor == System.Windows.MessageBoxResult.Yes;
        }
        if (!_operacje.SprobujPrzywroc(wiersz.Wyprawa, odlicz, out var blad))
        {
            PokazBlad(blad);
            return;
        }
        _magazyn.ZapiszDebounce(_dane);
        if (PokazUsuniete)
            PokazUsuniete = false; // Przejdź do listy aktywnych, żeby użytkownik widział wynik.
        else
            Odswiez();
        Wybrana = Wiersze.FirstOrDefault(w => w.Id == wiersz.Id);
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

/// <summary>Czytelna opcja wariantu. Typ UI, nie jest częścią JSON.</summary>
public sealed record OpcjaWariantuWyprawy(string Id, string Opis);

/// <summary>Wiersz edytora wariantów.</summary>
public sealed class WyborWariantuWyprawy : ObservableObject
{
    public WyborWariantuWyprawy(string slotId, string nazwaSlotu,
        IReadOnlyList<OpcjaWariantuWyprawy> opcje, string? wybranyWariantId)
    {
        SlotId = slotId;
        NazwaSlotu = nazwaSlotu;
        Opcje = opcje;
        _wybranyWariantId = wybranyWariantId;
    }

    public string SlotId { get; }
    public string NazwaSlotu { get; }
    public IReadOnlyList<OpcjaWariantuWyprawy> Opcje { get; }

    private string? _wybranyWariantId;
    public string? WybranyWariantId
    {
        get => _wybranyWariantId;
        set => SetProperty(ref _wybranyWariantId, value);
    }
}
