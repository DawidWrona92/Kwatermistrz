using System.Diagnostics;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Obliczenia;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.Core.Tests;

/// <summary>Testy indeksu obliczeniowego (NFR-04): zgodność z Kalkulatorem na danych syntetycznych + budżet czasowy.</summary>
public class IndeksObliczeniowyTests
{
    private static DaneAplikacji DaneZZestawem(out Przedmiot nosnik, out Przedmiot ladownica, out Przedmiot komplet, out Przedmiot czescKompletu)
    {
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        nosnik = new Przedmiot { Nazwa = "Kamizelka", KategoriaId = dane.Slowniki.Kategorie.First().Id };
        ladownica = new Przedmiot { Nazwa = "Ładownica", KategoriaId = dane.Slowniki.Kategorie.First().Id };
        komplet = new Przedmiot { Nazwa = "Komplet", KategoriaId = dane.Slowniki.Kategorie.First().Id, LicznikHistoryczny = 2 };
        czescKompletu = new Przedmiot { Nazwa = "Część kompletu", KategoriaId = dane.Slowniki.Kategorie.First().Id, KompletId = komplet.Id };
        nosnik.Konfiguracje.Add(new KonfiguracjaNosnika { Nazwa = "Light", ElementyId = new List<string> { ladownica.Id } });
        dane.Przedmioty.AddRange(new[] { nosnik, ladownica, komplet, czescKompletu });

        var slot = new SlotZestawu
        {
            Nazwa = "Kamizerka",
            Warianty = new List<WariantSlotu>
            {
                new() { Elementy = new List<ElementWariantu> { new() { Typ = TypElementu.Przedmiot, ElementId = nosnik.Id } } },
                new() { Elementy = new List<ElementWariantu> { new() { Typ = TypElementu.KonfiguracjaNosnika, ElementId = nosnik.Konfiguracje[0].Id } } },
                new() { Elementy = new List<ElementWariantu> { new() { Typ = TypElementu.Komplet, ElementId = komplet.Id } } },
                new() { CzyPuste = true },
            },
        };
        var zestaw = new Zestaw { Nazwa = "Z1", Sloty = new List<SlotZestawu> { slot } };
        dane.Zestawy.Add(zestaw);

        var replika = new Replika { ModelProducenta = "AK", LicznikHistorycznyWypraw = 3 };
        dane.Repliki.Add(replika);
        dane.Wyprawy.Add(new Wyprawa
        {
            Data = new DateOnly(2026, 1, 5),
            ReplikaGlownaId = replika.Id,
            ZestawId = zestaw.Id,
            WybraneWarianty = new Dictionary<string, int> { [slot.Id] = 0 },
        });
        dane.Wyprawy.Add(new Wyprawa
        {
            Data = new DateOnly(2026, 2, 10),
            ReplikaGlownaId = replika.Id,
            ZestawId = zestaw.Id,
            WybraneWarianty = new Dictionary<string, int> { [slot.Id] = 1 },
        });
        dane.Wyprawy.Add(new Wyprawa
        {
            Data = new DateOnly(2026, 3, 1),
            ReplikaGlownaId = replika.Id,
            ZestawId = zestaw.Id,
            WybraneWarianty = new Dictionary<string, int> { [slot.Id] = 2 },
        });
        dane.Wyprawy.Add(new Wyprawa { Data = new DateOnly(2026, 3, 2) }); // bez repliki, bez zestawu
        dane.Wyprawy.Add(new Wyprawa { Data = new DateOnly(2020, 1, 1), CzyUsunieta = true, ReplikaGlownaId = replika.Id });
        return dane;
    }

    [Fact]
    public void Indeks_ZgodnyZKalkulatorem_DlaWszystkichReplikIPrzedmiotow()
    {
        var dane = DaneZZestawem(out _, out _, out _, out _);
        var kalk = new Kalkulator(dane);
        var indeks = new IndeksObliczeniowy(dane);

        Assert.Equal(kalk.LacznaLiczbaWypraw(), indeks.LacznaLiczbaWypraw);
        foreach (var r in dane.Repliki)
        {
            Assert.Equal(kalk.WyprawyRepliki(r), indeks.WyprawyRepliki(r));
            Assert.Equal(kalk.ProcentWypraw(kalk.WyprawyRepliki(r)), indeks.ProcentWypraw(indeks.WyprawyRepliki(r)));
        }
        foreach (var p in dane.Przedmioty)
            Assert.Equal(kalk.UzyciaPrzedmiotu(p), indeks.UzyciaPrzedmiotu(p));
    }

    [Fact]
    public void Indeks_ElementKompletuDzieliLicznikKompletu_BR08()
    {
        var dane = DaneZZestawem(out _, out _, out var komplet, out var czesc);
        var kalk = new Kalkulator(dane);
        var indeks = new IndeksObliczeniowy(dane);

        // komplet użyty raz przez wariant zestawu + licznik historyczny 2
        Assert.Equal(3, kalk.UzyciaPrzedmiotu(komplet));
        Assert.Equal(3, indeks.UzyciaPrzedmiotu(komplet));
        Assert.Equal(3, indeks.UzyciaPrzedmiotu(czesc));
    }

