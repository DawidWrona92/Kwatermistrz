using Kwatermistrz.Core.Model;

namespace Kwatermistrz.App.ViewModels;

/// <summary>Wiersz listy zestawów (FR-709): dane + statystyki użycia.</summary>
public class WierszZestawu
{
    public WierszZestawu(Zestaw zestaw, int wyprawy, decimal procent, DateOnly? ostatnieUzycie)
    {
        Zestaw = zestaw;
        Wyprawy = wyprawy;
        Procent = procent;
        OstatnieUzycie = ostatnieUzycie;
    }

    public Zestaw Zestaw { get; }
    public string Id => Zestaw.Id;
    public string Nazwa => Zestaw.Nazwa;
    public string? RekonstrukcjaId => Zestaw.RekonstrukcjaId;
    public string? StronaId => Zestaw.StronaId;
    public bool CzySzkic => Zestaw.CzySzkic;
    public int LiczbaSlotow => Zestaw.Sloty.Count;
    public int Wyprawy { get; }
    public decimal Procent { get; }
    public DateOnly? OstatnieUzycie { get; }
}
