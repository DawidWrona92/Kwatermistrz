using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwatermistrz.App.Controls;
using Kwatermistrz.App.Services;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Obliczenia;

namespace Kwatermistrz.App.ViewModels;

/// <summary>Wiersz rankingu replik na pulpicie (FR-105).</summary>
public class WierszRankingu
{
    public string ReplikaId { get; set; } = "";
    public string Nazwa { get; set; } = "";
    public int Wyprawy { get; set; }
    public decimal KosztNaWyprawe { get; set; }
}

/// <summary>ViewModel pulpitu (FR-101, FR-102): liczby ogólne, karty skrótowe (FR-104), ranking replik (FR-105), mozaika wypraw.</summary>
public partial class PulpitViewModel : ObservableObject
{
    private readonly DaneAplikacji _dane;
    private readonly KalkulatorSerwis _kalkulator;

    public PulpitViewModel(DaneAplikacji dane, KalkulatorSerwis kalkulator)
    {
        _dane = dane;
        _kalkulator = kalkulator;
    }

    /// <summary>Zdarzenie nawigacji do ekranu z filtrem — podpina widok główny.</summary>
    public event Action<string, string?>? PrzejdzDo;

    /// <summary>Odświeża dane pulpitu (po zmianie danych lub rekonstrukcji).</summary>
    public void Odswiez()
    {
        var kalk = _kalkulator.Kalkulator();
        LacznaLiczbaWypraw = kalk.LacznaLiczbaWypraw();
        WyprawyWRoku = kalk.WyprawyWRoku(DateOnly.FromDateTime(DateTime.Today).Year);
        DataPierwszejWyprawy = kalk.DataPierwszejWyprawy();
        var ostatnia = _dane.Wyprawy.Where(w => !w.CzyUsunieta).Select(w => (DateOnly?)w.Data).Max();
        DataOstatniejWyprawy = ostatnia;
        DniOdOstatniej = ostatnia != null ? DateOnly.FromDateTime(DateTime.Today).DayNumber - ostatnia.Value.DayNumber : null;
        Kafelki = BudujKafelki(kalk, ostatnia);
        // karty skrótowe (FR-104)
        OtwarteAwaria = kalk.OtwarteAwaria();
        OtwarteZadania = kalk.OtwarteZadania();
        ReplikiBezTestu = kalk.ReplikiBezTestu(_dane.Ustawienia.DniBezTestu).Count;
        PrzedmiotyBezUzycia = kalk.PrzedmiotyBezUzycia();
        PolaNieznane = kalk.PolaNieznane();
        // ranking (FR-105)
        Ranking = kalk.RankingReplik(5)
            .Select(w => new WierszRankingu
            {
                ReplikaId = w.ReplikaId,
                Nazwa = NazwaRepliki(w.ReplikaId),
                Wyprawy = w.Wyprawy,
                KosztNaWyprawe = w.KosztNaWyprawe,
            })
            .ToList();
        OnPropertyChanged(nameof(LacznaLiczbaWypraw));
        OnPropertyChanged(nameof(WyprawyWRoku));
        OnPropertyChanged(nameof(DataPierwszejWyprawy));
        OnPropertyChanged(nameof(DataOstatniejWyprawy));
        OnPropertyChanged(nameof(DniOdOstatniej));
        OnPropertyChanged(nameof(Kafelki));
        OnPropertyChanged(nameof(OtwarteAwaria));
        OnPropertyChanged(nameof(OtwarteZadania));
        OnPropertyChanged(nameof(ReplikiBezTestu));
        OnPropertyChanged(nameof(PrzedmiotyBezUzycia));
        OnPropertyChanged(nameof(PolaNieznane));
        OnPropertyChanged(nameof(Ranking));
        OnPropertyChanged(nameof(CzyRankingPusty));
    }

    private string NazwaRepliki(string id)
    {
        var r = _dane.Repliki.FirstOrDefault(x => x.Id == id);
        return r == null ? "" : r.Prototyp != null ? $"{r.ModelProducenta} ({r.Prototyp})" : r.ModelProducenta;
    }

    /// <summary>FR-104: otwarte awarie — przejście do listy replik z filtrem „tylko awaria”.</summary>
    [RelayCommand]
    private void OtworzAwaria() => PrzejdzDo?.Invoke("Bronie", "awaria");

    /// <summary>FR-104: otwarte zadania — przejście do widoku „Do uzupełnienia”.</summary>
    [RelayCommand]
    private void OtworzZadania() => PrzejdzDo?.Invoke("DoUzupelnienia", "zadania");

    /// <summary>FR-104: repliki bez testu — przejście do listy replik z filtrem „bez testu”.</summary>
    [RelayCommand]
    private void OtworzBezTestu() => PrzejdzDo?.Invoke("Bronie", "bezTestu");

