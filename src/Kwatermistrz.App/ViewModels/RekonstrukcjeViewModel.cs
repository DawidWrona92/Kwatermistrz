using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.App.ViewModels;

/// <summary>ViewModel ekranu rekonstrukcji (FR-1301, FR-1306): lista, dodawanie, edycja szablonu slotów.</summary>
public partial class RekonstrukcjeViewModel : ObservableObject
{
    private readonly DaneAplikacji _dane;
    private readonly MagazynJson _magazyn;

    public RekonstrukcjeViewModel(DaneAplikacji dane, MagazynJson magazyn)
    {
        _dane = dane;
        _magazyn = magazyn;
    }

    /// <summary>Lista rekonstrukcji.</summary>
    public IReadOnlyList<Rekonstrukcja> Rekonstrukcje => _dane.Rekonstrukcje;

    /// <summary>Wybrana rekonstrukcja do edycji.</summary>
    [ObservableProperty]
    private Rekonstrukcja? _wybrana;

    /// <summary>Nowa rekonstrukcja w formularzu.</summary>
    [ObservableProperty]
    private string _nowaNazwa = "";

    /// <summary>FR-1406: zastosowanie dołączonego szablonu konfiguracji („pusta” lub „przyklad”) do bieżących danych — kategorie, wzory, sloty i szablony kart; dane o sprzęcie zostają.</summary>
    [RelayCommand]
    private void ZastosujSzablon(string? klucz)
    {
        if (klucz == null)
            return;
        var (slowniki, szablonRekonstrukcji, szablonyKart) = Kwatermistrz.Core.Konfiguracja.SzablonyKonfiguracji.Buduj(klucz);
        // kategorie i wzory: zastępujemy konfiguracją szablonu (dane o sprzęcie tracą przypisania do starych Id — użytkownik przypisuje ponownie)
        var stareKategorie = _dane.Slowniki.Kategorie.ToDictionary(k => k.Id, k => k.Nazwa, StringComparer.Ordinal);
        _dane.Slowniki = slowniki;
        _dane.SzablonyKart.Clear();
        _dane.SzablonyKart.AddRange(szablonyKart);
        // przemapuj kategorie przedmiotów po nazwie (zachowanie przypisań tam, gdzie nazwa się zgadza)
        var noweKategorie = slowniki.Kategorie.ToDictionary(k => Normalizacja.TekstDoPorownania(k.Nazwa), k => k.Id, StringComparer.Ordinal);
        foreach (var p in _dane.Przedmioty)
        {
            if (stareKategorie.TryGetValue(p.KategoriaId, out var staraNazwa)
                && noweKategorie.TryGetValue(Normalizacja.TekstDoPorownania(staraNazwa), out var noweId))
                p.KategoriaId = noweId;
        }
        // rekonstrukcja z szablonu: dodaj jako nową (z unikalną nazwą), jeśli taka już istnieje
        szablonRekonstrukcji.Nazwa = UnikalnaNazwa(szablonRekonstrukcji.Nazwa);
        _dane.Rekonstrukcje.Add(szablonRekonstrukcji);
        _dane.Ustawienia.OstatniaRekonstrukcjaId = szablonRekonstrukcji.Id;
        _magazyn.Zapisz(_dane);
        OnPropertyChanged(nameof(Rekonstrukcje));
        Wybrana = szablonRekonstrukcji;
    }

    private string UnikalnaNazwa(string nazwa)
    {
        var istnieje = _dane.Rekonstrukcje.Any(r => Normalizacja.TekstDoPorownania(r.Nazwa) == Normalizacja.TekstDoPorownania(nazwa));
        if (!istnieje)
            return nazwa;
        var i = 2;
        while (_dane.Rekonstrukcje.Any(r => Normalizacja.TekstDoPorownania(r.Nazwa) == Normalizacja.TekstDoPorownania($"{nazwa} {i}")))
            i++;
        return $"{nazwa} {i}";
    }

