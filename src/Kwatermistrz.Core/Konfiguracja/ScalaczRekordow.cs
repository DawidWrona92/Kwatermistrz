using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Konfiguracja;

/// <summary>
/// Scalanie rekord\u00f3w z eksportu .kwreko z danymi lokalnymi (FR-1405):
/// pomija rekordy, kt\u00f3re ju\u017c istniej\u0105 (po Id), przemapowuje Id s\u0142ownik\u00f3w
/// na lokalne (mapy z <see cref="ImporterPakietuKonfiguracji.MapyId"/>).
/// Dzi\u0119ki temu ponowny import tego samego pliku nie tworzy duplikat\u00f3w.
/// </summary>
public static class ScalaczRekordow
{
    /// <summary>Wynik scalania: liczba pomini\u0119tych rekord\u00f3w (ju\u017c istnia\u0142y o tym Id).</summary>
    public record WynikScalania(int Pominiete);

    /// <summary>Scala wszystkie listy rekord\u00f3w eksportu z danymi lokalnymi i przemapowuje referencje. Zwraca liczb\u0119 pomini\u0119tych rekord\u00f3w.</summary>
    public static WynikScalania Scal(DaneAplikacji docelowe, DaneAplikacji eksport, ImporterPakietuKonfiguracji.MapyId mapy)
    {
        var pominiete = 0;

        // mapy Id eksportu -> Id lokalne (dla referencji między rekordami)
        var mapaRekonstrukcji = new Dictionary<string, string>(StringComparer.Ordinal);
        var mapaPrzedmiotow = new Dictionary<string, string>(StringComparer.Ordinal);
        var mapaReplik = new Dictionary<string, string>(StringComparer.Ordinal);
        var mapaZestawow = new Dictionary<string, string>(StringComparer.Ordinal);
        var mapaSlotowSzablonu = new Dictionary<string, string>(StringComparer.Ordinal);

        pominiete += ScalListy(docelowe.Rekonstrukcje, eksport.Rekonstrukcje, r => r.Id, mapaRekonstrukcji);
        // sloty szablonu: mapa po Id (sloty zestawów wskazują SzablonSlotId — musi wskazywać lokalny slot)
        foreach (var rekEksport in eksport.Rekonstrukcje)
            if (mapaRekonstrukcji.TryGetValue(rekEksport.Id, out var rekLokalna))
                foreach (var slotEksport in rekEksport.SzablonSlotow)
                {
                    var slotLokalny = docelowe.Rekonstrukcje.First(r => r.Id == rekLokalna).SzablonSlotow
                        .FirstOrDefault(s => s.Id == slotEksport.Id);
                    if (slotLokalny != null)
                        mapaSlotowSzablonu[slotEksport.Id] = slotLokalny.Id;
                }
        pominiete += ScalListy(docelowe.Przedmioty, eksport.Przedmioty, p => p.Id, mapaPrzedmiotow);
        pominiete += ScalListy(docelowe.Repliki, eksport.Repliki, r => r.Id, mapaReplik);
        pominiete += ScalListy(docelowe.Zestawy, eksport.Zestawy, z => z.Id, mapaZestawow);
        pominiete += ScalListy(docelowe.Zadania, eksport.Zadania, z => z.Id, new Dictionary<string, string>(StringComparer.Ordinal));
        pominiete += ScalListy(docelowe.Wyprawy, eksport.Wyprawy, w => w.Id, new Dictionary<string, string>(StringComparer.Ordinal));

        // przemapuj referencje we wszystkich rekordach lokalnych (tak\u017ce ju\u017c istniej\u0105cych \u2014 ich s\u0142owniki mog\u0142y si\u0119 zmieni\u0107)
        foreach (var p in docelowe.Przedmioty)
            PrzemapujPrzedmiot(p, mapy, mapaPrzedmiotow, mapaRekonstrukcji);
        foreach (var r in docelowe.Repliki)
            PrzemapujReplika(r, mapy, mapaRekonstrukcji);
        foreach (var z in docelowe.Zestawy)
            PrzemapujZestaw(z, mapy, mapaRekonstrukcji, mapaPrzedmiotow, mapaReplik, mapaSlotowSzablonu);
        foreach (var w in docelowe.Wyprawy)
            PrzemapujWyprawe(w, mapaRekonstrukcji, mapaReplik, mapaZestawow, mapaPrzedmiotow);
        foreach (var z in docelowe.Zadania)
            PrzemapujZadanie(z, mapy, mapaPrzedmiotow, mapaReplik, mapaZestawow);

        return new WynikScalania(pominiete);
    }

    /// <summary>Pomija rekordy o Id ju\u017c istniej\u0105cym; nowe dopisuje i zapisuje w mapie. Zwraca liczb\u0119 pomini\u0119tych.</summary>
    private static int ScalListy<T>(List<T> docelowa, List<T> eksport, Func<T, string> klucz, Dictionary<string, string> mapa) where T : class
    {
        var istniejace = docelowa.Select(klucz).ToHashSet(StringComparer.Ordinal);
        var pominiete = 0;
        foreach (var rekord in eksport)
        {
            var id = klucz(rekord);
            if (istniejace.Contains(id))
            {
                pominiete++;
                continue;
            }
            istniejace.Add(id);
            docelowa.Add(rekord);
            mapa[id] = id;
        }
        return pominiete;
    }

