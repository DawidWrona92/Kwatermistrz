using System.Windows;
using Kwatermistrz.App.ViewModels;
using Wpf.Ui.Controls;

namespace Kwatermistrz.App.Views;

/// <summary>Okno główne: nawigacja (§10.4), przełącznik rekonstrukcji (FR-1303), panel karty po prawej.</summary>
public partial class MainWindow : FluentWindow
{
    private readonly Dictionary<string, FrameworkElement> _strony = new();
    private readonly Dictionary<string, Func<FrameworkElement>> _fabrykiStron = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null)
            return;
        if (DataContext is MainWindowViewModel vm && vm.Pulpit != null)
        {
            vm.Pulpit.Odswiez();
            // karty skrótowe pulpitu (FR-104) i ranking (FR-105) — przejście do listy z filtrem
            vm.Pulpit.PrzejdzDo += PrzejdzZPulpitu;
            vm.FokusujSzukanieWidok += () => PoleSzukania.Focus();
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.AktualnyEkran))
                {
                    PokazEkran(vm.AktualnyEkran);
                    ZaznaczNawigacje(vm.AktualnyEkran);
                }
            };
        }
        // globalne wyszukiwanie (FR-1201): wyniki pod polem, Enter otwiera pierwszy
        PoleSzukania.TextChanged += (_, _) =>
        {
            if (ViewModel == null)
                return;
            ViewModel.Szukanie.Szukaj(ViewModel.SzukajTekst);
            PopupWyniki.IsOpen = ViewModel.Szukanie.Wyniki.Count > 0 && !string.IsNullOrWhiteSpace(ViewModel.SzukajTekst);
        };
        PoleSzukania.PreviewKeyDown += (_, args) =>
        {
            if (args.Key == System.Windows.Input.Key.Enter && ViewModel?.Szukanie.Wyniki.Count > 0)
            {
                OtworzWynik(ViewModel.Szukanie.Wyniki[0]);
                args.Handled = true;
            }
            if (args.Key == System.Windows.Input.Key.Escape)
                PopupWyniki.IsOpen = false;
        };

        // rozmiar okna z ustawień (NFR-12)
        var u = App.Dane.Ustawienia;
        if (u.SzerokoscOkna > 0)
            Width = u.SzerokoscOkna;
        if (u.WysokoscOkna > 0)
            Height = u.WysokoscOkna;

        ZarejestrujStrony();
        // przywrócenie ostatniego ekranu (FR-1303/NFR-12)
        var ekran = u.OstatniEkran is { } zapamietany && _fabrykiStron.ContainsKey(zapamietany)
            ? zapamietany : "Pulpit";
        PokazEkran(ekran);
        ZaznaczNawigacje(ekran);
    }

    private void ZarejestrujStrony()
    {
        var vm = ViewModel!;
        // XAML każdej strony tworzymy dopiero przy pierwszym wejściu.
        // Awaria pojedynczego ekranu nie blokuje pulpitu i pozostałych funkcji.
        _fabrykiStron["Pulpit"] = () => new PulpitPage { DataContext = vm.Pulpit };
        _fabrykiStron["Slowniki"] = () => new SlownikiPage { DataContext = vm.Slowniki };
        _fabrykiStron["Ustawienia"] = () => new UstawieniaPage { DataContext = vm.Ustawienia };
        _fabrykiStron["Import"] = () => new ImportPage { DataContext = vm.Import };
        _fabrykiStron["Pakiety"] = () => new PakietyPage { DataContext = vm.Pakiety };
        _fabrykiStron["Wyprawy"] = () => new WyprawyPage { DataContext = vm.Wyprawy };
        _fabrykiStron["Bronie"] = () => new BroniePage { DataContext = vm.Repliki };
        _fabrykiStron["Wyposazenie"] = () => new WyposazeniePage { DataContext = vm.Wyposazenie };
        _fabrykiStron["Zestawy"] = () => new ZestawyPage { DataContext = vm.Zestawy };
        _fabrykiStron["Raporty"] = () => new RaportyPage { DataContext = vm.Raporty };
        _fabrykiStron["DoUzupelnienia"] = () => new DoUzupelnieniaPage { DataContext = vm.DoUzupelnienia };
        _fabrykiStron["Rekonstrukcje"] = () => new RekonstrukcjePage { DataContext = vm.Rekonstrukcje };
    }

    /// <summary>Tworzy każdy ekran i sprawdza parsowanie XAML bez wyświetlania okna.</summary>
    public void ZweryfikujEkrany()
    {
        if (_fabrykiStron.Count == 0)
            ZarejestrujStrony();
        foreach (var (nazwa, fabryka) in _fabrykiStron)
        {
            try
            {
                _strony.TryAdd(nazwa, fabryka());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Nie udało się utworzyć ekranu: {nazwa}", ex);
            }
        }
        // Karty są tworzone poza Frame; testujemy także ich zasoby i konwertery.
        _ = new KartaPrzedmiotu { ViewModel = ViewModel!.Wyposazenie };
        _ = new KartaRepliki { ViewModel = ViewModel!.Repliki };
        _ = new KreatorWindow();
    }

    /// <summary>Smoke test Windows: rzeczywiste przejścia Frame.Navigate i zdarzenia Loaded.</summary>
    public void ZweryfikujNawigacje()
    {
        foreach (var ekran in _fabrykiStron.Keys.ToList())
        {
            PokazEkran(ekran);
            // Frame może planować ładowanie na dispatcherze zamiast wykonywać je od razu.
            var klatka = new System.Windows.Threading.DispatcherFrame();
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(() => klatka.Continue = false));
            System.Windows.Threading.Dispatcher.PushFrame(klatka);
        }
    }

    /// <summary>Windows CI: pierwszy start w motywie ciemnym i jasnym oraz zapis kreatora.</summary>
    public void ZweryfikujKreator()
    {
        foreach (var motyw in new[] { Wpf.Ui.Appearance.ApplicationTheme.Dark, Wpf.Ui.Appearance.ApplicationTheme.Light })
        {
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(motyw);
            var vm = new KreatorViewModel(App.Dane, App.Magazyn);
            var kreator = new KreatorWindow { Owner = this, DataContext = vm };
            Exception? blad = null;
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                try
                {
                    kreator.ZweryfikujOpcjeStartu();
                    if (motyw == Wpf.Ui.Appearance.ApplicationTheme.Light)
                    {
                        vm.NazwaRekonstrukcji = "Test pierwszego uruchomienia";
                        vm.PotwierdzCommand.Execute(null);
                        if (App.Dane.CzyPierwszeUruchomienie || vm.Komunikat.Length > 0)
                            throw new InvalidOperationException("Nie udało się zakończyć kreatora.");
                    }
                    else
                    {
                        kreator.Close();
                    }
                }
                catch (Exception ex)
                {
                    blad = ex;
                    kreator.Close();
                }
            }));
            var wynik = kreator.ShowDialog();
            if (blad != null)
                throw new InvalidOperationException($"Test kreatora w motywie {motyw} nie powiódł się.", blad);
            if (motyw == Wpf.Ui.Appearance.ApplicationTheme.Light && wynik != true)
                throw new InvalidOperationException("Kreator nie zakończył się poprawnie po zatwierdzeniu.");
        }
    }

    private static IEnumerable<T> ZnajdzWizualne<T>(DependencyObject rodzic) where T : DependencyObject
    {
        if (rodzic is T znaleziony)
            yield return znaleziony;
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(rodzic); i++)
            foreach (var element in ZnajdzWizualne<T>(System.Windows.Media.VisualTreeHelper.GetChild(rodzic, i)))
                yield return element;
    }

    /// <summary>Weryfikuje realne piksele 2x2 i zmianę Wzor już po pierwszym renderze.</summary>
    private static void ZweryfikujRysowanieKamuflazu()
    {
        var kontrolka = new Kwatermistrz.App.Controls.ProbkaKamuflazu();
        var paleta = new Kwatermistrz.Core.Model.WzorKamuflazu
        {
            KoloryProbki = ["#FF0000", "#00FF00", "#0000FF", "#FFFFFF"],
        };
        void Renderuj()
        {
            kontrolka.Measure(new Size(24, 16));
            kontrolka.Arrange(new Rect(0, 0, 24, 16));
            kontrolka.UpdateLayout();
            var obraz = new System.Windows.Media.Imaging.RenderTargetBitmap(
                24, 16, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            obraz.Render(kontrolka);
            var piksele = new byte[24 * 16 * 4];
            obraz.CopyPixels(piksele, 24 * 4, 0);
            if (kontrolka.Wzor == paleta)
            {
                void Sprawdz(int x, int y, byte r, byte g, byte b)
                {
                    var i = (y * 24 + x) * 4;
                    if (piksele[i] != b || piksele[i + 1] != g || piksele[i + 2] != r)
                        throw new InvalidOperationException(
                            $"Błędny kolor próbki kamuflażu w punkcie ({x},{y}).");
                }
                Sprawdz(3, 3, 255, 0, 0);
                Sprawdz(17, 3, 0, 255, 0);
                Sprawdz(3, 12, 0, 0, 255);
                Sprawdz(17, 12, 255, 255, 255);
            }
        }

        // Zmiana powiązania w zrealizowanym już wierszu nie może modyfikować Template.
        kontrolka.Wzor = paleta;
        Renderuj();
        foreach (var ilosc in new[] { 1, 2, 3, 4 })
        {
            kontrolka.Wzor = new Kwatermistrz.Core.Model.WzorKamuflazu
            {
                KoloryProbki = paleta.KoloryProbki.Take(ilosc).ToList(),
            };
            Renderuj();
        }
        kontrolka.Wzor = new Kwatermistrz.Core.Model.WzorKamuflazu
        {
            KoloryProbki = ["#NIEPOPRAWNY"],
        };
        Renderuj();
        kontrolka.Wzor = null;
        Renderuj();
        kontrolka.Wzor = paleta;
        Renderuj();
    }

    private void PrzepompujNawigacje()
    {
        var klatka = new System.Windows.Threading.DispatcherFrame();
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle,
            new Action(() => klatka.Continue = false));
        System.Windows.Threading.Dispatcher.PushFrame(klatka);
    }

    /// <summary>Regresja: poprzednio każde przejście pozostawiało niebieskie
    /// podświetlenie poprzedniej pozycji NavigationView.</summary>
    public void ZweryfikujJednoZaznaczenieMenu()
    {
        foreach (var ekran in new[]
        {
            "Pulpit", "Wyprawy", "Bronie", "Wyposazenie",
            "Zestawy", "Raporty", "DoUzupelnienia", "Slowniki",
            "Import", "Pakiety", "Ustawienia", "Pulpit",
        })
        {
            OtworzEkranZMenu(ekran);
            PrzepompujNawigacje();

            var aktywne = WszystkiePozycjeNawigacji()
                .OfType<NavigationViewItem>()
                .Where(item => item.IsActive).ToList();
            if (aktywne.Count != 1 || aktywne[0].Tag as string != ekran)
                throw new InvalidOperationException(
                    $"Błędny stan menu po przejściu do '{ekran}': " +
                    string.Join(", ", aktywne.Select(item => item.Tag)));
        }
    }

    /// <summary>Windows smoke: prawdziwy edytor zestawu, dwie galerie, limit i zapis plików.</summary>
    public void ZweryfikujGalerieZestawu()
    {
        PokazEkran("Zestawy");
        PrzepompujNawigacje();
        UpdateLayout();
        if (ObszarZawartosci.Content is not ZestawyPage strona)
            throw new InvalidOperationException("Nie udało się wyświetlić edytora zestawu.");

        // Loaded odświeża Wiersze (i resetuje DataGrid.SelectedItem);
        // wybór musi nastąpić po zakończeniu ładowania prawdziwego ekranu.
        ViewModel!.Zestawy.Odswiez();
        var wiersz = ViewModel.Zestawy.Wiersze.FirstOrDefault();
        if (wiersz == null)
            throw new InvalidOperationException("Brak zestawu do testu galerii.");
        ViewModel.Zestawy.Wybrany = wiersz;
        PrzepompujNawigacje();
        strona.UpdateLayout();
        var galerie = ZnajdzWizualne<Kwatermistrz.App.Controls.GaleriaZdjec>(strona).ToList();
        if (galerie.Count != 2)
            throw new InvalidOperationException($"Oczekiwano 2 galerii, znaleziono: {galerie.Count}.");

        var wlasne = galerie.Single(g => g.MaksymalnaLiczbaZdjec == 0);
        var referencje = galerie.Single(g => g.MaksymalnaLiczbaZdjec == 10);
        if (!ReferenceEquals(wlasne.Sciezki, wiersz.Zestaw.Zdjecia) ||
            !ReferenceEquals(referencje.Sciezki, wiersz.Zestaw.ZdjeciaReferencyjne) ||
            !ReferenceEquals(wlasne.Opisy, wiersz.Zestaw.OpisyZdjec) ||
            referencje.Serwis == null)
            throw new InvalidOperationException(
                $"Nieprawidłowe powiązania galerii: moje={ReferenceEquals(wlasne.Sciezki, wiersz.Zestaw.Zdjecia)}, " +
                $"ref={ReferenceEquals(referencje.Sciezki, wiersz.Zestaw.ZdjeciaReferencyjne)}, " +
                $"opisy={ReferenceEquals(wlasne.Opisy, wiersz.Zestaw.OpisyZdjec)}, " +
                $"serwis={(referencje.Serwis != null)}; " +
                $"Wybrany={(ViewModel.Zestawy.Wybrany?.Id ?? "null")}, " +
                $"DataContext={strona.DataContext?.GetType().Name}.");

        var plik = System.IO.Path.Combine(App.KatalogDanych, "probka-galerii.png");
        var piksel = System.Windows.Media.Imaging.BitmapSource.Create(
            1, 1, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null,
            new byte[] { 4, 15, 230, 255 }, 4);
        var png = new System.Windows.Media.Imaging.PngBitmapEncoder();
        png.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(piksel));
        using (var strumien = System.IO.File.Create(plik))
            png.Save(strumien);
        try
        {
            var własnaPoczatkowa = wiersz.Zestaw.Zdjecia.Count;
            var refsPoczatkowe = wiersz.Zestaw.ZdjeciaReferencyjne.Count;
            if (wlasne.DodajPliki([plik]) != 1 ||
                wiersz.Zestaw.Zdjecia.Count != własnaPoczatkowa + 1)
                throw new InvalidOperationException("Dodawanie własnego zdjęcia nie działa.");

            var ileDodac = Math.Max(0, 10 - refsPoczatkowe);
            if (referencje.DodajPliki(Enumerable.Repeat(plik, ileDodac)) != ileDodac ||
                wiersz.Zestaw.ZdjeciaReferencyjne.Count != 10)
                throw new InvalidOperationException("Dodawanie referencji do limitu 10 nie działa.");

            // Gdy galeria jest pełna, przycisk dodawania musi zniknąć,
            // ale żaden zapisany obraz nie może zostać automatycznie usunięty.
            referencje.UpdateLayout();
            var panelMiniatur = referencje.Template?.FindName("PART_Kontener", referencje)
                as System.Windows.Controls.Panel;
            if (panelMiniatur == null ||
                panelMiniatur.Children.OfType<System.Windows.Controls.Button>().Any())
                throw new InvalidOperationException("Pełna galeria referencji nadal pozwala dodać zdjęcie.");
            if (ViewModel.Zestawy.LicznikReferencji != "10/10")
                throw new InvalidOperationException("Licznik referencji nie odświeżył się po dodawaniu.");
            var opisSciezka = wiersz.Zestaw.ZdjeciaReferencyjne.Last();
            wiersz.Zestaw.OpisyZdjec[opisSciezka] = "Zdjęcie referencyjne — test";
            ViewModel.Zestawy.ZapiszGalerie();

            var odczyt = new Kwatermistrz.Core.Storage.MagazynJson(App.KatalogDanych).Wczytaj();
            var zestaw = odczyt.Zestawy.Single(z => z.Id == wiersz.Id);
            if (zestaw.Zdjecia.Count != wiersz.Zestaw.Zdjecia.Count ||
                zestaw.ZdjeciaReferencyjne.Count != wiersz.Zestaw.ZdjeciaReferencyjne.Count ||
                zestaw.OpisyZdjec[opisSciezka] != "Zdjęcie referencyjne — test" ||
                !System.IO.File.Exists(referencje.Serwis.PelnaSciezka(opisSciezka)))
                throw new InvalidOperationException("Zdjęcia i opisy zestawu nie zostały trwale zapisane.");
        }
        finally
        {
            // Plik źródłowy może zniknąć; skopiowane fotografie i JSON są w
            // izolowanym profilu smoke i zostaną posprzątane przez runner.
            System.IO.File.Delete(plik);
        }
        PokazEkran("Pulpit");
    }

    /// <summary>Test wizualnej regresji: podstawowe etykiety pulpitu nie mogą mieć czarnego tekstu w dark mode.</summary>
    public void ZweryfikujKoloryPulpitu()
    {
        foreach (var motyw in new[] { Wpf.Ui.Appearance.ApplicationTheme.Dark, Wpf.Ui.Appearance.ApplicationTheme.Light })
        {
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(motyw);
            App.UstawAliasyWykresow(motyw == Wpf.Ui.Appearance.ApplicationTheme.Light);
            PokazEkran("Pulpit");
            UpdateLayout();

            if (!_strony.TryGetValue("Pulpit", out var pulpit))
                throw new InvalidOperationException("Nie znaleziono Pulpitu do testu kolorów.");

            var etykiety = ZnajdzTeksty(pulpit)
                .Where(t => t.Text is "Pulpit" or "Wyprawy łącznie" or "Do zrobienia i sprawdzenia")
                .ToList();
            if (etykiety.Count != 3)
                throw new InvalidOperationException($"Nie znaleziono etykiet Pulpitu: {etykiety.Count}/3.");

            var wzorcowy = App.Kolory.Glowny as System.Windows.Media.SolidColorBrush;
            if (wzorcowy == null)
                throw new InvalidOperationException("Brak podstawowego pędzla tekstu WPF-UI.");

            foreach (var etykieta in etykiety)
            {
                if (etykieta.Foreground is not System.Windows.Media.SolidColorBrush kolor ||
                    kolor.Color != wzorcowy.Color)
                    throw new InvalidOperationException(
                        $"Nieprawidłowy kolor '{etykieta.Text}' w motywie {motyw}: " +
                        $"Fore={etykieta.Foreground}, expected={wzorcowy.Color}, " +
                        $"Page={pulpit.GetValue(System.Windows.Documents.TextElement.ForegroundProperty)}, " +
                        $"style={etykieta.Style?.TargetType}.");
            }
        }
        // Po zakończeniu testu motyw ustawiamy tak jak konfiguracja systemowa.
        Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Dark);
        App.UstawAliasyWykresow(jasny: false);
    }

    private static IEnumerable<System.Windows.Controls.TextBlock> ZnajdzTeksty(DependencyObject element)
    {
        if (element is System.Windows.Controls.TextBlock tekst)
            yield return tekst;
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(element); i++)
        {
            foreach (var potomny in ZnajdzTeksty(System.Windows.Media.VisualTreeHelper.GetChild(element, i)))
                yield return potomny;
        }
    }

    /// <summary>UI smoke: pasek przeciągania oraz czytelne, dopasowane kolumny tabel.</summary>
    public void ZweryfikujTabeleIPasekTytulu()
    {
        if (!PasekTytulowy.IsVisible || PasekTytulowy.ActualHeight < 30 ||
            !PasekTytulowy.ShowMinimize || !PasekTytulowy.ShowMaximize ||
            !PasekTytulowy.ShowClose)
            throw new InvalidOperationException("Brak aktywnego paska tytułowego i przycisków okna.");

        foreach (var ekran in new[]
        {
            "Wyposazenie", "Bronie", "Wyprawy", "Zestawy",
            "DoUzupelnienia", "Slowniki", "Pakiety", "Raporty",
        })
        {
            PokazEkran(ekran);
            PrzepompujNawigacje();
            UpdateLayout();

            if (ObszarZawartosci.Content is not DependencyObject zawartosc)
                throw new InvalidOperationException($"Brak widoku: {ekran}.");
            if (ekran == "Zestawy" && zawartosc is ZestawyPage stronaZestawow)
                stronaZestawow.ZweryfikujUklad();

            var tabele = ZnajdzWizualne<System.Windows.Controls.DataGrid>(zawartosc).ToList();
            if (tabele.Count == 0)
                throw new InvalidOperationException($"Brak tabeli w ekranie: {ekran}.");

            foreach (var tabela in tabele)
            {
                if (tabela.GridLinesVisibility != System.Windows.Controls.DataGridGridLinesVisibility.All ||
                    !tabela.CanUserResizeColumns ||
                    tabela.BorderThickness.Left < 1 ||
                    tabela.HorizontalGridLinesBrush == null ||
                    tabela.VerticalGridLinesBrush == null ||
                    tabela.AlternationCount < 2)
                    throw new InvalidOperationException($"Tabela {ekran} nie ma poprawnego wyglądu/resize.");

                if (!tabela.ColumnWidth.IsStar ||
                    !Kwatermistrz.App.Controls.ProporcjonalneKolumny.GetWlaczone(tabela))
                    throw new InvalidOperationException(
                        $"Tabela {ekran} nie ma włączonego automatycznego wypełniania szerokości.");

                foreach (var kolumna in tabela.Columns.OfType<System.Windows.Controls.DataGridTextColumn>())
                {
                    if ((!kolumna.Width.IsStar && !kolumna.Width.IsAbsolute) ||
                        kolumna.MinWidth < 40)
                        throw new InvalidOperationException(
                            $"Kolumna tekstowa '{kolumna.Header}' w {ekran} ma niepoprawny rozmiar.");
                }
            }
        }
        PokazEkran("Pulpit");
    }

    /// <summary>
    /// Regresja: wyposażenie po samym otwarciu strony ma wypełnić szerokie
    /// okno i pokazywać pełne checkboxy; bez jakiejkolwiek interakcji użytkownika.
    /// </summary>
    public void ZweryfikujPierwszyRenderWyposazenia()
    {
        var widok = new WyposazeniePage { DataContext = ViewModel!.Wyposazenie };
        var okno = new Window
        {
            Title = "Kwatermistrz — test pierwszego renderowania wyposażenia",
            Owner = this,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = 24,
            Top = 24,
            Width = 1650,
            Height = 760,
            Content = widok,
        };
        try
        {
            // Test świadomie NIE zmienia szerokości okna po Show().
            okno.Show();
            PrzepompujNawigacje();
            okno.UpdateLayout();

            var tabela = ZnajdzWizualne<System.Windows.Controls.DataGrid>(widok)
                .SingleOrDefault()
                ?? throw new InvalidOperationException("Brak tabeli wyposażenia.");
            if (tabela.Items.Count < 100)
                throw new InvalidOperationException("Nie wczytano danych wyposażenia do testu.");

            var zajeta = tabela.Columns
                .Where(k => k.Visibility == Visibility.Visible)
                .Sum(k => k.ActualWidth);
            if (zajeta < tabela.ActualWidth - 45)
                throw new InvalidOperationException(
                    $"Przy pierwszym renderze tabela nie wypełnia okna: " +
                    $"kolumny={zajeta:F0}px, DataGrid={tabela.ActualWidth:F0}px.");

            if (tabela.Columns[0].ActualWidth < 60)
                throw new InvalidOperationException(
                    $"Kolumna checkboxów jest za wąska: {tabela.Columns[0].ActualWidth:F0}px.");

            var kolumnaMiejsca = tabela.Columns
                .OfType<System.Windows.Controls.DataGridTextColumn>()
                .SingleOrDefault(k => Equals(k.Header, "Miejsce"));
            if (kolumnaMiejsca?.Binding is not System.Windows.Data.MultiBinding)
                throw new InvalidOperationException(
                    "Wyposażenie nadal wyświetla wewnętrzne ID miejsca zamiast nazwy.");

            var checkbox = ZnajdzWizualne<System.Windows.Controls.CheckBox>(tabela)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("Nie wyrenderowano checkboxa wiersza.");
            if (checkbox.ActualWidth < 24 || checkbox.ActualHeight < 18)
                throw new InvalidOperationException(
                    $"Checkbox ma za mały rozmiar: {checkbox.ActualWidth:F0}×{checkbox.ActualHeight:F0}px.");

            var rodzic = System.Windows.Media.VisualTreeHelper.GetParent(checkbox);
            while (rodzic != null && rodzic is not System.Windows.Controls.DataGridCell)
                rodzic = System.Windows.Media.VisualTreeHelper.GetParent(rodzic);
            if (rodzic is not System.Windows.Controls.DataGridCell komorka)
                throw new InvalidOperationException("Checkbox nie znajduje się w komórce tabeli.");

            var granice = checkbox.TransformToAncestor(komorka)
                .TransformBounds(new Rect(new Point(0, 0), checkbox.RenderSize));
            if (granice.Left < -1 || granice.Right > komorka.ActualWidth + 1 ||
                granice.Top < -1 || granice.Bottom > komorka.ActualHeight + 1)
                throw new InvalidOperationException(
                    $"Checkbox wychodzi poza komórkę: checkbox={granice}, " +
                    $"komórka={komorka.ActualWidth:F0}×{komorka.ActualHeight:F0}px.");

            // Ręcznie ustawiona szerokość kolumny powinna pozostać nietknięta
            // podczas późniejszego rozszerzania okna; inne kolumny mogą rosnąć.
            var kolumnaNazwa = tabela.Columns
                .OfType<System.Windows.Controls.DataGridTextColumn>()
                .Single(k => Equals(k.Header, "Nazwa"));
            var kolumnaKomentarz = tabela.Columns
                .OfType<System.Windows.Controls.DataGridTextColumn>()
                .Single(k => Equals(k.Header, "Komentarz"));
            kolumnaNazwa.Width = new System.Windows.Controls.DataGridLength(260);
            PrzepompujNawigacje();
            var komentarzPrzed = kolumnaKomentarz.ActualWidth;
            okno.Width = 1850;
            okno.UpdateLayout();
            PrzepompujNawigacje();
            if (Math.Abs(kolumnaNazwa.ActualWidth - 260) > 2)
                throw new InvalidOperationException(
                    "Zmiana rozmiaru okna nadpisuje ręcznie wybraną szerokość kolumny.");
            if (kolumnaKomentarz.ActualWidth < komentarzPrzed + 10)
                throw new InvalidOperationException(
                    "Pozostałe kolumny nie wykorzystują przestrzeni po poszerzeniu okna.");
        }
        finally
        {
            okno.Close();
        }
    }

    /// <summary>
    /// Windows UI test: szerokość raportu należy rozdzielić na wszystkie kolumny,
    /// a nie zostawiać pusty obszar po ostatniej, jak w zgłoszonym zrzucie.
    /// </summary>
    public void ZweryfikujProporcjonalneKolumny()
    {
        // Izolowane okno hostuje prawdziwy RaportyPage. Frame.Navigate ma własną
        // kolejkę i potrafi przełączać poprzednią stronę w trakcie pomiaru;
        // UI test ma badać resize, a nie wyścig kolejki nawigacji.
        var poprzedniRaport = ViewModel!.Raporty.WybranyRaport;
        ViewModel.Raporty.WybranyRaport = "Umundurowanie";
        var widok = new RaportyPage { DataContext = ViewModel.Raporty };
        var okno = new Window
        {
            Title = "Kwatermistrz — test szerokości kolumn",
            Owner = this,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = 30,
            Top = 30,
            Width = 950,
            Height = 650,
            Content = widok,
        };
        try
        {
            okno.Show();
            PrzepompujNawigacje();
            okno.UpdateLayout();

            var tabela = ZnajdzWizualne<System.Windows.Controls.DataGrid>(widok)
                .SingleOrDefault(d => d.IsVisible &&
                    d.Columns.Count(c => c is System.Windows.Controls.DataGridTextColumn) == 7)
                ?? throw new InvalidOperationException("Nie załadowano siedmiu kolumn raportu Umundurowanie.");
            var kolumny = tabela.Columns
                .Where(c => c.Visibility == Visibility.Visible &&
                    c is System.Windows.Controls.DataGridTextColumn)
                .ToList();
            if (kolumny.Count != 7)
                throw new InvalidOperationException("Raport Umundurowanie nie ma 7 elastycznych kolumn.");

            var waskie = kolumny.Select(c => c.ActualWidth).ToArray();
            var szerokoscWaska = tabela.ActualWidth;
            okno.Width = 1350;
            okno.UpdateLayout();
            PrzepompujNawigacje();
            okno.UpdateLayout();
            var szerokie = kolumny.Select(c => c.ActualWidth).ToArray();
            var rosnie = kolumny.Where((_, i) => szerokie[i] > waskie[i] + 12).Count();
            if (rosnie < 4)
                throw new InvalidOperationException(
                    $"Po poszerzeniu Raportów rośnie tylko {rosnie}/{kolumny.Count} kolumn; " +
                    $"tabela: {szerokoscWaska:F0} -> {tabela.ActualWidth:F0} px; " +
                    $"przed=[{string.Join(", ", waskie.Select(w => w.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)))}], " +
                    $"po=[{string.Join(", ", szerokie.Select(w => w.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)))}].");

            if (kolumny.Sum(c => c.ActualWidth) < tabela.ActualWidth - 45)
                throw new InvalidOperationException("Po rozszerzeniu okna kolumny nie wypełniają szerokości tabeli.");
        }
        finally
        {
            okno.Close();
            ViewModel.Raporty.WybranyRaport = poprzedniRaport;
        }
    }

    /// <summary>Test regresji: realny import repozytoryjnego Excela po pokazaniu okna.</summary>
    public void ZweryfikujImportPoStarcie(string sciezkaXlsx)
    {
        if (!System.IO.File.Exists(sciezkaXlsx))
            throw new System.IO.FileNotFoundException("Brak arkusza do testu importu", sciezkaXlsx);

        // Tworzymy i kończymy kreator tak samo jak użytkownik, w tym zdarzenie
        // Zakonczono, zapis do JSON i zamknięcie okna modalnego.
        var vm = new KreatorViewModel(App.Dane, App.Magazyn)
        {
            WybranyStart = "import",
            SciezkaImportu = sciezkaXlsx,
        };
        var kreator = new KreatorWindow { Owner = this, DataContext = vm };
        Exception? blad = null;
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(() =>
        {
            try
            {
                vm.PotwierdzCommand.Execute(null);
                if (!string.IsNullOrWhiteSpace(vm.Komunikat))
                    throw new InvalidOperationException(vm.Komunikat);
            }
            catch (Exception ex)
            {
                blad = ex;
                kreator.Close();
            }
        }));
        var wynik = kreator.ShowDialog();
        if (blad != null)
            throw new InvalidOperationException("Nie udało się zaimportować repozytoryjnego arkusza.", blad);
        if (wynik != true)
            throw new InvalidOperationException("Kreator importu nie zakończył się pomyślnie.");

        ViewModel!.OdswiezWszystkieEkrany();

        var zapisane = new Kwatermistrz.Core.Storage.MagazynJson(App.KatalogDanych).Wczytaj();
        if (App.Dane.Repliki.Count < 27 || App.Dane.Przedmioty.Count < 105 ||
            zapisane.Repliki.Count != App.Dane.Repliki.Count ||
            zapisane.Przedmioty.Count != App.Dane.Przedmioty.Count)
            throw new InvalidOperationException("Rekordy nie zostały poprawnie zaimportowane i zapisane.");

        if (ViewModel.Pulpit.LacznaLiczbaWypraw != 89)
            throw new InvalidOperationException(
                $"Pulpit nie pokazuje 89 wypraw po imporcie; pokazuje {ViewModel.Pulpit.LacznaLiczbaWypraw}.");

        if (ViewModel.RekonstrukcjeLista.Count < 3)
            throw new InvalidOperationException("Wybór rekonstrukcji nie odświeżył się po imporcie.");

        // Sprawdzamy akcje użytkownika, a nie wyłącznie utworzenie widoków XAML.
        ViewModel.DodajWypraweCommand.Execute(null);
        PrzepompujNawigacje();
        if (ViewModel.AktualnyEkran != "Wyprawy" || ObszarZawartosci.Content is not WyprawyPage)
            throw new InvalidOperationException($"Przycisk 'Dodaj wyprawę' nie przełącza ekranu: " +
                $"VM={ViewModel.AktualnyEkran}, Frame={ObszarZawartosci.Content?.GetType().Name ?? "null"}.");

        ViewModel.NawigujCommand.Execute("Bronie");
        PrzepompujNawigacje();
        if (ViewModel.AktualnyEkran != "Bronie" || ObszarZawartosci.Content is not BroniePage)
            throw new InvalidOperationException($"Komenda nawigacji do Broni nie działa: " +
                $"VM={ViewModel.AktualnyEkran}, Frame={ObszarZawartosci.Content?.GetType().Name ?? "null"}.");

        // Regresja: wejście w kartę repliki ładowało readonly Status/Wyprawy
        // do Run.Text w trybie TwoWay i kończyło się InvalidOperationException.
        var testowaneRepliki = ViewModel.Repliki.Wiersze.Take(2).ToList();
        if (testowaneRepliki.Count < 2)
            throw new InvalidOperationException("Brak dwóch replik do testowania kart.");
        foreach (var replika in testowaneRepliki)
        {
            ViewModel.Repliki.Wybrana = replika;
            OdswiezKarte();
            PrzepompujNawigacje();
            ZawartoscKarty.UpdateLayout();
            if (ZawartoscKarty.Content is not KartaRepliki karta ||
                !ReferenceEquals(karta.DataContext, replika) ||
                !PanelKarty.IsVisible)
                throw new InvalidOperationException("Wybranie repliki nie otwiera poprawnej karty.");

            foreach (var blok in ZnajdzWizualne<System.Windows.Controls.TextBlock>(karta))
            {
                foreach (var run in blok.Inlines.OfType<System.Windows.Documents.Run>())
                {
                    var binding = System.Windows.Data.BindingOperations.GetBinding(run,
                        System.Windows.Documents.Run.TextProperty);
                    if (binding != null && binding.Mode != System.Windows.Data.BindingMode.OneWay)
                        throw new InvalidOperationException(
                            $"Run.Text w karcie repliki ma niebezpieczny tryb: {binding.Path?.Path}.");
                }
            }
        }

        // Regresja po zgłoszeniu 08.11: duża lista wyposażenia wcześniej wywoływała
        // InvalidOperationException: nie wolno modyfikować zapieczętowanego Template.
        ViewModel.NawigujCommand.Execute("Wyposazenie");
        PrzepompujNawigacje();
        if (ViewModel.AktualnyEkran != "Wyposazenie" ||
            ObszarZawartosci.Content is not WyposazeniePage stronaWyposazenia)
            throw new InvalidOperationException("Nie udało się otworzyć wyposażenia po imporcie.");

        var tabela = ZnajdzWizualne<System.Windows.Controls.DataGrid>(stronaWyposazenia).FirstOrDefault()
            ?? throw new InvalidOperationException("Nie znaleziono tabeli wyposażenia.");
        if (tabela.Items.Count < 105)
            throw new InvalidOperationException(
                $"Wyposażenie ma za mało wierszy po imporcie: {tabela.Items.Count}.");

        foreach (var indeks in new[] { 0, tabela.Items.Count / 4, tabela.Items.Count / 2,
                                       tabela.Items.Count - 1 })
        {
            tabela.ScrollIntoView(tabela.Items[indeks]);
            PrzepompujNawigacje();
            tabela.UpdateLayout();
        }

        ZweryfikujRysowanieKamuflazu();

        ViewModel.NawigujCommand.Execute("Pulpit");
        PrzepompujNawigacje();
        if (ViewModel.AktualnyEkran != "Pulpit" ||
            ViewModel.Pulpit.LacznaLiczbaWypraw != 89)
            throw new InvalidOperationException("Nie udało się powrócić do odświeżonego Pulpitu.");
    }

    /// <summary>Pokazuje ekran o podanym kluczu (bez zmiany zaznaczenia w nawigacji).</summary>
    public void PokazEkran(string ekran)
    {
        if (!_fabrykiStron.TryGetValue(ekran, out var fabryka))
            return;
        if (!_strony.TryGetValue(ekran, out var strona))
        {
            try
            {
                strona = fabryka();
                _strony[ekran] = strona;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Błąd inicjalizacji ekranu: {ekran}", ex);
            }
        }
        // Każda Page ma binding do palety INPC, aby wcześniej utworzone
        // widoki nie przechowywały czarnego Foreground po przełączeniu motywu.
        UstawWiazanieKoloru(strona);
        if (!ReferenceEquals(ObszarZawartosci.Content, strona))
            ObszarZawartosci.Navigate(strona);
        if (ekran == "Pulpit" && ViewModel?.Pulpit != null)
            ViewModel.Pulpit.Odswiez();
        // karta po prawej pokazuje wybrany element (FR-204/FR-302, §10.4)
        if (ViewModel == null)
            return;
        if (ekran == "Wyposazenie" && ViewModel.Wyposazenie.Wybrany != null)
            PokazKarte(new KartaPrzedmiotu { DataContext = ViewModel.Wyposazenie.Wybrany, ViewModel = ViewModel.Wyposazenie });
        else if (ekran == "Bronie" && ViewModel.Repliki.Wybrana != null)
            PokazKarte(new KartaRepliki { DataContext = ViewModel.Repliki.Wybrana, ViewModel = ViewModel.Repliki });
        else
            UkryjKarte();
    }

    /// <summary>Odświeża kartę po prawej po zmianie wyboru (FR-204, FR-302).</summary>
    public void OdswiezKarte()
    {
        if (ViewModel == null)
            return;
        if (ViewModel.AktualnyEkran == "Wyposazenie" && ViewModel.Wyposazenie.Wybrany != null)
            PokazKarte(new KartaPrzedmiotu { DataContext = ViewModel.Wyposazenie.Wybrany, ViewModel = ViewModel.Wyposazenie });
        else if (ViewModel.AktualnyEkran == "Bronie" && ViewModel.Repliki.Wybrana != null)
            PokazKarte(new KartaRepliki { DataContext = ViewModel.Repliki.Wybrana, ViewModel = ViewModel.Repliki });
        else
            UkryjKarte();
    }

    private bool _synchronizujeNawigacje;

    private void ZaznaczNawigacje(string ekran)
    {
        // WPF-UI Activate nie usuwa IsActive z pozostałych pozycji podczas
        // ręcznego nawigowania we własnym Frame. Synchronizujemy cały pasek.
        if (_synchronizujeNawigacje)
            return;
        _synchronizujeNawigacje = true;
        try
        {
            var pozycje = WszystkiePozycjeNawigacji()
                .OfType<NavigationViewItem>()
                .ToList();
            var aktywna = pozycje.FirstOrDefault(p => p.Tag as string == ekran);
            foreach (var pozycja in pozycje)
                if (!ReferenceEquals(pozycja, aktywna) && pozycja.IsActive)
                    pozycja.Deactivate(Nawigacja);
            if (aktywna != null && (!aktywna.IsActive ||
                !ReferenceEquals(Nawigacja.SelectedItem, aktywna)))
                aktywna.Activate(Nawigacja);
        }
        finally
        {
            _synchronizujeNawigacje = false;
        }
    }

    private IEnumerable<INavigationViewItem> WszystkiePozycjeNawigacji()
    {
        foreach (var pozycja in Nawigacja.MenuItems)
            if (pozycja is INavigationViewItem item)
                yield return item;
        foreach (var pozycja in Nawigacja.FooterMenuItems)
            if (pozycja is INavigationViewItem item)
                yield return item;
    }

    // WPF-UI może przetworzyć kliknięcie elementu menu przed zmianą SelectedItem.
    // Obsługujemy też rzeczywisty klik myszy, nie polegamy wyłącznie na SelectionChanged.
    private void Nawigacja_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var element = e.OriginalSource as DependencyObject;
        while (element != null && !ReferenceEquals(element, Nawigacja))
        {
            if (element is NavigationViewItem { Tag: string ekran })
            {
                OtworzEkranZMenu(ekran);
                return;
            }
            element = System.Windows.Media.VisualTreeHelper.GetParent(element);
        }
    }

    private void Nawigacja_SelectionChanged(NavigationView sender, RoutedEventArgs args)
    {
        if (_synchronizujeNawigacje)
            return;
        if (sender.SelectedItem is NavigationViewItem { Tag: string ekran })
            OtworzEkranZMenu(ekran);
    }

    private bool _obslugaMenu;

    private void OtworzEkranZMenu(string ekran)
    {
        if (ViewModel == null || _obslugaMenu)
            return;
        _obslugaMenu = true;
        try
        {
            PokazEkran(ekran);
        ViewModel.AktualnyEkran = ekran;
        // nawigacja przez menu czyści flagi ustawione skrótami z pulpitu (FR-104/105)
        WyczyscFlagiFiltru(ekran);
        // przy zmianie ekranu odświeżamy dane ekranu docelowego
        if (ekran == "Wyposazenie")
            ViewModel.Wyposazenie.Odswiez();
        if (ekran == "Bronie")
            ViewModel.Repliki.Odswiez();
        if (ekran == "Wyprawy")
            ViewModel.Wyprawy.Odswiez();
        if (ekran == "Zestawy")
            ViewModel.Zestawy.Odswiez();
        if (ekran == "Raporty")
            ViewModel.Raporty.Odswiez();
        if (ekran == "DoUzupelnienia")
            ViewModel.DoUzupelnienia.Odswiez();
        ZaznaczNawigacje(ekran);
        }
        finally
        {
            _obslugaMenu = false;
        }
    }

    /// <summary>Czyści flagi filtrów ustawiane skrótami z pulpitu — zostają włączone po powrocie przez menu, choć użytkownik oczekiwał pełnej listy.</summary>
    private void WyczyscFlagiFiltru(string ekran)
    {
        if (ViewModel == null)
            return;
        if (ekran == "Bronie")
        {
            ViewModel.Repliki.ZawiesOdswiezanieFiltru();
            ViewModel.Repliki.TylkoAwaria = false;
            ViewModel.Repliki.TylkoBezTestu = false;
            ViewModel.Repliki.WznowOdswiezanieFiltru();
        }
        if (ekran == "Wyposazenie")
        {
            ViewModel.Wyposazenie.ZawiesOdswiezanieFiltru();
            ViewModel.Wyposazenie.TylkoBezUzycia = false;
            ViewModel.Wyposazenie.WznowOdswiezanieFiltru();
        }
        if (ekran == "DoUzupelnienia")
        {
            ViewModel.DoUzupelnienia.ZawiesOdswiezanieFiltru();
            ViewModel.DoUzupelnienia.TylkoZadania = false;
            ViewModel.DoUzupelnienia.WznowOdswiezanieFiltru();
        }
    }

    private void ZamknijPanelKarty_Click(object sender, RoutedEventArgs e) => UkryjKarte();

    private void UkryjKarte()
    {
        ZawartoscKarty.Content = null;
        PanelKarty.Visibility = Visibility.Collapsed;
    }

    /// <summary>Przejście z karty pulpitu (FR-104) lub rankingu (FR-105): ekran + filtr/wybór rekordu.</summary>
    private void PrzejdzZPulpitu(string ekran, string? parametr)
    {
        if (ViewModel == null)
            return;
        PokazEkran(ekran);
        ZaznaczNawigacje(ekran);
        ViewModel.AktualnyEkran = ekran;
        if (ekran == "Bronie")
        {
            // flagi ustawiamy z wyłączonym odświeżaniem — każda zmiana flagi przebudowuje indeks; na końcu jedno Odswiez
            ViewModel.Repliki.ZawiesOdswiezanieFiltru();
            ViewModel.Repliki.TylkoAwaria = parametr == "awaria";
            ViewModel.Repliki.TylkoBezTestu = parametr == "bezTestu";
            ViewModel.Repliki.WznowOdswiezanieFiltru();
            if (parametr != null && parametr != "awaria" && parametr != "bezTestu")
                ViewModel.Repliki.Wybrana = ViewModel.Repliki.Wiersze.FirstOrDefault(w => w.Id == parametr);
        }
        if (ekran == "Wyposazenie")
        {
            ViewModel.Wyposazenie.ZawiesOdswiezanieFiltru();
            ViewModel.Wyposazenie.TylkoBezUzycia = parametr == "bezUzycia";
            ViewModel.Wyposazenie.WznowOdswiezanieFiltru();
        }
        if (ekran == "DoUzupelnienia")
        {
            ViewModel.DoUzupelnienia.ZawiesOdswiezanieFiltru();
            ViewModel.DoUzupelnienia.TylkoZadania = parametr == "zadania";
            ViewModel.DoUzupelnienia.WznowOdswiezanieFiltru();
        }
        // jedno odświeżenie po ustawieniu wszystkich flag
        if (ekran == "Bronie")
            ViewModel.Repliki.Odswiez();
        if (ekran == "Wyposazenie")
            ViewModel.Wyposazenie.Odswiez();
        if (ekran == "DoUzupelnienia")
            ViewModel.DoUzupelnienia.Odswiez();
        OdswiezKarte();
    }

    private void WynikSzukania_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is ViewModels.WynikSzukania wynik)
            OtworzWynik(wynik);
    }

    private void OtworzWynik(ViewModels.WynikSzukania wynik)
    {
        PopupWyniki.IsOpen = false;
        if (wynik.Ekran == null || ViewModel == null)
            return;
        PokazEkran(wynik.Ekran);
        ZaznaczNawigacje(wynik.Ekran);
        ViewModel.AktualnyEkran = wynik.Ekran;
        // wybór rekordu na ekranie docelowym
        if (wynik.Ekran == "Wyposazenie" && wynik.RekordId != null)
        {
            ViewModel.Wyposazenie.Odswiez();
            ViewModel.Wyposazenie.Wybrany = ViewModel.Wyposazenie.Wiersze.FirstOrDefault(w => w.Id == wynik.RekordId);
        }
        if (wynik.Ekran == "Bronie" && wynik.RekordId != null)
        {
            ViewModel.Repliki.Odswiez();
            ViewModel.Repliki.Wybrana = ViewModel.Repliki.Wiersze.FirstOrDefault(w => w.Id == wynik.RekordId);
        }
        if (wynik.Ekran == "Zestawy" && wynik.RekordId != null)
        {
            ViewModel.Zestawy.Odswiez();
            ViewModel.Zestawy.Wybrany = ViewModel.Zestawy.Wiersze.FirstOrDefault(w => w.Id == wynik.RekordId);
        }
        if (wynik.Ekran == "Wyprawy" && wynik.RekordId != null)
        {
            ViewModel.Wyprawy.Odswiez();
            ViewModel.Wyprawy.Wybrana = ViewModel.Wyprawy.Wiersze.FirstOrDefault(w => w.Id == wynik.RekordId);
        }
        OdswiezKarte();
    }

    /// <summary>Otwiera panel karty po prawej (§10.4).</summary>
    public void PokazKarte(FrameworkElement zawartosc)
    {
        UstawWiazanieKoloru(zawartosc);
        ZawartoscKarty.Content = zawartosc;
        PanelKarty.Visibility = Visibility.Visible;
    }

    private static void UstawWiazanieKoloru(FrameworkElement element)
    {
        var wlasciwosc = System.Windows.Documents.TextElement.ForegroundProperty;
        if (System.Windows.Data.BindingOperations.GetBindingBase(element, wlasciwosc) == null)
        {
            System.Windows.Data.BindingOperations.SetBinding(element, wlasciwosc,
                new System.Windows.Data.Binding(nameof(App.Kolory.Glowny))
                {
                    Source = App.Kolory,
                    Mode = System.Windows.Data.BindingMode.OneWay,
                });
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        var u = App.Dane.Ustawienia;
        u.SzerokoscOkna = Width;
        u.WysokoscOkna = Height;
        App.Magazyn.ZapiszDebounce(App.Dane);
        base.OnClosed(e);
    }
}
