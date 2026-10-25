using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Import;

/// <summary>Część importera: repliki z arkuszy „Repliki” i „Repliki_sprzedane” (FR-508, 13.3).</summary>
public partial class ImporterKwatermistrz
{
    /// <summary>Nazwa kolumny w arkuszu „Repliki”.</summary>
    private const string ArkuszRepliki = "Repliki";
    private const string ArkuszReplikiSprzedane = "Repliki_sprzedane";

    private static void ImportujRepliki(KontekstImportu k, List<ArkuszXlsx> arkusze)
    {
        ImportujBlokReplik(k, arkusze, ArkuszRepliki, StatusWlasnosci.Posiadany, zSprzedanych: false);
        ImportujBlokReplik(k, arkusze, ArkuszReplikiSprzedane, StatusWlasnosci.Sprzedany, zSprzedanych: true);
        k.Podglad.LiczbaRekordow["Repliki"] = k.Dane.Repliki.Count;
    }

    private static void ImportujBlokReplik(KontekstImportu k, List<ArkuszXlsx> arkusze, string nazwaArkusza, StatusWlasnosci status, bool zSprzedanych)
    {
        var arkusz = ZnajdzArkusz(arkusze, nazwaArkusza);
        if (arkusz == null)
        {
            k.Podglad.DodajKonflikt(RodzajKonfliktu.DoPotwierdzenia, $"Nie znaleziono arkusza „{nazwaArkusza}”", "plik");
            return;
        }
        var naglowek = Naglowek(arkusz);
        if (naglowek == null)
            return;
        var kolumny = IndeksyKolumn(naglowek);

        foreach (var wiersz in arkusz.Wiersze.SkipWhile(w => w.Numer <= naglowek.Numer))
        {
            var nazwa = Normalizacja.Przytnij(wiersz.Komorka(kolumny.Nazwa));
            if (nazwa == null)
                continue;

            var replika = new Replika
            {
                ModelProducenta = nazwa,
                Prototyp = WyciagnijPrototyp(nazwa),
                Status = status,
                LicznikHistorycznyWypraw = CzytnikXlsx.LiczbaCalkowita(wiersz.Komorka(kolumny.Wyprawy)) ?? 0,
                ZnacznikZImportu = true,
            };
            UstawSlownikiRepliki(k, wiersz, kolumny, replika);
            ImportujKosztyRepliki(k, wiersz, kolumny, replika);
            PrzypiszDoRekonstrukcji(k, replika.StronyId, replika.RekonstrukcjeId);

            // SA-G12 (G36C): według autora (README, Q7) nadal w posiadaniu — status „do sprzedaży”, stan „W częściach” (13.3)
            var nazwaNorm = Normalizacja.TekstDoPorownania(nazwa);
            if (nazwaNorm.StartsWith("sa g12", StringComparison.Ordinal) || nazwaNorm.Contains("(g36c)", StringComparison.Ordinal))
            {
                replika.Status = StatusWlasnosci.DoSprzedazy;
                var stanWCzesciach = Model.Slowniki.Znajdz(k.Dane.Slowniki.StanyReplik, "W częściach");
                if (stanWCzesciach != null)
                    replika.StanId = stanWCzesciach.Id;
            }

            k.Dane.Repliki.Add(replika);
        }
    }

    private static (int Nazwa, int Wyprawy, int Magazynek, int Rola, int Strona, int Koszt, int CenaOryg, int KosztFormula, int KosztOrygFormula) IndeksyKolumn(WierszXlsx naglowek)
    {
        int Znajdz(params string[] wzorce)
        {
            for (var i = 0; i < naglowek.Komorki.Count; i++)
            {
                var komorka = naglowek.Komorka(i);
                if (komorka == null)
                    continue;
                var k = Normalizacja.TekstDoPorownania(komorka);
                if (wzorce.Any(w => k.Contains(w)))
                    return i;
            }
            return -1;
        }
        int ZnajdzDokladnie(string wzorzec)
        {
            for (var i = 0; i < naglowek.Komorki.Count; i++)
            {
                var komorka = naglowek.Komorka(i);
                if (komorka == null)
                    continue;
                if (Normalizacja.TekstDoPorownania(komorka) == wzorzec)
                    return i;
            }
            return -1;
        }
        return (
            Nazwa: Znajdz("nazwa"),
            Wyprawy: Znajdz("wyprawy", "liczba uzyc"),
            Magazynek: Znajdz("magazynek", "typ magazynka"),
            Rola: Znajdz("teren", "rola"),
            Strona: Znajdz("strona"),
            Koszt: ZnajdzDokladnie("koszt"),
            CenaOryg: ZnajdzDokladnie("cena oryginalna"),
            KosztFormula: Znajdz("skladniki"),
            KosztOrygFormula: Znajdz("skladniki ceny oryginalnej"));
    }

