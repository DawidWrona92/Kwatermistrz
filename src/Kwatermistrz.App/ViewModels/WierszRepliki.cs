using CommunityToolkit.Mvvm.ComponentModel;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Obliczenia;

namespace Kwatermistrz.App.ViewModels;

/// <summary>Wiersz listy replik (FR-301): wszystkie kolumny listy + dane do karty.</summary>
public partial class WierszRepliki : ObservableObject
{
    private readonly Replika _replika;
    private readonly WierszReplikiObliczenia _wiersz;

    public WierszRepliki(Replika replika, WierszReplikiObliczenia wiersz)
    {
        _replika = replika;
        _wiersz = wiersz;
    }

    /// <summary>Zaznaczona do porównania kart (FR-406).</summary>
    [ObservableProperty]
    private bool _zaznaczonaDoPorownania;

    /// <summary>Model repliki.</summary>
    public Replika Replika => _replika;

    /// <summary>Id repliki.</summary>
    public string Id => _replika.Id;

    /// <summary>Miejsce w rankingu wg wypraw (FR-301).</summary>
    public int MiejsceWRankingu { get; set; }

    /// <summary>Nazwa w postaci „model (prototyp)” (FR-301).</summary>
    public string NazwaPelna => _replika.Prototyp != null
        ? $"{_replika.ModelProducenta} ({_replika.Prototyp})"
        : _replika.ModelProducenta;

    /// <summary>Model producenta.</summary>
    public string ModelProducenta => _replika.ModelProducenta;

    /// <summary>Prototyp.</summary>
    public string? Prototyp => _replika.Prototyp;

    /// <summary>Strony (Id).</summary>
    public List<string> StronyId => _replika.StronyId;

    /// <summary>Role (Id).</summary>
    public List<string> RoleId => _replika.RoleId;

    /// <summary>Typ magazynka (Id).</summary>
    public string? TypMagazynkaId => _replika.TypMagazynkaId;

    /// <summary>Napęd (Id).</summary>
    public string? NapedId => _replika.NapedId;

    /// <summary>Stan (Id).</summary>
    public string? StanId => _replika.StanId;

    /// <summary>Status własności (FR-305).</summary>
    public StatusWlasnosci Status => _replika.Status;

    /// <summary>Liczba wypraw (BR-02).</summary>
    public int Wyprawy => _wiersz.Wyprawy;

    /// <summary>Procent wypraw (BR-03).</summary>
    public decimal ProcentWypraw => _wiersz.ProcentWypraw;

    /// <summary>Koszt całkowity (BR-04).</summary>
    public decimal KosztCalkowity => _wiersz.KosztCalkowity;

    /// <summary>Cena oryginalna (BR-04).</summary>
    public decimal CenaOryginalna => _wiersz.CenaOryginalna;

    /// <summary>Koszt dodatków (BR-04).</summary>
    public decimal KosztDodatkow => _wiersz.KosztDodatkow;

    /// <summary>Koszt na wyprawę (BR-05).</summary>
    public decimal KosztNaWyprawe => _wiersz.KosztNaWyprawe;

    /// <summary>Data ostatniego testu (FR-409).</summary>
    public DateOnly? DataOstatniegoTestu => _wiersz.DataOstatniegoTestu;

    /// <summary>Ostatni pomiar (energia, zasięg, „na maksimum”).</summary>
    public Pomiar? OstatniPomiar => _replika.Pomiary.Count > 0
        ? _replika.Pomiary.OrderByDescending(p => p.Data).First()
        : null;

    /// <summary>Energia docelowa (J).</summary>
    public decimal? EnergiaDocelowaJ => _replika.EnergiaDocelowaJ;

    /// <summary>Prawda, gdy replika ma otwartą awarię (FR-602).</summary>
    public bool CzyOtwartaAwaria => _wiersz.CzyOtwartaAwaria;

    /// <summary>Liczba otwartych zadań.</summary>
    public int OtwarteZadania => _wiersz.OtwarteZadania;

    /// <summary>Ostatnie trzy wyprawy (FR-304).</summary>
    public IReadOnlyList<Wyprawa> OstatnieWyprawy => _wiersz.OstatnieWyprawy;

    /// <summary>Rekonstrukcje (Id, FR-1302).</summary>
    public List<string> RekonstrukcjeId => _replika.RekonstrukcjeId;

    /// <summary>Zaznaczenie do edycji zbiorczej (FR-216).</summary>
    [ObservableProperty]
    private bool _zaznaczona;
}
