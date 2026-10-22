using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Obliczenia;

/// <summary>Widok obliczeniowy jednej repliki — wartości wynikające z reguł BR-01..BR-06.</summary>
public class WierszRepliki
{
    public string ReplikaId { get; set; } = "";
    public StatusWlasnosci Status { get; set; }
    public int Wyprawy { get; set; }
    public decimal ProcentWypraw { get; set; }
    public decimal KosztCalkowity { get; set; }
    public decimal CenaOryginalna { get; set; }
    public decimal KosztDodatkow { get; set; }
    public decimal KosztNaWyprawe { get; set; }
    public bool BezWypraw => Wyprawy == 0;
    public DateOnly? DataOstatniegoTestu { get; set; }
}

/// <summary>Widok obliczeniowy użycia przedmiotu — reguły BR-05, BR-08.</summary>
public class WierszUzyciaPrzedmiotu
{
    public string PrzedmiotId { get; set; } = "";
    public string? KompletId { get; set; }
    public int Uzycia { get; set; }
    public decimal Koszt { get; set; }
    public decimal KosztNaUzycie { get; set; }
}

/// <summary>Wiersz raportu umundurowania — reguły BR-05, BR-09, BR-10.</summary>
public class WierszUmundurowania
{
    public string Id { get; set; } = "";
    public string Nazwa { get; set; } = "";
    public List<string> StronyId { get; set; } = new();
    public string? WzorId { get; set; }
    public int Wyprawy { get; set; }
    public decimal ProcentWypraw { get; set; }
    public decimal Koszt { get; set; }
    public decimal KosztNaWyprawe { get; set; }
}

/// <summary>Implementacja reguł biznesowych rozdziału 8. Definicje są stałe i niekonfigurowalne (NFR-14); klasa nie zna interfejsu.</summary>
public class Kalkulator
{
    private readonly DaneAplikacji _dane;

    public Kalkulator(DaneAplikacji dane) => _dane = dane;

    /// <summary>BR-01: wyprawy repliki = licznik historyczny + zapisane wyprawy, w których jest repliką główną.</summary>
    public int WyprawyRepliki(Replika replika)
    {
        var zapisane = _dane.Wyprawy.Count(w => !w.CzyUsunieta && w.ReplikaGlownaId == replika.Id);
        return replika.LicznikHistorycznyWypraw + zapisane;
    }

    /// <summary>BR-02: łączna liczba wypraw = suma wypraw wszystkich replik, także do sprzedaży i sprzedanych.</summary>
    public int LacznaLiczbaWypraw()
    {
        var suma = _dane.Repliki.Sum(r => WyprawyRepliki(r));
        // wyprawy bez repliki głównej (gdy szablon tego nie wymaga) liczą się osobno
        suma += _dane.Wyprawy.Count(w => !w.CzyUsunieta && w.ReplikaGlownaId == null);
        return suma;
    }

    /// <summary>BR-03: procent wypraw repliki względem łącznej liczby wypraw. Przy zerowej sumie wynosi 0.</summary>
    public decimal ProcentWypraw(int wyprawyRepliki)
    {
        var suma = LacznaLiczbaWypraw();
        return suma == 0 ? 0m : (decimal)wyprawyRepliki / suma * 100m;
    }

    /// <summary>BR-04/FR-502: koszt całkowity = suma pozycji; cena oryginalna = suma pozycji rodzaju „Zakup repliki”; dodatki = reszta.</summary>
    public static (decimal Calkowity, decimal Oryginalna, decimal Dodatki) Koszty(IEnumerable<PozycjaKosztowa> pozycje)
    {
        var calkowity = pozycje.Sum(p => p.Kwota);
        var oryginalna = pozycje.Where(p => p.Semantyka == SemantykaKosztu.Zakup).Sum(p => p.Kwota);
        return (calkowity, oryginalna, calkowity - oryginalna);
    }

    /// <summary>BR-05: koszt na wyprawę = koszt / wyprawy; przy zerowej liczbie wypraw równy kosztowi całkowitemu.</summary>
    public static decimal KosztNaWyprawe(decimal koszt, int wyprawy) => wyprawy == 0 ? koszt : koszt / wyprawy;

