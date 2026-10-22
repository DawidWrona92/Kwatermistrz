using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Konfiguracja;

/// <summary>
/// Dołączone szablony konfiguracji (FR-1406 [S]): „Pusta” (minimalna) oraz przykład
/// według układu autora (kategorie i sloty z rozdziału 5) — bez żadnych danych o sprzęcie.
/// </summary>
public static class SzablonyKonfiguracji
{
    /// <summary>Identyfikatory szablonów do wyboru w kreatorze pierwszego uruchomienia.</summary>
    public const string Pusta = "pusta";
    public const string Przyklad = "przyklad";

    /// <summary>Lista szablonów: klucz → nazwa wyświetlana.</summary>
    public static IReadOnlyList<(string Klucz, string Nazwa)> Lista { get; } = new[]
    {
        (Pusta, "Pusta (minimalna)"),
        (Przyklad, "Przykład — współczesna Rosja (kategorie i sloty)"),
    };

    /// <summary>Buduje konfigurację szablonu: słowniki (z kategoriami i wzorami), rekonstrukcję z szablonem slotów, szablony kart. Bez przedmiotów, replik, wypraw i zdjęć (NFR-15).</summary>
    public static (Slowniki Slowniki, Rekonstrukcja Rekonstrukcja, List<SzablonKarty> SzablonyKart) Buduj(string klucz)
    {
        return klucz switch
        {
            Przyklad => Przykladowa(),
            _ => BudujPusta(),
        };
    }

    /// <summary>Szablon „Pusta”: startowe słowniki, jedna rekonstrukcja bez slotów, szablony kart AEG/HPA.</summary>
    public static (Slowniki, Rekonstrukcja, List<SzablonKarty>) BudujPusta()
    {
        var slowniki = StartoweSlowniki.Utworz();
        var rekonstrukcja = new Rekonstrukcja { Nazwa = "Rekonstrukcja", SzablonSlotow = new List<SlotSzablonu>() };
        return (slowniki, rekonstrukcja, new List<SzablonKarty>
        {
            StartoweSlowniki.SzablonKartyAeg(slowniki),
            StartoweSlowniki.SzablonKartyHpa(slowniki),
        });
    }

    /// <summary>
    /// Szablon przykładowy według układu autora: kategorie z arkusza wyposażenia
    /// (FR-202), sloty zestawu z FR-702, wzory kamuflaży i kategorie nośników (Kamizelki, Pasy).
    /// </summary>
    public static (Slowniki, Rekonstrukcja, List<SzablonKarty>) Przykladowa()
    {
        var (slowniki, _, szablonyKart) = BudujPusta();

        // kategorie wg arkusza wyposażenia (8.2: Czapki 4, Hełmy 3, Szalokominiarki 4, Mundury 15,
        // Kamizelki 8, Pasoszelki 3, Pasy 4, Nakolanniki 2, Plecaki 3, Ładownice 60)
        slowniki.Kategorie.Clear();
        (string Nazwa, bool Nosnik)[] kategorie =
        {
            ("Czapki", false), ("Hełmy", false), ("Szalokominiarki", false), ("Mundury", false),
            ("Kamizelki", true), ("Pasoszelki", false), ("Pasy", true), ("Nakolanniki", false),
            ("Plecaki", false), ("Ładownice", false),
        };
        slowniki.Kategorie.AddRange(kategorie.Select((k, i) => new Kategoria
        {
            Nazwa = k.Nazwa,
            CzyNosnik = k.Nosnik,
            Kolejnosc = i,
        }));

        // wzory kamuflaży spotykane w arkuszach (FR-1004: próbki przybliżone, użytkownik poprawia)
        slowniki.Wzory.Clear();
        slowniki.Wzory.AddRange(new[]
        {
            Wzor("Flecktarn", "#8A9A5B", "#5C6E3C", "#3E4A2A"),
            Wzor("WZ. 93", "#7A8B5A", "#4A5D3A", "#2E3B24", "#A8B080"),
            Wzor("Palma", "#C8B060", "#8A7040", "#5A4A28"),
            Wzor("EMR (VKBO)", "#6B7A4A", "#4A5A3A", "#8A9A6A"),
            Wzor("ATACS-FG", "#7A8A6A", "#5A6A4A", "#3A4A30"),
            Wzor("Multicam", "#A8A080", "#7A7A60", "#5A5A48", "#C8C0A8"),
        });

        var rekonstrukcja = new Rekonstrukcja
        {
            Nazwa = "Współczesna Rosja",
            Okres = "2010–2025",
            StronaId = Slowniki.Znajdz(slowniki.Strony, "Rosja")?.Id,
            Opis = "Przykładowa konfiguracja: kategorie, sloty zestawu i szablony kart. Samego sprzętu tu nie ma — to tylko wzór organizacji.",
            SzablonSlotow = StartoweSlowniki.StartoweSloty(),
        };
        return (slowniki, rekonstrukcja, szablonyKart);
    }

    private static WzorKamuflazu Wzor(string nazwa, params string[] kolory)
        => new() { Nazwa = nazwa, KoloryProbki = kolory.ToList() };
}
