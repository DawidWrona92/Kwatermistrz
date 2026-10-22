using System.IO.Compression;
using Kwatermistrz.Core.Import;
using Kwatermistrz.Core.Konfiguracja;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Obliczenia;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.Core.Tests;

/// <summary>Testy napraw z raportu przegl\u0105du (wydanie 1.0.1): cykl komplet\u00f3w, odczyt magazynu, zip-slip, eksport ustawie\u0144.</summary>
public class Naprawy101Tests
{
    private static (DaneAplikacji dane, Kalkulator kalk) Przygotuj()
    {
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        return (dane, new Kalkulator(dane));
    }

    [Fact]
    public void UzyciaPrzedmiotu_CyklKompletowNieWywolujeStackOverflow()
    {
        var (dane, kalk) = Przygotuj();
        var a = new Przedmiot { Nazwa = "A", LicznikHistoryczny = 3 };
        var b = new Przedmiot { Nazwa = "B", LicznikHistoryczny = 7 };
        a.KompletId = b.Id;
        b.KompletId = a.Id; // cykl A<->B
        dane.Przedmioty.Add(a);
        dane.Przedmioty.Add(b);
        dane.Wyprawy.Add(new Wyprawa { Data = new DateOnly(2026, 1, 1), SkladReczny = new List<ElementWariantu> { new() { Typ = TypElementu.Przedmiot, ElementId = a.Id } } });

        // przed napraw\u0105: StackOverflowException (proces ginie); teraz: sko\u0144czona warto\u015b\u0107
        var uzyciaA = kalk.UzyciaPrzedmiotu(a);
        var uzyciaB = kalk.UzyciaPrzedmiotu(b);
        Assert.True(uzyciaA >= 3);
        Assert.True(uzyciaB >= 7);

        var indeks = new IndeksObliczeniowy(dane);
        Assert.True(indeks.UzyciaPrzedmiotu(a) >= 3);
        Assert.True(indeks.UzyciaPrzedmiotu(b) >= 7);
    }

    [Fact]
    public void CzyTworzyCyklKompletu_WykrywaCyklBezposredniIZagniezdzony()
    {
        var (dane, kalk) = Przygotuj();
        var a = new Przedmiot { Nazwa = "A" };
        var b = new Przedmiot { Nazwa = "B" };
        var c = new Przedmiot { Nazwa = "C" };
        dane.Przedmioty.AddRange(new[] { a, b, c });

        Assert.False(kalk.CzyTworzyCyklKompletu(a.Id, b.Id)); // A -> B: OK
        b.KompletId = a.Id;
        Assert.True(kalk.CzyTworzyCyklKompletu(a.Id, b.Id)); // A -> B -> A: cykl
        Assert.False(kalk.CzyTworzyCyklKompletu(b.Id, c.Id)); // B -> C: OK
        c.KompletId = b.Id;
        Assert.True(kalk.CzyTworzyCyklKompletu(a.Id, c.Id)); // A -> C -> B -> A: cykl
        Assert.True(kalk.CzyTworzyCyklKompletu(c.Id, c.Id)); // w\u0142asny komplet: cykl
    }

    [Fact]
    public void MagazynJson_UszkodzonyGlownyIPustyBak_DajePustyDokumentBezWyjatku()
    {
        var katalog = Path.Combine(Path.GetTempPath(), "kw-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(katalog);
            File.WriteAllText(Path.Combine(katalog, "dane.json"), "{ to nie jest json");
            File.WriteAllText(Path.Combine(katalog, "dane.json.bak"), ""); // pusty .bak

            var magazyn = new MagazynJson(katalog);
            var (dane, kontrola) = magazyn.WczytajZKontrola();

            Assert.False(kontrola.CzyPoprawne);
            Assert.NotNull(kontrola.Komunikat);
            Assert.NotNull(dane); // przed napraw\u0105: wyj\u0105tek przy odczycie pustego .bak
        }
        finally
        {
            Directory.Delete(katalog, recursive: true);
        }
    }

