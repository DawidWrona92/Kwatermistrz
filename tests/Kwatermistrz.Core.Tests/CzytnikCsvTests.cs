using System.Text;
using Kwatermistrz.Core.Import;

namespace Kwatermistrz.Core.Tests;

public class CzytnikCsvTests
{
    [Fact]
    public void Csv_ZCytowanymiSeparatoramiINowymiLiniami_NieRozbijaKolumn()
    {
        const string csv = "Nazwa;Komentarz;Firma\r\n" +
            "\"Ładownica; AK\";\"Pierwsza linia\r\nDruga \"\"linia\"\"\";\"Producent, Polska\"\r\n";
        var sciezka = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".csv");
        try
        {
            File.WriteAllText(sciezka, csv, new UTF8Encoding(false));
            var arkusz = CzytnikCsv.Wczytaj(sciezka);
            Assert.Equal(2, arkusz.Wiersze.Count);
            Assert.Equal(3, arkusz.Wiersze[1].Komorki.Count);
            Assert.Equal("Ładownica; AK", arkusz.Wiersze[1].Komorka(0));
            Assert.Equal("Pierwsza linia\r\nDruga \"linia\"", arkusz.Wiersze[1].Komorka(1));
            Assert.Equal("Producent, Polska", arkusz.Wiersze[1].Komorka(2));
        }
        finally
        {
            File.Delete(sciezka);
        }
    }

    [Fact]
    public void Csv_ZPrzecinkami_CytowanySrednikWKolumnieNieZaburzaDetekcji()
    {
        const string csv = "\"Nazwa; opis\",Firma,Uwagi\n\"Kamizelka; light\",ACME,\"test, OK\"\n";
        var sciezka = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".csv");
        try
        {
            File.WriteAllText(sciezka, csv);
            var arkusz = CzytnikCsv.Wczytaj(sciezka);
            Assert.Equal(3, arkusz.Wiersze[0].Komorki.Count);
            Assert.Equal("Nazwa; opis", arkusz.Wiersze[0].Komorka(0));
            Assert.Equal("Kamizelka; light", arkusz.Wiersze[1].Komorka(0));
            Assert.Equal("test, OK", arkusz.Wiersze[1].Komorka(2));
        }
        finally
        {
            File.Delete(sciezka);
        }
    }

    [Fact]
    public void Csv_Cp1250_PolskieLiterySaCzytelne()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var sciezka = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".csv");
        try
        {
            File.WriteAllBytes(sciezka, Encoding.GetEncoding(1250).GetBytes("Nazwa;Firma\nŁadownica;Łódź\n"));
            var arkusz = CzytnikCsv.Wczytaj(sciezka);
            Assert.Equal("Ładownica", arkusz.Wiersze[1].Komorka(0));
            Assert.Equal("Łódź", arkusz.Wiersze[1].Komorka(1));
        }
        finally
        {
            File.Delete(sciezka);
        }
    }

    [Fact]
    public void Csv_NiedomknietyCudzyslow_ZglaszaBladPrzedImportem()
    {
        var sciezka = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".csv");
        try
        {
            File.WriteAllText(sciezka, "Nazwa;Firma\n\"Niedomknięte;ACME");
            Assert.Throws<InvalidDataException>(() => CzytnikCsv.Wczytaj(sciezka));
        }
        finally
        {
            File.Delete(sciezka);
        }
    }
}
