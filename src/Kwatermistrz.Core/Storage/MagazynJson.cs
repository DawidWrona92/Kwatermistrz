using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Storage;

/// <summary>Wynik kontroli integralności przy starcie (NFR-05).</summary>
public class WynikKontroli
{
    public bool CzyPoprawne { get; set; }
    public string? Komunikat { get; set; }
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
            var dane = DeserializujZMigracja(json) ?? NoweDane();
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
                var dane = DeserializujZMigracja(json) ?? throw new InvalidDataException("Pusty dokument");
                Waliduj(dane);
                Migruj(dane);
                return (dane, new WynikKontroli { CzyPoprawne = true, Zrodlo = _sciezka });
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException)
            {
                // odczyt .bak też w try — uszkodzony główny i .bak nie mogą wywalić startu
                try
                {
                    var kopia = _sciezka + ".bak";
                    if (File.Exists(kopia))
                    {
                        var json = File.ReadAllText(kopia);
                        var dane = DeserializujZMigracja(json);
                        if (dane != null)
                        {
                            Waliduj(dane);
                            Migruj(dane);
                            return (dane, new WynikKontroli { CzyPoprawne = false, Komunikat = "Odczytano ostatnią kopię zapasową", Zrodlo = kopia });
                        }
                    }
                }
                catch (Exception exBak) when (exBak is IOException or InvalidDataException or System.Text.Json.JsonException)
                {
                    // oba pliki uszkodzone — pusty dokument + komunikat
                }
                var nowe = NoweDane();
                return (nowe, new WynikKontroli { CzyPoprawne = false, Komunikat = "Nie udało się odczytać danych; utworzono pusty dokument", Zrodlo = null });
            }
        }
    }

    /// <summary>Wczytuje dane z konkretnego pliku (przywracanie z kopii, FR-1107). Przy błędzie rzuca wyjątek.</summary>
    public static DaneAplikacji WczytajZPliku(string sciezka)
    {
        var json = File.ReadAllText(sciezka);
        var dane = DeserializujZMigracja(json) ?? throw new InvalidDataException("Pusty dokument");
        if (dane.SchemaVersion > AktualnaWersjaSchematu)
            throw new InvalidDataException("Dane pochodzą z nowszej wersji aplikacji");
        Migruj(dane);
        return dane;
    }

    /// <summary>Wczytuje dane ze strumienia (eksport rekonstrukcji, testy). Bez migracji — eksport zachowuje schemat źródła.</summary>
    public static DaneAplikacji WczytajZeStrumienia(Stream strumien)
    {
        var dane = JsonSerializer.Deserialize<DaneAplikacji>(strumien, Opcje) ?? throw new InvalidDataException("Pusty dokument");
        return dane;
    }

    /// <summary>Zapisuje dane do strumienia w formacie magazynu (eksport rekonstrukcji, testy).</summary>
    public static void ZapiszDoStrumienia(DaneAplikacji dane, Stream strumien)
    {
        JsonSerializer.Serialize(strumien, dane, Opcje);
    }

    /// <summary>Zapisuje dane atomowo: najpierw plik tymczasowy, potem zamiana. Po powrocie tej metody dane są na dysku (NFR-05).</summary>
    public void Zapisz(DaneAplikacji dane)
    {
        lock (_blokada)
        {
            Directory.CreateDirectory(KatalogDanych);
            dane.OstatniaZmiana = DateTime.Now;
            var start = System.Diagnostics.Stopwatch.StartNew();
            var json = JsonSerializer.Serialize(dane, Opcje);
            var tmp = _sciezka + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(_sciezka))
            {
                var kopia = _sciezka + ".bak";
                try
                {
                    File.Copy(_sciezka, kopia, overwrite: true);
                }
                catch (IOException)
                {
                    // kopia .bak zablokowana (antywirus, synchronizacja) — stara kopia .bak zostaje; zapis główny musi się udać
                }
            }
            File.Move(tmp, _sciezka, overwrite: true);
            _brudne = false;
            start.Stop();
            if (start.ElapsedMilliseconds > 500)
                ZanotujWolnyZapis(start.ElapsedMilliseconds);
        }
    }

    /// <summary>Flaga „są niezapisane zmiany” — ustawiana przez ZapiszDebounce, zdejmowana przez Zapisz/ZapiszJezeliBrudne.</summary>
    private bool _brudne;

    /// <summary>Odkłada zapis na później (debounce): znacznik „brudne” spłukuje okresowy ZapiszJezeliBrudne na wątku UI
    /// (DispatcherTimer w App) oraz ZapiszJezeliBrudne przy zamykaniu i przed kopią zapasową. Krytyczne ścieżki
    /// (przywracanie danych, kopie) nadal wołają Zapisz bezpośrednio.</summary>
    public void ZapiszDebounce(DaneAplikacji dane)
    {
        lock (_blokada)
        {
            _brudne = true;
            dane.OstatniaZmiana = DateTime.Now;
        }
    }

    /// <summary>Czy są odłożone (niezapisane) zmiany.</summary>
    public bool CzyBrudne
    {
        get { lock (_blokada) return _brudne; }
    }

    /// <summary>Spłukuje odłożony zapis (debounce), gdy istnieje. Bezpieczne także, gdy nic nie jest brudne.</summary>
    public void ZapiszJezeliBrudne(DaneAplikacji dane)
    {
        lock (_blokada)
        {
            if (!_brudne)
                return;
        }
        Zapisz(dane);
    }

    /// <summary>Notuje powolny zapis do error.log — pomiar czasu zapisu (diagnostyka rosnącego katalogu zdjęć w JSON).</summary>
    private void ZanotujWolnyZapis(long milisekundy)
    {
        try
        {
            File.AppendAllText(Path.Combine(KatalogDanych, "error.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Zapis danych trwał {milisekundy} ms (próg 500 ms){Environment.NewLine}");
        }
        catch
        {
            // logowanie nie może być przyczyną kolejnego wyjątku
        }
    }

    /// <summary>Maksymalna liczba trzymanych kopii zapasowych (FR-1108); starsze są usuwane (najstarsze pierwsze).</summary>
    public const int MaksymalnaLiczbaKopii = 20;

    /// <summary>Tworzy ręczną kopię zapasową całego katalogu danych (dane + zdjęcia) do podkatalogu kopie/ (FR-1107).
    /// Po kopii egzekwowana jest retencja (MaksymalnaLiczbaKopii). Zwraca ścieżkę katalogu kopii.</summary>
    public string UtworzKopieZapasowa(DaneAplikacji? dane = null)
    {
        lock (_blokada)
        {
            // kopia ma zawierać bieżący (też odłożony debounce'em) stan — najpierw spłukujemy zapis
            if (dane != null && _brudne)
                Zapisz(dane);
            // nazwa z dokładnością do sekundy — dwie kopie w tej samej sekundzie dostają sufiks
            var baza = DateTime.Now.ToString("yyyy-MM-dd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
            var katalog = Path.Combine(KatalogDanych, "kopie", baza);
            var i = 2;
            while (Directory.Exists(katalog))
                katalog = Path.Combine(KatalogDanych, "kopie", $"{baza}-{i++}");
            Directory.CreateDirectory(katalog);
            foreach (var plik in Directory.EnumerateFiles(KatalogDanych, "*", SearchOption.TopDirectoryOnly))
                File.Copy(plik, Path.Combine(katalog, Path.GetFileName(plik)));
            if (Directory.Exists(Path.Combine(KatalogDanych, "zdjecia")))
                KopiujKatalog(Path.Combine(KatalogDanych, "zdjecia"), Path.Combine(katalog, "zdjecia"));
            UsunStareKopie();
            return katalog;
        }
    }

    /// <summary>Usuwa najstarsze kopie ponad limit (FR-1108) — katalogi posortowane wg nazwy (data w nazwie).</summary>
    private void UsunStareKopie()
    {
        var katalogKopii = Path.Combine(KatalogDanych, "kopie");
        if (!Directory.Exists(katalogKopii))
            return;
        var kopie = Directory.EnumerateDirectories(katalogKopii)
            .OrderBy(d => d, StringComparer.Ordinal)
            .ToList();
        foreach (var stara in kopie.Take(Math.Max(0, kopie.Count - MaksymalnaLiczbaKopii)))
        {
            try
            {
                Directory.Delete(stara, recursive: true);
            }
            catch (IOException)
            {
                // kopia w użyciu (druga instancja) — zostaje na następny raz
            }
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
        // kontrola integralności (NFR-05) — minimum: Id muszą być unikalne w obrębie każdej kolekcji,
        // inaczej wyszukiwanie po Id (FirstOrDefault) zwracałoby „pierwszy lepszy” rekord i sumy by się rozjeżdżały
        static void SprawdzUnikalnosc<T>(IEnumerable<T> kolekcja, Func<T, string> id, string nazwa)
        {
            var duplikaty = kolekcja.GroupBy(id, StringComparer.Ordinal).Where(g => g.Count() > 1).ToList();
            if (duplikaty.Count > 0)
                throw new InvalidDataException($"Zduplikowane Id w kolekcji {nazwa}: {string.Join(", ", duplikaty.Select(g => g.Key))}");
        }
        SprawdzUnikalnosc(dane.Przedmioty, p => p.Id, "Przedmioty");
        SprawdzUnikalnosc(dane.Repliki, r => r.Id, "Repliki");
        SprawdzUnikalnosc(dane.Zestawy, z => z.Id, "Zestawy");
        SprawdzUnikalnosc(dane.Rekonstrukcje, r => r.Id, "Rekonstrukcje");
        SprawdzUnikalnosc(dane.SzablonyKart, s => s.Id, "SzablonyKart");
    }

    private static void Migruj(DaneAplikacji dane)
    {
        if (dane.SchemaVersion < 1)
            dane.SchemaVersion = 1;
        // kolejne migracje dopisywane tutaj; przed migracją wywołujący robi kopię (FR-1107)
    }

    /// <summary>Deserializuje dokument danych; przed deserializacją przepisuje stare indeksy wariantów (format 1.0.0) na Id.
    /// Strażnik oszczędności: podwójne parsowanie (JsonNode + deserializacja) robimy tylko dla plików 1.0.0,
    /// rozpoznawanych po polu „IndeksWariantuDomyslnego” — pliki 1.0.1 i nowsze czytane są jednym przejściem.</summary>
    private static DaneAplikacji? DeserializujZMigracja(string json)
    {
        if (!json.Contains("IndeksWariantuDomyslnego", StringComparison.Ordinal))
            return JsonSerializer.Deserialize<DaneAplikacji>(json, Opcje);
        var node = JsonNode.Parse(json);
        if (node is JsonObject doc)
            PrzepiszStareIndeksyWariantow(doc);
        return JsonSerializer.Deserialize<DaneAplikacji>(node, Opcje);
    }

    /// <summary>Migracja 1.0.1: wyprawy i zestawy trzymały wybór wariantu jako INDEKS na liście wariantów slotu
    /// (pola WybraneWarianty / OstatnieWarianty jako liczby, IndeksWariantuDomyslnego; warianty bez Id).
    /// Edycja zestawu przepisywała wtedy historię wypraw. Indeksy przepisujemy na Id wariantu
    /// w węłach JSON przed deserializacją — działa też dla plików 1.0.0, których warianty nie mają jeszcze Id.</summary>
    private static void PrzepiszStareIndeksyWariantow(JsonObject doc)
    {
        if (doc["Zestawy"] is not JsonArray zestawy)
            return;

        // 1. wariantom bez Id przypisz nowe (pliki 1.0.0 nie mają Id wariantów)
        foreach (var slot in Sloty(zestawy))
        {
            if (slot["Warianty"] is not JsonArray warianty)
                continue;
            foreach (var wariant in warianty.OfType<JsonObject>())
            {
                if (wariant["Id"] is not JsonValue idWartosc
                    || !idWartosc.TryGetValue<string>(out var id)
                    || string.IsNullOrEmpty(id))
                    wariant["Id"] = Guid.NewGuid().ToString("N");
            }
        }

        // 2. mapa slotId → Id wariantów (w kolejności) + stary indeks domyślnego → WariantDomyslnyId
        var idyWgSlotu = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var slot in Sloty(zestawy))
        {
            var slotId = slot["Id"]?.GetValue<string>();
            if (slotId == null || slot["Warianty"] is not JsonArray warianty)
                continue;
            var idy = warianty.OfType<JsonObject>()
                .Select(w => w["Id"]?.GetValue<string>() ?? "")
                .ToList();
            idyWgSlotu[slotId] = idy;

            if (slot["IndeksWariantuDomyslnego"] is JsonValue indeksWartosc
                && indeksWartosc.TryGetValue<int>(out var indeks)
                && indeks >= 0 && indeks < idy.Count)
            {
                slot["WariantDomyslnyId"] = idy[indeks];
                slot.Remove("IndeksWariantuDomyslnego");
            }
        }

        // 3. słowniki slotId → wartość: liczby (stare indeksy) przepisz na Id wariantu
        foreach (var zestaw in zestawy.OfType<JsonObject>())
            PrzepiszSlownikNaId(zestaw, "OstatnieWarianty", idyWgSlotu);
        if (doc["Wyprawy"] is JsonArray wyprawy)
            foreach (var wyprawa in wyprawy.OfType<JsonObject>())
                PrzepiszSlownikNaId(wyprawa, "WybraneWarianty", idyWgSlotu);
    }

    private static IEnumerable<JsonObject> Sloty(JsonArray zestawy)
    {
        foreach (var zestaw in zestawy.OfType<JsonObject>())
            if (zestaw["Sloty"] is JsonArray sloty)
                foreach (var slot in sloty.OfType<JsonObject>())
                    yield return slot;
    }

    private static void PrzepiszSlownikNaId(JsonObject kontener, string nazwaPola, Dictionary<string, List<string>> idyWgSlotu)
    {
        if (kontener[nazwaPola] is not JsonObject slownik)
            return;
        var zmiany = new List<(string Klucz, string Wartosc)>();
        foreach (var (klucz, wartosc) in slownik)
        {
            if (wartosc is JsonValue v
                && v.TryGetValue<int>(out var indeks)
                && klucz != null
                && idyWgSlotu.TryGetValue(klucz, out var idy)
                && indeks >= 0 && indeks < idy.Count)
                zmiany.Add((klucz, idy[indeks]));
        }
        foreach (var (klucz, wartosc) in zmiany)
            slownik[klucz] = wartosc;
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
