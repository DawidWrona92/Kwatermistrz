using Kwatermistrz.Core.Model;

namespace Kwatermistrz.App.ViewModels;

/// <summary>Wiersz listy wypraw (FR-808): dane + nazwy powiązanych rekordów do wyświetlenia.</summary>
public class WierszWyprawy
{
    public WierszWyprawy(Wyprawa wyprawa, string? replikaNazwa, string? zestawNazwa, string? mundurNazwa,
        bool czyWirtualna = false)
    {
        Wyprawa = wyprawa;
        CzyWirtualna = czyWirtualna;
        ReplikaNazwa = replikaNazwa;
        ZestawNazwa = zestawNazwa;
        MundurNazwa = mundurNazwa;
    }

    public Wyprawa Wyprawa { get; }
    public bool CzyWirtualna { get; }
    public bool CzyZapisana => !CzyWirtualna;
    public string Nazwa => Wyprawa.NazwaHistoryczna ?? "Wyprawa";
    public string DataTekst => Wyprawa.Data == default ? "NN-NN-NNNN" : Wyprawa.Data.ToString("dd-MM-yyyy");
    public string CzyWymagaUzupelnienia => CzyWirtualna ? "Do rozpoznania" : Wyprawa.Data == default ? "Data nieznana" : "";
    public string Id => Wyprawa.Id;
    public DateOnly Data => Wyprawa.Data;
    public int Rok => Kwatermistrz.Core.Obliczenia.RejestrWypraw.Rok(Wyprawa);
    public string? Miejsce => Wyprawa.Miejsce;
    public string? RekonstrukcjaId => Wyprawa.RekonstrukcjaId;
    public string? ReplikaNazwa { get; }
    public string? ZestawNazwa { get; }
    public string? MundurNazwa { get; }
    public string? Komentarz => Wyprawa.Komentarz;
    public bool CzyUsunieta => Wyprawa.CzyUsunieta;
}