    [Fact]
    public void MagazynJson_UszkodzonyGlownyIDobryBak_OdczytujeBak()
    {
        var katalog = Path.Combine(Path.GetTempPath(), "kw-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var magazyn = new MagazynJson(katalog);
            var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
            dane.Repliki.Add(new Replika { ModelProducenta = "R1" });
            magazyn.Zapisz(dane);
            magazyn.Zapisz(dane); // drugi zapis tworzy dane.json.bak
            File.WriteAllText(Path.Combine(katalog, "dane.json"), "zepsute");

            var (wczytane, kontrola) = new MagazynJson(katalog).WczytajZKontrola();

            Assert.False(kontrola.CzyPoprawne);
            Assert.Single(wczytane.Repliki);
        }
        finally
        {
            Directory.Delete(katalog, recursive: true);
        }
    }

    [Fact]
    public void WyodrebnijZdjecia_PomijaWpisySpozaKataloguDanych()
    {
        var katalog = Path.Combine(Path.GetTempPath(), "kw-test-" + Guid.NewGuid().ToString("N"));
        var plik = Path.Combine(Path.GetTempPath(), "kw-test-" + Guid.NewGuid().ToString("N") + ".kwreko");
        try
        {
            Directory.CreateDirectory(katalog);
            var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
            using (var zip = ZipFile.Open(plik, ZipArchiveMode.Create))
            {
                var wpis = zip.CreateEntry("dane.json");
                using (var str = wpis.Open())
                    MagazynJson.ZapiszDoStrumienia(dane, str);
                // konwencja eksportu: wpis = "zdjecia/" + ścieżka z modelu ("zdjecia/foto.jpg")
                var zlosliwy = zip.CreateEntry("zdjecia/../../evil.txt");
                using (var str = zlosliwy.Open())
                using (var pisarz = new StreamWriter(str))
                    pisarz.Write("x");
                var dobry = zip.CreateEntry("zdjecia/zdjecia/foto.jpg");
                using (var str = dobry.Open())
                using (var pisarz = new StreamWriter(str))
                    pisarz.Write("foto");
            }

            var liczba = EksportRekonstrukcji.WyodrebnijZdjecia(plik, katalog);

            Assert.Equal(1, liczba); // tylko dobry wpis
            Assert.True(File.Exists(Path.Combine(katalog, "zdjecia", "foto.jpg")));
            Assert.False(File.Exists(Path.Combine(katalog, "..", "evil.txt")));
            Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(katalog)!, "evil.txt")));
        }
        finally
        {
            if (Directory.Exists(katalog)) Directory.Delete(katalog, recursive: true);
            if (File.Exists(plik)) File.Delete(plik);
            var evil = Path.Combine(Path.GetTempPath(), "evil.txt");
            if (File.Exists(evil)) File.Delete(evil);
        }
    }

    [Fact]
    public void EksportRekonstrukcji_ZabieraUstawieniaZLicznikamiHistorycznymi()
    {
        var katalog = Path.Combine(Path.GetTempPath(), "kw-test-" + Guid.NewGuid().ToString("N"));
        var plik = Path.Combine(Path.GetTempPath(), "kw-test-" + Guid.NewGuid().ToString("N") + ".kwreko");
        try
        {
            Directory.CreateDirectory(katalog);
            var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
            dane.Rekonstrukcje.Add(new Rekonstrukcja { Nazwa = "Test" });
            dane.Ustawienia.HistoryczneWyprawyWgRok[2024] = 12;
            dane.Ustawienia.DzienZerowy = new DateOnly(2025, 1, 1);
            dane.Ustawienia.Motyw = Motyw.Ciemny; // osobiste \u2014 nie powinno wędrować

            EksportRekonstrukcji.Eksportuj(dane, dane.Rekonstrukcje[0].Id, katalog, plik);
            var wczytane = EksportRekonstrukcji.Wczytaj(plik);

            Assert.Equal(12, wczytane.Ustawienia.HistoryczneWyprawyWgRok[2024]);
            Assert.Equal(new DateOnly(2025, 1, 1), wczytane.Ustawienia.DzienZerowy);
            Assert.Equal(Motyw.Systemowy, wczytane.Ustawienia.Motyw); // domyślny, nie osobisty
        }
        finally
        {
            if (Directory.Exists(katalog)) Directory.Delete(katalog, recursive: true);
            if (File.Exists(plik)) File.Delete(plik);
        }
    }
}

