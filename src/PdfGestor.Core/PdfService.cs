using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfGestor.Core;

/// <summary>
/// Operaciones de unir, dividir y extraer páginas.
/// Todo se hace en local: aquí no hay ninguna llamada de red.
/// Los documentos resultantes son nuevos, así que no heredan
/// los metadatos (autor, título…) de los originales.
/// </summary>
public class PdfService
{
    /// <summary>Número de páginas de un PDF.</summary>
    public int GetPageCount(string inputPath)
    {
        using var document = OpenForImport(inputPath);
        return document.PageCount;
    }

    /// <summary>Une varios PDFs, en el orden recibido, en un único archivo.</summary>
    public void Merge(IReadOnlyList<string> inputPaths, string outputPath)
    {
        if (inputPaths.Count < 2)
            throw new PdfGestorException("Necesitas al menos dos PDFs para unirlos.");

        EnsureNotOverwritingInput(outputPath, inputPaths);

        using var output = new PdfDocument();
        foreach (var path in inputPaths)
        {
            using var input = OpenForImport(path);
            foreach (PdfPage page in input.Pages)
                output.AddPage(page);
        }

        Save(output, outputPath);
    }

    /// <summary>
    /// Crea un archivo por cada rango. Ejemplo: "1-3, 4-6" genera dos PDFs.
    /// </summary>
    public IReadOnlyList<string> SplitByRanges(string inputPath, string ranges, string outputDirectory)
    {
        using var input = OpenForImport(inputPath);
        var parsed = PageRangeParser.Parse(ranges, input.PageCount);
        return WriteRanges(input, inputPath, parsed, outputDirectory);
    }

    /// <summary>Crea un archivo cada <paramref name="pagesPerFile"/> páginas (1 = página a página).</summary>
    public IReadOnlyList<string> SplitEvery(string inputPath, int pagesPerFile, string outputDirectory)
    {
        if (pagesPerFile < 1)
            throw new PdfGestorException("El número de páginas por archivo tiene que ser 1 o más.");

        using var input = OpenForImport(inputPath);

        var ranges = new List<PageRange>();
        for (var start = 1; start <= input.PageCount; start += pagesPerFile)
            ranges.Add(new PageRange(start, Math.Min(start + pagesPerFile - 1, input.PageCount)));

        return WriteRanges(input, inputPath, ranges, outputDirectory);
    }

    /// <summary>Copia las páginas indicadas, en ese orden, a un solo PDF nuevo.</summary>
    public void Extract(string inputPath, string ranges, string outputPath)
    {
        EnsureNotOverwritingInput(outputPath, [inputPath]);

        using var input = OpenForImport(inputPath);
        var parsed = PageRangeParser.Parse(ranges, input.PageCount);

        using var output = new PdfDocument();
        foreach (var range in parsed)
            AddRange(input, output, range);

        Save(output, outputPath);
    }

    private static IReadOnlyList<string> WriteRanges(
        PdfDocument input, string inputPath, IReadOnlyList<PageRange> ranges, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var baseName = Path.GetFileNameWithoutExtension(inputPath);
        var created = new List<string>();

        foreach (var range in ranges)
        {
            using var output = new PdfDocument();
            AddRange(input, output, range);

            var path = Path.Combine(outputDirectory, $"{baseName}_p{range}.pdf");
            Save(output, path);
            created.Add(path);
        }

        return created;
    }

    private static void AddRange(PdfDocument input, PdfDocument output, PageRange range)
    {
        // PDFsharp indexa desde 0; los rangos van desde 1.
        for (var i = range.Start - 1; i < range.End; i++)
            output.AddPage(input.Pages[i]);
    }

    private static PdfDocument OpenForImport(string path)
    {
        if (!File.Exists(path))
            throw new PdfGestorException($"No encuentro el archivo \"{path}\".");

        try
        {
            return PdfReader.Open(path, PdfDocumentOpenMode.Import);
        }
        catch (Exception ex) when (ex is not IOException and not UnauthorizedAccessException)
        {
            throw new PdfGestorException(
                $"No se pudo leer \"{Path.GetFileName(path)}\". Puede que esté dañado o protegido con contraseña.", ex);
        }
    }

    private static void Save(PdfDocument document, string outputPath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        document.Save(outputPath);
    }

    private static void EnsureNotOverwritingInput(string outputPath, IEnumerable<string> inputPaths)
    {
        var output = Path.GetFullPath(outputPath);
        if (inputPaths.Any(p => string.Equals(Path.GetFullPath(p), output, StringComparison.OrdinalIgnoreCase)))
            throw new PdfGestorException("El archivo de salida no puede ser uno de los de entrada.");
    }
}
