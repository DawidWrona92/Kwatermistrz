using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Kwatermistrz.App.Controls;

/// <summary>Pomocnik rysowania tekstu dla wykresów (FR-909).</summary>
internal static class WykresRysowanie
{
    public static FormattedText Tekst(Control kontrolka, string tekst, double rozmiar, Brush pedzel, FontWeight? waga = null)
    {
        var typeface = new Typeface(kontrolka.FontFamily, kontrolka.FontStyle, waga ?? kontrolka.FontWeight, FontStretches.Normal);
#pragma warning disable CS0618 // PixelsPerDip — konstruktor bez niego jest wystarczający dla rysowania w OnRender
        return new FormattedText(tekst, System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, typeface, rozmiar, pedzel);
#pragma warning restore CS0618
    }
}

/// <summary>Wiersz wykresu: etykieta, wartość i opcjonalny udział (0–1) oraz kolor słupka.</summary>
public class WierszWykresu
{
    public string Etykieta { get; set; } = "";
    public decimal Wartosc { get; set; }
    /// <summary>Udział 0–1 (dla paska 100%); gdy null, nie pokazuj procentu.</summary>
    public decimal? Udzial { get; set; }
    public string? KolorHex { get; set; }
}

/// <summary>Wykres słupkowy pionowy (FR-909): słupki wg kategorii (np. wyprawy wg lat), z etykietami pod słupkami i wartościami na górze.</summary>
public class WykresPionowy : Control
{
    public static readonly DependencyProperty DaneProperty = DependencyProperty.Register(
        nameof(Dane), typeof(IReadOnlyList<WierszWykresu>), typeof(WykresPionowy),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TytulProperty = DependencyProperty.Register(
        nameof(Tytul), typeof(string), typeof(WykresPionowy),
        new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

    public IReadOnlyList<WierszWykresu>? Dane
    {
        get => (IReadOnlyList<WierszWykresu>?)GetValue(DaneProperty);
        set => SetValue(DaneProperty, value);
    }

    public string Tytul
    {
        get => (string)GetValue(TytulProperty);
        set => SetValue(TytulProperty, value);
    }

    static WykresPionowy()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(WykresPionowy), new FrameworkPropertyMetadata(typeof(WykresPionowy)));
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var dane = Dane;
        if (dane == null || dane.Count == 0)
        {
            var puste = WykresRysowanie.Tekst(this, "Brak danych", 12, ForegroundBrush());
            drawingContext.DrawText(puste, new Point(8, 8));
            return;
        }
        var szer = Math.Max(ActualWidth, 200);
        var wys = Math.Max(ActualHeight, 120);
        var marginesLewy = 8.0;
        var marginesGora = string.IsNullOrEmpty(Tytul) ? 8.0 : 24.0;
        var marginesDol = 28.0;
        var max = dane.Max(w => w.Wartosc);
        if (max <= 0)
            max = 1;
        var obszarWykresu = wys - marginesGora - marginesDol;
        var szerokoscSlupka = (szer - marginesLewy - 8) / dane.Count;

        if (!string.IsNullOrEmpty(Tytul))
        {
            var tytul = WykresRysowanie.Tekst(this, Tytul, 13, ForegroundBrush(), FontWeights.SemiBold);
            drawingContext.DrawText(tytul, new Point(marginesLewy, 4));
        }

        for (var i = 0; i < dane.Count; i++)
        {
            var w = dane[i];
            var wysokosc = obszarWykresu * (double)(w.Wartosc / max);
            var x = marginesLewy + i * szerokoscSlupka + szerokoscSlupka * 0.15;
            var szerSlupka = szerokoscSlupka * 0.7;
            var y = marginesGora + obszarWykresu - wysokosc;
            var kolor = Kolor(w.KolorHex) ?? StalBrush();
            drawingContext.DrawRectangle(kolor, null, new Rect(x, y, szerSlupka, Math.Max(wysokosc, 1)));
            // wartość nad słupkiem
            var wartosc = WykresRysowanie.Tekst(this, WartoscTekst(w.Wartosc), 11, ForegroundBrush());
            drawingContext.DrawText(wartosc, new Point(x + szerSlupka / 2 - wartosc.Width / 2, y - wartosc.Height - 2));
            // etykieta pod słupkiem
            var etykieta = WykresRysowanie.Tekst(this, w.Etykieta, 11, ForegroundBrush());
            drawingContext.DrawText(etykieta, new Point(x + szerSlupka / 2 - etykieta.Width / 2, wys - marginesDol + 6));
        }
        drawingContext.DrawLine(new Pen(ForegroundBrush(), 0.5), new Point(marginesLewy, marginesGora + obszarWykresu), new Point(szer - 8, marginesGora + obszarWykresu));
    }

