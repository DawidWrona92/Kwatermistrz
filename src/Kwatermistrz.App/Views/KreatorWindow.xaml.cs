using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
                    // Ustawienie wyniku zamyka okno modalne ShowDialog.
                    DialogResult = true;
                };
        };
    }

    /// <summary>Test UI uruchamiany na Windows CI już po rzeczywistym wyrenderowaniu kreatora.</summary>
    public void ZweryfikujOpcjeStartu()
    {
        if (DataContext is not KreatorViewModel vm)
            throw new InvalidOperationException("Kreator nie ma ViewModelu.");

        UpdateLayout();
        var przyciski = ZnajdzDzieci<RadioButton>(this)
            .Where(radio => radio.GroupName == "Start")
            .ToList();

        if (przyciski.Count != KreatorViewModel.Starty.Count)
            throw new InvalidOperationException(
                $"Nieprawidłowa liczba opcji startu: {przyciski.Count}, oczekiwano {KreatorViewModel.Starty.Count}.");

        foreach (var opcja in KreatorViewModel.Starty)
        {
            var radio = przyciski.SingleOrDefault(r =>
                r.DataContext is OpcjaStartu wybor && wybor.Klucz == opcja.Klucz);
            if (radio == null || radio.Content is not string nazwa || nazwa != opcja.Nazwa)
                throw new InvalidOperationException($"Opcja '{opcja.Klucz}' nie wyświetla poprawnej nazwy.");

            radio.IsChecked = true;
            if (vm.WybranyStart != opcja.Klucz)
                throw new InvalidOperationException($"Wybranie '{opcja.Klucz}' nie aktualizuje kreatora.");
        }

        // Przywracamy domyślny punkt startu, którego używa test finalizacji.
        var domyslny = przyciski.Single(r =>
            r.DataContext is OpcjaStartu opcja && opcja.Klucz == Kwatermistrz.Core.Konfiguracja.SzablonyKonfiguracji.Pusta);
        domyslny.IsChecked = true;

        // Walidujemy realnie zastosowane kolory w obydwu motywach.
        if (Foreground is not SolidColorBrush tekst || Background is not SolidColorBrush tlo)
            throw new InvalidOperationException("Brak pędzli tekstu/tła kreatora.");

        static double Jasnosc(byte r, byte g, byte b)
        {
            static double Kanal(byte x)
            {
                var v = x / 255.0;
                return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
            }
            return 0.2126 * Kanal(r) + 0.7152 * Kanal(g) + 0.0722 * Kanal(b);
        }

        var przezroczystosc = (tekst.Color.A / 255.0) * tekst.Opacity;
        byte Mieszaj(byte fg, byte bg)
            => (byte)Math.Round(fg * przezroczystosc + bg * (1 - przezroczystosc));

        var a = Jasnosc(Mieszaj(tekst.Color.R, tlo.Color.R),
            Mieszaj(tekst.Color.G, tlo.Color.G), Mieszaj(tekst.Color.B, tlo.Color.B));
        var b = Jasnosc(tlo.Color.R, tlo.Color.G, tlo.Color.B);
        var kontrast = (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
        if (kontrast < 4.5)
            throw new InvalidOperationException($"Niewystarczający kontrast tekstu kreatora: {kontrast:F2}:1.");
    }

    private static IEnumerable<T> ZnajdzDzieci<T>(DependencyObject korzen) where T : DependencyObject
    {
        if (korzen is T znaleziony)
            yield return znaleziony;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(korzen); i++)
        {
            foreach (var element in ZnajdzDzieci<T>(VisualTreeHelper.GetChild(korzen, i)))
                yield return element;
        }
    }

    /// <summary>Zapisuje wybór punktu startu (radio „Start”; IsChecked jest jednokierunkowe przez MultiBinding, więc wybór trafia do ViewModelu tutaj).</summary>
    private void Start_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { DataContext: OpcjaStartu start }
            && DataContext is KreatorViewModel vm)
            vm.WybranyStart = start.Klucz;
    }
}
