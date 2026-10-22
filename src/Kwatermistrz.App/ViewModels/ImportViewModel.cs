using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwatermistrz.Core.Import;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Storage;
using Microsoft.Win32;

namespace Kwatermistrz.App.ViewModels;

/// <summary>Wiersz podglądu: ile rekordów danego typu powstanie przy imporcie (FR-1102).</summary>
public class RekordPodgladu
{
    public string Nazwa { get; set; } = "";
    public int Liczba { get; set; }
}

/// <summary>ViewModel ekranu importu (FR-1101, FR-1102): wybór pliku xlsx, podgląd rekordów i konfliktów, zatwierdzenie z kopią zapasową (FR-1107).</summary>
public partial class ImportViewModel : ObservableObject
{
    private readonly DaneAplikacji _dane;
    private readonly MagazynJson _magazyn;

    public ImportViewModel(DaneAplikacji dane, MagazynJson magazyn)
    {
        _dane = dane;
        _magazyn = magazyn;
    }

    /// <summary>Przygotowany podgląd importu; null przed wyborem pliku.</summary>
    [ObservableProperty]
    private PodgladImportu? _podglad;

    /// <summary>Wybrana ścieżka pliku źródłowego.</summary>
    [ObservableProperty]
    private string _sciezka = "";

    /// <summary>Komunikat dla użytkownika (wynik importu lub błąd).</summary>
    [ObservableProperty]
    private string _komunikat = "";

    /// <summary>Rekordy podglądu: ile powstanie rekordów wg typu.</summary>
    public IReadOnlyList<RekordPodgladu> LiczbaRekordow =>
        Podglad?.LiczbaRekordow.Select(x => new RekordPodgladu { Nazwa = x.Key, Liczba = x.Value }).ToList()
        ?? new List<RekordPodgladu>();

    /// <summary>Konflikty i wątpliwości do rozstrzygnięcia przed importem.</summary>
    public IReadOnlyList<KonfliktImportu> Konflikty => Podglad?.Konflikty ?? new List<KonfliktImportu>();

    partial void OnPodgladChanged(PodgladImportu? value)
    {
        OnPropertyChanged(nameof(LiczbaRekordow));
        OnPropertyChanged(nameof(Konflikty));
        if (value != null)
            foreach (var konflikt in value.Konflikty)
                if (konflikt.WyborUzytkownika == null && konflikt.DomyslnyWybor >= 0)
                    konflikt.WyborUzytkownika = konflikt.DomyslnyWybor;
    }

    /// <summary>Wybiera plik xlsx i przygotowuje podgląd importu (FR-1102). Nic nie jest zapisywane.</summary>
    [RelayCommand]
    private void WybierzPlik()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Wybierz plik źródłowy (Dane_zrodlowe_Kwatermistrz.xlsx)",
            Filter = "Arkusz Excel (*.xlsx)|*.xlsx|Wszystkie pliki (*.*)|*.*",
            CheckFileExists = true,
        };
        if (dialog.ShowDialog() != true)
            return;
        try
        {
            Sciezka = dialog.FileName;
            Podglad = ImporterKwatermistrz.PrzygotujPodglad(dialog.FileName);
            Komunikat = $"Podgląd gotowy: {Podglad.Konflikty.Count} pozycji do rozstrzygnięcia.";
        }
        catch (Exception ex)
        {
            Podglad = null;
            Komunikat = $"Nie udało się odczytać pliku: {ex.Message}";
        }
    }

    /// <summary>Zatwierdza import: kopia zapasowa, zastosowanie podglądu, zapis (FR-1102, FR-1107).</summary>
    [RelayCommand]
    private void Zastosuj()
    {
        if (Podglad == null)
            return;
        _magazyn.Zapisz(_dane);
        _magazyn.UtworzKopieZapasowa();
        ImporterKwatermistrz.Zastosuj(Podglad, _dane);
        _magazyn.Zapisz(_dane);
        Komunikat = "Import zakończony. Dane zapisane.";
        Podglad = null;
        Sciezka = "";
    }
}

