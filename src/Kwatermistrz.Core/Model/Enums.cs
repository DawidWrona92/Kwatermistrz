namespace Kwatermistrz.Core.Model;

/// <summary>Stan wypełnienia pola karty technicznej (FR-404). Czwarty stan „nie uzupełniono” to wartość domyślna.</summary>
public enum StanWartosciPola
{
    NieUzupelnione = 0,
    Wypelnione = 1,
    Nieznane = 2,
    NieDotyczy = 3,
}

/// <summary>Status własności repliki lub przedmiotu (FR-209, FR-305).</summary>
public enum StatusWlasnosci
{
    Posiadany = 0,
    DoSprzedazy = 1,
    Sprzedany = 2,
    Wycofany = 3,
}

/// <summary>Znaczenie rodzaju pozycji kosztowej dla reguł BR-04. Etykiety rodzajów są słownikiem, semantyka jest stała (NFR-14).</summary>
public enum SemantykaKosztu
{
    Zakup = 0,
    Ulepszenie = 1,
    Serwis = 2,
    Zwrot = 3,
    Inne = 4,
}

/// <summary>Znaczenie rodzaju zadania dla filtrów i podpowiedzi (FR-602, FR-604).</summary>
public enum SemantykaZadania
{
    Awaria = 0,
    DoZrobienia = 1,
    DoKupienia = 2,
}

/// <summary>Status zadania (FR-601).</summary>
public enum StatusZadania
{
    Otwarte = 0,
    Zamkniete = 1,
}

/// <summary>Wynik testu działania repliki (FR-408).</summary>
public enum WynikTestu
{
    Sprawna = 0,
    Usterka = 1,
}

/// <summary>Rodzaj elementu użytego w składzie zestawu lub wyprawy.</summary>
public enum TypElementu
{
    Przedmiot = 0,
    Replika = 1,
    Komplet = 2,
}

/// <summary>Motyw interfejsu (FR-1006).</summary>
public enum Motyw
{
    Systemowy = 0,
    Jasny = 1,
    Ciemny = 2,
}
