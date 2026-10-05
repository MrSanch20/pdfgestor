using PdfGestor.Core;

namespace PdfGestor.App;

/// <summary>
/// Ventana principal con dos pestañas: Unir y Dividir.
/// La interfaz está hecha en código (sin diseñador) para que sea fácil de leer.
/// </summary>
public sealed class MainForm : Form
{
    private readonly PdfService _service = new();

    // Pestaña "Unir"
    private readonly ListBox _mergeList = new();

    // Pestaña "Dividir"
    private readonly TextBox _splitInput = new();
    private readonly Label _splitPages = new();
    private readonly RadioButton _modeEachPage = new();
    private readonly RadioButton _modeEvery = new();
    private readonly RadioButton _modeRanges = new();
    private readonly RadioButton _modeExtract = new();
    private readonly NumericUpDown _everyCount = new();
    private readonly TextBox _rangesText = new();

    private readonly ToolStripStatusLabel _status = new();

    public MainForm()
    {
        Text = "PDF Gestor · une y divide PDFs sin subirlos a internet";
        MinimumSize = new Size(640, 460);
        Size = new Size(720, 520);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10f);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildMergeTab());
        tabs.TabPages.Add(BuildSplitTab());

        var statusStrip = new StatusStrip();
        statusStrip.Items.Add(_status);
        SetStatus("Todo se procesa en tu equipo. Nada se envía a internet.");

        Controls.Add(tabs);
        Controls.Add(statusStrip);
    }

    // ───────────────────────────── Unir ─────────────────────────────

    private TabPage BuildMergeTab()
    {
        var page = new TabPage("Unir") { Padding = new Padding(10) };

        var hint = new Label
        {
            Text = "Arrastra aquí los PDFs (o pulsa Añadir) y ordénalos. Se unirán de arriba abajo.",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 8),
        };

        _mergeList.Dock = DockStyle.Fill;
        _mergeList.SelectionMode = SelectionMode.MultiExtended;
        _mergeList.HorizontalScrollbar = true;
        _mergeList.AllowDrop = true;
        _mergeList.DragEnter += OnDragEnter;
        _mergeList.DragDrop += (_, e) => AddToMergeList(GetDroppedPdfs(e));

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.TopDown,
            Width = 150,
            Padding = new Padding(8, 0, 0, 0),
        };
        buttons.Controls.Add(MakeButton("Añadir…", (_, _) => AddToMergeList(PickPdfs(multiple: true))));
        buttons.Controls.Add(MakeButton("Subir", (_, _) => MoveSelected(-1)));
        buttons.Controls.Add(MakeButton("Bajar", (_, _) => MoveSelected(+1)));
        buttons.Controls.Add(MakeButton("Quitar", (_, _) => RemoveSelected()));
        buttons.Controls.Add(MakeButton("Vaciar", (_, _) => _mergeList.Items.Clear()));

        var mergeButton = MakeButton("Unir PDFs…", (_, _) => Merge());
        mergeButton.Dock = DockStyle.Bottom;
        mergeButton.Height = 40;

        page.Controls.Add(_mergeList);
        page.Controls.Add(buttons);
        page.Controls.Add(hint);
        page.Controls.Add(mergeButton);
        return page;
    }

    private void AddToMergeList(IEnumerable<string> files)
    {
        foreach (var file in files)
            _mergeList.Items.Add(new PdfItem(file));
    }

    private void MoveSelected(int direction)
    {
        var indices = _mergeList.SelectedIndices.Cast<int>().OrderBy(i => i).ToList();
        if (indices.Count == 0) return;
        if (direction < 0 && indices[0] == 0) return;
        if (direction > 0 && indices[^1] == _mergeList.Items.Count - 1) return;

        // Al bajar se recorren de abajo arriba para no pisarse entre sí.
        if (direction > 0) indices.Reverse();

        foreach (var i in indices)
        {
            var item = _mergeList.Items[i];
            _mergeList.Items.RemoveAt(i);
            _mergeList.Items.Insert(i + direction, item);
        }

        _mergeList.ClearSelected();
        foreach (var i in indices)
            _mergeList.SetSelected(i + direction, true);
    }

    private void RemoveSelected()
    {
        foreach (var i in _mergeList.SelectedIndices.Cast<int>().OrderByDescending(i => i).ToList())
            _mergeList.Items.RemoveAt(i);
    }

    private void Merge()
    {
        var files = _mergeList.Items.Cast<PdfItem>().Select(i => i.Path).ToList();
        if (files.Count < 2)
        {
            ShowWarning("Añade al menos dos PDFs para unirlos.");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = "unido.pdf",
            InitialDirectory = Path.GetDirectoryName(files[0]),
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        Run(() =>
        {
            _service.Merge(files, dialog.FileName);
            return $"Listo: {files.Count} archivos unidos en {Path.GetFileName(dialog.FileName)}.";
        }, folderToOpen: Path.GetDirectoryName(Path.GetFullPath(dialog.FileName)));
    }

    // ──────────────────────────── Dividir ────────────────────────────

    private TabPage BuildSplitTab()
    {
        var page = new TabPage("Dividir") { Padding = new Padding(10) };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            AutoSize = true,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // Archivo de entrada (también admite arrastrar y soltar)
        _splitInput.ReadOnly = true;
        _splitInput.Dock = DockStyle.Fill;
        _splitInput.PlaceholderText = "Arrastra aquí un PDF o pulsa Examinar";
        _splitInput.AllowDrop = true;
        _splitInput.DragEnter += OnDragEnter;
        _splitInput.DragDrop += (_, e) => LoadSplitInput(GetDroppedPdfs(e).FirstOrDefault());

        layout.Controls.Add(new Label { Text = "PDF:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        layout.Controls.Add(_splitInput, 1, 0);
        layout.Controls.Add(MakeButton("Examinar…", (_, _) => LoadSplitInput(PickPdfs(multiple: false).FirstOrDefault())), 2, 0);

        _splitPages.AutoSize = true;
        layout.Controls.Add(_splitPages, 1, 1);

        // Modos
        _modeEachPage.Text = "Una página por archivo";
        _modeEvery.Text = "Cada N páginas:";
        _modeRanges.Text = "Un archivo por rango:";
        _modeExtract.Text = "Extraer páginas a un solo archivo:";
        _modeEachPage.Checked = true;

        _everyCount.Minimum = 1;
        _everyCount.Maximum = 10000;
        _everyCount.Value = 2;
        _everyCount.Width = 80;

        _rangesText.PlaceholderText = "Ej.: 1-3, 5, 8-  (8- = de la 8 al final)";
        _rangesText.Dock = DockStyle.Fill;

        foreach (var radio in new[] { _modeEachPage, _modeEvery, _modeRanges, _modeExtract })
        {
            radio.AutoSize = true;
            radio.CheckedChanged += (_, _) => UpdateSplitControls();
        }

        layout.Controls.Add(_modeEachPage, 0, 2);
        layout.SetColumnSpan(_modeEachPage, 3);
        layout.Controls.Add(_modeEvery, 0, 3);
        layout.Controls.Add(_everyCount, 1, 3);
        layout.Controls.Add(_modeRanges, 0, 4);
        layout.Controls.Add(_modeExtract, 0, 5);
        layout.Controls.Add(_rangesText, 1, 4);
        layout.SetRowSpan(_rangesText, 2);

        var splitButton = MakeButton("Dividir…", (_, _) => Split());
        splitButton.Dock = DockStyle.Bottom;
        splitButton.Height = 40;

        page.Controls.Add(layout);
        page.Controls.Add(splitButton);
        UpdateSplitControls();
        return page;
    }

    private void UpdateSplitControls()
    {
        _everyCount.Enabled = _modeEvery.Checked;
        _rangesText.Enabled = _modeRanges.Checked || _modeExtract.Checked;
    }

    private void LoadSplitInput(string? path)
    {
        if (path is null) return;

        try
        {
            var pages = _service.GetPageCount(path);
            _splitInput.Text = path;
            _splitPages.Text = $"{pages} página(s)";
        }
        catch (Exception ex) when (ex is PdfGestorException or IOException or UnauthorizedAccessException)
        {
            ShowWarning(ex.Message);
        }
    }

    private void Split()
    {
        var input = _splitInput.Text;
        if (string.IsNullOrEmpty(input))
        {
            ShowWarning("Elige primero un PDF.");
            return;
        }

        if (_modeExtract.Checked)
        {
            using var save = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"{Path.GetFileNameWithoutExtension(input)}_extracto.pdf",
                InitialDirectory = Path.GetDirectoryName(input),
            };
            if (save.ShowDialog(this) != DialogResult.OK) return;

            Run(() =>
            {
                _service.Extract(input, _rangesText.Text, save.FileName);
                return $"Listo: páginas extraídas en {Path.GetFileName(save.FileName)}.";
            }, folderToOpen: Path.GetDirectoryName(Path.GetFullPath(save.FileName)));
            return;
        }

        using var folder = new FolderBrowserDialog
        {
            Description = "¿Dónde guardo los archivos?",
            UseDescriptionForTitle = true,
            InitialDirectory = Path.GetDirectoryName(input) ?? "",
        };
        if (folder.ShowDialog(this) != DialogResult.OK) return;

        var everyCount = (int)_everyCount.Value;
        Run(() =>
        {
            var created =
                _modeEachPage.Checked ? _service.SplitEvery(input, 1, folder.SelectedPath) :
                _modeEvery.Checked ? _service.SplitEvery(input, everyCount, folder.SelectedPath) :
                _service.SplitByRanges(input, _rangesText.Text, folder.SelectedPath);

            return $"Listo: {created.Count} archivo(s) creados.";
        }, folderToOpen: folder.SelectedPath);
    }

    // ──────────────────────────── Comunes ────────────────────────────

    private void Run(Func<string> action, string? folderToOpen)
    {
        UseWaitCursor = true;
        try
        {
            SetStatus(action());
            var folder = folderToOpen;
            if (!string.IsNullOrEmpty(folder) &&
                MessageBox.Show(this, "Hecho. ¿Abrir la carpeta?", "PDF Gestor",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                System.Diagnostics.Process.Start("explorer.exe", $"\"{folder}\"");
            }
        }
        catch (Exception ex) when (ex is PdfGestorException or IOException or UnauthorizedAccessException)
        {
            SetStatus("No se pudo completar la operación.");
            ShowWarning(ex.Message);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private IEnumerable<string> PickPdfs(bool multiple)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            Multiselect = multiple,
        };
        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileNames : [];
    }

    private static void OnDragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private static IEnumerable<string> GetDroppedPdfs(DragEventArgs e) =>
        (e.Data?.GetData(DataFormats.FileDrop) as string[] ?? [])
            .Where(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));

    private static Button MakeButton(string text, EventHandler onClick)
    {
        var button = new Button { Text = text, Width = 130, Height = 34 };
        button.Click += onClick;
        return button;
    }

    private void SetStatus(string text) => _status.Text = text;

    private void ShowWarning(string message) =>
        MessageBox.Show(this, message, "PDF Gestor", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    /// <summary>Elemento de la lista: muestra el nombre y guarda la ruta completa.</summary>
    private sealed record PdfItem(string Path)
    {
        public override string ToString() => $"{System.IO.Path.GetFileName(Path)}   ({System.IO.Path.GetDirectoryName(Path)})";
    }
}