/// <summary>Wiersz podglądu importu ogólnego (FR-1403): proponowany przedmiot + ostrzeżenia + decyzja „pomiń”.</summary>
public partial class WierszPodgladuOgolnegoVm : ObservableObject
{
    public WierszPodgladuOgolnegoVm(WierszPodgladuOgolnego wiersz)
    {
        Wiersz = wiersz;
    }

    public WierszPodgladuOgolnego Wiersz { get; }

    /// <summary>Pominięty przez użytkownika (nie trafi do katalogu).</summary>
    [ObservableProperty]
    private bool _pomin;

    public int NumerWiersza => Wiersz.NumerWiersza;
    public string Nazwa => Wiersz.Przedmiot.Nazwa;
    public string? Firma => Wiersz.Przedmiot.Firma;
    public string Kategoria => Wiersz.KategoriaNazwa;
    public string? Ostrzezenie => Wiersz.Ostrzezenie;
}

/// <summary>Wiersz wyboru kolumny źródłowej → pole importu (FR-1403).</summary>
/// <summary>Wiersz mapowania kolumny w imporcie ogólnym (FR-1403) — z INPC, żeby „Wczytaj mapowanie” odświeżał UI.</summary>
public partial class WierszMapowaniaKolumny : ObservableObject
{
    public int Indeks { get; set; }
    public string? Naglowek { get; set; }
    [ObservableProperty]
    private PoleImportu _pole = PoleImportu.Puste;
}

public partial class ImportViewModel
{
    private MagazynMapowan? _magazynMapowan;
    private List<WierszPodgladuOgolnegoVm> _podgladOgolny = new();

    /// <summary>Inicjalizuje magazyn mapowań (katalog danych).</summary>
    public void InicjalizujMapowania()
    {
        _magazynMapowan = new MagazynMapowan(_magazyn.KatalogDanych);
        OdswiezMapowania();
    }

    // ---- Import ogólny z mapowaniem kolumn (FR-1403) ----

    /// <summary>Ścieżka pliku do importu ogólnego (xlsx lub csv).</summary>
    [ObservableProperty]
    private string _sciezkaOgolna = "";

    /// <summary>Nazwa aktywnego arkusza (pierwszy, gdy plik xlsx).</summary>
    [ObservableProperty]
    private string _nazwaArkusza = "";

    /// <summary>Wiersze mapowania: kolumna źródłowa → pole (FR-1403).</summary>
    public ObservableCollection<WierszMapowaniaKolumny> MapowanieKolumny { get; } = new();

    /// <summary>Wszystkie pola do wyboru w mapowaniu.</summary>
    public static IReadOnlyList<(PoleImportu Pole, string Nazwa)> PolaMapowania { get; } = new[]
    {
        (PoleImportu.Puste, "— nie importuj —"),
        (PoleImportu.Nazwa, "Nazwa"),
        (PoleImportu.Firma, "Firma"),
        (PoleImportu.Kategoria, "Kategoria"),
        (PoleImportu.Strona, "Strona"),
        (PoleImportu.Wzor, "Wzór"),
        (PoleImportu.Miejsce, "Miejsce"),
        (PoleImportu.Pochodzenie, "Pochodzenie"),
        (PoleImportu.LiczbaUzyc, "Liczba użyć (licznik historyczny)"),
        (PoleImportu.Komentarz, "Komentarz"),
    };

    /// <summary>Zapisane mapowania (FR-1403 — wielokrotnego użytku).</summary>
    public ObservableCollection<MapowanieKolumn> ZapisaneMapowania { get; } = new();

    /// <summary>Wybrane zapisane mapowanie.</summary>
    [ObservableProperty]
    private MapowanieKolumn? _wybraneMapowanie;

    /// <summary>Nazwa, pod którą zapisać bieżące mapowanie.</summary>
    [ObservableProperty]
    private string _nazwaMapowania = "";

