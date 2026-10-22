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

/// <summary>Wiersz raportu „Koszty” (FR-905): wiersz grupujący (rok / kategoria / rodzaj).</summary>
public class WierszRaportuKosztow
{
    public string Grupa { get; set; } = "";
    public string Nazwa { get; set; } = "";
    public decimal Kwota { get; set; }
    public bool CzySuma { get; set; }
}

/// <summary>Wiersz raportu „Zestawy” (FR-906).</summary>
public class WierszRaportuZestawow
{
    public string Nazwa { get; set; } = "";
    public int Wyprawy { get; set; }
    public decimal Procent { get; set; }
    public DateOnly? OstatnieUzycie { get; set; }
    public decimal KosztZestawu { get; set; }
    public decimal KosztNaWyprawe { get; set; }
    public bool CzySuma { get; set; }
}

/// <summary>Wiersz raportu „Stan replik” (FR-907): rozkład stanów, bez testu, otwarte awarie.</summary>
public class WierszRaportuStanu
{
    public string Nazwa { get; set; } = "";
    public int Liczba { get; set; }
    public bool CzyNaglowek { get; set; }
    public string? ReplikaId { get; set; }
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

    /// <summary>Wiersze raportu „Koszty” (FR-905).</summary>
    public ObservableCollection<WierszRaportuKosztow> RaportKoszty { get; } = new();

    /// <summary>Wiersze raportu „Zestawy” (FR-906).</summary>
    public ObservableCollection<WierszRaportuZestawow> RaportZestawy { get; } = new();

    /// <summary>Wiersze raportu „Stan replik” (FR-907).</summary>
    public ObservableCollection<WierszRaportuStanu> RaportStan { get; } = new();

    /// <summary>Wynik kontroli spójności (FR-910).</summary>
    [ObservableProperty]
    private string _kontrolaSpójnosci = "";

    /// <summary>Słowniki.</summary>
    public Slowniki Slowniki => _dane.Slowniki;

    /// <summary>Lista raportów do wyboru.</summary>
    public static IReadOnlyList<string> Raporty => new[] { "Bronie", "Umundurowanie", "Wyprawy wg lat", "Użycie wyposażenia", "Koszty", "Zestawy", "Stan replik", "Kontrola spójności" };

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
            case "Koszty": BudujRaportKosztow(); break;
            case "Zestawy": BudujRaportZestawow(); break;
            case "Stan replik": BudujRaportStanu(); break;
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

