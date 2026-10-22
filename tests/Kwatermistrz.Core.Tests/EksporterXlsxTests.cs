using Kwatermistrz.Core.Eksport;
using Kwatermistrz.Core.Import;
using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Tests;

/// <summary>Testy eksportu całości do xlsx (FR-1106): budowa arkuszy + zapis/odczyt przez NPOI (NFR-09).</summary>
public class EksporterXlsxTests
{
    private static DaneAplikacji DaneTestowe()
    {
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        var kategoria = dane.Slowniki.Kategorie.First();
        var przedmiot = new Przedmiot
        {
            Nazwa = "Kamizelka 6B46",
            KategoriaId = kategoria.Id,
            Firma = "6B46",
            LicznikHistoryczny = 4,
            CenaZakupu = 350m,
        };
        dane.Przedmioty.Add(przedmiot);
        var replika = new Replika
        {
            ModelProducenta = "SA-J01 Edge 2",
            Prototyp = "AK-74M Alfa",
            LicznikHistorycznyWypraw = 19,
        };
        replika.Koszty.Add(new PozycjaKosztowa { Kwota = 960m, Semantyka = SemantykaKosztu.Zakup });
        replika.Koszty.Add(new PozycjaKosztowa { Kwota = 112m, Semantyka = SemantykaKosztu.Ulepszenie });
        dane.Repliki.Add(replika);
        var zestaw = new Zestaw
        {
            Nazwa = "Zestaw podstawowy",
            Sloty = new List<SlotZestawu>
            {
                new()
                {
                    Nazwa = "Kamizelka",
                    Warianty = new List<WariantSlotu>
                    {
                        new() { Elementy = new List<ElementWariantu> { new() { Typ = TypElementu.Przedmiot, ElementId = przedmiot.Id } } },
                    },
                },
            },
        };
        dane.Zestawy.Add(zestaw);
        dane.Wyprawy.Add(new Wyprawa
        {
            Data = new DateOnly(2026, 1, 10),
            ReplikaGlownaId = replika.Id,
            ZestawId = zestaw.Id,
            Miejsce = "Leśny poligon",
        });
        dane.Zadania.Add(new Zadanie { DotyczyTyp = "Replika", DotyczyId = replika.Id, Opis = "Wymienić sznurek" });
        return dane;
    }

    [Fact]
    public void BudujArkusze_TworzyWszystkieArkusze_ZDanymi()
    {
        var arkusze = EksporterXlsx.BudujArkusze(DaneTestowe());

        Assert.Equal(new[] { "Broń", "Wyposażenie", "Zestawy", "Wyprawy", "Zadania", "Słowniki" },
            arkusze.Select(a => a.Nazwa).ToArray());
        var bron = arkusze[0];
        Assert.Equal("Miejsce", bron.Wiersze[0].Komorki[0]); // nagłówek
        Assert.Equal("1", bron.Wiersze[1].Komorki[0]);
        Assert.Equal("SA-J01 Edge 2 (AK-74M Alfa)", bron.Wiersze[1].Komorki[1]);
        Assert.Equal("20", bron.Wiersze[1].Komorki[2]); // wyprawy: 19 historycznych + 1 zapisana (BR-01)
        Assert.Equal("1 072,00 zł", bron.Wiersze[1].Komorki[7]); // koszt całkowity (BR-04, BR-14)
        Assert.Equal("960,00 zł", bron.Wiersze[1].Komorki[8]); // cena oryginalna
        Assert.Equal("112,00 zł", bron.Wiersze[1].Komorki[9]); // dodatki
        Assert.Equal("Suma (posiadane)", bron.Wiersze[^1].Komorki[0]); // wiersz sumy

        var wyposazenie = arkusze[1];
        Assert.Equal("Kamizelka 6B46", wyposazenie.Wiersze[1].Komorki[2]);
        Assert.Equal("4", wyposazenie.Wiersze[1].Komorki[7]); // użycia (BR-08)

        var wyprawy = arkusze[3];
        Assert.Equal("10.01.2026", wyprawy.Wiersze[1].Komorki[0]); // data BR-14
        Assert.Equal("Leśny poligon", wyprawy.Wiersze[1].Komorki[1]);
        Assert.Equal("SA-J01 Edge 2", wyprawy.Wiersze[1].Komorki[2]);
    }

    [Fact]
    public void Zapis_Odczyt_PrzezNpoi_ZachowujeWartosci()
    {
        var arkusze = EksporterXlsx.BudujArkusze(DaneTestowe());
        using var strumien = new MemoryStream();
        EksporterXlsx.Zapisz(arkusze, strumien);
        strumien.Position = 0;

        var odczytane = CzytnikXlsx.Wczytaj(strumien);
        Assert.Equal(arkusze.Count, odczytane.Count);
        Assert.Equal("Broń", odczytane[0].Nazwa);
        Assert.Equal("SA-J01 Edge 2 (AK-74M Alfa)", odczytane[0].Wiersze[1].Komorka(1));
        Assert.Equal("1 072,00 zł", odczytane[0].Wiersze[1].Komorka(7));
        Assert.Equal("Kamizelka 6B46", odczytane[1].Wiersze[1].Komorka(2));
        Assert.Equal("10.01.2026", odczytane[3].Wiersze[1].Komorka(0));
    }
}