    /// <summary>Wiersz raportu „Bronie” dla repliki (BR-01, BR-03, BR-04, BR-05).</summary>
    public WierszRepliki WierszRepliki(Replika replika)
    {
        var wyprawy = WyprawyRepliki(replika);
        var (calkowity, oryginalna, dodatki) = Koszty(replika.Koszty);
        return new WierszRepliki
        {
            ReplikaId = replika.Id,
            Status = replika.Status,
            Wyprawy = wyprawy,
            ProcentWypraw = ProcentWypraw(wyprawy),
            KosztCalkowity = calkowity,
            CenaOryginalna = oryginalna,
            KosztDodatkow = dodatki,
            KosztNaWyprawe = KosztNaWyprawe(calkowity, wyprawy),
            DataOstatniegoTestu = replika.Pomiary.Count > 0 ? replika.Pomiary.Max(p => p.Data) : null,
        };
    }

    /// <summary>BR-06: średnie i sumy kosztów tylko dla replik o statusie „posiadana”.</summary>
    public IEnumerable<Replika> ReplikiPosiadane() => _dane.Repliki.Where(r => r.Status == StatusWlasnosci.Posiadany);

    /// <summary>BR-07: wyprawy w roku R = licznik historyczny roku + zapisane wyprawy z roku R (także sprzed dnia zerowego).</summary>
    public int WyprawyWRoku(int rok)
    {
        var historyczne = _dane.Ustawienia.HistoryczneWyprawyWgRok.TryGetValue(rok, out var h) ? h : 0;
        var zapisane = _dane.Wyprawy.Count(w => !w.CzyUsunieta && w.Data.Year == rok);
        return historyczne + zapisane;
    }

    /// <summary>BR-15: data pierwszej wyprawy = ustawienie (z importu) albo najwcześniejsza zapisana wyprawa.</summary>
    public DateOnly? DataPierwszejWyprawy()
    {
        var zapisane = _dane.Wyprawy.Where(w => !w.CzyUsunieta).Select(w => (DateOnly?)w.Data).Min();
        var ustawienie = _dane.Ustawienia.DataPierwszejWyprawy;
        if (zapisane == null)
            return ustawienie;
        return ustawienie == null || zapisane < ustawienie ? zapisane : ustawienie;
    }

    /// <summary>BR-08: użycia przedmiotu = licznik historyczny + wyprawy, w których przedmiot jest w składzie. Elementy kompletu mają wspólną liczbę użyć.</summary>
    public int UzyciaPrzedmiotu(Przedmiot przedmiot)
    {
        // element kompletu ma wspólny licznik z kompletem (BR-08)
        if (przedmiot.KompletId != null)
        {
            var komplet = _dane.Przedmioty.FirstOrDefault(p => p.Id == przedmiot.KompletId);
            if (komplet != null && komplet.Id != przedmiot.Id)
                return UzyciaPrzedmiotu(komplet);
        }
        var wyprawyPrzedmiotu = _dane.Wyprawy.Count(w => !w.CzyUsunieta && CzyWnaSkładzie(w, przedmiot.Id));
        var licznik = przedmiot.LicznikHistoryczny;
        return licznik + wyprawyPrzedmiotu;
    }

    /// <summary>Czy przedmiot jest w składzie wyprawy (przez wybrane warianty zestawu lub skład ręczny).</summary>
    public bool CzyWnaSkładzie(Wyprawa wyprawa, string przedmiotId)
    {
        if (wyprawa.ZestawId != null)
        {
            var zestaw = _dane.Zestawy.FirstOrDefault(z => z.Id == wyprawa.ZestawId);
            if (zestaw == null)
                return false;
            foreach (var (slotId, indeks) in wyprawa.WybraneWarianty)
            {
                var slot = zestaw.Sloty.FirstOrDefault(s => s.Id == slotId);
                if (slot == null || indeks < 0 || indeks >= slot.Warianty.Count)
                    continue;
                if (slot.Warianty[indeks].Elementy.Any(e => e.Typ == TypElementu.Przedmiot && e.ElementId == przedmiotId))
                    return true;
                if (slot.Warianty[indeks].Elementy.Any(e => e.Typ == TypElementu.Komplet && CzyWKomplecie(e.ElementId, przedmiotId)))
                    return true;
            }
            return false;
        }
        return wyprawa.SkladReczny.Any(e =>
            (e.Typ == TypElementu.Przedmiot && e.ElementId == przedmiotId) ||
            (e.Typ == TypElementu.Komplet && CzyWKomplecie(e.ElementId, przedmiotId)));
    }

