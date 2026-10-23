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

    /// <summary>Dopasowuje nazwę do listy kandydatów po tekście „kluczowym” (np. „firma nazwa”). Zwraca kandydatów spełniających wszystkie słowa kluczowe; gdy żaden nie spełnia wszystkich — kandydaci z tolerancją n-1 słów (np. „SSO Spektr” nie trafi w „SSO Sumrak”, ale „Spektr” już tak, gdy dodamy tekst wzoru).</summary>
    public static WynikDopasowania<T> Dopasuj<T>(string tekstZrodlowy, IEnumerable<T> kandydaci, Func<T, string> tekstKandydata)
    {
        var wynik = new WynikDopasowania<T>();
        var slowa = SlowaKluczowe(tekstZrodlowy);
        if (slowa.Count == 0)
            return wynik;
        var wszystkie = kandydaci
            .Select(kandydat => (Kandydat: kandydat, Tekst: Normalizacja.TekstDoPorownania(tekstKandydata(kandydat))))
            .ToList();
        // słowo kluczowe trafia jako CAŁE słowo (na granicy słowa) — „pas” nie trafi w „pasoszelki”, „m4” nie trafi w „m43”
        foreach (var (kandydat, tekst) in wszystkie)
            if (slowa.All(w => SlowoWTekscie(w, tekst)))
                wynik.Kandydaci.Add(kandydat);
        if (wynik.Kandydaci.Count == 0 && slowa.Count > 1)
        {
            // tolerancja n-1 słów — tylko gdy brak pełnych trafień; brakujące słowo musi być podobne do jakiegoś słowa kandydata („Sumak”→„Sumrak” tak, „Ręczny”→6Sh122 nie)
            foreach (var (kandydat, tekst) in wszystkie)
            {
                var trafione = slowa.Count(w => SlowoWTekscie(w, tekst));
                if (trafione < slowa.Count - 1)
                    continue;
                var brakujace = slowa.Where(w => !SlowoWTekscie(w, tekst)).ToList();
                var slowaKandydata = tekst.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (brakujace.All(b => slowaKandydata.Any(sk => Podobne(b, sk))))
                    wynik.Kandydaci.Add(kandydat);
            }
        }
        if (wynik.Kandydaci.Count == 1)
            wynik.Element = wynik.Kandydaci[0];
        return wynik;
    }

    /// <summary>Prawda, gdy słowo występuje w tekście jako całe słowo (granice: początek/koniec lub znak niebędący literą/cyfrą). „pas” ≠ „pasoszelki”, „m4” ≠ „m43”, „m4” = „EM4 RIS”.</summary>
    public static bool SlowoWTekscie(string slowo, string tekst)
    {
        var start = 0;
        while ((start = tekst.IndexOf(slowo, start, StringComparison.Ordinal)) >= 0)
        {
            var przedOk = start == 0 || !JestLiteraCyfra(tekst[start - 1]);
            var poOk = start + slowo.Length >= tekst.Length || !JestLiteraCyfra(tekst[start + slowo.Length]);
            if (przedOk && poOk)
                return true;
            start++;
        }
        return false;

        static bool JestLiteraCyfra(char znak) => char.IsLetterOrDigit(znak);
    }

    /// <summary>Podobieństwo słów: ta sama długość (±1) i odległość Levenshteina ≤ 2 (dla słów ≥5 znaków) lub ≤1 (krótsze). „Sumak”/„Sumrak” = 2, „Ręczny”/„6Sh122” = daleko.</summary>
    public static bool Podobne(string a, string b)
    {
        if (a == b)
            return true;
        if (Math.Abs(a.Length - b.Length) > 1)
            return false;
        var max = a.Length >= 5 ? 2 : 1;
        return OdlegloscLevenshteina(a, b) <= max;
    }

    private static int OdlegloscLevenshteina(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++)
            d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++)
            d[0, j] = j;
        for (var i = 1; i <= a.Length; i++)
            for (var j = 1; j <= b.Length; j++)
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
        return d[a.Length, b.Length];
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
