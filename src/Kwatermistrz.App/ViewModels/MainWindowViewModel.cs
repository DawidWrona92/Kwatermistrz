using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwatermistrz.App.Services;
using Kwatermistrz.Core.Model;

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
        WyposazenieViewModel wyposazenie,
        KategorieViewModel kategorie,
        ReplikiViewModel repliki)
    {
        _dane = dane;
        _stan = stan;
        Pulpit = pulpit;
        Slowniki = slowniki;
        Rekonstrukcje = rekonstrukcje;
        Ustawienia = ustawienia;
        Import = import;
        Wyposazenie = wyposazenie;
        Kategorie = kategorie;
        Repliki = repliki;
        _stan.AktywnaRekonstrukcjaId = dane.Ustawienia.OstatniaRekonstrukcjaId;
        if (!string.IsNullOrEmpty(dane.Ustawienia.OstatniEkran))
            _stan.AktualnyEkran = dane.Ustawienia.OstatniEkran;
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

    /// <summary>ViewModel wyposażenia (FR-201–216).</summary>
    public WyposazenieViewModel Wyposazenie { get; }

    /// <summary>ViewModel kategorii (FR-202).</summary>
    public KategorieViewModel Kategorie { get; }

    /// <summary>ViewModel replik (FR-301–305).</summary>
    public ReplikiViewModel Repliki { get; }

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
