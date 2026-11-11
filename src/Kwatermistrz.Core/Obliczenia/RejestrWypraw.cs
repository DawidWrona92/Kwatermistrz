using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Obliczenia;

/// <summary>
/// Jedyny punkt uzgadniania rejestru wypraw i odziedziczonych liczników.
/// Liczniki roczne i liczniki replik opisują TE SAME historyczne zdarzenia:
/// nigdy nie wolno dodawać tych dwóch sum do siebie.
/// </summary>
public static class RejestrWypraw
{
    public sealed record PozycjaHistoryczna(string Id, string Nazwa, int Rok, int Numer);

    /// <summary>Wirtualne, nierozpoznane wyprawy — bez wymyślania dat i relacji.</summary>
    public static IReadOnlyList<PozycjaHistoryczna> Nierozpoznane(DaneAplikacji dane)
    {
        var wynik = new List<PozycjaHistoryczna>();
        var nr = 0;
        foreach (var (rok, liczba) in dane.Ustawienia.HistoryczneWyprawyWgRok
                     .Where(p => p.Value > 0).OrderBy(p => p.Key))
            for (var i = 0; i < Math.Min(liczba, 100000); i++)
            {
                nr++;
                wynik.Add(new PozycjaHistoryczna(
                    $"hist:{rok}:{i + 1}", $"Wyprawa_{nr}", rok, nr));
            }
        return wynik;
    }

    /// <summary>Znane wyprawy + historyczne bez danych. Podział historycznych na lata
    /// i na repliki jest dwiema projekcjami tej samej historii, nie dwoma źródłami zdarzeń.</summary>
    public static int LacznaLiczbaWypraw(DaneAplikacji dane)
    {
        var rzeczywiste = dane.Wyprawy.Count(w => !w.CzyUsunieta);
        var wedlugLat = dane.Ustawienia.HistoryczneWyprawyWgRok.Values.Sum(n => Math.Max(0, n));
        var wedlugReplik = dane.Repliki.Sum(r => Math.Max(0, r.LicznikHistorycznyWypraw));
        return checked(rzeczywiste + Math.Max(wedlugLat, wedlugReplik));
    }

    /// <summary>Rok w historii jest znany nawet gdy konkretna data jest nieznana.</summary>
    public static int Rok(Wyprawa wyprawa)
        => wyprawa.Data == default ? wyprawa.RokHistoryczny ?? 0 : wyprawa.Data.Year;

    /// <summary>
    /// Rozpoznaje pojedynczy historyczny wpis. Zmniejsza licznik roczny oraz, jeśli
    /// wskazano replikę, jej licznik historyczny. Od tego momentu zdarzenie jest
    /// liczone wyłącznie przez Wyprawa; nie następuje podwójne naliczenie.
    /// </summary>
    public static Wyprawa Rozpoznaj(DaneAplikacji dane, string identyfikator,
        DateOnly? data, string? replikaId, string? zestawId)
    {
        var wpis = Nierozpoznane(dane).FirstOrDefault(p => p.Id == identyfikator)
            ?? throw new InvalidOperationException("Wyprawa historyczna nie jest już dostępna. Odśwież listę.");
        if (data.HasValue && data.Value.Year != wpis.Rok)
            throw new InvalidOperationException("Data wyprawy musi należeć do roku historycznego. Rok można zmienić po rozpoznaniu.");
        if (data == null && replikaId == null && zestawId == null)
            throw new InvalidOperationException("Uzupełnij przynajmniej datę, replikę albo zestaw.");
        var replika = replikaId == null ? null : dane.Repliki.SingleOrDefault(r => r.Id == replikaId);
        if (replikaId != null && replika == null)
            throw new InvalidOperationException("Wybrana replika nie istnieje w katalogu.");
        if (zestawId != null && !dane.Zestawy.Any(z => z.Id == zestawId))
            throw new InvalidOperationException("Wybrany zestaw nie istnieje w katalogu.");

        // Gdy historia już przypisuje wszystkie użycia do konkretnych replik,
        // przeniesienie kolejnej wyprawy na replikę o zerowym stanie wymagałoby
        // zgadywania, której innej replice odebrać użycie. Blokujemy to.
        var stanHistorii = dane.Repliki.Sum(r => Math.Max(0, r.LicznikHistorycznyWypraw));
        if (replika != null && replika.LicznikHistorycznyWypraw <= 0 && stanHistorii > 0)
            throw new InvalidOperationException(
                "Ta replika nie ma wolnych historycznych użyć. Najpierw uzgodnij liczniki replik w Konfliktach.");

        var wyprawa = new Wyprawa
        {
            NazwaHistoryczna = wpis.Nazwa,
            RokHistoryczny = wpis.Rok,
            CzyHistoryczna = true,
            Data = data ?? default,
            ReplikaGlownaId = replikaId,
            ZestawId = zestawId,
            Komentarz = "Rozpoznana wyprawa z importowanego rocznego zestawienia.",
        };
        // Mutacja dopiero po całej walidacji.
        dane.Ustawienia.HistoryczneWyprawyWgRok[wpis.Rok]--;
        if (replika != null)
            replika.LicznikHistorycznyWypraw--;
        dane.Wyprawy.Add(wyprawa);
        return wyprawa;
    }

    /// <summary>Niedopasowane liczby nie mogą być po cichu traktowane jako nowe wyprawy.</summary>
    public static int RozbieznoscLicznikow(DaneAplikacji dane)
        => dane.Ustawienia.HistoryczneWyprawyWgRok.Values.Sum()
           - dane.Repliki.Sum(r => r.LicznikHistorycznyWypraw);
}
