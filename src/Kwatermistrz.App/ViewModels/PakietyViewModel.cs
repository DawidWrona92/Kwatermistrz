using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwatermistrz.Core.Konfiguracja;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Storage;
using Microsoft.Win32;

namespace Kwatermistrz.App.ViewModels;

/// <summary>Wiersz decyzji w podglądzie importu pakietu (FR-1404, FR-1003).</summary>
public partial class WierszDecyzji : ObservableObject
{
    public WierszDecyzji(string rodzaj, string nazwa, string? opis, object decyzja)
    {
        Rodzaj = rodzaj;
        Nazwa = nazwa;
        Opis = opis ?? "";
        Decyzja = decyzja;
    }

    public string Rodzaj { get; }
    public string Nazwa { get; }
    public string Opis { get; }
    /// <summary>Obiekt decyzji z Core (DecyzjaWartosc&lt;...&gt;, DecyzjaRekonstrukcji lub DecyzjaSzablonuKarty).</summary>
    public object Decyzja { get; }

    [ObservableProperty]
    private string _akcja = "";

    [ObservableProperty]
    private string _istniejaca = "";

    /// <summary>Indeks wybranej akcji: 0 = dodaj nową, 1 = scal z istniejącą / scal szablony, 2 = pomiń.</summary>
    [ObservableProperty]
    private int _indeksAkcji;

    /// <summary>Czy wiersz reprezentuje wartość, którą można scalić (ma dopasowanie lokalne).</summary>
    [ObservableProperty]
    private bool _czyMoznaScalac;
}

/// <summary>ViewModel ekranu pakietów konfiguracji (FR-1404): eksport konfiguracji bez danych o sprzęcie, import z podglądem i scalaniem słowników (FR-1003).</summary>
public partial class PakietyViewModel : ObservableObject
{
    private readonly DaneAplikacji _dane;
    private readonly MagazynJson _magazyn;
    private PodgladPakietuKonfiguracji? _podglad;

    public PakietyViewModel(DaneAplikacji dane, MagazynJson magazyn)
    {
        _dane = dane;
        _magazyn = magazyn;
    }

    [ObservableProperty]
    private string _nazwaPakietu = "";

    [ObservableProperty]
    private string _sciezkaPakietu = "";

    [ObservableProperty]
    private string _infoPakietu = "";

    public System.Collections.ObjectModel.ObservableCollection<WierszDecyzji> Wiersze { get; } = new();

    [ObservableProperty]
    private string _komunikat = "";

