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
