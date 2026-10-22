using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwatermistrz.App.Services;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.App.ViewModels;

/// <summary>Pozycja widoku „Do uzupełnienia” (FR-1204, FR-1110, FR-508, FR-706): jedna rzecz wymagająca decyzji lub danych.</summary>
public class PozycjaDoUzupelnienia
{
    public string Rodzaj { get; set; } = "";
    public string Opis { get; set; } = "";
    public string? Ekran { get; set; }
    public string? RekordId { get; set; }
}

/// <summary>ViewModel widoku „Do uzupełnienia” (FR-1204): jedna lista rzeczy wymagających decyzji lub danych.</summary>
public partial class DoUzupelnieniaViewModel : ObservableObject
{
    private readonly DaneAplikacji _dane;
    private readonly MagazynJson _magazyn;
    private readonly StanAplikacji _stan;

    public DoUzupelnieniaViewModel(DaneAplikacji dane, MagazynJson magazyn, StanAplikacji stan)
    {
        _dane = dane;
        _magazyn = magazyn;
        _stan = stan;
    }

    /// <summary>Pozycje do uzupełnienia.</summary>
    public ObservableCollection<PozycjaDoUzupelnienia> Pozycje { get; } = new();

    /// <summary>Liczba pozycji.</summary>
    public int Liczba => Pozycje.Count;

    /// <summary>Tylko otwarte zadania (FR-104, FR-601) — ustawiane z pulpitu.</summary>
    [ObservableProperty]
    private bool _tylkoZadania;

    /// <summary>Odświeża listę (FR-1204: pola nieznane, pozycje kosztowe bez opisu, przedmioty/repliki „do uzupełnienia”, liczniki do weryfikacji, otwarte zadania FR-104).</summary>
    public void Odswiez()
    {
        Pozycje.Clear();
        // otwarte zadania (FR-601, FR-104)
        foreach (var z in _dane.Zadania.Where(z => z.Status == StatusZadania.Otwarte))
            Pozycje.Add(new PozycjaDoUzupelnienia
            {
                Rodzaj = "Zadanie",
                Opis = $"{NazwaRodzaju(z.RodzajId)} — {z.Opis}" + (z.DotyczyId != null ? $" ({NazwaDotyczy(z)})" : ""),
                Ekran = z.DotyczyId != null ? (CzyReplika(z.DotyczyId) ? "Bronie" : "Wyposazenie") : null,
                RekordId = z.DotyczyId,
            });
        if (TylkoZadania)
        {
            OnPropertyChanged(nameof(Liczba));
            return;
        }
        // przedmioty i repliki ze znacznikiem „z importu, do przeglądu” (FR-1110)
        foreach (var p in _dane.Przedmioty.Where(p => p.ZnacznikZImportu))
            Pozycje.Add(new PozycjaDoUzupelnienia
            {
                Rodzaj = "Przedmiot z importu",
                Opis = $"{p.Nazwa} ({p.Firma}) — przejrzyj i zatwierdź kartę",
                Ekran = "Wyposazenie",
                RekordId = p.Id,
            });
        foreach (var r in _dane.Repliki.Where(r => r.ZnacznikZImportu))
            Pozycje.Add(new PozycjaDoUzupelnienia
            {
                Rodzaj = "Replika z importu",
                Opis = $"{r.ModelProducenta} — przejrzyj i zatwierdź kartę",
                Ekran = "Bronie",
                RekordId = r.Id,
            });
        // przedmioty „do uzupełnienia” (FR-706 — utworzone z samej nazwy)
        foreach (var p in _dane.Przedmioty.Where(p => p.DoUzupelnienia))
            Pozycje.Add(new PozycjaDoUzupelnienia
            {
                Rodzaj = "Przedmiot do uzupełnienia",
                Opis = p.Nazwa,
                Ekran = "Wyposazenie",
                RekordId = p.Id,
            });
        // pola karty „nieznane” (FR-404)
        foreach (var r in _dane.Repliki)
            foreach (var (parametrId, wartosc) in r.KartaTechniczna)
                if (wartosc.Stan == StanWartosciPola.Nieznane)
                    Pozycje.Add(new PozycjaDoUzupelnienia
                    {
                        Rodzaj = "Pole „nieznane”",
                        Opis = $"{r.ModelProducenta} — uzupełnij wartość parametru karty",
                        Ekran = "Bronie",
                        RekordId = r.Id,
                    });
        // pozycje kosztowe bez opisu (FR-508)
        foreach (var r in _dane.Repliki)
            foreach (var k in r.Koszty.Where(k => string.IsNullOrWhiteSpace(k.Opis)))
                Pozycje.Add(new PozycjaDoUzupelnienia
                {
                    Rodzaj = "Pozycja kosztowa bez opisu",
                    Opis = $"{r.ModelProducenta} — {k.Data:dd.MM.yyyy}, {k.Kwota:0.00} zł",
                    Ekran = "Bronie",
                    RekordId = r.Id,
                });
        // liczniki historyczne do weryfikacji (FR-806/1110)
        foreach (var p in _dane.Przedmioty.Where(p => p.LicznikHistorycznyDoWeryfikacji))
            Pozycje.Add(new PozycjaDoUzupelnienia
            {
                Rodzaj = "Licznik do weryfikacji",
                Opis = $"{p.Nazwa} — potwierdź licznik historyczny {p.LicznikHistoryczny}",
                Ekran = "Wyposazenie",
                RekordId = p.Id,
            });
        OnPropertyChanged(nameof(Liczba));
    }

    /// <summary>Potwierdza przegląd pozycji (znacznik znika, FR-1110).</summary>
    [RelayCommand]
    private void Potwierdz(PozycjaDoUzupelnienia? pozycja)
    {
        if (pozycja?.RekordId == null)
            return;
        var przedmiot = _dane.Przedmioty.FirstOrDefault(p => p.Id == pozycja.RekordId);
        if (przedmiot != null)
        {
            przedmiot.ZnacznikZImportu = false;
            przedmiot.LicznikHistorycznyDoWeryfikacji = false;
            przedmiot.DoUzupelnienia = false;
        }
        var replika = _dane.Repliki.FirstOrDefault(r => r.Id == pozycja.RekordId);
        if (replika != null)
            replika.ZnacznikZImportu = false;
        _magazyn.Zapisz(_dane);
        Pozycje.Remove(pozycja!);
        OnPropertyChanged(nameof(Liczba));
    }

    private string NazwaRodzaju(string rodzajId)
        => _dane.Slowniki.RodzajeZadan.FirstOrDefault(r => r.Id == rodzajId)?.Nazwa ?? "Zadanie";

    private string NazwaDotyczy(Zadanie z)
    {
        var replika = _dane.Repliki.FirstOrDefault(r => r.Id == z.DotyczyId);
        if (replika != null)
            return replika.ModelProducenta;
        var przedmiot = _dane.Przedmioty.FirstOrDefault(p => p.Id == z.DotyczyId);
        return przedmiot?.Nazwa ?? "";
    }

    private bool CzyReplika(string id) => _dane.Repliki.Any(r => r.Id == id);

    partial void OnTylkoZadaniaChanged(bool value) => Odswiez();
}
