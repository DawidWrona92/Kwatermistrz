using System.Windows.Controls;
using Kwatermistrz.Core.Model;

namespace Kwatermistrz.App.Views;

/// <summary>Ekran wypraw (FR-801–809): szybkie dodawanie, lista z filtrami, edycja/usuwanie.</summary>
public partial class WyprawyPage : Page
{
    public WyprawyPage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is ViewModels.WyprawyViewModel vm)
                vm.Odswiez();
        };

    }

    private void Zestaw_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ViewModels.WyprawyViewModel vm &&
            sender is ComboBox combo &&
            combo.SelectedItem is Zestaw zestaw)
            vm.UstawZestaw(zestaw);
    }
}
