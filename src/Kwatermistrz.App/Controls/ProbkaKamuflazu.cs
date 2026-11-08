using System.Windows;
using System.Windows.Media;
using Kwatermistrz.Core.Model;

namespace Kwatermistrz.App.Controls;

/// <summary>
/// Lekka próbka wzoru kamuflażu 1–4 kolorów. Rysuje bez szablonu ControlTemplate:
/// DataGrid wielokrotnie zmienia DataContext wirtualizowanych wierszy, a już użyty
/// FrameworkTemplate jest zapieczętowany i nie wolno modyfikować jego VisualTree.
/// </summary>
public sealed class ProbkaKamuflazu : FrameworkElement
{
    private static readonly Brush KolorDomyslny = Brushes.LightGray;
    private Brush[] _kolory = [KolorDomyslny];

    public static readonly DependencyProperty WzorProperty = DependencyProperty.Register(
        nameof(Wzor), typeof(WzorKamuflazu), typeof(ProbkaKamuflazu),
        new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.AffectsRender, OnWzorChanged));

    /// <summary>Wzór do pokazania; null oznacza neutralną próbkę.</summary>
    public WzorKamuflazu? Wzor
    {
        get => (WzorKamuflazu?)GetValue(WzorProperty);
        set => SetValue(WzorProperty, value);
    }

    public ProbkaKamuflazu()
    {
        Width = 24;
        Height = 16;
        SnapsToDevicePixels = true;
    }

    private static void OnWzorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var kontrolka = (ProbkaKamuflazu)d;
        var kolory = (e.NewValue as WzorKamuflazu)?.KoloryProbki;
        kontrolka._kolory = kolory == null || kolory.Count == 0
            ? [KolorDomyslny]
            : kolory.Take(4).Select(UtworzPedzel).ToArray();
        // AffectsRender automatycznie invaliduje wizualizację, bez zmiany szablonu.
    }

    private static Brush UtworzPedzel(string? wartosc)
    {
        if (string.IsNullOrWhiteSpace(wartosc))
            return KolorDomyslny;

        try
        {
            var kolor = (Color)ColorConverter.ConvertFromString(wartosc);
            var pedzel = new SolidColorBrush(kolor);
            pedzel.Freeze();
            return pedzel;
        }
        catch (FormatException)
        {
            return KolorDomyslny;
        }
        catch (NotSupportedException)
        {
            return KolorDomyslny;
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var w = RenderSize.Width;
        var h = RenderSize.Height;
        if (w <= 0 || h <= 0)
            return;

        var polowaW = w / 2;
        var polowaH = h / 2;
        switch (_kolory.Length)
        {
            case 1:
                drawingContext.DrawRectangle(_kolory[0], null, new Rect(0, 0, w, h));
                break;
            case 2:
                drawingContext.DrawRectangle(_kolory[0], null, new Rect(0, 0, polowaW, h));
                drawingContext.DrawRectangle(_kolory[1], null, new Rect(polowaW, 0, w - polowaW, h));
                break;
            case 3:
                drawingContext.DrawRectangle(_kolory[0], null, new Rect(0, 0, polowaW, polowaH));
                drawingContext.DrawRectangle(_kolory[1], null, new Rect(polowaW, 0, w - polowaW, polowaH));
                drawingContext.DrawRectangle(_kolory[2], null, new Rect(0, polowaH, w, h - polowaH));
                break;
            default:
                drawingContext.DrawRectangle(_kolory[0], null, new Rect(0, 0, polowaW, polowaH));
                drawingContext.DrawRectangle(_kolory[1], null, new Rect(polowaW, 0, w - polowaW, polowaH));
                drawingContext.DrawRectangle(_kolory[2], null, new Rect(0, polowaH, polowaW, h - polowaH));
                drawingContext.DrawRectangle(_kolory[3], null, new Rect(polowaW, polowaH, w - polowaW, h - polowaH));
                break;
        }
    }
}
