namespace Kwatermistrz.Core.Model;

/// <summary>Wartość pola karty technicznej z trzema stanami (FR-404).</summary>
public class WartoscPolaKarty
{
    public string? Tekst { get; set; }
    public StanWartosciPola Stan { get; set; } = StanWartosciPola.NieUzupelnione;
}

/// <summary>Przedmiot katalogu wyposażenia (rozdział 6.1).</summary>
public class Przedmiot
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string KategoriaId { get; set; } = "";
    public string? Firma { get; set; }
    public string Nazwa { get; set; } = "";
    public string? WzorId { get; set; }
    public List<string> StronyId { get; set; } = new();
    public string? MiejsceId { get; set; }
    public string? PochodzenieId { get; set; }
    public decimal? CenaZakupu { get; set; }
    public DateOnly? DataZakupu { get; set; }
    public StatusWlasnosci Status { get; set; } = StatusWlasnosci.Posiadany;
    public DateOnly? DataSprzedazy { get; set; }
    public decimal? CenaSprzedazy { get; set; }
    public List<string> RekonstrukcjeId { get; set; } = new();
    public int LicznikHistoryczny { get; set; }
    public bool LicznikHistorycznyDoWeryfikacji { get; set; }
    public string? Komentarz { get; set; }
    public List<string> Zdjecia { get; set; } = new();
    /// <summary>Id nośnika, na którym zamontowany jest przedmiot (FR-207); null gdy nie zamontowany.</summary>
    public string? ZamontowanyNaId { get; set; }
    /// <summary>Id kompletu, do którego należy przedmiot (FR-206); null gdy nie należy.</summary>
    public string? KompletId { get; set; }
    /// <summary>Nazwane konfiguracje nośnika (FR-208): np. „Skanda Light” jako lista elementów zamontowanych na tym nośniku.</summary>
    public List<KonfiguracjaNosnika> Konfiguracje { get; set; } = new();
    public List<PozycjaKosztowa> Koszty { get; set; } = new();
    public bool ZnacznikZImportu { get; set; }
    public bool DoUzupelnienia { get; set; }
    public DateTime OstatniaZmiana { get; set; } = DateTime.Now;

    /// <summary>FR-503: synchronizuje uproszczone pole „cena zakupu” z pozycjami kosztowymi — tworzy lub aktualizuje pozycję rodzaju „Zakup” (semantyka stała BR-04). Wywoływane przy zapisie karty przedmiotu.</summary>
    public void SynchronizujCenaZakupu(Slowniki slowniki, DateOnly dataZakupu)
    {
        var rodzajZakup = slowniki.RodzajeKosztow.FirstOrDefault(r => r.Semantyka == nameof(SemantykaKosztu.Zakup));
        // pole zarządza tylko własną pozycją (rozpoznawaną po opisie); ręczne pozycje zakupu zostają
        var pozycjaZakupu = Koszty.FirstOrDefault(k => k.Opis == "Cena zakupu");
        if (CenaZakupu == null || CenaZakupu == 0m)
        {
            // brak ceny — pozycja zakupu znika (jeśli była tylko z pola „cena zakupu”)
            if (pozycjaZakupu != null && pozycjaZakupu.Opis == "Cena zakupu")
                Koszty.Remove(pozycjaZakupu);
            return;
        }
        if (pozycjaZakupu == null)
        {
            Koszty.Add(new PozycjaKosztowa
            {
                Data = dataZakupu,
                Opis = "Cena zakupu",
                Kwota = CenaZakupu.Value,
                RodzajId = rodzajZakup?.Id ?? "",
                Semantyka = SemantykaKosztu.Zakup,
            });
        }
        else
        {
            pozycjaZakupu.Kwota = CenaZakupu.Value;
            if (DataZakupu != null)
                pozycjaZakupu.Data = DataZakupu.Value;
        }
    }
}

