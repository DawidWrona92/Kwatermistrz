using System.Globalization;

namespace Kwatermistrz.Core.Obliczenia;

/// <summary>Formaty wyświetlania zgodne z BR-14: waluta PLN „3 297,12 zł”, procent „21,3%”, data „01.09.2022”. Zaokrąglanie tylko tutaj, nigdy w obliczeniach (BR-13).</summary>
public static class Formatowanie
{
    // separator tysięcy to zwykła spacja, zgodnie z BR-14 („3 297,12 zł”)
    private static readonly CultureInfo kultura = new("pl-PL")
    {
        NumberFormat = { NumberGroupSeparator = " ", NumberDecimalSeparator = "," },
    };

    /// <summary>Kwota w PLN, np. „3 297,12 zł”. Ujemne ze znakiem minus przed kwotą.</summary>
    public static string Waluta(decimal kwota)
        => kwota.ToString("#,##0.00 zł", kultura);

    /// <summary>Procent z jednym miejscem po przecinku, np. „21,3%”. Wartość wejściowa jest już w procentach (0–100).</summary>
    public static string Procent(decimal procent)
        => procent.ToString("0.0\\%", kultura);

    /// <summary>Data w formacie „01.09.2022”.</summary>
    public static string Data(DateOnly data)
        => data.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    /// <summary>Data i godzina „01.09.2022 14:32” (FR-1205).</summary>
    public static string DataGodzina(DateTime data)
        => data.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);

    /// <summary>Liczba całkowita z separatorem tysięcy.</summary>
    public static string Liczba(int liczba)
        => liczba.ToString("#,##0", kultura);

    /// <summary>Energia w J, np. „1,90 J”.</summary>
    public static string Energia(decimal? joule)
        => joule == null ? "" : joule.Value.ToString("0.00 J", kultura);
}
