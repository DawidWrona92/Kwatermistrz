using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Import;

/// <summary>Część importera: rekonstrukcje (FR-1103, Q16) i liczniki wypraw wg lat (BR-07, BR-15).</summary>
public partial class ImporterKwatermistrz
{
    /// <summary>Tworzy po jednej rekonstrukcji na stronę występującą w słownikach (Q16: Rosja, Polska, NATO), ze startowym szablonem slotów (Załącznik B).</summary>
    private static void ImportujRekonstrukcje(KontekstImportu k)
    {
        foreach (var strona in k.Dane.Slowniki.Strony)
        {
            var rekonstrukcja = new Rekonstrukcja
            {
                Nazwa = strona.Nazwa,
                StronaId = strona.Id,
                SzablonSlotow = StartoweSlowniki.StartoweSloty(),
                ZnacznikZImportu = true,
            };
            k.Dane.Rekonstrukcje.Add(rekonstrukcja);
            k.RekonstrukcjeWgStrony[strona.Nazwa] = rekonstrukcja;
        }
        k.Podglad.LiczbaRekordow["Rekonstrukcje"] = k.Dane.Rekonstrukcje.Count;
    }

    /// <summary>Wczytuje arkusz „Wyprawy_wg_lat”: datę pierwszej wyprawy i liczby wypraw w latach jako liczniki historyczne (A3: to wartości roczne, nie sumy narastające; BR-07).</summary>
    private static void ImportujLata(KontekstImportu k, List<ArkuszXlsx> arkusze)
    {
        var arkusz = ZnajdzArkusz(arkusze, "Wyprawy_wg_lat");
        if (arkusz == null)
        {
            k.Podglad.DodajKonflikt(RodzajKonfliktu.DoPotwierdzenia, "Nie znaleziono arkusza „Wyprawy_wg_lat”", "plik");
            return;
        }
        foreach (var wiersz in arkusz.Wiersze)
        {
            var etykieta = Normalizacja.Przytnij(wiersz.Komorka(0));
            var wartosc = Normalizacja.Przytnij(wiersz.Komorka(1));
            if (etykieta == null || wartosc == null)
                continue;
            var klucz = Normalizacja.TekstDoPorownania(etykieta);
            if (klucz.StartsWith("pierwsza wyprawa", StringComparison.Ordinal))
            {
                var data = CzytnikXlsx.Data(wartosc);
                if (data != null)
                    k.Dane.Ustawienia.DataPierwszejWyprawy = data;
            }
            else if (klucz.StartsWith("do konca roku", StringComparison.Ordinal))
            {
                // etykiety „Do końca roku 2022/2023” — A3: traktujemy jako wartości roczne
                if (int.TryParse(etykieta.Substring(etykieta.Length - 4), out var rok) && CzytnikXlsx.LiczbaCalkowita(wartosc) is int liczba)
                    k.Dane.Ustawienia.HistoryczneWyprawyWgRok[rok] = liczba;
            }
            else if (klucz.StartsWith("w ", StringComparison.Ordinal) && int.TryParse(etykieta.Substring(2).Trim(), out var rok2)
                     && CzytnikXlsx.LiczbaCalkowita(wartosc) is int liczba2)
            {
                k.Dane.Ustawienia.HistoryczneWyprawyWgRok[rok2] = liczba2;
            }
        }
        k.Podglad.LiczbaRekordow["Liczniki wypraw wg lat"] = k.Dane.Ustawienia.HistoryczneWyprawyWgRok.Count;
    }

    /// <summary>Przypisuje element do rekonstrukcji swojej strony; przy wielu stronach do każdej (FR-1103). Brak strony = element wspólny.</summary>
    internal static void PrzypiszDoRekonstrukcji(KontekstImportu k, List<string> stronyId, List<string> rekonstrukcjeId)
    {
        foreach (var stronaId in stronyId)
        {
            var strona = k.Dane.Slowniki.Strony.FirstOrDefault(s => s.Id == stronaId);
            if (strona != null && k.RekonstrukcjeWgStrony.TryGetValue(strona.Nazwa, out var rek))
                if (!rekonstrukcjeId.Contains(rek.Id))
                    rekonstrukcjeId.Add(rek.Id);
        }
    }
}
