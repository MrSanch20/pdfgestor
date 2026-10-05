using PdfGestor.Core;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace PdfGestor.Core.Tests;

public sealed class PdfServiceTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("pdfgestor-tests-").FullName;
    private readonly PdfService _service = new();

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    /// <summary>Crea un PDF de prueba con páginas en blanco de distinto ancho,
    /// para poder comprobar qué página acabó dónde.</summary>
    private string CreatePdf(string name, params int[] widths)
    {
        var path = Path.Combine(_dir, name);
        using var doc = new PdfDocument();
        foreach (var width in widths)
        {
            var page = doc.AddPage();
            page.MediaBox = new PdfRectangle(new XPoint(0, 0), new XPoint(width, 842));
        }
        doc.Save(path);
        return path;
    }

    private static int[] Widths(string path)
    {
        using var doc = PdfSharp.Pdf.IO.PdfReader.Open(path, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
        return Enumerable.Range(0, doc.PageCount)
            .Select(i => (int)Math.Round(doc.Pages[i].MediaBox.Width))
            .ToArray();
    }

    [Fact]
    public void Merge_keeps_order_of_inputs()
    {
        var a = CreatePdf("a.pdf", 101, 102);
        var b = CreatePdf("b.pdf", 201);
        var output = Path.Combine(_dir, "out.pdf");

        _service.Merge([b, a], output);

        Assert.Equal(new[] { 201, 101, 102 }, Widths(output));
    }

    [Fact]
    public void Merge_needs_two_files()
    {
        var a = CreatePdf("a.pdf", 101);

        Assert.Throws<PdfGestorException>(() => _service.Merge([a], Path.Combine(_dir, "out.pdf")));
    }

    [Fact]
    public void Merge_refuses_to_overwrite_an_input()
    {
        var a = CreatePdf("a.pdf", 101);
        var b = CreatePdf("b.pdf", 201);

        Assert.Throws<PdfGestorException>(() => _service.Merge([a, b], a));
    }

    [Fact]
    public void SplitEvery_groups_pages_and_keeps_the_remainder()
    {
        var input = CreatePdf("doc.pdf", 101, 102, 103, 104, 105);
        var outDir = Path.Combine(_dir, "out");

        var files = _service.SplitEvery(input, 2, outDir);

        Assert.Equal(new[] { "doc_p1-2.pdf", "doc_p3-4.pdf", "doc_p5.pdf" }, files.Select(f => Path.GetFileName(f)));
        Assert.Equal(new[] { 101, 102 }, Widths(files[0]));
        Assert.Equal(new[] { 105 }, Widths(files[2]));
    }

    [Fact]
    public void SplitByRanges_creates_one_file_per_range()
    {
        var input = CreatePdf("doc.pdf", 101, 102, 103, 104);

        var files = _service.SplitByRanges(input, "1, 3-", _dir);

        Assert.Equal(2, files.Count);
        Assert.Equal(new[] { 101 }, Widths(files[0]));
        Assert.Equal(new[] { 103, 104 }, Widths(files[1]));
    }

    [Fact]
    public void Extract_puts_selected_pages_in_one_file_in_given_order()
    {
        var input = CreatePdf("doc.pdf", 101, 102, 103, 104);
        var output = Path.Combine(_dir, "extracto.pdf");

        _service.Extract(input, "4, 1-2", output);

        Assert.Equal(new[] { 104, 101, 102 }, Widths(output));
    }

    [Fact]
    public void GetPageCount_reads_the_number_of_pages()
    {
        var input = CreatePdf("doc.pdf", 101, 102, 103);

        Assert.Equal(3, _service.GetPageCount(input));
    }

    [Fact]
    public void Invalid_pdf_gives_friendly_error()
    {
        var fake = Path.Combine(_dir, "falso.pdf");
        File.WriteAllText(fake, "esto no es un pdf");

        var ex = Assert.Throws<PdfGestorException>(() => _service.GetPageCount(fake));
        Assert.Contains("falso.pdf", ex.Message);
    }

    [Fact]
    public void Missing_file_gives_friendly_error()
    {
        Assert.Throws<PdfGestorException>(() => _service.GetPageCount(Path.Combine(_dir, "no-existe.pdf")));
    }
}
