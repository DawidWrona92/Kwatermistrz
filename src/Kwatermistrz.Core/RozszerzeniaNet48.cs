#if NET48
using System.Collections.Generic;
using System.Linq;

namespace System
{
    /// <summary>Rozszerzenia zgodne z API .NET 10, których brak w .NET Framework 4.8.</summary>
    public static class RozszerzeniaNet48
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> zrodlo, IEqualityComparer<T> porownywacz)
            => new HashSet<T>(zrodlo, porownywacz);

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> zrodlo) => new HashSet<T>(zrodlo);

        public static bool Contains(this string tekst, char znak) => tekst.IndexOf(znak) >= 0;

        public static bool Contains(this string tekst, string wartosc, StringComparison porownanie)
            => tekst.IndexOf(wartosc, porownanie) >= 0;

        public static bool StartsWith(this string tekst, char znak) => tekst.Length > 0 && tekst[0] == znak;

        public static bool EndsWith(this string tekst, char znak) => tekst.Length > 0 && tekst[tekst.Length - 1] == znak;
    }

    namespace Collections.Generic
    {
        /// <summary>TryAdd dla IDictionary — brak w net48.</summary>
        public static class RozszerzeniaSlownikowNet48
        {
            public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> slownik, TKey klucz, TValue wartosc)
            {
                if (slownik.ContainsKey(klucz))
                    return false;
                slownik.Add(klucz, wartosc);
                return true;
            }
        }
    }

    namespace IO
    {
        /// <summary>Pomocnicy plikowi zgodni z API .NET 10, których brak w .NET Framework 4.8 (File.Move z overwrite, Path.GetRelativePath).</summary>
        public static class PlikiNet48
        {
            /// <summary>File.Move(zrodlo, docelowa, overwrite: true) — brak przeciążenia w net48; najpierw usunięcie celu, potem przeniesienie.</summary>
            public static void PrzeniesNadpisujac(string zrodlo, string docelowa)
            {
                if (File.Exists(docelowa))
                    File.Delete(docelowa);
                File.Move(zrodlo, docelowa);
            }

            /// <summary>Path.GetRelativePath — brak w net48; implementacja przez Uri (obie ścieżki bezwzględne).</summary>
            public static string SciezkaWzgledna(string wzgledem, string sciezka)
            {
                var bezwzgledna1 = Path.GetFullPath(wzgledem);
                var bezwzgledna2 = Path.GetFullPath(sciezka);
                var uri1 = new Uri(bezwzgledna1);
                var uri2 = new Uri(bezwzgledna2);
                return Uri.UnescapeDataString(uri1.MakeRelativeUri(uri2).ToString()).Replace('/', Path.DirectorySeparatorChar);
            }
        }
    }
}
#endif
