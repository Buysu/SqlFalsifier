using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using SqlFalsifier.Core;

namespace SqlFalsifier;

public partial class MainWindow : Window
{
    private const string SampleSql = """
        CREATE TABLE dbo.Client (
            IdClient        INT IDENTITY(1,1) NOT NULL,
            IdSociete       INT NOT NULL,
            NomClient       NVARCHAR(120) NOT NULL,
            DateCreation    DATETIME2 NOT NULL CONSTRAINT DF_Client_DateCreation DEFAULT (SYSDATETIME()),
            CONSTRAINT PK_Client PRIMARY KEY CLUSTERED (IdClient)
        );
        GO

        CREATE VIEW dbo.V_ClientFacture AS
        SELECT  c.IdClient,
                c.NomClient,
                f.IdFacture,
                f.MontantHT,
                SUM(l.Quantite) AS [Nombre lignes]
        FROM    dbo.Client       AS c
        JOIN    dbo.Facture      AS f ON f.IdClient  = c.IdClient
        JOIN    dbo.LigneFacture AS l ON l.IdFacture = f.IdFacture
        WHERE   c.DateCreation >= '2024-01-01'   -- literals are never touched
        GROUP BY c.IdClient, c.NomClient, f.IdFacture, f.MontantHT;
        """;

    private readonly ObservableCollection<MappingRow> _rows = [];
    private readonly ICollectionView _rowsView;

    private QueryFalsifier _falsifier;
    private FalsifyOptions _options;
    private string _optionsSignature;

    public MainWindow()
    {
        InitializeComponent();

        _options = new FalsifyOptions();
        _optionsSignature = string.Empty;
        _falsifier = new QueryFalsifier(_options);

        _rowsView = CollectionViewSource.GetDefaultView(_rows);
        _rowsView.Filter = FilterRow;
        MappingGrid.ItemsSource = _rowsView;

        InputBox.Text = SampleSql;
        Loaded += (_, _) => FalsifyButton.Focus();
    }

    // ---------------------------------------------------------------- options

    private FalsifyOptions ReadOptions()
    {
        if (!int.TryParse(SeedBox.Text.Trim(), out int seed))
        {
            seed = SeedBox.Text.Trim().GetHashCode();
            SeedBox.Text = seed.ToString();
        }

        return new FalsifyOptions
        {
            Seed = seed,
            SplitCompoundNames = SplitCompoundCheck.IsChecked == true,
            RenameVariables = RenameVariablesCheck.IsChecked == true,
            RenameFunctionNames = RenameFunctionsCheck.IsChecked == true,
            PreservedWords = FalsifyOptions.ParseWordList(PreservedWordsBox.Text),
            PreservedNames = FalsifyOptions.ParseWordList(PreservedNamesBox.Text),
        };
    }

    private static string Signature(FalsifyOptions o) => string.Join(
        '|',
        o.Seed,
        o.SplitCompoundNames,
        o.RenameVariables,
        o.RenameFunctionNames,
        string.Join(',', o.PreservedWords.OrderBy(w => w)),
        string.Join(',', o.PreservedNames.OrderBy(w => w)));

    /// <summary>
    /// Keeps the existing mapping alive across runs, but rebuilds it from scratch as soon
    /// as an option that changes how names are derived is modified.
    /// </summary>
    private void SyncEngine()
    {
        _options = ReadOptions();
        string signature = Signature(_options);

        if (signature != _optionsSignature)
        {
            _optionsSignature = signature;
            _falsifier = new QueryFalsifier(_options);
            _rows.Clear();
        }

        // The grid is the source of truth: user overrides win.
        foreach (var row in _rows)
        {
            if (!string.IsNullOrWhiteSpace(row.Fake))
                _falsifier.SetMapping(row.Real, row.Fake.Trim());
        }
    }

    // ---------------------------------------------------------------- actions

