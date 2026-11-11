using System.Windows.Controls;

namespace Kwatermistrz.App.Views;

/// <summary>Konflikty odczytujemy na nowo przy wejściu, aby nigdy nie korzystać z nieaktualnych decyzji.</summary>
public partial class KonfliktyPage : Page
{
    public KonfliktyPage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is ViewModels.KonfliktyViewModel vm)
                vm.Odswiez();
        };
    }
}