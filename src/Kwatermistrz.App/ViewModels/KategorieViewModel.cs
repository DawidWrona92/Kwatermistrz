using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.App.ViewModels;

/// <summary>ViewModel zarządzania kategoriami (FR-202): dodawanie, zmiana nazwy, usuwanie tylko pustych, oznaczenie „nośnik”.</summary>
public partial class KategorieViewModel : ObservableObject
{
    private readonly DaneAplikacji _dane;
    private readonly MagazynJson _magazyn;

    public KategorieViewModel(DaneAplikacji dane, MagazynJson magazyn)
    {
        _dane = dane;
        _magazyn = magazyn;
    }

    /// <summary>Kategorie w kolejności (FR-201).</summary>
    public IReadOnlyList<Kategoria> Kategorie => _dane.Slowniki.Kategorie.OrderBy(k => k.Kolejnosc).ToList();

    /// <summary>Nowa nazwa kategorii.</summary>
    [ObservableProperty]
    private string _nowaNazwa = "";

    /// <summary>Czy nowa kategoria jest nośnikiem (FR-202, FR-207).</summary>
    [ObservableProperty]
    private bool _nowaCzyNosnik;

    /// <summary>Dodaje kategorię (FR-202).</summary>
    [RelayCommand]
    private void Dodaj()
    {
        var nazwa = Normalizacja.Przytnij(NowaNazwa);
        if (nazwa == null)
            return;
        if (_dane.Slowniki.Kategorie.Any(k => Normalizacja.TekstDoPorownania(k.Nazwa) == Normalizacja.TekstDoPorownania(nazwa)))
            return;
        _dane.Slowniki.Kategorie.Add(new Kategoria
        {
            Nazwa = nazwa,
            CzyNosnik = NowaCzyNosnik,
            Kolejnosc = _dane.Slowniki.Kategorie.Count,
        });
        NowaNazwa = "";
        NowaCzyNosnik = false;
        _magazyn.ZapiszDebounce(_dane);
        OnPropertyChanged(nameof(Kategorie));
    }

    /// <summary>Zmienia nazwę kategorii (FR-1002 — wszędzie są Id, więc nazwa aktualizuje się sama).</summary>
    [RelayCommand]
    private void ZapiszNazwe(Kategoria? kategoria)
    {
        if (kategoria == null)
            return;
        _magazyn.ZapiszDebounce(_dane);
        OnPropertyChanged(nameof(Kategorie));
    }

    /// <summary>Usuwa kategorię tylko gdy pusta (FR-202).</summary>
    [RelayCommand]
    private void Usun(Kategoria? kategoria)
    {
        if (kategoria == null)
            return;
        if (_dane.Przedmioty.Any(p => p.KategoriaId == kategoria.Id))
            return;
        _dane.Slowniki.Kategorie.Remove(kategoria);
        _magazyn.ZapiszDebounce(_dane);
        OnPropertyChanged(nameof(Kategorie));
    }

    /// <summary>Zapisuje zmianę „nośnik” (FR-202).</summary>
    [RelayCommand]
    private void ZapiszNosnik(Kategoria? kategoria)
    {
        if (kategoria == null)
            return;
        _magazyn.ZapiszDebounce(_dane);
    }

    /// <summary>Liczba przedmiotów w kategorii (FR-201).</summary>
    public int LiczbaPrzedmiotow(string kategoriaId) => _dane.Przedmioty.Count(p => p.KategoriaId == kategoriaId);
}