    /// <summary>FR-104: przedmioty bez użyć — przejście do wyposażenia z filtrem „bez użyć”.</summary>
    [RelayCommand]
    private void OtworzBezUzycia() => PrzejdzDo?.Invoke("Wyposazenie", "bezUzycia");

    /// <summary>FR-104: pola „nieznane” — przejście do widoku „Do uzupełnienia”.</summary>
    [RelayCommand]
    private void OtworzPolaNieznane() => PrzejdzDo?.Invoke("DoUzupelnienia", null);

    /// <summary>FR-105: otwarcie karty repliki z rankingu.</summary>
    [RelayCommand]
    private void OtworzRanking(WierszRankingu? wiersz)
    {
        if (wiersz != null)
            PrzejdzDo?.Invoke("Bronie", wiersz.ReplikaId);
    }

    /// <summary>Łączna liczba wypraw (BR-01).</summary>
    public int LacznaLiczbaWypraw { get; private set; }

    /// <summary>Wyprawy w bieżącym roku (BR-07).</summary>
    public int WyprawyWRoku { get; private set; }

    /// <summary>Data pierwszej wyprawy (BR-15).</summary>
    public DateOnly? DataPierwszejWyprawy { get; private set; }

    /// <summary>Data ostatniej zapisanej wyprawy.</summary>
    public DateOnly? DataOstatniejWyprawy { get; private set; }

    /// <summary>Dni od ostatniej wyprawy; null gdy brak wypraw.</summary>
    public int? DniOdOstatniej { get; private set; }

    /// <summary>Kafelki mozaiki wypraw (FR-102).</summary>
    public IReadOnlyList<KafelekWyprawy> Kafelki { get; private set; } = new List<KafelekWyprawy>();

    /// <summary>Dane aplikacji — do kolorów kafelków (wzory mundurów).</summary>
    public DaneAplikacji Dane => _dane;

    /// <summary>FR-104: liczba otwartych awarii.</summary>
    public int OtwarteAwaria { get; private set; }

    /// <summary>FR-104: liczba otwartych zadań (łącznie).</summary>
    public int OtwarteZadania { get; private set; }

    /// <summary>FR-104: liczba replik posiadanych bez testu dłużej niż próg.</summary>
    public int ReplikiBezTestu { get; private set; }

    /// <summary>FR-104: liczba przedmiotów posiadanych bez żadnego użycia.</summary>
    public int PrzedmiotyBezUzycia { get; private set; }

    /// <summary>FR-104: liczba pól kart technicznych „nieznane” do uzupełnienia.</summary>
    public int PolaNieznane { get; private set; }

    /// <summary>FR-105: ranking pięciu replik z największą liczbą wypraw, z kosztem na wyprawę.</summary>
    public IReadOnlyList<WierszRankingu> Ranking { get; private set; } = new List<WierszRankingu>();

    /// <summary>Prawda, gdy ranking jest pusty (brak replik posiadanych).</summary>
    public bool CzyRankingPusty => Ranking.Count == 0;

    private List<KafelekWyprawy> BudujKafelki(Kalkulator kalk, DateOnly? ostatnia)
    {
        var kafelki = new List<KafelekWyprawy>();
        // kafelki zapisanych wypraw w kolorze munduru
        foreach (var wyprawa in _dane.Wyprawy.Where(w => !w.CzyUsunieta).OrderBy(w => w.Data))
        {
            var (mundurId, _) = kalk.MundurWyprawy(wyprawa);
            var replika = wyprawa.ReplikaGlownaId != null
                ? _dane.Repliki.FirstOrDefault(r => r.Id == wyprawa.ReplikaGlownaId)?.ModelProducenta
                : null;
            var zestaw = wyprawa.ZestawId != null
                ? _dane.Zestawy.FirstOrDefault(z => z.Id == wyprawa.ZestawId)?.Nazwa
                : null;
            kafelki.Add(new KafelekWyprawy
            {
                Rok = wyprawa.Data.Year,
                CzyHistoryczny = false,
                WyprawaId = wyprawa.Id,
                MundurId = mundurId,
                Etykieta = $"{wyprawa.Data:dd.MM.yyyy}{(replika != null ? $" — {replika}" : "")}{(zestaw != null ? $" — {zestaw}" : "")}",
            });
        }
        // kafelki historyczne neutralne, w liczbie zgodnej z licznikiem roku (FR-102)
        foreach (var (rok, liczba) in _dane.Ustawienia.HistoryczneWyprawyWgRok.OrderBy(x => x.Key))
            for (var i = 0; i < liczba; i++)
                kafelki.Add(new KafelekWyprawy
                {
                    Rok = rok,
                    CzyHistoryczny = true,
                    Etykieta = $"{rok} — wpis historyczny (bez daty)",
                });
        return kafelki;
    }
}