/// <summary>Nazwana konfiguracja nośnika (FR-208): lista elementów (przedmiotów) zamontowanych na nośniku w tej konfiguracji; może być użyta jako wariant slotu w zestawie.</summary>
public class KonfiguracjaNosnika
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Nazwa { get; set; } = "";
    /// <summary>Id elementów (przedmiotów) w konfiguracji.</summary>
    public List<string> ElementyId { get; set; } = new();
}

/// <summary>Replika ASG (rozdział 6.2).</summary>
public class Replika
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ModelProducenta { get; set; } = "";
    public string? Prototyp { get; set; }
    public List<string> StronyId { get; set; } = new();
    public List<string> RoleId { get; set; } = new();
    public string? TypMagazynkaId { get; set; }
    public string? NapedId { get; set; }
    public string? StanId { get; set; }
    public decimal? EnergiaDocelowaJ { get; set; }
    public string? Komentarz { get; set; }
    public StatusWlasnosci Status { get; set; } = StatusWlasnosci.Posiadany;
    public DateOnly? DataSprzedazy { get; set; }
    public decimal? CenaSprzedazy { get; set; }
    public List<string> RekonstrukcjeId { get; set; } = new();
    public int LicznikHistorycznyWypraw { get; set; }
    public List<string> Zdjecia { get; set; } = new();
    /// <summary>Id szablonu karty technicznej użytego dla tej repliki; null gdy brak dopasowania (FR-1307).</summary>
    public string? SzablonKartyId { get; set; }
    /// <summary>Parametry karty: klucz to Id parametru szablonu, wartość to stan pola (FR-401, FR-404).</summary>
    public Dictionary<string, WartoscPolaKarty> KartaTechniczna { get; set; } = new();
    public List<Pomiar> Pomiary { get; set; } = new();
    public List<Modyfikacja> Modyfikacje { get; set; } = new();
    public List<PozycjaKosztowa> Koszty { get; set; } = new();
    public bool ZnacznikZImportu { get; set; }
    public bool DoUzupelnienia { get; set; }
    public DateTime OstatniaZmiana { get; set; } = DateTime.Now;
}

/// <summary>Pozycja kosztowa (rozdział 6.4, FR-501).</summary>
public class PozycjaKosztowa
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateOnly Data { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string Opis { get; set; } = "";
    public decimal Kwota { get; set; }
    /// <summary>Id wartości słownika rodzajów kosztów.</summary>
    public string RodzajId { get; set; } = "";
    /// <summary>Stała semantyka odwzorowana ze słownika przy zapisie (BR-04, NFR-14).</summary>
    public SemantykaKosztu Semantyka { get; set; } = SemantykaKosztu.Inne;
    public string? Zrodlo { get; set; }
}

/// <summary>Zmiana parametru karty z wartościami przed i po (FR-504).</summary>
public class ZmianaParametru
{
    public string ParametrId { get; set; } = "";
    public string? WartoscPrzed { get; set; }
    public string? WartoscPo { get; set; }
}

/// <summary>Modyfikacja repliki (rozdział 6.4, FR-504).</summary>
public class Modyfikacja
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateOnly Data { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string Opis { get; set; } = "";
    public List<ZmianaParametru> Zmiany { get; set; } = new();
    public List<string> PozycjeKosztoweId { get; set; } = new();
}

/// <summary>Pomiar lub test repliki (rozdział 6.4, FR-408).</summary>
public class Pomiar
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateOnly Data { get; set; }
    public decimal? MasaKulkiG { get; set; }
    public decimal? EnergiaJ { get; set; }
    public decimal? Rof { get; set; }
    public string? UstawienieHopUp { get; set; }
    public int? ZasiegM { get; set; }
    public WynikTestu? Wynik { get; set; }
    public string? Komentarz { get; set; }
    /// <summary>Pomiar osiągnął idealną, maksymalną energię dla repliki (serduszko w arkuszu, FR-411).</summary>
    public bool NaMaksimum { get; set; }
}

