using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwatermistrz.App.Services;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Obliczenia;
using Kwatermistrz.Core.Storage;
using Microsoft.Win32;

namespace Kwatermistrz.App.ViewModels;

/// <summary>Wiersz raportu „Broń” (FR-901).</summary>
public class WierszRaportuBroni
{
    public int Miejsce { get; set; }
    public string Nazwa { get; set; } = "";
    public int Wyprawy { get; set; }
    public decimal Procent { get; set; }
    public string? TypMagazynka { get; set; }
    public string? Rola { get; set; }
    public string? Strona { get; set; }
    public decimal Koszt { get; set; }
    public decimal CenaOryginalna { get; set; }
    public decimal Dodatki { get; set; }
    public decimal KosztNaWyprawe { get; set; }
    public bool CzySuma { get; set; }
    public bool CzySrednia { get; set; }
}

/// <summary>Wiersz raportu „Umundurowanie” (FR-902).</summary>
public class WierszRaportuUmundurowania
{
    public string Nazwa { get; set; } = "";
    public string? Strona { get; set; }
    public string? Wzor { get; set; }
    public int Wyprawy { get; set; }
    public decimal Procent { get; set; }
    public decimal Koszt { get; set; }
    public decimal KosztNaWyprawe { get; set; }
    public bool CzySuma { get; set; }
}

/// <summary>Wiersz raportu „Wyprawy wg lat” (FR-903).</summary>
public class WierszRaportuLat
{
    public int Rok { get; set; }
    public int Wyprawy { get; set; }
    public int SumaNarastajaco { get; set; }
    public decimal SredniaNaMiesiac { get; set; }
}

/// <summary>Wiersz raportu „Użycie wyposażenia” (FR-904).</summary>
public class WierszRaportuUzycia
{
    public string Nazwa { get; set; } = "";
    public string? Kategoria { get; set; }
    public int Uzycia { get; set; }
    public decimal Cena { get; set; }
    public decimal KosztNaUzycie { get; set; }
}

/// <summary>ViewModel ekranu raportów (FR-901–904, FR-908, FR-910, FR-911): cztery raporty, eksport CSV, kontrola spójności.</summary>
public partial class RaportyViewModel : ObservableObject
{
    private readonly DaneAplikacji _dane;
    private readonly MagazynJson _magazyn;
    private readonly KalkulatorSerwis _kalkulator;
    private readonly StanAplikacji _stan;

    public RaportyViewModel(DaneAplikacji dane, MagazynJson magazyn, KalkulatorSerwis kalkulator, StanAplikacji stan)
    {
        _dane = dane;
        _magazyn = magazyn;
        _kalkulator = kalkulator;
        _stan = stan;
    }

    /// <summary>Wybrany raport (FR-901/902/903/904/910).</summary>
    [ObservableProperty]
    private string _wybranyRaport = "Bronie";

    /// <summary>Przełącznik „Uwzględnij sprzedane” (FR-901).</summary>
    [ObservableProperty]
    private bool _uwzglednijSprzedane;

    /// <summary>Wiersze raportu „Broń” (FR-901).</summary>
    public ObservableCollection<WierszRaportuBroni> RaportBroni { get; } = new();

    /// <summary>Wiersze raportu „Umundurowanie” (FR-902).</summary>
    public ObservableCollection<WierszRaportuUmundurowania> RaportUmundurowanie { get; } = new();

    /// <summary>Wiersze raportu „Wyprawy wg lat” (FR-903).</summary>
    public ObservableCollection<WierszRaportuLat> RaportLata { get; } = new();

    /// <summary>Wiersze raportu „Użycie wyposażenia” (FR-904).</summary>
    public ObservableCollection<WierszRaportuUzycia> RaportUzycia { get; } = new();

    /// <summary>Wynik kontroli spójności (FR-910).</summary>
    [ObservableProperty]
    private string _kontrolaSpójnosci = "";

    /// <summary>Słowniki.</summary>
    public Slowniki Slowniki => _dane.Slowniki;

    /// <summary>Lista raportów do wyboru.</summary>
    public static IReadOnlyList<string> Raporty => new[] { "Bronie", "Umundurowanie", "Wyprawy wg lat", "Użycie wyposażenia", "Kontrola spójności" };

    partial void OnWybranyRaportChanged(string value) => Odswiez();
    partial void OnUwzglednijSprzedaneChanged(bool value) => Odswiez();

