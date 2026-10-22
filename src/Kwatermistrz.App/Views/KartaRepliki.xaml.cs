using System.Windows;
using System.Windows.Controls;
using Kwatermistrz.App.ViewModels;

namespace Kwatermistrz.App.Views;

/// <summary>Karta repliki (FR-302): zakładki Przegląd / Karta techniczna / Pomiary / Modyfikacje i koszty / Zadania / Wyprawy; galeria zdjęć (FR-211).</summary>
public partial class KartaRepliki : UserControl
{
    public KartaRepliki()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            // po zmianie zdjęć w galerii zapisujemy dane natychmiast (FR-1203)
            Galeria.Zmieniono += (_, _) =>
            {
                if (Window.GetWindow(this)?.DataContext is MainWindowViewModel mw)
                    mw.Repliki.ZapiszCicho();
            };
        };
    }
}
