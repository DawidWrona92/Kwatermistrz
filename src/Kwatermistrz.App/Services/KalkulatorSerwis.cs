using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Obliczenia;

namespace Kwatermistrz.App.Services;

/// <summary>Opakowanie kalkulatora na potrzeby interfejsu — jedna instancja na dane aplikacji.</summary>
public class KalkulatorSerwis
{
    private readonly DaneAplikacji _dane;

    public KalkulatorSerwis(DaneAplikacji dane)
    {
        _dane = dane;
    }

    /// <summary>Tworzy kalkulator dla bieżących danych.</summary>
    public Kalkulator Kalkulator() => new(_dane);
}
