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
    /// <summary>Komunikat paska stanu (wyniki eksportów).</summary>
    [ObservableProperty]
    private string _komunikat = "";

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

    /// <summary>Wiersz konfiguracji nośnika do wyboru w wariantach (FR-208): nośnik + nazwa konfiguracji.</summary>
    public IReadOnlyList<WierszKonfiguracjiNosnika> KonfiguracjeNosnikow => _dane.Przedmioty
        .Where(p => p.Konfiguracje.Count > 0)
        .SelectMany(p => p.Konfiguracje.Select(k => new WierszKonfiguracjiNosnika(p, k)))
        .OrderBy(w => w.NazwaPelna, StringComparer.CurrentCulture)
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
        OdswiezPakowanie();
        if (wiersz != null)
        {
            wiersz.Zestaw.ZnacznikZImportu = false;
            _magazyn.ZapiszDebounce(_dane);
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
                    zestaw.Sloty.Add(new SlotZestawu { Nazwa = slot.Nazwa, Kolejnosc = slot.Kolejnosc, SzablonSlotId = slot.Id });
            }
        }
        _dane.Zestawy.Add(zestaw);
        _magazyn.ZapiszDebounce(_dane);
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
        _magazyn.ZapiszDebounce(_dane);
        Odswiez();
        OdswiezPakowanie();
    }

    /// <summary>Usuwa zestaw (FR-1202).</summary>
    [RelayCommand]
    private void Usun(WierszZestawu? wiersz)
    {
        if (wiersz == null)
            return;
        _dane.Zestawy.Remove(wiersz.Zestaw);
        _magazyn.ZapiszDebounce(_dane);
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
        _magazyn.ZapiszDebounce(_dane);
    }

    /// <summary>Usuwa slot (tylko gdy nie ma wariantów; FR-702).</summary>
    [RelayCommand]
    private void UsunSlot(SlotZestawu? slot)
    {
        if (Wybrany == null || slot == null)
            return;
        Wybrany.Zestaw.Sloty.Remove(slot);
        _magazyn.ZapiszDebounce(_dane);
    }

    /// <summary>Dodaje wariant do slotu (FR-703); domyślnie pierwszy element lub „puste”.</summary>
    [RelayCommand]
    private void DodajWariant(SlotZestawu? slot)
    {
        if (Wybrany == null || slot == null)
            return;
        slot.Warianty.Add(new WariantSlotu());
        _magazyn.ZapiszDebounce(_dane);
    }

    /// <summary>Usuwa wariant (FR-703).</summary>
    [RelayCommand]
    private void UsunWariant(WariantSlotu? wariant)
    {
        if (Wybrany == null || wariant == null)
            return;
        foreach (var slot in Wybrany.Zestaw.Sloty)
            slot.Warianty.Remove(wariant);
        _magazyn.ZapiszDebounce(_dane);
    }

    /// <summary>Ustawia wariant domyślny slotu (FR-704) — po Id wariantu (nie po indeksie).</summary>
    [RelayCommand]
    private void UstawDomyslny(object? parametry)
    {
        if (Wybrany == null || parametry is not object[] { Length: 2 } tablica)
            return;
        if (tablica[0] is SlotZestawu slot && tablica[1] is WariantSlotu wariant)
        {
            slot.WariantDomyslnyId = wariant.Id;
            _magazyn.ZapiszDebounce(_dane);
        }
    }

    /// <summary>Dodaje element do wariantu (FR-703).</summary>
    [RelayCommand]
    private void DodajElement(WariantSlotu? wariant)
    {
        if (Wybrany == null || wariant == null)
            return;
        wariant.Elementy.Add(new ElementWariantu { Typ = TypElementu.Przedmiot });
        _magazyn.ZapiszDebounce(_dane);
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
        _magazyn.ZapiszDebounce(_dane);
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
            TypElementu.KonfiguracjaNosnika => NazwaKonfiguracji(element.ElementId),
            _ => "?",
        };
    }

    private string NazwaKonfiguracji(string konfiguracjaId)
    {
        var para = _dane.Przedmioty
            .Where(p => p.Konfiguracje.Count > 0)
            .SelectMany(p => p.Konfiguracje.Select(k => (Nosnik: p, Konfiguracja: k)))
            .FirstOrDefault(x => x.Konfiguracja.Id == konfiguracjaId);
        return para.Konfiguracja != null ? $"{para.Nosnik.Nazwa} — {para.Konfiguracja.Nazwa}" : "?";
    }

    // ---- Lista pakowania (FR-707) ----

    /// <summary>Wiersz listy pakowania: element, miejsce przechowywania, czy spakowany, czy zamontowany na nośniku.</summary>
    public partial class WierszPakowania : ObservableObject
    {
        public WierszPakowania(string klucz, ElementWariantu element, string nazwa, string? miejsceId, string? nosnikNazwa)
        {
            Klucz = klucz;
            Element = element;
            Nazwa = nazwa;
            MiejsceId = miejsceId;
            NosnikNazwa = nosnikNazwa;
        }

        public string Klucz { get; }
        public ElementWariantu Element { get; }
        public string Nazwa { get; }
        public string? MiejsceId { get; }
        public string? NosnikNazwa { get; }
        /// <summary>Czy spakowany (odhaczenie na liście, FR-707) — zapisywane w zestawie.</summary>
        [ObservableProperty]
        private bool _spakowany;
    }

    /// <summary>Grupy listy pakowania wg miejsca przechowywania (FR-707).</summary>
    public partial class GrupaPakowania : ObservableObject
    {
        public GrupaPakowania(string nazwaMiejsca)
        {
            NazwaMiejsca = nazwaMiejsca;
        }

        public string NazwaMiejsca { get; }
        public ObservableCollection<WierszPakowania> Wiersze { get; } = new();
        /// <summary>Liczba spakowanych / liczba wszystkich.</summary>
        public string Postep => $"{Wiersze.Count(w => w.Spakowany)}/{Wiersze.Count}";

        /// <summary>Powiadamia o zmianie postępu (spakowane/wсе).</summary>
        public void OdswiezPostep() => OnPropertyChanged(nameof(Postep));
    }

    /// <summary>Grupy listy pakowania dla wybranego zestawu (warianty domyślne), pogrupowane wg miejsca przechowywania (FR-707).</summary>
    public ObservableCollection<GrupaPakowania> GrupyPakowania { get; } = new();

    /// <summary>Koszt zestawu (FR-708): warianty domyślne.</summary>
    public decimal KosztZestawu { get; private set; }

    /// <summary>Zakres kosztu zestawu min–max po wszystkich wariantach (FR-708 [C]).</summary>
    public string ZakresKosztu { get; private set; } = "";

    /// <summary>Buduje listę pakowania dla wybranego zestawu (FR-707) i koszty (FR-708). Wywoływane przy wyborze/zapisie.</summary>
    public void OdswiezPakowanie()
    {
        GrupyPakowania.Clear();
        KosztZestawu = 0;
        ZakresKosztu = "";
        if (Wybrany == null)
        {
            OnPropertyChanged(nameof(KosztZestawu));
            OnPropertyChanged(nameof(ZakresKosztu));
            return;
        }
        var kalk = _kalkulator.Kalkulator();
        var elementy = kalk.ElementyListyPakowania(Wybrany.Zestaw);
        KosztZestawu = kalk.KosztZestawu(Wybrany.Zestaw);
        var (min, max) = kalk.ZakresKosztuZestawu(Wybrany.Zestaw);
        ZakresKosztu = min == max ? "" : $" (zakres {min:0}–{max:0} zł)";

        var grupy = new Dictionary<string, GrupaPakowania>(StringComparer.CurrentCulture);
        foreach (var (element, slot, indeksWariantu) in elementy)
        {
            var przedmiot = element.Typ == TypElementu.Przedmiot || element.Typ == TypElementu.Komplet
                ? _dane.Przedmioty.FirstOrDefault(p => p.Id == element.ElementId)
                : null;
            var replika = element.Typ == TypElementu.Replika
                ? _dane.Repliki.FirstOrDefault(r => r.Id == element.ElementId)
                : null;
            var miejsceId = przedmiot?.MiejsceId;
            var miejsceNazwa = miejsceId != null
                ? _dane.Slowniki.Miejsca.FirstOrDefault(m => m.Id == miejsceId)?.Nazwa ?? "Bez miejsca"
                : "Bez miejsca";
            var nosnik = przedmiot?.ZamontowanyNaId != null
                ? _dane.Przedmioty.FirstOrDefault(p => p.Id == przedmiot.ZamontowanyNaId)
                : null;
            var klucz = $"{slot.Id}|{indeksWariantu}|{(int)element.Typ}|{element.ElementId}";
            var wiersz = new WierszPakowania(klucz, element, NazwaElementu(element), miejsceId, nosnik?.Nazwa)
            {
                Spakowany = Wybrany.Zestaw.SpakowaneKlucze.Contains(klucz),
            };
            if (!grupy.TryGetValue(miejsceNazwa, out var grupa))
            {
                grupa = new GrupaPakowania(miejsceNazwa);
                grupy[miejsceNazwa] = grupa;
                GrupyPakowania.Add(grupa);
            }
            grupa.Wiersze.Add(wiersz);
        }
        foreach (var g in GrupyPakowania)
            g.Wiersze.CollectionChanged += (_, _) => g.OdswiezPostep();
        OnPropertyChanged(nameof(KosztZestawu));
        OnPropertyChanged(nameof(ZakresKosztu));
    }

    /// <summary>Odwraca „spakowany” na liście pakowania i zapisuje (FR-707).</summary>
    [RelayCommand]
    private void PrzelaczSpakowany(WierszPakowania? wiersz)
    {
        if (Wybrany == null || wiersz == null)
            return;
        if (wiersz.Spakowany)
            Wybrany.Zestaw.SpakowaneKlucze.Add(wiersz.Klucz);
        else
            Wybrany.Zestaw.SpakowaneKlucze.Remove(wiersz.Klucz);
        _magazyn.ZapiszDebounce(_dane);
        foreach (var g in GrupyPakowania)
            g.OdswiezPostep();
    }

    /// <summary>Eksportuje listę pakowania do CSV (FR-707).</summary>
    [RelayCommand]
    private void EksportujListePakowania()
    {
        if (Wybrany == null)
            return;
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = (string.IsNullOrWhiteSpace(Wybrany.Zestaw.Nazwa) ? "lista-pakowania" : Wybrany.Zestaw.Nazwa) + ".csv",
                DefaultExt = ".csv",
            };
            if (dialog.ShowDialog() != true)
                return;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Miejsce;Element;Spakowany");
            foreach (var g in GrupyPakowania)
                foreach (var w in g.Wiersze)
                    sb.Append(Csv(g.NazwaMiejsca)).Append(';').Append(Csv(w.Nazwa)).Append(';')
                      .AppendLine(w.Spakowany ? "tak" : "nie");
            System.IO.File.WriteAllText(dialog.FileName, sb.ToString(), System.Text.Encoding.UTF8);
            Komunikat = $"Wyeksportowano listę pakowania: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            // eksport nie może przepaść cicho — użytkownik ma wiedzieć o błędzie (inaczej myśli, że zapisał)
            Komunikat = "Błąd eksportu listy pakowania: " + ex.Message;
        }
    }

    /// <summary>Zabezpiecza pole CSV: cudzysłowy przy separatorze/cudzysłowie/nowej linii oraz apostrof
    /// przed formułą Excela (=, +, -, @ na początku) — klasyczny CSV formula injection.</summary>
    private static string Csv(string? tekst)
    {
        var t = tekst ?? "";
        if (t.Length > 0 && (t[0] == '=' || t[0] == '+' || t[0] == '-' || t[0] == '@' || t[0] == '\t'))
            t = "'" + t;
        return t.Contains(';') || t.Contains('"') || t.Contains('\n') ? "\"" + t.Replace("\"", "\"\"") + "\"" : t;
    }
}

/// <summary>Wiersz konfiguracji nośnika w selektorze wariantu zestawu (FR-208): „nośnik — konfiguracja”.</summary>
public class WierszKonfiguracjiNosnika
{
    private readonly Przedmiot _nosnik;
    private readonly KonfiguracjaNosnika _konfiguracja;

    public WierszKonfiguracjiNosnika(Przedmiot nosnik, KonfiguracjaNosnika konfiguracja)
    {
        _nosnik = nosnik;
        _konfiguracja = konfiguracja;
    }

    /// <summary>Id konfiguracji (jako Id elementu wariantu).</summary>
    public string Id => _konfiguracja.Id;
    public string NazwaPelna => $"{_nosnik.Nazwa} — {_konfiguracja.Nazwa}";
    public string NosnikId => _nosnik.Id;
    public IReadOnlyList<string> ElementyId => _konfiguracja.ElementyId;
}