    internal static string WartoscTekst(decimal w) =>
        w == Math.Floor(w) ? ((long)w).ToString(System.Globalization.CultureInfo.InvariantCulture) : w.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    internal static SolidColorBrush? Kolor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return null;
        try
        {
            return (SolidColorBrush)new BrushConverter().ConvertFrom(hex)!;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    internal Brush ForegroundBrush() => Foreground as Brush ?? Brushes.Black;

    internal static SolidColorBrush StalBrush() =>
        Application.Current?.TryFindResource("Stal") as SolidColorBrush
            ?? new SolidColorBrush(Color.FromRgb(0x2F, 0x4A, 0x6B));
}

/// <summary>Wykres poziomych słupków (FR-909): etykieta po lewej, pasek 100% z udziałem, wartość i procent po prawej.</summary>
public class WykresPoziomy : Control
{
    public static readonly DependencyProperty DaneProperty = DependencyProperty.Register(
        nameof(Dane), typeof(IReadOnlyList<WierszWykresu>), typeof(WykresPoziomy),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TytulProperty = DependencyProperty.Register(
        nameof(Tytul), typeof(string), typeof(WykresPoziomy),
        new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

    public IReadOnlyList<WierszWykresu>? Dane
    {
        get => (IReadOnlyList<WierszWykresu>?)GetValue(DaneProperty);
        set => SetValue(DaneProperty, value);
    }

    public string Tytul
    {
        get => (string)GetValue(TytulProperty);
        set => SetValue(TytulProperty, value);
    }

    static WykresPoziomy()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(WykresPoziomy), new FrameworkPropertyMetadata(typeof(WykresPoziomy)));
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var dane = Dane;
        if (dane == null || dane.Count == 0)
        {
            var puste = WykresRysowanie.Tekst(this, "Brak danych", 12, Foreground);
            drawingContext.DrawText(puste, new Point(8, 8));
            return;
        }
        var szer = Math.Max(ActualWidth, 300);
        var wys = Math.Max(ActualHeight, 40 + dane.Count * 26);
        var marginesGora = string.IsNullOrEmpty(Tytul) ? 4.0 : 22.0;
        var szerEtykiety = 140.0;
        var szerWartosci = 110.0;
        var szerPaska = szer - szerEtykiety - szerWartosci - 24;
        if (szerPaska < 50)
            szerPaska = 50;
        var max = dane.Max(w => w.Wartosc);
        if (max <= 0)
            max = 1;

        if (!string.IsNullOrEmpty(Tytul))
        {
            var tytul = WykresRysowanie.Tekst(this, Tytul, 13, Foreground, FontWeights.SemiBold);
            drawingContext.DrawText(tytul, new Point(4, 4));
        }

        var y = marginesGora;
        foreach (var w in dane)
        {
            var etykieta = WykresRysowanie.Tekst(this, w.Etykieta, 12, Foreground);
            drawingContext.DrawText(etykieta, new Point(4, y + 3));
            // tło paska
            drawingContext.DrawRectangle(TloBrush(), null, new Rect(szerEtykiety + 8, y, szerPaska, 18));
            // wypełnienie
            var udzial = w.Udzial ?? (max > 0 ? w.Wartosc / max : 0m);
            var szerWypelnienia = szerPaska * (double)udzial;
            var kolor = WykresPionowy.Kolor(w.KolorHex) ?? WykresPionowy.StalBrush();
            if (szerWypelnienia > 0)
                drawingContext.DrawRectangle(kolor, null, new Rect(szerEtykiety + 8, y, szerWypelnienia, 18));
            // wartość i procent
            var tekst = w.Udzial != null
                ? $"{WykresPionowy.WartoscTekst(w.Wartosc)}  ({w.Udzial.Value * 100m:0}%)"
                : WykresPionowy.WartoscTekst(w.Wartosc);
            var wartosc = WykresRysowanie.Tekst(this, tekst, 12, Foreground);
            drawingContext.DrawText(wartosc, new Point(szerEtykiety + 16 + szerPaska, y + 3));
            y += 26;
        }
    }

    private static Brush TloBrush() =>
        Application.Current?.TryFindResource("Linia") as Brush
            ?? new SolidColorBrush(Color.FromArgb(40, 128, 128, 128));
}
