using CommunityToolkit.Mvvm.ComponentModel;
using Kwatermistrz.Core.Model;

namespace Kwatermistrz.App.ViewModels;

/// <summary>Wiersz listy przedmiotów (FR-203): dane + stan zaznaczenia do edycji zbiorczej (FR-216).</summary>
public partial class WierszPrzedmiotu : ObservableObject
{
    private readonly Przedmiot _przedmiot;

    public WierszPrzedmiotu(Przedmiot przedmiot, int uzycia)
    {
        _przedmiot = przedmiot;
        Uzycia = uzycia;
    }

    /// <summary>Model przedmiotu.</summary>
    public Przedmiot Przedmiot => _przedmiot;

    /// <summary>Id przedmiotu.</summary>
    public string Id => _przedmiot.Id;

    /// <summary>Zaznaczenie do edycji zbiorczej (FR-216).</summary>
    [ObservableProperty]
    private bool _zaznaczony;

    /// <summary>Firma (producent).</summary>
    public string? Firma => _przedmiot.Firma;

    /// <summary>Nazwa przedmiotu.</summary>
    public string Nazwa => _przedmiot.Nazwa;

    /// <summary>Id kategorii.</summary>
    public string KategoriaId => _przedmiot.KategoriaId;

    /// <summary>Id wzoru kamuflażu.</summary>
    public string? WzorId => _przedmiot.WzorId;

    /// <summary>Id miejsca przechowywania.</summary>
    public string? MiejsceId => _przedmiot.MiejsceId;

    /// <summary>Id pochodzenia / klasy.</summary>
    public string? PochodzenieId => _przedmiot.PochodzenieId;

    /// <summary>Strony (Id).</summary>
    public List<string> StronyId => _przedmiot.StronyId;

    /// <summary>Status własności.</summary>
    public StatusWlasnosci Status => _przedmiot.Status;

    /// <summary>Łączna liczba użyć (historyczne + zapisane wyprawy, BR-08).</summary>
    public int Uzycia { get; }

    /// <summary>Komentarz.</summary>
    public string? Komentarz => _przedmiot.Komentarz;

    /// <summary>Pierwsze zdjęcie (ścieżka względna); null gdy brak.</summary>
    public string? ZdjecieMiniatury => _przedmiot.Zdjecia.Count > 0 ? _przedmiot.Zdjecia[0] : null;

    /// <summary>Prawda, gdy przedmiot jest elementem kompletu.</summary>
    public bool CzyWKomplecie => _przedmiot.KompletId != null;

    /// <summary>Prawda, gdy przedmiot jest zamontowany na nośniku.</summary>
    public bool CzyZamontowany => _przedmiot.ZamontowanyNaId != null;

    /// <summary>Prawda, gdy ma znacznik „z importu, do przeglądu” (FR-1110).</summary>
    public bool CzyZImportu => _przedmiot.ZnacznikZImportu;

    /// <summary>Prawda, gdy oznaczony „do uzupełnienia”.</summary>
    public bool CzyDoUzupelnienia => _przedmiot.DoUzupelnienia;

    /// <summary>Cena zakupu (zł) lub null.</summary>
    public decimal? CenaZakupu => _przedmiot.CenaZakupu;

    /// <summary>Rekonstrukcje (Id).</summary>
    public List<string> RekonstrukcjeId => _przedmiot.RekonstrukcjeId;
}
