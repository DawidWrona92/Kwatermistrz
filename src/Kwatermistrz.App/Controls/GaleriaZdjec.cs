using System.Collections;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Kwatermistrz.App.Services;
using Microsoft.Win32;

namespace Kwatermistrz.App.Controls;

/// <summary>Galeria zdjęć (FR-211): miniatury z „+”, powiększenie po kliknięciu, usuwanie, zdjęcie główne (pierwsze). Wymaga SerwisZdjec jako DataContext lub przez właściwość.</summary>
public class GaleriaZdjec : Control
{
    public static readonly DependencyProperty SciezkiProperty = DependencyProperty.Register(
        nameof(Sciezki), typeof(IList), typeof(GaleriaZdjec),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange));

    public static readonly DependencyProperty SerwisProperty = DependencyProperty.Register(
        nameof(Serwis), typeof(SerwisZdjec), typeof(GaleriaZdjec),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange));

    public static readonly DependencyProperty WlascicielNazwaProperty = DependencyProperty.Register(
        nameof(WlascicielNazwa), typeof(string), typeof(GaleriaZdjec),
        new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsArrange));

    /// <summary>Lista ścieżek względnych (List&lt;string&gt;) — pierwsza to zdjęcie główne.</summary>
    public IList? Sciezki
    {
        get => (IList?)GetValue(SciezkiProperty);
        set => SetValue(SciezkiProperty, value);
    }

    /// <summary>Serwis zdjęć (ścieżki bezwzględne, powiększenie).</summary>
    public SerwisZdjec? Serwis
    {
        get => (SerwisZdjec?)GetValue(SerwisProperty);
        set => SetValue(SerwisProperty, value);
    }

    /// <summary>Nazwa właściciela (do etykiety powiększenia).</summary>
    public string WlascicielNazwa
    {
        get => (string)GetValue(WlascicielNazwaProperty);
        set => SetValue(WlascicielNazwaProperty, value);
    }

    /// <summary>Zdarzenie po zmianie listy (dodanie/usunięcie) — podpinane przez kod.</summary>
    public event EventHandler? Zmieniono;

    static GaleriaZdjec()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GaleriaZdjec), new FrameworkPropertyMetadata(typeof(GaleriaZdjec)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        OdswiezGalerie();
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == SciezkiProperty || e.Property == SerwisProperty)
            OdswiezGalerie();
    }

    private void OdswiezGalerie()
    {
        var kontener = Template?.FindName("PART_Kontener", this) as Panel;
        if (kontener == null)
            return;
        kontener.Children.Clear();

        var sciezki = Sciezki?.Cast<object>().Select(x => x?.ToString()).Where(x => !string.IsNullOrEmpty(x)).ToList()
            ?? new List<string?>();
        var i = 0;
        foreach (var sciezka in sciezki)
        {
            var indeks = i;
            var pelna = Serwis?.PelnaSciezka(sciezka!);
            var miniatura = new Border
            {
                Width = 72,
                Height = 72,
                Margin = new Thickness(0, 0, 6, 6),
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(indeks == 0 ? 2 : 1),
                BorderBrush = indeks == 0
                    ? (Brush?)Application.Current.TryFindResource("Stal") ?? Brushes.SteelBlue
                    : (Brush?)Application.Current.TryFindResource("Linia") ?? Brushes.LightGray,
                Cursor = Cursors.Hand,
                ToolTip = indeks == 0 ? "Zdjęcie główne — kliknij, aby powiększyć" : "Kliknij, aby powiększyć",
            };
            // NFR-08: miniatura dostępna dla czytnika ekranu — nazwa = właściciel + numer + rola
            AutomationProperties.SetName(miniatura,
                $"{WlascicielNazwa}, zdjęcie {indeks + 1}{(indeks == 0 ? " (główne)" : "")}");
            var obraz = pelna != null ? SerwisZdjec.WczytajObraz(pelna, 144) : null;
            miniatura.Child = obraz != null
                ? new Image { Source = obraz, Stretch = Stretch.UniformToFill }
                : new TextBlock { Text = "?", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.Gray };
            var sciezkaLokalna = sciezka;
            var indeksLokalny = indeks;
            miniatura.MouseLeftButtonUp += (_, _) => PokazPowiekszenie(sciezkaLokalna!);
            // menu kontekstowe: powiększ / ustaw jako główne / usuń
            var menu = new ContextMenu();
            var pozycjaPowieksz = new MenuItem { Header = "Powiększ" };
            pozycjaPowieksz.Click += (_, _) => PokazPowiekszenie(sciezkaLokalna!);
            var pozycjaGlowne = new MenuItem { Header = "Ustaw jako główne" };
            pozycjaGlowne.Click += (_, _) => UstawGlowne(indeksLokalny);
            var pozycjaUsun = new MenuItem { Header = "Usuń" };
            pozycjaUsun.Click += (_, _) => Usun(indeksLokalny);
            menu.Items.Add(pozycjaPowieksz);
            menu.Items.Add(pozycjaGlowne);
            menu.Items.Add(pozycjaUsun);
            miniatura.ContextMenu = menu;
            kontener.Children.Add(miniatura);
            i++;
        }

        // przycisk „Dodaj zdjęcie…"
        var dodaj = new Button
        {
            Width = 72,
            Height = 72,
            Margin = new Thickness(0, 0, 6, 6),
            Content = new TextBlock { Text = "+", FontSize = 24, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
            ToolTip = "Dodaj zdjęcie…",
        };
        // NFR-08: przycisk „+” czytelny dla czytnika ekranu (sam znak „+” byłby niejednoznaczny)
        AutomationProperties.SetName(dodaj, $"Dodaj zdjęcie do: {WlascicielNazwa}");
        dodaj.Click += (_, _) => DodajZdjecia();
        kontener.Children.Add(dodaj);
    }

    private void DodajZdjecia()
    {
        if (Sciezki is not List<string> lista)
            return;
        var dialog = new OpenFileDialog
        {
            Title = "Wybierz zdjęcia",
            Filter = "Obrazy (*.jpg;*.jpeg;*.png;*.bmp;*.webp)|*.jpg;*.jpeg;*.png;*.bmp;*.webp|Wszystkie pliki (*.*)|*.*",
            Multiselect = true,
        };
        if (dialog.ShowDialog() != true || Serwis == null)
            return;
        foreach (var plik in dialog.FileNames)
            lista.Add(Serwis.ZapiszDoKatalogu(plik));
        Zmieniono?.Invoke(this, EventArgs.Empty);
        OdswiezGalerie();
    }

    private void Usun(int indeks)
    {
        if (Sciezki is not List<string> lista || indeks < 0 || indeks >= lista.Count)
            return;
        var sciezka = lista[indeks];
        lista.RemoveAt(indeks);
        Serwis?.UsunPlik(sciezka);
        Zmieniono?.Invoke(this, EventArgs.Empty);
        OdswiezGalerie();
    }

    private void UstawGlowne(int indeks)
    {
        if (Sciezki is not List<string> lista || indeks <= 0 || indeks >= lista.Count)
            return;
        var sciezka = lista[indeks];
        lista.RemoveAt(indeks);
        lista.Insert(0, sciezka);
        Zmieniono?.Invoke(this, EventArgs.Empty);
        OdswiezGalerie();
    }

    private void PokazPowiekszenie(string sciezka)
    {
        var pelna = Serwis?.PelnaSciezka(sciezka);
        if (pelna == null)
            return;
        var obraz = SerwisZdjec.WczytajObraz(pelna);
        if (obraz == null)
            return;
        var okno = new Views.PowiekszenieZdjeciaWindow
        {
            Owner = Window.GetWindow(this),
        };
        okno.UstawObraz(obraz, string.IsNullOrEmpty(WlascicielNazwa) ? null : WlascicielNazwa);
        okno.ShowDialog();
    }
}
