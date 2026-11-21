using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Tests;

public class CzyscicielReferencjiTests
{
    [Theory]
    [InlineData(TypElementu.Przedmiot)]
    [InlineData(TypElementu.Komplet)]
    public void UsunReferencjePrzedmiotu_CzysciZestawIWypraweDlaObuTypow(TypElementu typ)
    {
        var dane = new DaneAplikacji();
        var przedmiot = new Przedmiot { Nazwa = "Usuwany przedmiot" };
        dane.Przedmioty.Add(przedmiot);

        var elementZestawu = new ElementWariantu { Typ = typ, ElementId = przedmiot.Id };
        var wariant = new WariantSlotu();
        wariant.Elementy.Add(elementZestawu);
        var slot = new SlotZestawu();
        slot.Warianty.Add(wariant);
        var zestaw = new Zestaw();
        zestaw.Sloty.Add(slot);
        dane.Zestawy.Add(zestaw);

        var elementWyprawy = new ElementWariantu { Typ = typ, ElementId = przedmiot.Id };
        var wyprawa = new Wyprawa { Data = new DateOnly(2026, 11, 11) };
        wyprawa.SkladReczny.Add(elementWyprawy);
        dane.Wyprawy.Add(wyprawa);

        CzyscicielReferencji.UsunReferencjePrzedmiotu(dane, przedmiot.Id);

        Assert.Equal("", elementZestawu.ElementId);
        Assert.Equal("", elementWyprawy.ElementId);
    }

    [Fact]
    public void UsunReferencjePrzedmiotu_NieUsuwaInnychOdwolan()
    {
        var dane = new DaneAplikacji();
        var usuwany = new Przedmiot();
        var pozostawiony = new Przedmiot();
        var element = new ElementWariantu { Typ = TypElementu.Komplet, ElementId = pozostawiony.Id };
        var wariant = new WariantSlotu();
        wariant.Elementy.Add(element);
        var slot = new SlotZestawu();
        slot.Warianty.Add(wariant);
        dane.Zestawy.Add(new Zestaw { Sloty = new List<SlotZestawu> { slot } });

        CzyscicielReferencji.UsunReferencjePrzedmiotu(dane, usuwany.Id);

        Assert.Equal(pozostawiony.Id, element.ElementId);
    }
}