    /// <summary>FR-905: raport „Koszty” — wydatki wg roku, kategorii i rodzaju oraz wartość posiadanej kolekcji wg cen zakupu.</summary>
    private void BudujRaportKosztow()
    {
        var kalk = _kalkulator.Kalkulator();
        RaportKoszty.Clear();
        bool Pasuje(Przedmiot p) => _stan.AktywnaRekonstrukcjaId == null || p.RekonstrukcjeId.Count == 0 || p.RekonstrukcjeId.Contains(_stan.AktywnaRekonstrukcjaId);
        bool PasujeR(Replika r) => _stan.AktywnaRekonstrukcjaId == null || r.RekonstrukcjeId.Count == 0 || r.RekonstrukcjeId.Contains(_stan.AktywnaRekonstrukcjaId);

        var wszystkie = _dane.Przedmioty.Where(Pasuje).SelectMany(p => p.Koszty.Select(k => (Koszt: k, Przedmiot: (Przedmiot?)p, Replika: (Replika?)null)))
            .Concat(_dane.Repliki.Where(PasujeR).SelectMany(r => r.Koszty.Select(k => (Koszt: k, Przedmiot: (Przedmiot?)null, Replika: (Replika?)r))))
            .ToList();

        // wg roku
        RaportKoszty.Add(new WierszRaportuKosztow { Grupa = "Wg roku", Nazwa = "", CzySuma = true });
        foreach (var g in wszystkie.GroupBy(x => x.Koszt.Data.Year).OrderBy(x => x.Key))
            RaportKoszty.Add(new WierszRaportuKosztow { Grupa = "Wg roku", Nazwa = g.Key.ToString(System.Globalization.CultureInfo.InvariantCulture), Kwota = g.Sum(x => x.Koszt.Kwota) });

        // wg kategorii (tylko przedmioty)
        RaportKoszty.Add(new WierszRaportuKosztow { Grupa = "Wg kategorii", Nazwa = "", CzySuma = true });
        var kategorie = _dane.Slowniki.Kategorie.ToDictionary(k => k.Id, k => k.Nazwa);
        foreach (var g in wszystkie.Where(x => x.Przedmiot != null)
                     .GroupBy(x => kategorie.TryGetValue(x.Przedmiot!.KategoriaId, out var n) ? n : "?")
                     .OrderByDescending(x => x.Sum(y => y.Koszt.Kwota)))
            RaportKoszty.Add(new WierszRaportuKosztow { Grupa = "Wg kategorii", Nazwa = g.Key, Kwota = g.Sum(x => x.Koszt.Kwota) });

        // wg rodzaju (nazwa słownikowa)
        RaportKoszty.Add(new WierszRaportuKosztow { Grupa = "Wg rodzaju", Nazwa = "", CzySuma = true });
        foreach (var g in wszystkie.GroupBy(x => x.Koszt.RodzajId)
                     .OrderByDescending(x => x.Sum(y => y.Koszt.Kwota)))
        {
            var nazwa = NazwaSlownika(_dane.Slowniki.RodzajeKosztow, g.Key) ?? "?";
            RaportKoszty.Add(new WierszRaportuKosztow { Grupa = "Wg rodzaju", Nazwa = nazwa, Kwota = g.Sum(x => x.Koszt.Kwota) });
        }

        // wartość posiadanej kolekcji wg cen zakupu (tylko pozycje „Zakup”, BR-04)
        var wartoscPrzedmiotow = _dane.Przedmioty.Where(p => p.Status == StatusWlasnosci.Posiadany && Pasuje(p))
            .Sum(p => Kalkulator.Koszty(p.Koszty).Oryginalna);
        var wartoscReplik = _dane.Repliki.Where(r => r.Status == StatusWlasnosci.Posiadany && PasujeR(r))
            .Sum(r => Kalkulator.Koszty(r.Koszty).Oryginalna);
        RaportKoszty.Add(new WierszRaportuKosztow { Grupa = "Kolekcja", Nazwa = "Wartość przedmiotów (cena zakupu)", Kwota = wartoscPrzedmiotow });
        RaportKoszty.Add(new WierszRaportuKosztow { Grupa = "Kolekcja", Nazwa = "Wartość replik (cena oryginalna)", Kwota = wartoscReplik });
        RaportKoszty.Add(new WierszRaportuKosztow { Grupa = "Kolekcja", Nazwa = "Razem", Kwota = wartoscPrzedmiotow + wartoscReplik, CzySuma = true });
        _ = kalk;
    }

    /// <summary>FR-906: raport „Zestawy” — wyprawy, procent, ostatnie użycie, koszt zestawu, koszt na wyprawę.</summary>
    private void BudujRaportZestawow()
    {
        var kalk = _kalkulator.Kalkulator();
        RaportZestawy.Clear();
        decimal sumaKoszt = 0;
        var liczba = 0;
        foreach (var z in _dane.Zestawy
                     .Where(z => _stan.AktywnaRekonstrukcjaId == null || z.RekonstrukcjaId == null || z.RekonstrukcjaId == _stan.AktywnaRekonstrukcjaId)
                     .OrderBy(z => z.Nazwa, StringComparer.CurrentCulture))
        {
            var (wyprawy, procent, ostatnie) = kalk.StatystykiZestawu(z);
            var koszt = kalk.KosztZestawu(z);
            RaportZestawy.Add(new WierszRaportuZestawow
            {
                Nazwa = z.Nazwa,
                Wyprawy = wyprawy,
                Procent = procent,
                OstatnieUzycie = ostatnie,
                KosztZestawu = koszt,
                KosztNaWyprawe = Kalkulator.KosztNaWyprawe(koszt, wyprawy),
            });
            sumaKoszt += koszt;
            liczba++;
        }
        if (liczba > 0)
            RaportZestawy.Add(new WierszRaportuZestawow { Nazwa = "Suma", KosztZestawu = sumaKoszt, CzySuma = true });
    }

