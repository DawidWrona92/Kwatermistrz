using System.ComponentModel;
using System.Windows.Media;

namespace Kwatermistrz.App.Services;

/// <summary>
/// Źródło kolorów dla standardowych kontrolek WPF. Binding odświeża istniejące
/// widoki również wtedy, gdy WPF-UI podmienia słowniki motywu.
/// </summary>
public sealed class KoloryInterfejsu : INotifyPropertyChanged
{
    private Brush _glowny = new SolidColorBrush(Color.FromRgb(0x22, 0x27, 0x25));
    private Brush _drugorzedny = new SolidColorBrush(Color.FromRgb(0x59, 0x62, 0x5E));

    public Brush Glowny
    {
        get => _glowny;
        private set
        {
            _glowny = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Glowny)));
        }
    }

    public Brush Drugorzedny
    {
        get => _drugorzedny;
        private set
        {
            _drugorzedny = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Drugorzedny)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void UstawMotyw(bool jasny)
    {
        Glowny = new SolidColorBrush(jasny
            ? Color.FromRgb(0x22, 0x27, 0x25)
            : Color.FromRgb(0xF0, 0xF2, 0xF0));
        Drugorzedny = new SolidColorBrush(jasny
            ? Color.FromRgb(0x59, 0x62, 0x5E)
            : Color.FromRgb(0xB8, 0xBF, 0xBB));
    }
}
