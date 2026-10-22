namespace Kwatermistrz.Core.Model;

/// <summary>Pojedyncza wartość słownika edytowalnego (FR-1001). Aliasy służą do rozpoznawania wartości przy imporcie (FR-1003).</summary>
public class WartoscSlownika
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Nazwa { get; set; } = "";
    public List<string> Aliasy { get; set; } = new();
    /// <summary>Dla słowników rodzajów kosztów i zadań: stała semantyka używana w regułach BR (NFR-14). Pusta dla pozostałych słowników.</summary>
    public string Semantyka { get; set; } = "";
}

/// <summary>Wzór kamuflażu lub kolor jednolity z próbnikiem barw (FR-1004, §10.5).</summary>
public class WzorKamuflazu
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Nazwa { get; set; } = "";
    public List<string> Aliasy { get; set; } = new();
    /// <summary>Kolory próbnika jako wartości hex (#RRGGBB), 1–4 barwy.</summary>
    public List<string> KoloryProbki { get; set; } = new();
    /// <summary>Prawda, gdy to kolor jednolity (np. Olive), nie wzór kamuflażu.</summary>
    public bool CzyKolorJednolity { get; set; }
}

/// <summary>Kategoria przedmiotu (FR-202). Kategorie oznaczone jako nośnik mogą przyjmować zamontowane elementy (FR-207).</summary>
public class Kategoria
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Nazwa { get; set; } = "";
    public bool CzyNosnik { get; set; }
    /// <summary>Prawda, gdy kategoria to mundur (BR-09; tabela umundurowania 13.2). Historyczne liczniki
    /// mundurów w kontroli spójności liczymy tylko z takich kategorii — nazwa kategorii nie jest definicją.</summary>
    public bool CzyMundur { get; set; }
    public int Kolejnosc { get; set; }
}

/// <summary>Wszystkie słowniki edytowalne użytkownika (FR-1001). Wartości startowe w <see cref="StartoweSlowniki"/>. </summary>
public class Slowniki
{
    public List<WartoscSlownika> Strony { get; set; } = new();
    public List<WzorKamuflazu> Wzory { get; set; } = new();
    public List<WartoscSlownika> Miejsca { get; set; } = new();
    public List<WartoscSlownika> Role { get; set; } = new();
    public List<WartoscSlownika> TypyMagazynkow { get; set; } = new();
    public List<WartoscSlownika> Napedy { get; set; } = new();
    public List<WartoscSlownika> StanyReplik { get; set; } = new();
    public List<WartoscSlownika> Pochodzenie { get; set; } = new();
    public List<WartoscSlownika> RodzajeKosztow { get; set; } = new();
    public List<WartoscSlownika> RodzajeZadan { get; set; } = new();
    public List<Kategoria> Kategorie { get; set; } = new();

    /// <summary>Znajduje wartość słownika po nazwie lub aliasie, bez rozróżniania wielkości liter i bez skrajnych spacji.</summary>
    public static WartoscSlownika? Znajdz(List<WartoscSlownika> slownik, string? tekst)
    {
        if (string.IsNullOrWhiteSpace(tekst))
            return null;
        var szukane = Normalizacja.TekstDoPorownania(tekst);
        return slownik.FirstOrDefault(w =>
            Normalizacja.TekstDoPorownania(w.Nazwa) == szukane ||
            w.Aliasy.Any(a => Normalizacja.TekstDoPorownania(a) == szukane));
    }

    /// <summary>Znajduje wzór kamuflażu po nazwie lub aliasie.</summary>
    public WzorKamuflazu? ZnajdzWzor(string? tekst)
    {
        if (string.IsNullOrWhiteSpace(tekst))
            return null;
        var szukane = Normalizacja.TekstDoPorownania(tekst);
        return Wzory.FirstOrDefault(w =>
            Normalizacja.TekstDoPorownania(w.Nazwa) == szukane ||
            w.Aliasy.Any(a => Normalizacja.TekstDoPorownania(a) == szukane));
    }
}
