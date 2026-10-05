namespace PdfGestor.Core;

/// <summary>
/// Interpreta textos de rangos como "1-3, 5, 8-10" o "7-" (de la 7 al final).
/// </summary>
public static class PageRangeParser
{
    public static IReadOnlyList<PageRange> Parse(string text, int pageCount)
    {
        if (pageCount < 1)
            throw new PdfGestorException("El documento no tiene páginas.");

        if (string.IsNullOrWhiteSpace(text))
            throw new PdfGestorException("Escribe al menos un rango de páginas, por ejemplo: 1-3, 5.");

        var ranges = new List<PageRange>();

        foreach (var rawPart in text.Split(',', ';'))
        {
            var part = rawPart.Trim();
            if (part.Length == 0)
                continue;

            ranges.Add(ParsePart(part, pageCount));
        }

        if (ranges.Count == 0)
            throw new PdfGestorException("Escribe al menos un rango de páginas, por ejemplo: 1-3, 5.");

        return ranges;
    }

    private static PageRange ParsePart(string part, int pageCount)
    {
        var dash = part.IndexOf('-');

        int start, end;
        if (dash < 0)
        {
            start = end = ParseNumber(part, part);
        }
        else
        {
            var left = part[..dash].Trim();
            var right = part[(dash + 1)..].Trim();

            start = left.Length == 0 ? 1 : ParseNumber(left, part);
            end = right.Length == 0 ? pageCount : ParseNumber(right, part);
        }

        if (start > end)
            throw new PdfGestorException($"El rango \"{part}\" está al revés: empieza en {start} y acaba en {end}.");

        if (end > pageCount)
            throw new PdfGestorException($"El rango \"{part}\" se sale del documento, que tiene {pageCount} página(s).");

        return new PageRange(start, end);
    }

    private static int ParseNumber(string value, string part)
    {
        if (!int.TryParse(value, out var number) || number < 1)
            throw new PdfGestorException($"No entiendo \"{part}\". Usa números de página desde 1, por ejemplo: 1-3, 5, 8-.");

        return number;
    }
}