    /// <summary>FR-907: raport „Stan replik” — rozkład stanów, repliki bez testu powyżej progu, otwarte awarie.</summary>
    private void BudujRaportStanu()
    {
        var kalk = _kalkulator.Kalkulator();
        RaportStan.Clear();
        RaportStan.Add(new WierszRaportuStanu { Nazwa = "Rozkład stanów", CzyNaglowek = true });
        foreach (var g in _dane.Repliki.GroupBy(r => r.StanId ?? "")
                     .OrderByDescending(x => x.Count()))
        {
            var nazwa = g.Key.Length == 0 ? "(bez stanu)" : NazwaSlownika(_dane.Slowniki.StanyReplik, g.Key) ?? g.Key;
            RaportStan.Add(new WierszRaportuStanu { Nazwa = nazwa, Liczba = g.Count() });
        }
        var bezTestu = kalk.ReplikiBezTestu(_dane.Ustawienia.DniBezTestu);
        RaportStan.Add(new WierszRaportuStanu { Nazwa = $"Bez testu > {_dane.Ustawienia.DniBezTestu} dni (FR-104)", CzyNaglowek = true });
        foreach (var r in bezTestu)
            RaportStan.Add(new WierszRaportuStanu { Nazwa = r.ModelProducenta, Liczba = r.Pomiary.Count > 0 ? -1 : 0, ReplikaId = r.Id });
        var otwarteAwaria = _dane.Zadania
            .Where(z => z.Status == StatusZadania.Otwarte && z.Semantyka == SemantykaZadania.Awaria)
            .ToList();
        RaportStan.Add(new WierszRaportuStanu { Nazwa = "Otwarte awarie", CzyNaglowek = true });
        foreach (var z in otwarteAwaria)
        {
            var replika = z.DotyczyId != null ? _dane.Repliki.FirstOrDefault(r => r.Id == z.DotyczyId) : null;
            RaportStan.Add(new WierszRaportuStanu
            {
                Nazwa = $"{replika?.ModelProducenta ?? "?"} — {z.Opis}",
                Liczba = 1,
                ReplikaId = z.DotyczyId,
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
                linie.Add("Nazwa;Kategoria;Użycie;Cena;Koszt na użycie");
                foreach (var w in RaportUzycia)
                    linie.Add($"{Csv(w.Nazwa)};{Csv(w.Kategoria)};{w.Uzycia};{Formatowanie.Waluta(w.Cena)};{Formatowanie.Waluta(w.KosztNaUzycie)}");
                break;
            case "Koszty":
                linie.Add("Grupa;Nazwa;Kwota");
                foreach (var w in RaportKoszty)
                    linie.Add($"{Csv(w.Grupa)};{Csv(w.Nazwa)};{Formatowanie.Waluta(w.Kwota)}");
                break;
            case "Zestawy":
                linie.Add("Nazwa;Wyprawy;Procent;Ostatnie użycie;Koszt zestawu;Koszt na wyprawę");
                foreach (var w in RaportZestawy)
                    linie.Add($"{Csv(w.Nazwa)};{w.Wyprawy};{Formatowanie.Procent(w.Procent)};{(w.OstatnieUzycie?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? "")};{Formatowanie.Waluta(w.KosztZestawu)};{Formatowanie.Waluta(w.KosztNaWyprawe)}");
                break;
            case "Stan replik":
                linie.Add("Nazwa;Liczba");
                foreach (var w in RaportStan)
                    linie.Add($"{Csv(w.Nazwa)};{(w.Liczba < 0 ? "ostatni test" : w.Liczba.ToString(System.Globalization.CultureInfo.InvariantCulture))}");
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
