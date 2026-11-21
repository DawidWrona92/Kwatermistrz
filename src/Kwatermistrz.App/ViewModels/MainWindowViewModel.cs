using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwatermistrz.App.Services;
using Kwatermistrz.Core.Konfiguracja;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Storage;
using Microsoft.Win32;

namespace Kwatermistrz.App.ViewModels;

/// <summary>ViewModel okna głównego: nawigacja, przełącznik rekonstrukcji (FR-1303), wyszukiwanie.</summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly DaneAplikacji _dane;
    private readonly StanAplikacji _stan;

    public MainWindowViewModel(
        DaneAplikacji dane,
        StanAplikacji stan,
        PulpitViewModel pulpit,
        SlownikiViewModel slowniki,
        RekonstrukcjeViewModel rekonstrukcje,
        UstawieniaViewModel ustawienia,
        ImportViewModel import,
        PakietyViewModel pakiety,
        WyposazenieViewModel wyposazenie,
        KategorieViewModel kategorie,
        ReplikiViewModel repliki,
        ZestawyViewModel zestawy,
        WyprawyViewModel wyprawy,
        RaportyViewModel raporty,
        DoUzupelnieniaViewModel doUzupelnienia,
        SzukanieViewModel szukanie,
        MagazynJson magazyn)
    {
        _dane = dane;
        _magazyn = magazyn;
        _stan = stan;
        Pulpit = pulpit;
        Slowniki = slowniki;
        Rekonstrukcje = rekonstrukcje;
        Ustawienia = ustawienia;
        Import = import;
        Pakiety = pakiety;
        Wyposazenie = wyposazenie;
        Kategorie = kategorie;
        Repliki = repliki;
        Zestawy = zestawy;
        Wyprawy = wyprawy;
        Raporty = raporty;
        DoUzupelnienia = doUzupelnienia;
        Szukanie = szukanie;
        _stan.AktywnaRekonstrukcjaId = dane.Ustawienia.OstatniaRekonstrukcjaId;
        if (!string.IsNullOrEmpty(dane.Ustawienia.OstatniEkran))
            _stan.AktualnyEkran = dane.Ustawienia.OstatniEkran;
        // po przywróceniu danych z kopii wszystkie ekrany odświeżają listy (FR-1107)
        ustawienia.DanePrzywrocone += OdswiezWszystkieEkrany;
        import.DaneZmienione += OdswiezWszystkieEkrany;
        pakiety.DaneZmienione += OdswiezWszystkieEkrany;
        // Jedno źródło powiadomień dla zwykłych zapisów (również z innych zakładek).
        magazyn.DaneZmienione += ZaplanujOdswiezenieDanych;
    }

    private bool _odswiezanieZaplanowane;

    /// <summary>
    /// Jeden sygnał zmiany danych: odświeżenie widoków po zakończeniu bieżącej operacji,
    /// bez ponownego liczenia wszystkiego przy każdej serializacji lub klawiszu.
    /// </summary>
    private void ZaplanujOdswiezenieDanych()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.HasShutdownStarted)
            return;
        if (!dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(new Action(ZaplanujOdswiezenieDanych), DispatcherPriority.Background);
            return;
        }
        if (_odswiezanieZaplanowane)
            return;
        _odswiezanieZaplanowane = true;
        dispatcher.BeginInvoke(new Action(() =>
        {
            if (!_odswiezanieZaplanowane)
                return; // pełna wymiana danych mogła już odświeżyć wszystkie widoki
            _odswiezanieZaplanowane = false;
            OdswiezWidokiZalezne();
        }), DispatcherPriority.ContextIdle);
    }

    /// <summary>
    /// Zwykła edycja: aktualizuje widoki zależne, ale nie przebudowuje aktualnie
    /// edytowanego formularza (utrata fokusu i niezapisanych zmian).
    /// Ekran docelowy odświeża się ponadto przy wejściu na niego.
    /// </summary>
    private void OdswiezWidokiZalezne()
    {
        var aktywny = _stan.AktualnyEkran;
        if (aktywny != "Slowniki")
            Slowniki.Odswiez();
        if (aktywny != "Wyposazenie")
        {
            Kategorie.Odswiez();
            Wyposazenie.Odswiez();
        }
        if (aktywny != "Rekonstrukcje")
            Rekonstrukcje.Odswiez();
        if (aktywny != "Bronie")
            Repliki.Odswiez();
        if (aktywny != "Zestawy")
            Zestawy.Odswiez();
        if (aktywny != "Wyprawy")
            Wyprawy.Odswiez();
        if (aktywny != "Ustawienia")
            Ustawienia.Odswiez();
        Pulpit.Odswiez();
        Raporty.Odswiez();
        DoUzupelnienia.Odswiez();
        if (!string.IsNullOrWhiteSpace(_stan.SzukajTekst))
            Szukanie.Szukaj(_stan.SzukajTekst);
        OnPropertyChanged(nameof(RekonstrukcjeLista));
        OnPropertyChanged(nameof(AktywnaRekonstrukcjaNazwa));
    }

    /// <summary>Pełny refresh po imporcie/przywróceniu. Resetuje kopie edycji tylko wtedy.</summary>
    private void OdswiezWszystkieEkrany()
    {
        _odswiezanieZaplanowane = false;
        // Aktywne ID musi istnieć w nowej bazie. Inaczej wszystkie listy filtrują do zera.
        var przywroconeId = _dane.Ustawienia.OstatniaRekonstrukcjaId;
        if (przywroconeId != null && !_dane.Rekonstrukcje.Any(r => r.Id == przywroconeId))
        {
            przywroconeId = null;
            _dane.Ustawienia.OstatniaRekonstrukcjaId = null;
            _magazyn.ZapiszDebounce(_dane); // napraw filtr również na kolejny restart
        }
        if (_stan.AktywnaRekonstrukcjaId != przywroconeId)
            _stan.AktywnaRekonstrukcjaId = przywroconeId;

        Slowniki.Odswiez();
        Kategorie.Odswiez();
        Rekonstrukcje.Odswiez();
        Ustawienia.Odswiez();
        Import.OdswiezZrodla();
        Pakiety.UniewaznijPodgladPoZmianieDanych();
        Pulpit.Odswiez();
        Wyposazenie.Odswiez();
        Repliki.Odswiez();
        Zestawy.Odswiez();
        Wyprawy.PrzeladujPoWymianieDanych();
        Raporty.Odswiez();
        DoUzupelnienia.Odswiez();
        Szukanie.Szukaj(_stan.SzukajTekst);
        OnPropertyChanged(nameof(RekonstrukcjeLista));
        OnPropertyChanged(nameof(AktywnaRekonstrukcjaId));
        OnPropertyChanged(nameof(AktywnaRekonstrukcjaNazwa));
    }

    private readonly MagazynJson _magazyn;

    /// <summary>Komunikat paska stanu (wyniki eksportów/importów rekonstrukcji, FR-1106/FR-1405).</summary>
    [ObservableProperty]
    private string _komunikat = "";

    /// <summary>Eksport całości do jednego pliku Excel (FR-1106): arkusze Broń, Wyposażenie, Zestawy, Wyprawy, Zadania, Słowniki.</summary>
    [RelayCommand]
    private void EksportujXlsx()
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Title = "Eksport całości do Excela",
                Filter = "Arkusz Excel (*.xlsx)|*.xlsx",
                FileName = $"Kwatermistrz-eksport-{DateTime.Now:yyyyMMdd}.xlsx",
                DefaultExt = ".xlsx",
            };
            if (dialog.ShowDialog() != true)
                return;
            var arkusze = Kwatermistrz.Core.Eksport.EksporterXlsx.BudujArkusze(_dane);
            Kwatermistrz.Core.Eksport.EksporterXlsx.Zapisz(arkusze, dialog.FileName);
            Komunikat = $"Wyeksportowano do Excela: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            Komunikat = "Błąd eksportu: " + ex.Message;
        }
    }

    /// <summary>Eksport wybranej rekonstrukcji (dane + zdjęcia) do pliku .kwreko (FR-1405).</summary>
    [RelayCommand]
    private void EksportujRekonstrukcje(Rekonstrukcja? rekonstrukcja)
    {
        if (rekonstrukcja == null)
            return;
        try
        {
            var dialog = new SaveFileDialog
            {
                Title = $"Eksport rekonstrukcji „{rekonstrukcja.Nazwa}”",
                Filter = "Eksport rekonstrukcji Kwatermistrz (*.kwreko)|*.kwreko",
                FileName = $"Kwatermistrz-{rekonstrukcja.Nazwa}-{DateTime.Now:yyyyMMdd}.kwreko",
                DefaultExt = ".kwreko",
            };
            if (dialog.ShowDialog() != true)
                return;
            var (przedmioty, repliki, zestawy, wyprawy, zadania, zdjecia) =
                EksportRekonstrukcji.Eksportuj(_dane, rekonstrukcja.Id, _magazyn.KatalogDanych, dialog.FileName);
            Komunikat = $"Wyeksportowano rekonstrukcję „{rekonstrukcja.Nazwa}”: {przedmioty} przedmiotów, {repliki} replik, " +
                $"{zestawy} zestawów, {wyprawy} wypraw, {zadania} zadań, {zdjecia} zdjęć.";
        }
        catch (Exception ex)
        {
            Komunikat = "Błąd eksportu: " + ex.Message;
        }
    }

    /// <summary>Import .kwreko: najpierw walidacja i potwierdzenie, potem kopia + transakcja.</summary>
    [RelayCommand]
    private void ImportujRekonstrukcje()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "Wybierz eksport rekonstrukcji (.kwreko)",
                Filter = "Eksport rekonstrukcji Kwatermistrz (*.kwreko)|*.kwreko|Wszystkie pliki (*.*)|*.*",
                CheckFileExists = true,
            };
            if (dialog.ShowDialog() != true)
                return;
            var dane = EksportRekonstrukcji.Wczytaj(dialog.FileName);
            if (dane.SchemaVersion > _dane.SchemaVersion)
            {
                Komunikat = "Ten plik pochodzi z nowszej wersji bazy. Zaktualizuj aplikację przed importem.";
                return;
            }
            var decyzja = System.Windows.MessageBox.Show(
                $"Import obejmuje {dane.Przedmioty.Count} przedmiotów, {dane.Repliki.Count} replik, " +
                $"{dane.Zestawy.Count} zestawów i {dane.Wyprawy.Count} wypraw.\n" +
                "Dopasowane identyfikatory zostaną zachowane; istniejące historyczne liczniki " +
                "i dzień zerowy nie zostaną nadpisane.\n" +
                "Istniejące zdjęcia o tych samych nazwach nie będą nadpisywane.\n\nKontynuować?",
                "Import rekonstrukcji — potwierdzenie",
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (decyzja != System.Windows.MessageBoxResult.Yes)
                return;

            _magazyn.UtworzKopieZapasowa(_dane);
            var (wynik, liczbaZdjec) = TransakcjaDanych.Wykonaj(_dane, robocza =>
            {
                var pakiet = PakietKonfiguracji.Eksportuj(dane, "Z eksportu rekonstrukcji");
                var podglad = ImporterPakietuKonfiguracji.PrzygotujPodglad(robocza, pakiet);
                var mapy = ImporterPakietuKonfiguracji.Zastosuj(robocza, podglad);
                var scalanie = ScalaczRekordow.Scal(robocza, dane, mapy);
                robocza.Ustawienia.DzienZerowy ??= dane.Ustawienia.DzienZerowy;
                foreach (var (rok, liczba) in dane.Ustawienia.HistoryczneWyprawyWgRok)
                    robocza.Ustawienia.HistoryczneWyprawyWgRok.TryAdd(rok, liczba);
                robocza.Ustawienia.DataPierwszejWyprawy ??= dane.Ustawienia.DataPierwszejWyprawy;
                // W przypadku błędu kopiowania zdjęć transakcja danych nie zostaje zatwierdzona.
                var zdjecia = EksportRekonstrukcji.WyodrebnijZdjecia(dialog.FileName, _magazyn.KatalogDanych);
                return (scalanie, zdjecia);
            });
            _magazyn.ZapiszDebounce(_dane);
            _magazyn.Zapisz(_dane);
            OdswiezWszystkieEkrany();
            var pominieteInfo = wynik.Pominiete > 0 ? $" (pominięto {wynik.Pominiete} już istniejących)" : "";
            Komunikat = $"Zaimportowano rekonstrukcję: {_dane.Przedmioty.Count} przedmiotów (łącznie), " +
                $"{_dane.Repliki.Count} replik, {_dane.Zestawy.Count} zestawów, " +
                $"{_dane.Wyprawy.Count} wypraw, {liczbaZdjec} nowych zdjęć{pominieteInfo}.";
        }
        catch (Exception ex)
        {
            Komunikat = "Błąd importu. Poprzedni stan zachowano w kopii zapasowej: " + ex.Message;
        }
    }

    /// <summary>ViewModel pulpitu (FR-101, FR-102).</summary>
    public PulpitViewModel Pulpit { get; }

    /// <summary>ViewModel słowników (FR-1001).</summary>
    public SlownikiViewModel Slowniki { get; }

    /// <summary>ViewModel rekonstrukcji (FR-1301).</summary>
    public RekonstrukcjeViewModel Rekonstrukcje { get; }

    /// <summary>ViewModel ustawień (FR-1006, FR-1107).</summary>
    public UstawieniaViewModel Ustawienia { get; }

    /// <summary>ViewModel importu (FR-1101, FR-1102).</summary>
    public ImportViewModel Import { get; }

    /// <summary>ViewModel pakietów konfiguracji (FR-1404).</summary>
    public PakietyViewModel Pakiety { get; }

    /// <summary>ViewModel wyposażenia (FR-201–216).</summary>
    public WyposazenieViewModel Wyposazenie { get; }

    /// <summary>ViewModel kategorii (FR-202).</summary>
    public KategorieViewModel Kategorie { get; }

    /// <summary>ViewModel replik (FR-301–305).</summary>
    public ReplikiViewModel Repliki { get; }

    /// <summary>ViewModel zestawów (FR-701–709).</summary>
    public ZestawyViewModel Zestawy { get; }

    /// <summary>ViewModel wypraw (FR-801–809).</summary>
    public WyprawyViewModel Wyprawy { get; }

    /// <summary>ViewModel raportów (FR-901–911).</summary>
    public RaportyViewModel Raporty { get; }

    /// <summary>ViewModel „Do uzupełnienia” (FR-1204).</summary>
    public DoUzupelnieniaViewModel DoUzupelnienia { get; }

    /// <summary>ViewModel globalnego wyszukiwania (FR-1201).</summary>
    public SzukanieViewModel Szukanie { get; }

    /// <summary>Wszystkie rekonstrukcje do przełącznika (nowa instancja przy każdym odczycie —
    /// ItemsSource się odświeża po OnPropertyChanged).</summary>
    public IReadOnlyList<Rekonstrukcja> RekonstrukcjeLista => _dane.Rekonstrukcje.ToList();

    /// <summary>Aktywna rekonstrukcja (null = „Wszystkie”).</summary>
    public string? AktywnaRekonstrukcjaId
    {
        get => _stan.AktywnaRekonstrukcjaId;
        set
        {
            if (_stan.AktywnaRekonstrukcjaId == value)
                return;
            _stan.AktywnaRekonstrukcjaId = value;
            _dane.Ustawienia.OstatniaRekonstrukcjaId = value;
            _magazyn.ZapiszDebounce(_dane);
            OnPropertyChanged(nameof(AktywnaRekonstrukcjaId));
            OnPropertyChanged(nameof(AktywnaRekonstrukcjaNazwa));
            Pulpit.Odswiez();
            Wyposazenie.Odswiez();
            Repliki.Odswiez();
            Zestawy.Odswiez();
            Wyprawy.Odswiez();
            Raporty.Odswiez();
            DoUzupelnienia.Odswiez();
        }
    }

    /// <summary>Nazwa aktywnej rekonstrukcji lub „Wszystkie”.</summary>
    public string AktywnaRekonstrukcjaNazwa =>
        AktywnaRekonstrukcjaId == null
            ? "Wszystkie"
            : _dane.Rekonstrukcje.FirstOrDefault(r => r.Id == AktywnaRekonstrukcjaId)?.Nazwa ?? "Wszystkie";

    /// <summary>Aktualny ekran.</summary>
    public string AktualnyEkran
    {
        get => _stan.AktualnyEkran;
        set
        {
            if (_stan.AktualnyEkran == value)
                return;
            _stan.AktualnyEkran = value;
            _dane.Ustawienia.OstatniEkran = value;
            _magazyn.ZapiszDebounce(_dane);
            OnPropertyChanged();
        }
    }

    /// <summary>Tekst wyszukiwania globalnego.</summary>
    public string SzukajTekst
    {
        get => _stan.SzukajTekst;
        set => _stan.SzukajTekst = value;
    }

    /// <summary>Nawigacja do ekranu.</summary>
    [RelayCommand]
    private void Nawiguj(string ekran) => AktualnyEkran = ekran;

    /// <summary>Przycisk „Dodaj wyprawę” — na razie przenosi do ekranu Wyprawy (FR-103).</summary>
    [RelayCommand]
    private void DodajWyprawe() => AktualnyEkran = "Wyprawy";

    /// <summary>Skrót Ctrl+F — fokus na polu wyszukiwania (obsługuje widok, bo pole jest w szablonie okna).</summary>
    [RelayCommand]
    private void FokusujSzukanie() => FokusujSzukanieWidok?.Invoke();

    /// <summary>Zdarzenie fokusowania pola wyszukiwania — podpina widok.</summary>
    public event Action? FokusujSzukanieWidok;
}
