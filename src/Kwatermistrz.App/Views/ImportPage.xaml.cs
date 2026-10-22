using System.Windows.Controls;

namespace Kwatermistrz.App.Views;

/// <summary>Ekran importu (FR-1101, FR-1102, FR-1403): wybór pliku, podgląd rekordów i konfliktów, zatwierdzenie; import ogólny z mapowaniem kolumn.</summary>
public partial class ImportPage : Page
{
    public ImportPage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            // magazyn mapowań kolumn (FR-1403) — inicjalizacja po ustawieniu DataContext
            if (DataContext is Kwatermistrz.App.ViewModels.ImportViewModel vm)
                vm.InicjalizujMapowania();
        };
    }
}
