using Kwatermistrz.Core.Konfiguracja;
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

    /// <summary>Stosuje zatwierdzony podgląd do istniejących danych (FR-1102, FR-1107 — przed importem wywołujący robi kopię).
    /// Słowniki i szablony kart są SCALANE z lokalnymi (brakujące dopisywane, istniejące zachowane — FR-1003), referencje przemapowane;
    /// rekonstrukcje o istniejącej nazwie są pomijane (ponowny import nie dubluje). Zwraca mapy Id importu → lokalne.</summary>
    public static MergerSlownikow.WynikScalenia Zastosuj(PodgladImportu podglad, DaneAplikacji daneBazowe)
    {
        var dane = podglad.Dane ?? throw new InvalidOperationException("Brak danych w podglądzie");

        // scalenie konfiguracji (słowniki, wzory, kategorie) z lokalną — zamiast osierocania Id
        var wynik = MergerSlownikow.ScalSlowniki(daneBazowe.Slowniki, dane.Slowniki);

        // szablony kart: dopisz brakujące (po nazwie), napęd przemapowany
        foreach (var szablon in dane.SzablonyKart)
        {
            var istniejacy = daneBazowe.SzablonyKart.FirstOrDefault(x =>
                Normalizacja.TekstDoPorownania(x.Nazwa) == Normalizacja.TekstDoPorownania(szablon.Nazwa));
            if (istniejacy != null)
                continue;
            szablon.NapedId = wynik.MapujSlownik("Napedy", szablon.NapedId);
            daneBazowe.SzablonyKart.Add(szablon);
        }

        // mapy Id rekordów (rekonstrukcje pomijane, gdy już istnieją o tej nazwie)
        var mapaRekonstrukcji = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var r in dane.Rekonstrukcje)
        {
            var istniejaca = daneBazowe.Rekonstrukcje.FirstOrDefault(x =>
                Normalizacja.TekstDoPorownania(x.Nazwa) == Normalizacja.TekstDoPorownania(r.Nazwa));
            if (istniejaca != null)
            {
                mapaRekonstrukcji[r.Id] = istniejaca.Id;
                continue;
            }
            r.StronaId = wynik.MapujSlownik("Strony", r.StronaId);
            daneBazowe.Rekonstrukcje.Add(r);
            mapaRekonstrukcji[r.Id] = r.Id;
        }

        // rekordy z przemapowanymi referencjami
        foreach (var p in dane.Przedmioty)
        {
            p.KategoriaId = wynik.MapujKategoria(p.KategoriaId) ?? "";
            p.WzorId = wynik.MapujWzor(p.WzorId);
            p.StronyId = p.StronyId.Select(id => wynik.MapujSlownik("Strony", id) ?? id).ToList();
            p.MiejsceId = wynik.MapujSlownik("Miejsca", p.MiejsceId);
            p.PochodzenieId = wynik.MapujSlownik("Pochodzenie", p.PochodzenieId);
            p.RekonstrukcjeId = p.RekonstrukcjeId.Select(id => Remap(mapaRekonstrukcji, id)).ToList();
            foreach (var koszt in p.Koszty)
                koszt.RodzajId = wynik.MapujSlownik("RodzajeKosztow", koszt.RodzajId) ?? koszt.RodzajId;
        }
        foreach (var r in dane.Repliki)
        {
            r.StronyId = r.StronyId.Select(id => wynik.MapujSlownik("Strony", id) ?? id).ToList();
            r.RoleId = r.RoleId.Select(id => wynik.MapujSlownik("Role", id) ?? id).ToList();
            r.TypMagazynkaId = wynik.MapujSlownik("TypyMagazynkow", r.TypMagazynkaId);
            r.NapedId = wynik.MapujSlownik("Napedy", r.NapedId);
            r.StanId = wynik.MapujSlownik("StanyReplik", r.StanId);
            r.RekonstrukcjeId = r.RekonstrukcjeId.Select(id => Remap(mapaRekonstrukcji, id)).ToList();
            if (r.SzablonKartyId != null)
            {
                var szablon = daneBazowe.SzablonyKart.FirstOrDefault(x =>
                    Normalizacja.TekstDoPorownania(x.Nazwa) ==
                    Normalizacja.TekstDoPorownania(dane.SzablonyKart.FirstOrDefault(s => s.Id == r.SzablonKartyId)?.Nazwa ?? ""));
                if (szablon != null)
                    r.SzablonKartyId = szablon.Id;
            }
            foreach (var koszt in r.Koszty)
                koszt.RodzajId = wynik.MapujSlownik("RodzajeKosztow", koszt.RodzajId) ?? koszt.RodzajId;
        }
        foreach (var z in dane.Zestawy)
        {
            z.RekonstrukcjaId = z.RekonstrukcjaId != null ? Remap(mapaRekonstrukcji, z.RekonstrukcjaId) : null;
            z.StronaId = wynik.MapujSlownik("Strony", z.StronaId);
        }
        foreach (var w in dane.Wyprawy)
        {
            if (w.RekonstrukcjaId != null)
                w.RekonstrukcjaId = Remap(mapaRekonstrukcji, w.RekonstrukcjaId);
        }
        foreach (var zadanie in dane.Zadania)
        {
            zadanie.RodzajId = wynik.MapujSlownik("RodzajeZadan", zadanie.RodzajId) ?? zadanie.RodzajId;
            if (zadanie.DotyczyTyp == "Replika" && zadanie.DotyczyId != null)
            {
                // DotyczyId wskazuje replikę z importu — na tym etapie importujemy wszystkie, więc Id zostają
            }
        }

        daneBazowe.Repliki.AddRange(dane.Repliki);
        daneBazowe.Przedmioty.AddRange(dane.Przedmioty);
        daneBazowe.Zestawy.AddRange(dane.Zestawy);
        daneBazowe.Wyprawy.AddRange(dane.Wyprawy);
        daneBazowe.Zadania.AddRange(dane.Zadania);
        daneBazowe.Ustawienia.DzienZerowy = podglad.DzienZerowy;
        daneBazowe.Ustawienia.HistoryczneWyprawyWgRok = dane.Ustawienia.HistoryczneWyprawyWgRok;
        daneBazowe.Ustawienia.DataPierwszejWyprawy = dane.Ustawienia.DataPierwszejWyprawy;
        daneBazowe.CzyPierwszeUruchomienie = false;
        return wynik;
    }

    private static string Remap(Dictionary<string, string> mapa, string id)
        => mapa.TryGetValue(id, out var nowe) ? nowe : id;

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