    /// <summary>Eksportuje bieżącą konfigurację (słowniki, wzory, kategorie, rekonstrukcje, szablony kart) do pliku .kwpakiet (FR-1404). Bez przedmiotów, replik, wypraw, zdjęć i cen.</summary>
    [RelayCommand]
    private void Eksportuj()
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Pakiet konfiguracji Kwatermistrz (*.kwpakiet)|*.kwpakiet",
                FileName = string.IsNullOrWhiteSpace(NazwaPakietu) ? "konfiguracja.kwpakiet" : NazwaPakietu + ".kwpakiet",
                DefaultExt = ".kwpakiet",
            };
            if (dialog.ShowDialog() != true)
                return;
            var pakiet = PakietKonfiguracji.Eksportuj(_dane, string.IsNullOrWhiteSpace(NazwaPakietu) ? null : NazwaPakietu);
            PakietKonfiguracji.ZapiszDoPliku(pakiet, dialog.FileName);
            Komunikat = $"Wyeksportowano: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            Komunikat = "Błąd eksportu: " + ex.Message;
        }
    }

    /// <summary>Wczytuje pakiet i pokazuje podgląd z decyzjami (FR-1404, FR-1102).</summary>
    [RelayCommand]
    private void WczytajPakiet()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Pakiet konfiguracji Kwatermistrz (*.kwpakiet)|*.kwpakiet|Wszystkie pliki (*.*)|*.*",
            };
            if (dialog.ShowDialog() != true)
                return;
            var pakiet = PakietKonfiguracji.WczytajZPliku(dialog.FileName);
            _podglad = ImporterPakietuKonfiguracji.PrzygotujPodglad(_dane, pakiet);
            SciezkaPakietu = dialog.FileName;
            NazwaPakietu = pakiet.Nazwa ?? "";
            InfoPakietu = $"Wersja pakietu: {pakiet.WersjaPakietu}, reguły: {pakiet.WersjaRegul}, " +
                $"wartości do dodania: {_podglad.LiczbaWartosciDoDodania}, do scalenia: {_podglad.LiczbaWartosciDoScalenia}, " +
                $"rekonstrukcje: {_podglad.LiczbaRekonstrukcjiDoDodania}, szablony kart: {_podglad.LiczbaSzablonowDoDodania}";
            BudujWiersze();
            Komunikat = "";
        }
        catch (Exception ex)
        {
            Komunikat = "Nie udało się wczytać pakietu: " + ex.Message;
        }
    }

    private void BudujWiersze()
    {
        Wiersze.Clear();
        if (_podglad == null)
            return;

        foreach (var (rodzaj, decyzje) in _podglad.Slowniki)
        {
            foreach (var d in decyzje)
                Wiersze.Add(NowyWiersz(rodzaj.Nazwa, d.NazwaPakietu, OpisSlownika(d), d));
        }
        foreach (var d in _podglad.Wzory)
            Wiersze.Add(NowyWiersz("Wzór", d.NazwaPakietu, "", d));
        foreach (var d in _podglad.Kategorie)
            Wiersze.Add(NowyWiersz("Kategoria", d.NazwaPakietu, d.WartoscPakietu.CzyNosnik ? "nośnik" : "", d));
        foreach (var d in _podglad.Rekonstrukcje)
            Wiersze.Add(NowyWierszRekonstrukcji(d));
        foreach (var d in _podglad.SzablonyKart)
            Wiersze.Add(NowyWierszSzablonu(d));
    }

    private static string OpisSlownika<T>(DecyzjaWartosc<T> d) where T : class
    {
        var opis = d.WartoscPakietu switch
        {
            WartoscSlownika w when w.Semantyka.Length > 0 => "semantyka: " + w.Semantyka,
            WartoscSlownika w when w.Aliasy.Count > 0 => "aliasy: " + string.Join(", ", w.Aliasy),
            _ => "",
        };
        return opis;
    }

    private static WierszDecyzji NowyWiersz<T>(string rodzaj, string nazwa, string opis, DecyzjaWartosc<T> d) where T : class
    {
        var wiersz = new WierszDecyzji(rodzaj, nazwa, opis, d)
        {
            CzyMoznaScalac = d.Istniejaca != null,
            Istniejaca = d.Istniejaca switch
            {
                WartoscSlownika w => w.Nazwa,
                WzorKamuflazu wz => wz.Nazwa,
                Kategoria k => k.Nazwa,
                _ => "",
            },
        };
        UstawIndeksDomyslny(wiersz, d.Akcja);
        return wiersz;
    }

    private static void UstawIndeksDomyslny(WierszDecyzji wiersz, AkcjaWartosci akcja)
    {
        // 0 = dodaj nową, 1 = scal z istniejącą, 2 = pomiń
        wiersz.IndeksAkcji = akcja switch
        {
            AkcjaWartosci.DodajNowa => 0,
            AkcjaWartosci.ScalZIstniejaca => 1,
            _ => 2,
        };
        wiersz.Akcja = akcja switch
        {
            AkcjaWartosci.DodajNowa => "Dodaj nową",
            AkcjaWartosci.ScalZIstniejaca => "Scal z istniejącą",
            _ => "Pomiń",
        };
    }

    private static WierszDecyzji NowyWierszRekonstrukcji(DecyzjaRekonstrukcji d)
    {
        var wiersz = new WierszDecyzji("Rekonstrukcja", d.Pakiet.Nazwa,
            d.NazwaStrony != null ? "strona: " + d.NazwaStrony : "", d)
        {
            CzyMoznaScalac = d.Istniejaca != null,
            Istniejaca = d.Istniejaca?.Nazwa ?? "",
        };
        // 0 = dodaj nową, 1 = scal szablony, 2 = pomiń
        wiersz.IndeksAkcji = d.Akcja switch
        {
            AkcjaRekonstrukcji.DodajNowa => 0,
            AkcjaRekonstrukcji.ScalSzablony => 1,
            _ => 2,
        };
        wiersz.Akcja = d.Akcja switch
        {
            AkcjaRekonstrukcji.DodajNowa => "Dodaj nową",
            AkcjaRekonstrukcji.ScalSzablony => "Scal szablony",
            _ => "Pomiń",
        };
        return wiersz;
    }

    private static WierszDecyzji NowyWierszSzablonu(DecyzjaSzablonuKarty d)
    {
        var wiersz = new WierszDecyzji("Szablon karty", d.Pakiet.Nazwa,
            d.NazwaNapedu.Length > 0 ? "napęd: " + d.NazwaNapedu : "wspólny", d)
        {
            CzyMoznaScalac = false,
            Istniejaca = d.Istniejacy?.Nazwa ?? "",
        };
        // 0 = dodaj nowy, 1 = pomiń
        wiersz.IndeksAkcji = d.Akcja == AkcjaSzablonuKarty.DodajNowy ? 0 : 1;
        wiersz.Akcja = d.Akcja == AkcjaSzablonuKarty.DodajNowy ? "Dodaj nowy" : "Pomiń";
        return wiersz;
    }

    /// <summary>Zmiana decyzji w wierszu podglądu (indeks 0/1/2 zależy od rodzaju).</summary>
    [RelayCommand]
    private static void ZmienDecyzje(WierszDecyzji wiersz)
    {
        if (wiersz == null)
            return;
        switch (wiersz.Decyzja)
        {
            case DecyzjaRekonstrukcji dr:
                dr.Akcja = wiersz.IndeksAkcji switch
                {
                    0 => AkcjaRekonstrukcji.DodajNowa,
                    1 => AkcjaRekonstrukcji.ScalSzablony,
                    _ => AkcjaRekonstrukcji.Pominij,
                };
                break;
            case DecyzjaSzablonuKarty ds:
                ds.Akcja = wiersz.IndeksAkcji == 0 ? AkcjaSzablonuKarty.DodajNowy : AkcjaSzablonuKarty.Pominij;
                break;
            case DecyzjaWartosc<WartoscSlownika> d:
                d.Akcja = IndeksDoAkcji(wiersz.IndeksAkcji);
                break;
            case DecyzjaWartosc<WzorKamuflazu> d:
                d.Akcja = IndeksDoAkcji(wiersz.IndeksAkcji);
                break;
            case DecyzjaWartosc<Kategoria> d:
                d.Akcja = IndeksDoAkcji(wiersz.IndeksAkcji);
                break;
        }
    }

    private static AkcjaWartosci IndeksDoAkcji(int indeks) => indeks switch
    {
        0 => AkcjaWartosci.DodajNowa,
        1 => AkcjaWartosci.ScalZIstniejaca,
        _ => AkcjaWartosci.Pominij,
    };

    /// <summary>Zastosowuje zatwierdzone decyzje: kopia zapasowa, import, zapis (FR-1107, FR-1404).</summary>
    [RelayCommand]
    private void Zastosuj()
    {
        if (_podglad == null)
        {
            Komunikat = "Najpierw wczytaj pakiet.";
            return;
        }
        try
        {
            _magazyn.Zapisz(_dane);
            _magazyn.UtworzKopieZapasowa();
            ImporterPakietuKonfiguracji.Zastosuj(_dane, _podglad);
            _magazyn.Zapisz(_dane);
            Komunikat = $"Zaimportowano: {_podglad.LiczbaWartosciDoDodania} nowych wartości, " +
                $"{_podglad.LiczbaWartosciDoScalenia} scalonych, {_podglad.LiczbaRekonstrukcjiDoDodania} rekonstrukcji, " +
                $"{_podglad.LiczbaSzablonowDoDodania} szablonów kart.";
            _podglad = null;
            Wiersze.Clear();
            SciezkaPakietu = "";
            InfoPakietu = "";
        }
        catch (Exception ex)
        {
            Komunikat = "Błąd importu: " + ex.Message;
        }
    }
}
