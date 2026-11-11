using System.Windows.Controls;

namespace Kwatermistrz.App.Views;

/// <summary>Ekran „Broń” (FR-301): lista replik z filtrami i sortowaniem wg wypraw.</summary>
public partial class BroniePage : Page
{
    public BroniePage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is ViewModels.ReplikiViewModel vm)
            {
                vm.Odswiez();
                vm.PropertyChanged -= WyborZmieniony;
                vm.PropertyChanged += WyborZmieniony;
            }
        };
        Unloaded += (_, _) =>
        {
            if (DataContext is ViewModels.ReplikiViewModel vm)
                vm.PropertyChanged -= WyborZmieniony;
        };
    }
    private void WyborZmieniony(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModels.ReplikiViewModel.Wybrana) &&
            System.Windows.Window.GetWindow(this) is MainWindow okno)
            okno.OdswiezKarte();
    }
}
