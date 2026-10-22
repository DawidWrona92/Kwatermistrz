using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Import;

/// <summary>Część importera: arkusz „Wyposazenie” — 106 przedmiotów w kategoriach (FR-201..FR-211, 13.7, 13.8).</summary>
public partial class ImporterKwatermistrz
{
    private static void ImportujWyposazenie(KontekstImportu k, List<ArkuszXlsx> arkusze)
    {
        var arkusz = ZnajdzArkusz(arkusze, "Wyposazenie");
        if (arkusz == null)
        {
            k.Podglad.DodajKonflikt(RodzajKonfliktu.DoPotwierdzenia, "Nie znaleziono arkusza „Wyposazenie”", "plik");
            return;
        }
        var naglowek = Naglowek(arkusz);
        if (naglowek == null)
            return;

        // znajdź kolumny po nagłówkach
        int Kolumna(string wzorzec)
        {
            for (var i = 0; i < naglowek.Komorki.Count; i++)
            {
                var c = naglowek.Komorka(i);
                if (c != null && Normalizacja.TekstDoPorownania(c).Contains(wzorzec))
                    return i;
            }
            return -1;
        }
        var kolKat = Kolumna("kategoria");
        var kolFirma = Kolumna("firma");
        var kolNazwa = Kolumna("nazwa");
        var kolStrona = Kolumna("strona");
        var kolWzor = Kolumna("kolor");
        var kolGdzie = Kolumna("gdzie");
        var kolUzycia = Kolumna("liczba uzyc");
        var kolKomentarz = Kolumna("komentarz");
        // ostatnia kolumna (bez nagłówka w arkuszach Pasy/Nakolanniki/Plecaki) = klasa wykonania (13.8)
        var kolKlasa = naglowek.Komorki.Count - 1;

        var slowniki = k.Dane.Slowniki;
        var poprzedniaKategoria = "";
        var licznik = 0;

        foreach (var wiersz in arkusz.Wiersze.SkipWhile(w => w.Numer <= naglowek.Numer))
        {
            var nazwa = Normalizacja.Przytnij(wiersz.Komorka(kolNazwa));
            var kategoriaTekst = Normalizacja.Przytnij(wiersz.Komorka(kolKat));
            if (kategoriaTekst != null)
                poprzedniaKategoria = kategoriaTekst;
            var firma = Normalizacja.Przytnij(wiersz.Komorka(kolFirma));
            if (nazwa == null && firma == null)
                continue;
            if (nazwa == null && firma != null)
            {
                // wiersz tylko z firmą (np. „Aha Tactical”) — nazwa = firma, pełny przedmiot
                nazwa = firma;
            }
            if (nazwa == null)
                continue;

            var kategoria = Model.Slowniki.Znajdz(slowniki.Kategorie.Select(k2 => new WartoscSlownika { Id = k2.Id, Nazwa = k2.Nazwa }).ToList(), poprzedniaKategoria);
            var kategoriaId = slowniki.Kategorie.FirstOrDefault(k2 => Normalizacja.TekstDoPorownania(k2.Nazwa) == Normalizacja.TekstDoPorownania(poprzedniaKategoria))?.Id
                ?? slowniki.Kategorie.First().Id;

            var przedmiot = new Przedmiot
            {
                KategoriaId = kategoriaId,
                Nazwa = nazwa,
                Firma = Normalizacja.Przytnij(wiersz.Komorka(kolFirma)),
                ZnacznikZImportu = true,
            };

            var wzor = Normalizacja.Przytnij(wiersz.Komorka(kolWzor));
            if (wzor != null)
            {
                var w = slowniki.ZnajdzWzor(wzor);
                if (w != null)
                    przedmiot.WzorId = w.Id;
                else
                    k.Podglad.DodajKonflikt(RodzajKonfliktu.BrakOdpowiednika,
                        $"Wzór „{wzor}” ({nazwa}) nie jest w słowniku — dodaj do słownika lub przypisz ręcznie", $"Wyposazenie wiersz {wiersz.Numer}");
            }

            var strona = Normalizacja.Przytnij(wiersz.Komorka(kolStrona));
            if (strona != null)
            {
                var w = Model.Slowniki.Znajdz(slowniki.Strony, strona);
                if (w != null)
                    przedmiot.StronyId.Add(w.Id);
            }

            var gdzie = Normalizacja.Przytnij(wiersz.Komorka(kolGdzie));
            if (gdzie != null)
            {
                var w = Model.Slowniki.Znajdz(slowniki.Miejsca, gdzie);
                if (w != null)
                    przedmiot.MiejsceId = w.Id;
            }

            var klasa = Normalizacja.Przytnij(wiersz.Komorka(kolKlasa));
            if (klasa != null)
            {
                var w = Model.Slowniki.Znajdz(slowniki.Pochodzenie, klasa);
                if (w != null)
                    przedmiot.PochodzenieId = w.Id;
            }

            // licznik użyć: importujemy dla wszystkich kategorii, mundury dostaną później wartość z tabeli umundurowania (13.2)
            var uzycia = CzytnikXlsx.LiczbaCalkowita(wiersz.Komorka(kolUzycia));
            if (uzycia is > 0)
            {
                przedmiot.LicznikHistoryczny = uzycia.Value;
                przedmiot.LicznikHistorycznyDoWeryfikacji = true;
            }

            przedmiot.Komentarz = Normalizacja.Przytnij(wiersz.Komorka(kolKomentarz));
            PrzypiszDoRekonstrukcji(k, przedmiot.StronyId, przedmiot.RekonstrukcjeId);

            k.Dane.Przedmioty.Add(przedmiot);
            licznik++;

        }

        k.Podglad.LiczbaRekordow["Przedmioty"] = licznik;
        RozwiazMontazLadownic(k);
    }

