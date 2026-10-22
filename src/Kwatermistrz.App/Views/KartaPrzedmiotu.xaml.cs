using System.Windows;
using System.Windows.Controls;
using Kwatermistrz.App.ViewModels;

namespace Kwatermistrz.App.Views;

/// <summary>Karta przedmiotu (FR-204): formularz pól z 6.1, komplet (FR-206), montaż (FR-207), galeria zdjęć (FR-211).</summary>
public partial class KartaPrzedmiotu : UserControl
{
    public KartaPrzedmiotu()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            // po zmianie zdjęć w galerii zapisujemy dane natychmiast (FR-1203)
            Galeria.Zmieniono += (_, _) => ZapiszCicho();
        };
    }

    private void ZapiszCicho()
    {
        // karta siedzi w panelu karty okna głównego; DataContext okna to MainWindowViewModel
        if (Window.GetWindow(this)?.DataContext is MainWindowViewModel mw)
            mw.Wyposazenie.ZapiszCicho();
    }
}
