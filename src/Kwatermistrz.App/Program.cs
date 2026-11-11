using System.IO;
using System.Windows;

namespace Kwatermistrz.App;

/// <summary>Osłona startu: także błędy parsowania App.xaml są logowane.</summary>
internal static class Program
{
    [STAThread]
    private static int Main()
    {
        try
        {
            var app = new App();
            app.InitializeComponent();
            return app.Run();
        }
        catch (Exception ex)
        {
            string? sciezkaLogu = null;
            try
            {
                var katalog = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kwatermistrz");
                Directory.CreateDirectory(katalog);
                sciezkaLogu = Path.Combine(katalog, "error.log");
                File.AppendAllText(sciezkaLogu,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Błąd startu:{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
            }
            catch
            {
                // Nawet brak uprawnień do katalogu danych nie może ukryć wyjątku.
            }
            try
            {
                MessageBox.Show(
                    "Nie udało się uruchomić Kwatermistrza. " +
                    (sciezkaLogu == null ? ex.ToString() : $"Szczegóły: {sciezkaLogu}"),
                    "Kwatermistrz — błąd uruchamiania", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
                // Brak pulpitu graficznego w CI: zwróć niezerowy kod wyjścia.
            }
            return 1;
        }
    }
}
