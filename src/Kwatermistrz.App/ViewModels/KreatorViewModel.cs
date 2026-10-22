using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.App.ViewModels;

/// <summary>ViewModel kreatora pierwszego uruchomienia (FR-1401). W tym etapie dostępny tylko wariant „pusta aplikacja”.</summary>
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

    /// <summary>Potwierdza kreator: tworzy pierwszą rekonstrukcję i zamyka okno (FR-1401).</summary>
    [RelayCommand]
    private void Potwierdz()
    {
        var nazwa = Normalizacja.Przytnij(NazwaRekonstrukcji);
        if (nazwa == null)
            return;
        var rekonstrukcja = new Rekonstrukcja
        {
            Nazwa = nazwa,
            StronaId = WybranaStronaId,
            SzablonSlotow = StartoweSlowniki.StartoweSloty(),
        };
        _dane.Rekonstrukcje.Add(rekonstrukcja);
        _dane.Ustawienia.OstatniaRekonstrukcjaId = rekonstrukcja.Id;
        _dane.CzyPierwszeUruchomienie = false;
        _magazyn.Zapisz(_dane);
        Zakonczono?.Invoke(this, EventArgs.Empty);
    }
}
