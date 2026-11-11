using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Obliczenia;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.App.ViewModels;

/// <summary>Nie rozwiązuje konfliktu przez zgadywanie; decyzje użytkownika są jawne.</summary>
public sealed class WierszKonfliktu
{
    public string Klucz { get; init; } = "";
    public string Typ { get; init; } = "";
    public string Nazwa { get; init; } = "";
    public string Szczegoly { get; init; } = "";
    public int? Rok { get; init; }
    public Wyprawa? Wyprawa { get; init; }
    public Przedmiot? Przedmiot { get; init; }
    public bool CzyHistorycznaWirtualna => Typ == "Wyprawa historyczna";
    public bool CzyHistorycznaZapisana => Typ == "Brak danych wyprawy";
    public bool CzyWyprawa => CzyHistorycznaWirtualna || CzyHistorycznaZapisana;
    public bool CzyMiejsce => Typ == "Miejsce wyposażenia";
}

public partial class KonfliktyViewModel : ObservableObject
{
    private readonly DaneAplikacji _dane;
    private readonly MagazynJson _magazyn;
    public event Action? DaneUzgodnione;

    public KonfliktyViewModel(DaneAplikacji dane, MagazynJson magazyn)
    {
        _dane = dane;
        _magazyn = magazyn;
    }

    public ObservableCollection<WierszKonfliktu> Wiersze { get; } = new();

    [ObservableProperty] private WierszKonfliktu? _wybrany;
    [ObservableProperty] private DateTime? _data;
    [ObservableProperty] private string? _replikaId;
    [ObservableProperty] private string? _zestawId;
    [ObservableProperty] private string? _miejsceId;
    [ObservableProperty] private string _komunikat = "";

    public IReadOnlyList<Replika> Repliki => _dane.Repliki
        .OrderBy(r => r.ModelProducenta).ToList();
    public IReadOnlyList<Zestaw> Zestawy => _dane.Zestawy
        .OrderBy(z => z.Nazwa).ToList();
    public IReadOnlyList<WartoscSlownika> Miejsca => _dane.Slowniki.Miejsca
        .OrderBy(m => m.Nazwa).ToList();
    public bool CzyWyprawa => Wybrany?.CzyWyprawa == true;
    public bool CzyMiejsce => Wybrany?.CzyMiejsce == true;
    public int LiczbaKonfliktow => Wiersze.Count;
    public int LiczbaWyprawHistorycznych => RejestrWypraw.Nierozpoznane(_dane).Count;

    partial void OnWybranyChanged(WierszKonfliktu? value)
    {
        Data = value?.Wyprawa?.Data is DateOnly d && d != default
            ? d.ToDateTime(TimeOnly.MinValue) : null;
        ReplikaId = value?.Wyprawa?.ReplikaGlownaId;
        ZestawId = value?.Wyprawa?.ZestawId;
        MiejsceId = value?.Przedmiot?.MiejsceId;
        OnPropertyChanged(nameof(CzyWyprawa));
        OnPropertyChanged(nameof(CzyMiejsce));
    }

