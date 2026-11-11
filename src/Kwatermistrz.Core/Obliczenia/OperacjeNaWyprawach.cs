using System.Globalization;
using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Obliczenia;

/// <summary>
/// Operacje na wyprawach i historycznych licznikach. Nie zmienia schematu JSON.
/// Edycja odbywa się na kopii, a liczniki są sprawdzane przed jakąkolwiek zmianą danych.
/// </summary>
public sealed class OperacjeNaWyprawach
{
    private readonly DaneAplikacji _dane;

    public OperacjeNaWyprawach(DaneAplikacji dane) => _dane = dane;

    /// <summary>Kopia robocza; nie współdzieli mutowalnych kolekcji z zapisaną wyprawą.</summary>
    public static Wyprawa Kopia(Wyprawa zrodlo) => new()
    {
        Id = zrodlo.Id,
        Data = zrodlo.Data,
        RekonstrukcjaId = zrodlo.RekonstrukcjaId,
        Miejsce = zrodlo.Miejsce,
        ReplikaGlownaId = zrodlo.ReplikaGlownaId,
        ZestawId = zrodlo.ZestawId,
        WybraneWarianty = new Dictionary<string, string>(zrodlo.WybraneWarianty, StringComparer.Ordinal),
        SkladReczny = zrodlo.SkladReczny.Select(e => new ElementWariantu
        {
            Typ = e.Typ, ElementId = e.ElementId, NazwaSzkicu = e.NazwaSzkicu,
        }).ToList(),
        OdliczonaOdLicznikow = zrodlo.OdliczonaOdLicznikow,
        Komentarz = zrodlo.Komentarz,
        Zdjecia = new List<string>(zrodlo.Zdjecia),
        CzyUsunieta = zrodlo.CzyUsunieta,
        OstatniaZmiana = zrodlo.OstatniaZmiana,
    };

    public bool CzyHistoryczna(Wyprawa wyprawa) =>
        _dane.Ustawienia.DzienZerowy is { } dzien && wyprawa.Data != default && wyprawa.Data <= dzien;

    public bool SprobujDodac(Wyprawa wyprawa, bool odlicz, out string blad)
    {
        blad = "";
        if (!PoprawnaData(wyprawa, out blad))
            return false;
        if (_dane.Wyprawy.Any(w => w.Id == wyprawa.Id))
        {
            blad = "Wyprawa z tym identyfikatorem już istnieje.";
            return false;
        }
        odlicz = odlicz && CzyHistoryczna(wyprawa);
        if (!SprobujPrzeliczyc(null, false, wyprawa, odlicz, out blad))
            return false;
        wyprawa.OdliczonaOdLicznikow = odlicz;
        wyprawa.CzyUsunieta = false;
        wyprawa.OstatniaZmiana = DateTime.Now;
        _dane.Wyprawy.Add(wyprawa);
        return true;
    }

    /// <summary>
    /// Zmienia oryginał dopiero po walidacji i przeliczeniu korekty.
    /// Nie włącza automatycznie odliczania na starych wyprawach, które go nie miały.
    /// </summary>
    public bool SprobujZapisac(Wyprawa oryginal, Wyprawa szkic, out string blad)
    {
        blad = "";
        if (!_dane.Wyprawy.Contains(oryginal) || oryginal.CzyUsunieta || oryginal.Id != szkic.Id)
        {
            blad = "Wybrana wyprawa nie jest dostępna do edycji.";
            return false;
        }
        if (!PoprawnaData(szkic, out blad))
            return false;

        var byloOdliczone = oryginal.OdliczonaOdLicznikow;
        var maBycOdliczone = byloOdliczone && CzyHistoryczna(szkic);
        if (!SprobujPrzeliczyc(oryginal, byloOdliczone, szkic, maBycOdliczone, out blad))
            return false;

        oryginal.Data = szkic.Data;
        oryginal.RekonstrukcjaId = szkic.RekonstrukcjaId;
        oryginal.Miejsce = szkic.Miejsce;
        oryginal.ReplikaGlownaId = szkic.ReplikaGlownaId;
        oryginal.ZestawId = szkic.ZestawId;
        oryginal.WybraneWarianty = new Dictionary<string, string>(szkic.WybraneWarianty, StringComparer.Ordinal);
        oryginal.SkladReczny = szkic.SkladReczny.Select(e => new ElementWariantu
        {
            Typ = e.Typ, ElementId = e.ElementId, NazwaSzkicu = e.NazwaSzkicu,
        }).ToList();
        oryginal.Komentarz = szkic.Komentarz;
        oryginal.Zdjecia = new List<string>(szkic.Zdjecia);
        oryginal.OdliczonaOdLicznikow = maBycOdliczone;
        oryginal.OstatniaZmiana = DateTime.Now;
        return true;
    }