    private static void PrzemapujPrzedmiot(Przedmiot p, ImporterPakietuKonfiguracji.MapyId mapy,
        Dictionary<string, string> mapaPrzedmiotow, Dictionary<string, string> mapaRekonstrukcji)
    {
        p.KategoriaId = mapy.MapujKategoria(p.KategoriaId) ?? "";
        p.WzorId = mapy.MapujWzor(p.WzorId);
        p.StronyId = p.StronyId.Select(id => mapy.MapujSlownik("Strony", id) ?? id).ToList();
        p.MiejsceId = mapy.MapujSlownik("Miejsca", p.MiejsceId);
        p.PochodzenieId = mapy.MapujSlownik("Pochodzenie", p.PochodzenieId);
        p.RekonstrukcjeId = p.RekonstrukcjeId.Select(id => Remap(mapaRekonstrukcji, id)).ToList();
        if (p.KompletId != null)
            p.KompletId = Remap(mapaPrzedmiotow, p.KompletId);
        if (p.ZamontowanyNaId != null)
            p.ZamontowanyNaId = Remap(mapaPrzedmiotow, p.ZamontowanyNaId);
        foreach (var k in p.Konfiguracje)
            k.ElementyId = k.ElementyId.Select(id => Remap(mapaPrzedmiotow, id)).ToList();
        foreach (var koszt in p.Koszty)
            koszt.RodzajId = mapy.MapujSlownik("RodzajeKosztow", koszt.RodzajId) ?? "";
    }

    private static void PrzemapujReplika(Replika r, ImporterPakietuKonfiguracji.MapyId mapy,
        Dictionary<string, string> mapaRekonstrukcji)
    {
        r.StronyId = r.StronyId.Select(id => mapy.MapujSlownik("Strony", id) ?? id).ToList();
        r.RoleId = r.RoleId.Select(id => mapy.MapujSlownik("Role", id) ?? id).ToList();
        r.TypMagazynkaId = mapy.MapujSlownik("TypyMagazynkow", r.TypMagazynkaId);
        r.NapedId = mapy.MapujSlownik("Napedy", r.NapedId);
        r.StanId = mapy.MapujSlownik("StanyReplik", r.StanId);
        r.RekonstrukcjeId = r.RekonstrukcjeId.Select(id => Remap(mapaRekonstrukcji, id)).ToList();
        foreach (var koszt in r.Koszty)
            koszt.RodzajId = mapy.MapujSlownik("RodzajeKosztow", koszt.RodzajId) ?? "";
    }

    private static void PrzemapujZestaw(Zestaw z, ImporterPakietuKonfiguracji.MapyId mapy,
        Dictionary<string, string> mapaRekonstrukcji,
        Dictionary<string, string> mapaPrzedmiotow, Dictionary<string, string> mapaReplik,
        Dictionary<string, string> mapaSlotowSzablonu)
    {
        z.RekonstrukcjaId = z.RekonstrukcjaId != null ? Remap(mapaRekonstrukcji, z.RekonstrukcjaId) : null;
        z.StronaId = mapy.MapujSlownik("Strony", z.StronaId);
        foreach (var slot in z.Sloty)
        {
            if (slot.SzablonSlotId != null)
                slot.SzablonSlotId = Remap(mapaSlotowSzablonu, slot.SzablonSlotId);

            foreach (var wariant in slot.Warianty)
                foreach (var element in wariant.Elementy)
                    element.ElementId = element.Typ switch
                    {
                        TypElementu.Przedmiot => Remap(mapaPrzedmiotow, element.ElementId),
                        TypElementu.Replika => Remap(mapaReplik, element.ElementId),
                        TypElementu.Komplet => Remap(mapaPrzedmiotow, element.ElementId),
                        _ => element.ElementId,
                    };
        }
    }

    private static void PrzemapujWyprawe(Wyprawa w,
        Dictionary<string, string> mapaRekonstrukcji,
        Dictionary<string, string> mapaReplik, Dictionary<string, string> mapaZestawow,
        Dictionary<string, string> mapaPrzedmiotow)
    {
        if (w.RekonstrukcjaId != null)
            w.RekonstrukcjaId = Remap(mapaRekonstrukcji, w.RekonstrukcjaId);
        if (w.ReplikaGlownaId != null)
            w.ReplikaGlownaId = Remap(mapaReplik, w.ReplikaGlownaId);
        if (w.ZestawId != null)
            w.ZestawId = Remap(mapaZestawow, w.ZestawId);
        foreach (var element in w.SkladReczny)
            if (element.Typ is TypElementu.Przedmiot or TypElementu.Komplet)
                element.ElementId = Remap(mapaPrzedmiotow, element.ElementId);
            else if (element.Typ == TypElementu.Replika)
                element.ElementId = Remap(mapaReplik, element.ElementId);
    }

    private static void PrzemapujZadanie(Zadanie z, ImporterPakietuKonfiguracji.MapyId mapy,
        Dictionary<string, string> mapaPrzedmiotow,
        Dictionary<string, string> mapaReplik, Dictionary<string, string> mapaZestawow)
    {
        z.RodzajId = mapy.MapujSlownik("RodzajeZadan", z.RodzajId) ?? "";
        if (z.DotyczyId == null)
            return;
        z.DotyczyId = z.DotyczyTyp switch
        {
            "Przedmiot" => Remap(mapaPrzedmiotow, z.DotyczyId),
            "Replika" => Remap(mapaReplik, z.DotyczyId),
            "Zestaw" => Remap(mapaZestawow, z.DotyczyId),
            _ => z.DotyczyId,
        };
    }

    private static string Remap(Dictionary<string, string> mapa, string id)
        => mapa.TryGetValue(id, out var nowe) ? nowe : id;
}