    private void OnFalsify(object sender, RoutedEventArgs e)
    {
        try
        {
            MappingGrid.CommitEdit(DataGridEditingUnit.Row, true);
            SyncEngine();

            _falsifier.RememberSpelling(InputBox.Text);
            OutputBox.Text = _falsifier.Falsify(InputBox.Text);
            RefreshMapping();

            StatusText.Text = $"Falsified - {_falsifier.Usage.Count} distinct word(s) replaced, {_rows.Count} pair(s) in the mapping.";
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    private void OnRestore(object sender, RoutedEventArgs e)
    {
        try
        {
            MappingGrid.CommitEdit(DataGridEditingUnit.Row, true);
            SyncEngine();

            if (_falsifier.Map.Count == 0)
            {
                StatusText.Text = "Nothing to restore: the mapping is empty. Falsify a script first, or paste a mapping.";
                return;
            }

            InputBox.Text = _falsifier.Restore(OutputBox.Text);
            StatusText.Text = "Restored the falsified script back into the left pane.";
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    private void RefreshMapping()
    {
        var existing = _rows.ToDictionary(r => r.Real, StringComparer.OrdinalIgnoreCase);

        foreach (var (real, fake) in _falsifier.Map)
        {
            string display = _falsifier.DisplayNameFor(real);
            int count = _falsifier.Usage.GetValueOrDefault(real);

            if (existing.TryGetValue(display, out var row))
            {
                row.Fake = fake;
                row.Count = count;
            }
            else
            {
                _rows.Add(new MappingRow { Real = display, Fake = fake, Count = count });
            }
        }

        var sorted = _rows.OrderBy(r => r.Real, StringComparer.OrdinalIgnoreCase).ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            int current = _rows.IndexOf(sorted[i]);
            if (current != i) _rows.Move(current, i);
        }
    }

    private void OnNewSeed(object sender, RoutedEventArgs e)
    {
        SeedBox.Text = Random.Shared.Next(1, int.MaxValue).ToString();
        _rows.Clear();
        OnFalsify(sender, e);
    }

    private void OnResetMapping(object sender, RoutedEventArgs e)
    {
        _falsifier.Clear();
        _rows.Clear();
        _optionsSignature = string.Empty;
        OutputBox.Clear();
        StatusText.Text = "Mapping cleared.";
    }

    private void OnFilterChanged(object sender, TextChangedEventArgs e) => _rowsView.Refresh();

    private bool FilterRow(object item)
    {
        string filter = FilterBox.Text.Trim();
        if (filter.Length == 0) return true;

        return item is MappingRow row &&
               (row.Real.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                row.Fake.Contains(filter, StringComparison.OrdinalIgnoreCase));
    }

    // ---------------------------------------------------------------- files

    private void OnOpenFile(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "SQL scripts (*.sql)|*.sql|All files (*.*)|*.*" };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            InputBox.Text = File.ReadAllText(dialog.FileName);
            StatusText.Text = $"Loaded {Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    private void OnSaveOutput(object sender, RoutedEventArgs e)
    {
        if (OutputBox.Text.Length == 0)
        {
            StatusText.Text = "Nothing to save yet.";
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "SQL scripts (*.sql)|*.sql|All files (*.*)|*.*",
            FileName = "falsified.sql",
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            File.WriteAllText(dialog.FileName, OutputBox.Text, new UTF8Encoding(true));
            StatusText.Text = $"Saved {Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    private void OnCopyOutput(object sender, RoutedEventArgs e)
    {
        if (OutputBox.Text.Length == 0)
        {
            StatusText.Text = "Nothing to copy yet.";
            return;
        }

        Clipboard.SetText(OutputBox.Text);
        StatusText.Text = "Falsified SQL copied to the clipboard.";
    }

    private void OnExportMapping(object sender, RoutedEventArgs e)
    {
        if (_rows.Count == 0)
        {
            StatusText.Text = "The mapping is empty.";
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            FileName = "mapping.csv",
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            var sb = new StringBuilder("Real;Fake;Uses\r\n");
            foreach (var row in _rows.OrderBy(r => r.Real, StringComparer.OrdinalIgnoreCase))
                sb.Append(row.Real).Append(';').Append(row.Fake).Append(';').Append(row.Count).Append("\r\n");

            File.WriteAllText(dialog.FileName, sb.ToString(), new UTF8Encoding(true));
            StatusText.Text = $"Mapping exported to {Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    private void Fail(Exception ex)
    {
        StatusText.Text = "Error: " + ex.Message;
        MessageBox.Show(this, ex.ToString(), "SQL Falsifier", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
