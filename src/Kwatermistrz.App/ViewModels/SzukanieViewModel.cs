using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwatermistrz.Core.Model;

namespace Kwatermistrz.App.ViewModels;

/// <summary>Wynik globalnego wyszukiwania (FR-1201): jedna pozycja pogrupowana wg typu.</summary>
public class WynikSzukania
{
    public string Typ { get; set; } = "";
    public string Nazwa { get; set; } = "";
    public string? Ekran { get; set; }
    public string? RekordId { get; set; }
}

/// <summary>ViewModel globalnego wyszukiwania (FR-1201): przedmioty, repliki, zestawy, wyprawy, zadania; wyniki pogrupowane wg typu.</summary>
public partial class SzukanieViewModel : ObservableObject
{
    private readonly DaneAplikacji _dane;

    public SzukanieViewModel(DaneAplikacji dane)
    {
        _dane = dane;
    }

    /// <summary>Wyniki wyszukiwania.</summary>
    public IReadOnlyList<WynikSzukania> Wyniki { get; private set; } = new List<WynikSzukania>();

    /// <summary>Wyszukuje we wszystkich typach (FR-1201).</summary>
    public void Szukaj(string? tekst)
    {
        var wyniki = new List<WynikSzukania>();
        var t = Normalizacja.Przytnij(tekst);
        if (t != null)
        {
            var szukane = Normalizacja.TekstDoPorownania(t);
            foreach (var p in _dane.Przedmioty)
                if (Normalizacja.TekstDoPorownania(p.Nazwa).Contains(szukane) ||
                    (p.Firma != null && Normalizacja.TekstDoPorownania(p.Firma).Contains(szukane)))
                    wyniki.Add(new WynikSzukania { Typ = "Przedmiot", Nazwa = p.Nazwa, Ekran = "Wyposazenie", RekordId = p.Id });
            foreach (var r in _dane.Repliki)
                if (Normalizacja.TekstDoPorownania(r.ModelProducenta).Contains(szukane) ||
                    (r.Prototyp != null && Normalizacja.TekstDoPorownania(r.Prototyp).Contains(szukane)))
                    wyniki.Add(new WynikSzukania { Typ = "Replika", Nazwa = r.ModelProducenta, Ekran = "Bronie", RekordId = r.Id });
            foreach (var z in _dane.Zestawy)
                if (Normalizacja.TekstDoPorownania(z.Nazwa).Contains(szukane))
                    wyniki.Add(new WynikSzukania { Typ = "Zestaw", Nazwa = z.Nazwa, Ekran = "Zestawy", RekordId = z.Id });
            foreach (var w in _dane.Wyprawy.Where(w => !w.CzyUsunieta))
                if ((w.Miejsce != null && Normalizacja.TekstDoPorownania(w.Miejsce).Contains(szukane)) ||
                    (w.Komentarz != null && Normalizacja.TekstDoPorownania(w.Komentarz).Contains(szukane)))
                    wyniki.Add(new WynikSzukania { Typ = "Wyprawa", Nazwa = $"{w.Data:dd.MM.yyyy} — {w.Miejsce}", Ekran = "Wyprawy", RekordId = w.Id });
            foreach (var z in _dane.Zadania.Where(z => z.Status == StatusZadania.Otwarte))
                if (Normalizacja.TekstDoPorownania(z.Opis).Contains(szukane))
                {
                    // wynik prowadzi do rekordu (repliki/przedmiotu), którego dotyczy zadanie — ekran go zaznacza
                    var dotyczyRepliki = z.DotyczyId != null && _dane.Repliki.Any(r => r.Id == z.DotyczyId);
                    var dotyczyPrzedmiotu = z.DotyczyId != null && _dane.Przedmioty.Any(p => p.Id == z.DotyczyId);
                    wyniki.Add(new WynikSzukania
                    {
                        Typ = "Zadanie",
                        Nazwa = z.Opis,
                        Ekran = dotyczyRepliki ? "Bronie" : dotyczyPrzedmiotu ? "Wyposazenie" : null,
                        RekordId = z.DotyczyId,
                    });
                }
        }
        Wyniki = wyniki;
        OnPropertyChanged(nameof(Wyniki));
    }
}
