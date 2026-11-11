using System.IO;
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

    /// <summary>Powiadomienie po udanym imporcie: wszystkie zakładki odświeżają dane.</summary>
    public event Action? DaneZmienione;

    [ObservableProperty]
    private bool _czyZajety;

    public bool CzyNieZajety => !CzyZajety;
    public bool MaPodglad => Podglad != null && CzyNieZajety;
    public bool MaPodgladOgolny => _podgladOgolny.Count > 0 && CzyNieZajety;

    partial void OnCzyZajetyChanged(bool value)
    {
        OnPropertyChanged(nameof(CzyNieZajety));
        OnPropertyChanged(nameof(MaPodglad));
        OnPropertyChanged(nameof(MaPodgladOgolny));
    }

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

    /// <summary>Inne importy mogły zmienić słowniki; stary podgląd należy zbudować od nowa.</summary>
    public void OdswiezZrodla()
    {
        OnPropertyChanged(nameof(Kategorie));
        if (Podglad != null)
        {
            Podglad = null;
            Komunikat = "Dane zmieniły się. Wczytaj plik ponownie, aby przygotować aktualny podgląd.";
        }
        UniewaznijPodgladOgolny();
    }

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
        OnPropertyChanged(nameof(MaPodglad));
        if (value != null)
            foreach (var konflikt in value.Konflikty)
                if (konflikt.WyborUzytkownika == null && konflikt.DomyslnyWybor >= 0)
                    konflikt.WyborUzytkownika = konflikt.DomyslnyWybor;
    }

    /// <summary>Wybiera plik xlsx i przygotowuje podgląd importu (FR-1102). Nic nie jest zapisywane.</summary>
    [RelayCommand]
    private async Task WybierzPlik()
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
            CzyZajety = true;
            Podglad = null;
            Sciezka = dialog.FileName;
            Komunikat = "Wczytywanie arkusza i przygotowywanie podglądu…";
            var wynik = await Task.Run(() => ImporterKwatermistrz.PrzygotujPodglad(dialog.FileName));
            Podglad = wynik;
            Komunikat = $"Podgląd gotowy: {wynik.Konflikty.Count} pozycji do rozstrzygnięcia.";
        }
        catch (Exception ex)
        {
            Podglad = null;
            Sciezka = "";
            Komunikat = $"Nie udało się odczytać pliku: {ex.Message}";
        }
        finally
        {
            CzyZajety = false;
        }
    }

    /// <summary>Zatwierdza import: kopia zapasowa, zastosowanie podglądu, zapis (FR-1102, FR-1107).</summary>
    [RelayCommand]
    private void Zastosuj()
    {
        if (Podglad == null || CzyZajety)
            return;
        if (_dane.Przedmioty.Count > 0 || _dane.Repliki.Count > 0 || _dane.Wyprawy.Count > 0)
        {
            var decyzja = System.Windows.MessageBox.Show(
                "Baza zawiera już dane. Ten import doda kolejne rekordy i może utworzyć duplikaty " +
                "przy ponownym wczytaniu tego samego arkusza.\n" +
                "Istniejący dzień zerowy i liczniki historyczne nie zostaną nadpisane.\n\n" +
                "Czy mimo to kontynuować?",
                "Import do istniejącej bazy",
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (decyzja != System.Windows.MessageBoxResult.Yes)
                return;
        }
        try
        {
            CzyZajety = true;
            // Kopia przed zmianą, potem import na niezależnym obiekcie.
            _magazyn.UtworzKopieZapasowa(_dane);
            TransakcjaDanych.Wykonaj(_dane, robocza =>
            {
                ImporterKwatermistrz.Zastosuj(Podglad, robocza);
                return true;
            });
            _magazyn.ZapiszDebounce(_dane);
            _magazyn.Zapisz(_dane);
            Podglad = null;
            Sciezka = "";
            Komunikat = "Import zakończony. Dane zapisane, poprzedni stan w kopii zapasowej.";
            DaneZmienione?.Invoke();
        }
        catch (Exception ex)
        {
            Podglad = null; // nie ponawiaj automatycznie częściowo przetworzonego podglądu
            Komunikat = "Błąd importu. Sprawdź kopię zapasową: " + ex.Message;
        }
        finally
        {
            CzyZajety = false;
        }
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

    private void UniewaznijPodgladOgolny()
    {
        if (_podgladOgolny.Count == 0)
            return;
        _podgladOgolny = new();
        OnPropertyChanged(nameof(PodgladOgolny));
        OnPropertyChanged(nameof(LiczbaPodgladOgolny));
        OnPropertyChanged(nameof(MaPodgladOgolny));
    }

    partial void OnKategoriaDocelowaChanged(Kategoria? value) => UniewaznijPodgladOgolny();
    partial void OnSciezkaOgolnaChanged(string value) => UniewaznijPodgladOgolny();

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
    private async Task WybierzPlikOgolny()
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
            CzyZajety = true;
            SciezkaOgolna = dialog.FileName;
            _arkuszeOgolne.Clear();
            MapowanieKolumny.Clear();
            Komunikat = "Wczytywanie arkusza i nagłówków…";
            var arkusze = await Task.Run(() => ImporterOgolny.WczytajPlik(dialog.FileName));
            var arkusz = arkusze.FirstOrDefault(a => a.Wiersze.Any(w => w.Komorki.Any(c => !string.IsNullOrWhiteSpace(c))));
            if (arkusz == null)
                throw new InvalidDataException("Brak niepustego arkusza z nagłówkami.");
            _arkuszeOgolne = new List<ArkuszXlsx> { arkusz };
            NazwaArkusza = arkusz.Nazwa;
            _wierszNaglowkaOgolny = arkusz.Wiersze.FindIndex(w => w.Komorki.Any(c => !string.IsNullOrWhiteSpace(c)));
            var sugestia = ImporterOgolny.SugerujMapowanie(arkusz, _wierszNaglowkaOgolny);
            var maxKol = arkusz.Wiersze.Select(w => w.Komorki.Count).DefaultIfEmpty(0).Max();
            var naglowek = arkusz.Wiersze[_wierszNaglowkaOgolny];
            for (var i = 0; i < maxKol; i++)
            {
                var w = new WierszMapowaniaKolumny
                {
                    Indeks = i,
                    Naglowek = naglowek.Komorka(i),
                    Pole = sugestia.Kolumny.TryGetValue(i, out var p) ? p : PoleImportu.Puste,
                };
                w.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(WierszMapowaniaKolumny.Pole))
                        UniewaznijPodgladOgolny();
                };
                MapowanieKolumny.Add(w);
            }
            Komunikat = $"Plik wczytany: {arkusz.Wiersze.Count} wierszy. Sprawdź mapowanie i kliknij „Podgląd”.";
        }
        catch (Exception ex)
        {
            _arkuszeOgolne.Clear();
            SciezkaOgolna = "";
            MapowanieKolumny.Clear();
            Komunikat = $"Nie udało się odczytać pliku: {ex.Message}";
        }
        finally
        {
            CzyZajety = false;
        }
    }

    /// <summary>Buduje podgląd importu ogólnego wg bieżącego mapowania (FR-1102, FR-1403). Nic nie zapisuje.</summary>
    [RelayCommand]
    private void PodgladOgolnyKomenda()
    {
        if (_arkuszeOgolne.Count == 0 || CzyZajety)
        {
            Komunikat = "Najpierw wybierz plik.";
            return;
        }
        if (!MapowanieKolumny.Any(w => w.Pole is PoleImportu.Nazwa or PoleImportu.Firma))
        {
            UniewaznijPodgladOgolny();
            Komunikat = "Przypisz co najmniej jedną kolumnę do pola „Nazwa” lub „Firma”.";
            return;
        }
        if (MapowanieKolumny.Where(w => w.Pole != PoleImportu.Puste)
            .GroupBy(w => w.Pole).Any(g => g.Count() > 1))
        {
            UniewaznijPodgladOgolny();
            Komunikat = "Jedno pole docelowe nie może być przypisane do wielu kolumn.";
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
        OnPropertyChanged(nameof(MaPodgladOgolny));
        Komunikat = $"Podgląd: {LiczbaPodgladOgolny} przedmiotów do dodania (zaznacz „Pomiń”, aby wykluczyć wiersz).";
    }

    /// <summary>Zatwierdza import ogólny: kopia zapasowa, dodanie przedmiotów, zapis (FR-1102, FR-1107, FR-1403).</summary>
    [RelayCommand]
    private void ZastosujOgolny()
    {
        if (_podgladOgolny.Count == 0 || CzyZajety)
        {
            Komunikat = "Najpierw przygotuj podgląd.";
            return;
        }
        try
        {
            CzyZajety = true;
            var wybrane = _podgladOgolny.Select(w =>
            {
                w.Wiersz.Pomin = w.Pomin;
                return w.Wiersz;
            }).ToList();
            _magazyn.UtworzKopieZapasowa(_dane);
            var liczba = TransakcjaDanych.Wykonaj(_dane, robocza => ImporterOgolny.Zastosuj(robocza, wybrane));
            _magazyn.ZapiszDebounce(_dane);
            _magazyn.Zapisz(_dane);
            UniewaznijPodgladOgolny();
            Komunikat = $"Zaimportowano {liczba} przedmiotów. Rekordy mają znacznik „z importu, do przeglądu”.";
            DaneZmienione?.Invoke();
        }
        catch (Exception ex)
        {
            UniewaznijPodgladOgolny();
            Komunikat = "Błąd importu ogólnego: " + ex.Message;
        }
        finally
        {
            CzyZajety = false;
        }
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
        UniewaznijPodgladOgolny();
    }
}
