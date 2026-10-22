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

    /// <summary>FR-1406: zastosowanie szablonu konfiguracji („pusta” lub „przyklad”) do bieżących danych.
    /// Szablon jest SCALANY z istniejącą konfiguracją (brakujące wartości dopisywane, istniejące zachowane z aliasami — FR-1003); dane o sprzęcie zostają, referencje przemapowane na zachowane wartości.</summary>
    [RelayCommand]
    private void ZastosujSzablon(string? klucz)
    {
        if (klucz == null)
            return;
        var odpowiedz = System.Windows.MessageBox.Show(
            "Zastosować szablon konfiguracji?\n\nBrakujące kategorie, wzory i wartości słowników zostaną dopisane, istniejące zachowane. Przedmioty i repliki nie zostaną usunięte. Zostanie też wykonana kopia zapasowa.",
            "Kwatermistrz", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (odpowiedz != System.Windows.MessageBoxResult.Yes)
            return;
        _magazyn.UtworzKopieZapasowa(); // FR-1107: przed zmianą konfiguracji kopia
        var (slownikiSzablonu, szablonRekonstrukcji, szablonyKart) = Kwatermistrz.Core.Konfiguracja.SzablonyKonfiguracji.Buduj(klucz);
        // scalenie słowników zamiast podmiany — istniejące wartości zostają, brakujące dopisane
        var wynik = Kwatermistrz.Core.Konfiguracja.MergerSlownikow.ScalSlowniki(_dane.Slowniki, slownikiSzablonu);
        // szablony kart: dopisz tylko te, których jeszcze nie ma (po nazwie); napęd przemapowany
        foreach (var szablon in szablonyKart)
        {
            var istniejacy = _dane.SzablonyKart.FirstOrDefault(x =>
                Normalizacja.TekstDoPorownania(x.Nazwa) == Normalizacja.TekstDoPorownania(szablon.Nazwa));
            if (istniejacy != null)
                continue;
            szablon.NapedId = wynik.MapujSlownik("Napedy", szablon.NapedId);
            _dane.SzablonyKart.Add(szablon);
        }
        // przemapuj referencje istniejących rekordów (stare Id mogły się zmienić przy scaleniu)
        foreach (var p in _dane.Przedmioty)
        {
            p.KategoriaId = wynik.MapujKategoria(p.KategoriaId) ?? p.KategoriaId;
            p.WzorId = wynik.MapujWzor(p.WzorId);
            p.StronyId = p.StronyId.Select(id => wynik.MapujSlownik("Strony", id) ?? id).ToList();
            p.MiejsceId = wynik.MapujSlownik("Miejsca", p.MiejsceId);
            p.PochodzenieId = wynik.MapujSlownik("Pochodzenie", p.PochodzenieId);
            foreach (var koszt in p.Koszty)
                koszt.RodzajId = wynik.MapujSlownik("RodzajeKosztow", koszt.RodzajId) ?? koszt.RodzajId;
        }
        foreach (var r in _dane.Repliki)
        {
            r.StronyId = r.StronyId.Select(id => wynik.MapujSlownik("Strony", id) ?? id).ToList();
            r.RoleId = r.RoleId.Select(id => wynik.MapujSlownik("Role", id) ?? id).ToList();
            r.TypMagazynkaId = wynik.MapujSlownik("TypyMagazynkow", r.TypMagazynkaId);
            r.NapedId = wynik.MapujSlownik("Napedy", r.NapedId);
            r.StanId = wynik.MapujSlownik("StanyReplik", r.StanId);
            foreach (var koszt in r.Koszty)
                koszt.RodzajId = wynik.MapujSlownik("RodzajeKosztow", koszt.RodzajId) ?? koszt.RodzajId;
        }
        foreach (var z in _dane.Zestawy)
        {
            z.StronaId = wynik.MapujSlownik("Strony", z.StronaId);
            foreach (var zadanie in _dane.Zadania)
                zadanie.RodzajId = wynik.MapujSlownik("RodzajeZadan", zadanie.RodzajId) ?? zadanie.RodzajId;
        }
        // rekonstrukcja z szablonu: dodaj jako nową (z unikalną nazwą), jeśli taka już istnieje
        szablonRekonstrukcji.Nazwa = UnikalnaNazwa(szablonRekonstrukcji.Nazwa);
        szablonRekonstrukcji.StronaId = wynik.MapujSlownik("Strony", szablonRekonstrukcji.StronaId);
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
