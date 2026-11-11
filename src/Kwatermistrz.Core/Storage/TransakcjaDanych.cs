using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Storage;

/// <summary>
/// Przygotowuje import na niezależnej kopii danych i zatwierdza ją dopiero po sukcesie.
/// Zachowuje obiekt DaneAplikacji współdzielony przez ViewModele oraz format JSON v1.
/// Nie zastępuje wykonania kopii zapasowej na dysku przed importem.
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
        var kopia = MagazynJson.WczytajZeStrumienia(strumien);
        // Wyjątek podczas importu nie narusza oryginału.
        var wynik = akcja(kopia);
        Zatwierdz(zrodlo, kopia);
        return wynik;
    }

    private static void Zatwierdz(DaneAplikacji cel, DaneAplikacji kopia)
    {
        // Zmieniamy zawartość istniejących list, nie wymieniamy głównego obiektu
        // ani list powiązanych z nim przez widoki.
        Podmien(cel.Przedmioty, kopia.Przedmioty);
        Podmien(cel.Repliki, kopia.Repliki);
        Podmien(cel.Zestawy, kopia.Zestawy);
        Podmien(cel.Wyprawy, kopia.Wyprawy);
        Podmien(cel.Rekonstrukcje, kopia.Rekonstrukcje);
        Podmien(cel.SzablonyKart, kopia.SzablonyKart);
        Podmien(cel.Zadania, kopia.Zadania);

        // Słowniki i ustawienia są obiektami współdzielonymi — szczegóły ich
        // prezentacji odświeżamy po zatwierdzeniu importu.
        cel.Slowniki = kopia.Slowniki;
        cel.Ustawienia = kopia.Ustawienia;
        cel.SchemaVersion = kopia.SchemaVersion;
        cel.WersjaRegul = kopia.WersjaRegul;
        cel.CzyPierwszeUruchomienie = kopia.CzyPierwszeUruchomienie;
        cel.OstatniaZmiana = DateTime.Now;
    }

    private static void Podmien<T>(List<T> cel, List<T> zrodlo)
    {
        cel.Clear();
        cel.AddRange(zrodlo);
    }
}
