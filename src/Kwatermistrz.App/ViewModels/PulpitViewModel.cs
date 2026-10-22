using CommunityToolkit.Mvvm.ComponentModel;
using Kwatermistrz.App.Controls;
using Kwatermistrz.App.Services;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Obliczenia;

namespace Kwatermistrz.App.ViewModels;

/// <summary>ViewModel pulpitu (FR-101, FR-102): liczby ogólne i mozaika wypraw.</summary>
public partial class PulpitViewModel : ObservableObject
{
    private readonly DaneAplikacji _dane;
    private readonly KalkulatorSerwis _kalkulator;

    public PulpitViewModel(DaneAplikacji dane, KalkulatorSerwis kalkulator)
    {
        _dane = dane;
        _kalkulator = kalkulator;
    }

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
        OnPropertyChanged(nameof(LacznaLiczbaWypraw));
        OnPropertyChanged(nameof(WyprawyWRoku));
        OnPropertyChanged(nameof(DataPierwszejWyprawy));
        OnPropertyChanged(nameof(DataOstatniejWyprawy));
        OnPropertyChanged(nameof(DniOdOstatniej));
        OnPropertyChanged(nameof(Kafelki));
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
