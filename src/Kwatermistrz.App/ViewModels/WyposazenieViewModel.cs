using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwatermistrz.App.Services;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.App.ViewModels;

/// <summary>ViewModel ekranu wyposażenia (FR-201–207, FR-209, FR-216): lista przedmiotów kategorii, karta, komplety, montaż, edycja zbiorcza.</summary>
public partial class WyposazenieViewModel : ObservableObject
{
    private readonly DaneAplikacji _dane;
    private readonly MagazynJson _magazyn;
    private readonly KalkulatorSerwis _kalkulator;
    private readonly SerwisZdjec _serwisZdjec;

    /// <summary>Serwis zdjęć (FR-211) — dla galerii na karcie.</summary>
    public SerwisZdjec SerwisZdjec => _serwisZdjec;
    private readonly StanAplikacji _stan;

    public WyposazenieViewModel(DaneAplikacji dane, MagazynJson magazyn, KalkulatorSerwis kalkulator, StanAplikacji stan, SerwisZdjec serwisZdjec)
    {
        _dane = dane;
        _magazyn = magazyn;
        _kalkulator = kalkulator;
        _stan = stan;
        _serwisZdjec = serwisZdjec;
    }

    /// <summary>Zapisuje dane bez odświeżania listy — po zmianie samych zdjęć (FR-211, FR-1203).</summary>
    public void ZapiszCicho() => _magazyn.Zapisz(_dane);

    /// <summary>Wszystkie kategorie z licznikami (FR-201).</summary>
    public IReadOnlyList<Kategoria> Kategorie => _dane.Slowniki.Kategorie.OrderBy(k => k.Kolejnosc).ToList();

    /// <summary>Wybrana kategoria (null = wszystkie przedmioty).</summary>
    [ObservableProperty]
    private Kategoria? _wybranaKategoria;

    /// <summary>Wybrany przedmiot (karta po prawej, FR-204).</summary>
    [ObservableProperty]
    private WierszPrzedmiotu? _wybrany;

    /// <summary>Wiersze listy po filtrach.</summary>
    public ObservableCollection<WierszPrzedmiotu> Wiersze { get; } = new();

    /// <summary>Filtr tekstowy (FR-203).</summary>
    [ObservableProperty]
    private string _filtrTekst = "";

    /// <summary>Filtr: pokaż sprzedane/wycofane (FR-209; domyślnie ukryte).</summary>
    [ObservableProperty]
    private bool _pokazSprzedane;

    /// <summary>Tylko przedmioty bez żadnego użycia (FR-104).</summary>
    [ObservableProperty]
    private bool _tylkoBezUzycia;

    /// <summary>Licznik pozycji na liście.</summary>
    public int LiczbaWierszy => Wiersze.Count;

    /// <summary>Liczba zaznaczonych (FR-216).</summary>
    public int LiczbaZaznaczonych => Wiersze.Count(w => w.Zaznaczony);

    /// <summary>Liczba przedmiotów w kategorii (do liczników w nawigacji, FR-201).</summary>
    public int LiczbaPrzedmiotow(string kategoriaId) => _dane.Przedmioty.Count(p => p.KategoriaId == kategoriaId);

    /// <summary>Słowniki do filtrów i karty.</summary>
    public Slowniki Slowniki => _dane.Slowniki;

    /// <summary>Rekonstrukcje do przypisania (FR-1302).</summary>
    public IReadOnlyList<Rekonstrukcja> Rekonstrukcje => _dane.Rekonstrukcje;

    /// <summary>Odświeża listę po zmianie danych/filtrów.</summary>
    public void Odswiez()
    {
        var kalk = _kalkulator.Kalkulator();
        var zapytanie = _dane.Przedmioty.AsEnumerable();
        if (WybranaKategoria != null)
            zapytanie = zapytanie.Where(p => p.KategoriaId == WybranaKategoria.Id);
        if (!PokazSprzedane)
            zapytanie = zapytanie.Where(p => p.Status != StatusWlasnosci.Sprzedany && p.Status != StatusWlasnosci.Wycofany);
        // aktywna rekonstrukcja filtruje listę (FR-1303)
        if (_stan.AktywnaRekonstrukcjaId != null)
            zapytanie = zapytanie.Where(p => p.RekonstrukcjeId.Count == 0 || p.RekonstrukcjeId.Contains(_stan.AktywnaRekonstrukcjaId));
        var tekst = Normalizacja.Przytnij(FiltrTekst);
        if (tekst != null)
        {
            var szukane = Normalizacja.TekstDoPorownania(tekst);
            zapytanie = zapytanie.Where(p =>
                Normalizacja.TekstDoPorownania(p.Nazwa).Contains(szukane) ||
                (p.Firma != null && Normalizacja.TekstDoPorownania(p.Firma).Contains(szukane)) ||
                (p.Komentarz != null && Normalizacja.TekstDoPorownania(p.Komentarz).Contains(szukane)));
        }
        if (TylkoBezUzycia)
            zapytanie = zapytanie.Where(p => kalk.UzyciaPrzedmiotu(p) == 0);
        Wiersze.Clear();
        foreach (var p in zapytanie.OrderBy(p => p.Nazwa, StringComparer.CurrentCulture))
            Wiersze.Add(new WierszPrzedmiotu(p, kalk.UzyciaPrzedmiotu(p)));
        OnPropertyChanged(nameof(LiczbaWierszy));
        OnPropertyChanged(nameof(LiczbaZaznaczonych));
    }

