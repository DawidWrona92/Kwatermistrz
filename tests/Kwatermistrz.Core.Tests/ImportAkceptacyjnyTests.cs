using Kwatermistrz.Core.Import;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Obliczenia;

namespace Kwatermistrz.Core.Tests;

/// <summary>Test akceptacyjny importu: wartości z rozdziału 8.2 po imporcie pliku źródłowego (NFR-09). Q12 zaktualizowane: EMR → Rosja, DKWS → Polska.</summary>
public class ImportAkceptacyjnyTests
{
    private static string SciezkaDanych => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "dane-startowe", "Dane_zrodlowe_Kwatermistrz.xlsx"));

    private static (PodgladImportu Podglad, Kalkulator Kalk) Importuj()
    {
        var podglad = ImporterKwatermistrz.PrzygotujPodglad(SciezkaDanych, new DateOnly(2026, 10, 22));
        var dane = podglad.Dane!;
        return (podglad, new Kalkulator(dane));
    }

    [Fact]
    public void Import_PodgladMaDaneBezZapisu()
    {
        var (podglad, _) = Importuj();
        Assert.NotNull(podglad.Dane);
        Assert.True(podglad.LiczbaRekordow["Repliki"] > 0);
        Assert.True(podglad.LiczbaRekordow["Przedmioty"] > 0);
    }

    [Fact]
    public void Import_Repliki_18Posiadanych1DoSprzedazy8Sprzedanych()
    {
        var (podglad, _) = Importuj();
        var repliki = podglad.Dane!.Repliki;
        Assert.Equal(18, repliki.Count(r => r.Status == StatusWlasnosci.Posiadany));
        Assert.Equal(1, repliki.Count(r => r.Status == StatusWlasnosci.DoSprzedazy));
        Assert.Equal(8, repliki.Count(r => r.Status == StatusWlasnosci.Sprzedany));
    }

    [Fact]
    public void Import_SA_G12_JestDoSprzedazyIWczesciach_1Wyprawa()
    {
        var (podglad, _) = Importuj();
        var g36 = podglad.Dane!.Repliki.First(r => Normalizacja.TekstDoPorownania(r.ModelProducenta).Contains("g36c"));
        Assert.Equal(StatusWlasnosci.DoSprzedazy, g36.Status);
        Assert.Equal(1, g36.LicznikHistorycznyWypraw);
        var stan = podglad.Dane.Slowniki.StanyReplik.First(s => s.Id == g36.StanId);
        Assert.Equal("W częściach", stan.Nazwa);
    }

    [Fact]
    public void Import_Wyprawy_58_1_30_Lacznie89()
    {
        var (podglad, kalk) = Importuj();
        var repliki = podglad.Dane!.Repliki;
        var posiadane = repliki.Where(r => r.Status == StatusWlasnosci.Posiadany).Sum(r => kalk.WyprawyRepliki(r));
        var doSprzedazy = repliki.Where(r => r.Status == StatusWlasnosci.DoSprzedazy).Sum(r => kalk.WyprawyRepliki(r));
        var sprzedane = repliki.Where(r => r.Status == StatusWlasnosci.Sprzedany).Sum(r => kalk.WyprawyRepliki(r));
        Assert.Equal(58, posiadane);
        Assert.Equal(1, doSprzedazy);
        Assert.Equal(30, sprzedane);
        Assert.Equal(89, kalk.LacznaLiczbaWypraw());
    }

    [Fact]
    public void Import_WyprawyWgLat_10_30_17_19_13()
    {
        var (podglad, kalk) = Importuj();
        Assert.Equal(10, kalk.WyprawyWRoku(2022));
        Assert.Equal(30, kalk.WyprawyWRoku(2023));
        Assert.Equal(17, kalk.WyprawyWRoku(2024));
        Assert.Equal(19, kalk.WyprawyWRoku(2025));
        Assert.Equal(13, kalk.WyprawyWRoku(2026));
        Assert.Equal(new DateOnly(2022, 9, 1), podglad.Dane!.Ustawienia.DataPierwszejWyprawy);
    }

    [Fact]
    public void Import_Replika_SAJ01_KosztyIPercent()
    {
        var (podglad, kalk) = Importuj();
        var j01 = podglad.Dane!.Repliki.First(r => r.ModelProducenta.StartsWith("SA-J01"));
        var w = kalk.WierszRepliki(j01);
        Assert.Equal(19, w.Wyprawy);
        Assert.Equal(3297.12m, w.KosztCalkowity);
        Assert.Equal(960m, w.CenaOryginalna);
        Assert.Equal(2337.12m, w.KosztDodatkow);
        Assert.Equal(173.53m, Math.Round(w.KosztNaWyprawe, 2));
        Assert.Equal(21.3m, Math.Round(w.ProcentWypraw, 1));
    }

    [Fact]
    public void Import_Replika_ELMS_UjemneSkladnikiKosztu()
    {
        var (podglad, kalk) = Importuj();
        var elms = podglad.Dane!.Repliki.First(r => r.ModelProducenta.StartsWith("E&L ELMS"));
        var w = kalk.WierszRepliki(elms);
        // 626-100-35+76+252+257 = 1076; zakup = 626-100-35 = 491
        Assert.Equal(1076m, w.KosztCalkowity);
        Assert.Equal(491m, w.CenaOryginalna);
        Assert.Equal(585m, w.KosztDodatkow);
        Assert.Equal(0, w.Wyprawy);
        Assert.Equal(1076m, w.KosztNaWyprawe); // BR-05: bez wypraw = pełny koszt
    }

    [Fact]
    public void Import_SumyKosztow_ReplikPosiadanych()
    {
        var (podglad, kalk) = Importuj();
        var posiadane = kalk.ReplikiPosiadane().ToList();
        var sumaKosztow = posiadane.Sum(r => Kalkulator.Koszty(r.Koszty).Calkowity);
        var sumaOryginalnych = posiadane.Sum(r => Kalkulator.Koszty(r.Koszty).Oryginalna);
        Assert.Equal(27769.07m, sumaKosztow);
        Assert.Equal(19752.95m, sumaOryginalnych);
        Assert.Equal(1542.73m, Math.Round(sumaKosztow / posiadane.Count, 2));
        Assert.Equal(1097.39m, Math.Round(sumaOryginalnych / posiadane.Count, 2));
        Assert.Equal(445.34m, Math.Round((sumaKosztow - sumaOryginalnych) / posiadane.Count, 2));
    }

    [Fact]
    public void Import_SredniKosztNaWyprawe_883_36()
    {
        var (podglad, kalk) = Importuj();
        var posiadane = kalk.ReplikiPosiadane().ToList();
        var srednia = posiadane.Average(r => kalk.WierszRepliki(r).KosztNaWyprawe);
        Assert.Equal(883.36m, Math.Round(srednia, 2));
    }

    [Fact]
    public void Import_Wyposazenie_106PrzedmiotowWKategoriach()
    {
        var (podglad, _) = Importuj();
        var dane = podglad.Dane!;
        // 106 przedmiotów z arkusza Wyposazenie („Aha Tactical” bez nazwy dostaje nazwę = firma); szkice dochodzą z umundurowania i zestawów
        var zWyposazenia = dane.Przedmioty.Count(p => p.ZnacznikZImportu && !p.DoUzupelnienia);
        var szkice = dane.Przedmioty.Count(p => p.DoUzupelnienia);
        Assert.True(zWyposazenia >= 105, $"Oczekiwano >= 105 przedmiotów z nazwą, jest {zWyposazenia}");
        Assert.True(szkice >= 1, "Oczekiwano szkiców do uzupełnienia (brak odpowiednika w katalogu)");
    }

    [Fact]
    public void Import_Kategorie_ZgodneZRozkladem()
    {
        var (podglad, _) = Importuj();
        var dane = podglad.Dane!;
        int Liczba(string nazwaKategorii) => dane.Przedmioty.Count(p =>
            dane.Slowniki.Kategorie.First(k => k.Id == p.KategoriaId).Nazwa == nazwaKategorii && !p.DoUzupelnienia);
        Assert.Equal(4, Liczba("Czapki"));
        Assert.Equal(3, Liczba("Hełmy"));
        Assert.Equal(4, Liczba("Szalokominiarki"));
        Assert.Equal(15, Liczba("Mundury"));
        Assert.Equal(8, Liczba("Kamizelki"));
        Assert.Equal(3, Liczba("Pasoszelki i chest rigi"));
        Assert.Equal(4, Liczba("Pasy"));
        Assert.Equal(2, Liczba("Nakolanniki"));
        Assert.Equal(3, Liczba("Plecaki"));
        Assert.Equal(60, Liczba("Ładownice"));
    }

    [Fact]
    public void Import_MontazLadownic_60NaNosnikach()
    {
        var (podglad, _) = Importuj();
        var dane = podglad.Dane!;
        var ladownice = dane.Przedmioty.Where(p =>
            dane.Slowniki.Kategorie.First(k => k.Id == p.KategoriaId).Nazwa == "Ładownice" && !p.DoUzupelnienia).ToList();
        var zamontowane = ladownice.Count(l => l.ZamontowanyNaId != null);
        // wszystkie 60 ma rozpoznany nośnik (jednoznaczny) lub konflikt do decyzji (niejednoznaczny albo brak odpowiednika)
        var konfliktyMontazu = podglad.Konflikty.Count(k =>
            (k.Rodzaj == RodzajKonfliktu.Niejednoznaczne || k.Rodzaj == RodzajKonfliktu.BrakOdpowiednika) && k.Opis.Contains("Ładownica"));
        Assert.True(zamontowane + konfliktyMontazu >= 60, $"zamontowane {zamontowane}, konflikty {konfliktyMontazu}");
    }

    [Fact]
    public void Import_Zestawy_15()
    {
        var (podglad, _) = Importuj();
        Assert.Equal(15, podglad.Dane!.Zestawy.Count);
    }

    [Fact]
    public void Import_Zadania_18()
    {
        var (podglad, _) = Importuj();
        Assert.Equal(18, podglad.Dane!.Zadania.Count);
    }

    [Fact]
    public void Import_KartaTechniczna_SAJ01_ParametryIPomiar()
    {
        var (podglad, _) = Importuj();
        var dane = podglad.Dane!;
        var j01 = dane.Repliki.First(r => r.ModelProducenta.StartsWith("SA-J01"));
        Assert.NotEmpty(j01.KartaTechniczna);
        var pomiary = j01.Pomiary;
        Assert.NotEmpty(pomiary);
        var ostatni = pomiary.OrderByDescending(p => p.Data).First();
        Assert.Equal(1.9m, ostatni.EnergiaJ);
        Assert.True(ostatni.NaMaksimum);
        Assert.Equal(1.9m, j01.EnergiaDocelowaJ);
    }

    [Fact]
    public void Import_KartaTechniczna_HPA_WykryteDlaVSSiSAH20()
    {
        var (podglad, _) = Importuj();
        var dane = podglad.Dane!;
        var hpa = Model.Slowniki.Znajdz(dane.Slowniki.Napedy, "HPA");
        var vss = dane.Repliki.First(r => r.ModelProducenta.Contains("VSS"));
        var h20 = dane.Repliki.First(r => r.ModelProducenta.StartsWith("SA-H20"));
        Assert.Equal(hpa!.Id, vss.NapedId);
        Assert.Equal(hpa.Id, h20.NapedId);
        // pola mechanizmu mają stan „nie dotyczy”
        var nieDotyczy = vss.KartaTechniczna.Values.Count(v => v.Stan == StanWartosciPola.NieDotyczy);
        Assert.True(nieDotyczy > 0);
    }

    [Fact]
    public void Import_KartaTechniczna_Duplikaty_SAJ02_RPKS_LCT_Scalone()
    {
        var (podglad, _) = Importuj();
        var dane = podglad.Dane!;
        // każda nazwa występuje raz na liście replik
        Assert.Single(dane.Repliki, r => Normalizacja.TekstDoPorownania(r.ModelProducenta).Contains("sa j02"));
        Assert.Single(dane.Repliki, r => Normalizacja.TekstDoPorownania(r.ModelProducenta).Contains("rpks"));
        // konflikty duplikatów są w podglądzie
        var duplikaty = podglad.Konflikty.Count(k => k.Rodzaj == RodzajKonfliktu.Duplikat);
        Assert.True(duplikaty >= 3, $"oczekiwano >= 3 konfliktów duplikatów, jest {duplikaty}");
    }

    [Fact]
    public void Import_Rekonstrukcje_PoJednejNaStrone()
    {
        var (podglad, _) = Importuj();
        var nazwy = podglad.Dane!.Rekonstrukcje.Select(r => r.Nazwa).OrderBy(n => n).ToList();
        Assert.Equal(new[] { "NATO", "Polska", "Rosja" }, nazwy);
    }

    [Fact]
    public void Import_Umundurowanie_LicznikiHistoryczne_SumujaSieDo89()
    {
        var (podglad, _) = Importuj();
        var dane = podglad.Dane!;
        var kategoriaMundury = dane.Slowniki.Kategorie.First(k => k.Nazwa == "Mundury");
        var mundury = dane.Przedmioty.Where(p => p.KategoriaId == kategoriaMundury.Id).ToList();
        // mundury z tabeli umundurowania (20 pozycji + 3 wcześniejsze EMR) mają liczniki sumujące się do 89
        var suma = mundury.Sum(m => m.LicznikHistoryczny);
        Assert.Equal(89, suma);
    }

    [Fact]
    public void Import_KontrolaSpojnosci_89_89_89()
    {
        var (_, kalk) = Importuj();
        var (wgReplik, wgLat, wgMundur, bezRepliki, bezMunduru) = kalk.KontrolaSpojnosci();
        Assert.Equal(89, wgReplik);
        Assert.Equal(89, wgLat);
        Assert.Equal(89, wgMundur);
        Assert.Equal(0, bezRepliki);
    }

    [Fact]
    public void Import_ZnacznikZImportu_NaWszystkichRekordach()
    {
        var (podglad, _) = Importuj();
        var dane = podglad.Dane!;
        Assert.All(dane.Repliki, r => Assert.True(r.ZnacznikZImportu));
        Assert.All(dane.Przedmioty, p => Assert.True(p.ZnacznikZImportu));
        Assert.All(dane.Zestawy, z => Assert.True(z.ZnacznikZImportu));
        Assert.All(dane.Zadania, z => Assert.True(z.ZnacznikZImportu));
    }

    [Fact]
    public void Import_FormaKosztu_J9_Dla_PPK20_NieJestKwota()
    {
        // „J9+70” w arkuszu to literówka/oznaczenie, nie kwota — nie może trafić jako koszt 9 zł
        var (podglad, _) = Importuj();
        var ppk = podglad.Dane!.Repliki.First(r => r.ModelProducenta.Contains("PPK20"));
        var suma = ppk.Koszty.Sum(k => k.Kwota);
        Assert.NotEqual(9m, suma);
    }
}
