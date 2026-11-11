using System.Windows;
using System.Windows.Controls;

namespace Kwatermistrz.App.Views;

/// <summary>Ekran zestawów (FR-701–705, FR-709): lista zestawów, karta z edytorem slotów i wariantów, lista pakowania (FR-707).</summary>
public partial class ZestawyPage : Page
{
    public ZestawyPage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is ViewModels.ZestawyViewModel vm)
            {
                vm.Odswiez();
                vm.OdswiezPakowanie();
            }
        };
    }

    /// <summary>Windows smoke: lista wypełnia wysokość, oba panele rozciągają
    /// się na szerokość okna i rozdzielacz realnie zmienia wielkość.</summary>
    public void ZweryfikujUklad()
    {
        UpdateLayout();
        if (DataContext is not ViewModels.ZestawyViewModel vm)
            throw new InvalidOperationException("Brak ViewModelu zestawów.");
        if (SiatkaZestawow.ActualWidth < 650 || ListaZestawow.ActualHeight < 180)
            throw new InvalidOperationException(
                $"Tabela zestawów nie wypełnia dostępnego obszaru: " +
                $"{SiatkaZestawow.ActualWidth:F0}×{ListaZestawow.ActualHeight:F0}.");
        if (!PodzialZestawow.IsVisible ||
            PodzialZestawow.ResizeDirection != GridResizeDirection.Columns)
            throw new InvalidOperationException("Rozdzielacz listy i edytora nie jest aktywny.");

        var przed = KolumnaListyZestawow.ActualWidth;
        var poprzedniaSzerokosc = KolumnaListyZestawow.Width;
        try
        {
            var nowa = przed < 650
                ? Math.Min(740, przed + 85)
                : Math.Max(335, przed - 85);
            KolumnaListyZestawow.Width = new GridLength(nowa);
            SiatkaZestawow.UpdateLayout();
            if (Math.Abs(KolumnaListyZestawow.ActualWidth - przed) < 35)
                throw new InvalidOperationException("Nie można zmienić szerokości listy zestawów.");
        }
        finally
        {
            KolumnaListyZestawow.Width = poprzedniaSzerokosc;
            SiatkaZestawow.UpdateLayout();
        }

        if (vm.Wiersze.Count == 0)
            return;
        var wybraneId = vm.Wybrany?.Id
            ?? throw new InvalidOperationException("Po załadowaniu zestawów nie wybrano żadnego.");
        if (EdytorZestawu.Visibility != Visibility.Visible ||
            EdytorZestawu.ActualWidth < 300)
            throw new InvalidOperationException("Karta wybranego zestawu nie jest widoczna.");

        vm.Odswiez();
        UpdateLayout();
        if (vm.Wybrany?.Id != wybraneId ||
            ListaZestawow.SelectedItem is not ViewModels.WierszZestawu aktywny ||
            aktywny.Id != wybraneId)
            throw new InvalidOperationException("Odświeżenie listy zgubiło zaznaczony zestaw.");
    }

    private void GaleriaZdjec_Zmieniono(object? sender, EventArgs e)
    {
        if (DataContext is ViewModels.ZestawyViewModel vm)
            vm.ZapiszGalerie();
    }

    private void Domyslny_Checked(object sender, RoutedEventArgs e)
    {
        // RadioButton "Domyślny": DataContext = WariantSlotu, przodek DataTemplate = SlotZestawu
        if (sender is RadioButton { DataContext: Kwatermistrz.Core.Model.WariantSlotu wariant }
            && sender is System.Windows.DependencyObject dep
            && FindAncestorSlot(dep) is { } slot
            && DataContext is ViewModels.ZestawyViewModel vm)
            vm.UstawDomyslnyCommand.Execute(new object[] { slot, wariant });
    }

    private static Kwatermistrz.Core.Model.SlotZestawu? FindAncestorSlot(System.Windows.DependencyObject element)
    {
        // wzorzec DataTemplate: szukamy ramki z DataContext typu SlotZestawu
        var aktualny = System.Windows.Media.VisualTreeHelper.GetParent(element);
        while (aktualny != null)
        {
            if (aktualny is System.Windows.FrameworkElement fe && fe.DataContext is Kwatermistrz.Core.Model.SlotZestawu slot)
                return slot;
            aktualny = System.Windows.Media.VisualTreeHelper.GetParent(aktualny);
        }
        return null;
    }

    private void Spakowany_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox { DataContext: ViewModels.ZestawyViewModel.WierszPakowania wiersz }
            && DataContext is ViewModels.ZestawyViewModel vm)
            vm.PrzelaczSpakowanyCommand.Execute(wiersz);
    }
}
