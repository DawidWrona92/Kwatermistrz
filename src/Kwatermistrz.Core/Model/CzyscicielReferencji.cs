namespace Kwatermistrz.Core.Model;

/// <summary>Czyszczenie referencji po usunięciu rekordu (raport przeglądu #17):
/// usunięcie repliki/przedmiotu bez czyszczenia zostawia wiszące Id w wyprawach, slotach zestawów,
/// zadaniach, kompletach, montażu i konfiguracjach — co psuje sumy (BR-02) i zostawia otwarte „awarie”.
/// Elementy wariantów trzymane w wyprawach tylko w <see cref="Wyprawa.SkladReczny"/> (wyprawa bez zestawu);
/// przy zestawie wybór wariantów wskazuje po Id wariantu, więc usunięcie rekordu nie psuje wyprawy.</summary>
public static class CzyscicielReferencji
{
    /// <summary>Usuwa wszelkie odwołania do repliki o podanym Id (bez samego rekordu) oraz jej zadania.</summary>
    public static void UsunReferencjeRepliki(DaneAplikacji dane, string replikaId)
    {
        foreach (var w in dane.Wyprawy)
        {
            if (w.ReplikaGlownaId == replikaId)
                w.ReplikaGlownaId = null;
            foreach (var element in w.SkladReczny)
                if (element.Typ == TypElementu.Replika && element.ElementId == replikaId)
                    element.ElementId = "";
        }
        foreach (var z in dane.Zestawy)
            foreach (var element in ElementyZestawu(z))
                if (element.Typ == TypElementu.Replika && element.ElementId == replikaId)
                    element.ElementId = "";
        dane.Zadania.RemoveAll(z => z.DotyczyTyp == "Replika" && z.DotyczyId == replikaId);
    }

    /// <summary>Usuwa wszelkie odwołania do przedmiotu o podanym Id (bez samego rekordu) oraz jego zadania.</summary>
    public static void UsunReferencjePrzedmiotu(DaneAplikacji dane, string przedmiotId)
    {
        foreach (var p in dane.Przedmioty)
        {
            if (p.ZamontowanyNaId == przedmiotId)
                p.ZamontowanyNaId = null;
            if (p.KompletId == przedmiotId)
                p.KompletId = null;
            foreach (var konfiguracja in p.Konfiguracje)
                konfiguracja.ElementyId.RemoveAll(id => id == przedmiotId);
        }
        foreach (var z in dane.Zestawy)
            foreach (var element in ElementyZestawu(z))
                if (element.Typ == TypElementu.Przedmiot && element.ElementId == przedmiotId)
                    element.ElementId = "";
        foreach (var w in dane.Wyprawy)
            foreach (var element in w.SkladReczny)
                if (element.Typ == TypElementu.Przedmiot && element.ElementId == przedmiotId)
                    element.ElementId = "";
        dane.Zadania.RemoveAll(z => z.DotyczyTyp == "Przedmiot" && z.DotyczyId == przedmiotId);
    }

    private static IEnumerable<ElementWariantu> ElementyZestawu(Zestaw zestaw)
    {
        foreach (var slot in zestaw.Sloty)
            foreach (var wariant in slot.Warianty)
                foreach (var element in wariant.Elementy)
                    yield return element;
    }
}
