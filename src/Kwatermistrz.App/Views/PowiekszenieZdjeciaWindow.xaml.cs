using System.Windows;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

namespace Kwatermistrz.App.Views;

/// <summary>Okno powiększenia zdjęcia (FR-211): obraz dopasowany do okna, zamykane przyciskiem lub Escape.</summary>
public partial class PowiekszenieZdjeciaWindow : FluentWindow
{
    public PowiekszenieZdjeciaWindow()
    {
        InitializeComponent();
    }

    /// <summary>Ustawia obraz do pokazania.</summary>
    public void UstawObraz(BitmapImage obraz, string? etykieta)
    {
        Obraz.Source = obraz;
        Etykieta.Text = etykieta ?? "";
        Title = string.IsNullOrEmpty(etykieta) ? "Zdjęcie" : etykieta;
    }

    private void Zamknij_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }
}
