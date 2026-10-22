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
        _strony["Wyprawy"] = new PlaceholderPage("Wyprawy", "Etap 3");
        _strony["Bronie"] = new PlaceholderPage("Broń i repliki", "Etap 2");
        _strony["Wyposazenie"] = new WyposazeniePage { DataContext = vm.Wyposazenie };
        _strony["Zestawy"] = new PlaceholderPage("Zestawy", "Etap 3");
        _strony["Raporty"] = new PlaceholderPage("Raporty", "Etap 4");
        _strony["DoUzupelnienia"] = new PlaceholderPage("Do uzupełnienia", "Etap 4");
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
        // karta po prawej pokazuje wybrany przedmiot (FR-204, §10.4)
        if (ekran == "Wyposazenie" && ViewModel?.Wyposazenie.Wybrany != null)
            PokazKarte(new KartaPrzedmiotu { DataContext = ViewModel.Wyposazenie.Wybrany });
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
    }

    private void ZamknijPanelKarty_Click(object sender, RoutedEventArgs e)
    {
        PanelKarty.Visibility = Visibility.Collapsed;
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
