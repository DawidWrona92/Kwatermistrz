using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Kwatermistrz.App.Controls;
using Kwatermistrz.Core.Model;

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

        Console.WriteLine("OK: ProbkaKamuflazu — zmiany wzoru, wielokrotny ApplyTemplate i błędny kolor.");
        return 0;

        void Sprawdz(WzorKamuflazu wzor, string nazwa)
        {
            probka.Wzor = wzor;
            SprawdzSiatke(nazwa);
        }

        void SprawdzSiatke(string nazwa)
        {
            if (!probka.ApplyTemplate())
                throw new InvalidOperationException($"Nie zastosowano szablonu: {nazwa}");
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
