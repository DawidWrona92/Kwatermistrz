using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Kwatermistrz.App.Controls;
using Kwatermistrz.App.ViewModels;
using Kwatermistrz.App.Views;
using System.Windows.Data;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Storage;

internal static class Program
{
    // Smoke test WPF musi działać na wątku STA (nie jest testem Core/xUnit).
    [STAThread]
    private static int Main()
    {
        var probka = new ProbkaKamuflazu();

        Sprawdz(new WzorKamuflazu
        {
            KoloryProbki = new() { "#112233", "#445566", "#778899", "#AABBCC" },
        }, "cztery kolory");

        Sprawdz(new WzorKamuflazu
        {
            KoloryProbki = new() { "#FFFFFF", "#000000" },
        }, "dwa kolory");

        Sprawdz(new WzorKamuflazu
        {
            KoloryProbki = new() { "NIE-JEST-KOLOREM", "#112233" },
        }, "niepoprawny kolor z importu");

        Sprawdz(new WzorKamuflazu(), "pusta paleta");

        probka.Wzor = null;
        SprawdzSiatke("brak wzoru");

        // Powtórne zmiany są sednem regresji: stara implementacja zmieniała
        // VisualTree już zapieczętowanego ControlTemplate i wyrzucała wyjątek.
        for (var i = 0; i < 10; i++)
            Sprawdz(new WzorKamuflazu { KoloryProbki = new() { "#00AACC" } }, $"zmiana {i}");

        // Regresja FR-1401: ValueTuple miał tylko pola Item1/Item2, więc binding
        // do Nazwa/Klucz pokazywał trzy puste przyciski radiowe.
        var modelKreatora = new KreatorViewModel(new DaneAplikacji(), new MagazynJson(Path.GetTempPath()));
        var starty = modelKreatora.Starty;
        if (starty.Count != 3 || starty.Select(x => x.Klucz).Distinct().Count() != 3)
            throw new InvalidOperationException("Kreator: nieprawidłowe opcje startowe.");
        foreach (var start in starty)
        {
            var radio = new RadioButton { DataContext = start };
            BindingOperations.SetBinding(radio, ContentControl.ContentProperty,
                new Binding(nameof(OpcjaStartu.Nazwa)));
            radio.GetBindingExpression(ContentControl.ContentProperty)?.UpdateTarget();
            if (radio.Content is not string nazwa || string.IsNullOrWhiteSpace(nazwa)
                || nazwa != start.Nazwa)
                throw new InvalidOperationException($"Kreator: brak etykiety dla {start.Klucz}.");
        }
        var kreator = new KreatorWindow { DataContext = modelKreatora };
        if (kreator.FindName("ListaStartow") is not ItemsControl lista)
            throw new InvalidOperationException("Kreator: brak kontrolki listy startowej.");
        lista.GetBindingExpression(ItemsControl.ItemsSourceProperty)?.UpdateTarget();
        if (lista.Items.Count != 3)
            throw new InvalidOperationException($"Kreator: ItemsSource nie pokazało trzech opcji (jest {lista.Items.Count}).");
        if (kreator.WindowStyle != WindowStyle.SingleBorderWindow
            || kreator.ResizeMode != ResizeMode.CanMinimize
            || !kreator.ShowInTaskbar)
            throw new InvalidOperationException("Kreator: brak natywnego paska tytułu z minimalizacją.");
        Console.WriteLine("OK: Kreator — 3 pozycje ItemsSource, widoczne etykiety i natywny pasek z minimalizacją.");

        Console.WriteLine("OK: ProbkaKamuflazu — zmiany wzoru, wielokrotny ApplyTemplate i błędny kolor.");
        return 0;

        void Sprawdz(WzorKamuflazu wzor, string nazwa)
        {
            probka.Wzor = wzor;
            SprawdzSiatke(nazwa);
        }

        void SprawdzSiatke(string nazwa)
        {
            probka.ApplyTemplate(); // może zwrócić false, gdy szablon zastosowano już w OnWzorChanged
            if (probka.Template == null)
                throw new InvalidOperationException($"Brak szablonu: {nazwa}");
            probka.Measure(new Size(24, 24));
            probka.Arrange(new Rect(0, 0, 24, 24));
            if (VisualTreeHelper.GetChildrenCount(probka) != 1
                || VisualTreeHelper.GetChild(probka, 0) is not UniformGrid siatka
                || siatka.Children.Count != 4
                || siatka.Children.OfType<Border>().Count() != 4)
            {
                throw new InvalidOperationException($"Błędna siatka próbki: {nazwa}");
            }
        }
    }
}
