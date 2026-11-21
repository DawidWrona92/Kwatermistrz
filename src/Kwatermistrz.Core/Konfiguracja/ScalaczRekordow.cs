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
        var noweRekonstrukcje = new List<Rekonstrukcja>();
        var nowePrzedmioty = new List<Przedmiot>();
        var noweRepliki = new List<Replika>();
        var noweZestawy = new List<Zestaw>();
        var noweZadania = new List<Zadanie>();
        var noweWyprawy = new List<Wyprawa>();

        // Pakiet konfiguracji mógł już scalić rekonstrukcję po nazwie z lokalną pod innym Id.
        // Dwa różne Id z identyczną nazwą nie powinny tworzyć zdublowanych profili.
        foreach (var rekEksport in eksport.Rekonstrukcje)
        {
            var lokalna = docelowe.Rekonstrukcje.FirstOrDefault(r => r.Id == rekEksport.Id)
                ?? docelowe.Rekonstrukcje.FirstOrDefault(r =>
                    Normalizacja.TekstDoPorownania(r.Nazwa) == Normalizacja.TekstDoPorownania(rekEksport.Nazwa));
            if (lokalna != null)
            {
                mapaRekonstrukcji[rekEksport.Id] = lokalna.Id;
                pominiete++;
            }
            else
            {
                docelowe.Rekonstrukcje.Add(rekEksport);
                noweRekonstrukcje.Add(rekEksport);
                mapaRekonstrukcji[rekEksport.Id] = rekEksport.Id;
            }
        }
        // Mapujemy sloty szablonu nie tylko po Id, ale też po nazwie, jeśli profile już
        // istniały w obu instalacjach pod różnymi identyfikatorami.
        foreach (var rekEksport in eksport.Rekonstrukcje)
            if (mapaRekonstrukcji.TryGetValue(rekEksport.Id, out var rekLokalna))
            {
                var lokalna = docelowe.Rekonstrukcje.First(r => r.Id == rekLokalna);
                foreach (var slotEksport in rekEksport.SzablonSlotow)
                {
                    var slotLokalny = lokalna.SzablonSlotow.FirstOrDefault(s => s.Id == slotEksport.Id)
                        ?? lokalna.SzablonSlotow.FirstOrDefault(s =>
                            Normalizacja.TekstDoPorownania(s.Nazwa) ==
                            Normalizacja.TekstDoPorownania(slotEksport.Nazwa));
                    if (slotLokalny != null)
                        mapaSlotowSzablonu[slotEksport.Id] = slotLokalny.Id;
                }
            }
        pominiete += ScalListy(docelowe.Przedmioty, eksport.Przedmioty, p => p.Id, mapaPrzedmiotow, nowePrzedmioty);
        pominiete += ScalListy(docelowe.Repliki, eksport.Repliki, r => r.Id, mapaReplik, noweRepliki);
        pominiete += ScalListy(docelowe.Zestawy, eksport.Zestawy, z => z.Id, mapaZestawow, noweZestawy);
        pominiete += ScalListy(docelowe.Zadania, eksport.Zadania, z => z.Id, new Dictionary<string, string>(StringComparer.Ordinal), noweZadania);
        pominiete += ScalListy(docelowe.Wyprawy, eksport.Wyprawy, w => w.Id, new Dictionary<string, string>(StringComparer.Ordinal), noweWyprawy);

        // ZMIENIAMY WYŁĄCZNIE NOWO ZAIMPORTOWANE REKORDY.
        // Mapy pakietu zawierają Id obcego źródła, a nie pełny słownik lokalnej bazy.
        // Przepisanie wszystkich lokalnych rekordów zerowałoby ich kategorie i referencje.
        foreach (var p in nowePrzedmioty)
            PrzemapujPrzedmiot(p, mapy, mapaPrzedmiotow, mapaRekonstrukcji);
        foreach (var r in noweRepliki)
            PrzemapujReplika(r, mapy, mapaRekonstrukcji);
        foreach (var z in noweZestawy)
            PrzemapujZestaw(z, mapy, mapaRekonstrukcji, mapaPrzedmiotow, mapaReplik, mapaSlotowSzablonu);
        foreach (var w in noweWyprawy)
            PrzemapujWyprawe(w, mapaRekonstrukcji, mapaReplik, mapaZestawow, mapaPrzedmiotow);
        foreach (var z in noweZadania)
            PrzemapujZadanie(z, mapy, mapaPrzedmiotow, mapaReplik, mapaZestawow);

        return new WynikScalania(pominiete);
    }

    /// <summary>Pomija rekordy o Id ju\u017c istniej\u0105cym; nowe dopisuje i zapisuje w mapie. Zwraca liczb\u0119 pomini\u0119tych.</summary>
    private static int ScalListy<T>(List<T> docelowa, List<T> eksport, Func<T, string> klucz,
        Dictionary<string, string> mapa, List<T> dodane) where T : class
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
            dodane.Add(rekord);
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
