using System.Windows.Controls;

namespace Kwatermistrz.App.Views;

/// <summary>Ekran wyposażenia (FR-201–207, FR-209, FR-216): kategorie, lista przedmiotów, edycja zbiorcza.</summary>
public partial class WyposazeniePage : Page
{
    private ViewModels.WyposazenieViewModel? _obserwowanyViewModel;
    public WyposazeniePage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is ViewModels.WyposazenieViewModel vm)
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
        if (e.PropertyName == nameof(ViewModels.WyposazenieViewModel.Wybrany) &&
            System.Windows.Window.GetWindow(this) is MainWindow okno)
            okno.OdswiezKarte();
    }
}
