# PDF Gestor

**Une y divide PDFs en tu ordenador, sin subirlos a ninguna web.**

[![Compilar y probar](https://github.com/MrSanch20/pdfgestor/actions/workflows/build.yml/badge.svg)](https://github.com/MrSanch20/pdfgestor/actions/workflows/build.yml)
![.NET 8](https://img.shields.io/badge/.NET-8-512BD4)
![Licencia MIT](https://img.shields.io/badge/licencia-MIT-green)

Las webs para unir o dividir PDFs te piden subir el archivo a sus servidores. Si es una nómina, un contrato o un DNI escaneado, no sabes qué pasa después con él.

PDF Gestor hace lo mismo **en tu equipo**:

- 🔒 **Nada sale de tu ordenador.** No hay conexión a internet, ni telemetría, ni cuentas. Funciona con el cable desenchufado.
- 👀 **Código abierto.** Cualquiera puede revisar que no envía nada.
- 🧹 **Sin metadatos heredados.** Los PDFs que genera son nuevos: no copian el autor, el título ni otros datos de los originales.
- 📦 **Portable.** Un solo `.exe`, sin instalar nada (ni siquiera .NET).

## Qué hace

| | |
|---|---|
| **Unir** | Varios PDFs en uno, en el orden que elijas. Arrastrar y soltar, subir y bajar. |
| **Dividir página a página** | Un PDF por cada página. |
| **Dividir cada N páginas** | Bloques de N páginas (el último lleva las que sobren). |
| **Dividir por rangos** | Un archivo por cada rango: `1-3, 5, 8-` |
| **Extraer páginas** | Las páginas que elijas, en ese orden, a un solo PDF. |

Los rangos se escriben con números desde 1 separados por comas. `8-` significa "de la 8 al final".

## Descargar

Ve a [Releases](https://github.com/MrSanch20/pdfgestor/releases) y descarga:

- `PdfGestor.exe`: la aplicación con ventana.
- `pdfgestor-cli.exe`: la versión de consola, para scripts.

> Como es un programa nuevo, el navegador o Windows pueden avisar al descargarlo o abrirlo la primera vez:
> - **Chrome / Edge**: en el aviso de la descarga, pulsa **Conservar**.
> - **Windows SmartScreen**: pulsa **Más información → Ejecutar de todas formas**.
>
> Si prefieres no fiarte, compílalo tú mismo (abajo).

## Consola

```text
pdfgestor-cli unir     salida.pdf a.pdf b.pdf c.pdf
pdfgestor-cli dividir  informe.pdf --cada 2
pdfgestor-cli dividir  informe.pdf --rangos "1-3, 5, 8-" --carpeta partes
pdfgestor-cli extraer  informe.pdf "4, 1-2" extracto.pdf
pdfgestor-cli paginas  informe.pdf
```

## Compilar desde el código

Necesitas el [SDK de .NET 8](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/MrSanch20/pdfgestor.git
cd pdfgestor
dotnet test
dotnet run --project src/PdfGestor.App
```

Para generar el `.exe` portable:

```bash
dotnet publish src/PdfGestor.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

## Estructura

```text
src/
  PdfGestor.Core/   Lógica (unir, dividir, rangos). Sin interfaz, reutilizable.
  PdfGestor.App/    Aplicación de escritorio (WinForms).
  PdfGestor.Cli/    Versión de consola.
tests/
  PdfGestor.Core.Tests/   Tests con xUnit.
```

La lógica está separada de la interfaz: la app y la consola usan el mismo `PdfService`, y los tests lo prueban sin abrir ninguna ventana. Cada subida a `main` se compila y se prueba en GitHub Actions. Cuando cambia la versión de `Directory.Build.props`, se publica automáticamente una Release nueva con los ejecutables, firmados con SignPath si está configurado.

## Tecnologías

- .NET 8 y C#
- [PDFsharp](https://github.com/empira/PDFsharp) (MIT) para leer y escribir PDFs
- WinForms
- xUnit
- GitHub Actions

## Privacidad

Este programa no transfiere ninguna información a otros sistemas de la red, salvo que lo pida expresamente el usuario o quien lo instale o utilice.

*This program will not transfer any information to other networked systems unless specifically requested by the user or the person installing or operating it.*

PDF Gestor no tiene ninguna función de red: no comprueba actualizaciones, no envía estadísticas y no usa servicios externos.

## Política de firma de código

Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by [SignPath Foundation](https://signpath.org).

*Firma de código gratuita proporcionada por SignPath.io, con certificado de SignPath Foundation.*

Los ejecutables se compilan con GitHub Actions a partir del código de este repositorio y se firman en ese mismo proceso. Cada versión se aprueba a mano antes de firmarla.

| Rol | Personas |
|---|---|
| Committers y revisores | [Daniel Sancho Goñi](https://github.com/MrSanch20) |
| Aprobadores | [Daniel Sancho Goñi](https://github.com/MrSanch20) |

## Licencia

[MIT](LICENSE) © Daniel Sancho Goñi