    /// <summary>Odświeża wybrany raport.</summary>
    public void Odswiez()
    {
        switch (WybranyRaport)
        {
            case "Bronie": BudujRaportBroni(); break;
            case "Umundurowanie": BudujRaportUmundurowania(); break;
            case "Wyprawy wg lat": BudujRaportLat(); break;
            case "Użycie wyposażenia": BudujRaportUzycia(); break;
            case "Kontrola spójności": BudujKontrola(); break;
        }
    }

    private void BudujRaportBroni()
    {
        var kalk = _kalkulator.Kalkulator();
        RaportBroni.Clear();
        var repliki = _dane.Repliki
            .Where(r => UwzglednijSprzedane || r.Status != StatusWlasnosci.Sprzedany)
            .Where(r => _stan.AktywnaRekonstrukcjaId == null || r.RekonstrukcjeId.Count == 0 || r.RekonstrukcjeId.Contains(_stan.AktywnaRekonstrukcjaId))
            .Select(r => kalk.WierszRepliki(r))
            .OrderByDescending(w => w.Wyprawy)
            .ToList();
        var miejsce = 1;
        foreach (var w in repliki)
        {
            var r = _dane.Repliki.First(x => x.Id == w.ReplikaId);
            RaportBroni.Add(new WierszRaportuBroni
            {
                Miejsce = miejsce++,
                Nazwa = r.Prototyp != null ? $"{r.ModelProducenta} ({r.Prototyp})" : r.ModelProducenta,
                Wyprawy = w.Wyprawy,
                Procent = w.ProcentWypraw,
                TypMagazynka = NazwaSlownika(_dane.Slowniki.TypyMagazynkow, r.TypMagazynkaId),
                Rola = string.Join(", ", r.RoleId.Select(id => NazwaSlownika(_dane.Slowniki.Role, id))),
                Strona = string.Join(", ", r.StronyId.Select(id => NazwaSlownika(_dane.Slowniki.Strony, id))),
                Koszt = w.KosztCalkowity,
                CenaOryginalna = w.CenaOryginalna,
                Dodatki = w.KosztDodatkow,
                KosztNaWyprawe = w.KosztNaWyprawe,
            });
        }
        // wiersz średnich i sum dla replik posiadanych (FR-901, BR-06)
        var posiadane = repliki.Where(w => w.Status == StatusWlasnosci.Posiadany).ToList();
        if (posiadane.Count > 0)
        {
            RaportBroni.Add(new WierszRaportuBroni
            {
                Nazwa = "Średnia",
                Wyprawy = posiadane.Sum(w => w.Wyprawy) / posiadane.Count,
                Procent = posiadane.Average(w => w.ProcentWypraw),
                Koszt = posiadane.Average(w => w.KosztCalkowity),
                CenaOryginalna = posiadane.Average(w => w.CenaOryginalna),
                Dodatki = posiadane.Average(w => w.KosztDodatkow),
                KosztNaWyprawe = posiadane.Average(w => w.KosztNaWyprawe),
                CzySrednia = true,
            });
            RaportBroni.Add(new WierszRaportuBroni
            {
                Nazwa = "Suma",
                Koszt = posiadane.Sum(w => w.KosztCalkowity),
                CenaOryginalna = posiadane.Sum(w => w.CenaOryginalna),
                Dodatki = posiadane.Sum(w => w.KosztDodatkow),
                CzySuma = true,
            });
        }
    }

