using Kwatermistrz.Core.Konfiguracja;
using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Tests;

/// <summary>Testy pakietu konfiguracji (FR-1404, FR-1003): eksport, import z podglądem, scalanie słowników z aliasem.</summary>
public class PakietKonfiguracjiTests : IDisposable
{
    private readonly string _katalog;

    public PakietKonfiguracjiTests()
    {
        _katalog = Path.Combine(Path.GetTempPath(), "kwatermistrz-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_katalog);
    }

    public void Dispose()
    {
        if (Directory.Exists(_katalog))
            Directory.Delete(_katalog, recursive: true);
    }

    private static DaneAplikacji DaneStartowe()
    {
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        dane.SzablonyKart.Add(StartoweSlowniki.SzablonKartyAeg(dane.Slowniki));
        return dane;
    }

    [Fact]
    public void Eksport_Zapisz_Wczytaj_Roundtrip_ZPolakimiZnakami()
    {
        var dane = DaneStartowe();
        dane.Rekonstrukcje.Add(new Rekonstrukcja
        {
            Nazwa = "Współczesna Rosja",
            Okres = "2020–2025",
            Opis = "Strzelcy z 6B47",
            SzablonSlotow = new()
            {
                new SlotSzablonu { Nazwa = "Mundur", Kolejnosc = 0, CzySlotMunduru = true },
                new SlotSzablonu { Nazwa = "Broń szturmowa", Kolejnosc = 1, CzySlotBroniGlownej = true },
            },
        });
        dane.Slowniki.Kategorie.Add(new Kategoria { Nazwa = "Ładownice", CzyNosnik = true, Kolejnosc = 99 });
        dane.Slowniki.Wzory.Add(new WzorKamuflazu { Nazwa = "EMR Letni", KoloryProbki = new() { "#4A5D3A" } });

        var pakiet = PakietKonfiguracji.Eksportuj(dane, "Pakiet testowy");
        var sciezka = Path.Combine(_katalog, "pakiet.kwpakiet");
        PakietKonfiguracji.ZapiszDoPliku(pakiet, sciezka);

        var wczytany = PakietKonfiguracji.WczytajZPliku(sciezka);

        Assert.Equal(PakietKonfiguracji.TypPakietu, wczytany.Typ);
        Assert.Equal("Pakiet testowy", wczytany.Nazwa);
        Assert.Single(wczytany.Rekonstrukcje);
        Assert.Equal(2, wczytany.Rekonstrukcje[0].SzablonSlotow.Count);
        Assert.Contains(wczytany.Slowniki, w => w.Slownik == "Strony" && w.Wartosc.Nazwa == "Rosja");
        Assert.Contains(wczytany.Wzory, w => w.Nazwa == "EMR Letni");
        Assert.Contains(wczytany.Kategorie, k => k.Nazwa == "Ładownice" && k.CzyNosnik);
        Assert.Single(wczytany.SzablonyKart);
        Assert.Empty(wczytany.Wzory.SelectMany(w => w.KoloryProbki).Where(k => k.Length == 0));
    }

    [Fact]
    public void Eksport_NieZawieraDanychOSprzecie()
    {
        var dane = DaneStartowe();
        dane.Przedmioty.Add(new Przedmiot { Nazwa = "Gorka 3", CenaZakupu = 500m });
        dane.Repliki.Add(new Replika { ModelProducenta = "AK-74M" });
        dane.Wyprawy.Add(new Wyprawa { Data = new DateOnly(2025, 5, 1) });
        dane.Zestawy.Add(new Zestaw { Nazwa = "Zestaw letni" });

        var pakiet = PakietKonfiguracji.Eksportuj(dane, null);

        // pakiet nie ma pól na dane o sprzęcie — rekonstrukcje zawierają tylko konfigurację
        var json = System.Text.Json.JsonSerializer.Serialize(pakiet);
        Assert.DoesNotContain("AK-74M", json);
        Assert.DoesNotContain("Gorka", json);
        Assert.DoesNotContain("Zestaw letni", json);
    }

    [Fact]
    public void Podglad_Domyslnie_ScalaWartosciPoNazwie()
    {
        var dane = DaneStartowe();
        var pakiet = PakietKonfiguracji.Eksportuj(dane, null);
        // w pakiecie zmieniamy nazwę jednej strony, żeby wymusić „dodaj nową"
        pakiet.Slowniki.First(w => w.Slownik == "Strony" && w.Wartosc.Nazwa == "Rosja").Wartosc.Nazwa = "Federacja Rosyjska";

        var podglad = ImporterPakietuKonfiguracji.PrzygotujPodglad(DaneStartowe(), pakiet);

        var strony = podglad.Slowniki.Single(s => s.Rodzaj.Klucz == "Strony").Decyzje;
        var rosja = strony.Single(d => d.NazwaPakietu == "Federacja Rosyjska");
        Assert.Equal(AkcjaWartosci.DodajNowa, rosja.Akcja);
        Assert.Null(rosja.Istniejaca);
        var polska = strony.Single(d => d.NazwaPakietu == "Polska");
        Assert.Equal(AkcjaWartosci.ScalZIstniejaca, polska.Akcja);
        Assert.NotNull(polska.Istniejaca);
    }

    [Fact]
    public void Zastosuj_Scalanie_DodajeAliasIPrzenosiSemantyke()
    {
        var dane = DaneStartowe();
        var pakiet = new PakietKonfiguracji();
        var idIstniejacej = dane.Slowniki.Role.Single(r => r.Nazwa == "Wsparcie").Id;
        pakiet.Slowniki.Add(new WartoscSlownikaPakietu
        {
            Slownik = "Role",
            Wartosc = new WartoscSlownika { Nazwa = "Wsparcie", Aliasy = new() { "LKM", "MG" } },
        });
        pakiet.Slowniki.Add(new WartoscSlownikaPakietu
        {
            Slownik = "RodzajeKosztow",
            Wartosc = new WartoscSlownika { Nazwa = "Zakup repliki", Semantyka = nameof(SemantykaKosztu.Zakup) },
        });

        var podglad = ImporterPakietuKonfiguracji.PrzygotujPodglad(dane, pakiet);
        ImporterPakietuKonfiguracji.Zastosuj(dane, podglad);

        var wsparcie = dane.Slowniki.Role.Single(r => r.Nazwa == "Wsparcie");
        Assert.Contains("MG", wsparcie.Aliasy); // nowy alias z pakietu (FR-1003)
        Assert.Equal("MG", wsparcie.Aliasy.Single(a => a == "MG"));
        Assert.Single(dane.Slowniki.Role.Where(r => r.Nazwa == "Wsparcie"));
        Assert.Equal(nameof(SemantykaKosztu.Zakup), dane.Slowniki.RodzajeKosztow.Single(r => r.Nazwa == "Zakup repliki").Semantyka);
        Assert.Equal(idIstniejacej, wsparcie.Id); // Id lokalne zachowane
    }

    [Fact]
    public void Zastosuj_AliasJestRozpoznawanyPrzyKolejnymImporcie()
    {
        var dane = DaneStartowe();
        var pakiet = new PakietKonfiguracji();
        pakiet.Slowniki.Add(new WartoscSlownikaPakietu
        {
            Slownik = "Role",
            Wartosc = new WartoscSlownika { Nazwa = "Wsparcie MG", Aliasy = new() { "MG" } },
        });
        ImporterPakietuKonfiguracji.Zastosuj(dane, ImporterPakietuKonfiguracji.PrzygotujPodglad(dane, pakiet));
        var dodana = dane.Slowniki.Role.Single(r => r.Nazwa == "Wsparcie MG");
        Assert.Contains("MG", dodana.Aliasy);

        // drugi import: wartość o nazwie „MG” (alias) ma być rozpoznana jako istniejąca
        var pakiet2 = new PakietKonfiguracji();
        pakiet2.Slowniki.Add(new WartoscSlownikaPakietu
        {
            Slownik = "Role",
            Wartosc = new WartoscSlownika { Nazwa = "MG" },
        });
        var podglad2 = ImporterPakietuKonfiguracji.PrzygotujPodglad(dane, pakiet2);
        var decyzja = podglad2.Slowniki.Single(s => s.Rodzaj.Klucz == "Role").Decyzje.Single();
        Assert.Equal(AkcjaWartosci.ScalZIstniejaca, decyzja.Akcja);
        Assert.Equal(dodana.Id, decyzja.Istniejaca!.Id);
    }

    [Fact]
    public void Zastosuj_DodajeRekonstrukcjeISzablonKartyZMapowaniemNapedow()
    {
        var dane = DaneStartowe();
        var napedHpa = dane.Slowniki.Napedy.Single(n => n.Nazwa == "HPA");
        var pakiet = new PakietKonfiguracji();
        pakiet.Slowniki.Add(new WartoscSlownikaPakietu
        {
            Slownik = "Strony",
            Wartosc = new WartoscSlownika { Nazwa = "NATO" },
        });
        pakiet.Slowniki.Add(new WartoscSlownikaPakietu
        {
            Slownik = "Napedy",
            Wartosc = new WartoscSlownika { Id = "pakiet-naped-hpa", Nazwa = "HPA" },
        });
        pakiet.Rekonstrukcje.Add(new RekonstrukcjaPakietu
        {
            Nazwa = "101. Dywizja",
            StronaId = pakiet.Slowniki.First(w => w.Slownik == "Strony").Wartosc.Id,
            SzablonSlotow = new()
            {
                new SlotSzablonu { Nazwa = "Mundur", Kolejnosc = 0, CzySlotMunduru = true },
            },
        });
        pakiet.SzablonyKart.Add(new SzablonKarty
        {
            Nazwa = "HPA wysokiego ciśnienia",
            NapedId = "pakiet-naped-hpa",
            Parametry = new() { new ParametrKarty { Nazwa = "Ciśnienie robocze", Kolejnosc = 0 } },
        });

        var podglad = ImporterPakietuKonfiguracji.PrzygotujPodglad(dane, pakiet);
        // domyślnie: NATO już istnieje → scalanie; HPA też istnieje → scalanie
        ImporterPakietuKonfiguracji.Zastosuj(dane, podglad);

        var rek = Assert.Single(dane.Rekonstrukcje);
        Assert.Equal("101. Dywizja", rek.Nazwa);
        Assert.Equal(dane.Slowniki.Strony.Single(s => s.Nazwa == "NATO").Id, rek.StronaId);
        Assert.Single(rek.SzablonSlotow);
        var szablon = Assert.Single(dane.SzablonyKart, s => s.Nazwa == "HPA wysokiego ciśnienia");
        Assert.Equal(napedHpa.Id, szablon.NapedId);
        Assert.Single(szablon.Parametry);
    }

    [Fact]
    public void Zastosuj_ScalSzablony_DopisujeBrakujaceSloty()
    {
        var dane = DaneStartowe();
        dane.Rekonstrukcje.Add(new Rekonstrukcja
        {
            Nazwa = "Rosja",
            SzablonSlotow = new() { new SlotSzablonu { Nazwa = "Mundur", Kolejnosc = 0, CzySlotMunduru = true } },
        });
        var pakiet = new PakietKonfiguracji();
        pakiet.Rekonstrukcje.Add(new RekonstrukcjaPakietu
        {
            Nazwa = "Rosja",
            SzablonSlotow = new()
            {
                new SlotSzablonu { Nazwa = "Mundur", Kolejnosc = 0, CzySlotMunduru = true },
                new SlotSzablonu { Nazwa = "Kamizelka", Kolejnosc = 1 },
            },
        });

        var podglad = ImporterPakietuKonfiguracji.PrzygotujPodglad(dane, pakiet);
        var decyzja = Assert.Single(podglad.Rekonstrukcje);
        decyzja.Akcja = AkcjaRekonstrukcji.ScalSzablony;
        ImporterPakietuKonfiguracji.Zastosuj(dane, podglad);

        var rek = Assert.Single(dane.Rekonstrukcje);
        Assert.Equal(2, rek.SzablonSlotow.Count);
        Assert.Contains(rek.SzablonSlotow, s => s.Nazwa == "Kamizelka");
    }

    [Fact]
    public void Zastosuj_PowtorzenieNieTworzyDuplikatow()
    {
        var dane = DaneStartowe();
        var pakiet = PakietKonfiguracji.Eksportuj(dane, null);
        pakiet.Slowniki.Add(new WartoscSlownikaPakietu
        {
            Slownik = "Strony",
            Wartosc = new WartoscSlownika { Nazwa = "Niemcy" },
        });

        ImporterPakietuKonfiguracji.Zastosuj(dane, ImporterPakietuKonfiguracji.PrzygotujPodglad(dane, pakiet));
        var liczbaStron = dane.Slowniki.Strony.Count;
        var liczbaRekonstrukcji = dane.Rekonstrukcje.Count;

        ImporterPakietuKonfiguracji.Zastosuj(dane, ImporterPakietuKonfiguracji.PrzygotujPodglad(dane, pakiet));

        Assert.Equal(liczbaStron, dane.Slowniki.Strony.Count);
        Assert.Equal(liczbaRekonstrukcji, dane.Rekonstrukcje.Count);
    }

    [Fact]
    public void WczytajZPliku_ZlyTyp_RzucaWyjatek()
    {
        var sciezka = Path.Combine(_katalog, "zly.kwpakiet");
        File.WriteAllText(sciezka, """{"Typ":"Inny","WersjaPakietu":1}""");
        Assert.Throws<InvalidDataException>(() => PakietKonfiguracji.WczytajZPliku(sciezka));
    }

    [Fact]
    public void WczytajZPliku_NowszaWersja_RzucaWyjatek()
    {
        var sciezka = Path.Combine(_katalog, "nowszy.kwpakiet");
        File.WriteAllText(sciezka, $$"""{"Typ":"{{PakietKonfiguracji.TypPakietu}}","WersjaPakietu":{{PakietKonfiguracji.AktualnaWersjaPakietu + 1}}}""");
        Assert.Throws<InvalidDataException>(() => PakietKonfiguracji.WczytajZPliku(sciezka));
    }
}
