using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace Kwatermistrz.App.Controls;

/// <summary>
/// Dzieli rzeczywistą szerokość DataGrid pomiędzy kolumny oznaczone w XAML
/// przez n*. WPF bywa w stanie początkowo pozostawić kolumny przy MinWidth,
/// mimo dużego pustego obszaru. Przeliczenie odbywa się po Loaded i SizeChanged,
/// nie wymaga ruszania oknem i zachowuje ręcznie ustawione szerokości.
/// </summary>
public static class ProporcjonalneKolumny
{
    public static readonly DependencyProperty WlaczoneProperty =
        DependencyProperty.RegisterAttached("Wlaczone", typeof(bool),
            typeof(ProporcjonalneKolumny), new PropertyMetadata(false, ZmienionoWlaczone));

    private static readonly DependencyProperty StanProperty =
        DependencyProperty.RegisterAttached("Stan", typeof(Stan), typeof(ProporcjonalneKolumny));

    public static bool GetWlaczone(DependencyObject element)
        => (bool)element.GetValue(WlaczoneProperty);

    public static void SetWlaczone(DependencyObject element, bool value)
        => element.SetValue(WlaczoneProperty, value);

    private static void ZmienionoWlaczone(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not DataGrid tabela)
            return;
        if ((bool)e.NewValue)
        {
            if (tabela.GetValue(StanProperty) is Stan)
                return;
            var stan = new Stan(tabela);
            tabela.SetValue(StanProperty, stan);
            stan.Podlacz();
        }
        else if (tabela.GetValue(StanProperty) is Stan stan)
        {
            stan.Odlacz();
            tabela.ClearValue(StanProperty);
        }
    }

    private sealed class Stan
    {
        private readonly DataGrid _tabela;
        private readonly Dictionary<DataGridColumn, double> _wagi = new();
        private readonly HashSet<DataGridColumn> _reczne = new();
        private readonly DependencyPropertyDescriptor _szerokosc =
            DependencyPropertyDescriptor.FromProperty(DataGridColumn.WidthProperty, typeof(DataGridColumn))
                ?? throw new InvalidOperationException("Brak właściwości Width w DataGridColumn.");
        private bool _zmieniam;
        private bool _zaplanowano;
        private bool _podlaczono;
        private double _ostatniaSzerokosc;

        public Stan(DataGrid tabela) => _tabela = tabela;

        public void Podlacz()
        {
            if (_podlaczono)
                return;
            _podlaczono = true;
            _tabela.Loaded += Zaladowano;
            _tabela.Unloaded += Wyladowano;
            _tabela.SizeChanged += ZmienionoRozmiar;
            _tabela.IsVisibleChanged += ZmienionoWidocznosc;
            if (_tabela.IsLoaded)
                Zaladowano(_tabela, new RoutedEventArgs());
        }

        public void Odlacz()
        {
            _podlaczono = false;
            _tabela.Loaded -= Zaladowano;
            _tabela.Unloaded -= Wyladowano;
            _tabela.SizeChanged -= ZmienionoRozmiar;
            _tabela.IsVisibleChanged -= ZmienionoWidocznosc;
            foreach (var kolumna in _wagi.Keys)
                _szerokosc.RemoveValueChanged(kolumna, UzytkownikZmienilSzerokosc);
            _wagi.Clear();
            _reczne.Clear();
        }

        private void Zaladowano(object? sender, RoutedEventArgs args)
        {
            foreach (var kolumna in _tabela.Columns)
            {
                if (!kolumna.Width.IsStar || _wagi.ContainsKey(kolumna))
                    continue;
                _wagi[kolumna] = kolumna.Width.Value;
                _szerokosc.AddValueChanged(kolumna, UzytkownikZmienilSzerokosc);
            }
            Zaplanuj();
        }

        private void Wyladowano(object? sender, RoutedEventArgs args)
        {
            // Strony są buforowane w MainWindow, a po ponownym wejściu mają
            // odświeżyć pomiar (ale nie zapominać rozmiarów nadanych ręcznie).
            _ostatniaSzerokosc = 0;
        }

        private void ZmienionoRozmiar(object sender, SizeChangedEventArgs args)
        {
            if (args.WidthChanged)
                Zaplanuj();
        }

        private void ZmienionoWidocznosc(object sender, DependencyPropertyChangedEventArgs args)
        {
            if (args.NewValue is true)
                Zaplanuj();
        }

        private void UzytkownikZmienilSzerokosc(object? sender, EventArgs args)
        {
            if (_zmieniam || sender is not DataGridColumn kolumna)
                return;
            // WPF ustawia Width=Pixel, gdy użytkownik przeciąga separator
            // kolumny. Tę kolumnę wyłączamy z kolejnych automatycznych zmian.
            _reczne.Add(kolumna);
            Zaplanuj();
        }

        private void Zaplanuj()
        {
            if (!_podlaczono || _zaplanowano || !_tabela.IsLoaded)
                return;
            _zaplanowano = true;
            _tabela.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                _zaplanowano = false;
                if (_podlaczono && _tabela.IsLoaded && _tabela.IsVisible)
                    UstawSzerokosci();
            }));
        }

        private void UstawSzerokosci()
        {
            var calkowita = _tabela.ActualWidth;
            if (double.IsNaN(calkowita) || double.IsInfinity(calkowita) || calkowita < 100)
                return;

            // Przy zmianie rozmiaru różniącej się o mniej niż 1px nie warto
            // powodować ponownego measure/arrange setek wierszy.
            if (Math.Abs(_ostatniaSzerokosc - calkowita) < 1 && _wagi.Count > 0)
                return;

            var dostepna = calkowita - _tabela.BorderThickness.Left -
                _tabela.BorderThickness.Right - 4;
            if (_tabela.HeadersVisibility is DataGridHeadersVisibility.Row or DataGridHeadersVisibility.All)
                dostepna -= Math.Max(0, _tabela.RowHeaderWidth);

            var pionowyPasek = ZnajdzPionowyPasek(_tabela);
            if (pionowyPasek != null && pionowyPasek.Visibility == Visibility.Visible)
                dostepna -= pionowyPasek.ActualWidth;

            var stale = _tabela.Columns.Where(k =>
                    k.Visibility == Visibility.Visible && (!_wagi.ContainsKey(k) || _reczne.Contains(k)))
                .Sum(k => Math.Max(k.MinWidth, k.ActualWidth));

            var automatyczne = _tabela.Columns.Where(k =>
                    k.Visibility == Visibility.Visible && _wagi.ContainsKey(k) && !_reczne.Contains(k))
                .ToList();
            if (automatyczne.Count == 0)
                return;

            var sumaMinimum = automatyczne.Sum(k => k.MinWidth);
            var nadwyzka = Math.Max(0, dostepna - stale - sumaMinimum);
            var sumaWag = automatyczne.Sum(k => _wagi[k]);
            if (sumaWag <= 0)
                return;

            _zmieniam = true;
            try
            {
                foreach (var kolumna in automatyczne)
                {
                    var szerokosc = kolumna.MinWidth + nadwyzka * _wagi[kolumna] / sumaWag;
                    // Nie przeliczaj przy niezmienionym wymiarze — mniej pracy dla WPF.
                    if (Math.Abs(kolumna.ActualWidth - szerokosc) > 0.5)
                        kolumna.Width = new DataGridLength(szerokosc, DataGridLengthUnitType.Pixel);
                }
                _ostatniaSzerokosc = calkowita;
            }
            finally
            {
                _zmieniam = false;
            }
        }

        private static ScrollBar? ZnajdzPionowyPasek(DependencyObject element)
        {
            for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var dziecko = System.Windows.Media.VisualTreeHelper.GetChild(element, i);
                if (dziecko is ScrollBar pasek && pasek.Orientation == Orientation.Vertical)
                    return pasek;
                var znaleziony = ZnajdzPionowyPasek(dziecko);
                if (znaleziony != null)
                    return znaleziony;
            }
            return null;
        }
    }
}
