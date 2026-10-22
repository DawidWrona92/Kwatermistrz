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