    /// <summary>Kategorie do wyboru kategorii docelowej (gdy wiersze nie mają kolumny kategorii).</summary>
    public IReadOnlyList<Kategoria> Kategorie => _dane.Slowniki.Kategorie.OrderBy(k => k.Kolejnosc).ToList();

    /// <summary>Wybrana kategoria docelowa importu ogólnego; null = wg kolumny „Kategoria” wiersza.</summary>
    [ObservableProperty]
    private Kategoria? _kategoriaDocelowa;

    /// <summary>Podgląd wierszy importu ogólnego (FR-1102).</summary>
    public IReadOnlyList<WierszPodgladuOgolnegoVm> PodgladOgolny => _podgladOgolny;

    /// <summary>Liczba wierszy w podglądzie importu ogólnego (niepominiętych).</summary>
    public int LiczbaPodgladOgolny => _podgladOgolny.Count(w => !w.Pomin);

    private List<ArkuszXlsx> _arkuszeOgolne = new();
    private int _wierszNaglowkaOgolny;

    private void OdswiezMapowania()
    {
        if (_magazynMapowan == null)
            return;
        ZapisaneMapowania.Clear();
        foreach (var m in _magazynMapowan.Wczytaj())
            ZapisaneMapowania.Add(m);
    }

    /// <summary>Wybiera plik xlsx/csv do importu ogólnego i pokazuje mapowanie kolumn z sugestią (FR-1403).</summary>
    [RelayCommand]
    private void WybierzPlikOgolny()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Wybierz plik do importu (xlsx lub csv)",
            Filter = "Arkusz Excel (*.xlsx)|*.xlsx|Plik CSV (*.csv)|*.csv|Wszystkie pliki (*.*)|*.*",
            CheckFileExists = true,
        };
        if (dialog.ShowDialog() != true)
            return;
        try
        {
            SciezkaOgolna = dialog.FileName;
            _arkuszeOgolne = ImporterOgolny.WczytajPlik(dialog.FileName);
            var arkusz = _arkuszeOgolne.First();
            NazwaArkusza = arkusz.Nazwa;
            // pierwszy niepusty wiersz to nagłówek — dane zaczynają się od następnego (inaczej nagłówek leciałby jako rekord)
            _wierszNaglowkaOgolny = arkusz.Wiersze.FindIndex(w => w.Komorki.Any(c => !string.IsNullOrWhiteSpace(c)));
            var sugestia = ImporterOgolny.SugerujMapowanie(arkusz, _wierszNaglowkaOgolny);
            MapowanieKolumny.Clear();
            var maxKol = arkusz.Wiersze.Select(w => w.Komorki.Count).DefaultIfEmpty(0).Max();
            var naglowek = arkusz.Wiersze.FirstOrDefault();
            for (var i = 0; i < maxKol; i++)
                MapowanieKolumny.Add(new WierszMapowaniaKolumny
                {
                    Indeks = i,
                    Naglowek = naglowek?.Komorka(i),
                    Pole = sugestia.Kolumny.TryGetValue(i, out var p) ? p : PoleImportu.Puste,
                });
            Komunikat = $"Plik wczytany: {arkusz.Wiersze.Count} wierszy. Ustaw mapowanie kolumn i kliknij „Podgląd”.";
        }
        catch (Exception ex)
        {
            Komunikat = $"Nie udało się odczytać pliku: {ex.Message}";
        }
    }

    /// <summary>Buduje podgląd importu ogólnego wg bieżącego mapowania (FR-1102, FR-1403). Nic nie zapisuje.</summary>
    [RelayCommand]
    private void PodgladOgolnyKomenda()
    {
        if (_arkuszeOgolne.Count == 0)
        {
            Komunikat = "Najpierw wybierz plik.";
            return;
        }
        var mapowanie = new MapowanieKolumn
        {
            Nazwa = NazwaMapowania,
            Kolumny = MapowanieKolumny.ToDictionary(w => w.Indeks, w => w.Pole),
            KategoriaId = KategoriaDocelowa?.Id,
        };
        var wiersze = ImporterOgolny.PrzygotujPodglad(_dane, _arkuszeOgolne.First(), mapowanie, _wierszNaglowkaOgolny + 1);
        _podgladOgolny = wiersze.Select(w => new WierszPodgladuOgolnegoVm(w)).ToList();
        foreach (var w in _podgladOgolny)
            w.PropertyChanged += (_, _) => OnPropertyChanged(nameof(LiczbaPodgladOgolny));
        OnPropertyChanged(nameof(PodgladOgolny));
        OnPropertyChanged(nameof(LiczbaPodgladOgolny));
        Komunikat = $"Podgląd: {LiczbaPodgladOgolny} przedmiotów do dodania (zaznacz „Pomiń”, aby wykluczyć wiersz).";
    }

    /// <summary>Zatwierdza import ogólny: kopia zapasowa, dodanie przedmiotów, zapis (FR-1102, FR-1107, FR-1403).</summary>
    [RelayCommand]
    private void ZastosujOgolny()
    {
        if (_podgladOgolny.Count == 0)
        {
            Komunikat = "Najpierw przygotuj podgląd.";
            return;
        }
        _magazyn.UtworzKopieZapasowa(); // FR-1107: przed importem kopia (kopia zawiera stan sprzed importu)
        var liczba = ImporterOgolny.Zastosuj(_dane, _podgladOgolny.Select(w => { w.Wiersz.Pomin = w.Pomin; return w.Wiersz; }));
        _magazyn.Zapisz(_dane);
        Komunikat = $"Zaimportowano {liczba} przedmiotów. Rekordy mają znacznik „z importu, do przeglądu” (FR-1110).";
        _podgladOgolny = new List<WierszPodgladuOgolnegoVm>();
        OnPropertyChanged(nameof(PodgladOgolny));
        OnPropertyChanged(nameof(LiczbaPodgladOgolny));
    }

    /// <summary>Zapisuje bieżące mapowanie pod nazwą (FR-1403 — wielokrotnego użytku).</summary>
    [RelayCommand]
    private void ZapiszMapowanie()
    {
        if (_magazynMapowan == null || string.IsNullOrWhiteSpace(NazwaMapowania))
            return;
        var mapowanie = new MapowanieKolumn
        {
            Nazwa = NazwaMapowania.Trim(),
            Kolumny = MapowanieKolumny.ToDictionary(w => w.Indeks, w => w.Pole),
            KategoriaId = KategoriaDocelowa?.Id,
        };
        _magazynMapowan.ZapiszJedno(mapowanie);
        OdswiezMapowania();
        Komunikat = $"Zapisano mapowanie „{mapowanie.Nazwa}”.";
    }

    /// <summary>Usuwa wybrane zapisane mapowanie.</summary>
    [RelayCommand]
    private void UsunMapowanie(MapowanieKolumn? mapowanie)
    {
        if (_magazynMapowan == null || mapowanie == null)
            return;
        _magazynMapowan.Usun(mapowanie.Nazwa);
        OdswiezMapowania();
    }

    /// <summary>Wczytuje wybrane zapisane mapowanie do edytora kolumn.</summary>
    [RelayCommand]
    private void WczytajMapowanie(MapowanieKolumn? mapowanie)
    {
        if (mapowanie == null)
            return;
        NazwaMapowania = mapowanie.Nazwa;
        KategoriaDocelowa = mapowanie.KategoriaId != null
            ? _dane.Slowniki.Kategorie.FirstOrDefault(k => k.Id == mapowanie.KategoriaId)
            : null;
        foreach (var w in MapowanieKolumny)
            w.Pole = mapowanie.Kolumny.TryGetValue(w.Indeks, out var p) ? p : PoleImportu.Puste;
    }
}
