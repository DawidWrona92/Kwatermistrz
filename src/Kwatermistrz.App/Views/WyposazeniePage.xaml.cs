using System.Windows.Controls;

namespace Kwatermistrz.App.Views;

/// <summary>Ekran wyposażenia (FR-201–207, FR-209, FR-216): kategorie, lista przedmiotów, edycja zbiorcza.</summary>
public partial class WyposazeniePage : Page
{
    public WyposazeniePage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is ViewModels.WyposazenieViewModel vm)
                vm.Odswiez();
        };
    }
}