public class Naprawy101ScalanieTests
{
    [Fact]
    public void ScalaczRekordow_PonownyImportNieTworzyDuplikatow()
    {
        var lokalne = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        var eksport = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        var stronaId = lokalne.Slowniki.Strony[0].Id;
        var katId = lokalne.Slowniki.Kategorie.Count > 0 ? lokalne.Slowniki.Kategorie[0].Id : "";
        var replika = new Replika { ModelProducenta = "AK", StronyId = new List<string> { stronaId } };
        var przedmiot = new Przedmiot { Nazwa = "Kamizelka", KategoriaId = katId };
        var zestaw = new Zestaw { Nazwa = "Zestaw letni", Sloty = new List<SlotZestawu>
        {
            new() { Nazwa = "Broń", Warianty = new List<WariantSlotu> { new() { Elementy = new List<ElementWariantu> { new() { Typ = TypElementu.Replika, ElementId = replika.Id } } } } },
        } };
        var wyprawa = new Wyprawa { Data = new DateOnly(2026, 3, 1), ReplikaGlownaId = replika.Id, ZestawId = zestaw.Id };
        var zadanie = new Zadanie { DotyczyTyp = "Replika", DotyczyId = replika.Id, RodzajId = lokalne.Slowniki.RodzajeZadan[0].Id, Opis = "Wymiana" };
        var rekonstrukcja = new Rekonstrukcja { Nazwa = "Rosja", StronaId = stronaId };
        lokalne.Rekonstrukcje.Add(rekonstrukcja);
        lokalne.Repliki.Add(replika);
        lokalne.Przedmioty.Add(przedmiot);
        lokalne.Zestawy.Add(zestaw);
        lokalne.Wyprawy.Add(wyprawa);
        lokalne.Zadania.Add(zadanie);

        // "eksport" = te same rekordy (symulacja eksportu własnej instalacji)
        var mapy = new Kwatermistrz.Core.Konfiguracja.ImporterPakietuKonfiguracji.MapyId();
        var wynik = Kwatermistrz.Core.Konfiguracja.ScalaczRekordow.Scal(lokalne, lokalne, mapy);

        Assert.Equal(6, wynik.Pominiete); // wszystkie 6 rekordów ju¿ istnieje
        Assert.Single(lokalne.Repliki);
        Assert.Single(lokalne.Przedmioty);
        Assert.Single(lokalne.Zestawy);
        Assert.Single(lokalne.Wyprawy);
        Assert.Single(lokalne.Zadania);
        Assert.Single(lokalne.Rekonstrukcje);
    }

    [Fact]
    public void ImporterXlsx_ScalanieSlownikow_PrzemapowujeIdIIstniejaceZostaja()
    {
        var daneBazowe = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        var importowane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() }; // inne Id!
        var stronaImport = importowane.Slowniki.Strony.First(s => s.Nazwa == "Rosja");
        var stronaBaza = daneBazowe.Slowniki.Strony.First(s => s.Nazwa == "Rosja");
        Assert.NotEqual(stronaImport.Id, stronaBaza.Id);
        var replika = new Replika { ModelProducenta = "AK", StronyId = new List<string> { stronaImport.Id } };
        var przedmiot = new Przedmiot { Nazwa = "Mundur", KategoriaId = importowane.Slowniki.Kategorie[0].Id };
        importowane.Repliki.Add(replika);
        importowane.Przedmioty.Add(przedmiot);
        importowane.Rekonstrukcje.Add(new Rekonstrukcja { Nazwa = "Rosja", StronaId = stronaImport.Id });

