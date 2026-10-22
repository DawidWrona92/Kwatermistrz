using System.Windows.Controls;

namespace Kwatermistrz.App.Views;

/// <summary>Widok „Do uzupełnienia” (FR-1204): jedna lista rzeczy wymagających decyzji lub danych.</summary>
public partial class DoUzupelnieniaPage : Page
{
    public DoUzupelnieniaPage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is ViewModels.DoUzupelnieniaViewModel vm)
                vm.Odswiez();
        };
    }
}