    private void BudujRaportUmundurowania()
    {
        var kalk = _kalkulator.Kalkulator();
        RaportUmundurowanie.Clear();
        var kategoriaMundury = _dane.Slowniki.Kategorie.FirstOrDefault(k =>
            Normalizacja.TekstDoPorownania(k.Nazwa) == Normalizacja.TekstDoPorownania("Mundury"));
        if (kategoriaMundury == null)
            return;
        // wiersz to mundur (pojedynczy przedmiot) lub komplet (grupa przedmiotów z KompletId)
        var mundury = _dane.Przedmioty
            .Where(p => p.KategoriaId == kategoriaMundury.Id)
            .Where(p => _stan.AktywnaRekonstrukcjaId == null || p.RekonstrukcjeId.Count == 0 || p.RekonstrukcjeId.Contains(_stan.AktywnaRekonstrukcjaId))
            .ToList();
        var grupy = new List<(string Nazwa, List<Przedmiot> Elementy, string? StronaId, string? WzorId)>();
        foreach (var m in mundury.Where(m => m.KompletId == null))
            grupy.Add((m.Nazwa, new List<Przedmiot> { m }, m.StronyId.FirstOrDefault(), m.WzorId));
        foreach (var kompletId in mundury.Where(m => m.KompletId != null).Select(m => m.KompletId).Distinct())
        {
            var elementy = mundury.Where(m => m.KompletId == kompletId || m.Id == kompletId).ToList();
            var komplet = elementy.FirstOrDefault(p => p.Id == kompletId) ?? elementy.First();
            grupy.Add((komplet.Nazwa + " (komplet)", elementy, komplet.StronyId.FirstOrDefault(), komplet.WzorId));
        }
        decimal sumaKoszt = 0;
        var liczba = 0;
        foreach (var (nazwa, elementy, stronaId, wzorId) in grupy.OrderBy(g => g.Nazwa, StringComparer.CurrentCulture))
        {
            // uzycia kompletu = wspólne (BR-08); dla pojedynczego munduru — jego uzycia
            var uzycia = elementy.Sum(e => kalk.UzyciaPrzedmiotu(e));
            var koszt = elementy.Sum(e => kalk.KosztMunduru(e));
            var wyprawy = uzycia; // dla munduru „użycia” = liczba wypraw (BR-08 przez skład)
            RaportUmundurowanie.Add(new WierszRaportuUmundurowania
            {
                Nazwa = nazwa,
                Strona = NazwaSlownika(_dane.Slowniki.Strony, stronaId),
                Wzor = NazwaSlownikaWzory(wzorId),
                Wyprawy = wyprawy,
                Procent = kalk.ProcentWypraw(wyprawy),
                Koszt = koszt,
                KosztNaWyprawe = Kalkulator.KosztNaWyprawe(koszt, wyprawy),
            });
            sumaKoszt += koszt;
            liczba++;
        }
        if (liczba > 0)
            RaportUmundurowanie.Add(new WierszRaportuUmundurowania
            {
                Nazwa = "Łącznie",
                Koszt = sumaKoszt,
                CzySuma = true,
            });
    }

    private void BudujRaportLat()
    {
        var kalk = _kalkulator.Kalkulator();
        RaportLata.Clear();
        var pierwsza = kalk.DataPierwszejWyprawy();
        var lata = _dane.Ustawienia.HistoryczneWyprawyWgRok.Keys
            .Concat(_dane.Wyprawy.Where(w => !w.CzyUsunieta).Select(w => w.Data.Year))
            .Distinct()
            .OrderBy(y => y);
        var suma = 0;
        var miesiace = pierwsza != null
            ? Math.Max(1, (DateOnly.FromDateTime(DateTime.Today).Year - pierwsza.Value.Year) * 12 + DateOnly.FromDateTime(DateTime.Today).Month - pierwsza.Value.Month + 1)
            : 1;
        foreach (var rok in lata)
        {
            var liczba = kalk.WyprawyWRoku(rok);
            suma += liczba;
            RaportLata.Add(new WierszRaportuLat
            {
                Rok = rok,
                Wyprawy = liczba,
                SumaNarastajaco = suma,
                SredniaNaMiesiac = miesiace > 0 ? (decimal)suma / miesiace : 0,
            });
        }
    }

    private void BudujRaportUzycia()
    {
        var kalk = _kalkulator.Kalkulator();
        RaportUzycia.Clear();
        var kategorie = _dane.Slowniki.Kategorie.ToDictionary(k => k.Id, k => k.Nazwa);
        foreach (var w in _dane.Przedmioty
                     .Where(p => p.KompletId == null) // elementy kompletu liczą się przez komplet (BR-08)
                     .Where(p => _stan.AktywnaRekonstrukcjaId == null || p.RekonstrukcjeId.Count == 0 || p.RekonstrukcjeId.Contains(_stan.AktywnaRekonstrukcjaId))
                     .Select(p => kalk.WierszUzycia(p))
                     .OrderByDescending(x => x.Uzycia == 0)
                     .ThenBy(x => x.Uzycia))
        {
            var przedmiot = _dane.Przedmioty.First(x => x.Id == w.PrzedmiotId);
            RaportUzycia.Add(new WierszRaportuUzycia
            {
                Nazwa = przedmiot.Nazwa,
                Kategoria = kategorie.TryGetValue(przedmiot.KategoriaId, out var kat) ? kat : null,
                Uzycia = w.Uzycia,
                Cena = w.Koszt,
                KosztNaUzycie = w.KosztNaUzycie,
            });
        }
    }

