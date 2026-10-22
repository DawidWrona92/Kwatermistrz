using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Import;

/// <summary>Rodzaj wątpliwości pokazywanej w podglądzie importu (FR-1102, rozdział 13).</summary>
public enum RodzajKonfliktu
{
    /// <summary>Dwie pozycje źródłowe pasują do jednego rekordu (np. duplikaty kolumn karty, 13.3).</summary>
    Duplikat,
    /// <summary>Nazwa nie pasuje jednoznacznie do katalogu (wiele kandydatów).</summary>
    Niejednoznaczne,
    /// <summary>Nazwa nie ma odpowiednika w katalogu — powstanie szkic (FR-706, 13.6).</summary>
    BrakOdpowiednika,
    /// <summary>Wartość wymaga potwierdzenia autora (np. literówka 13.6).</summary>
    DoPotwierdzenia,
    /// <summary>Informacja o pominiętych danych (np. nieaktualne liczniki mundurów, 13.2).</summary>
    Pominiete,
}

/// <summary>Pojedyncza pozycja konfliktu/wątpliwości do rozstrzygnięcia przed importem (FR-1102).</summary>
public class KonfliktImportu
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public RodzajKonfliktu Rodzaj { get; set; }
    /// <summary>Opis sytuacji widoczny użytkownikowi, po polsku.</summary>
    public string Opis { get; set; } = "";
    /// <summary>Kontekst: arkusz i wiersz/kolumna źródła.</summary>
    public string Zrodlo { get; set; } = "";
    /// <summary>Kandydaci do wyboru (nazwy), gdy dotyczy.</summary>
    public List<string> Kandydaci { get; set; } = new();
    /// <summary>Indeks domyślnie wybranego kandydata; -1 gdy wybór nie dotyczy.</summary>
    public int DomyslnyWybor { get; set; } = -1;
    /// <summary>Wybrany przez użytkownika indeks; null = decyzja domyślna.</summary>
    public int? WyborUzytkownika { get; set; }
    /// <summary>Prawda, gdy użytkownik zdecydował „pomiń”.</summary>
    public bool Pomin { get; set; }
    /// <summary>Rozwiązana decyzja: indeks kandydata albo -1 dla pominięcia.</summary>
    public int Decyzja => Pomin ? -1 : (WyborUzytkownika ?? DomyslnyWybor);
}

/// <summary>Podgląd importu: ile rekordów każdego typu powstanie, lista konfliktów i wątpliwości (FR-1102). Nic nie jest zapisywane przed zatwierdzeniem.</summary>
public class PodgladImportu
{
    /// <summary>Liczba planowanych rekordów wg typu (klucz: nazwa typu po polsku).</summary>
    public Dictionary<string, int> LiczbaRekordow { get; set; } = new();
    public List<KonfliktImportu> Konflikty { get; set; } = new();
    /// <summary>Importowane dane gotowe do wstawienia po zatwierdzeniu podglądu.</summary>
    public DaneAplikacji? Dane { get; set; }
    /// <summary>Data dnia zerowego ustawiona przy imporcie (FR-807).</summary>
    public DateOnly DzienZerowy { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Dodaje konflikt. Deduplikacja tylko dla tego samego źródła, rodzaju i opisu — różne pozycje źródłowe (różne ładownice) to osobne decyzje.</summary>
    public KonfliktImportu DodajKonflikt(RodzajKonfliktu rodzaj, string opis, string zrodlo)
    {
        var istniejacy = Konflikty.FirstOrDefault(k => k.Zrodlo == zrodlo && k.Rodzaj == rodzaj && k.Opis == opis);
        if (istniejacy != null)
            return istniejacy;
        var konflikt = new KonfliktImportu { Rodzaj = rodzaj, Opis = opis, Zrodlo = zrodlo };
        Konflikty.Add(konflikt);
        return konflikt;
    }
}
