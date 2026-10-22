namespace Kwatermistrz.Core.Model;

/// <summary>Całość danych aplikacji — jeden dokument JSON (decyzja 02, NFR-03).</summary>
public class DaneAplikacji
{
    /// <summary>Wersja schematu danych; migracje wykrywają starsze wartości (NFR-06).</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Wersja reguł obliczeń widoczna w „O programie” i w eksportach (NFR-14). Zmieniana wyłącznie z nową wersją aplikacji.</summary>
    public string WersjaRegul { get; set; } = "1.0";

    public Slowniki Slowniki { get; set; } = new();
    public List<Przedmiot> Przedmioty { get; set; } = new();
    public List<Replika> Repliki { get; set; } = new();
    public List<Zestaw> Zestawy { get; set; } = new();
    public List<Wyprawa> Wyprawy { get; set; } = new();
    public List<Rekonstrukcja> Rekonstrukcje { get; set; } = new();
    public List<SzablonKarty> SzablonyKart { get; set; } = new();
    public List<Zadanie> Zadania { get; set; } = new();
    public Ustawienia Ustawienia { get; set; } = new();
    /// <summary>Czy ukończono kreator pierwszego uruchomienia (FR-1401).</summary>
    public bool CzyPierwszeUruchomienie { get; set; } = true;
    public DateTime OstatniaZmiana { get; set; } = DateTime.Now;
}