        var podglad = new Kwatermistrz.Core.Import.PodgladImportu { Dane = importowane };
        Kwatermistrz.Core.Import.ImporterKwatermistrz.Zastosuj(podglad, daneBazowe);

        // referencje wskazuj\u0105 lokalne Id, nie importowane
        Assert.Equal(stronaBaza.Id, daneBazowe.Repliki[0].StronyId[0]);
        Assert.Equal(daneBazowe.Slowniki.Kategorie[0].Id, daneBazowe.Przedmioty[0].KategoriaId);
        Assert.Equal(stronaBaza.Id, daneBazowe.Rekonstrukcje.Single(r => r.Nazwa == "Rosja").StronaId);
        // lokalne warto\u015bci nie zosta\u0142y zdublowane
        Assert.Equal(StartoweSlowniki.Utworz().Strony.Count, daneBazowe.Slowniki.Strony.Count);
    }

    [Fact]
    public void ImporterXlsx_PonownyImport_PomijaIstniejacaRekonstrukcje()
    {
        var daneBazowe = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        var importowane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        importowane.Rekonstrukcje.Add(new Rekonstrukcja { Nazwa = "Rosja" });
        var podglad = new Kwatermistrz.Core.Import.PodgladImportu { Dane = importowane };

        Kwatermistrz.Core.Import.ImporterKwatermistrz.Zastosuj(podglad, daneBazowe);
        Kwatermistrz.Core.Import.ImporterKwatermistrz.Zastosuj(podglad, daneBazowe); // drugi raz

        Assert.Single(daneBazowe.Rekonstrukcje);
    }

    [Fact]
    public void MergerSlownikow_IstniejacaWartosc_ZostajeBezDublowania()
    {
        var lokalne = StartoweSlowniki.Utworz();
        var przed = lokalne.Strony.Count;
        var szablon = StartoweSlowniki.Utworz(); // te same nazwy, inne Id
        szablon.Strony.Add(new WartoscSlownika { Nazwa = "Własna strona" });

        var wynik = Kwatermistrz.Core.Konfiguracja.MergerSlownikow.ScalSlowniki(lokalne, szablon);

        Assert.Equal(przed + 1, lokalne.Strony.Count); // tylko "Własna strona" dopisana
        Assert.True(wynik.Dopisane >= 1);
        // Id lokalnej wartości zachowane: mapa szablon->lokalne wskazuje istniejącą wartość
        var rosjaSzablon = szablon.Strony.First(s => s.Nazwa == "Rosja");
        var rosjaLokalna = lokalne.Strony.First(s => s.Nazwa == "Rosja");
        Assert.Equal(rosjaLokalna.Id, wynik.MapujSlownik("Strony", rosjaSzablon.Id));
    }

    [Fact]
    public void ImporterOgolny_Pole_BezMapowaniaDajeNull()
    {
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        var arkusz = new ArkuszXlsx { Nazwa = "test" };
        arkusz.Wiersze.Add(new WierszXlsx { Numer = 1, Komorki = new List<string?> { "nagłówek A", "nagłówek B" } });
        arkusz.Wiersze.Add(new WierszXlsx { Numer = 2, Komorki = new List<string?> { "Wartość A", "Wartość B" } });
        var mapowanie = new Kwatermistrz.Core.Import.MapowanieKolumn { Nazwa = "test", TylkoPelneWiersze = false }; // puste mapowanie

        var podglad = Kwatermistrz.Core.Import.ImporterOgolny.PrzygotujPodglad(dane, arkusz, mapowanie, wierszNaglowka: 1);

        // nagłówek pominięty (wiersz 1), wiersz danych jako rekord; bez mapowania kolumna 0 nie może trafić do pól
        Assert.Single(podglad);
        Assert.Equal(2, podglad[0].NumerWiersza);
        Assert.Equal("", podglad[0].Przedmiot.Nazwa);
        Assert.Null(podglad[0].Przedmiot.Firma);
        Assert.Null(podglad[0].Przedmiot.Komentarz);
    }
}