    private static void UstawSlownikiRepliki(KontekstImportu k, WierszXlsx wiersz, (int Nazwa, int Wyprawy, int Magazynek, int Rola, int Strona, int Koszt, int CenaOryg, int KosztFormula, int KosztOrygFormula) kol, Replika replika)
    {
        var slowniki = k.Dane.Slowniki;

        var magazynki = Normalizacja.Przytnij(wiersz.Komorka(kol.Magazynek));
        if (magazynki != null)
        {
            // może być kilka rozdzielonych przecinkiem/ukośnikiem (np. „AK 12/AK 74”)
            var czesci = magazynki.Split(new[] { ',', '/' }, StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).Where(c => c.Length > 0);
            foreach (var czesc in czesci)
            {
                var w = Model.Slowniki.Znajdz(slowniki.TypyMagazynkow, czesc);
                if (w != null && !replika.TypMagazynkaId?.Contains(w.Id) == true)
                    replika.TypMagazynkaId = w.Id;
            }
        }

        var rola = Normalizacja.Przytnij(wiersz.Komorka(kol.Rola));
        if (rola != null)
        {
            foreach (var czesc in rola.Split(new[] { '/', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).Where(c => c.Length > 0))
            {
                var w = Model.Slowniki.Znajdz(slowniki.Role, czesc);
                if (w != null && !replika.RoleId.Contains(w.Id))
                    replika.RoleId.Add(w.Id);
            }
        }

        var strona = Normalizacja.Przytnij(wiersz.Komorka(kol.Strona));
        if (strona != null)
        {
            foreach (var czesc in strona.Split(new[] { '/', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).Where(c => c.Length > 0))
            {
                var w = Model.Slowniki.Znajdz(slowniki.Strony, czesc);
                if (w != null && !replika.StronyId.Contains(w.Id))
                    replika.StronyId.Add(w.Id);
            }
        }

        // napęd: domyślnie AEG; HPA wykrywamy po znacznikach w karcie (ustawiane przy imporcie karty)
        var napedAeg = Model.Slowniki.Znajdz(slowniki.Napedy, "Elektryczna (AEG)");
        if (napedAeg != null)
            replika.NapedId = napedAeg.Id;
    }

    /// <summary>Rozbija formułę składników kosztu na pozycje bez opisu (FR-508). Zakup = pierwsza pozycja formuły (lub kolumna „Cena oryginalna”, gdy jej brak); kolejne dodatnie = „Ulepszenie”, ujemne = „Zwrot / rabat”. Gdy formuła niejednoznaczna („J9+70”) — tylko kolumna „Koszt” jako jedna pozycja „Inne”.</summary>
    private static void ImportujKosztyRepliki(KontekstImportu k, WierszXlsx wiersz, (int Nazwa, int Wyprawy, int Magazynek, int Rola, int Strona, int Koszt, int CenaOryg, int KosztFormula, int KosztOrygFormula) kol, Replika replika)
    {
        var formula = Normalizacja.Przytnij(wiersz.Komorka(kol.KosztFormula));
        var skladniki = Normalizacja.ParsujFormuleKosztu(formula);
        var kosztCalkowity = CzytnikXlsx.Liczba(wiersz.Komorka(kol.Koszt));
        var cenaOryg = CzytnikXlsx.Liczba(wiersz.Komorka(kol.CenaOryg));

        if (skladniki == null)
        {
            // bez formuły: zakup = cena oryginalna (BR-04); gdy jej brak lub zerowa — cały koszt jako „Inne”; różnica jako „Inne”
            if (kosztCalkowity is > 0 && cenaOryg is > 0)
            {
                replika.Koszty.Add(new PozycjaKosztowa { Kwota = cenaOryg.Value, Opis = "Cena oryginalna z arkusza", Semantyka = SemantykaKosztu.Zakup, RodzajId = IdRodzaju(k, SemantykaKosztu.Zakup), Zrodlo = "import" });
                if (cenaOryg != kosztCalkowity)
                    replika.Koszty.Add(new PozycjaKosztowa { Kwota = kosztCalkowity.Value - cenaOryg.Value, Opis = "Różnica kosztu z arkusza (bez opisu)", Semantyka = SemantykaKosztu.Inne, RodzajId = IdRodzaju(k, SemantykaKosztu.Inne), Zrodlo = "import" });
            }
            else if (kosztCalkowity is > 0)
            {
                replika.Koszty.Add(new PozycjaKosztowa { Kwota = kosztCalkowity.Value, Opis = "Koszt z arkusza", Semantyka = SemantykaKosztu.Inne, RodzajId = IdRodzaju(k, SemantykaKosztu.Inne), Zrodlo = "import" });
            }
            return;
        }

        // walidacja formuły: suma składników musi równać się kolumnie „Koszt”
        // liczby z Excela (double): porównanie z tolerancją, bo 110.49+49.51 = 160.00000000000003
        const decimal tolerancja = 0.005m;
        if (kosztCalkowity != null && Math.Abs(skladniki.Sum() - kosztCalkowity.Value) > tolerancja)
        {
            // formuła niezgodna z kolumną Koszt („J9+70” = 79 ≠ 1660) — jedna pozycja „Inne” z pełnym kosztem
            if (kosztCalkowity is > 0)
            {
                replika.Koszty.Add(new PozycjaKosztowa
                {
                    Kwota = kosztCalkowity.Value,
                    Opis = "Koszt z arkusza (formuła składników niezgodna)",
                    Semantyka = SemantykaKosztu.Inne,
                    RodzajId = IdRodzaju(k, SemantykaKosztu.Inne),
                    Zrodlo = "import",
                });
            }
            return;
        }

        // FR-508: pierwsza pozycja = „Zakup repliki”, kolejne dodatnie = „Ulepszenie”, ujemne = „Zwrot / rabat”.
        // Wyjątek: ujemne składniki bezpośrednio po pierwszym (np. „626-100-35”) to rabat na zakup — też „Zakup”,
        // bo cena oryginalna = 626-100-35 = 491 (BR-04, rozdział 8.2); „Zwrot” zostaje dla późniejszych zwrotów.
        var czyWGrupieZakupu = true;
        for (var i = 0; i < skladniki.Count; i++)
        {
            SemantykaKosztu semantyka;
            if (skladniki[i] < 0)
                semantyka = czyWGrupieZakupu ? SemantykaKosztu.Zakup : SemantykaKosztu.Zwrot;
            else
                semantyka = i == 0 ? SemantykaKosztu.Zakup : SemantykaKosztu.Ulepszenie;
            if (skladniki[i] > 0 && i > 0)
                czyWGrupieZakupu = false;
            replika.Koszty.Add(new PozycjaKosztowa
            {
                Kwota = skladniki[i],
                Opis = $"Pozycja z arkusza {i + 1} z {skladniki.Count}",
                Semantyka = semantyka,
                RodzajId = IdRodzaju(k, semantyka),
                Zrodlo = "import",
            });
        }
    }

    /// <summary>Id wartości słownika rodzajów kosztów wg semantyki (NFR-14).</summary>
    internal static string IdRodzaju(KontekstImportu k, SemantykaKosztu semantyka)
        => k.Dane.Slowniki.RodzajeKosztow.First(w => w.Semantyka == semantyka.ToString()).Id;

    /// <summary>Wyciąga prototyp z nazwy „model (prototyp)” — nawias na końcu.</summary>
    internal static string? WyciagnijPrototyp(string nazwa)
    {
        var idx = nazwa.LastIndexOf('(');
        if (idx > 0 && nazwa.EndsWith(')'))
            return nazwa[(idx + 1)..^1].Trim();
        return null;
    }
}
