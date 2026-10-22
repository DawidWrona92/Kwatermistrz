using System.Globalization;
using System.Text;

namespace Kwatermistrz.Core.Model;

/// <summary>Normalizacja tekstu z arkuszy: przycinanie, usuwanie niewidocznych znaków, ujednolicenie spacji (§13.4).</summary>
public static class Normalizacja
{
    /// <summary>Przycina tekst i sprowadza wszystkie spacje (w tym niełamliwe) do pojedynczej zwykłej spacji.</summary>
    public static string? Przytnij(string? tekst)
    {
        if (string.IsNullOrWhiteSpace(tekst))
            return null;
        var sb = new StringBuilder(tekst.Length);
        var bylaSpacja = false;
        foreach (var znak in tekst)
        {
            if (char.IsWhiteSpace(znak))
            {
                bylaSpacja = true;
                continue;
            }
            if (bylaSpacja && sb.Length > 0)
                sb.Append(' ');
            bylaSpacja = false;
            sb.Append(znak);
        }
        return sb.ToString();
    }


    /// <summary>Tekst do porównań: bez wielkości liter, bez polskich znaków diakrytycznych.</summary>
    public static string TekstDoPorownania(string tekst)
    {
        var bezD = UsunDiakrytyki(tekst.Trim().ToLowerInvariant());
        return bezD;
    }

    private static string UsunDiakrytyki(string tekst)
    {
        var znormalizowany = tekst.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(znormalizowany.Length);
        foreach (var znak in znormalizowany)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(znak) != UnicodeCategory.NonSpacingMark)
                sb.Append(znak);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>Parsuje formułę składników kosztu z arkusza, np. „960+112+64” lub „626-100-35” (FR-508, §5.3). Zwraca null, gdy tekst nie jest formułą.</summary>
    public static List<decimal>? ParsujFormuleKosztu(string? tekst)
    {
        if (string.IsNullOrWhiteSpace(tekst))
            return null;
        var t = Przytnij(tekst)!;
        if (t.Length == 0)
            return null;
        var wynik = new List<decimal>();
        var biezacy = new StringBuilder();
        var poprzedniZnak = '+';
        foreach (var znak in t)
        {
            if (znak is '+' or '-')
            {
                if (biezacy.Length > 0)
                {
                    if (!decimal.TryParse(biezacy.ToString().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var wartosc))
                        return null;
                    wynik.Add(poprzedniZnak == '-' ? -wartosc : wartosc);
                    biezacy.Clear();
                }
                poprzedniZnak = znak;
            }
            else if (char.IsDigit(znak) || znak is '.' or ',')
            {
                biezacy.Append(znak);
            }
            else
            {
                return null;
            }
        }
        if (biezacy.Length > 0)
        {
            if (!decimal.TryParse(biezacy.ToString().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var wartosc))
                return null;
            wynik.Add(poprzedniZnak == '-' ? -wartosc : wartosc);
        }
        return wynik.Count > 0 ? wynik : null;
    }

    /// <summary>Rozpoznaje znacznik nieznanej wartości „?”, „??”, „???” (§13.5).</summary>
    public static bool CzyZnacznikNieznane(string? tekst)
    {
        var t = Przytnij(tekst);
        return t is "?" or "??" or "???";
    }

    /// <summary>Rozpoznaje znacznik „Nie dotyczy HPA” i warianty pisowni (§13.5).</summary>
    public static bool CzyZnacznikNieDotyczy(string? tekst)
    {
        var t = Przytnij(tekst);
        if (t == null)
            return false;
        return TekstDoPorownania(t).StartsWith("nie dotyczy", StringComparison.Ordinal);
    }

    /// <summary>Czy wartość oznacza faktyczny brak części (np. „brak” dla gumki hop-up, §13.4) — to wypełniona wartość, nie „nieznane”.</summary>
    public static bool CzyWartoscBrak(string? tekst)
    {
        var t = Przytnij(tekst);
        return t != null && TekstDoPorownania(t) == "brak";
    }
}
