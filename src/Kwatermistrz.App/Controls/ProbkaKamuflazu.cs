using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Kwatermistrz.Core.Model;

namespace Kwatermistrz.App.Controls;

/// <summary>Próbka wzoru kamuflażu — siatka 2x2 kolorów (jedyny kolorowy element interfejsu, §10.2).</summary>
public class ProbkaKamuflazu : Control
{
    public static readonly DependencyProperty WzorProperty = DependencyProperty.Register(
        nameof(Wzor), typeof(WzorKamuflazu), typeof(ProbkaKamuflazu),
        new PropertyMetadata(null, OnWzorChanged));

    /// <summary>Wzór kamuflażu do pokazania.</summary>
    public WzorKamuflazu? Wzor
    {
        get => (WzorKamuflazu?)GetValue(WzorProperty);
        set => SetValue(WzorProperty, value);
    }

    static ProbkaKamuflazu()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ProbkaKamuflazu),
            new FrameworkPropertyMetadata(typeof(ProbkaKamuflazu)));
    }

    private static void OnWzorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ProbkaKamuflazu kontrolka)
            kontrolka.Rysuj();
    }

    private void Rysuj()
    {
        // WAŻNE: najpierw budujemy CAŁY ControlTemplate, a dopiero potem przypisujemy
        // go do kontrolki. Po pierwszym ApplyTemplate WPF pieczętuje szablon —
        // ustawianie Template.VisualTree na już używanym szablonie powodowało
        // InvalidOperationException i pętlę wyjątków podczas startu aplikacji.
        var siatka = new FrameworkElementFactory(typeof(UniformGrid));
        siatka.SetValue(UniformGrid.RowsProperty, 2);
        siatka.SetValue(UniformGrid.ColumnsProperty, 2);

        var kolory = Wzor?.KoloryProbki?
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Take(4)
            .ToArray() ?? Array.Empty<string>();

        for (var i = 0; i < 4; i++)
        {
            var prostokat = new FrameworkElementFactory(typeof(Border));
            var kolor = kolory.Length > 0 ? kolory[i % kolory.Length] : "#CDD0CA";
            prostokat.SetValue(Border.BackgroundProperty, Pedzel(kolor));
            siatka.AppendChild(prostokat);
        }

        var szablon = new ControlTemplate(typeof(ProbkaKamuflazu))
        {
            VisualTree = siatka,
        };
        Template = szablon;
        ApplyTemplate();
    }

    private static Brush Pedzel(string kolor)
    {
        try
        {
            if (ColorConverter.ConvertFromString(kolor) is Color odczytany)
            {
                var pedzel = new SolidColorBrush(odczytany);
                pedzel.Freeze();
                return pedzel;
            }
        }
        catch (FormatException)
        {
            // Uszkodzony kolor z importu nie może zamknąć całej aplikacji.
        }
        catch (NotSupportedException)
        {
            // Nierozpoznany zapis koloru: bezpieczny zamiennik.
        }

        return Brushes.LightGray;
    }
}