    partial void OnWybranaKategoriaChanged(Kategoria? value) => Odswiez();
    partial void OnFiltrTekstChanged(string value) => Odswiez();
    partial void OnPokazSprzedaneChanged(bool value) => Odswiez();
    partial void OnTylkoBezUzyciaChanged(bool value) => Odswiez();

    /// <summary>Wybiera kategorię z nawigacji (FR-201).</summary>
    [RelayCommand]
    private void WybierzKategorie(Kategoria? kategoria)
    {
        WybranaKategoria = kategoria;
        Wybrany = null;
    }

    /// <summary>Wybiera przedmiot — otwiera kartę (FR-204).</summary>
    [RelayCommand]
    private void Wybierz(WierszPrzedmiotu? wiersz)
    {
        Wybrany = wiersz;
        if (wiersz != null)
            wiersz.Przedmiot.ZnacznikZImportu = false; // przeglądnięcie = akceptacja znacznika (FR-1110)
    }

    /// <summary>Tworzy nowy przedmiot w wybranej kategorii (FR-204; walidacja: kategoria + nazwa lub firma).</summary>
    [RelayCommand]
    private void Dodaj()
    {
        if (WybranaKategoria == null)
            return;
        var przedmiot = new Przedmiot { KategoriaId = WybranaKategoria.Id };
        _dane.Przedmioty.Add(przedmiot);
        Odswiez();
        Wybrany = Wiersze.FirstOrDefault(w => w.Id == przedmiot.Id);
    }

    /// <summary>Ile kopii przy „Dodaj N takich samych” (FR-205).</summary>
    [ObservableProperty]
    private int _ileKopii = 1;

    /// <summary>„Dodaj N takich samych” (FR-205).</summary>
    [RelayCommand]
    private void DodajWiele()
    {
        if (Wybrany == null)
            return;
        var ile = Math.Max(1, IleKopii);
        for (var i = 0; i < ile; i++)
        {
            var kopia = new Przedmiot
            {
                KategoriaId = Wybrany.Przedmiot.KategoriaId,
                Firma = Wybrany.Przedmiot.Firma,
                Nazwa = Wybrany.Przedmiot.Nazwa,
                WzorId = Wybrany.Przedmiot.WzorId,
                StronyId = Wybrany.Przedmiot.StronyId.ToList(),
                MiejsceId = Wybrany.Przedmiot.MiejsceId,
                PochodzenieId = Wybrany.Przedmiot.PochodzenieId,
                CenaZakupu = Wybrany.Przedmiot.CenaZakupu,
                Komentarz = Wybrany.Przedmiot.Komentarz,
            };
            kopia.SynchronizujCenaZakupu(_dane.Slowniki, DateOnly.FromDateTime(DateTime.Today));
            _dane.Przedmioty.Add(kopia);
        }
        _magazyn.Zapisz(_dane);
        Odswiez();
    }

    /// <summary>„Duplikuj” (FR-205) — osobny rekord, własna historia użycia.</summary>
    [RelayCommand]
    private void Duplikuj()
    {
        if (Wybrany == null)
            return;
        var kopia = new Przedmiot
        {
            KategoriaId = Wybrany.Przedmiot.KategoriaId,
            Firma = Wybrany.Przedmiot.Firma,
            Nazwa = Wybrany.Przedmiot.Nazwa,
            WzorId = Wybrany.Przedmiot.WzorId,
            StronyId = Wybrany.Przedmiot.StronyId.ToList(),
            MiejsceId = Wybrany.Przedmiot.MiejsceId,
            PochodzenieId = Wybrany.Przedmiot.PochodzenieId,
            CenaZakupu = Wybrany.Przedmiot.CenaZakupu,
            Komentarz = Wybrany.Przedmiot.Komentarz,
        };
        kopia.SynchronizujCenaZakupu(_dane.Slowniki, DateOnly.FromDateTime(DateTime.Today));
        _dane.Przedmioty.Add(kopia);
        _magazyn.Zapisz(_dane);
        Odswiez();
        Wybrany = Wiersze.FirstOrDefault(w => w.Id == kopia.Id);
    }

    /// <summary>Zapisuje kartę wybranego przedmiotu po zatwierdzeniu formularza (FR-1203, FR-204); pole „cena zakupu” synchronizuje pozycję kosztową (FR-503).</summary>
    [RelayCommand]
    private void ZapiszKarte()
    {
        if (Wybrany == null)
            return;
        Wybrany.Przedmiot.ZnacznikZImportu = false;
        Wybrany.Przedmiot.SynchronizujCenaZakupu(_dane.Slowniki, Wybrany.Przedmiot.DataZakupu ?? DateOnly.FromDateTime(DateTime.Today));
        _magazyn.Zapisz(_dane);
        Odswiez();
    }