/// <summary>Zadanie lub usterka (rozdział 6.4, FR-601).</summary>
public class Zadanie
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>Id repliki, przedmiotu lub puste, gdy dotyczy niczego konkretnego.</summary>
    public string? DotyczyTyp { get; set; }
    public string? DotyczyId { get; set; }
    public string RodzajId { get; set; } = "";
    public SemantykaZadania Semantyka { get; set; } = SemantykaZadania.DoZrobienia;
    public string Opis { get; set; } = "";
    public StatusZadania Status { get; set; } = StatusZadania.Otwarte;
    public DateOnly DataUtworzenia { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? DataZamkniecia { get; set; }
    public bool ZnacznikZImportu { get; set; }
}

/// <summary>Zestaw (rozdział 6.3, FR-701).</summary>
public class Zestaw
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Nazwa { get; set; } = "";
    public string? RekonstrukcjaId { get; set; }
    public string? StronaId { get; set; }
    public string? Opis { get; set; }
    public bool CzySzkic { get; set; }
    public List<string> Zdjecia { get; set; } = new();
    public List<SlotZestawu> Sloty { get; set; } = new();
    /// <summary>Ostatnio użyte warianty (FR-704): slotId → Id wariantu (od 1.0.1; wcześniej indeks, migrowane przy wczytaniu).</summary>
    public Dictionary<string, string> OstatnieWarianty { get; set; } = new();
    /// <summary>Klucze elementów odhaczonych na liście pakowania (FR-707): „slotId|indeksWariantu|typ|elementId”.</summary>
    public List<string> SpakowaneKlucze { get; set; } = new();
    /// <summary>Id ostatnio użytego wariantu slotu (FR-704) lub null.</summary>
    public string? WariantDomyslnyNaSlot(string slotId) => OstatnieWarianty.TryGetValue(slotId, out var w) ? w : null;
    public bool ZnacznikZImportu { get; set; }
    public DateTime OstatniaZmiana { get; set; } = DateTime.Now;
}

/// <summary>Slot zestawu z wariantami (FR-702, FR-703).</summary>
public class SlotZestawu
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Nazwa { get; set; } = "";
    public int Kolejnosc { get; set; }
    public List<WariantSlotu> Warianty { get; set; } = new();
    /// <summary>Id wariantu domyślnego (FR-704; od 1.0.1 — wcześniej indeks, migrowany przy wczytaniu).</summary>
    public string? WariantDomyslnyId { get; set; }
}

/// <summary>Wariant slotu: lista elementów połączonych „+” albo jawne „puste” (FR-703).</summary>
public class WariantSlotu
{
    /// <summary>Id wariantu (FR-703). Wyprawy przechowują wybór po Id wariantu, nie po indeksie — edycja zestawu nie przepisuje historii.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public List<ElementWariantu> Elementy { get; set; } = new();
    /// <summary>Prawda dla jawnego „puste” (odpowiednik PUSTE w arkuszu) — celowy brak elementu.</summary>
    public bool CzyPuste { get; set; }
}

/// <summary>Element wariantu: przedmiot, replika albo komplet.</summary>
public class ElementWariantu
{
    public TypElementu Typ { get; set; } = TypElementu.Przedmiot;
    public string ElementId { get; set; } = "";
    /// <summary>Nazwa spisana przy imporcie, gdy elementu nie ma jeszcze w katalogu (FR-706).</summary>
    public string? NazwaSzkicu { get; set; }
}

/// <summary>Wyprawa (rozdział 6.5, FR-801).</summary>
public class Wyprawa
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateOnly Data { get; set; }
    public string? RekonstrukcjaId { get; set; }
    public string? Miejsce { get; set; }
    public string? ReplikaGlownaId { get; set; }
    public string? ZestawId { get; set; }
    /// <summary>Wybrane warianty slotów: slotId → Id wariantu (FR-703; od 1.0.1 — wcześniej indeks, migrowany przy wczytaniu).</summary>
    public Dictionary<string, string> WybraneWarianty { get; set; } = new();
    /// <summary>Elementy użyte ręcznie, gdy wyprawa jest bez zestawu (FR-804).</summary>
    public List<ElementWariantu> SkladReczny { get; set; } = new();
    public string? Komentarz { get; set; }
    public List<string> Zdjecia { get; set; } = new();
    public bool CzyUsunieta { get; set; }
    public DateTime OstatniaZmiana { get; set; } = DateTime.Now;
}

