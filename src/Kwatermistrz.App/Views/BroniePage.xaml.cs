using System.Windows.Controls;

namespace Kwatermistrz.App.Views;

/// <summary>Ekran „Broń” (FR-301): lista replik z filtrami i sortowaniem wg wypraw.</summary>
public partial class BroniePage : Page
{
    private ViewModels.ReplikiViewModel? _obserwowanyViewModel;
    public BroniePage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is ViewModels.ReplikiViewModel vm)
            {
                vm.Odswiez();
                if (!ReferenceEquals(_obserwowanyViewModel, vm))
                {
                    if (_obserwowanyViewModel != null)
                        _obserwowanyViewModel.PropertyChanged -= ViewModel_PropertyChanged;
                    _obserwowanyViewModel = vm;
                    vm.PropertyChanged += ViewModel_PropertyChanged;
                }
            }
        };
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModels.ReplikiViewModel.Wybrana) &&
            System.Windows.Window.GetWindow(this) is MainWindow okno)
            okno.OdswiezKarte();
    }
}
