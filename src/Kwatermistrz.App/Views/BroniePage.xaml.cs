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
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(vm.Wybrana) &&
                        System.Windows.Window.GetWindow(this) is MainWindow okno)
                        okno.OdswiezKarte();
                };
            }
        };
    }
}
