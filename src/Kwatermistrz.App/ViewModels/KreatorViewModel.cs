using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwatermistrz.Core.Import;
using Kwatermistrz.Core.Konfiguracja;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Storage;
using Microsoft.Win32;

namespace Kwatermistrz.App.ViewModels;

/// <summary>ViewModel kreatora pierwszego uruchomienia (FR-1401): wariant pusty, szablon konfiguracji (FR-1406) lub import z arkusza (FR-1101).</summary>
public partial class KreatorViewModel : ObservableObject
{
    private readonly DaneAplikacji _dane;
    private readonly MagazynJson _magazyn;

    public KreatorViewModel(DaneAplikacji dane, MagazynJson magazyn)
    {
        _dane = dane;
        _magazyn = magazyn;
    }

    /// <summary>Zakończenie kreatora — okno zamyka się z wynikiem pozytywnym.</summary>
    public event EventHandler? Zakonczono;

    /// <summary>Nazwa pierwszej rekonstrukcji.</summary>
    [ObservableProperty]
    private string _nazwaRekonstrukcji = "";

    /// <summary>Wybrana strona (Id wartości słownika); null = brak wyboru.</summary>
    [ObservableProperty]
    private string? _wybranaStronaId;

    /// <summary>Strony do wyboru.</summary>
    public IReadOnlyList<WartoscSlownika> Strony => _dane.Slowniki.Strony;

    /// <summary>Wybrany punkt startu (FR-1401): „pusta”, „przyklad”, „import”.</summary>
    [ObservableProperty]
    private string _wybranyStart = SzablonyKonfiguracji.Pusta;

    /// <summary>Lista punktów startu: klucz → opis.</summary>
    public static IReadOnlyList<(string Klucz, string Nazwa)> Starty { get; } = SzablonyKonfiguracji.Lista
        .Concat(new[] { ("import", "Import z arkusza Excel (Dane_zrodlowe_Kwatermistrz.xlsx)") })
        .ToList();

    /// <summary>Ścieżka pliku do importu (wariant „import”).</summary>
    [ObservableProperty]
    private string _sciezkaImportu = "";

    /// <summary>Komunikat błędu/ostrzeżenia kreatora.</summary>
    [ObservableProperty]
    private string _komunikat = "";

    /// <summary>Wybiera plik xlsx do importu (wariant „import”, FR-1101).</summary>
    [RelayCommand]
    private void WybierzPlikImportu()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Arkusz Excel (*.xlsx)|*.xlsx|Wszystkie pliki (*.*)|*.*",
            Title = "Wybierz plik Dane_zrodlowe_Kwatermistrz.xlsx",
        };
        if (dialog.ShowDialog() == true)
            SciezkaImportu = dialog.FileName;
    }

    /// <summary>Potwierdza kreator: tworzy pierwszą rekonstrukcję (i opcjonalnie importuje dane) oraz zamyka okno (FR-1401).</summary>
    [RelayCommand]
    private void Potwierdz()
    {
        var nazwa = Normalizacja.Przytnij(NazwaRekonstrukcji);
        if (nazwa == null && WybranyStart != "import")
            return;
        try
        {
            if (WybranyStart == "import")
            {
                // FR-1101: import początkowy z podglądem; do zatwierdzenia nic nie jest zapisywane (FR-1102)
                if (string.IsNullOrWhiteSpace(SciezkaImportu) || !File.Exists(SciezkaImportu))
                {
                    Komunikat = "Wskaż plik .xlsx do importu.";
                    return;
                }
                var podglad = ImporterKwatermistrz.PrzygotujPodglad(SciezkaImportu);
                _magazyn.UtworzKopieZapasowa(); // FR-1107: przed importem kopia automatyczna
                ImporterKwatermistrz.Zastosuj(podglad, _dane);
            }
            else
            {
                var (slowniki, szablonRekonstrukcji, szablonyKart) = SzablonyKonfiguracji.Buduj(WybranyStart);
                _dane.Slowniki = slowniki;
                _dane.SzablonyKart.AddRange(szablonyKart);
                var rekonstrukcja = szablonRekonstrukcji;
                if (nazwa != null)
                    rekonstrukcja.Nazwa = nazwa;
                if (WybranaStronaId != null)
                    rekonstrukcja.StronaId = WybranaStronaId;
                if (rekonstrukcja.SzablonSlotow.Count == 0)
                    rekonstrukcja.SzablonSlotow = StartoweSlowniki.StartoweSloty();
                _dane.Rekonstrukcje.Add(rekonstrukcja);
                _dane.Ustawienia.OstatniaRekonstrukcjaId = rekonstrukcja.Id;
            }
            _dane.CzyPierwszeUruchomienie = false;
            _magazyn.Zapisz(_dane);
            Zakonczono?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Komunikat = "Błąd: " + ex.Message;
        }
    }
}
