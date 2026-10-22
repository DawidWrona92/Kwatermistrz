using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Obliczenia;

/// <summary>
/// Indeks obliczeniowy na czas odświeżenia listy (NFR-04): raz rozwiazuje przecięcia
/// wypraw z replikami i przedmiotami do słowników, zamiast liczyć O(P·W) na każdy wiersz.
/// Budowa: O(W + P + suma elementów zestawów użytych w wyprawach).
/// </summary>
public class IndeksObliczeniowy
{
    private readonly Dictionary<string, Przedmiot> _przedmiotyWgId;
    private readonly Dictionary<string, int> _wyprawyWgRepliki = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _uzyciaWgPrzedmiotu = new(StringComparer.Ordinal);

    /// <summary>Łączna liczba wypraw (BR-02): suma liczników replik + wyprawy bez repliki głównej.</summary>
    public int LacznaLiczbaWypraw { get; private set; }

    public IndeksObliczeniowy(DaneAplikacji dane)
    {
        var kalk = new Kalkulator(dane);
        _przedmiotyWgId = dane.Przedmioty.ToDictionary(p => p.Id, StringComparer.Ordinal);

        foreach (var wyprawa in dane.Wyprawy.Where(w => !w.CzyUsunieta))
        {
            if (wyprawa.ReplikaGlownaId != null)
            {
                _wyprawyWgRepliki.TryGetValue(wyprawa.ReplikaGlownaId, out var liczba);
                _wyprawyWgRepliki[wyprawa.ReplikaGlownaId] = liczba + 1;
            }
            // rozwinięcie składu wyprawy na konkretne przedmioty (BR-08, FR-208)
            if (wyprawa.ZestawId != null)
            {
                var zestaw = dane.Zestawy.FirstOrDefault(z => z.Id == wyprawa.ZestawId);
                if (zestaw != null)
                {
                    foreach (var (slotId, indeks) in wyprawa.WybraneWarianty)
                    {
                        var slot = zestaw.Sloty.FirstOrDefault(s => s.Id == slotId);
                        if (slot == null || indeks < 0 || indeks >= slot.Warianty.Count)
                            continue;
                        foreach (var element in slot.Warianty[indeks].Elementy)
                            foreach (var przedmiotId in kalk.RozwinNaPrzedmioty(element))
                                DodajUzycie(przedmiotId);
                    }
                }
            }
            foreach (var element in wyprawa.SkladReczny)
                foreach (var przedmiotId in kalk.RozwinNaPrzedmioty(element))
                    DodajUzycie(przedmiotId);
        }

        LacznaLiczbaWypraw = dane.Repliki.Sum(r =>
            r.LicznikHistorycznyWypraw + (_wyprawyWgRepliki.TryGetValue(r.Id, out var l) ? l : 0))
            + dane.Wyprawy.Count(w => !w.CzyUsunieta && w.ReplikaGlownaId == null);

        void DodajUzycie(string przedmiotId)
        {
            _uzyciaWgPrzedmiotu.TryGetValue(przedmiotId, out var uzycia);
            _uzyciaWgPrzedmiotu[przedmiotId] = uzycia + 1;
        }
    }

    /// <summary>BR-01: wyprawy repliki = licznik historyczny + zapisane wyprawy.</summary>
    public int WyprawyRepliki(Replika replika)
        => replika.LicznikHistorycznyWypraw + (_wyprawyWgRepliki.TryGetValue(replika.Id, out var l) ? l : 0);

    /// <summary>BR-08: użycia przedmiotu = licznik historyczny + wyprawy ze składu (element kompletu dzieli licznik kompletu).</summary>
    public int UzyciaPrzedmiotu(Przedmiot przedmiot)
    {
        // element kompletu ma wspólny licznik z kompletem (BR-08); pętla zamiast rekurencji — dane mogą zawierać cykl kompletów (A↔B), który przy rekurencji dawałby StackOverflow
        var aktualny = przedmiot;
        var odwiedzone = new HashSet<string>(StringComparer.Ordinal);
        while (aktualny.KompletId != null && aktualny.KompletId != aktualny.Id
            && odwiedzone.Add(aktualny.Id)
            && _przedmiotyWgId.TryGetValue(aktualny.KompletId, out var komplet))
        {
            aktualny = komplet;
        }
        return aktualny.LicznikHistoryczny + (_uzyciaWgPrzedmiotu.TryGetValue(aktualny.Id, out var u) ? u : 0);
    }

    /// <summary>BR-03: procent wypraw repliki względem łącznej liczby wypraw; przy zerze wynosi 0.</summary>
    public decimal ProcentWypraw(int wyprawyRepliki)
        => LacznaLiczbaWypraw == 0 ? 0m : (decimal)wyprawyRepliki / LacznaLiczbaWypraw * 100m;
}