/// <summary>Rekonstrukcja — profil formacji (rozdział 6.6, FR-1301).</summary>
public class Rekonstrukcja
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Nazwa { get; set; } = "";
    public string? Okres { get; set; }
    public string? StronaId { get; set; }
    public string? Opis { get; set; }
    public string? Zdjecie { get; set; }
    public List<SlotSzablonu> SzablonSlotow { get; set; } = new();
    public bool ZnacznikZImportu { get; set; }
}

/// <summary>Slot w szablonie rekonstrukcji (FR-702).</summary>
public class SlotSzablonu
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Nazwa { get; set; } = "";
    public int Kolejnosc { get; set; }
    /// <summary>Prawda, gdy slot należy do munduru (BR-09; domyślnie slot „Mundur”).</summary>
    public bool CzySlotMunduru { get; set; }
    /// <summary>Prawda, gdy slot jest slotem broni głównej (FR-802; domyślnie sloty broni szturmowej, DMR i wsparcia).</summary>
    public bool CzySlotBroniGlownej { get; set; }
}

/// <summary>Parametr szablonu karty technicznej (FR-401, FR-402).</summary>
public class ParametrKarty
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Nazwa { get; set; } = "";
    public int Kolejnosc { get; set; }
    public bool Ukryty { get; set; }
}

/// <summary>Szablon karty technicznej powiązany z napędem (FR-403, FR-1307).</summary>
public class SzablonKarty
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Nazwa { get; set; } = "";
    /// <summary>Id wartości słownika napędów; puste oznacza szablon wspólny dla wszystkich napędów.</summary>
    public string? NapedId { get; set; }
    public List<ParametrKarty> Parametry { get; set; } = new();
}

/// <summary>Ustawienia aplikacji (FR-1006, NFR-12).</summary>
public class Ustawienia
{
    public Motyw Motyw { get; set; } = Motyw.Systemowy;
    public string? FolderDanych { get; set; }
    /// <summary>Próg „bez testu” w dniach dla pulpitu i raportów (FR-104).</summary>
    public int DniBezTestu { get; set; } = 180;
    /// <summary>Tolerancja dla znacznika „na maksimum” energii w J (FR-411).</summary>
    public decimal TolerancjaEnergiiJ { get; set; } = 0.05m;
    /// <summary>Data pierwszej wyprawy z importu (BR-15); null przed importem.</summary>
    public DateOnly? DataPierwszejWyprawy { get; set; }
    /// <summary>Dzień zerowy — od tej daty liczą się zapisane wyprawy (FR-807). Ustawiany przy imporcie.</summary>
    public DateOnly? DzienZerowy { get; set; }
    /// <summary>Licznik historyczny wypraw wg roku (BR-07), przeniesiony z arkusza.</summary>
    public Dictionary<int, int> HistoryczneWyprawyWgRok { get; set; } = new();
    /// <summary>Id ostatnio wybranej rekonstrukcji; null oznacza „Wszystkie” (FR-1303).</summary>
    public string? OstatniaRekonstrukcjaId { get; set; }
    public string? OstatniEkran { get; set; }
    public double SzerokoscOkna { get; set; } = 1280;
    public double WysokoscOkna { get; set; } = 800;
    /// <summary>NFR-12: ostatnio użyte filtry wg ekranu (klucz ekranu → tekst filtra wyszukiwania).</summary>
    public Dictionary<string, string> OstatnieFiltry { get; set; } = new();
    /// <summary>NFR-11 [S]: zmniejszaj zdjęcia przy zapisie do rozsądnego rozmiaru (dłuższy bok, px); 0 = bez zmniejszania.</summary>
    public int MaksymalnyBokZdjecia { get; set; } = 2048;
    /// <summary>NFR-11 [S]: zachowaj oryginał obok zmniejszonego (w podkatalogu oryginaly/).</summary>
    public bool ZachowajOryginalZdjecia { get; set; }
}
