using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwatermistrz.App.Services;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.App.ViewModels;

/// <summary>ViewModel ekranu zestawów (FR-701–705, FR-709): lista, karta z edytorem slotów i wariantów.</summary>
public partial class ZestawyViewModel : ObservableObject
{
    private readonly DaneAplikacji _dane;
    private readonly MagazynJson _magazyn;
    private readonly KalkulatorSerwis _kalkulator;
    private readonly StanAplikacji _stan;

    public ZestawyViewModel(DaneAplikacji dane, MagazynJson magazyn, KalkulatorSerwis kalkulator, StanAplikacji stan)
    {
        _dane = dane;
        _magazyn = magazyn;
        _kalkulator = kalkulator;
        _stan = stan;
    }

    /// <summary>Wiersze listy zestawów (FR-709).</summary>
    public ObservableCollection<WierszZestawu> Wiersze { get; } = new();

    /// <summary>Wybrany zestaw (karta).</summary>
    [ObservableProperty]
    private WierszZestawu? _wybrany;

    /// <summary>Filtr tekstowy.</summary>
    [ObservableProperty]
    private string _filtrTekst = "";

    /// <summary>Tylko szkice.</summary>
    [ObservableProperty]
    private bool _tylkoSzkice;

    /// <summary>Słowniki.</summary>
    public Slowniki Slowniki => _dane.Slowniki;

    /// <summary>Rekonstrukcje (FR-701).</summary>
    public IReadOnlyList<Rekonstrukcja> Rekonstrukcje => _dane.Rekonstrukcje;

    /// <summary>Przedmioty do wyboru w wariantach (FR-703).</summary>
    public IReadOnlyList<Przedmiot> Przedmioty => _dane.Przedmioty
        .Where(p => p.Status == StatusWlasnosci.Posiadany || p.Status == StatusWlasnosci.DoSprzedazy)
        .OrderBy(p => p.Nazwa)
        .ToList();

    /// <summary>Repliki do wyboru w wariantach (FR-703, FR-805 — bez sprzedanych/wycofanych).</summary>
    public IReadOnlyList<Replika> Repliki => _dane.Repliki
        .Where(r => r.Status == StatusWlasnosci.Posiadany || r.Status == StatusWlasnosci.DoSprzedazy)
        .OrderBy(r => r.ModelProducenta)
        .ToList();

    /// <summary>Odświeża listę (FR-709, FR-1303).</summary>
    public void Odswiez()
    {
        var kalk = _kalkulator.Kalkulator();
        var zapytanie = _dane.Zestawy.AsEnumerable();
        if (_stan.AktywnaRekonstrukcjaId != null)
            zapytanie = zapytanie.Where(z => z.RekonstrukcjaId == null || z.RekonstrukcjaId == _stan.AktywnaRekonstrukcjaId);
        if (TylkoSzkice)
            zapytanie = zapytanie.Where(z => z.CzySzkic);
        var tekst = Normalizacja.Przytnij(FiltrTekst);
        if (tekst != null)
        {
            var szukane = Normalizacja.TekstDoPorownania(tekst);
            zapytanie = zapytanie.Where(z => Normalizacja.TekstDoPorownania(z.Nazwa).Contains(szukane));
        }
        Wiersze.Clear();
        foreach (var z in zapytanie.OrderBy(z => z.Nazwa, StringComparer.CurrentCulture))
        {
            var (wyprawy, procent, ostatnie) = kalk.StatystykiZestawu(z);
            Wiersze.Add(new WierszZestawu(z, wyprawy, procent, ostatnie));
        }
    }

    partial void OnFiltrTekstChanged(string value) => Odswiez();
    partial void OnTylkoSzkiceChanged(bool value) => Odswiez();

    /// <summary>Wybiera zestaw (karta).</summary>
    [RelayCommand]
    private void Wybierz(WierszZestawu? wiersz)
    {
        Wybrany = wiersz;
        if (wiersz != null)
        {
            wiersz.Zestaw.ZnacznikZImportu = false;
            _magazyn.Zapisz(_dane);
        }
    }

    /// <summary>Dodaje nowy zestaw (FR-701); sloty z szablonu aktywnej rekonstrukcji (FR-702).</summary>
    [RelayCommand]
    private void Dodaj()
    {
        var zestaw = new Zestaw();
        if (_stan.AktywnaRekonstrukcjaId != null)
        {
            zestaw.RekonstrukcjaId = _stan.AktywnaRekonstrukcjaId;
            var rek = _dane.Rekonstrukcje.FirstOrDefault(r => r.Id == _stan.AktywnaRekonstrukcjaId);
            if (rek != null)
            {
                zestaw.StronaId = rek.StronaId;
                foreach (var slot in rek.SzablonSlotow.OrderBy(s => s.Kolejnosc))
                    zestaw.Sloty.Add(new SlotZestawu { Nazwa = slot.Nazwa, Kolejnosc = slot.Kolejnosc });
            }
        }
        _dane.Zestawy.Add(zestaw);
        _magazyn.Zapisz(_dane);
        Odswiez();
        Wybrany = Wiersze.FirstOrDefault(w => w.Id == zestaw.Id);
    }

