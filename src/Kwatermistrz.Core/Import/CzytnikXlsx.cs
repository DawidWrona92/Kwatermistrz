using System.Globalization;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Kwatermistrz.Core.Import;

/// <summary>Wiersz arkusza: lista komórek jako tekst (puste komórki to null). Daty i liczby już przekonwertowane na tekst kanoniczny.</summary>
public class WierszXlsx
{
    public List<string?> Komorki { get; set; } = new();
    /// <summary>Numer wiersza w arkuszu (od 1).</summary>
    public int Numer { get; set; }

    public string? Komorka(int indeks) => indeks >= 0 && indeks < Komorki.Count ? Komorki[indeks] : null;
}

/// <summary>Arkusz: wiersze odczytane z pliku.</summary>
public class ArkuszXlsx
{
    public string Nazwa { get; set; } = "";
    public List<WierszXlsx> Wiersze { get; set; } = new();
}

/// <summary>Czytnik plików .xlsx (NPOI). Odczytuje wartości komórek, w tym wyniki formuł z pamięci podręcznej (decyzja 05).</summary>
public static class CzytnikXlsx
{
    /// <summary>Wczytuje wszystkie arkusze z pliku .xlsx.</summary>
    public static List<ArkuszXlsx> Wczytaj(string sciezka)
    {
        using var strumien = File.OpenRead(sciezka);
        return Wczytaj(strumien);
    }

    /// <summary>Wczytuje wszystkie arkusze ze strumienia.</summary>
    public static List<ArkuszXlsx> Wczytaj(Stream strumien)
    {
        var skoroszyt = new XSSFWorkbook(strumien);
        var arkusze = new List<ArkuszXlsx>();
        for (var i = 0; i < skoroszyt.NumberOfSheets; i++)
        {
            var arkusz = skoroszyt.GetSheetAt(i);
            arkusze.Add(new ArkuszXlsx { Nazwa = arkusz.SheetName, Wiersze = CzytajWiersze(arkusz) });
        }
        return arkusze;
    }

    private static List<WierszXlsx> CzytajWiersze(ISheet arkusz)
    {
        var wynik = new List<WierszXlsx>();
        for (var r = 0; r <= arkusz.LastRowNum; r++)
        {
            var wiersz = arkusz.GetRow(r);
            if (wiersz == null)
            {
                wynik.Add(new WierszXlsx { Numer = r + 1 });
                continue;
            }
            var komorki = new List<string?>();
            for (var c = 0; c <= wiersz.LastCellNum; c++)
                komorki.Add(KomorkaDoTekstu(wiersz.GetCell(c)));
            wynik.Add(new WierszXlsx { Numer = r + 1, Komorki = komorki });
        }
        return wynik;
    }

    /// <summary>Konwertuje komórkę na tekst; dla formuł bierze wartość z pamięci podręcznej; daty w formacie ISO.</summary>
    public static string? KomorkaDoTekstu(ICell? komorka)
    {
        if (komorka == null)
            return null;
        var typ = komorka.CellType == CellType.Formula ? komorka.CachedFormulaResultType : komorka.CellType;
        switch (typ)
        {
            case CellType.Blank:
                return null;
            case CellType.Numeric:
                if (DateUtil.IsCellDateFormatted(komorka))
                    return DateUtil.GetJavaDate(komorka.NumericCellValue).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                return komorka.NumericCellValue.ToString(CultureInfo.InvariantCulture);
            case CellType.String:
                return komorka.StringCellValue;
            case CellType.Boolean:
                return komorka.BooleanCellValue ? "TAK" : "NIE";
            default:
                return komorka.ToString();
        }
    }

    /// <summary>Parsuje liczbę z komórki (double z Excela) na decimal bez utraty groszy.</summary>
    public static decimal? Liczba(string? tekst)
    {
        if (string.IsNullOrWhiteSpace(tekst))
            return null;
        var t = tekst.Trim().Replace(',', '.');
        // Excel trzyma wartości z arkusza; dopuszczamy też polski zapis z przecinkiem
        if (decimal.TryParse(t, NumberStyles.Number, CultureInfo.InvariantCulture, out var wynik))
            return wynik;
        if (decimal.TryParse(tekst.Trim(), new CultureInfo("pl-PL"), out wynik))
            return wynik;
        return null;
    }

    /// <summary>Parsuje datę w formatach ISO (yyyy-MM-dd) lub polskim (dd.MM.yyyy).</summary>
    public static DateOnly? Data(string? tekst)
    {
        if (string.IsNullOrWhiteSpace(tekst))
            return null;
        var t = tekst.Trim();
        if (DateOnly.TryParseExact(t, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1))
            return d1;
        if (DateOnly.TryParseExact(t, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d2))
            return d2;
        if (DateOnly.TryParse(t, out var d3))
            return d3;
        return null;
    }

    /// <summary>Parsuje liczbę całkowitą.</summary>
    public static int? LiczbaCalkowita(string? tekst)
    {
        var liczba = Liczba(tekst);
        return liczba == null ? null : (int)Math.Round(liczba.Value);
    }
}
