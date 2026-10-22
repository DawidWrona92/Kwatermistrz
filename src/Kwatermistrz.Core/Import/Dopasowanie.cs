using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Import;

/// <summary>Wynik dopasowania nazwy z arkusza do katalogu.</summary>
public class WynikDopasowania<T>
{
    /// <summary>Dopasowany element (null, gdy brak).</summary>
    public T? Element { get; set; }
    /// <summary>Wszyscy kandydaci, gdy dopasowanie niejednoznaczne.</summary>
    public List<T> Kandydaci { get; set; } = new();
    /// <summary>Prawda, gdy znaleziono dokładnie jeden pasujący element.</summary>
    public bool Jednoznaczne => Element != null && Kandydaci.Count <= 1;
    /// <summary>Prawda, gdy znaleziono kilku kandydatów (wymaga decyzji).</summary>
    public bool Niejednoznaczne => Kandydaci.Count > 1;
}

/// <summary>Pomocnik dopasowywania nazw z arkuszy do rekordów katalogu (FR-1103, rozdział 13). Porównanie bez wielkości liter i diakrytyków.</summary>
public static class Dopasowanie
{
    /// <summary>Dzieli tekst źródłowy na słowa kluczowe (bez diakrytyków, małe litery).</summary>
    public static List<string> SlowaKluczowe(string tekst)
    {
        var t = Normalizacja.TekstDoPorownania(tekst);
        return t.Split(new[] { ' ', '/', '+', ',', ';', '(', ')', '"', '„', '”' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(s => s.Length > 0)
            .ToList();
    }

    /// <summary>Dopasowuje nazwę do listy kandydatów po tekście „kluczowym” (np. „firma nazwa”). Zwraca wszystkich kandydatów zawierających wszystkie słowa kluczowe.</summary>
    public static WynikDopasowania<T> Dopasuj<T>(string tekstZrodlowy, IEnumerable<T> kandydaci, Func<T, string> tekstKandydata)
    {
        var wynik = new WynikDopasowania<T>();
        var slowa = SlowaKluczowe(tekstZrodlowy);
        if (slowa.Count == 0)
            return wynik;
        foreach (var kandydat in kandydaci)
        {
            var tekst = Normalizacja.TekstDoPorownania(tekstKandydata(kandydat));
            if (slowa.All(s => tekst.Contains(s, StringComparison.Ordinal)))
                wynik.Kandydaci.Add(kandydat);
        }
        if (wynik.Kandydaci.Count == 1)
            wynik.Element = wynik.Kandydaci[0];
        return wynik;
    }

    /// <summary>Dopasowanie dokładne po nazwie (po normalizacji), potem częściowe.</summary>
    public static T? DokladneLubCzesciowe<T>(string tekstZrodlowy, IEnumerable<T> kandydaci, Func<T, string> tekstKandydata)
    {
        var szukane = Normalizacja.TekstDoPorownania(tekstZrodlowy);
        var dokladne = kandydaci.Where(k => Normalizacja.TekstDoPorownania(tekstKandydata(k)) == szukane).ToList();
        if (dokladne.Count > 0)
            return dokladne[0];
        return Dopasuj(tekstZrodlowy, kandydaci, tekstKandydata).Element;
    }
}
