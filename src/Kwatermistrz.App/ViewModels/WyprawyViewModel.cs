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

    /// <summary>Wybiera wyprawę do edycji (FR-808).</summary>
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
                // FR-704: wariant ostatnio użyty na wyprawie, potem domyślny, potem pierwszy
                var indeks = zestaw.WariantDomyslnyNaSlot(slot.Id)
                    ?? slot.IndeksWariantuDomyslnego
                    ?? (slot.Warianty.Count > 0 ? 0 : null);
                if (indeks != null)
                    Nowa.WybraneWarianty[slot.Id] = indeks.Value;
            }
            // podpowiedź repliki głównej: pierwszy wariant slotu broni głównej (FR-802)
            var rek = _dane.Rekonstrukcje.FirstOrDefault(r => r.Id == (zestaw.RekonstrukcjaId ?? _stan.AktywnaRekonstrukcjaId));
            var slotyBroni = rek?.SzablonSlotow.Where(s => s.CzySlotBroniGlownej).Select(s => s.Nazwa).ToHashSet() ?? new HashSet<string>();
            foreach (var slot in zestaw.Sloty.Where(s => slotyBroni.Contains(s.Nazwa)))
            {
                if (!Nowa.WybraneWarianty.TryGetValue(slot.Id, out var indeks) || indeks < 0 || indeks >= slot.Warianty.Count)
                    continue;
                var pierwszy = slot.Warianty[indeks].Elementy.FirstOrDefault(e => e.Typ == TypElementu.Replika);
                if (pierwszy != null && Nowa.ReplikaGlownaId == null)
                    Nowa.ReplikaGlownaId = pierwszy.ElementId;
            }
        }
        OnPropertyChanged(nameof(Nowa));
    }

    /// <summary>Szybkie dodanie wyprawy (FR-802): Enter, domyślna data dziś, zestaw ostatni (FR-803).</summary>
    [RelayCommand]
    private void DodajWyprawe()
    {
        if (Nowa.Data == default)
            Nowa.Data = DateOnly.FromDateTime(DateTime.Today);
        if (Nowa.RekonstrukcjaId == null)
            Nowa.RekonstrukcjaId = _stan.AktywnaRekonstrukcjaId;
        // dzień zerowy: wyprawa z datą ≤ dzień zerowy to uzupełnienie historii (FR-807) — na razie zapisujemy
        _dane.Wyprawy.Add(Nowa);
        _magazyn.Zapisz(_dane);
        // zapamiętaj warianty jako ostatnio użyte (FR-704)
        if (Nowa.ZestawId != null)
        {
            var zestaw = _dane.Zestawy.FirstOrDefault(z => z.Id == Nowa.ZestawId);
            if (zestaw != null)
                foreach (var (slotId, indeks) in Nowa.WybraneWarianty)
                    zestaw.OstatnieWarianty[slotId] = indeks;
        }
        Nowa = new Wyprawa { Data = DateOnly.FromDateTime(DateTime.Today), ZestawId = Nowa.ZestawId };
        OnPropertyChanged(nameof(Nowa));
        Odswiez();
    }

    /// <summary>Zapisuje edycję wybranej wyprawy (FR-808, FR-809).</summary>
    [RelayCommand]
    private void ZapiszWybrana()
    {
        if (Wybrana == null)
            return;
        _magazyn.Zapisz(_dane);
        Odswiez();
    }

    /// <summary>Usuwa wyprawę (oznacza jako usuniętą; FR-808/809 — liczniki aktualizują się od razu).</summary>
    [RelayCommand]
    private void Usun(WierszWyprawy? wiersz)
    {
        if (wiersz == null)
            return;
        wiersz.Wyprawa.CzyUsunieta = true;
        _magazyn.Zapisz(_dane);
        Odswiez();
    }

    /// <summary>Przywraca usuniętą wyprawę (cofnięcie, FR-808).</summary>
    [RelayCommand]
    private void Przywroc(WierszWyprawy? wiersz)
    {
        if (wiersz == null || !wiersz.Wyprawa.CzyUsunieta)
            return;
        wiersz.Wyprawa.CzyUsunieta = false;
        _magazyn.Zapisz(_dane);
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