    /// <summary>Wyliczane z aktualnych danych; nie utrzymujemy osobnej kopii konfliktów.</summary>
    public void Odswiez()
    {
        var poprzedniKlucz = Wybrany?.Klucz;
        Wiersze.Clear();

        foreach (var wpis in RejestrWypraw.Nierozpoznane(_dane))
            Wiersze.Add(new WierszKonfliktu
            {
                Klucz = wpis.Id,
                Typ = "Wyprawa historyczna",
                Nazwa = wpis.Nazwa,
                Szczegoly = $"Rok {wpis.Rok}. Data NN-NN-NNNN; nieznane powiązania. Wpis pochodzi z rocznego licznika.",
                Rok = wpis.Rok,
            });

        foreach (var wyprawa in _dane.Wyprawy.Where(w =>
                     !w.CzyUsunieta && w.CzyHistoryczna &&
                     (w.Data == default || w.ReplikaGlownaId == null || w.ZestawId == null)))
        {
            var braki = new List<string>();
            if (wyprawa.Data == default) braki.Add("data");
            if (wyprawa.ReplikaGlownaId == null) braki.Add("replika");
            if (wyprawa.ZestawId == null) braki.Add("zestaw");
            Wiersze.Add(new WierszKonfliktu
            {
                Klucz = "wyprawa:" + wyprawa.Id,
                Typ = "Brak danych wyprawy",
                Nazwa = wyprawa.NazwaHistoryczna ?? "Wyprawa historyczna",
                Szczegoly = "Brak: " + string.Join(", ", braki) + ". Znane pola pozostają zachowane.",
                Rok = RejestrWypraw.Rok(wyprawa),
                Wyprawa = wyprawa,
            });
        }

        var znaneMiejsca = _dane.Slowniki.Miejsca.Select(m => m.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var przedmiot in _dane.Przedmioty.Where(p =>
                     !string.IsNullOrWhiteSpace(p.MiejsceId) && !znaneMiejsca.Contains(p.MiejsceId)))
            Wiersze.Add(new WierszKonfliktu
            {
                Klucz = "miejsce:" + przedmiot.Id,
                Typ = "Miejsce wyposażenia",
                Nazwa = string.IsNullOrWhiteSpace(przedmiot.Nazwa) ? "Przedmiot bez nazwy" : przedmiot.Nazwa,
                Szczegoly = "Nie rozpoznano miejsca przechowywania. Wybierz poprawne miejsce ze słownika.",
                Przedmiot = przedmiot,
            });

        var roznica = RejestrWypraw.RozbieznoscLicznikow(_dane);
        if (roznica != 0)
            Wiersze.Add(new WierszKonfliktu
            {
                Klucz = "liczniki",
                Typ = "Rozbieżne liczniki",
                Nazwa = "Historia wypraw",
                Szczegoly = $"Różnica pozostałych liczników rocznych i replik: {roznica:+#;-#;0}. " +
                    "Nie można ustalić powiązań automatycznie. Przejrzyj import i historię replik.",
            });

        Wybrany = Wiersze.FirstOrDefault(w => w.Klucz == poprzedniKlucz)
            ?? Wiersze.FirstOrDefault();
        OnPropertyChanged(nameof(LiczbaKonfliktow));
        OnPropertyChanged(nameof(LiczbaWyprawHistorycznych));
        OnPropertyChanged(nameof(Repliki));
        OnPropertyChanged(nameof(Zestawy));
        OnPropertyChanged(nameof(Miejsca));
    }

    [RelayCommand]
    private void ZapiszRozwiazanie()
    {
        if (Wybrany == null)
            return;
        try
        {
            if (Wybrany.CzyHistorycznaWirtualna)
            {
                RejestrWypraw.Rozpoznaj(_dane, Wybrany.Klucz,
                    Data == null ? null : DateOnly.FromDateTime(Data.Value),
                    ReplikaId, ZestawId);
                Komunikat = "Wyprawa zapisana. Historyczne liczniki zostały skorygowane bez podwójnego naliczania.";
            }
            else if (Wybrany.CzyHistorycznaZapisana && Wybrany.Wyprawa != null)
            {
                RejestrWypraw.Dopelnij(_dane, Wybrany.Wyprawa,
                    Data == null ? null : DateOnly.FromDateTime(Data.Value),
                    ReplikaId, ZestawId);
                Komunikat = "Uzupełniono znane dane wyprawy.";
            }
            else if (Wybrany.CzyMiejsce && Wybrany.Przedmiot != null)
            {
                if (MiejsceId == null || !_dane.Slowniki.Miejsca.Any(m => m.Id == MiejsceId))
                    throw new InvalidOperationException("Wybierz istniejące miejsce przechowywania.");
                Wybrany.Przedmiot.MiejsceId = MiejsceId;
                Komunikat = "Uzgodniono miejsce. W tabeli Wyposażenie będzie teraz widoczna jego nazwa.";
            }
            else
            {
                Komunikat = "Tego konfliktu nie można rozstrzygnąć automatycznie. Uzupełnij dane źródłowe.";
                return;
            }
            _magazyn.Zapisz(_dane);
            Odswiez();
            DaneUzgodnione?.Invoke();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            Komunikat = ex.Message;
        }
    }
}
