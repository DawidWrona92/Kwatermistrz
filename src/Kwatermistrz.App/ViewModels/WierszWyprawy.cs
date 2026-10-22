using Kwatermistrz.Core.Model;

namespace Kwatermistrz.App.ViewModels;

/// <summary>Wiersz listy wypraw (FR-808): dane + nazwy powiązanych rekordów do wyświetlenia.</summary>
public class WierszWyprawy
{
    public WierszWyprawy(Wyprawa wyprawa, string? replikaNazwa, string? zestawNazwa, string? mundurNazwa)
    {
        Wyprawa = wyprawa;
        ReplikaNazwa = replikaNazwa;
        ZestawNazwa = zestawNazwa;
        MundurNazwa = mundurNazwa;
    }

    public Wyprawa Wyprawa { get; }
    public string Id => Wyprawa.Id;
    public DateOnly Data => Wyprawa.Data;
    public int Rok => Wyprawa.Data.Year;
    public string? Miejsce => Wyprawa.Miejsce;
    public string? RekonstrukcjaId => Wyprawa.RekonstrukcjaId;
    public string? ReplikaNazwa { get; }
    public string? ZestawNazwa { get; }
    public string? MundurNazwa { get; }
    public string? Komentarz => Wyprawa.Komentarz;
    public bool CzyUsunieta => Wyprawa.CzyUsunieta;
}
