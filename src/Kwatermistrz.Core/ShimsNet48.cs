#if NET48
using System.Globalization;

namespace System
{
    /// <summary>Shim DateOnly dla .NET Framework 4.8 — logika identyczna jak System.DateOnly (data bez czasu), zakres 1900-01-01..9999-12-31.</summary>
    public readonly struct DateOnly : IEquatable<DateOnly>, IComparable<DateOnly>
    {
        private readonly int _liczbaDni; // dni od 1900-01-01
        private static readonly DateTime Poczatek = new(1900, 1, 1);

        private DateOnly(int liczbaDni) { _liczbaDni = liczbaDni; }

        public DateOnly(int rok, int miesiac, int dzien)
            : this(new DateTime(rok, miesiac, dzien).Subtract(Poczatek).Days) { }

        public int Year => ToDateTime().Year;
        public int Month => ToDateTime().Month;
        public int Day => ToDateTime().Day;
        public DayOfWeek DayOfWeek => ToDateTime().DayOfWeek;
        public int DayNumber => _liczbaDni;

        public static DateOnly FromDateTime(DateTime dateTime) => new((int)(dateTime.Date - Poczatek).Days);

        public DateTime ToDateTime() => Poczatek.AddDays(_liczbaDni);

        public DateOnly AddDays(int dni)
        {
            var dt = ToDateTime().AddDays(dni);
            return FromDateTime(dt);
        }

        public DateOnly AddMonths(int miesiace) => FromDateTime(ToDateTime().AddMonths(miesiace));

        public static DateOnly ParseExact(string s, string format, IFormatProvider? provider)
        {
            if (!TryParseExact(s, format, provider, out var wynik))
                throw new FormatException("Niepoprawna data: " + s);
            return wynik;
        }

        public static bool TryParseExact(string s, string format, IFormatProvider? provider, out DateOnly result)
        {
            result = default;
            if (format != "yyyy-MM-dd" || s is null || s.Length != 10 || s[4] != '-' || s[7] != '-')
                return false;
            if (!int.TryParse(s.Substring(0, 4), NumberStyles.Integer, CultureInfo.InvariantCulture, out var rok)
                || !int.TryParse(s.Substring(5, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var miesiac)
                || !int.TryParse(s.Substring(8, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var dzien))
                return false;
            try { result = new DateOnly(new DateTime(rok, miesiac, dzien).Subtract(Poczatek).Days); }
            catch (ArgumentOutOfRangeException) { return false; }
            return true;
        }

        public static bool TryParseExact(string s, string[] formats, IFormatProvider? provider, out DateOnly result)
            => TryParseExact(s, formats != null && formats.Length > 0 ? formats[0] : "yyyy-MM-dd", provider, out result);

        public static DateOnly Parse(string s) => ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        public static bool TryParse(string s, out DateOnly result) => TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, out result);

        public bool Equals(DateOnly other) => _liczbaDni == other._liczbaDni;
        public override bool Equals(object? obj) => obj is DateOnly other && Equals(other);
        public override int GetHashCode() => _liczbaDni;
        public int CompareTo(DateOnly other) => _liczbaDni.CompareTo(other._liczbaDni);

        public static bool operator ==(DateOnly lewa, DateOnly prawa) => lewa.Equals(prawa);
        public static bool operator !=(DateOnly lewa, DateOnly prawa) => !lewa.Equals(prawa);
        public static bool operator <(DateOnly lewa, DateOnly prawa) => lewa.CompareTo(prawa) < 0;
        public static bool operator >(DateOnly lewa, DateOnly prawa) => lewa.CompareTo(prawa) > 0;
        public static bool operator <=(DateOnly lewa, DateOnly prawa) => lewa.CompareTo(prawa) <= 0;
        public static bool operator >=(DateOnly lewa, DateOnly prawa) => lewa.CompareTo(prawa) >= 0;

        public override string ToString() => ToString("yyyy-MM-dd", null);

        public string ToString(string format, IFormatProvider? provider)
        {
            var dt = ToDateTime();
            if (format == "yyyy-MM-dd")
                return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (format == "dd.MM.yyyy")
                return dt.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            return dt.ToString(format, provider ?? CultureInfo.InvariantCulture);
        }
    }
}

namespace System.Runtime.CompilerServices
{
    /// <summary>Shim IsExternalInit dla net48 — pozwala kompilować „init” i „record” (C# 9+ na net48).</summary>
    public static class IsExternalInit { }
}
#endif
