using CommunityToolkit.Mvvm.ComponentModel;
using Kwatermistrz.Core.Model;

namespace Kwatermistrz.App.Services;

/// <summary>Stan globalny interfejsu: aktywna rekonstrukcja (FR-1303), nawigacja, wyszukiwanie.</summary>
public partial class StanAplikacji : ObservableObject
{
    /// <summary>Id aktywnej rekonstrukcji; null = „Wszystkie”.</summary>
    [ObservableProperty]
    private string? _aktywnaRekonstrukcjaId;

    /// <summary>Aktualny ekran (klucz nawigacji).</summary>
    [ObservableProperty]
    private string _aktualnyEkran = "Pulpit";

    /// <summary>Tekst wyszukiwania globalnego.</summary>
    [ObservableProperty]
    private string _szukajTekst = "";

    partial void OnAktywnaRekonstrukcjaIdChanged(string? value)
    {
        App.Dane.Ustawienia.OstatniaRekonstrukcjaId = value;
    }
}