    /// <summary>Zapisuje kartę zestawu (FR-1203).</summary>
    [RelayCommand]
    private void ZapiszKarte()
    {
        if (Wybrany == null)
            return;
        // etykieta z nazwy munduru, gdy nazwa pusta (FR-701)
        if (string.IsNullOrWhiteSpace(Wybrany.Zestaw.Nazwa))
        {
            var pierwszy = Wybrany.Zestaw.Sloty.FirstOrDefault();
            if (pierwszy?.Warianty.FirstOrDefault()?.Elementy.FirstOrDefault() is { } el && el.Typ == TypElementu.Przedmiot)
                Wybrany.Zestaw.Nazwa = _dane.Przedmioty.FirstOrDefault(p => p.Id == el.ElementId)?.Nazwa ?? "";
        }
        Wybrany.Zestaw.ZnacznikZImportu = false;
        _magazyn.Zapisz(_dane);
        Odswiez();
    }

    /// <summary>Usuwa zestaw (FR-1202).</summary>
    [RelayCommand]
    private void Usun(WierszZestawu? wiersz)
    {
        if (wiersz == null)
            return;
        _dane.Zestawy.Remove(wiersz.Zestaw);
        _magazyn.Zapisz(_dane);
        Odswiez();
    }

    /// <summary>Dodaje slot do zestawu (FR-702; lista edytowalna).</summary>
    [RelayCommand]
    private void DodajSlot()
    {
        if (Wybrany == null)
            return;
        Wybrany.Zestaw.Sloty.Add(new SlotZestawu
        {
            Nazwa = "Nowy slot",
            Kolejnosc = Wybrany.Zestaw.Sloty.Count,
        });
        _magazyn.Zapisz(_dane);
    }

    /// <summary>Usuwa slot (tylko gdy nie ma wariantów; FR-702).</summary>
    [RelayCommand]
    private void UsunSlot(SlotZestawu? slot)
    {
        if (Wybrany == null || slot == null)
            return;
        Wybrany.Zestaw.Sloty.Remove(slot);
        _magazyn.Zapisz(_dane);
    }

    /// <summary>Dodaje wariant do slotu (FR-703); domyślnie pierwszy element lub „puste”.</summary>
    [RelayCommand]
    private void DodajWariant(SlotZestawu? slot)
    {
        if (Wybrany == null || slot == null)
            return;
        slot.Warianty.Add(new WariantSlotu());
        _magazyn.Zapisz(_dane);
    }

    /// <summary>Usuwa wariant (FR-703).</summary>
    [RelayCommand]
    private void UsunWariant(WariantSlotu? wariant)
    {
        if (Wybrany == null || wariant == null)
            return;
        foreach (var slot in Wybrany.Zestaw.Sloty)
            slot.Warianty.Remove(wariant);
        _magazyn.Zapisz(_dane);
    }

    /// <summary>Ustawia wariant domyślny slotu (FR-704).</summary>
    [RelayCommand]
    private void UstawDomyslny(object? parametry)
    {
        if (Wybrany == null || parametry is not object[] { Length: 2 } tablica)
            return;
        if (tablica[0] is SlotZestawu slot && tablica[1] is int indeks)
        {
            slot.IndeksWariantuDomyslnego = indeks;
            _magazyn.Zapisz(_dane);
        }
    }

    /// <summary>Dodaje element do wariantu (FR-703).</summary>
    [RelayCommand]
    private void DodajElement(WariantSlotu? wariant)
    {
        if (Wybrany == null || wariant == null)
            return;
        wariant.Elementy.Add(new ElementWariantu { Typ = TypElementu.Przedmiot });
        _magazyn.Zapisz(_dane);
    }

    /// <summary>Usuwa element z wariantu (FR-703).</summary>
    [RelayCommand]
    private void UsunElement(ElementWariantu? element)
    {
        if (Wybrany == null || element == null)
            return;
        foreach (var slot in Wybrany.Zestaw.Sloty)
            foreach (var wariant in slot.Warianty)
                wariant.Elementy.Remove(element);
        _magazyn.Zapisz(_dane);
    }

    /// <summary>Próbki wzorów elementów wybranego wariantu slotu (FR-705).</summary>
    public IReadOnlyList<WzorKamuflazu> ProbkiWzorow(SlotZestawu? slot, int indeksWariantu)
    {
        if (Wybrany == null || slot == null || indeksWariantu < 0 || indeksWariantu >= slot.Warianty.Count)
            return new List<WzorKamuflazu>();
        var wynik = new List<WzorKamuflazu>();
        foreach (var element in slot.Warianty[indeksWariantu].Elementy)
        {
            if (element.Typ == TypElementu.Przedmiot)
            {
                var p = _dane.Przedmioty.FirstOrDefault(x => x.Id == element.ElementId);
                var wzor = p?.WzorId != null ? _dane.Slowniki.Wzory.FirstOrDefault(w => w.Id == p.WzorId) : null;
                if (wzor != null && !wynik.Contains(wzor))
                    wynik.Add(wzor);
            }
        }
        return wynik;
    }

    /// <summary>Nazwa elementu wariantu (do wyświetlenia).</summary>
    public string NazwaElementu(ElementWariantu element)
    {
        if (element.NazwaSzkicu != null)
            return element.NazwaSzkicu;
        return element.Typ switch
        {
            TypElementu.Przedmiot => _dane.Przedmioty.FirstOrDefault(p => p.Id == element.ElementId)?.Nazwa ?? "?",
            TypElementu.Replika => _dane.Repliki.FirstOrDefault(r => r.Id == element.ElementId)?.ModelProducenta ?? "?",
            TypElementu.Komplet => _dane.Przedmioty.FirstOrDefault(p => p.Id == element.ElementId)?.Nazwa ?? "?",
            _ => "?",
        };
    }
}
