using System.Windows.Controls;

namespace Kwatermistrz.App.Views;

/// <summary>Ekran zestawów (FR-701–705, FR-709): lista zestawów, karta z edytorem slotów i wariantów.</summary>
public partial class ZestawyPage : Page
{
    public ZestawyPage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is ViewModels.ZestawyViewModel vm)
                vm.Odswiez();
        };
    }
}
