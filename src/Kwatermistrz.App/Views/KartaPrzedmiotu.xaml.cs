using System.Windows;
using System.Windows.Controls;
using Kwatermistrz.App.ViewModels;

namespace Kwatermistrz.App.Views;

/// <summary>Karta przedmiotu; dane edytowanego wiersza są w DataContext, a akcje w ViewModel.</summary>
public partial class KartaPrzedmiotu : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel), typeof(WyposazenieViewModel), typeof(KartaPrzedmiotu));

    public WyposazenieViewModel? ViewModel
    {
        get => (WyposazenieViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public KartaPrzedmiotu()
    {
        InitializeComponent();
        // Podpinamy raz, nie przy każdym ponownym Loaded.
        Galeria.Zmieniono += (_, _) => ViewModel?.ZapiszCicho();
    }
}
