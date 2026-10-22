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
        services.AddSingleton<StanAplikacji>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<PulpitViewModel>();
        services.AddSingleton<SlownikiViewModel>();
        services.AddSingleton<RekonstrukcjeViewModel>();
        services.AddSingleton<UstawieniaViewModel>();
        services.AddSingleton<ImportViewModel>();
        services.AddSingleton<WyposazenieViewModel>();
        services.AddSingleton<KategorieViewModel>();
        services.AddSingleton<ReplikiViewModel>();
        services.AddSingleton<ZestawyViewModel>();
        services.AddSingleton<WyprawyViewModel>();
        _provider = services.BuildServiceProvider();

        // motyw z ustawień (FR-1006)
        var motyw = dane.Ustawienia.Motyw switch
        {
            Motyw.Jasny => ApplicationTheme.Light,
            Motyw.Ciemny => ApplicationTheme.Dark,
            _ => ApplicationThemeManager.GetSystemTheme() == SystemTheme.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light,
        };
        ApplicationThemeManager.Apply(motyw);

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

        if (!kontrola.CzyPoprawne && kontrola.komunikat != null)
            System.Windows.MessageBox.Show(kontrola.komunikat, "Kwatermistrz", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _provider?.Dispose();
        base.OnExit(e);
    }
}
