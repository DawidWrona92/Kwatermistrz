using System.Text.Json;
using System.Text.Json.Serialization;
using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Storage;

/// <summary>Wynik kontroli integralności przy starcie (NFR-05).</summary>
public class WynikKontroli
{
    public bool CzyPoprawne { get; set; }
    public string? komunikat { get; set; }
    /// <summary>Ścieżka pliku, z którego wczytano dane (może być kopią .bak, gdy główny plik był uszkodzony).</summary>
    public string? Zrodlo { get; set; }
}

/// <summary>Magazyn danych w jednym pliku JSON z zapisem atomowym (decyzja 02, NFR-03, NFR-05).</summary>
public class MagazynJson
{
    private const int AktualnaWersjaSchematu = 1;

    private static readonly JsonSerializerOptions Opcje = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter(), new DateOnlyJsonConverter(), new DateOnlyNullableJsonConverter() },
    };

    private readonly string _sciezka;
    private readonly object _blokada = new();

    public MagazynJson(string katalogDanych)
    {
        KatalogDanych = katalogDanych;
        _sciezka = Path.Combine(katalogDanych, "dane.json");
    }

    /// <summary>Katalog, w którym leżą dane i podkatalogi (zdjecia, kopie).</summary>
    public string KatalogDanych { get; }

    /// <summary>Ścieżka pliku danych.</summary>
    public string SciezkaPliku => _sciezka;

    /// <summary>Czy istnieją dane na dysku.</summary>
    public bool IstniejaDane => File.Exists(_sciezka);

    /// <summary>Wczytuje dane; przy braku pliku zwraca nowy, pusty dokument z słownikami startowymi (FR-1401, NFR-15).</summary>
    public DaneAplikacji Wczytaj()
    {
        lock (_blokada)
        {
            if (!File.Exists(_sciezka))
                return NoweDane();
            var json = File.ReadAllText(_sciezka);
            var dane = JsonSerializer.Deserialize<DaneAplikacji>(json, Opcje) ?? NoweDane();
            Migruj(dane);
            return dane;
        }
    }

    /// <summary>Wczytuje dane z kontrolą integralności; przy uszkodzonym pliku próbuje odczytać ostatnią kopię .bak (NFR-05).</summary>
    public (DaneAplikacji Dane, WynikKontroli Kontrola) WczytajZKontrola()
    {
        lock (_blokada)
        {
            if (!File.Exists(_sciezka))
            {
                var puste = NoweDane();
                return (puste, new WynikKontroli { CzyPoprawne = true, Zrodlo = null });
            }
            try
            {
                var json = File.ReadAllText(_sciezka);
                var dane = JsonSerializer.Deserialize<DaneAplikacji>(json, Opcje) ?? throw new InvalidDataException("Pusty dokument");
                Waliduj(dane);
                Migruj(dane);
                return (dane, new WynikKontroli { CzyPoprawne = true, Zrodlo = _sciezka });
            }
            catch (Exception)
            {
                var kopia = _sciezka + ".bak";
                if (File.Exists(kopia))
                {
                    var json = File.ReadAllText(kopia);
                    var dane = JsonSerializer.Deserialize<DaneAplikacji>(json, Opcje) ?? NoweDane();
                    Migruj(dane);
                    return (dane, new WynikKontroli { CzyPoprawne = false, komunikat = "Odczytano ostatnią kopię zapasową", Zrodlo = kopia });
                }
                var nowe = NoweDane();
                return (nowe, new WynikKontroli { CzyPoprawne = false, komunikat = "Nie udało się odczytać danych; utworzono pusty dokument", Zrodlo = null });
            }
        }
    }

    /// <summary>Wczytuje dane z konkretnego pliku (przywracanie z kopii, FR-1107). Przy błędzie rzuca wyjątek.</summary>
    public static DaneAplikacji WczytajZPliku(string sciezka)
    {
        var json = File.ReadAllText(sciezka);
        var dane = JsonSerializer.Deserialize<DaneAplikacji>(json, Opcje) ?? throw new InvalidDataException("Pusty dokument");
        if (dane.SchemaVersion > AktualnaWersjaSchematu)
            throw new InvalidDataException("Dane pochodzą z nowszej wersji aplikacji");
        Migruj(dane);
        return dane;
    }

    /// <summary>Zapisuje dane atomowo: najpierw plik tymczasowy, potem zamiana. Po powrocie tej metody dane są na dysku (NFR-05).</summary>
    public void Zapisz(DaneAplikacji dane)
    {
        lock (_blokada)
        {
            Directory.CreateDirectory(KatalogDanych);
            dane.OstatniaZmiana = DateTime.Now;
            var json = JsonSerializer.Serialize(dane, Opcje);
            var tmp = _sciezka + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(_sciezka))
            {
                var kopia = _sciezka + ".bak";
                File.Copy(_sciezka, kopia, overwrite: true);
            }
            File.Move(tmp, _sciezka, overwrite: true);
        }
    }

    /// <summary>Tworzy ręczną kopię zapasową całego katalogu danych (dane + zdjęcia) do podkatalogu kopie/ (FR-1107). Zwraca ścieżkę katalogu kopii.</summary>
    public string UtworzKopieZapasowa()
    {
        lock (_blokada)
        {
            var katalog = Path.Combine(KatalogDanych, "kopie", DateTime.Now.ToString("yyyy-MM-dd-HHmmss", System.Globalization.CultureInfo.InvariantCulture));
            Directory.CreateDirectory(katalog);
            foreach (var plik in Directory.EnumerateFiles(KatalogDanych, "*", SearchOption.TopDirectoryOnly))
                File.Copy(plik, Path.Combine(katalog, Path.GetFileName(plik)));
            if (Directory.Exists(Path.Combine(KatalogDanych, "zdjecia")))
                KopiujKatalog(Path.Combine(KatalogDanych, "zdjecia"), Path.Combine(katalog, "zdjecia"));
            return katalog;
        }
    }

    private static void KopiujKatalog(string zrodlo, string cel)
    {
        Directory.CreateDirectory(cel);
        foreach (var plik in Directory.EnumerateFiles(zrodlo, "*", SearchOption.AllDirectories))
        {
            var wzgledna = Path.GetRelativePath(zrodlo, plik);
            var docelowy = Path.Combine(cel, wzgledna);
            Directory.CreateDirectory(Path.GetDirectoryName(docelowy)!);
            File.Copy(plik, docelowy);
        }
    }

    private static DaneAplikacji NoweDane()
    {
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz(), CzyPierwszeUruchomienie = true };
        dane.SzablonyKart.Add(StartoweSlowniki.SzablonKartyAeg(dane.Slowniki));
        dane.SzablonyKart.Add(StartoweSlowniki.SzablonKartyHpa(dane.Slowniki));
        return dane;
    }

    private static void Waliduj(DaneAplikacji dane)
    {
        if (dane.SchemaVersion > AktualnaWersjaSchematu)
            throw new InvalidDataException("Dane pochodzą z nowszej wersji aplikacji");
    }

    private static void Migruj(DaneAplikacji dane)
    {
        if (dane.SchemaVersion < 1)
            dane.SchemaVersion = 1;
        // kolejne migracje dopisywane tutaj; przed migracją wywołujący robi kopię (FR-1107)
    }

    private sealed class DateOnlyJsonConverter : JsonConverter<DateOnly>
    {
        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => DateOnly.ParseExact(reader.GetString()!, "yyyy-MM-dd", null);

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
    }

    private sealed class DateOnlyNullableJsonConverter : JsonConverter<DateOnly?>
    {
        public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType == JsonTokenType.Null ? null : DateOnly.ParseExact(reader.GetString()!, "yyyy-MM-dd", null);

        public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
            else
                writer.WriteNullValue();
        }
    }
}
