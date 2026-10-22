using System.Windows;
using System.Windows.Controls;
using Kwatermistrz.App.ViewModels;

namespace Kwatermistrz.App.Views;

/// <summary>Ekran startowy: liczby ogólne (FR-101), karty skrótowe (FR-104), ranking (FR-105), mozaika wypraw (FR-102).</summary>
public partial class PulpitPage : Page
{
    public PulpitPage()
    {
        InitializeComponent();
        Loaded += (_, _) => (DataContext as PulpitViewModel)?.Odswiez();
    }

    private void KartaSkrotowa_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is not PulpitViewModel vm || sender is not FrameworkElement element)
            return;
        switch (element.Tag as string)
        {
            case "awaria": vm.OtworzAwariaCommand.Execute(null); break;
            case "zadania": vm.OtworzZadaniaCommand.Execute(null); break;
            case "bezTestu": vm.OtworzBezTestuCommand.Execute(null); break;
            case "bezUzycia": vm.OtworzBezUzyciaCommand.Execute(null); break;
            case "polaNieznane": vm.OtworzPolaNieznaneCommand.Execute(null); break;
        }
    }

    private void Ranking_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not PulpitViewModel vm || sender is not FrameworkElement element)
            return;
        var id = element.Tag as string;
        if (id != null)
            vm.OtworzRankingCommand.Execute(vm.Ranking.FirstOrDefault(w => w.ReplikaId == id));
    }
}
