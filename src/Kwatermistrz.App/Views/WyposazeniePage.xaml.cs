using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Kwatermistrz.App.ViewModels;

namespace Kwatermistrz.App.Views;

/// <summary>Ekran wyposażenia (FR-201–207, FR-209, FR-216): kategorie, lista przedmiotów, edycja zbiorcza.</summary>
public partial class WyposazeniePage : Page
{
    public WyposazeniePage()
    {
        InitializeComponent();
        // WPF potrzebuje istniejącego menu już przy pierwszym kliknięciu PPM.
        PrzedmiotyTabela.ContextMenu = new ContextMenu();
        Loaded += (_, _) =>
        {
            if (DataContext is ViewModels.WyposazenieViewModel vm)
            {
                vm.Odswiez();
                vm.PropertyChanged -= WyborZmieniony;
                vm.PropertyChanged += WyborZmieniony;
            }
        };
        Unloaded += (_, _) =>
        {
            if (DataContext is ViewModels.WyposazenieViewModel vm)
                vm.PropertyChanged -= WyborZmieniony;
        };
    }
    private void WyborZmieniony(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModels.WyposazenieViewModel.Wybrany) &&
            System.Windows.Window.GetWindow(this) is MainWindow okno)
            okno.OdswiezKarte();
    }
    private void PrzedmiotyTabela_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        var zrodlo = e.OriginalSource as DependencyObject;
        var wiersz = zrodlo == null ? null :
            ItemsControl.ContainerFromElement(PrzedmiotyTabela, zrodlo) as DataGridRow;
        var przedmiot = wiersz?.Item as WierszPrzedmiotu;

        // PPM nie zmienia zaznaczenia checkboxów ani SelectedItem.
        // Kontekst jednego rekordu jest niezależny od operacji na grupie.
        var menu = PrzedmiotyTabela.ContextMenu ?? new ContextMenu();
        menu.Items.Clear();
        WypelnijMenu(menu, przedmiot);
        PrzedmiotyTabela.ContextMenu = menu;
    }

    /// <summary>Tworzy menu do testów i dla obsługi prawego przycisku myszy.</summary>
    public ContextMenu ZbudujMenuKontekstowe(WierszPrzedmiotu? przedmiot)
    {
        var menu = new ContextMenu();
        WypelnijMenu(menu, przedmiot);
        return menu;
    }

    private void WypelnijMenu(ContextMenu menu, WierszPrzedmiotu? przedmiot)
    {
        if (DataContext is not WyposazenieViewModel vm)
            return;

        void Element(string nazwa, ICommand komenda, object? parametr = null)
            => menu.Items.Add(new MenuItem
            {
                Header = nazwa,
                Command = komenda,
                CommandParameter = parametr,
            });

        if (przedmiot != null)
        {
            Element("Otwórz kartę tego przedmiotu", vm.WybierzCommand, przedmiot);
            Element("Duplikuj ten przedmiot", vm.DuplikujWierszCommand, przedmiot);
            Element("Usuń ten przedmiot…", vm.UsunCommand, przedmiot);
            menu.Items.Add(new Separator());
        }

        if (vm.WybranaKategoria != null)
            Element($"Dodaj przedmiot — {vm.WybranaKategoria.Nazwa}",
                vm.DodajWKategoriiCommand, vm.WybranaKategoria);

        var kategorie = new MenuItem { Header = "Dodaj przedmiot w kategorii" };
        foreach (var kategoria in vm.Kategorie)
            kategorie.Items.Add(new MenuItem
            {
                Header = kategoria.Nazwa,
                Command = vm.DodajWKategoriiCommand,
                CommandParameter = kategoria,
            });
        menu.Items.Add(kategorie);

        if (vm.LiczbaZaznaczonych > 0)
        {
            menu.Items.Add(new Separator());
            // Oddzielne operacje: PPM na wierszu usuwa tylko ten wiersz.
            // Ta pozycja usuwa WYŁĄCZNIE checkboxy, niezależnie od wskazanego wiersza.
            Element($"Usuń zaznaczone ({vm.LiczbaZaznaczonych})…",
                vm.UsunZaznaczoneCommand);
            var odznacz = new MenuItem { Header = "Odznacz wszystkie" };
            odznacz.Click += (_, _) =>
            {
                foreach (var w in vm.Wiersze)
                    w.Zaznaczony = false;
            };
            menu.Items.Add(odznacz);
        }
    }

}
