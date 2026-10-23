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
    private ServiceProvider? _provider;

    /// <summary>Aktualne dane aplikacji (wstrzykiwane do widoków).</summary>
    public static DaneAplikacji Dane { get; private set; } = null!;

    /// <summary>Magazyn danych (zapis/odczyt, kopie).</summary>
    public static MagazynJson Magazyn { get; private set; } = null!;

    /// <summary>Katalog danych aplikacji (%AppData%\Kwatermistrz, decyzja 02).</summary>
    public static string KatalogDanych { get; private set; } = "";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // jedna instancja na użytkownika — dwie instancje nadpisywałyby sobie dane (NFR-05)
        _mutex = new System.Threading.Mutex(true, NazwaMutexa, out var czyNowa);
        if (!czyNowa)
        {
            System.Windows.MessageBox.Show(
                "Kwatermistrz jest już uruchomiony.", "Kwatermistrz",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // globalna obsługa wyjątków: log + komunikat zamiast cichego zamknięcia (NFR-05)
        DispatcherUnhandledException += (_, args) =>
        {
            args.Handled = true;
            ZapiszLogWyjatku(args.Exception);
            System.Windows.MessageBox.Show(
                "Wystąpił nieoczekiwany błąd. Szczegóły zapisano w pliku error.log w katalogu danych.",
                "Kwatermistrz", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        };

        KatalogDanych = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kwatermistrz");
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

        var mainWindow = new MainWindow
        {
            DataContext = _provider.GetRequiredService<MainWindowViewModel>(),
        };
        mainWindow.Show();

        if (dane.CzyPierwszeUruchomienie)
        {
            var kreator = new KreatorWindow
            {
                Owner = mainWindow,
                DataContext = new KreatorViewModel(dane, Magazyn),
            };
            kreator.ShowDialog();
        }

        if (!kontrola.CzyPoprawne && kontrola.Komunikat != null)
            System.Windows.MessageBox.Show(kontrola.Komunikat, "Kwatermistrz", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
    }

    /// <summary>Ustawia aliasy kolorów wykresów (FR-909) wg motywu — ta sama paleta co w UstawieniaViewModel.ZastosujMotyw.</summary>
    internal static void UstawAliasyWykresow(bool jasny)
    {
        System.Windows.Media.Color Kolor(byte r, byte g, byte b) => System.Windows.Media.Color.FromRgb(r, g, b);
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
        _provider?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        _mutex = null;
        base.OnExit(e);
    }

    private static void ZapiszLogWyjatku(Exception ex)
    {
        try
        {
            var sciezka = Path.Combine(KatalogDanych, "error.log");
            File.AppendAllText(sciezka,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().FullName}: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // logowanie nie może być przyczyną kolejnego wyjątku
        }
    }
}
