using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Obliczenia;

namespace Kwatermistrz.App.Converters;

/// <summary>Waluta „3 297,12 zł” (BR-14).</summary>
public class KonwerterWaluty : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is decimal d ? Formatowanie.Waluta(d) : "";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Procent „21,3%” (BR-14).</summary>
public class KonwerterProcentu : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is decimal d ? Formatowanie.Procent(d) : "";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Data „01.09.2022” (BR-14); puste dla null.</summary>
public class KonwerterDaty : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is DateOnly d ? Formatowanie.Data(d) : "";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Nazwa wartości słownika po Id; puste dla null.</summary>
public class KonwerterNazwySlownika : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string id || parameter is not List<WartoscSlownika> slownik)
            return "";
        return slownik.FirstOrDefault(w => w.Id == id)?.Nazwa ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Wiele Id → nazwy połączone przecinkiem.</summary>
public class KonwerterNazwSlownikaWiele : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not List<string> ids || parameter is not List<WartoscSlownika> slownik)
            return "";
        return string.Join(", ", ids.Select(id => slownik.FirstOrDefault(w => w.Id == id)?.Nazwa).Where(n => n != null));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Prawda, gdy wartość jest równa parametrowi (do zaznaczania w selektorach).</summary>
public class KonwerterRowne : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => Equals(value?.ToString(), parameter?.ToString());

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Odwrotność — prawda, gdy wartość różni się od parametra.</summary>
public class KonwerterNieRowne : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => !Equals(value?.ToString(), parameter?.ToString());

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Konwersja enum na indeks (dla selektorów).</summary>
public class KonwerterEnumIndeks : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Enum e ? System.Convert.ToInt32(e, CultureInfo.InvariantCulture) : 0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int i && parameter is Type t && t.IsEnum ? Enum.ToObject(t, i) : Binding.DoNothing;
}

/// <summary>Prawda, gdy wartość nie jest null (do widoczności formularza).</summary>
public class KonwerterNieRowneNull : IValueConverter
{
    public static readonly KonwerterNieRowneNull Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value != null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Prawda, gdy liczba kolekcji jest większa od zera (do widoczności selektora kandydatów).</summary>
public class KonwerterWiekszeOdZera : IValueConverter
{
    public static readonly KonwerterWiekszeOdZera Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int liczba && liczba > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Licznik przedmiotów w kategorii: parametr to Id kategorii, kontekst (DataContext strony) to ViewModel z metodą LiczbaPrzedmiotow.</summary>
public class KonwerterLicznikaKategorii : IMultiValueConverter
{
    public static readonly KonwerterLicznikaKategorii Instance = new();

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 1 && values[0] is ViewModels.WyposazenieViewModel vm && parameter is string kategoriaId)
            return vm.LiczbaPrzedmiotow(kategoriaId).ToString(CultureInfo.InvariantCulture);
        return "";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Ścieżka względna zdjęcia → miniatura (BitmapImage z katalogu danych, DecodePixelWidth=80). Zwraca null, gdy brak ścieżki lub pliku (FR-211 [S]).</summary>
public class KonwerterMiniatury : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string sciezka || string.IsNullOrWhiteSpace(sciezka))
            return null;
        var pelna = Services.SerwisZdjec.PelnaSciezkaBezwzgledna(sciezka);
        return pelna != null ? Services.SerwisZdjec.WczytajObraz(pelna, 80) : null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Liczba równa parametrowi → Visibility.Visible, inaczej Collapsed.</summary>
public class KonwerterRowneLiczbe : IValueConverter
{
    public static readonly KonwerterRowneLiczbe Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var rowne = value != null && parameter != null
            && System.Convert.ToInt64(value, CultureInfo.InvariantCulture) == System.Convert.ToInt64(parameter, CultureInfo.InvariantCulture);
        return rowne ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Bool → Visibility (do oznaczeń awarii itp.).</summary>
public class KonwerterBoolToVisibility : IValueConverter
{
    public static readonly KonwerterBoolToVisibility Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility v && v == Visibility.Visible;
}