    /// <summary>Czy przedmiot należy do kompletu o podanym Id.</summary>
    public bool CzyWKomplecie(string kompletId, string przedmiotId)
    {
        var komplet = _dane.Przedmioty.FirstOrDefault(p => p.KompletId == kompletId && p.Id == kompletId);
        if (komplet != null)
            return true;
        return _dane.Przedmioty.Any(p => p.KompletId == kompletId && p.Id == przedmiotId);
    }

    /// <summary>BR-09: mundur wyprawy = elementy ze slotów oznaczonych jako sloty munduru, traktowane jako komplet; przy mieszance pierwszy element, oznaczenie „mieszanka”.</summary>
    public (string? MundurId, bool CzyMieszanka) MundurWyprawy(Wyprawa wyprawa)
    {
        if (wyprawa.ZestawId == null)
            return (null, false);
        var zestaw = _dane.Zestawy.FirstOrDefault(z => z.Id == wyprawa.ZestawId);
        if (zestaw == null)
            return (null, false);
        var rekonstrukcja = wyprawa.RekonstrukcjaId != null
            ? _dane.Rekonstrukcje.FirstOrDefault(r => r.Id == wyprawa.RekonstrukcjaId)
            : _dane.Rekonstrukcje.FirstOrDefault(r => r.Id == zestaw.RekonstrukcjaId);
        if (rekonstrukcja == null)
            return (null, false);
        var slotyMunduru = rekonstrukcja.SzablonSlotow.Where(s => s.CzySlotMunduru).Select(s => s.Nazwa).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var elementy = new List<ElementWariantu>();
        foreach (var (slotId, indeks) in wyprawa.WybraneWarianty)
        {
            var slot = zestaw.Sloty.FirstOrDefault(s => s.Id == slotId);
            if (slot == null || !slotyMunduru.Contains(slot.Nazwa) || indeks < 0 || indeks >= slot.Warianty.Count)
                continue;
            elementy.AddRange(slot.Warianty[indeks].Elementy);
        }
        if (elementy.Count == 0)
            return (null, false);
        var pierwszy = elementy[0];
        var id = pierwszy.Typ == TypElementu.Replika ? null : pierwszy.ElementId;
        return (id, elementy.Count > 1);
    }

    /// <summary>BR-08 w wariancie kompletu: użycia kompletu = użycia dowolnego z jego elementów (wspólny licznik).</summary>
    public WierszUzyciaPrzedmiotu WierszUzycia(Przedmiot przedmiot)
    {
        var uzycia = UzyciaPrzedmiotu(przedmiot);
        var koszt = Koszty(przedmiot.Koszty).Calkowity;
        return new WierszUzyciaPrzedmiotu
        {
            PrzedmiotId = przedmiot.Id,
            KompletId = przedmiot.KompletId,
            Uzycia = uzycia,
            Koszt = koszt,
            KosztNaUzycie = KosztNaWyprawe(koszt, uzycia),
        };
    }

    /// <summary>BR-10/FR-902: koszt munduru w raporcie = suma cen gór i dołów kompletu (pozycje „Zakup”), bez dodatków.</summary>
    public decimal KosztMunduru(Przedmiot mundur)
    {
        if (mundur.KompletId == null)
            return Koszty(mundur.Koszty).Oryginalna;
        var elementyKompletu = _dane.Przedmioty.Where(p => p.KompletId == mundur.KompletId || p.Id == mundur.KompletId);
        return elementyKompletu.Sum(p => Koszty(p.Koszty).Oryginalna);
    }

    /// <summary>Liczba wypraw zestawu (FR-709) i procent względem łącznej liczby wypraw.</summary>
    public (int Wyprawy, decimal Procent, DateOnly? OstatnieUzycie) StatystykiZestawu(Zestaw zestaw)
    {
        var wyprawy = _dane.Wyprawy.Where(w => !w.CzyUsunieta && w.ZestawId == zestaw.Id).ToList();
        var liczba = wyprawy.Count;
        var ostatnie = wyprawy.Count > 0 ? wyprawy.Max(w => (DateOnly?)w.Data) : null;
        return (liczba, ProcentWypraw(liczba), ostatnie);
    }

    /// <summary>BR-16: element widoczny w aktywnej rekonstrukcji, gdy należy do niej lub nie ma przypisania (wspólny).</summary>
    public static bool WidocznyDlaRekonstrukcji(List<string> przypisania, string? aktywnaRekonstrukcjaId)
    {
        if (aktywnaRekonstrukcjaId == null)
            return true;
        return przypisania.Count == 0 || przypisania.Contains(aktywnaRekonstrukcjaId);
    }