public class Naprawy101LiczbyTests
{
    private static (DaneAplikacji dane, Kalkulator kalk) Przygotuj()
    {
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        return (dane, new Kalkulator(dane));
    }

    [Fact]
    public void Indeks_Uzycia_RazNaWyprawe_GdyPrzedmiotWDwochSlochach()
    {
        var (dane, kalk) = Przygotuj();
        var p = new Przedmiot { Nazwa = "Pas" };
        dane.Przedmioty.Add(p);
        var zestaw = new Zestaw { Nazwa = "Z" };
        var slotA = new SlotZestawu { Nazwa = "Pas 1" };
        slotA.Warianty.Add(new WariantSlotu { Elementy = new() { new ElementWariantu { Typ = TypElementu.Przedmiot, ElementId = p.Id } } });
        var slotB = new SlotZestawu { Nazwa = "Pas 2" };
        slotB.Warianty.Add(new WariantSlotu { Elementy = new() { new ElementWariantu { Typ = TypElementu.Przedmiot, ElementId = p.Id } } });
        zestaw.Sloty.AddRange(new[] { slotA, slotB });
        dane.Zestawy.Add(zestaw);
        // ta sama wyprawa: ten sam przedmiot w dwóch słotach (BR-08: liczy się raz)
        dane.Wyprawy.Add(new Wyprawa
        {
            Data = new DateOnly(2026, 1, 1),
            ZestawId = zestaw.Id,
            WybraneWarianty = new Dictionary<string, string>
            {
                [slotA.Id] = slotA.Warianty[0].Id,
                [slotB.Id] = slotB.Warianty[0].Id,
            },
        });

        var indeks = new IndeksObliczeniowy(dane);
        Assert.Equal(kalk.UzyciaPrzedmiotu(p), indeks.UzyciaPrzedmiotu(p));
        Assert.Equal(1, indeks.UzyciaPrzedmiotu(p));
    }

    [Fact]
    public void KosztZestawu_LiczyReplikiWLiściePakowania()
    {
        var (dane, kalk) = Przygotuj();
        var replika = new Replika
        {
            ModelProducenta = "AK",
            Koszty = new List<PozycjaKosztowa> { new() { Kwota = 500m, Semantyka = SemantykaKosztu.Zakup } },
        };
        dane.Repliki.Add(replika);
        var zestaw = new Zestaw { Nazwa = "Z" };
        var slot = new SlotZestawu { Nazwa = "Broń" };
        slot.Warianty.Add(new WariantSlotu { Elementy = new() { new ElementWariantu { Typ = TypElementu.Replika, ElementId = replika.Id } } });
        zestaw.Sloty.Add(slot);
        dane.Zestawy.Add(zestaw);

        var lista = kalk.ElementyListyPakowania(zestaw);
        Assert.Single(lista);
        Assert.Equal(TypElementu.Replika, lista[0].Element.Typ);
        Assert.Equal(500m, kalk.KosztZestawu(zestaw));
    }

