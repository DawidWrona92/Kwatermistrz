using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Konfiguracja;

/// <summary>Wartość słownika w pakiecie konfiguracji wraz z kluczem słownika (FR-1404).</summary>
public class WartoscSlownikaPakietu
{
    /// <summary>Klucz słownika (patrz <see cref="RodzajeSlownikow"/>), np. „Strony”.</summary>
    public string Slownik { get; set; } = "";
    public WartoscSlownika Wartosc { get; set; } = new();
}

/// <summary>Rekonstrukcja w pakiecie konfiguracji — tylko konfiguracja, bez zdjęcia i znacznika importu (FR-1404).</summary>
public class RekonstrukcjaPakietu
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Nazwa { get; set; } = "";
    public string? Okres { get; set; }
    public string? StronaId { get; set; }
    public string? Opis { get; set; }
    public List<SlotSzablonu> SzablonSlotow { get; set; } = new();
}

/// <summary>Pakiet konfiguracji wymieniany między instalacjami: słowniki, wzory, kategorie, rekonstrukcje (szablony slotów) i szablony kart. Bez przedmiotów, replik, zestawów, wypraw, zdjęć i cen (FR-1404, FR-1402).</summary>
public class PakietKonfiguracji
{
    public const string TypPakietu = "Kwatermistrz.PakietKonfiguracji";
    public const int AktualnaWersjaPakietu = 1;

    public string Typ { get; set; } = TypPakietu;
    public int WersjaPakietu { get; set; } = AktualnaWersjaPakietu;
    /// <summary>Wersja reguł obliczeń instalacji, z której pochodzi pakiet (NFR-14).</summary>
    public string WersjaRegul { get; set; } = "";
    public string? WersjaAplikacji { get; set; }
    public string? Nazwa { get; set; }
    public DateTime DataUtworzenia { get; set; } = DateTime.Now;
    public List<WartoscSlownikaPakietu> Slowniki { get; set; } = new();
    public List<WzorKamuflazu> Wzory { get; set; } = new();
    public List<Kategoria> Kategorie { get; set; } = new();
    public List<RekonstrukcjaPakietu> Rekonstrukcje { get; set; } = new();
    public List<SzablonKarty> SzablonyKart { get; set; } = new();

    private static readonly JsonSerializerOptions Opcje = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Buduje pakiet z konfiguracji bieżących danych: wszystkie słowniki (z aliasami i semantyką), wzory, kategorie, rekonstrukcje z szablonami slotów oraz szablony kart (FR-1404).</summary>
    public static PakietKonfiguracji Eksportuj(DaneAplikacji dane, string? nazwa)
    {
        var pakiet = new PakietKonfiguracji
        {
            Nazwa = nazwa,
            WersjaRegul = dane.WersjaRegul,
            WersjaAplikacji = typeof(PakietKonfiguracji).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
        };
        foreach (var rodzaj in RodzajeSlownikow.Wszystkie)
        {
            foreach (var wartosc in rodzaj.Pobierz(dane.Slowniki))
                pakiet.Slowniki.Add(new WartoscSlownikaPakietu { Slownik = rodzaj.Klucz, Wartosc = wartosc });
        }
        pakiet.Wzory.AddRange(dane.Slowniki.Wzory);
        pakiet.Kategorie.AddRange(dane.Slowniki.Kategorie);
        foreach (var r in dane.Rekonstrukcje)
        {
            pakiet.Rekonstrukcje.Add(new RekonstrukcjaPakietu
            {
                Id = r.Id,
                Nazwa = r.Nazwa,
                Okres = r.Okres,
                StronaId = r.StronaId,
                Opis = r.Opis,
                SzablonSlotow = r.SzablonSlotow,
            });
        }
        pakiet.SzablonyKart.AddRange(dane.SzablonyKart);
        return pakiet;
    }

    /// <summary>Zapisuje pakiet do pliku (.kwpakiet — JSON).</summary>
    public static void ZapiszDoPliku(PakietKonfiguracji pakiet, string sciezka)
    {
        var json = JsonSerializer.Serialize(pakiet, Opcje);
        File.WriteAllText(sciezka, json);
    }

    /// <summary>Wczytuje pakiet z pliku; przy błędnym typie lub nowszej wersji rzuca wyjątek.</summary>
    public static PakietKonfiguracji WczytajZPliku(string sciezka)
    {
        var json = File.ReadAllText(sciezka);
        var pakiet = JsonSerializer.Deserialize<PakietKonfiguracji>(json, Opcje)
            ?? throw new InvalidDataException("Pusty pakiet");
        if (pakiet.Typ != TypPakietu)
            throw new InvalidDataException("To nie jest pakiet konfiguracji Kwatermistrz");
        if (pakiet.WersjaPakietu > AktualnaWersjaPakietu)
            throw new InvalidDataException("Pakiet pochodzi z nowszej wersji aplikacji");
        return pakiet;
    }
}
