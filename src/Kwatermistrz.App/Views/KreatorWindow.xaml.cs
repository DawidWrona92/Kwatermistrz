using System.Windows;
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
}
