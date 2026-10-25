using System.IO;
using System.Reflection;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Storage;
using Microsoft.Win32;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Kwatermistrz.App.ViewModels;

/// <summary>ViewModel ekranu ustawień (FR-1006): motyw, folder danych, próg „bez testu”; kopia zapasowa i przywracanie (FR-1107).</summary>
public partial class UstawieniaViewModel : ObservableObject
{
    private readonly DaneAplikacji _dane;
    private readonly MagazynJson _magazyn;

    public UstawieniaViewModel(DaneAplikacji dane, MagazynJson magazyn)
    {
        _dane = dane;
        _magazyn = magazyn;
    }

    /// <summary>Wybrany motyw (FR-1006): 0 = systemowy, 1 = jasny, 2 = ciemny.</summary>
    public int IndeksMotywu
    {
        get => (int)_dane.Ustawienia.Motyw;
        set
        {
            if (value < 0 || value > 2)
                return;
            _dane.Ustawienia.Motyw = (Motyw)value;
            ZastosujMotyw();
            OnPropertyChanged();
            Zapisz();
        }
    }

    /// <summary>Próg „brak testu” w dniach (FR-104).</summary>
    public int DniBezTestu
    {
        get => _dane.Ustawienia.DniBezTestu;
        set
        {
            if (value < 1)
                return;
            _dane.Ustawienia.DniBezTestu = value;
            OnPropertyChanged();
            Zapisz();
        }
    }

    /// <summary>NFR-11: maksymalny dłuższy bok zdjęcia przy zapisie (0 = bez zmniejszania).</summary>
    public int MaksymalnyBokZdjecia
    {
        get => _dane.Ustawienia.MaksymalnyBokZdjecia;
        set
        {
            if (value < 0)
                return;
            _dane.Ustawienia.MaksymalnyBokZdjecia = value;
            OnPropertyChanged();
            Zapisz();
        }
    }

    /// <summary>NFR-11: zachowanie oryginału obok zmniejszonego zdjęcia.</summary>
    public bool ZachowajOryginalZdjecia
    {
        get => _dane.Ustawienia.ZachowajOryginalZdjecia;
        set
        {
            _dane.Ustawienia.ZachowajOryginalZdjecia = value;
            OnPropertyChanged();
            Zapisz();
        }
    }

    /// <summary>Ścieżka katalogu danych.</summary>
    public string KatalogDanych => _magazyn.KatalogDanych;

    /// <summary>Wersja reguł obliczeń (NFR-14) — tylko do odczytu.</summary>
    public string WersjaRegul => _dane.WersjaRegul;

    /// <summary>Wersja aplikacji z metadanych zestawu (NFR-14, widoczna w O programie/Ustawieniach).</summary>
    public static string WersjaAplikacji { get; } =
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        is { } wersja && wersja.Contains('+')
            ? wersja.Substring(0, wersja.IndexOf('+'))
            : "dev";

    /// <summary>Komunikat po operacji na kopii (FR-1107).</summary>
    [ObservableProperty]
    private string _komunikat = "";

    private void ZastosujMotyw()
    {
        switch (_dane.Ustawienia.Motyw)
        {
            case Motyw.Jasny:
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                break;
            case Motyw.Ciemny:
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                break;
            default:
                ApplicationThemeManager.ApplySystemTheme();
                break;
        }
        // aliasy kolorów wykresów (FR-909) podążają za motywem — „Systemowy” wg faktycznego motywu systemu
        var jasny = ApplicationThemeManager.GetSystemTheme() == SystemTheme.Light;
        App.UstawAliasyWykresow(jasny);
    }

    /// <summary>Kopia zapasowa jednym poleceniem (FR-1107).</summary>
    [RelayCommand]
    private void UtworzKopie()
    {
        _magazyn.Zapisz(_dane);
        var katalog = _magazyn.UtworzKopieZapasowa(_dane);
        Komunikat = $"Kopia zapasowa zapisana: {katalog}";
    }

    /// <summary>Przywraca dane z wybranego pliku kopii (FR-1107): przed nadpisaniem wykonuje kopię
    /// bieżących danych, kopiuje też zdjęcia z katalogu kopii i prosi o odświeżenie wszystkich ekranów.</summary>
    [RelayCommand]
    private void Przywroc()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Wybierz plik danych do przywrócenia (dane.json)",
            Filter = "Dane Kwatermistrza (dane.json)|dane.json|Wszystkie pliki (*.*)|*.*",
            CheckFileExists = true,
        };
        if (dialog.ShowDialog() != true)
            return;
        try
        {
            var dane = MagazynJson.WczytajZPliku(dialog.FileName);
            // przed nadpisaniem: kopia bieżących danych (FR-1107)
            _magazyn.Zapisz(_dane);
            _magazyn.UtworzKopieZapasowa(_dane);
            _dane.Slowniki = dane.Slowniki;
            _dane.Przedmioty = dane.Przedmioty;
            _dane.Repliki = dane.Repliki;
            _dane.Zestawy = dane.Zestawy;
            _dane.Wyprawy = dane.Wyprawy;
            _dane.Rekonstrukcje = dane.Rekonstrukcje;
            _dane.SzablonyKart = dane.SzablonyKart;
            _dane.Zadania = dane.Zadania;
            _dane.Ustawienia = dane.Ustawienia;
            _dane.CzyPierwszeUruchomienie = dane.CzyPierwszeUruchomienie;
            // zdjęcia z katalogu kopii (obok wybranego dane.json) — nadpisują lokalne
            var katalogKopii = Path.GetDirectoryName(dialog.FileName);
            if (katalogKopii != null && Directory.Exists(Path.Combine(katalogKopii, "zdjecia")))
            {
                foreach (var plik in Directory.EnumerateFiles(Path.Combine(katalogKopii, "zdjecia"), "*", SearchOption.AllDirectories))
                {
                    var wzgledna = Path.GetRelativePath(Path.Combine(katalogKopii, "zdjecia"), plik);
                    var docelowy = Path.Combine(_magazyn.KatalogDanych, "zdjecia", wzgledna);
                    Directory.CreateDirectory(Path.GetDirectoryName(docelowy)!);
                    File.Copy(plik, docelowy, overwrite: true);
                }
            }
            _magazyn.Zapisz(_dane);
            ZastosujMotyw();
            Komunikat = "Przywrócono dane z kopii. Odśwież ekrany (F5 lub przełącz ponownie), aby zobaczyć przywrócone dane.";
            OnPropertyChanged(nameof(IndeksMotywu));
            OnPropertyChanged(nameof(DniBezTestu));
            DanePrzywrocone?.Invoke();
        }
        catch (Exception ex)
        {
            Komunikat = $"Nie udało się przywrócić: {ex.Message}";
        }
    }

    /// <summary>Wywoływane po przywróceniu danych z kopii — pozostałe ViewModele odświeżają listy.</summary>
    public event Action? DanePrzywrocone;

    private void Zapisz() => _magazyn.Zapisz(_dane);
}
