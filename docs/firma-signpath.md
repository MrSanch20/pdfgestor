# Activar la firma de código con SignPath

La firma ya está preparada en `.github/workflows/build.yml`, pero solo se activa cuando existe el secreto `SIGNPATH_API_TOKEN`. Hasta entonces, las Releases salen sin firmar, como hasta ahora.

## 1. Antes de pedirla

- Activa la **autenticación en dos pasos** en GitHub (Settings → Password and authentication). SignPath la exige.
- Comprueba que el README tiene las secciones **Privacidad** y **Política de firma de código** (ya están).

## 2. Solicitud

1. Entra en https://signpath.org/apply y rellena el formulario con:
   - Repositorio: `https://github.com/MrSanch20/pdfgestor`
   - Licencia: MIT
   - Descarga: `https://github.com/MrSanch20/pdfgestor/releases`
   - Descripción: herramienta de escritorio para unir y dividir PDFs en local, sin enviar datos a internet.
2. Espera su respuesta. Te crearán una organización y un proyecto en SignPath.

## 3. Cuando te aprueben (en SignPath)

1. **Instala la GitHub App de SignPath** y dale acceso al repo `pdfgestor`.
2. En el proyecto, en **Trusted Build Systems**, enlaza **GitHub.com**.
3. En **Artifact Configurations**, pega el contenido de `.signpath/artifact-configuration.xml` y márcala como predeterminada.
4. Apunta el **slug del proyecto** (si no es `pdfgestor`) y el de la **política de firma** (normalmente `release-signing`).
5. Crea un **usuario de CI** (o un token de API) con permiso de *submitter* y copia el token.
6. Copia el **Organization ID** (sale en la configuración de la organización).

## 4. Conectar GitHub (en el repo → Settings → Secrets and variables → Actions)

| Tipo | Nombre | Valor |
|---|---|---|
| Secret | `SIGNPATH_API_TOKEN` | el token del paso 3.5 |
| Variable | `SIGNPATH_ORGANIZATION_ID` | el ID del paso 3.6 |
| Variable | `SIGNPATH_PROJECT_SLUG` | solo si no es `pdfgestor` |
| Variable | `SIGNPATH_SIGNING_POLICY_SLUG` | solo si no es `release-signing` |

## 5. Sacar una versión firmada

1. Sube la versión en `Directory.Build.props` (por ejemplo, de `0.1.0` a `0.1.1`) y haz push a `main`.
2. GitHub Actions compila, prueba y manda los `.exe` a SignPath.
3. Te llegará un aviso de SignPath: **aprueba la firma** (tienes 1 hora; si no, el paso falla y basta con relanzarlo).
4. Se publica la Release con los ejecutables firmados.

> La firma no quita el aviso de SmartScreen de golpe: se va quitando a medida que el programa acumula descargas.