    /// <summary>
    /// Przy usunięciu zwraca korektę, ale zachowuje jej flagę w usuniętym rekordzie.
    /// Pozwala to bez dodatkowego pola JSON odtworzyć stan przy przywracaniu.
    /// </summary>
    public bool SprobujUsun(Wyprawa wyprawa, out string blad)
    {
        blad = "";
        if (!_dane.Wyprawy.Contains(wyprawa) || wyprawa.CzyUsunieta)
        {
            blad = "Wyprawa jest już usunięta albo nie istnieje.";
            return false;
        }
        if (!SprobujPrzeliczyc(wyprawa, wyprawa.OdliczonaOdLicznikow, null, false, out blad))
            return false;
        wyprawa.CzyUsunieta = true;
        wyprawa.OstatniaZmiana = DateTime.Now;
        return true;
    }

    /// <summary>
    /// Usunięty rekord z wcześniejszej wersji może nie mieć flagi korekty.
    /// W takim przypadku decyzję o nowym odliczeniu podejmuje użytkownik.
    /// </summary>
    public bool SprobujPrzywroc(Wyprawa wyprawa, bool odlicz, out string blad)
    {
        blad = "";
        if (!_dane.Wyprawy.Contains(wyprawa) || !wyprawa.CzyUsunieta)
        {
            blad = "Wyprawa nie jest usunięta.";
            return false;
        }
        var maBycOdliczone = wyprawa.OdliczonaOdLicznikow || (odlicz && CzyHistoryczna(wyprawa));
        if (!SprobujPrzeliczyc(null, false, wyprawa, maBycOdliczone, out blad))
            return false;
        wyprawa.OdliczonaOdLicznikow = maBycOdliczone;
        wyprawa.CzyUsunieta = false;
        wyprawa.OstatniaZmiana = DateTime.Now;
        return true;
    }

    private static bool PoprawnaData(Wyprawa w, out string blad)
    {
        blad = w.Data == default ? "Wybierz datę wyprawy." : "";
        return blad.Length == 0;
    }

    private bool SprobujPrzeliczyc(Wyprawa? poprzednia, bool bylaKorekta,
        Wyprawa? nowa, bool nowaKorekta, out string blad)
    {
        blad = "";
        var delty = new Dictionary<(string Rodzaj, string Id), int>();
        var kalk = new Kalkulator(_dane);
        if (poprzednia != null && bylaKorekta)
            foreach (var (rodzaj, id, _) in kalk.WplywNaLicznikiHistoryczne(poprzednia))
                DodajDelte(rodzaj, id, +1);
        if (nowa != null && nowaKorekta)
            foreach (var (rodzaj, id, _) in kalk.WplywNaLicznikiHistoryczne(nowa))
                DodajDelte(rodzaj, id, -1);

        // Transakcja w pamięci: jeśli choć jeden licznik byłby ujemny albo brakujący,
        // nie dotykamy żadnego licznika ani wyprawy.
        var aktualizacje = new List<(Action<int> Zapis, int Wartosc)>();
        foreach (var (klucz, delta) in delty)
        {
            if (delta == 0)
                continue;
            int? obecny = null;
            Action<int>? zapis = null;
            switch (klucz.Rodzaj)
            {
                case "Replika":
                    var replika = _dane.Repliki.FirstOrDefault(r => r.Id == klucz.Id);
                    if (replika != null)
                    {
                        obecny = replika.LicznikHistorycznyWypraw;
                        zapis = v => replika.LicznikHistorycznyWypraw = v;
                    }
                    break;
                case "Mundur":
                    var mundur = _dane.Przedmioty.FirstOrDefault(p => p.Id == klucz.Id);
                    if (mundur != null)
                    {
                        obecny = mundur.LicznikHistoryczny;
                        zapis = v => mundur.LicznikHistoryczny = v;
                    }
                    break;
                case "Rok":
                    if (int.TryParse(klucz.Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rok)
                        && _dane.Ustawienia.HistoryczneWyprawyWgRok.TryGetValue(rok, out var liczba))
                    {
                        obecny = liczba;
                        zapis = v => _dane.Ustawienia.HistoryczneWyprawyWgRok[rok] = v;
                    }
                    break;
            }
            if (obecny == null || zapis == null || obecny.Value + delta < 0)
            {
                blad = $"Nie można bezpiecznie skorygować licznika historycznego ({klucz.Rodzaj}: {klucz.Id}). " +
                    "Dane pozostawiono bez zmian. Sprawdź liczniki historyczne przed ponowną próbą.";
                return false;
            }
            aktualizacje.Add((zapis, obecny.Value + delta));
        }

        foreach (var (zapis, wartosc) in aktualizacje)
            zapis(wartosc);
        return true;

        void DodajDelte(string rodzaj, string id, int delta)
        {
            var klucz = (rodzaj, id);
            delty[klucz] = delty.GetValueOrDefault(klucz) + delta;
        }
    }
}
