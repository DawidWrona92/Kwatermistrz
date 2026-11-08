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
        if (ViewModel.AktualnyEkran != "Wyprawy" || ObszarZawartosci.Content is not WyprawyPage)
            throw new InvalidOperationException("Przycisk 'Dodaj wyprawę' nie przełącza ekranu.");

        ViewModel.NawigujCommand.Execute("Bronie");
        if (ViewModel.AktualnyEkran != "Bronie" || ObszarZawartosci.Content is not BroniePage)
            throw new InvalidOperationException("Komenda nawigacji do Broni nie działa.");

        ViewModel.NawigujCommand.Execute("Pulpit");
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

    private void ZaznaczNawigacje(string ekran)
    {
        foreach (var pozycja in WszystkiePozycjeNawigacji())
            if (pozycja is NavigationViewItem item && item.Tag as string == ekran)
            {
                if (!ReferenceEquals(Nawigacja.SelectedItem, item))
                    item.Activate(Nawigacja);
                return;
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
