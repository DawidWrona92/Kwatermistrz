using System.Windows;
using System.Windows.Controls;
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
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ProbkaKamuflazu), new FrameworkPropertyMetadata(typeof(ProbkaKamuflazu)));
    }

    private static void OnWzorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ProbkaKamuflazu kontrolka)
            kontrolka.Rysuj();
    }

    private void Rysuj()
    {
        Template = new ControlTemplate(typeof(ProbkaKamuflazu));
        var grid = new FrameworkElementFactory(typeof(Grid));
        var kolory = Wzor?.KoloryProbki.Count > 0 ? Wzor.KoloryProbki : new List<string> { "#CDD0CA" };
        var liczba = Math.Min(kolory.Count, 4);
        for (var i = 0; i < liczba; i++)
        {
            var prostokat = new FrameworkElementFactory(typeof(Border));
            prostokat.SetValue(Grid.RowProperty, i / 2);
            prostokat.SetValue(Grid.ColumnProperty, i % 2);
            prostokat.SetValue(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString(kolory[i])));
            grid.AppendChild(prostokat);
        }
        Template.VisualTree = grid;
        ApplyTemplate();
    }
}
