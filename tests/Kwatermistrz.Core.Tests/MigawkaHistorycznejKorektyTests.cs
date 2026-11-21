using System.Text;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Obliczenia;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.Core.Tests;

public class MigawkaHistorycznejKorektyTests
{
    private static (DaneAplikacji Dane, Wyprawa Wyprawa, Replika Replika,
        Przedmiot Pierwszy, Przedmiot Drugi, WariantSlotu Wariant) Przyklad()
    {
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        dane.Ustawienia.DzienZerowy = new DateOnly(2022, 12, 31);
        dane.Ustawienia.HistoryczneWyprawyWgRok[2021] = 5;
        var replika = new Replika { Id = "replika", LicznikHistorycznyWypraw = 4 };
        var pierwszy = new Przedmiot { Id = "mundur-a", LicznikHistoryczny = 3 };
        var drugi = new Przedmiot { Id = "mundur-b", LicznikHistoryczny = 8 };
        dane.Repliki.Add(replika);
        dane.Przedmioty.AddRange(new[] { pierwszy, drugi });
        var rekonstrukcja = new Rekonstrukcja
        {
            Id = "rekonstrukcja", Nazwa = "Rek",
            SzablonSlotow = new() { new SlotSzablonu
            {
                Id = "szablon-munduru", Nazwa = "Mundur", CzySlotMunduru = true,
            } },
        };
        dane.Rekonstrukcje.Add(rekonstrukcja);
        var wariant = new WariantSlotu
        {
            Id = "wariant",
            Elementy = new() { new ElementWariantu { Typ = TypElementu.Przedmiot, ElementId = pierwszy.Id } },
        };
        dane.Zestawy.Add(new Zestaw
        {
            Id = "zestaw", RekonstrukcjaId = rekonstrukcja.Id,
            Sloty = new() { new SlotZestawu
            {
                Id = "slot", SzablonSlotId = "szablon-munduru",
                Warianty = new() { wariant },
            } },
        });
        var wyprawa = new Wyprawa
        {
            Id = "wyprawa", Data = new DateOnly(2021, 4, 3),
            ReplikaGlownaId = replika.Id, ZestawId = "zestaw",
            RekonstrukcjaId = rekonstrukcja.Id,
            WybraneWarianty = new() { ["slot"] = wariant.Id },
        };
        return (dane, wyprawa, replika, pierwszy, drugi, wariant);
    }

    [Fact]
    public void NowaKorekta_ZapamietujeReplikeMundurIRok_PozniejZmianaZestawuJejNiePrzepisuje()
    {
        var (dane, wyprawa, replika, pierwszy, drugi, wariant) = Przyklad();
        var operacje = new OperacjeNaWyprawach(dane);
        Assert.True(operacje.SprobujDodac(wyprawa, true, out var blad), blad);
        Assert.Equal(3, wyprawa.SkladnikiKorekty?.Count);
        Assert.Contains(wyprawa.SkladnikiKorekty!, s => s.Rodzaj == "Mundur" && s.Id == pierwszy.Id);
        Assert.Equal(3, replika.LicznikHistorycznyWypraw);
        Assert.Equal(2, pierwszy.LicznikHistoryczny);
        Assert.Equal(8, drugi.LicznikHistoryczny);
        Assert.Equal(4, dane.Ustawienia.HistoryczneWyprawyWgRok[2021]);

        // Użytkownik edytuje obecny zestaw. To nie jest zmiana składu wyprawy w przeszłości.
        wariant.Elementy[0].ElementId = drugi.Id;
        var szkic = OperacjeNaWyprawach.Kopia(wyprawa);
        szkic.Komentarz = "Opis po latach";
        Assert.True(operacje.SprobujZapisac(wyprawa, szkic, out blad), blad);
        Assert.Equal(2, pierwszy.LicznikHistoryczny);
        Assert.Equal(8, drugi.LicznikHistoryczny);
        Assert.Equal("Opis po latach", wyprawa.Komentarz);

        Assert.True(operacje.SprobujUsun(wyprawa, out blad), blad);
        Assert.Equal(3, pierwszy.LicznikHistoryczny);
        Assert.Equal(8, drugi.LicznikHistoryczny);
        Assert.Equal(4, replika.LicznikHistorycznyWypraw);
        Assert.Equal(5, dane.Ustawienia.HistoryczneWyprawyWgRok[2021]);

        Assert.True(operacje.SprobujPrzywroc(wyprawa, false, out blad), blad);
        Assert.Equal(2, pierwszy.LicznikHistoryczny);
        Assert.Equal(8, drugi.LicznikHistoryczny);
        Assert.Equal(3, replika.LicznikHistorycznyWypraw);
        Assert.Equal(4, dane.Ustawienia.HistoryczneWyprawyWgRok[2021]);
    }

    [Fact]
    public void MigawkaHistoryczna_PrzetrwaZapisIWczytanieStaregoFormatuSchemaVersion1()
    {
        var (dane, wyprawa, _, _, _, _) = Przyklad();
        Assert.True(new OperacjeNaWyprawach(dane).SprobujDodac(wyprawa, true, out var blad), blad);
        using var strumien = new MemoryStream();
        MagazynJson.ZapiszDoStrumienia(dane, strumien);
        strumien.Position = 0;
        var poRestarcie = MagazynJson.WczytajZeStrumienia(strumien);
        Assert.Equal(1, poRestarcie.SchemaVersion);
        var przywrocona = Assert.Single(poRestarcie.Wyprawy);
        Assert.Equal(3, przywrocona.SkladnikiKorekty?.Count);
        Assert.Equal("mundur-a", przywrocona.SkladnikiKorekty!.Single(s => s.Rodzaj == "Mundur").Id);

        const string stary = """
            {
              "SchemaVersion": 1,
              "Wyprawy": [{ "Id": "w-stara", "Data": "2021-01-01", "OdliczonaOdLicznikow": true }]
            }
            """;
        using var staraBaza = new MemoryStream(Encoding.UTF8.GetBytes(stary));
        var odczytStarej = MagazynJson.WczytajZeStrumienia(staraBaza);
        Assert.Null(Assert.Single(odczytStarej.Wyprawy).SkladnikiKorekty);
        using var poZapisie = new MemoryStream();
        MagazynJson.ZapiszDoStrumienia(odczytStarej, poZapisie);
        Assert.DoesNotContain("SkladnikiKorekty", Encoding.UTF8.GetString(poZapisie.ToArray()));
    }

    [Fact]
    public void StaraWyprawaBezMigawki_OpisNiePrzeliczaNiepewnejHistorii()
    {
        var (dane, wyprawa, _, pierwszy, drugi, wariant) = Przyklad();
        wyprawa.OdliczonaOdLicznikow = true; // starszy rekord JSON
        dane.Wyprawy.Add(wyprawa);
        wariant.Elementy[0].ElementId = drugi.Id; // obecna konfiguracja może być inna
        var szkic = OperacjeNaWyprawach.Kopia(wyprawa);
        szkic.Miejsce = "Zmienione miejsce";
        Assert.True(new OperacjeNaWyprawach(dane).SprobujZapisac(wyprawa, szkic, out var blad), blad);
        Assert.Null(wyprawa.SkladnikiKorekty);
        Assert.Equal(3, pierwszy.LicznikHistoryczny);
        Assert.Equal(8, drugi.LicznikHistoryczny);
        Assert.Equal(5, dane.Ustawienia.HistoryczneWyprawyWgRok[2021]);
    }
}
