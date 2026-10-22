using System.Windows.Controls;

namespace Kwatermistrz.App.Views;

/// <summary>Ekran raportów (FR-901–904, FR-908, FR-910, FR-911): wybór raportu, tabele, eksport CSV, kontrola spójności.</summary>
public partial class RaportyPage : Page
{
    public RaportyPage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is ViewModels.RaportyViewModel vm)
                vm.Odswiez();
        };
    }
}