    /// <summary>Usuwa przedmiot (FR-1202 — cofnięcie; na razie trwałe usunięcie z listy).</summary>
    [RelayCommand]
    private void Usun(WierszPrzedmiotu? wiersz)
    {
        if (wiersz == null)
            return;
        _dane.Przedmioty.Remove(wiersz.Przedmiot);
        _magazyn.Zapisz(_dane);
        Odswiez();
    }

    /// <summary>Łączy wybrany przedmiot z innym w komplet (FR-206): wybrany staje się elementem kompletu.</summary>
    [RelayCommand]
    private void DolaczDoKompletu(WierszPrzedmiotu? komplet)
    {
        if (Wybrany == null || komplet == null || komplet.Id == Wybrany.Id)
            return;
        Wybrany.Przedmiot.KompletId = komplet.Id;
        _magazyn.Zapisz(_dane);
        Odswiez();
    }

    /// <summary>Odłącza przedmiot od kompletu (FR-206).</summary>
    [RelayCommand]
    private void OdlaczZKompletu()
    {
        if (Wybrany == null || Wybrany.Przedmiot.KompletId == null)
            return;
        Wybrany.Przedmiot.KompletId = null;
        _magazyn.Zapisz(_dane);
        Odswiez();
    }

    /// <summary>Montuje przedmiot na nośniku (FR-207; nadpisuje poprzedni montaż).</summary>
    [RelayCommand]
    private void MontujNa(WierszPrzedmiotu? nosnik)
    {
        if (Wybrany == null || nosnik == null)
            return;
        Wybrany.Przedmiot.ZamontowanyNaId = nosnik.Id;
        _magazyn.Zapisz(_dane);
        Odswiez();
    }

    /// <summary>Zdejmuje przedmiot z nośnika (FR-207).</summary>
    [RelayCommand]
    private void Zdejmij()
    {
        if (Wybrany == null)
            return;
        Wybrany.Przedmiot.ZamontowanyNaId = null;
        _magazyn.Zapisz(_dane);
        Odswiez();
    }

    /// <summary>Nośniki (kategorie oznaczone jako nośnik, FR-207) z zamontowanymi elementami.</summary>
    public IReadOnlyList<Przedmiot> Nosniki =>
        _dane.Slowniki.Kategorie.Where(k => k.CzyNosnik)
            .SelectMany(k => _dane.Przedmioty.Where(p => p.KategoriaId == k.Id))
            .ToList();

    /// <summary>Elementy zamontowane na nośniku.</summary>
    public IReadOnlyList<Przedmiot> ZamontowaneNa(string nosnikId) =>
        _dane.Przedmioty.Where(p => p.ZamontowanyNaId == nosnikId).ToList();

    /// <summary>Elementy kompletu (FR-206).</summary>
    public IReadOnlyList<Przedmiot> ElementyKompletu(string kompletId) =>
        _dane.Przedmioty.Where(p => p.KompletId == kompletId).ToList();

    /// <summary>Edycja zbiorcza zaznaczonych (FR-216): zmienia wspólne pola jednym poleceniem.</summary>
    [RelayCommand]
    private void EdytujZbiorczo()
    {
        var zaznaczone = Wiersze.Where(w => w.Zaznaczony).Select(w => w.Przedmiot).ToList();
        if (zaznaczone.Count == 0)
            return;
        if (EdytowanyMiejsceId != null)
            foreach (var p in zaznaczone) p.MiejsceId = EdytowanyMiejsceId;
        if (EdytowanyWzorId != null)
            foreach (var p in zaznaczone) p.WzorId = EdytowanyWzorId;
        if (EdytowanyPochodzenieId != null)
            foreach (var p in zaznaczone) p.PochodzenieId = EdytowanyPochodzenieId;
        if (EdytowanyStatus != null)
            foreach (var p in zaznaczone) p.Status = EdytowanyStatus.Value;
        foreach (var p in zaznaczone)
            foreach (var stronaId in EdytowaneStronyId)
                if (!p.StronyId.Contains(stronaId))
                    p.StronyId.Add(stronaId);
        _magazyn.Zapisz(_dane);
        Odswiez();
    }

    /// <summary>Wartości formularza edycji zbiorczej (FR-216); null = nie zmieniaj pola.</summary>
    [ObservableProperty]
    private string? _edytowanyMiejsceId;

    [ObservableProperty]
    private string? _edytowanyWzorId;

    [ObservableProperty]
    private string? _edytowanyPochodzenieId;

    [ObservableProperty]
    private StatusWlasnosci? _edytowanyStatus;

    [ObservableProperty]
    private List<string> _edytowaneStronyId = new();
}