    /// <summary>Id kategorii wg nazwy (13.7).</summary>
    internal static string IdKategorii(KontekstImportu k, string nazwa)
        => k.Dane.Slowniki.Kategorie.First(x => Normalizacja.TekstDoPorownania(x.Nazwa) == Normalizacja.TekstDoPorownania(nazwa)).Id;

    /// <summary>Rozwiązuje montaż 60 ładowownic na nośnikach według komentarzy (13.7). Komentarz to „firma nazwa” lub „nazwa” (np. „6b46”, „Pas MBC”, „Na 6b45”) — dopasowujemy po słowach kluczowych z tolerancją na kolejność. Gdy pasuje kilku nośników — konflikt do decyzji; gdy żaden — konflikt „brak odpowiednika”.</summary>
    private static void RozwiazMontazLadownic(KontekstImportu k)
    {
        var kategorie = k.Dane.Slowniki.Kategorie;
        // nośniki to tylko pozycje katalogowe (bez szkiców z zestawów) — szkic nie może być nośnikiem
        var nosniki = k.Dane.Przedmioty.Where(p => !p.DoUzupelnienia && kategorie.FirstOrDefault(x => x.Id == p.KategoriaId)?.CzyNosnik == true).ToList();
        var ladownice = k.Dane.Przedmioty.Where(p => p.KategoriaId == IdKategorii(k, "Ładownice") && !p.DoUzupelnienia).ToList();
        foreach (var ladownica in ladownice)
        {
            // Ładownica bez komentarza też ma być „rozpoznana” — jako konflikt „brak wskazania nośnika” (do uzupełnienia ręcznego)
            if (ladownica.Komentarz == null)
            {
                k.Podglad.DodajKonflikt(RodzajKonfliktu.BrakOdpowiednika,
                    $"Ładownica „{ladownica.Nazwa}” nie ma wskazanego nośnika — przypisz ręcznie",
                    $"Wyposazenie: Ładownice");
                continue;
            }
            var slowaKomentarza = Dopasowanie.SlowaKluczowe(ladownica.Komentarz)
                .Where(w => w != "na") // „Na 6b45” → „6b45”
                .ToList();
            if (slowaKomentarza.Count == 0)
                continue;
            var kandydaci = nosniki.Where(n =>
            {
                var tekst = Normalizacja.TekstDoPorownania($"{n.Firma} {n.Nazwa}");
                // „Pas SWD” (pas + swd) = „Smiersz Newgen SWD” — oba słowa muszą wystąpić razem
                if (slowaKomentarza.Contains("pas") && slowaKomentarza.Contains("swd"))
                    return tekst.Contains("smiersz", StringComparison.Ordinal) && tekst.Contains("swd", StringComparison.Ordinal);
                // wszystkie słowa komentarza muszą wystąpić w tekście nośnika; „6b46 Old Gen” odrzucamy, bo zawiera „old”/„gen”
                return slowaKomentarza.All(w => tekst.Contains(w, StringComparison.Ordinal))
                    && !slowaKomentarza.Any(w => SlowoJestRozne(w, tekst));
            }).ToList();

            if (kandydaci.Count == 1)
            {
                ladownica.ZamontowanyNaId = kandydaci[0].Id;
            }
            else
            {
                var konflikt = k.Podglad.DodajKonflikt(
                    kandydaci.Count > 1 ? RodzajKonfliktu.Niejednoznaczne : RodzajKonfliktu.BrakOdpowiednika,
                    kandydaci.Count > 1
                        ? $"Ładownica „{ladownica.Nazwa}” — komentarz „{ladownica.Komentarz}” pasuje do kilku nośników; wybierz, na którym wisi"
                        : $"Ładownica „{ladownica.Nazwa}” — komentarz „{ladownica.Komentarz}” nie wskazuje jednoznacznie nośnika; przypisz ręcznie",
                    $"Wyposazenie: Ładownice");
                konflikt.Kandydaci = kandydaci.Select(n => $"{n.Firma} {n.Nazwa}").ToList();
                konflikt.DomyslnyWybor = 0;
                // FR-1102: wybór nośnika z ekranu konfliktów (pominięcie = bez montażu)
                var wybor = konflikt.Decyzja;
                if (wybor >= 0 && wybor < kandydaci.Count)
                    ladownica.ZamontowanyNaId = kandydaci[wybor].Id;
            }
        }
    }

    /// <summary>Rozbieżność między słowem komentarza a nośnikiem (13.7): samotne „6b46” różni się od „6b46 Old Gen” — Old Gen zawiera „old”/„gen”, więc jest rozróżnialne.</summary>
    private static bool SlowoJestRozne(string slowo, string tekstNosnika)
    {
        if (slowo == "6b46" && tekstNosnika.Contains("6b46", StringComparison.Ordinal)
            && (tekstNosnika.Contains("old", StringComparison.Ordinal) || tekstNosnika.Contains("gen", StringComparison.Ordinal)))
            return true;
        return false;
    }
}