    [Fact]
    public void Migracja_WariantyIndeksy_PrzepisaneNaId()
    {
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        var zestaw = new Zestaw { Nazwa = "Z" };
        var slot = new SlotZestawu { Nazwa = "S" };
        var w0 = new WariantSlotu();
        var w1 = new WariantSlotu();
        slot.Warianty.AddRange(new[] { w0, w1 });
        zestaw.Sloty.Add(slot);
        var wyprawa = new Wyprawa
        {
            Data = new DateOnly(2026, 1, 1),
            ZestawId = zestaw.Id,
        };
        dane.Zestawy.Add(zestaw);
        dane.Wyprawy.Add(wyprawa);

        var katalog = Path.Combine(Path.GetTempPath(), "kw-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(katalog);
        try
        {
            var magazyn = new MagazynJson(katalog);
            // symulacja starego JSON (1.0.0): indeksy zamiast Id wariantów
            var staryJson = $$"""
            {
              "SchemaVersion": 1,
              "WersjaRegul": "1.0",
              "Slowniki": {{ MagazynJsonTestHelpers.SerializujSlowniki(dane.Slowniki) }},
              "Przedmioty": [],
              "Repliki": [],
              "Zestawy": [
                {
                  "Id": "{{zestaw.Id}}",
                  "Nazwa": "Z",
                  "RekonstrukcjaId": null,
                  "StronaId": null,
                  "Opis": null,
                  "CzySzkic": false,
                  "Zdjecia": [],
                  "Sloty": [
                    {
                      "Id": "{{slot.Id}}",
                      "Nazwa": "S",
                      "Kolejnosc": 0,
                      "Warianty": [
                        { "Elementy": [], "CzyPuste": true },
                        { "Elementy": [], "CzyPuste": true }
                      ],
                      "IndeksWariantuDomyslnego": 1
                    }
                  ],
                  "OstatnieWarianty": { "{{slot.Id}}": 1 },
                  "SpakowaneKlucze": [],
                  "ZnacznikZImportu": false
                }
              ],
              "Wyprawy": [
                {
                  "Id": "{{wyprawa.Id}}",
                  "Data": "2026-01-01",
                  "RekonstrukcjaId": null,
                  "Miejsce": null,
                  "ReplikaGlownaId": null,
                  "ZestawId": "{{zestaw.Id}}",
                  "WybraneWarianty": { "{{slot.Id}}": 1 },
                  "SkladReczny": [],
                  "Komentarz": null,
                  "Zdjecia": [],
                  "CzyUsunieta": false,
                  "OstatniaZmiana": "2026-01-01T00:00:00"
                }
              ],
              "Rekonstrukcje": [],
              "SzablonyKart": [],
              "Zadania": [],
              "Ustawienia": {},
              "CzyPierwszeUruchomienie": false
            }
            """;
            File.WriteAllText(Path.Combine(katalog, "dane.json"), staryJson);

            var wczytane = magazyn.Wczytaj();

            var wczytanySlot = wczytane.Zestawy[0].Sloty[0];
            var drugiWariantId = wczytanySlot.Warianty[1].Id;
            Assert.Equal(drugiWariantId, wczytanySlot.WariantDomyslnyId);
            Assert.Equal(drugiWariantId, wczytane.Zestawy[0].OstatnieWarianty[slot.Id]);
            Assert.Equal(drugiWariantId, wczytane.Wyprawy[0].WybraneWarianty[slot.Id]);

            // roundtrip: zapis nowym formatem nie zawiera starych pól ani duplikatów kluczy
            magazyn.Zapisz(wczytane);
            var json = File.ReadAllText(magazyn.SciezkaPliku);
            Assert.DoesNotContain("IndeksWariantuDomyslnego", json);
            Assert.Equal(1, json.Split("\"OstatnieWarianty\"").Length - 1);
            Assert.Equal(1, json.Split("\"WybraneWarianty\"").Length - 1);
            var ponownie = magazyn.Wczytaj();
            Assert.Equal(drugiWariantId, ponownie.Zestawy[0].Sloty[0].WariantDomyslnyId);
            Assert.Equal(drugiWariantId, ponownie.Zestawy[0].OstatnieWarianty[slot.Id]);
            Assert.Equal(drugiWariantId, ponownie.Wyprawy[0].WybraneWarianty[slot.Id]);
        }
        finally
        {
            if (Directory.Exists(katalog))
                Directory.Delete(katalog, recursive: true);
        }
    }

    [Fact]
    public void UzyciaPrzedmiotu_KompletLiczonyRazDlaElementuIKontenera()
    {
        var (dane, kalk) = Przygotuj();
        var komplet = new Przedmiot { Nazwa = "Komplet" };
        var czesc = new Przedmiot { Nazwa = "Część", KompletId = komplet.Id };
        dane.Przedmioty.AddRange(new[] { komplet, czesc });
        var zestaw = new Zestaw { Nazwa = "Z" };
        var slot = new SlotZestawu { Nazwa = "Mundur" };
        slot.Warianty.Add(new WariantSlotu { Elementy = new() { new ElementWariantu { Typ = TypElementu.Komplet, ElementId = komplet.Id } } });
        zestaw.Sloty.Add(slot);
        dane.Zestawy.Add(zestaw);
        dane.Wyprawy.Add(new Wyprawa
        {
            Data = new DateOnly(2026, 1, 1),
            ZestawId = zestaw.Id,
            WybraneWarianty = new Dictionary<string, string> { [slot.Id] = slot.Warianty[0].Id },
        });

        // komplet rozwinięty daje oba przedmioty, ale wyprawa liczy się raz dla każdego (BR-08)
        Assert.Equal(1, kalk.UzyciaPrzedmiotu(komplet));
        Assert.Equal(1, kalk.UzyciaPrzedmiotu(czesc));
        var indeks = new IndeksObliczeniowy(dane);
        Assert.Equal(1, indeks.UzyciaPrzedmiotu(komplet));
        Assert.Equal(1, indeks.UzyciaPrzedmiotu(czesc));
    }
}

/// <summary>Testy napraw 1.0.1 fala 4 (raport przeglądu): czyszczenie referencji, retencja kopii, kontrola spójności.</summary>
public class Naprawy101CzyszczenieTests
{
    private static DaneAplikacji Przygotuj()
        => new() { Slowniki = StartoweSlowniki.Utworz() };