    /// <summary>FR-104: liczba otwartych awarii (zadań o semantyce „Awaria” ze statusem otwartym).</summary>
    public int OtwarteAwaria() => _dane.Zadania.Count(z =>
        z.Status == StatusZadania.Otwarte && z.Semantyka == SemantykaZadania.Awaria);

    /// <summary>FR-104: liczba wszystkich otwartych zadań (także do kupienia i do zrobienia).</summary>
    public int OtwarteZadania() => _dane.Zadania.Count(z => z.Status == StatusZadania.Otwarte);

    /// <summary>FR-104: repliki posiadane, których ostatni test (pomiar) jest starszy niż próg dni lub których nigdy nie testowano. Repliki bez żadnego pomiaru liczą się od „zawsze”.</summary>
    public List<Replika> ReplikiBezTestu(int progDni)
    {
        var dzis = DateOnly.FromDateTime(DateTime.Today);
        var prog = dzis.AddDays(-progDni);
        return _dane.Repliki
            .Where(r => r.Status == StatusWlasnosci.Posiadany)
            .Where(r => r.Pomiary.Count == 0 || r.Pomiary.Max(p => p.Data) < prog)
            .ToList();
    }

    /// <summary>FR-104: liczba przedmiotów posiadanych bez żadnego użycia (BR-08, łącznie z licznikiem historycznym).</summary>
    public int PrzedmiotyBezUzycia() => _dane.Przedmioty
        .Count(p => p.Status == StatusWlasnosci.Posiadany && UzyciaPrzedmiotu(p) == 0);

    /// <summary>FR-104: liczba pól kart technicznych oznaczonych jako „nieznane” do uzupełnienia.</summary>
    public int PolaNieznane() => _dane.Repliki
        .Sum(r => r.KartaTechniczna.Count(k => k.Value.Stan == StanWartosciPola.Nieznane));

    /// <summary>FR-105: ranking replik posiadanych wg liczby wypraw (najwięcej na górze), z kosztem na wyprawę (BR-05).</summary>
    public List<WierszRepliki> RankingReplik(int liczba)
    {
        return ReplikiPosiadane()
            .Select(WierszRepliki)
            .OrderByDescending(w => w.Wyprawy)
            .ThenBy(w => w.ReplikaId, StringComparer.Ordinal)
            .Take(liczba)
            .ToList();
    }

    /// <summary>FR-910: trzy sumy kontrolne — wg replik, wg lat, wg mundurów.</summary>
    public (int WgReplik, int WgLat, int WgMundur, int BezRepliki, int BezMunduru) KontrolaSpojnosci()
    {
        var wgReplik = LacznaLiczbaWypraw();
        var lata = _dane.Ustawienia.HistoryczneWyprawyWgRok.Keys
            .Concat(_dane.Wyprawy.Where(w => !w.CzyUsunieta).Select(w => w.Data.Year))
            .Distinct();
        var wgLat = lata.Sum(WyprawyWRoku);
        var bezRepliki = _dane.Wyprawy.Count(w => !w.CzyUsunieta && w.ReplikaGlownaId == null);
        wgReplik = _dane.Repliki.Sum(r => WyprawyRepliki(r)) + bezRepliki;
        var bezMunduru = 0;
        var wgMundur = 0;
        foreach (var wyprawa in _dane.Wyprawy.Where(w => !w.CzyUsunieta))
        {
            var (mundurId, _) = MundurWyprawy(wyprawa);
            if (mundurId == null)
                bezMunduru++;
            else
                wgMundur++;
        }
        // historyczne liczniki mundurów: tylko przedmioty kategorii „Mundury” (tabela umundurowania, 13.2)
        var kategoriaMundury = _dane.Slowniki.Kategorie.FirstOrDefault(k => k.CzyNosnik == false && Normalizacja.TekstDoPorownania(k.Nazwa).Contains("mundur"));
        if (kategoriaMundury != null)
            wgMundur += _dane.Przedmioty.Where(p => p.KategoriaId == kategoriaMundury.Id).Sum(p => p.LicznikHistoryczny);
        return (wgReplik, wgLat, wgMundur, bezRepliki, bezMunduru);
    }
}
