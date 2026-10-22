using System.Windows;
using Kwatermistrz.App.ViewModels;
using Wpf.Ui.Controls;

namespace Kwatermistrz.App.Views;

/// <summary>Okno główne: nawigacja (§10.4), przełącznik rekonstrukcji (FR-1303), panel karty po prawej.</summary>
public partial class MainWindow : FluentWindow
{
    private readonly Dictionary<string, FrameworkElement> _strony = new();

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
            vm.Pulpit.Odswiez();
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
        var ekran = u.OstatniEkran ?? "Pulpit";
        PokazEkran(ekran);
        ZaznaczNawigacje(ekran);
    }

    private void ZarejestrujStrony()
    {
        var vm = ViewModel!;
        _strony["Pulpit"] = new PulpitPage { DataContext = vm.Pulpit };
        _strony["Slowniki"] = new SlownikiPage { DataContext = vm.Slowniki };
        _strony["Ustawienia"] = new UstawieniaPage { DataContext = vm.Ustawienia };
        _strony["Import"] = new ImportPage { DataContext = vm.Import };
        _strony["Pakiety"] = new PakietyPage { DataContext = vm.Pakiety };
        _strony["Wyprawy"] = new WyprawyPage { DataContext = vm.Wyprawy };
        _strony["Bronie"] = new BroniePage { DataContext = vm.Repliki };
        _strony["Wyposazenie"] = new WyposazeniePage { DataContext = vm.Wyposazenie };
        _strony["Zestawy"] = new ZestawyPage { DataContext = vm.Zestawy };
        _strony["Raporty"] = new RaportyPage { DataContext = vm.Raporty };
        _strony["DoUzupelnienia"] = new DoUzupelnieniaPage { DataContext = vm.DoUzupelnienia };
        // rekonstrukcje edytujemy w panelu karty po prawej stronie
        _strony["Rekonstrukcje"] = new RekonstrukcjePage { DataContext = vm.Rekonstrukcje };
    }

    /// <summary>Pokazuje ekran o podanym kluczu (bez zmiany zaznaczenia w nawigacji).</summary>
    public void PokazEkran(string ekran)
    {
        if (!_strony.TryGetValue(ekran, out var strona))
            return;
        ObszarZawartosci.Content = strona;
        if (ekran == "Pulpit" && ViewModel?.Pulpit != null)
            ViewModel.Pulpit.Odswiez();
        // karta po prawej pokazuje wybrany element (FR-204/FR-302, §10.4)
        if (ViewModel == null)
            return;
        if (ekran == "Wyposazenie" && ViewModel.Wyposazenie.Wybrany != null)
            PokazKarte(new KartaPrzedmiotu { DataContext = ViewModel.Wyposazenie.Wybrany });
        if (ekran == "Bronie" && ViewModel.Repliki.Wybrana != null)
            PokazKarte(new KartaRepliki { DataContext = ViewModel.Repliki.Wybrana });
    }

    /// <summary>Odświeża kartę po prawej po zmianie wyboru (FR-204, FR-302).</summary>
    public void OdswiezKarte()
    {
        if (ViewModel == null)
            return;
        if (ViewModel.AktualnyEkran == "Wyposazenie" && ViewModel.Wyposazenie.Wybrany != null)
            PokazKarte(new KartaPrzedmiotu { DataContext = ViewModel.Wyposazenie.Wybrany });
        else if (ViewModel.AktualnyEkran == "Bronie" && ViewModel.Repliki.Wybrana != null)
            PokazKarte(new KartaRepliki { DataContext = ViewModel.Repliki.Wybrana });
    }

    private void ZaznaczNawigacje(string ekran)
    {
        foreach (var pozycja in WszystkiePozycjeNawigacji())
            if (pozycja is NavigationViewItem item && item.Tag as string == ekran)
            {
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

    private void Nawigacja_SelectionChanged(NavigationView sender, RoutedEventArgs args)
    {
        if (sender.SelectedItem is not NavigationViewItem item)
            return;
        var ekran = item.Tag as string;
        if (ekran == null || ViewModel == null)
            return;
        PokazEkran(ekran);
        ViewModel.AktualnyEkran = ekran;
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

    private void ZamknijPanelKarty_Click(object sender, RoutedEventArgs e)
    {
        PanelKarty.Visibility = Visibility.Collapsed;
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
        ZawartoscKarty.Content = zawartosc;
        PanelKarty.Visibility = Visibility.Visible;
    }

    protected override void OnClosed(EventArgs e)
    {
        var u = App.Dane.Ustawienia;
        u.SzerokoscOkna = Width;
        u.WysokoscOkna = Height;
        App.Magazyn.Zapisz(App.Dane);
        base.OnClosed(e);
    }
}
