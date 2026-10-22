using System.Text.Json;
using Kwatermistrz.Core.Import;

namespace Kwatermistrz.Core.Storage;

/// <summary>
/// Magazyn zapisanych mapowań kolumn importu ogólnego (FR-1403) — plik mapowania-kolumn.json
/// w katalogu danych. Mapowania można zapisać i użyć ponownie.
/// </summary>
public class MagazynMapowan
{
    private static readonly JsonSerializerOptions Opcje = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly string _sciezka;

    public MagazynMapowan(string katalogDanych)
    {
        _sciezka = Path.Combine(katalogDanych, "mapowania-kolumn.json");
    }

    /// <summary>Wczytuje zapisane mapowania; pusta lista, gdy pliku nie ma.</summary>
    public List<MapowanieKolumn> Wczytaj()
    {
        if (!File.Exists(_sciezka))
            return new List<MapowanieKolumn>();
        var json = File.ReadAllText(_sciezka);
        return JsonSerializer.Deserialize<List<MapowanieKolumn>>(json, Opcje) ?? new List<MapowanieKolumn>();
    }

    /// <summary>Zapisuje listę mapowań (atomowo: plik tymczasowy + zamiana, NFR-05).</summary>
    public void Zapisz(List<MapowanieKolumn> mapowania)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_sciezka)!);
        var json = JsonSerializer.Serialize(mapowania, Opcje);
        var tmp = _sciezka + ".tmp";
        File.WriteAllText(tmp, json);
        File.Move(tmp, _sciezka, overwrite: true);
    }

    /// <summary>Zapisuje lub nadpisuje jedno mapowanie (po nazwie) i zwraca zaktualizowaną listę.</summary>
    public List<MapowanieKolumn> ZapiszJedno(MapowanieKolumn mapowanie)
    {
        var lista = Wczytaj();
        lista.RemoveAll(m => m.Nazwa == mapowanie.Nazwa);
        lista.Add(mapowanie);
        Zapisz(lista);
        return lista;
    }

    /// <summary>Usuwa mapowanie po nazwie.</summary>
    public void Usun(string nazwa)
    {
        var lista = Wczytaj();
        if (lista.RemoveAll(m => m.Nazwa == nazwa) > 0)
            Zapisz(lista);
    }
}
