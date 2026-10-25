using System.Windows;
using System.Windows.Controls;
using Kwatermistrz.App.ViewModels;

namespace Kwatermistrz.App.Views;

/// <summary>Kreator pierwszego uruchomienia (FR-1401). W tym etapie tylko wariant „pusta aplikacja”.</summary>
public partial class KreatorWindow : Window
{
    public KreatorWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is KreatorViewModel vm)
                vm.Zakonczono += (_, _) =>
                {
                    DialogResult = true;
                    Close();
                };
        };
    }

    /// <summary>Zapisuje wybór punktu startu (radio „Start”; IsChecked jest jednokierunkowe przez MultiBinding, więc wybór trafia do ViewModelu tutaj).</summary>
    private void Start_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { DataContext: ValueTuple<string, string> start }
            && DataContext is KreatorViewModel vm)
            vm.WybranyStart = start.Item1;
    }
}