    [Fact]
    public void Indeks_KonfiguracjaNosnikaRozwijaSieNaElementy_FR208()
    {
        var dane = DaneZZestawem(out _, out var ladownica, out _, out _);
        var indeks = new IndeksObliczeniowy(dane);

        // wyprawa 2026-02-10 ma wariant konfiguracji = { ladownica }
        Assert.Equal(1, indeks.UzyciaPrzedmiotu(ladownica));
    }

    [Fact]
    public void NFR04_BudowaIndeksu_PrzySkali5000x2000_WBudzecieCzasowym()
    {
        // NFR-04: 5 000 przedmiotów, 2 000 wypraw, 5 000 zdjęć — budowa indeksu poniżej 300 ms
        // (właściwe progi UI: start < 3 s, wyszukiwanie < 300 ms, przewijanie płynne)
        const int liczbaPrzedmiotow = 5000;
        const int liczbaWypraw = 2000;
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        var kategoria = dane.Slowniki.Kategorie.First();
        for (var i = 0; i < liczbaPrzedmiotow; i++)
        {
            var zdjecia = i < 1000
                ? Enumerable.Range(0, 5).Select(j => $"zdjecia/przedmiot{i}_zdjecie{j}.jpg").ToList()
                : new List<string>();
            dane.Przedmioty.Add(new Przedmiot
            {
                Nazwa = $"Przedmiot {i:D5}",
                KategoriaId = kategoria.Id,
                LicznikHistoryczny = i % 7,
                Zdjecia = zdjecia,
            });
        }
        var replika = new Replika { ModelProducenta = "Replika testowa", LicznikHistorycznyWypraw = 100 };
        dane.Repliki.Add(replika);
        var los = new Random(42);
        for (var i = 0; i < liczbaWypraw; i++)
            dane.Wyprawy.Add(new Wyprawa
            {
                Data = new DateOnly(2020, 1, 1).AddDays(i % 2400),
                ReplikaGlownaId = replika.Id,
                SkladReczny = new List<ElementWariantu>
                {
                    new() { Typ = TypElementu.Przedmiot, ElementId = dane.Przedmioty[los.Next(liczbaPrzedmiotow)].Id },
                },
            });

        var zegar = Stopwatch.StartNew();
        var indeks = new IndeksObliczeniowy(dane);
        zegar.Stop();

        Assert.True(zegar.ElapsedMilliseconds < 300,
            $"Budowa indeksu przy {liczbaPrzedmiotow} przedmiotach i {liczbaWypraw} wyprawach: {zegar.ElapsedMilliseconds} ms (budżet 300 ms)");
        Assert.Equal(2100, indeks.WyprawyRepliki(replika)); // 100 historycznych + 2000 zapisanych
        Assert.Equal(liczbaWypraw + 100, indeks.LacznaLiczbaWypraw);
    }

    [Fact]
    public void NFR04_ZapisIOdczytJson_PrzySkali5000x2000_WBudzecieCzasowym()
    {
        // start aplikacji < 3 s: serializacja i deserializacja pełnego zestawu danych
        var katalog = Path.Combine(Path.GetTempPath(), "kwatermistrz-nfr04-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(katalog);
        try
        {
            var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
            var kategoria = dane.Slowniki.Kategorie.First();
            for (var i = 0; i < 5000; i++)
                dane.Przedmioty.Add(new Przedmiot
                {
                    Nazwa = $"Przedmiot {i:D5}",
                    KategoriaId = kategoria.Id,
                    Zdjecia = i < 1000 ? Enumerable.Range(0, 5).Select(j => $"zdjecia/p{i}_{j}.jpg").ToList() : new List<string>(),
                });
            var replika = new Replika { ModelProducenta = "R", LicznikHistorycznyWypraw = 50 };
            dane.Repliki.Add(replika);
            for (var i = 0; i < 2000; i++)
                dane.Wyprawy.Add(new Wyprawa
                {
                    Data = new DateOnly(2021, 1, 1).AddDays(i % 1800),
                    ReplikaGlownaId = replika.Id,
                    Miejsce = $"Miejsce {i % 30}",
                });

            var magazyn = new MagazynJson(katalog);
            var zegar = Stopwatch.StartNew();
            magazyn.Zapisz(dane);
            var wczytane = magazyn.Wczytaj();
            zegar.Stop();

            Assert.True(zegar.ElapsedMilliseconds < 3000,
                $"Zapis + odczyt przy skali NFR-04: {zegar.ElapsedMilliseconds} ms (budżet 3000 ms)");
            Assert.Equal(5000, wczytane.Przedmioty.Count);
            Assert.Equal(2000, wczytane.Wyprawy.Count);
            Assert.Equal(5000, wczytane.Przedmioty.Take(1000).Sum(p => p.Zdjecia.Count));
        }
        finally
        {
            Directory.Delete(katalog, recursive: true);
        }
    }
}
