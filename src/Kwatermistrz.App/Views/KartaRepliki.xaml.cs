using System.Windows;
using System.Windows.Controls;
using Kwatermistrz.App.ViewModels;

namespace Kwatermistrz.App.Views;

/// <summary>Karta repliki: oddzielny model wiersza (DataContext) i ViewModel akcji/zakładek.</summary>
public partial class KartaRepliki : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel), typeof(ReplikiViewModel), typeof(KartaRepliki));

    public ReplikiViewModel? ViewModel
    {
        get => (ReplikiViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public KartaRepliki()
    {
        InitializeComponent();
        Galeria.Zmieniono += (_, _) => ViewModel?.ZapiszCicho();
    }

    private void Zakladka_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string zakladka } && ViewModel != null)
            ViewModel.Zakladka = zakladka;
    }
}
