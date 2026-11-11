using Kwatermistrz.Core.Konfiguracja;
using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Tests;

public class ScalaczRekordowZgodnoscTests
{
    [Fact]
    public void ImportKwreko_PrzetwarzaTylkoNoweRekordy_NieUszkadzaPowiazanIstniejacych()
    {
        var lokalna = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        var kategoria = lokalna.Slowniki.Kategorie.First();
        var strona = lokalna.Slowniki.Strony.First();
        var rola = lokalna.Slowniki.Role.First();
        var naped = lokalna.Slowniki.Napedy.First();

        var staryPrzedmiot = new Przedmiot
        {
            Id = "stary-przedmiot",
            KategoriaId = kategoria.Id,
            StronyId = new List<string> { strona.Id },
            LicznikHistoryczny = 17,
        };
        var staraReplika = new Replika
        {
            Id = "stara-replika",
            StronyId = new List<string> { strona.Id },
            RoleId = new List<string> { rola.Id },
            NapedId = naped.Id,
            LicznikHistorycznyWypraw = 23,
        };
        lokalna.Przedmioty.Add(staryPrzedmiot);
        lokalna.Repliki.Add(staraReplika);

        var importowana = new DaneAplikacji();
        importowana.Przedmioty.Add(new Przedmiot
        {
            Id = "nowy-przedmiot",
            KategoriaId = "kat-z-pakietu",
            StronyId = new List<string> { "strona-z-pakietu" },
        });
        importowana.Repliki.Add(new Replika
        {
            Id = "nowa-replika",
            StronyId = new List<string> { "strona-z-pakietu" },
        });
        var mapy = new ImporterPakietuKonfiguracji.MapyId();
        mapy.Kategorie["kat-z-pakietu"] = kategoria.Id;
        mapy.Slowniki["Strony"] = new Dictionary<string, string>
        {
            ["strona-z-pakietu"] = strona.Id,
        };

        var wynik = ScalaczRekordow.Scal(lokalna, importowana, mapy);

        Assert.Equal(0, wynik.Pominiete);
        Assert.Equal(kategoria.Id, staryPrzedmiot.KategoriaId);
        Assert.Equal(strona.Id, Assert.Single(staryPrzedmiot.StronyId));
        Assert.Equal(17, staryPrzedmiot.LicznikHistoryczny);
        Assert.Equal(strona.Id, Assert.Single(staraReplika.StronyId));
        Assert.Equal(rola.Id, Assert.Single(staraReplika.RoleId));
        Assert.Equal(naped.Id, staraReplika.NapedId);
        Assert.Equal(23, staraReplika.LicznikHistorycznyWypraw);

        var dodanyPrzedmiot = lokalna.Przedmioty.Single(p => p.Id == "nowy-przedmiot");
        var dodanaReplika = lokalna.Repliki.Single(r => r.Id == "nowa-replika");
        Assert.Equal(kategoria.Id, dodanyPrzedmiot.KategoriaId);
        Assert.Equal(strona.Id, Assert.Single(dodanyPrzedmiot.StronyId));
        Assert.Equal(strona.Id, Assert.Single(dodanaReplika.StronyId));
        Assert.Equal(1, lokalna.SchemaVersion);
    }

    [Fact]
    public void PonownyImportTegoSamegoKwreko_NieDodajeDuplikatowINiePrzepisujeStarychRekordow()
    {
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        var przedmiot = new Przedmiot
        {
            Id = "p-1", KategoriaId = dane.Slowniki.Kategorie.First().Id,
            LicznikHistoryczny = 8,
        };
        dane.Przedmioty.Add(przedmiot);
        var eksport = new DaneAplikacji
        {
            Przedmioty = new List<Przedmiot> { new() { Id = "p-1", KategoriaId = "obce" } },
        };

        var wynik = ScalaczRekordow.Scal(dane, eksport, new ImporterPakietuKonfiguracji.MapyId());

        Assert.Equal(1, wynik.Pominiete);
        Assert.Single(dane.Przedmioty);
        Assert.Equal(przedmiot.KategoriaId, Assert.Single(dane.Przedmioty).KategoriaId);
        Assert.Equal(8, Assert.Single(dane.Przedmioty).LicznikHistoryczny);
    }
}
