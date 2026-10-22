using System.Globalization;
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
        // element kompletu ma wspólny licznik z kompletem (BR-08); pętla zamiast rekurencji — dane mogą zawierać cykl kompletów (A↔B), który przy rekurencji dawałby StackOverflow
        var aktualny = przedmiot;
        var odwiedzone = new HashSet<string>(StringComparer.Ordinal);
        while (aktualny.KompletId != null && aktualny.KompletId != aktualny.Id && odwiedzone.Add(aktualny.Id))
        {
            var komplet = _dane.Przedmioty.FirstOrDefault(p => p.Id == aktualny.KompletId);
            if (komplet == null)
                break;
            aktualny = komplet;
        }
        var wyprawyPrzedmiotu = _dane.Wyprawy.Count(w => !w.CzyUsunieta && CzyWnaSkładzie(w, aktualny.Id));
        var licznik = aktualny.LicznikHistoryczny;
        return licznik + wyprawyPrzedmiotu;
    }

    /// <summary>Czy ustawienie kompletu dla przedmiotu utworzyłoby cykl (przedmiot byłby własnym przodkiem). Strażnik przy łączeniu w komplet (FR-206).</summary>
    public bool CzyTworzyCyklKompletu(string przedmiotId, string nowyKompletId)
    {
        var odwiedzone = new HashSet<string>(StringComparer.Ordinal);
        var aktualny = _dane.Przedmioty.FirstOrDefault(p => p.Id == nowyKompletId);
        while (aktualny != null && odwiedzone.Add(aktualny.Id))
        {
            if (aktualny.Id == przedmiotId)
                return true;
            aktualny = aktualny.KompletId == null
                ? null
                : _dane.Przedmioty.FirstOrDefault(p => p.Id == aktualny.KompletId);
        }
        return false;
    }

    /// <summary>Rozwinięcie elementu na konkretne Id przedmiotów (FR-208: konfiguracja nośnika → jej elementy; komplet → części; przedmiot → sam siebie). Replika nie jest przedmiotem — zwraca pustą listę (broń obsługują osobne ścieżki: KosztElementu, lista pakowania przez Typ).</summary>
    public List<string> RozwinNaPrzedmioty(ElementWariantu element)
    {
        switch (element.Typ)
        {
            case TypElementu.Przedmiot:
                return new List<string> { element.ElementId };
            case TypElementu.Komplet:
                return _dane.Przedmioty.Where(p => p.KompletId == element.ElementId || p.Id == element.ElementId)
                    .Select(p => p.Id).ToList();
            case TypElementu.KonfiguracjaNosnika:
                var konfiguracja = _dane.Przedmioty.SelectMany(p => p.Konfiguracje).FirstOrDefault(k => k.Id == element.ElementId);
                return konfiguracja?.ElementyId.ToList() ?? new List<string>();
            default:
                return new List<string>();
        }
    }

    /// <summary>Czy przedmiot jest w składzie wyprawy (przez wybrane warianty zestawu lub skład ręczny; FR-208: konfiguracja nośnika liczy się przez swoje elementy).</summary>
    public bool CzyWnaSkładzie(Wyprawa wyprawa, string przedmiotId)
    {
        bool CzyWElementach(IEnumerable<ElementWariantu> elementy) =>
            elementy.Any(e => RozwinNaPrzedmioty(e).Contains(przedmiotId));
        if (wyprawa.ZestawId != null)
        {
            var zestaw = _dane.Zestawy.FirstOrDefault(z => z.Id == wyprawa.ZestawId);
            if (zestaw == null)
                return false;
            foreach (var (slotId, wariantId) in wyprawa.WybraneWarianty)
            {
                var slot = zestaw.Sloty.FirstOrDefault(s => s.Id == slotId);
                var wariant = slot?.Warianty.FirstOrDefault(w => w.Id == wariantId);
                if (wariant == null)
                    continue;
                if (CzyWElementach(wariant.Elementy))
                    return true;
            }
            return false;
        }
        return CzyWElementach(wyprawa.SkladReczny);
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
        foreach (var (slotId, wariantId) in wyprawa.WybraneWarianty)
        {
            var slot = zestaw.Sloty.FirstOrDefault(s => s.Id == slotId);
            var wariant = slot?.Warianty.FirstOrDefault(w => w.Id == wariantId);
            if (slot == null || wariant == null || !slotyMunduru.Contains(slot.Nazwa))
                continue;
            elementy.AddRange(wariant.Elementy);
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

    /// <summary>FR-707: elementy listy pakowania dla wariantów domyślnych zestawu (lub wskazanych Id wariantów).
    /// Elementy kompletu rozwinięte osobno (jako Przedmiot); repliki i konfiguracje nośnika jako osobne elementy
    /// (z oryginalnym Typ — lista pakowania pokazuje też broń, FR-707). Zwraca (element, slot, indeksWariantu w liście — tylko do wyświetlenia).</summary>
    public List<(ElementWariantu Element, SlotZestawu Slot, int IndeksWariantu)> ElementyListyPakowania(Zestaw zestaw, IReadOnlyDictionary<string, string>? wybraneWarianty = null)
    {
        var wynik = new List<(ElementWariantu, SlotZestawu, int)>();
        foreach (var slot in zestaw.Sloty.OrderBy(s => s.Kolejnosc))
        {
            // wybór po Id wariantu; brak wyboru = wariant domyślny (po Id, potem pierwszy)
            var wariant = wybraneWarianty != null && wybraneWarianty.TryGetValue(slot.Id, out var wariantId)
                ? slot.Warianty.FirstOrDefault(w => w.Id == wariantId)
                : null;
            wariant ??= slot.Warianty.FirstOrDefault(w => w.Id == slot.WariantDomyslnyId) ?? slot.Warianty.FirstOrDefault();
            if (wariant == null || wariant.CzyPuste)
                continue;
            var indeks = slot.Warianty.IndexOf(wariant);
            foreach (var element in wariant.Elementy)
            {
                if (element.Typ == TypElementu.Komplet)
                {
                    foreach (var przedmiotId in RozwinNaPrzedmioty(element))
                        wynik.Add((new ElementWariantu { Typ = TypElementu.Przedmiot, ElementId = przedmiotId }, slot, indeks));
                }
                else
                {
                    // Przedmiot, Replika, KonfiguracjaNosnika — jako oryginalny element (bez gubienia replik)
                    wynik.Add((element, slot, indeks));
                }
            }
        }
        return wynik;
    }

    /// <summary>FR-708: koszt zestawu — suma cen elementów (pozycje „Zakup”, BR-04) wariantów domyślnych, w tym replik. Zwraca koszt domyślny.</summary>
    public decimal KosztZestawu(Zestaw zestaw)
    {
        decimal suma = 0;
        foreach (var (element, _, _) in ElementyListyPakowania(zestaw))
            suma += KosztElementu(element);
        return suma;
    }

    /// <summary>FR-708 [C]: zakres kosztu zestawu — minimum i maksimum po wszystkich kombinacjach wariantów (każdy slot wybiera jeden wariant; wariant „puste” = 0). Dla dużych zestawów ogranicza liczbę kombinacji przez sumę min/max na slot.</summary>
    public (decimal Minimum, decimal Maksimum) ZakresKosztuZestawu(Zestaw zestaw)
    {
        decimal min = 0, max = 0;
        foreach (var slot in zestaw.Sloty)
        {
            var kosztyWariantow = slot.Warianty
                .Select(w => w.CzyPuste ? 0m : w.Elementy.Sum(e => KosztElementu(e)))
                .ToList();
            if (kosztyWariantow.Count == 0)
                continue;
            min += kosztyWariantow.Min();
            max += kosztyWariantow.Max();
        }
        return (min, max);
    }

    private decimal KosztElementu(ElementWariantu element)
    {
        if (element.Typ == TypElementu.Przedmiot)
        {
            var p = _dane.Przedmioty.FirstOrDefault(x => x.Id == element.ElementId);
            return p != null ? Koszty(p.Koszty).Oryginalna : 0m;
        }
        if (element.Typ == TypElementu.Replika)
        {
            var r = _dane.Repliki.FirstOrDefault(x => x.Id == element.ElementId);
            return r != null ? Koszty(r.Koszty).Oryginalna : 0m;
        }
        if (element.Typ == TypElementu.Komplet)
            return _dane.Przedmioty.Where(p => p.KompletId == element.ElementId || p.Id == element.ElementId)
                .Sum(p => Koszty(p.Koszty).Oryginalna);
        if (element.Typ == TypElementu.KonfiguracjaNosnika)
            return RozwinNaPrzedmioty(element)
                .Select(id => _dane.Przedmioty.FirstOrDefault(p => p.Id == id))
                .Where(p => p != null)
                .Sum(p => Koszty(p!.Koszty).Oryginalna);
        return 0m;
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

    /// <summary>
    /// FR-807: wpływ wyprawy sprzed/dnia zerowego na liczniki historyczne — kandydaci do odjęcia
    /// (replika, mundur, rok). Zwraca pustą listę, gdy wyprawa jest po dniu zerowym lub nie ma wpływu.
    /// </summary>
    public List<(string Rodzaj, string Id, string Opis)> WplywNaLicznikiHistoryczne(Wyprawa wyprawa)
    {
        var wynik = new List<(string, string, string)>();
        var dzienZerowy = _dane.Ustawienia.DzienZerowy;
        if (dzienZerowy == null || wyprawa.Data > dzienZerowy.Value)
            return wynik;
        if (wyprawa.ReplikaGlownaId != null)
        {
            var replika = _dane.Repliki.FirstOrDefault(r => r.Id == wyprawa.ReplikaGlownaId);
            if (replika != null)
                wynik.Add(("Replika", replika.Id, $"{replika.ModelProducenta}: −1 wyprawa (licznik historyczny)"));
        }
        var (mundurId, _) = MundurWyprawy(wyprawa);
        if (mundurId != null)
        {
            var mundur = _dane.Przedmioty.FirstOrDefault(p => p.Id == mundurId);
            if (mundur != null)
                wynik.Add(("Mundur", mundur.Id, $"{mundur.Nazwa}: −1 użycie (licznik historyczny)"));
        }
        wynik.Add(("Rok", wyprawa.Data.Year.ToString(CultureInfo.InvariantCulture),
            $"Rok {wyprawa.Data.Year}: −1 wyprawa (licznik historyczny wg lat)"));
        return wynik;
    }
}
