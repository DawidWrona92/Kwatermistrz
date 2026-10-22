using System.Windows;
using System.Windows.Controls;
using Kwatermistrz.App.ViewModels;

namespace Kwatermistrz.App.Views;

/// <summary>Ekran pakietów konfiguracji (FR-1404): eksport i import konfiguracji między instalacjami.</summary>
public partial class PakietyPage : Page
{
    public PakietyPage()
    {
        InitializeComponent();
    }

    private void Decyzja_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox combo || combo.DataContext is not WierszDecyzji wiersz)
            return;
        if (DataContext is PakietyViewModel vm)
            vm.ZmienDecyzjeCommand.Execute(wiersz);
    }
}