    [Fact]
    public void UsunReferencjeRepliki_CzysciWyprawyElementyIZadania()
    {
        var dane = Przygotuj();
        var replika = new Replika { ModelProducenta = "AK" };
        var przedmiot = new Przedmiot { Nazwa = "Kamizelka" };
        dane.Repliki.Add(replika);
        dane.Przedmioty.Add(przedmiot);
        var zestaw = new Zestaw { Nazwa = "Z" };
        var slot = new SlotZestawu { Nazwa = "Broń" };
        slot.Warianty.Add(new WariantSlotu { Elementy = new() { new ElementWariantu { Typ = TypElementu.Replika, ElementId = replika.Id } } });
        zestaw.Sloty.Add(slot);
        dane.Zestawy.Add(zestaw);
        dane.Wyprawy.Add(new Wyprawa { Data = new DateOnly(2026, 1, 1), ReplikaGlownaId = replika.Id });
        dane.Wyprawy.Add(new Wyprawa
        {
            Data = new DateOnly(2026, 1, 2),
            SkladReczny = new() { new ElementWariantu { Typ = TypElementu.Replika, ElementId = replika.Id } },
        });
        dane.Zadania.Add(new Zadanie { DotyczyTyp = "Replika", DotyczyId = replika.Id, Opis = "Awaria" });
        dane.Zadania.Add(new Zadanie { DotyczyTyp = "Przedmiot", DotyczyId = przedmiot.Id, Opis = "Inne" });

        CzyscicielReferencji.UsunReferencjeRepliki(dane, replika.Id);

        Assert.Null(dane.Wyprawy[0].ReplikaGlownaId);
        Assert.Equal("", zestaw.Sloty[0].Warianty[0].Elementy[0].ElementId);
        Assert.Equal("", dane.Wyprawy[1].SkladReczny[0].ElementId);
        // zadanie przedmiotu zostaje — usuwamy tylko zadania usuwanej repliki
        var zostalo = Assert.Single(dane.Zadania);
        Assert.Equal("Przedmiot", zostalo.DotyczyTyp);
    }

