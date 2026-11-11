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
        var znaneWgRoku = dane.Wyprawy
            .Where(w => !w.CzyUsunieta && w.CzyHistoryczna && w.RokHistoryczny is not null)
            .GroupBy(w => w.RokHistoryczny!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Zakres numerów danego roku jest stały, nawet po rozpoznaniu kilku
        // jego wypraw. Inaczej Wyprawa_1 byłaby nadawana ponownie po zapisaniu
        // pierwszego anonimowego rekordu.
        var lata = dane.Ustawienia.HistoryczneWyprawyWgRok.Keys
            .Concat(znaneWgRoku.Keys).Distinct().OrderBy(x => x);
        var poprzednieLata = 0;
        foreach (var rok in lata)
        {
            dane.Ustawienia.HistoryczneWyprawyWgRok.TryGetValue(rok, out var nierozpoznane);
            znaneWgRoku.TryGetValue(rok, out var znane);
            var zakres = Math.Max(0, nierozpoznane) + (znane?.Count ?? 0);
            var zajete = (znane ?? new List<Wyprawa>())
                .Select(w => w.NazwaHistoryczna)
                .Where(n => n != null && n.StartsWith("Wyprawa_", StringComparison.Ordinal))
                .Select(n => int.TryParse(n!["Wyprawa_".Length..], out var numer) ? numer : -1)
                .ToHashSet();
            var dodane = 0;
            for (var i = 1; i <= zakres && dodane < nierozpoznane && i <= 100000; i++)
            {
                var numer = poprzednieLata + i;
                if (zajete.Contains(numer))
                    continue;
                wynik.Add(new PozycjaHistoryczna(
                    $"hist:{rok}:{i}", $"Wyprawa_{numer}", rok, numer));
                dodane++;
            }
            poprzednieLata += zakres;
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
        // Jeśli użytkownik rozpoznał jedynie datę, przypisanie repliki jest
        // wciąż nieznane: wydarzenie nadal znajduje się w starym łącznym
        // liczniku replik. Nie wolno naliczyć go drugi raz.
        var historiaBezRepliki = dane.Wyprawy.Count(w => !w.CzyUsunieta &&
            (w.CzyHistoryczna || w.OdliczonaOdLicznikow) && w.ReplikaGlownaId == null);
        wedlugReplik = Math.Max(0, wedlugReplik - historiaBezRepliki);
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
            HistorycznieOdliczonaReplikaId = replika?.LicznikHistorycznyWypraw > 0 ? replika.Id : null,
        };
        var przedmioty = ZnajdzPrzedmiotyZestawu(dane, wyprawa);
        var odliczone = przedmioty.Where(p => p.LicznikHistoryczny > 0)
            .Select(p => p.Id).Distinct().ToList();
        wyprawa.HistorycznieOdliczonePrzedmiotyId = odliczone;
        // Mutacja dopiero po walidacji. Wszystkie źródła liczników są pomniejszane
        // tylko o faktycznie skonsumowane wystąpienia historyczne.
        dane.Ustawienia.HistoryczneWyprawyWgRok[wpis.Rok]--;
        if (wyprawa.HistorycznieOdliczonaReplikaId != null)
            replika!.LicznikHistorycznyWypraw--;
        foreach (var p in przedmioty.Where(p => odliczone.Contains(p.Id)))
            p.LicznikHistoryczny--;
        dane.Wyprawy.Add(wyprawa);
        return wyprawa;
    }

    /// <summary>Wybrane domyślne warianty zestawu i elementy użyte w wyprawie.</summary>
    private static List<Przedmiot> ZnajdzPrzedmiotyZestawu(DaneAplikacji dane, Wyprawa wyprawa)
    {
        var zestaw = dane.Zestawy.FirstOrDefault(z => z.Id == wyprawa.ZestawId);
        if (zestaw == null)
            return new List<Przedmiot>();
        var kalkulator = new Kalkulator(dane);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var slot in zestaw.Sloty)
        {
            var wariant = slot.Warianty.FirstOrDefault(w => w.Id == slot.WariantDomyslnyId)
                ?? slot.Warianty.FirstOrDefault();
            if (wariant == null)
                continue;
            wyprawa.WybraneWarianty[slot.Id] = wariant.Id;
            foreach (var element in wariant.Elementy)
                foreach (var id in kalkulator.RozwinNaPrzedmioty(element))
                    ids.Add(id);
        }
        return dane.Przedmioty.Where(p => ids.Contains(p.Id)).ToList();
    }

    /// <summary>Przywraca anonimowe wystąpienie po odrzuceniu błędnego przypisania.</summary>
    public static void CofnijRozpoznanie(DaneAplikacji dane, Wyprawa wyprawa)
    {
        if (!wyprawa.CzyHistoryczna || wyprawa.CzyUsunieta || wyprawa.RokHistoryczny == null)
            throw new InvalidOperationException("To nie jest aktywna wyprawa z identyfikacji.");
        var rok = wyprawa.RokHistoryczny.Value;
        dane.Ustawienia.HistoryczneWyprawyWgRok.TryGetValue(rok, out var liczba);
        dane.Ustawienia.HistoryczneWyprawyWgRok[rok] = liczba + 1;
        var replika = dane.Repliki.FirstOrDefault(r => r.Id == wyprawa.HistorycznieOdliczonaReplikaId);
        if (replika != null)
            replika.LicznikHistorycznyWypraw++;
        foreach (var id in wyprawa.HistorycznieOdliczonePrzedmiotyId.Distinct())
        {
            var przedmiot = dane.Przedmioty.FirstOrDefault(p => p.Id == id);
            if (przedmiot != null)
                przedmiot.LicznikHistoryczny++;
        }
        wyprawa.CzyUsunieta = true;
    }

    /// <summary>Uzupełnia wcześniej częściowo rozpoznaną wyprawę bez ponownego zmniejszania licznika rocznego.</summary>
    public static void Dopelnij(DaneAplikacji dane, Wyprawa wyprawa,
        DateOnly? data, string? replikaId, string? zestawId)
    {
        if (!wyprawa.CzyHistoryczna || wyprawa.CzyUsunieta || wyprawa.RokHistoryczny == null)
            throw new InvalidOperationException("Wybrany wpis nie jest historyczną wyprawą.");
        if (data != null && data.Value.Year != wyprawa.RokHistoryczny)
            throw new InvalidOperationException("Wybrana data nie należy do roku historycznego.");
        if (wyprawa.ReplikaGlownaId != null && wyprawa.ReplikaGlownaId != replikaId)
            throw new InvalidOperationException("Zmiana już uzgodnionej repliki wymaga cofnięcia przypisania.");
        if (wyprawa.ZestawId != null && wyprawa.ZestawId != zestawId)
            throw new InvalidOperationException("Zmiana już uzgodnionego zestawu wymaga cofnięcia przypisania.");
        var replika = replikaId == null ? null : dane.Repliki.FirstOrDefault(r => r.Id == replikaId);
        if (replikaId != null && replika == null)
            throw new InvalidOperationException("Nie ma wskazanej repliki.");
        if (zestawId != null && !dane.Zestawy.Any(z => z.Id == zestawId))
            throw new InvalidOperationException("Nie ma wskazanego zestawu.");
        if (wyprawa.ReplikaGlownaId == null && replika != null &&
            replika.LicznikHistorycznyWypraw == 0 &&
            dane.Repliki.Any(r => r.LicznikHistorycznyWypraw > 0))
            throw new InvalidOperationException("Replika nie posiada wolnego historycznego użycia.");

        if (wyprawa.ReplikaGlownaId == null && replika != null)
        {
            if (replika.LicznikHistorycznyWypraw > 0)
            {
                replika.LicznikHistorycznyWypraw--;
                wyprawa.HistorycznieOdliczonaReplikaId = replika.Id;
            }
            wyprawa.ReplikaGlownaId = replika.Id;
        }
        if (wyprawa.ZestawId == null && zestawId != null)
        {
            wyprawa.ZestawId = zestawId;
            var przedmioty = ZnajdzPrzedmiotyZestawu(dane, wyprawa);
            foreach (var p in przedmioty.Where(p => p.LicznikHistoryczny > 0))
            {
                p.LicznikHistoryczny--;
                wyprawa.HistorycznieOdliczonePrzedmiotyId.Add(p.Id);
            }
        }
        if (data != null)
            wyprawa.Data = data.Value;
    }

    /// <summary>Niedopasowane liczby nie mogą być po cichu traktowane jako nowe wyprawy.</summary>
    public static int RozbieznoscLicznikow(DaneAplikacji dane)
        => dane.Ustawienia.HistoryczneWyprawyWgRok.Values.Sum()
           - Math.Max(0, dane.Repliki.Sum(r => r.LicznikHistorycznyWypraw)
               - dane.Wyprawy.Count(w => !w.CzyUsunieta &&
                   (w.CzyHistoryczna || w.OdliczonaOdLicznikow) && w.ReplikaGlownaId == null));
}
