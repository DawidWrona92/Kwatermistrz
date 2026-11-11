using Kwatermistrz.Core.Import;
using Kwatermistrz.Core.Model;
using Kwatermistrz.Core.Storage;

namespace Kwatermistrz.Core.Tests;

public class ImportHistorycznychLicznikowTests
{
    [Fact]
    public void ImportXlsx_DoIstniejacejBazy_NieNadpisujeDniaZerowegoAniDawnychSum()
    {
        var dane = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        dane.Ustawienia.DzienZerowy = new DateOnly(2021, 1, 1);
        dane.Ustawienia.DataPierwszejWyprawy = new DateOnly(2019, 7, 4);
        dane.Ustawienia.HistoryczneWyprawyWgRok[2019] = 12;
        dane.Wyprawy.Add(new Wyprawa
        {
            Id = "wyprawa-oryginalna", Data = new DateOnly(2019, 7, 4),
        });

        var arkusz = new DaneAplikacji { Slowniki = StartoweSlowniki.Utworz() };
        arkusz.Ustawienia.DataPierwszejWyprawy = new DateOnly(2015, 3, 3);
        arkusz.Ustawienia.HistoryczneWyprawyWgRok[2019] = 99;
        arkusz.Ustawienia.HistoryczneWyprawyWgRok[2020] = 7;
        var podglad = new PodgladImportu
        {
            DzienZerowy = new DateOnly(2026, 1, 1),
            Dane = arkusz,
        };

        TransakcjaDanych.Wykonaj(dane, robocza =>
        {
            ImporterKwatermistrz.Zastosuj(podglad, robocza);
            return true;
        });

        Assert.Equal(new DateOnly(2021, 1, 1), dane.Ustawienia.DzienZerowy);
        Assert.Equal(new DateOnly(2019, 7, 4), dane.Ustawienia.DataPierwszejWyprawy);
        Assert.Equal(12, dane.Ustawienia.HistoryczneWyprawyWgRok[2019]);
        Assert.Equal(7, dane.Ustawienia.HistoryczneWyprawyWgRok[2020]);
        Assert.Equal("wyprawa-oryginalna", Assert.Single(dane.Wyprawy).Id);
        Assert.Equal(1, dane.SchemaVersion);
    }
}
