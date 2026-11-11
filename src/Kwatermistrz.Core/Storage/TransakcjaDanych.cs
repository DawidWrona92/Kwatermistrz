using System.Reflection;
using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Storage;

/// <summary>
/// Jeden współdzielony dokument danych. Import jest wykonywany na odłączonej kopii,
/// a po sukcesie scala wyniki do istniejącego grafu obiektów, po stabilnych Id.
/// WPF może wtedy dalej wskazywać istniejące rekordy (bez zmiany formatu JSON).
/// </summary>
public static class TransakcjaDanych
{
    public static TResult Wykonaj<TResult>(DaneAplikacji zrodlo, Func<DaneAplikacji, TResult> akcja)
    {
        ArgumentNullException.ThrowIfNull(zrodlo);
        ArgumentNullException.ThrowIfNull(akcja);

        using var strumien = new MemoryStream();
        MagazynJson.ZapiszDoStrumienia(zrodlo, strumien);
        strumien.Position = 0;
        var robocza = MagazynJson.WczytajZeStrumienia(strumien);

        // Importer nie ma dostępu do oryginalnego grafu. Wyjątek przed commit
        // pozostawia dokument oraz wszystkie jego istniejące rekordy nietknięte.
        var wynik = akcja(robocza);
        Zastap(zrodlo, robocza);
        return wynik;
    }

    /// <summary>
    /// Przywrócenie z pliku lub commit importu: zachowuje instancję dokumentu,
    /// kolekcji oraz istniejących rekordów o tych samych identyfikatorach.
    /// Pozostałe obiekty modelu (np. składy wariantów) są zastępowane wewnątrz rekordu.
    /// </summary>
    public static void Zastap(DaneAplikacji cel, DaneAplikacji nowyStan)
    {
        ArgumentNullException.ThrowIfNull(cel);
        ArgumentNullException.ThrowIfNull(nowyStan);
        if (ReferenceEquals(cel, nowyStan))
            return;

        // Weryfikacja CAŁEGO grafu przed rozpoczęciem przepisywania czegokolwiek.
        // Duplikat Id w późniejszej kolekcji nie może przerwać commitu w połowie.
        SprawdzUnikalneId(cel);
        SprawdzUnikalneId(nowyStan);

        ScalWgId(cel.Przedmioty, nowyStan.Przedmioty, p => p.Id);
        ScalWgId(cel.Repliki, nowyStan.Repliki, r => r.Id);
        ScalWgId(cel.Zestawy, nowyStan.Zestawy, z => z.Id);
        ScalWgId(cel.Wyprawy, nowyStan.Wyprawy, w => w.Id);
        ScalWgId(cel.Rekonstrukcje, nowyStan.Rekonstrukcje, r => r.Id);
        ScalWgId(cel.SzablonyKart, nowyStan.SzablonyKart, s => s.Id);
        ScalWgId(cel.Zadania, nowyStan.Zadania, z => z.Id);

        ScalSlowniki(cel.Slowniki, nowyStan.Slowniki);
        PrzepiszWlasciwosci(cel.Ustawienia, nowyStan.Ustawienia);
        cel.SchemaVersion = nowyStan.SchemaVersion;
        cel.WersjaRegul = nowyStan.WersjaRegul;
        cel.CzyPierwszeUruchomienie = nowyStan.CzyPierwszeUruchomienie;
        cel.OstatniaZmiana = DateTime.Now;
    }

    private static void SprawdzUnikalneId(DaneAplikacji dane)
    {
        static void Sprawdz<T>(IEnumerable<T> elementy, Func<T, string> id, string nazwa)
        {
            var duplikaty = elementy.GroupBy(id, StringComparer.Ordinal)
                .Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplikaty.Count > 0)
                throw new InvalidDataException(
                    $"Duplikaty identyfikatorów w {nazwa}: {string.Join(", ", duplikaty)}");
        }

