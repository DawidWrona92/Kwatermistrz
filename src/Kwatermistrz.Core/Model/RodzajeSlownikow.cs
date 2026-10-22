namespace Kwatermistrz.Core.Model;

/// <summary>Metadane pojedynczego słownika edytowalnego: klucz, nazwa wyświetlana i dostęp do listy wartości (FR-1001). Używane przez pakiety konfiguracji (FR-1404) i scalanie wartości (FR-1003).</summary>
public class RodzajSlownika
{
    public required string Klucz { get; init; }
    public required string Nazwa { get; init; }
    public required Func<Slowniki, List<WartoscSlownika>> Pobierz { get; init; }
    public required Action<Slowniki, List<WartoscSlownika>> Ustaw { get; init; }

}

/// <summary>Rejestr wszystkich słowników edytowalnych (FR-1001).</summary>
public static class RodzajeSlownikow
{
    public static IReadOnlyList<RodzajSlownika> Wszystkie { get; } = new[]
    {
        new RodzajSlownika { Klucz = "Strony", Nazwa = "Strony", Pobierz = s => s.Strony, Ustaw = (s, l) => s.Strony = l },
        new RodzajSlownika { Klucz = "Miejsca", Nazwa = "Miejsca przechowywania", Pobierz = s => s.Miejsca, Ustaw = (s, l) => s.Miejsca = l },
        new RodzajSlownika { Klucz = "Role", Nazwa = "Role replik", Pobierz = s => s.Role, Ustaw = (s, l) => s.Role = l },
        new RodzajSlownika { Klucz = "TypyMagazynkow", Nazwa = "Typy magazynków", Pobierz = s => s.TypyMagazynkow, Ustaw = (s, l) => s.TypyMagazynkow = l },
        new RodzajSlownika { Klucz = "Napedy", Nazwa = "Napędy", Pobierz = s => s.Napedy, Ustaw = (s, l) => s.Napedy = l },
        new RodzajSlownika { Klucz = "StanyReplik", Nazwa = "Stany replik", Pobierz = s => s.StanyReplik, Ustaw = (s, l) => s.StanyReplik = l },
        new RodzajSlownika { Klucz = "Pochodzenie", Nazwa = "Pochodzenie / klasa", Pobierz = s => s.Pochodzenie, Ustaw = (s, l) => s.Pochodzenie = l },
        new RodzajSlownika { Klucz = "RodzajeKosztow", Nazwa = "Rodzaje kosztów", Pobierz = s => s.RodzajeKosztow, Ustaw = (s, l) => s.RodzajeKosztow = l },
        new RodzajSlownika { Klucz = "RodzajeZadan", Nazwa = "Rodzaje zadań", Pobierz = s => s.RodzajeZadan, Ustaw = (s, l) => s.RodzajeZadan = l },
    };

    public static RodzajSlownika? ZnajdzPoKluczu(string klucz)
        => Wszystkie.FirstOrDefault(r => r.Klucz == klucz);
}
