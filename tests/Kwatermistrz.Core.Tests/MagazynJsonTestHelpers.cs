using System.Text.Json;
using System.Text.Json.Serialization;
using Kwatermistrz.Core.Model;

namespace Kwatermistrz.Core.Tests;

/// <summary>Pomocnicze API dla testów: serializacja słowników w formacie magazynu (z konwerterami enumów).</summary>
public static class MagazynJsonTestHelpers
{
    private static readonly JsonSerializerOptions Opcje = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Serializuje słowniki do JSON (fragment dokumentu dane.json).</summary>
    public static string SerializujSlowniki(this Slowniki slowniki)
        => JsonSerializer.Serialize(slowniki, Opcje);
}
