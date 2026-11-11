using System.IO;
using System.Windows;
using Kwatermistrz.App.Services;
using Kwatermistrz.App.ViewModels;
using Kwatermistrz.App.Views;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Storage;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Kwatermistrz.App;

/// <summary>Aplikacja WPF: rejestracja usług, motyw, start okna głównego lub kreatora (FR-1401).</summary>
public partial class App : Application
{
    private const string NazwaMutexa = "Kwatermistrz.SingleInstance";
    private static System.Threading.Mutex? _mutex;
    private static bool _posiadamyMutex;
    private bool _wyjscieAwaryjne;
    private ServiceProvider? _provider;
    private System.Windows.Threading.DispatcherTimer? _timerZapisu;

    /// <summary>Obserwowalna paleta Foreground standardowych widoków WPF.</summary>
    public static KoloryInterfejsu Kolory { get; } = new();

    /// <summary>Aktualne dane aplikacji (wstrzykiwane do widoków).</summary>
    public static DaneAplikacji Dane { get; private set; } = null!;

    /// <summary>Magazyn danych (zapis/odczyt, kopie).</summary>
    public static MagazynJson Magazyn { get; private set; } = null!;

    /// <summary>Katalog danych aplikacji (%AppData%\Kwatermistrz, decyzja 02).</summary>
    public static string KatalogDanych { get; private set; } = "";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var testUruchomienia = e.Args.Contains("--smoke-test", StringComparer.OrdinalIgnoreCase);