    private void BudujKontrola()
    {
        var kalk = _kalkulator.Kalkulator();
        var (wgReplik, wgLat, wgMundur, bezRepliki, bezMunduru) = kalk.KontrolaSpojnosci();
        var roznice = new List<string>();
        if (wgReplik != wgLat) roznice.Add($"wg replik {wgReplik} ≠ wg lat {wgLat}");
        if (wgReplik != wgMundur) roznice.Add($"wg replik {wgReplik} ≠ wg mundurów {wgMundur}");
        if (wgLat != wgMundur) roznice.Add($"wg lat {wgLat} ≠ wg mundurów {wgMundur}");
        KontrolaSpójnosci = roznice.Count == 0
            ? $"OK — wszystkie trzy sumy wynoszą {wgReplik} (89 = 89 = 89 po imporcie). Wypraw bez repliki: {bezRepliki}, bez munduru: {bezMunduru}."
            : $"Różnice: {string.Join("; ", roznice)}. Wypraw bez repliki: {bezRepliki}, bez munduru: {bezMunduru}.";
    }

    /// <summary>Eksportuje wybrany raport do CSV (FR-908; Excel — Etap 5 przez bibliotekę).</summary>
    [RelayCommand]
    private void EksportujCsv()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Eksport raportu do CSV",
            Filter = "Plik CSV (*.csv)|*.csv",
            FileName = $"raport-{WybranyRaport.Replace(' ', '-')}-{DateTime.Now:yyyy-MM-dd}.csv",
        };
        if (dialog.ShowDialog() != true)
            return;
        var linie = new List<string>();
        switch (WybranyRaport)
        {
            case "Bronie":
                linie.Add("Miejsce;Nazwa;Wyprawy;Procent;Typ magazynka;Rola;Strona;Koszt;Cena oryginalna;Dodatki;Koszt na wyprawę");
                foreach (var w in RaportBroni)
                    linie.Add($"{w.Miejsce};{Csv(w.Nazwa)};{w.Wyprawy};{Formatowanie.Procent(w.Procent)};{Csv(w.TypMagazynka)};{Csv(w.Rola)};{Csv(w.Strona)};{Formatowanie.Waluta(w.Koszt)};{Formatowanie.Waluta(w.CenaOryginalna)};{Formatowanie.Waluta(w.Dodatki)};{Formatowanie.Waluta(w.KosztNaWyprawe)}");
                break;
            case "Umundurowanie":
                linie.Add("Nazwa;Strona;Wzór;Wyprawy;Procent;Koszt;Koszt na wyprawę");
                foreach (var w in RaportUmundurowanie)
                    linie.Add($"{Csv(w.Nazwa)};{Csv(w.Strona)};{Csv(w.Wzor)};{w.Wyprawy};{Formatowanie.Procent(w.Procent)};{Formatowanie.Waluta(w.Koszt)};{Formatowanie.Waluta(w.KosztNaWyprawe)}");
                break;
            case "Wyprawy wg lat":
                linie.Add("Rok;Wyprawy;Suma narastająco;Średnia na miesiąc");
                foreach (var w in RaportLata)
                    linie.Add($"{w.Rok};{w.Wyprawy};{w.SumaNarastajaco};{w.SredniaNaMiesiac:0.00}");
                break;
            case "Użycie wyposażenia":
                linie.Add("Nazwa;Kategoria;Użycia;Cena;Koszt na użycie");
                foreach (var w in RaportUzycia)
                    linie.Add($"{Csv(w.Nazwa)};{Csv(w.Kategoria)};{w.Uzycia};{Formatowanie.Waluta(w.Cena)};{Formatowanie.Waluta(w.KosztNaUzycie)}");
                break;
        }
        System.IO.File.WriteAllLines(dialog.FileName, linie, System.Text.Encoding.UTF8);
    }

    private static string Csv(string? tekst) =>
        tekst == null ? "" : "\"" + tekst.Replace("\"", "\"\"") + "\"";

    private static string? NazwaSlownika(List<WartoscSlownika> slownik, string? id) =>
        id == null ? null : slownik.FirstOrDefault(w => w.Id == id)?.Nazwa;

    private string? NazwaSlownikaWzory(string? id) =>
        id == null ? null : _dane.Slowniki.Wzory.FirstOrDefault(w => w.Id == id)?.Nazwa;
}