    /// <summary>Dodaje rekonstrukcję od zera — puste sloty (FR-1306).</summary>
    [RelayCommand]
    private void DodajPusta()
    {
        var nazwa = Normalizacja.Przytnij(NowaNazwa);
        if (nazwa == null)
            return;
        var rekonstrukcja = new Rekonstrukcja { Nazwa = nazwa };
        _dane.Rekonstrukcje.Add(rekonstrukcja);
        NowaNazwa = "";
        _magazyn.Zapisz(_dane);
        OnPropertyChanged(nameof(Rekonstrukcje));
    }

    /// <summary>Tworzy rekonstrukcję „na wzór” istniejącej — kopiuje sloty i szablony, bez przedmiotów (FR-1306).</summary>
    [RelayCommand]
    private void DodajNaWzor(Rekonstrukcja? wzorzec)
    {
        if (wzorzec == null)
            return;
        var nazwa = Normalizacja.Przytnij(NowaNazwa);
        if (nazwa == null)
            return;
        var rekonstrukcja = new Rekonstrukcja
        {
            Nazwa = nazwa,
            StronaId = wzorzec.StronaId,
            SzablonSlotow = wzorzec.SzablonSlotow.Select(s => new SlotSzablonu
            {
                Nazwa = s.Nazwa,
                Kolejnosc = s.Kolejnosc,
                CzySlotMunduru = s.CzySlotMunduru,
                CzySlotBroniGlownej = s.CzySlotBroniGlownej,
            }).ToList(),
        };
        _dane.Rekonstrukcje.Add(rekonstrukcja);
        NowaNazwa = "";
        _magazyn.Zapisz(_dane);
        OnPropertyChanged(nameof(Rekonstrukcje));
    }

    /// <summary>Zapisuje zmiany wybranej rekonstrukcji.</summary>
    [RelayCommand]
    private void ZapiszWybrana()
    {
        _magazyn.Zapisz(_dane);
        OnPropertyChanged(nameof(Rekonstrukcje));
    }

    /// <summary>Usuwa pustą rekonstrukcję (FR-1301).</summary>
    [RelayCommand]
    private void Usun(Rekonstrukcja? rekonstrukcja)
    {
        if (rekonstrukcja == null)
            return;
        var czyPusta = !_dane.Przedmioty.Any(p => p.RekonstrukcjeId.Contains(rekonstrukcja.Id))
            && !_dane.Repliki.Any(r => r.RekonstrukcjeId.Contains(rekonstrukcja.Id))
            && !_dane.Zestawy.Any(z => z.RekonstrukcjaId == rekonstrukcja.Id)
            && !_dane.Wyprawy.Any(w => w.RekonstrukcjaId == rekonstrukcja.Id);
        if (!czyPusta)
            return;
        _dane.Rekonstrukcje.Remove(rekonstrukcja);
        _magazyn.Zapisz(_dane);
        OnPropertyChanged(nameof(Rekonstrukcje));
    }

    /// <summary>Dodaje slot do szablonu wybranej rekonstrukcji (FR-702).</summary>
    [RelayCommand]
    private void DodajSlot(SlotSzablonu? _)
    {
        if (Wybrana == null)
            return;
        var nazwa = Normalizacja.Przytnij(NowaNazwa);
        if (nazwa == null)
            return;
        Wybrana.SzablonSlotow.Add(new SlotSzablonu
        {
            Nazwa = nazwa,
            Kolejnosc = Wybrana.SzablonSlotow.Count,
        });
        NowaNazwa = "";
        _magazyn.Zapisz(_dane);
    }

    /// <summary>Usuwa slot z szablonu (tylko gdy nie ma zestawów z tym slotem).</summary>
    [RelayCommand]
    private void UsunSlot(SlotSzablonu? slot)
    {
        if (Wybrana == null || slot == null)
            return;
        Wybrana.SzablonSlotow.Remove(slot);
        _magazyn.Zapisz(_dane);
    }
}