        // jedna instancja na użytkownika — dwie instancje nadpisywałyby sobie dane (NFR-05)
        _mutex = new System.Threading.Mutex(true, NazwaMutexa, out var czyNowa);
        _posiadamyMutex = czyNowa;
        if (!czyNowa)
        {
            System.Windows.MessageBox.Show(
                "Kwatermistrz jest już uruchomiony.", "Kwatermistrz",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // Wyjątki na wątku UI są krytyczne. Nie kontynuujemy z niekompletnym
        // drzewem wizualnym ani nie nadpisujemy danych po awarii.
        DispatcherUnhandledException += (_, args) =>
        {
            _wyjscieAwaryjne = true;
            var sciezkaLogu = ZapiszLogWyjatku(args.Exception);
            args.Handled = true;
            if (!testUruchomienia)
                System.Windows.MessageBox.Show(
                    $"Kwatermistrz napotkał błąd i zostanie zamknięty.\nSzczegóły: {sciezkaLogu}",
                    "Kwatermistrz", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            Shutdown(1);
        };

        KatalogDanych = testUruchomienia
            ? Path.Combine(Path.GetTempPath(), "Kwatermistrz-smoke-" + Guid.NewGuid().ToString("N"))
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kwatermistrz");
        Directory.CreateDirectory(KatalogDanych);
        Magazyn = new MagazynJson(KatalogDanych);
        var (dane, kontrola) = Magazyn.WczytajZKontrola();
        Dane = dane;

        var services = new ServiceCollection();
        services.AddSingleton(dane);
        services.AddSingleton(Magazyn);
        services.AddSingleton<KalkulatorSerwis>();
        var serwisZdjec = new SerwisZdjec(Magazyn);
        serwisZdjec.UstawDane(dane); // NFR-11: limity rozmiaru zdjęć z ustawień
        services.AddSingleton(serwisZdjec);
        services.AddSingleton<StanAplikacji>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<PulpitViewModel>();
        services.AddSingleton<SlownikiViewModel>();
        services.AddSingleton<RekonstrukcjeViewModel>();
        services.AddSingleton<UstawieniaViewModel>();
        services.AddSingleton<ImportViewModel>();
        services.AddSingleton<PakietyViewModel>();
        services.AddSingleton<WyposazenieViewModel>();
        services.AddSingleton<KategorieViewModel>();
        services.AddSingleton<ReplikiViewModel>();
        services.AddSingleton<ZestawyViewModel>();
        services.AddSingleton<WyprawyViewModel>();
        services.AddSingleton<RaportyViewModel>();
        services.AddSingleton<DoUzupelnieniaViewModel>();
        services.AddSingleton<KonfliktyViewModel>();
        services.AddSingleton<SzukanieViewModel>();
        _provider = services.BuildServiceProvider();

        // motyw z ustawień (FR-1006); „Systemowy” → aktualny motyw systemu, nie zawsze ciemny
        var motyw = dane.Ustawienia.Motyw switch
        {
            Motyw.Jasny => ApplicationTheme.Light,
            Motyw.Ciemny => ApplicationTheme.Dark,
            _ => ApplicationThemeManager.GetSystemTheme() == SystemTheme.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light,
        };
        ApplicationThemeManager.Apply(motyw);
        // aliasy kolorów wykresów (FR-909) ustawiane również przy starcie — wg faktycznego motywu
        UstawAliasyWykresow(motyw == ApplicationTheme.Light);

        // zapis odroczony (debounce): rutynowe edycje odkładają zapis znacznikiem „brudne”,
        // a ten timer na wątku UI spłukuje go co 2 s — jedna serializacja zamiast jednej na edycję
        _timerZapisu = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2),
        };
        _timerZapisu.Tick += (_, _) => Magazyn.ZapiszJezeliBrudne(Dane);
        _timerZapisu.Start();

        var mainWindow = new MainWindow
        {
            DataContext = _provider.GetRequiredService<MainWindowViewModel>(),
        };
        if (testUruchomienia)
        {
            // Windows CI: parsuj każdy ekran i karty bez pokazywania okna.
            // Awaria któregokolwiek widoku kończy proces kodem != 0.
            // Odpalamy prawdziwy lifecycle WPF, a nie tylko parsowanie konstruktorów.
            mainWindow.Show();
            mainWindow.ZweryfikujEkrany();
            mainWindow.ZweryfikujNawigacje();
            mainWindow.ZweryfikujKreator();
            if (e.Args.Length >= 2 && !string.IsNullOrWhiteSpace(e.Args[1]))
                mainWindow.ZweryfikujImportPoStarcie(e.Args[1]);
            mainWindow.ZweryfikujTabeleIPasekTytulu();
            mainWindow.ZweryfikujProporcjonalneKolumny();
            if (e.Args.Length >= 2 && !string.IsNullOrWhiteSpace(e.Args[1]))
            {
                mainWindow.ZweryfikujGalerieZestawu();
                mainWindow.ZweryfikujPierwszyRenderWyposazenia();
            }
            mainWindow.ZweryfikujJednoZaznaczenieMenu();
            mainWindow.ZweryfikujKoloryPulpitu();
            mainWindow.Close();
            Shutdown();
            return;
        }
        mainWindow.Show();

        if (dane.CzyPierwszeUruchomienie)
        {
            var kreator = new KreatorWindow
            {
                Owner = mainWindow,
                DataContext = new KreatorViewModel(dane, Magazyn),
            };
            if (kreator.ShowDialog() == true)
            {
                // MainWindow została pokazana wcześniej: jej Pulpit i filtry mają
                // migawki sprzed importu. Odświeżamy je po zakończeniu kreatora.
                if (mainWindow.DataContext is MainWindowViewModel widok)
                    widok.OdswiezWszystkieEkrany();
            }
        }

        if (!kontrola.CzyPoprawne && kontrola.Komunikat != null)
            System.Windows.MessageBox.Show(kontrola.Komunikat, "Kwatermistrz", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
    }

    /// <summary>Ustawia aliasy kolorów wykresów (FR-909) wg motywu — ta sama paleta co w UstawieniaViewModel.ZastosujMotyw.</summary>
    internal static void UstawAliasyWykresow(bool jasny)
    {
        Kolory.UstawMotyw(jasny);
        System.Windows.Media.Color Kolor(byte r, byte g, byte b) => System.Windows.Media.Color.FromRgb(r, g, b);
        // WPF-UI może utrzymywać stary pędzel TextFillColorPrimaryBrush
        // w już utworzonych stronach Frame. DynamicResource do własnego
        // klucza, podmienianego jawnie, odświeża kolory istniejących widoków.
        // Zmiana ResourceDictionary[key] nie odświeża już rozwiązanego Foreground
        // w starszych elementach WPF. Mutujemy ten SAM Brush: Freezable zgłasza
        // zmianę i aktualizuje każdy TextBlock bez ponownego budowania ekranu.
        void UstawKolorTekstu(string klucz, System.Windows.Media.Color kolor)
        {
            // Styles są w oddzielnym MergedDictionary i mogą zachować
            // referencję do pędzla z Paleta.xaml, mimo późniejszego dodania
            // klucza o tej samej nazwie do Current.Resources.
            foreach (var slownik in Current.Resources.MergedDictionaries)
            {
                if (slownik.Contains(klucz) &&
                    slownik[klucz] is System.Windows.Media.SolidColorBrush lokalny && !lokalny.IsFrozen)
                    lokalny.Color = kolor;
            }
            if (Current.TryFindResource(klucz) is System.Windows.Media.SolidColorBrush pedzel && !pedzel.IsFrozen)
            {
                pedzel.Color = kolor;
                return;
            }
            Current.Resources[klucz] = new System.Windows.Media.SolidColorBrush(kolor);
        }
        UstawKolorTekstu("TekstAplikacji",
            jasny ? Kolor(0x22, 0x27, 0x25) : Kolor(0xF0, 0xF2, 0xF0));
        UstawKolorTekstu("TekstAplikacjiDrugorzedny",
            jasny ? Kolor(0x59, 0x62, 0x5E) : Kolor(0xB8, 0xBF, 0xBB));
        UstawKolorTekstu("LiniaTabeli",
            jasny ? Kolor(0xB3, 0xBD, 0xB8) : Kolor(0x59, 0x65, 0x60));
        UstawKolorTekstu("WierszTabeliNaprzemienny",
            jasny ? Kolor(0xEE, 0xF0, 0xED) : Kolor(0x29, 0x2C, 0x2B));
        if (jasny)
        {
            Current.Resources["Stal"] = new System.Windows.Media.SolidColorBrush(Kolor(0x2F, 0x4A, 0x6B));
            Current.Resources["Linia"] = new System.Windows.Media.SolidColorBrush(Kolor(0xCD, 0xD0, 0xCA));
            Current.Resources["Sygnal"] = new System.Windows.Media.SolidColorBrush(Kolor(0xA8, 0x3A, 0x2B));
            Current.Resources["Poprawny"] = new System.Windows.Media.SolidColorBrush(Kolor(0x4A, 0x7A, 0x52));
            Current.Resources["Uwaga"] = new System.Windows.Media.SolidColorBrush(Kolor(0x9A, 0x6B, 0x00));
        }
        else
        {
            Current.Resources["Stal"] = new System.Windows.Media.SolidColorBrush(Kolor(0x8D, 0xB0, 0xD8));
            Current.Resources["Linia"] = new System.Windows.Media.SolidColorBrush(Kolor(0x3A, 0x40, 0x3D));
            Current.Resources["Sygnal"] = new System.Windows.Media.SolidColorBrush(Kolor(0xE0, 0x8A, 0x7A));
            Current.Resources["Poprawny"] = new System.Windows.Media.SolidColorBrush(Kolor(0x8C, 0xC4, 0x95));
            Current.Resources["Uwaga"] = new System.Windows.Media.SolidColorBrush(Kolor(0xE0, 0xB3, 0x4A));
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // spłukujemy odłożony debounce'em zapis — wyjście z aplikacji nie może gubić edycji
        // (Magazyn/Dane null przy wczesnym wyjściu, np. druga instancja)
        if (!_wyjscieAwaryjne && Magazyn != null && Dane != null)
            Magazyn.ZapiszJezeliBrudne(Dane);
        _timerZapisu?.Stop();
        _provider?.Dispose();
        if (_posiadamyMutex)
            _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        _mutex = null;
        _posiadamyMutex = false;
        base.OnExit(e);
    }

    /// <summary>Zapisuje szczegóły wyjątku do error.log w katalogu danych, a gdy niemożliwe — obok pliku exe. Zwraca ścieżkę zapisanego logu (do komunikatu).</summary>
    private static string ZapiszLogWyjatku(Exception ex)
    {
        var wpis =
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}{Environment.NewLine}{Environment.NewLine}";
        try
        {
            var sciezka = Path.Combine(KatalogDanych, "error.log");
            if (!string.IsNullOrEmpty(KatalogDanych))
            {
                File.AppendAllText(sciezka, wpis);
                return sciezka;
            }
        }
        catch
        {
            // logowanie nie może być przyczyną kolejnego wyjątku — próbujemy obok exe
        }
        try
        {
            var sciezka = Path.Combine(AppContext.BaseDirectory, "error.log");
            File.AppendAllText(sciezka, wpis);
            return sciezka;
        }
        catch
        {
            // brak możliwości zapisu logu — komunikat pokaże ścieżkę, którą próbowano zapisać
            return Path.Combine(AppContext.BaseDirectory, "error.log");
        }
    }
}
