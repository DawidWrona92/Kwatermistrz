using System.IO.Compression;
using Kwatermistrz.Core.Konfiguracja;

namespace Kwatermistrz.Core.Tests;

public class ImportZdjecRekonstrukcjiTests
{
    [Fact]
    public void Kwreko_NieNadpisujeIstniejacegoZdjeciaAniNieWychodziPozaKatalog()
    {
        var katalog = Path.Combine(Path.GetTempPath(), "kwreko-photo-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(katalog);
        try
        {
            var dane = Path.Combine(katalog, "dane");
            var zdjecie = Path.Combine(dane, "zdjecia", "istniejace.jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(zdjecie)!);
            File.WriteAllText(zdjecie, "MÓJ PLIK");
            var eksport = Path.Combine(katalog, "test.kwreko");
            using (var zip = ZipFile.Open(eksport, ZipArchiveMode.Create))
            {
                void Dodaj(string nazwa, string tresc)
                {
                    var wpis = zip.CreateEntry(nazwa);
                    using var pisarz = new StreamWriter(wpis.Open());
                    pisarz.Write(tresc);
                }
                Dodaj("zdjecia/zdjecia/istniejace.jpg", "NADPISANE");
                Dodaj("zdjecia/zdjecia/nowe.jpg", "NOWE");
                Dodaj("zdjecia/../spoza.txt", "ZŁY PLIK");
            }

            var ile = EksportRekonstrukcji.WyodrebnijZdjecia(eksport, dane);
            Assert.Equal(1, ile);
            Assert.Equal("MÓJ PLIK", File.ReadAllText(zdjecie));
            Assert.Equal("NOWE", File.ReadAllText(Path.Combine(dane, "zdjecia", "nowe.jpg")));
            Assert.False(File.Exists(Path.Combine(dane, "spoza.txt")));
            Assert.Empty(Directory.EnumerateFiles(Path.Combine(dane, "zdjecia"), "*.tmp"));
        }
        finally
        {
            Directory.Delete(katalog, recursive: true);
        }
    }
}
