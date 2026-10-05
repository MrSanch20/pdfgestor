namespace PdfGestor.Core;

/// <summary>
/// Rango de páginas, numeradas desde 1 e inclusivo en ambos extremos.
/// </summary>
public readonly record struct PageRange(int Start, int End)
{
    public int Count => End - Start + 1;

    /// <summary>Texto para nombres de archivo: "3" o "1-5".</summary>
    public override string ToString() => Start == End ? $"{Start}" : $"{Start}-{End}";
}
