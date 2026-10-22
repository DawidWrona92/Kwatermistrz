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

    private void Domyslny_Checked(object sender, RoutedEventArgs e)
    {
        // RadioButton "Domyślny": DataContext = WariantSlotu, przodek DataTemplate = SlotZestawu
        if (sender is RadioButton { DataContext: Kwatermistrz.Core.Model.WariantSlotu wariant }
            && sender is System.Windows.DependencyObject dep
            && FindAncestorSlot(dep) is { } slot
            && DataContext is ViewModels.ZestawyViewModel vm)
            vm.UstawDomyslnyCommand.Execute(new object[] { slot, wariant });
    }

    private static Kwatermistrz.Core.Model.SlotZestawu? FindAncestorSlot(System.Windows.DependencyObject element)
    {
        // wzorzec DataTemplate: szukamy ramki z DataContext typu SlotZestawu
        var aktualny = System.Windows.Media.VisualTreeHelper.GetParent(element);
        while (aktualny != null)
        {
            if (aktualny is System.Windows.FrameworkElement fe && fe.DataContext is Kwatermistrz.Core.Model.SlotZestawu slot)
                return slot;
            aktualny = System.Windows.Media.VisualTreeHelper.GetParent(aktualny);
        }
        return null;
    }

    private void Spakowany_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox { DataContext: ViewModels.ZestawyViewModel.WierszPakowania wiersz }
            && DataContext is ViewModels.ZestawyViewModel vm)
            vm.PrzelaczSpakowanyCommand.Execute(wiersz);
    }
}
