using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Import;

/// <summary>Importer początkowy z układu arkuszy autora (FR-1101). Działa na podglądzie: najpierw buduje <see cref="PodgladImportu"/> z konfliktami, dopiero po zatwierdzeniu zapisuje dane (FR-1102).</summary>
public partial class ImporterKwatermistrz
{
    /// <summary>Przygotowuje podgląd importu z pliku .xlsx bez zapisywania czegokolwiek (FR-1102).</summary>
    /// <param name="sciezkaXlsx">Ścieżka do pliku w układzie Dane_zrodlowe_Kwatermistrz.xlsx.</param>
    /// <param name="dzienZerowy">Dzień zerowy ustawiany przy imporcie (FR-807); domyślnie dziś.</param>
    public static PodgladImportu PrzygotujPodglad(string sciezkaXlsx, DateOnly? dzienZerowy = null)
    {
        var arkusze = CzytnikXlsx.Wczytaj(sciezkaXlsx);
        var podglad = new PodgladImportu { DzienZerowy = dzienZerowy ?? DateOnly.FromDateTime(DateTime.Today) };
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz(), CzyPierwszeUruchomienie = false };
        dane.SzablonyKart.Add(StartoweSlowniki.SzablonKartyAeg(dane.Slowniki));
        dane.SzablonyKart.Add(StartoweSlowniki.SzablonKartyHpa(dane.Slowniki));

        var kontekst = new KontekstImportu(dane, podglad);

        ImportujRekonstrukcje(kontekst);
        ImportujLata(kontekst, arkusze);
        ImportujRepliki(kontekst, arkusze);
        ImportujKartyTechniczne(kontekst, arkusze);
        ImportujWyposazenie(kontekst, arkusze);
        ImportujUmundurowanie(kontekst, arkusze);
        ImportujZestawy(kontekst, arkusze);

        podglad.Dane = dane;
        return podglad;
    }

    /// <summary>Stosuje zatwierdzony podgląd do istniejących danych: kopiuje rekordy i ustawia dzień zerowy (FR-1102, FR-1107 — przed importem wywołujący robi kopię).</summary>
    public static void Zastosuj(PodgladImportu podglad, DaneAplikacji daneBazowe)
    {
        var dane = podglad.Dane ?? throw new InvalidOperationException("Brak danych w podglądzie");
        daneBazowe.Repliki.AddRange(dane.Repliki);
        daneBazowe.Przedmioty.AddRange(dane.Przedmioty);
        daneBazowe.Zestawy.AddRange(dane.Zestawy);
        daneBazowe.Wyprawy.AddRange(dane.Wyprawy);
        daneBazowe.Rekonstrukcje.AddRange(dane.Rekonstrukcje);
        daneBazowe.Zadania.AddRange(dane.Zadania);
        daneBazowe.Ustawienia.DzienZerowy = podglad.DzienZerowy;
        daneBazowe.Ustawienia.HistoryczneWyprawyWgRok = dane.Ustawienia.HistoryczneWyprawyWgRok;
        daneBazowe.Ustawienia.DataPierwszejWyprawy = dane.Ustawienia.DataPierwszejWyprawy;
        daneBazowe.CzyPierwszeUruchomienie = false;
    }

    /// <summary>Kontekst pracy importera: dane wynikowe i podgląd konfliktów.</summary>
    internal sealed class KontekstImportu
    {
        public KontekstImportu(DaneAplikacji dane, PodgladImportu podglad)
        {
            Dane = dane;
            Podglad = podglad;
        }

        public DaneAplikacji Dane { get; }
        public PodgladImportu Podglad { get; }
        /// <summary>Rekonstrukcje wg strony (Q16: po jednej na stronę).</summary>
        public Dictionary<string, Rekonstrukcja> RekonstrukcjeWgStrony { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Znajduje arkusz po nazwie (bez rozróżniania wielkości liter).</summary>
    private static ArkuszXlsx? ZnajdzArkusz(List<ArkuszXlsx> arkusze, string nazwa)
        => arkusze.FirstOrDefault(a => string.Equals(a.Nazwa, nazwa, StringComparison.OrdinalIgnoreCase));

    private static WierszXlsx? Naglowek(ArkuszXlsx? arkusz)
        => arkusz?.Wiersze.FirstOrDefault(w => w.Komorki.Any(k => !string.IsNullOrWhiteSpace(k)));
}