        Sprawdz(dane.Przedmioty, x => x.Id, "Przedmioty");
        Sprawdz(dane.Repliki, x => x.Id, "Repliki");
        Sprawdz(dane.Zestawy, x => x.Id, "Zestawy");
        Sprawdz(dane.Wyprawy, x => x.Id, "Wyprawy");
        Sprawdz(dane.Rekonstrukcje, x => x.Id, "Rekonstrukcje");
        Sprawdz(dane.Zadania, x => x.Id, "Zadania");
        Sprawdz(dane.SzablonyKart, x => x.Id, "Szablony kart");
        Sprawdz(dane.Slowniki.Strony, x => x.Id, "Słownik stron");
        Sprawdz(dane.Slowniki.Wzory, x => x.Id, "Wzory");
        Sprawdz(dane.Slowniki.Miejsca, x => x.Id, "Miejsca");
        Sprawdz(dane.Slowniki.Role, x => x.Id, "Role");
        Sprawdz(dane.Slowniki.TypyMagazynkow, x => x.Id, "Typy magazynków");
        Sprawdz(dane.Slowniki.Napedy, x => x.Id, "Napędy");
        Sprawdz(dane.Slowniki.StanyReplik, x => x.Id, "Stany replik");
        Sprawdz(dane.Slowniki.Pochodzenie, x => x.Id, "Pochodzenie");
        Sprawdz(dane.Slowniki.RodzajeKosztow, x => x.Id, "Rodzaje kosztów");
        Sprawdz(dane.Slowniki.RodzajeZadan, x => x.Id, "Rodzaje zadań");
        Sprawdz(dane.Slowniki.Kategorie, x => x.Id, "Kategorie");
    }

    private static void ScalSlowniki(Slowniki cel, Slowniki nowe)
    {
        ScalWgId(cel.Strony, nowe.Strony, w => w.Id);
        ScalWgId(cel.Wzory, nowe.Wzory, w => w.Id);
        ScalWgId(cel.Miejsca, nowe.Miejsca, w => w.Id);
        ScalWgId(cel.Role, nowe.Role, w => w.Id);
        ScalWgId(cel.TypyMagazynkow, nowe.TypyMagazynkow, w => w.Id);
        ScalWgId(cel.Napedy, nowe.Napedy, w => w.Id);
        ScalWgId(cel.StanyReplik, nowe.StanyReplik, w => w.Id);
        ScalWgId(cel.Pochodzenie, nowe.Pochodzenie, w => w.Id);
        ScalWgId(cel.RodzajeKosztow, nowe.RodzajeKosztow, w => w.Id);
        ScalWgId(cel.RodzajeZadan, nowe.RodzajeZadan, w => w.Id);
        ScalWgId(cel.Kategorie, nowe.Kategorie, k => k.Id);
    }

    private static void ScalWgId<T>(List<T> cel, List<T> nowe, Func<T, string> id) where T : class
    {
        var wgId = cel.ToDictionary(id, StringComparer.Ordinal);
        var wynik = new List<T>(nowe.Count);
        foreach (var rekord in nowe)
        {
            if (wgId.TryGetValue(id(rekord), out var dotychczasowy))
            {
                PrzepiszWlasciwosci(dotychczasowy, rekord);
                wynik.Add(dotychczasowy);
            }
            else
            {
                wynik.Add(rekord);
            }
        }

        // Tożsamość kolekcji nie zmienia się (także dla powiązanych widoków).
        cel.Clear();
        cel.AddRange(wynik);
    }

    /// <summary>
    /// Aktualizuje tylko istniejący obiekt modelu. Modele mają publiczne pola
    /// właściwości (POCO), więc nie trzeba tworzyć drugiego zestawu ręcznych mapowań.
    /// </summary>
    private static void PrzepiszWlasciwosci<T>(T cel, T nowe) where T : class
    {
        foreach (var pole in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!pole.CanRead || !pole.CanWrite || pole.GetIndexParameters().Length > 0)
                continue;
            pole.SetValue(cel, pole.GetValue(nowe));
        }
    }
}
