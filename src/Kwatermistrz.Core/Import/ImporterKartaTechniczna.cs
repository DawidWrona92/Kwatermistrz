using System.Globalization;
using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Import;

/// <summary>Część importera: arkusz „Karta_techniczna” — karty, pomiary, zadania, wykrywanie HPA (FR-401..FR-411, FR-604, 13.3, 13.5).</summary>
public partial class ImporterKwatermistrz
{
    /// <summary>Nazwy parametrów karty w kolejności arkusza (FR-401) — indeks odpowiada kolumnie arkusza po kolumnie „Nazwa”.</summary>
    private static readonly string[] ParametryKarty =
    {
        "Body", "Lufa", "Gumka Hop-Up", "Komora Hop-Up", "Doszczelnienie", "Podkładkowanie", "Przewody",
        "Łożyska", "Zębatki", "Prowadnica sprężyny", "Tłok", "Głowica tłoka", "Cylinder",
        "Głowica cylindra", "Dysza", "Silnik", "MOSFET", "Wtyczka", "Sprężyna główna (aktualna)",
        "Kulki docelowe",
    };

    /// <summary>Indeksy kolumn arkusza: 0=Grupa, 1=Kolumna w arkuszu, 2=Nazwa, 3=Stan, 4..23=parametry, dalej Joule/Zasięg/Test/Teren/Magazynki/Uwagi.</summary>
    private const int KolumnaNazwa = 2;
    private const int KolumnaStan = 3;
    private const int KolumnaPierwszyParametr = 4;
    private const int KolumnaJoule = 24;
    private const int KolumnaZasieg = 25;
    private const int KolumnaTest = 26;
    private const int KolumnaTeren = 27;
    private const int KolumnaMagazynki = 28;
    private const int KolumnaUwaga1 = 29;
    private const int KolumnaUwaga2 = 30;

    private static void ImportujKartyTechniczne(KontekstImportu k, List<ArkuszXlsx> arkusze)
    {
        var arkusz = ZnajdzArkusz(arkusze, "Karta_techniczna");
        if (arkusz == null)
        {
            k.Podglad.DodajKonflikt(RodzajKonfliktu.DoPotwierdzenia, "Nie znaleziono arkusza „Karta_techniczna”", "plik");
            return;
        }
        var naglowek = Naglowek(arkusz);
        if (naglowek == null)
            return;

        var wiersze = arkusz.Wiersze.SkipWhile(w => w.Numer <= naglowek.Numer).ToList();
        var karty = new List<(WierszXlsx Wiersz, string Nazwa, bool CzyHpa)>();

        // najpierw zbierz wszystkie wiersze kart, żeby wykryć duplikaty (13.3)
        foreach (var wiersz in wiersze)
        {
            var nazwa = Normalizacja.Przytnij(wiersz.Komorka(KolumnaNazwa));
            if (nazwa == null)
                continue;
            var czyHpa = WykryjHpa(wiersz);
            karty.Add((wiersz, nazwa, czyHpa));
        }

        // grupuj karty po dopasowanej replice (13.3: te same repliki mogą mieć różne zapisy nazwy, np. „LCT AK-74 (AK-74M)” vs „LCT AK-74 (AK-74)”)
        var grupy = new List<List<(WierszXlsx Wiersz, string Nazwa, bool CzyHpa, Replika? Replika)>>();
        foreach (var karta in karty)
        {
            var replika = DopasujKarteDoRepliki(k, karta.Nazwa);
            var grupa = grupy.FirstOrDefault(g =>
                (replika != null && g[0].Replika is { } pierwszy && pierwszy.Id == replika.Id) ||
                (replika == null && g[0].Replika == null && Normalizacja.TekstDoPorownania(g[0].Nazwa) == Normalizacja.TekstDoPorownania(karta.Nazwa)));
            if (grupa == null)
                grupy.Add(new() { (karta.Wiersz, karta.Nazwa, karta.CzyHpa, replika) });
            else
                grupa.Add((karta.Wiersz, karta.Nazwa, karta.CzyHpa, replika));
        }
        foreach (var grupa in grupy)
        {
            var wybrany = grupa.Count > 1 ? WybierzPelnaKarte(grupa.Select(g => (g.Wiersz, g.Nazwa, g.CzyHpa)).ToList()) : (grupa[0].Wiersz, grupa[0].Nazwa, grupa[0].CzyHpa);
            if (grupa.Count > 1)
            {
                var konflikt = k.Podglad.DodajKonflikt(
                    RodzajKonfliktu.Duplikat,
                    $"Replika „{grupa.First().Nazwa}” występuje {grupa.Count} razy w karcie technicznej. Wybrano kolumnę z pełniejszymi danymi (Q8 domyślnie).",
                    $"Karta_techniczna: {string.Join(", ", grupa.Select(g => g.Wiersz.Numer))}");
                konflikt.Kandydaci = grupa.Select(g => $"wiersz {g.Wiersz.Numer}: {g.Nazwa}").ToList();
                konflikt.DomyslnyWybor = grupa.FindIndex(g => g.Wiersz.Numer == wybrany.Wiersz.Numer);
                // FR-1102: wybór użytkownika nadpisuje domyślny (Q8)
                var wybor = konflikt.Decyzja;
                if (wybor >= 0 && wybor < grupa.Count)
                    wybrany = (grupa[wybor].Wiersz, grupa[wybor].Nazwa, grupa[wybor].CzyHpa);
            }
            // zadania stosujemy zawsze z wybranej (pełniejszej) karty — duplikaty się nie dublują
            ZastosujKarte(k, wybrany.Wiersz, wybrany.Nazwa, wybrany.CzyHpa);
        }
        k.Podglad.LiczbaRekordow["Karty techniczne"] = karty.Count;
    }

