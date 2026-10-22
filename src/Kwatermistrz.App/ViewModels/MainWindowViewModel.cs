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

    /// <summary>Import rekonstrukcji z pliku .kwreko: wyodrębnia dane i zdjęcia, scala konfigurację przez pakiet (FR-1405, FR-1404).</summary>
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
            _magazyn.Zapisz(_dane);
            _magazyn.UtworzKopieZapasowa(); // FR-1107: przed importem kopia automatyczna
            // konfiguracja (słowniki, kategorie, wzory, szablony) przez pakiet z podglądem scalania (FR-1404)
            var pakiet = PakietKonfiguracji.Eksportuj(dane, "Z eksportu rekonstrukcji");
            var podglad = ImporterPakietuKonfiguracji.PrzygotujPodglad(_dane, pakiet);
            ImporterPakietuKonfiguracji.Zastosuj(_dane, podglad);
            // dane rekonstrukcji: rekonstrukcja + przedmioty/repliki/zestawy/wyprawy/zadania
            _dane.Rekonstrukcje.AddRange(dane.Rekonstrukcje);
            _dane.Przedmioty.AddRange(dane.Przedmioty);
            _dane.Repliki.AddRange(dane.Repliki);
            _dane.Zestawy.AddRange(dane.Zestawy);
            _dane.Wyprawy.AddRange(dane.Wyprawy);
            _dane.Zadania.AddRange(dane.Zadania);
            // zdjęcia do katalogu danych
            var liczbaZdjec = EksportRekonstrukcji.WyodrebnijZdjecia(dialog.FileName, _magazyn.KatalogDanych);
            _magazyn.Zapisz(_dane);
            Komunikat = $"Zaimportowano rekonstrukcję: {dane.Przedmioty.Count} przedmiotów, {dane.Repliki.Count} replik, " +
                $"{dane.Zestawy.Count} zestawów, {dane.Wyprawy.Count} wypraw, {liczbaZdjec} zdjęć. Konflikty konfiguracji rozstrzygnij na ekranie Pakiety.";
        }
        catch (Exception ex)
        {
            Komunikat = "Błąd importu: " + ex.Message;
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

    /// <summary>Wszystkie rekonstrukcje do przełącznika.</summary>
    public IReadOnlyList<Rekonstrukcja> RekonstrukcjeLista => _dane.Rekonstrukcje;

    /// <summary>Aktywna rekonstrukcja (null = „Wszystkie”).</summary>
    public string? AktywnaRekonstrukcjaId
    {
        get => _stan.AktywnaRekonstrukcjaId;
        set
        {
            _stan.AktywnaRekonstrukcjaId = value;
            OnPropertyChanged(nameof(AktywnaRekonstrukcjaId));
            OnPropertyChanged(nameof(AktywnaRekonstrukcjaNazwa));
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
            _stan.AktualnyEkran = value;
            OnPropertyChanged();
            _dane.Ustawienia.OstatniEkran = value;
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
