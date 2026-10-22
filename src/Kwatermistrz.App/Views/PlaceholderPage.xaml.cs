using System.Windows.Controls;

namespace Kwatermistrz.App.Views;

/// <summary>Strona zastępcza dla ekranów przyszłych etapów — pokazuje nazwę ekranu i etap.</summary>
public partial class PlaceholderPage : Page
{
    public PlaceholderPage(string tytul, string etap)
    {
        InitializeComponent();
        Tytul.Text = tytul;
        Opis.Text = $"Ten ekran powstanie w kolejnym etapie ({etap}).";
    }
}
