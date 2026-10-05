namespace PdfGestor.Core;

/// <summary>
/// Error esperado y con mensaje apto para mostrar al usuario
/// (rango mal escrito, PDF dañado o protegido, etc.).
/// </summary>
public class PdfGestorException : Exception
{
    public PdfGestorException(string message) : base(message) { }

    public PdfGestorException(string message, Exception inner) : base(message, inner) { }
}
