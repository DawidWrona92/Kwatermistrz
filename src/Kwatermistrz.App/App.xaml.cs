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
    private System.Windows.Threading.DispatcherTimer? _timerZapisu;

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
            var sciezkaLogu = ZapiszLogWyjatku(args.Exception);
            // lawina wyjątków (np. uszkodzony binding w pętli renderowania) zamiast zalewać użytkownika oknami kończy działanie
            _licznikNieobsluzonych++;
            if (_licznikNieobsluzonych >= 5)
            {
                // seria wyjątków — zapisujemy odłożone zmiany i robimy kopię zapasową przed zamknięciem, żeby nie stracić edycji
                var kopia = "";
                try
                {
                    if (Magazyn != null && Dane != null)
                    {
                        Magazyn.ZapiszJezeliBrudne(Dane);
                        kopia = Magazyn.UtworzKopieZapasowa(Dane);
                    }
                }
                catch
                {
                    // kopia po awarii jest best-effort — nie może przeszkodzić zamknięciu
                }
                var informacjeOkopii = kopia != "" ? $"\n\nKopia zapasowa danych: {kopia}" : "";
                System.Windows.MessageBox.Show(
                    $"Wystąpiła seria nieoczekiwanych błędów ({_licznikNieobsluzonych}). Aby nie uszkodzić danych, aplikacja zostanie zamknięta.{informacjeOkopii}\n\nSzczegóły zapisano w pliku:\n{sciezkaLogu}",
                    "Kwatermistrz", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                Shutdown();
                return;
            }
            System.Windows.MessageBox.Show(
                $"Wystąpił nieoczekiwany błąd. Szczegóły zapisano w pliku:\n{sciezkaLogu}",
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
        // spłukujemy odłożony debounce'em zapis — wyjście z aplikacji nie może gubić edycji
        // (Magazyn/Dane null przy wczesnym wyjściu, np. druga instancja)
        if (Magazyn != null && Dane != null)
            Magazyn.ZapiszJezeliBrudne(Dane);
        _timerZapisu?.Stop();
        _provider?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        _mutex = null;
        base.OnExit(e);
    }

    /// <summary>Liczba nieobsłużonych wyjątków od startu — po serii zamykamy aplikację, żeby nie zalać użytkownika oknami błędu.</summary>
    private int _licznikNieobsluzonych;

    /// <summary>Zapisuje szczegóły wyjątku do error.log w katalogu danych, a gdy niemożliwe — obok pliku exe. Zwraca ścieżkę zapisanego logu (do komunikatu).</summary>
    private static string ZapiszLogWyjatku(Exception ex)
    {
        var wpis =
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().FullName}: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}{Environment.NewLine}";
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
