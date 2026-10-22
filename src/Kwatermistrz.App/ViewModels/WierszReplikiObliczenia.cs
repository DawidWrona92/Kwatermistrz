using Kwatermistrz.Core.Model;

namespace Kwatermistrz.App.ViewModels;

/// <summary>Widok obliczeniowy repliki dla interfejsu: rozszerza WierszRepliki o pola karty (FR-304, FR-602).</summary>
public class WierszReplikiObliczenia
{
    public string ReplikaId { get; set; } = "";
    public StatusWlasnosci Status { get; set; }
    public int Wyprawy { get; set; }
    public decimal ProcentWypraw { get; set; }
    public decimal KosztCalkowity { get; set; }
    public decimal CenaOryginalna { get; set; }
    public decimal KosztDodatkow { get; set; }
    public decimal KosztNaWyprawe { get; set; }
    public DateOnly? DataOstatniegoTestu { get; set; }
    public bool CzyOtwartaAwaria { get; set; }
    public int OtwarteZadania { get; set; }
    public IReadOnlyList<Wyprawa> OstatnieWyprawy { get; set; } = new List<Wyprawa>();
}
