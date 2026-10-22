using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Obliczenia;

namespace Kwatermistrz.App.Controls;

/// <summary>Mozaika wypraw na pulpicie (FR-102): kafelek w kolorze wzoru munduru, wiersze po latach; kafelki historyczne neutralne z „bez daty”.</summary>
public class MozaikaWypraw : ContentControl
{
    public static readonly DependencyProperty DaneProperty = DependencyProperty.Register(
        nameof(Dane), typeof(DaneAplikacji), typeof(MozaikaWypraw),
        new PropertyMetadata(null, OnDaneChanged));

    public static readonly DependencyProperty KafelkiProperty = DependencyProperty.Register(
        nameof(Kafelki), typeof(IReadOnlyList<KafelekWyprawy>), typeof(MozaikaWypraw), new PropertyMetadata(null));

    /// <summary>Dane aplikacji (do kolorów mundurów i wzorów).</summary>
    public DaneAplikacji? Dane
    {
        get => (DaneAplikacji?)GetValue(DaneProperty);
        set => SetValue(DaneProperty, value);
    }

    /// <summary>Kafelki do narysowania (wiersze po latach).</summary>
    public IReadOnlyList<KafelekWyprawy>? Kafelki
    {
        get => (IReadOnlyList<KafelekWyprawy>?)GetValue(KafelkiProperty);
        set => SetValue(KafelkiProperty, value);
    }

    /// <summary>Zdarzenie kliknięcia kafelka wyprawy.</summary>
    public event EventHandler<KafelekWyprawy>? Kliknieto;

    static MozaikaWypraw()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MozaikaWypraw), new FrameworkPropertyMetadata(typeof(MozaikaWypraw)));
    }

    private static void OnDaneChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MozaikaWypraw kontrolka)
            kontrolka.Odswiez();
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == KafelkiProperty)
            Odswiez();
    }

    private void Odswiez()
    {
        if (Kafelki == null)
        {
            Template = null;
            return;
        }
        var panel = new StackPanel { Orientation = Orientation.Vertical };
        var grupy = Kafelki.GroupBy(k => k.Rok).OrderBy(g => g.Key);
        foreach (var grupa in grupy)
        {
            var wiersz = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 1) };
            var etykieta = new TextBlock
            {
                Text = grupa.Key.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Width = 48,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.Gray,
            };
            wiersz.Children.Add(etykieta);
            foreach (var kafelek in grupa)
            {
                var kolor = KolorKafelka(kafelek);
                var kafel = new Border
                {
                    Width = 18,
                    Height = 18,
                    Margin = new Thickness(1),
                    Background = kolor,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0xCD, 0xD0, 0xCA)),
                    BorderThickness = new Thickness(1),
                    ToolTip = kafelek.Etykieta,
                    Cursor = Cursors.Hand,
                    Tag = kafelek,
                };
                kafel.MouseLeftButtonUp += (_, _) => Kliknieto?.Invoke(this, kafelek);
                wiersz.Children.Add(kafel);
            }
            panel.Children.Add(wiersz);
        }
        Content = panel;
    }

    private SolidColorBrush KolorKafelka(KafelekWyprawy kafelek)
    {
        if (kafelek.CzyHistoryczny || kafelek.MundurId == null || Dane == null)
            return new SolidColorBrush(Color.FromRgb(0xEC, 0xED, 0xEA));
        var mundur = Dane.Przedmioty.FirstOrDefault(p => p.Id == kafelek.MundurId);
        var wzor = mundur?.WzorId != null ? Dane.Slowniki.Wzory.FirstOrDefault(w => w.Id == mundur.WzorId) : null;
        if (wzor == null || wzor.KoloryProbki.Count == 0)
            return new SolidColorBrush(Color.FromRgb(0xEC, 0xED, 0xEA));
        // pierwszy kolor próbki jako kolor kafelka
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(wzor.KoloryProbki[0]));
    }
}

/// <summary>Pojedynczy kafelek mozaiki: wyprawa albo wpis historyczny.</summary>
public class KafelekWyprawy
{
    public int Rok { get; set; }
    public bool CzyHistoryczny { get; set; }
    public string? WyprawaId { get; set; }
    public string? MundurId { get; set; }
    /// <summary>Tekst podpowiedzi: data, replika, zestaw.</summary>
    public string Etykieta { get; set; } = "";
}