    [Fact]
    public void UsunReferencjePrzedmiotu_CzysciMontazKompletKonfiguracjeIZadania()
    {
        var dane = Przygotuj();
        var nosnik = new Przedmiot { Nazwa = "Plate carrier" };
        var komplet = new Przedmiot { Nazwa = "Komplet mundurów" };
        // część kompletu zamontowana na nośniku; osobna część w konfiguracji nośnika
        var czesc = new Przedmiot { Nazwa = "Część", KompletId = komplet.Id, ZamontowanyNaId = nosnik.Id };
        var ladownica = new Przedmiot { Nazwa = "Ładownica", ZamontowanyNaId = nosnik.Id };
        nosnik.Konfiguracje.Add(new KonfiguracjaNosnika { Nazwa = "K1", ElementyId = new() { ladownica.Id, "inny" } });
        var zestaw = new Zestaw { Nazwa = "Z" };
        var slot = new SlotZestawu { Nazwa = "Mundur" };
        slot.Warianty.Add(new WariantSlotu { Elementy = new() { new ElementWariantu { Typ = TypElementu.Przedmiot, ElementId = komplet.Id } } });
        zestaw.Sloty.Add(slot);
        dane.Przedmioty.AddRange(new[] { nosnik, komplet, czesc, ladownica });
        dane.Zestawy.Add(zestaw);
        dane.Zadania.Add(new Zadanie { DotyczyTyp = "Przedmiot", DotyczyId = komplet.Id, Opis = "Kup" });

        CzyscicielReferencji.UsunReferencjePrzedmiotu(dane, komplet.Id);
        CzyscicielReferencji.UsunReferencjePrzedmiotu(dane, nosnik.Id);

        // część kompletu zostaje, ale bez kompletu; montaże na usuniętym nośniku też znikają
        Assert.Null(czesc.KompletId);
        Assert.Null(czesc.ZamontowanyNaId);
        Assert.Null(ladownica.ZamontowanyNaId);
        Assert.Equal(new[] { ladownica.Id, "inny" }, nosnik.Konfiguracje[0].ElementyId);
        Assert.Equal("", zestaw.Sloty[0].Warianty[0].Elementy[0].ElementId);
        Assert.Empty(dane.Zadania);
    }

    [Fact]
    public void RetencjaKopii_TrzymaMaksymalnie20Kopii()
    {
        var katalog = Path.Combine(Path.GetTempPath(), "kw-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(katalog);
        try
        {
            var magazyn = new MagazynJson(katalog);
            var dane = Przygotuj();
            magazyn.Zapisz(dane);
            for (var i = 0; i < MagazynJson.MaksymalnaLiczbaKopii + 5; i++)
            {
                Directory.CreateDirectory(Path.Combine(katalog, "kopie", $"2020-01-01-0000{i:00}"));
                magazyn.UtworzKopieZapasowa();
            }
            var kopie = Directory.GetDirectories(Path.Combine(katalog, "kopie"));
            Assert.Equal(MagazynJson.MaksymalnaLiczbaKopii, kopie.Length);
            // najstarsza (2020-01-01-000000) usunięta
            Assert.DoesNotContain(kopie, k => k.EndsWith("2020-01-01-000000"));
        }
        finally
        {
            if (Directory.Exists(katalog))
                Directory.Delete(katalog, recursive: true);
        }
    }

    [Fact]
    public void KontrolaSpojnosci_WgReplikLiczoneRaz()
    {
        var dane = Przygotuj();
        var replika = new Replika { ModelProducenta = "AK" };
        dane.Repliki.Add(replika);
        dane.Wyprawy.Add(new Wyprawa { Data = new DateOnly(2026, 1, 1), ReplikaGlownaId = replika.Id });
        var kalk = new Kalkulator(dane);

        var (wgReplik, wgLat, _, bezRepliki, _) = kalk.KontrolaSpojnosci();

        Assert.Equal(1, wgReplik);
        Assert.Equal(1, wgLat);
        Assert.Equal(0, bezRepliki);
    }
}