    /// <summary>Wykrywa HPA po znacznikach „Nie dotyczy HPA” w polach mechanizmu (13.5).</summary>
    private static bool WykryjHpa(WierszXlsx wiersz)
    {
        for (var i = KolumnaPierwszyParametr; i < KolumnaJoule && i < wiersz.Komorki.Count; i++)
            if (Normalizacja.CzyZnacznikNieDotyczy(wiersz.Komorka(i)))
                return true;
        return false;
    }

    /// <summary>Wybiera spośród duplikatów kartę z największą liczbą wypełnionych pól (Q8 domyślnie: pełniejsza).</summary>
    private static (WierszXlsx Wiersz, string Nazwa, bool CzyHpa) WybierzPelnaKarte(List<(WierszXlsx Wiersz, string Nazwa, bool CzyHpa)> kandydaci)
    {
        return kandydaci
            .OrderByDescending(g => g.Wiersz.Komorki.Count(c => !string.IsNullOrWhiteSpace(c) && !Normalizacja.CzyZnacznikNieznane(c) && !Normalizacja.CzyZnacznikNieDotyczy(c)))
            .First();
    }

    private static void ZastosujKarte(KontekstImportu k, WierszXlsx wiersz, string nazwaKarty, bool czyHpa)
    {
        // dopasuj kartę do repliki z listy (FR-1103, tolerancja na różnice zapisu 13.3)
        var replika = DopasujKarteDoRepliki(k, nazwaKarty);
        if (replika == null)
        {
            // karta bez repliki na liście — tworzymy replikę-szkic (FR-706)
            replika = new Replika
            {
                ModelProducenta = nazwaKarty,
                Prototyp = WyciagnijPrototyp(nazwaKarty),
                Status = StatusWlasnosci.Posiadany,
                ZnacznikZImportu = true,
                DoUzupelnienia = true,
            };
            var naped = Model.Slowniki.Znajdz(k.Dane.Slowniki.Napedy, "Elektryczna (AEG)");
            if (naped != null)
                replika.NapedId = naped.Id;
            k.Dane.Repliki.Add(replika);
            k.Podglad.DodajKonflikt(RodzajKonfliktu.BrakOdpowiednika,
                $"Karta techniczna „{nazwaKarty}” nie ma odpowiednika na liście replik — utworzono replikę-szkic do uzupełnienia",
                $"Karta_techniczna wiersz {wiersz.Numer}");
        }

        if (czyHpa)
        {
            var napedHpa = Model.Slowniki.Znajdz(k.Dane.Slowniki.Napedy, "HPA");
            if (napedHpa != null)
                replika.NapedId = napedHpa.Id;
        }
        replika.SzablonKartyId = czyHpa
            ? k.Dane.SzablonyKart.FirstOrDefault(s => s.Nazwa == "HPA")?.Id
            : k.Dane.SzablonyKart.FirstOrDefault(s => s.Nazwa == "AEG")?.Id;

        // stan repliki (kolumna 3) — nie nadpisujemy stanu ustawionego specjalnie przy imporcie listy (np. SA-G12 „W częściach”, 13.3)
        var stan = Normalizacja.Przytnij(wiersz.Komorka(KolumnaStan));
        if (stan != null && replika.Status != StatusWlasnosci.DoSprzedazy)
        {
            var w = Model.Slowniki.Znajdz(k.Dane.Slowniki.StanyReplik, stan);
            if (w != null)
                replika.StanId = w.Id;
        }
        // SA-G12: wymuś stan „W częściach” nawet gdy karta ma „Do zrobienia” (Q7, 13.3)
        if (replika.Status == StatusWlasnosci.DoSprzedazy && Normalizacja.TekstDoPorownania(replika.ModelProducenta).Contains("g36c"))
        {
            var w = Model.Slowniki.Znajdz(k.Dane.Slowniki.StanyReplik, "W częściach");
            if (w != null)
                replika.StanId = w.Id;
        }

        // parametry karty (kolumny 4..23)
        var parametrySzablonu = k.Dane.SzablonyKart.First(s => s.Id == replika.SzablonKartyId).Parametry;
        for (var i = 0; i < ParametryKarty.Length; i++)
        {
            var parametr = parametrySzablonu.FirstOrDefault(p => Normalizacja.TekstDoPorownania(p.Nazwa) == Normalizacja.TekstDoPorownania(ParametryKarty[i]));
            if (parametr == null)
                continue;
            var wartosc = Normalizacja.Przytnij(wiersz.Komorka(KolumnaPierwszyParametr + i));
            replika.KartaTechniczna[parametr.Id] = MapujWartoscPola(wartosc, czyHpa);
        }

        // rola i magazynki (kolumny Teren, Typ magazynków)
        var rola = Normalizacja.Przytnij(wiersz.Komorka(KolumnaTeren));
        if (rola != null)
            foreach (var czesc in rola.Split(new[] { '/', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var w = Model.Slowniki.Znajdz(k.Dane.Slowniki.Role, czesc);
                if (w != null && !replika.RoleId.Contains(w.Id))
                    replika.RoleId.Add(w.Id);
            }
        var mag = Normalizacja.Przytnij(wiersz.Komorka(KolumnaMagazynki));
        if (mag != null && replika.TypMagazynkaId == null)
        {
            var w = Model.Slowniki.Znajdz(k.Dane.Slowniki.TypyMagazynkow, mag);
            if (w != null)
                replika.TypMagazynkaId = w.Id;
        }

        ImportujPomiar(k, wiersz, replika);
        ImportujZadania(k, wiersz, replika);
    }

    /// <summary>Mapuje tekst komórki na wartość pola z trzema stanami (FR-404, 13.5).</summary>
    internal static WartoscPolaKarty MapujWartoscPola(string? tekst, bool czyHpa)
    {
        if (tekst == null)
            return new WartoscPolaKarty { Stan = StanWartosciPola.NieUzupelnione };
        if (Normalizacja.CzyZnacznikNieznane(tekst))
            return new WartoscPolaKarty { Stan = StanWartosciPola.Nieznane };
        if (Normalizacja.CzyZnacznikNieDotyczy(tekst))
            return new WartoscPolaKarty { Stan = StanWartosciPola.NieDotyczy };
        return new WartoscPolaKarty { Tekst = tekst, Stan = StanWartosciPola.Wypelnione };
    }

    /// <summary>Dopasowuje nazwę z karty do repliki z listy (13.3: tolerancja na „Custom (AK-74M Plum)” vs „Custom (AK-74 Plum)”, „CXP-APE (MASADA ACR)” vs „CXP-APE-L”, „VSS Wintorez(Wintorez LCT)”). Kolejność: dokładna nazwa, wszystkie słowa kluczowe (przy kilku kandydatów pierwszy), tolerancja n-1 słów (gdy jeden kandydat), dokładny model bez nawiasów (gdy jednoznaczny).</summary>
    private static Replika? DopasujKarteDoRepliki(KontekstImportu k, string nazwaKarty)
    {
        var repliki = k.Dane.Repliki;
        var nazwaNorm = Normalizacja.TekstDoPorownania(nazwaKarty);

        // 1. dokładna pełna nazwa
        var dokladne = repliki.Where(r => Normalizacja.TekstDoPorownania(r.ModelProducenta) == nazwaNorm).ToList();
        if (dokladne.Count > 0)
            return dokladne[0];

        // 2. wszystkie słowa kluczowe pełnej nazwy
        var slowa = Dopasowanie.SlowaKluczowe(nazwaKarty);
        if (slowa.Count > 0)
        {
            int Trafienia(Replika r) => slowa.Count(w => Normalizacja.TekstDoPorownania(r.ModelProducenta).Contains(w, StringComparison.Ordinal));
            var pelne = repliki.Where(r => Trafienia(r) == slowa.Count).ToList();
            if (pelne.Count > 0)
                return pelne[0];
            // 3. tolerancja: n-1 słów, tylko gdy dokładnie jeden kandydat
            if (slowa.Count > 1)
            {
                var prawie = repliki.Where(r => Trafienia(r) >= slowa.Count - 1).ToList();
                if (prawie.Count == 1)
                    return prawie[0];
            }
        }

        // 4. dokładny model bez nawiasów — tylko gdy jednoznaczny
        var bezNawiasow = nazwaKarty;
        var idx = bezNawiasow.IndexOf('(');
        if (idx > 0)
            bezNawiasow = bezNawiasow[..idx].Trim();
        var modelNorm = Normalizacja.TekstDoPorownania(bezNawiasow);
        var poModelu = repliki.Where(r =>
        {
            var m = r.ModelProducenta;
            var i = m.IndexOf('(');
            var model = i > 0 ? m[..i].Trim() : m;
            return Normalizacja.TekstDoPorownania(model) == modelNorm;
        }).ToList();
        return poModelu.Count == 1 ? poModelu[0] : null;
    }

    /// <summary>Importuje wpis pomiaru z kolumn Joule/Zasięg/Testowane działanie (FR-408, FR-411: serduszko = „na maksimum”, ustawia energię docelową).</summary>
    private static void ImportujPomiar(KontekstImportu k, WierszXlsx wiersz, Replika replika)
    {
        var jouleTekst = Normalizacja.Przytnij(wiersz.Komorka(KolumnaJoule));
        var zasiegTekst = Normalizacja.Przytnij(wiersz.Komorka(KolumnaZasieg));
        var testTekst = wiersz.Komorka(KolumnaTest);
        var dataTestu = CzytnikXlsx.Data(testTekst);
        var energia = ParsujJoule(jouleTekst, out var naMaksimum);
        var zasieg = CzytnikXlsx.LiczbaCalkowita(zasiegTekst?.Replace(" m", "").Replace("m", ""));

        if (energia == null && zasieg == null && dataTestu == null && !naMaksimum)
            return;

        var pomiar = new Pomiar
        {
            Data = dataTestu ?? k.Podglad.DzienZerowy,
            EnergiaJ = energia,
            ZasiegM = zasieg,
            NaMaksimum = naMaksimum,
            Wynik = WynikTestu.Sprawna,
        };
        replika.Pomiary.Add(pomiar);

        if (naMaksimum && energia != null)
            replika.EnergiaDocelowaJ = energia;
    }

    /// <summary>Parsuje energię „2.6 ❤”, „1,9J ❤”, „2,2 J  ❤” — serduszko oznacza „na maksimum” (Q2, FR-411).</summary>
    internal static decimal? ParsujJoule(string? tekst, out bool naMaksimum)
    {
        naMaksimum = false;
        if (tekst == null)
            return null;
        naMaksimum = tekst.Contains('❤') || tekst.Contains('♥');
        var t = tekst.Replace("❤", "").Replace("♥", "").Replace("️", "").Replace("J", "").Replace("j", "").Trim();
        return CzytnikXlsx.Liczba(t);
    }

    /// <summary>Dwa wiersze notatek stają się zadaniami; rodzaj podpowiadany słowami kluczowymi (FR-604).</summary>
    private static void ImportujZadania(KontekstImportu k, WierszXlsx wiersz, Replika replika)
    {
        foreach (var kolumna in new[] { KolumnaUwaga1, KolumnaUwaga2 })
        {
            var opis = Normalizacja.Przytnij(wiersz.Komorka(kolumna));
            if (opis == null)
                continue;
            var semantyka = PodpowiedzRodzajZadania(opis);
            k.Dane.Zadania.Add(new Zadanie
            {
                DotyczyTyp = "Replika",
                DotyczyId = replika.Id,
                Opis = opis,
                Semantyka = semantyka,
                RodzajId = k.Dane.Slowniki.RodzajeZadan.First(z => z.Semantyka == semantyka.ToString()).Id,
                DataUtworzenia = k.Podglad.DzienZerowy,
                ZnacznikZImportu = true,
            });
        }
        k.Podglad.LiczbaRekordow["Zadania"] = k.Dane.Zadania.Count;
    }

    /// <summary>Podpowiada rodzaj zadania po słowach kluczowych (FR-604): „awaria/złamana” → awaria, „kupić” → do kupienia, reszta → do zrobienia.</summary>
    internal static SemantykaZadania PodpowiedzRodzajZadania(string opis)
    {
        var t = Normalizacja.TekstDoPorownania(opis);
        if (t.Contains("awaria") || t.Contains("zlamana") || t.Contains("zlama") || t.Contains("zepsut"))
            return SemantykaZadania.Awaria;
        if (t.StartsWith("kupi", StringComparison.Ordinal) || t.Contains(" kupic ") || t.EndsWith(" kupic", StringComparison.Ordinal))
            return SemantykaZadania.DoKupienia;
        return SemantykaZadania.DoZrobienia;
    }
}
