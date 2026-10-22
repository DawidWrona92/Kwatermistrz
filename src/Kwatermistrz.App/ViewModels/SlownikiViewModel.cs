using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.App.ViewModels;

/// <summary>ViewModel ekranu słowników (FR-1001): edycja wartości słowników startowych.</summary>
public partial class SlownikiViewModel : ObservableObject
{
    private readonly DaneAplikacji _dane;
    private readonly MagazynJson _magazyn;

    public SlownikiViewModel(DaneAplikacji dane, MagazynJson magazyn)
    {
        _dane = dane;
        _magazyn = magazyn;
    }

    /// <summary>Wybrany słownik (klucz).</summary>
    [ObservableProperty]
    private string _wybranySlownik = "Strony";

    /// <summary>Nazwy wszystkich słowników do wyboru.</summary>
    public static IReadOnlyList<string> Slowniki => new[]
    {
        "Strony", "Miejsca", "Role", "TypyMagazynkow", "Napedy", "StanyReplik", "Pochodzenie", "RodzajeKosztow", "RodzajeZadan",
    };

    /// <summary>Wartości wybranego słownika.</summary>
    public IReadOnlyList<WartoscSlownika> Wartosci => Wybranego() switch
    {
        "Strony" => _dane.Slowniki.Strony,
        "Miejsca" => _dane.Slowniki.Miejsca,
        "Role" => _dane.Slowniki.Role,
        "TypyMagazynkow" => _dane.Slowniki.TypyMagazynkow,
        "Napedy" => _dane.Slowniki.Napedy,
        "StanyReplik" => _dane.Slowniki.StanyReplik,
        "Pochodzenie" => _dane.Slowniki.Pochodzenie,
        "RodzajeKosztow" => _dane.Slowniki.RodzajeKosztow,
        "RodzajeZadan" => _dane.Slowniki.RodzajeZadan,
        _ => _dane.Slowniki.Strony,
    };

    /// <summary>Nowa wartość do dodania.</summary>
    [ObservableProperty]
    private string _nowaNazwa = "";

    /// <summary>Wybrana wartość do edycji.</summary>
    [ObservableProperty]
    private WartoscSlownika? _wybranaWartosc;

    private string Wybranego() => WybranySlownik;

    partial void OnWybranySlownikChanged(string value)
    {
        OnPropertyChanged(nameof(Wartosci));
    }

    /// <summary>Dodaje wartość do wybranego słownika (FR-1001).</summary>
    [RelayCommand]
    private void Dodaj()
    {
        var nazwa = Normalizacja.Przytnij(NowaNazwa);
        if (nazwa == null)
            return;
        var lista = Wartosci.ToList();
        if (lista.Any(w => Normalizacja.TekstDoPorownania(w.Nazwa) == Normalizacja.TekstDoPorownania(nazwa)))
            return;
        var wartosc = new WartoscSlownika { Nazwa = nazwa };
        // słowniki z semantyką: nowa wartość dostaje semantykę „Inne”/„Do zrobienia” jak pozostałe niestandardowe
        DodajDoSlownika(wartosc);
        NowaNazwa = "";
        Zapisz();
        OnPropertyChanged(nameof(Wartosci));
    }

    /// <summary>Zmienia nazwę wybranej wartości (FR-1002: aktualizuje wszędzie, bo wszędzie są Id).</summary>
    [RelayCommand]
    private void ZapiszWartosc(WartoscSlownika? wartosc)
    {
        if (wartosc == null)
            return;
        var nazwa = Normalizacja.Przytnij(NowaNazwa);
        if (nazwa != null)
            wartosc.Nazwa = nazwa;
        Zapisz();
        OnPropertyChanged(nameof(Wartosci));
    }

    /// <summary>Usuwa wartość, gdy nie jest używana (FR-1002).</summary>
    [RelayCommand]
    private void Usun(WartoscSlownika? wartosc)
    {
        if (wartosc == null || CzyUzywana(wartosc))
            return;
        UsunZeSlownika(wartosc);
        Zapisz();
        OnPropertyChanged(nameof(Wartosci));
    }

    private bool CzyUzywana(WartoscSlownika wartosc)
    {
        var s = _dane.Slowniki;
        return _dane.Przedmioty.Any(p => p.StronyId.Contains(wartosc.Id) || p.MiejsceId == wartosc.Id || p.PochodzenieId == wartosc.Id || p.WzorId == wartosc.Id)
            || _dane.Repliki.Any(r => r.StronyId.Contains(wartosc.Id) || r.RoleId.Contains(wartosc.Id) || r.TypMagazynkaId == wartosc.Id || r.NapedId == wartosc.Id || r.StanId == wartosc.Id)
            || _dane.Zestawy.Any(z => z.StronaId == wartosc.Id)
            || _dane.Rekonstrukcje.Any(r => r.StronaId == wartosc.Id)
            || _dane.Zadania.Any(z => z.RodzajId == wartosc.Id)
            || _dane.Przedmioty.SelectMany(p => p.Koszty).Concat(_dane.Repliki.SelectMany(r => r.Koszty)).Any(k => k.RodzajId == wartosc.Id);
    }

    private void DodajDoSlownika(WartoscSlownika wartosc)
    {
        switch (Wybranego())
        {
            case "Strony": _dane.Slowniki.Strony.Add(wartosc); break;
            case "Miejsca": _dane.Slowniki.Miejsca.Add(wartosc); break;
            case "Role": _dane.Slowniki.Role.Add(wartosc); break;
            case "TypyMagazynkow": _dane.Slowniki.TypyMagazynkow.Add(wartosc); break;
            case "Napedy": _dane.Slowniki.Napedy.Add(wartosc); break;
            case "StanyReplik": _dane.Slowniki.StanyReplik.Add(wartosc); break;
            case "Pochodzenie": _dane.Slowniki.Pochodzenie.Add(wartosc); break;
            case "RodzajeKosztow": _dane.Slowniki.RodzajeKosztow.Add(wartosc); break;
            case "RodzajeZadan": _dane.Slowniki.RodzajeZadan.Add(wartosc); break;
        }
    }

    private void UsunZeSlownika(WartoscSlownika wartosc)
    {
        switch (Wybranego())
        {
            case "Strony": _dane.Slowniki.Strony.Remove(wartosc); break;
            case "Miejsca": _dane.Slowniki.Miejsca.Remove(wartosc); break;
            case "Role": _dane.Slowniki.Role.Remove(wartosc); break;
            case "TypyMagazynkow": _dane.Slowniki.TypyMagazynkow.Remove(wartosc); break;
            case "Napedy": _dane.Slowniki.Napedy.Remove(wartosc); break;
            case "StanyReplik": _dane.Slowniki.StanyReplik.Remove(wartosc); break;
            case "Pochodzenie": _dane.Slowniki.Pochodzenie.Remove(wartosc); break;
            case "RodzajeKosztow": _dane.Slowniki.RodzajeKosztow.Remove(wartosc); break;
            case "RodzajeZadan": _dane.Slowniki.RodzajeZadan.Remove(wartosc); break;
        }
    }

    private void Zapisz() => _magazyn.Zapisz(_dane);
}
