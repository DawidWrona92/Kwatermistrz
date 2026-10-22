using System.Windows;
using System.Windows.Controls;

namespace Kwatermistrz.App.Views;

/// <summary>Ekran zestawów (FR-701–705, FR-709): lista zestawów, karta z edytorem slotów i wariantów, lista pakowania (FR-707).</summary>
public partial class ZestawyPage : Page
{
    public ZestawyPage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is ViewModels.ZestawyViewModel vm)
            {
                vm.Odswiez();
                vm.OdswiezPakowanie();
            }
        };
    }

    private void Spakowany_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox { DataContext: ViewModels.ZestawyViewModel.WierszPakowania wiersz }
            && DataContext is ViewModels.ZestawyViewModel vm)
            vm.PrzelaczSpakowanyCommand.Execute(wiersz);
    }
}
