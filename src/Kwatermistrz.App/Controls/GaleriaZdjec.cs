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

    private static readonly HashSet<string> DozwoloneRozszerzenia = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp",
    };

    public static readonly DependencyProperty WlascicielNazwaProperty = DependencyProperty.Register(
        nameof(WlascicielNazwa), typeof(string), typeof(GaleriaZdjec),
        new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsArrange));

    public static readonly DependencyProperty MaksymalnaLiczbaZdjecProperty = DependencyProperty.Register(
        nameof(MaksymalnaLiczbaZdjec), typeof(int), typeof(GaleriaZdjec),
        new FrameworkPropertyMetadata(0, OnUstawieniaGaleriiChanged));

    public static readonly DependencyProperty OpisyProperty = DependencyProperty.Register(
        nameof(Opisy), typeof(Dictionary<string, string>), typeof(GaleriaZdjec),
        new FrameworkPropertyMetadata(null, OnUstawieniaGaleriiChanged));

    /// <summary>0 = bez limitu; galeria referencji ma limit 10.</summary>
    public int MaksymalnaLiczbaZdjec
    {
        get => (int)GetValue(MaksymalnaLiczbaZdjecProperty);
        set => SetValue(MaksymalnaLiczbaZdjecProperty, value);
    }

    /// <summary>Opcjonalne opisy zdjęć, zapisywane w modelu zestawu.</summary>
    public Dictionary<string, string>? Opisy
    {
        get => (Dictionary<string, string>?)GetValue(OpisyProperty);
        set => SetValue(OpisyProperty, value);
    }

    private static void OnUstawieniaGaleriiChanged(DependencyObject obiekt, DependencyPropertyChangedEventArgs args)
        => ((GaleriaZdjec)obiekt).OdswiezGalerie();

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

    public GaleriaZdjec()
    {
        AllowDrop = true;
        DragEnter += Galeria_DragOver;
        DragOver += Galeria_DragOver;
        Drop += Galeria_Drop;
    }

    private void Galeria_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) &&
            Sciezki is List<string> lista &&
            (MaksymalnaLiczbaZdjec <= 0 || lista.Count < MaksymalnaLiczbaZdjec)
            ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Galeria_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] sciezki)
            DodajPliki(sciezki);
        e.Handled = true;
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
            var opis = Opisy != null && Opisy.TryGetValue(sciezka!, out var zapisanyOpis)
                ? zapisanyOpis : null;
            if (!string.IsNullOrWhiteSpace(opis))
                miniatura.ToolTip = opis;
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
            menu.Items.Add(pozycjaPowieksz);
            menu.Items.Add(pozycjaGlowne);
            if (Opisy != null)
            {
                var pozycjaOpis = new MenuItem { Header = "Edytuj opis…" };
                pozycjaOpis.Click += (_, _) => EdytujOpis(sciezkaLokalna!);
                menu.Items.Add(pozycjaOpis);
            }
            var pozycjaUsun = new MenuItem { Header = "Usuń zdjęcie z galerii…" };
            pozycjaUsun.Click += (_, _) => Usun(indeksLokalny);
            menu.Items.Add(pozycjaUsun);
            miniatura.ContextMenu = menu;
            kontener.Children.Add(miniatura);
            i++;
        }

        // Limit odnosi się do pojedynczej galerii. Istniejące zdjęcia nigdy
        // nie są usuwane automatycznie przy obniżeniu limitu.
        if (MaksymalnaLiczbaZdjec > 0 && sciezki.Count >= MaksymalnaLiczbaZdjec)
            return;

        // przycisk „Dodaj zdjęcie…" / przeciągnij i upuść
        var dodaj = new Button
        {
            Width = 72,
            Height = 72,
            Margin = new Thickness(0, 0, 6, 6),
            Content = new TextBlock { Text = "+", FontSize = 24, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
            ToolTip = "Dodaj zdjęcia… (można również przeciągnąć pliki do galerii)",
        };
        // NFR-08: przycisk „+” czytelny dla czytnika ekranu (sam znak „+” byłby niejednoznaczny)
        AutomationProperties.SetName(dodaj, $"Dodaj zdjęcie do: {WlascicielNazwa}");
        dodaj.Click += (_, _) => DodajZdjecia();
        kontener.Children.Add(dodaj);
    }

    private void DodajZdjecia()
    {
        if (Sciezki is not List<string> || Serwis == null)
            return;
        var dialog = new OpenFileDialog
        {
            Title = "Wybierz zdjęcia",
            Filter = "Obrazy (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
            Multiselect = true,
            CheckFileExists = true,
        };
        if (dialog.ShowDialog() == true)
            DodajPliki(dialog.FileNames);
    }

    /// <summary>Wspólna ścieżka dla dialogu i drag&drop; zwraca liczbę dodanych plików.</summary>
    public int DodajPliki(IEnumerable<string> pliki)
    {
        if (Sciezki is not List<string> lista || Serwis == null)
            return 0;
        var dodane = 0;
        var odrzucone = new List<string>();
        var limit = MaksymalnaLiczbaZdjec > 0 ? MaksymalnaLiczbaZdjec : int.MaxValue;
        foreach (var plik in pliki)
        {
            if (lista.Count >= limit)
            {
                odrzucone.Add(Path.GetFileName(plik) + " (osiągnięto limit)");
                continue;
            }
            if (!File.Exists(plik) || !DozwoloneRozszerzenia.Contains(Path.GetExtension(plik)))
            {
                odrzucone.Add(Path.GetFileName(plik) + " (nieobsługiwany format)");
                continue;
            }
            try
            {
                var sciezka = Serwis.ZapiszDoKatalogu(plik);
                lista.Add(sciezka);
                dodane++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                       or ArgumentException or NotSupportedException)
            {
                odrzucone.Add(Path.GetFileName(plik) + " (" + ex.Message + ")");
            }
        }
        if (dodane > 0)
        {
            Zmieniono?.Invoke(this, EventArgs.Empty);
            OdswiezGalerie();
        }
        if (odrzucone.Count > 0)
            MessageBox.Show(Window.GetWindow(this),
                "Nie dodano następujących zdjęć:\n" + string.Join("\n", odrzucone),
                "Kwatermistrz — galeria", MessageBoxButton.OK, MessageBoxImage.Information);
        return dodane;
    }

    private void Usun(int indeks)
    {
        if (Sciezki is not List<string> lista || indeks < 0 || indeks >= lista.Count)
            return;
        var odpowiedz = MessageBox.Show(Window.GetWindow(this),
            "Usunąć zdjęcie z tej galerii?\n\n" +
            "Zdjęcie przestanie być widoczne w zestawie. Plik pozostanie w katalogu danych " +
            "jako zabezpieczenie istniejących kopii i innych zestawów.",
            "Kwatermistrz — usuń zdjęcie", MessageBoxButton.YesNo,
            MessageBoxImage.Question, MessageBoxResult.No);
        if (odpowiedz != MessageBoxResult.Yes)
            return;
        lista.RemoveAt(indeks);
        // Nie kasujemy fizycznego pliku: może być używany przez inny rekord
        // lub przez kopię zapasową JSON. Usunięcie z galerii = usunięcie referencji.
        Zmieniono?.Invoke(this, EventArgs.Empty);
        OdswiezGalerie();
    }

    private void EdytujOpis(string sciezka)
    {
        if (Opisy == null)
            return;
        var opis = Opisy.TryGetValue(sciezka, out var istniejacy) ? istniejacy : "";
        var okno = new Window
        {
            Title = "Opis zdjęcia",
            Width = 460,
            Height = 225,
            MinWidth = 340,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this),
            ResizeMode = ResizeMode.NoResize,
            Background = (Brush?)Application.Current.TryFindResource("ApplicationBackgroundBrush")
                ?? Brushes.White,
        };
        var zawartosc = new DockPanel { Margin = new Thickness(18) };
        var przyciski = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0),
        };
        var anuluj = new Button { Content = "Anuluj", MinWidth = 86, Margin = new Thickness(0, 0, 8, 0) };
        var zapisz = new Button { Content = "Zapisz", MinWidth = 86, IsDefault = true };
        anuluj.Click += (_, _) => okno.DialogResult = false;
        zapisz.Click += (_, _) => okno.DialogResult = true;
        przyciski.Children.Add(anuluj);
        przyciski.Children.Add(zapisz);
        DockPanel.SetDock(przyciski, Dock.Bottom);
        zawartosc.Children.Add(przyciski);
        var pole = new TextBox
        {
            Text = opis,
            MaxLength = 500,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
        zawartosc.Children.Add(pole);
        okno.Content = zawartosc;
        if (okno.ShowDialog() != true)
            return;
        var nowy = pole.Text.Trim();
        if (nowy.Length == 0)
            Opisy.Remove(sciezka);
        else
            Opisy[sciezka] = nowy;
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
