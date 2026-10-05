using PdfGestor.Core;

const string Help = """
    PDF Gestor · une y divide PDFs sin subirlos a internet

    Uso:
      pdfgestor unir     <salida.pdf> <entrada1.pdf> <entrada2.pdf> [...]
      pdfgestor dividir  <entrada.pdf> --rangos "1-3, 5, 8-" [--carpeta <dir>]
      pdfgestor dividir  <entrada.pdf> --cada <N>            [--carpeta <dir>]
      pdfgestor extraer  <entrada.pdf> "1-3, 5" <salida.pdf>
      pdfgestor paginas  <entrada.pdf>

    Rangos: números desde 1, separados por comas. "8-" = de la 8 al final.
    Si no indicas --carpeta, los archivos se crean junto al original.
    """;

var service = new PdfService();

try
{
    return args switch
    {
        ["unir" or "merge", var output, .. var inputs] when inputs.Length >= 2
            => Merge(output, inputs),

        ["dividir" or "split", var input, .. var options]
            => Split(input, options),

        ["extraer" or "extract", var input, var ranges, var output]
            => Extract(input, ranges, output),

        ["paginas" or "pages", var input]
            => PageCount(input),

        _ => ShowHelp(args.Length == 0 || args[0] is "-h" or "--help" or "ayuda" ? 0 : 1),
    };
}
catch (PdfGestorException ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 2;
}
catch (IOException ex)
{
    Console.Error.WriteLine($"Error de archivo: {ex.Message}");
    return 3;
}

int Merge(string output, string[] inputs)
{
    service.Merge(inputs, output);
    Console.WriteLine($"Creado {output} a partir de {inputs.Length} archivos.");
    return 0;
}

int Split(string input, string[] options)
{
    string? ranges = null, folder = null;
    int? every = null;

    for (var i = 0; i < options.Length; i++)
    {
        var value = i + 1 < options.Length ? options[i + 1] : null;
        switch (options[i])
        {
            case "--rangos" or "--ranges" when value is not null:
                ranges = value; i++; break;
            case "--cada" or "--every" when value is not null && int.TryParse(value, out var n):
                every = n; i++; break;
            case "--carpeta" or "--out" when value is not null:
                folder = value; i++; break;
            default:
                return ShowHelp(1);
        }
    }

    if ((ranges is null) == (every is null))
        return ShowHelp(1); // hay que elegir uno: --rangos o --cada

    folder ??= Path.GetDirectoryName(Path.GetFullPath(input))!;

    var created = ranges is not null
        ? service.SplitByRanges(input, ranges, folder)
        : service.SplitEvery(input, every!.Value, folder);

    foreach (var file in created)
        Console.WriteLine($"Creado {file}");
    return 0;
}

int Extract(string input, string ranges, string output)
{
    service.Extract(input, ranges, output);
    Console.WriteLine($"Creado {output}");
    return 0;
}

int PageCount(string input)
{
    Console.WriteLine(service.GetPageCount(input));
    return 0;
}

int ShowHelp(int exitCode)
{
    (exitCode == 0 ? Console.Out : Console.Error).WriteLine(Help);
    return exitCode;
}
