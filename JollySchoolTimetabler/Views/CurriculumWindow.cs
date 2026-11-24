using JollySchoolTimetabler.Data;
using JollySchoolTimetabler.Services.Scheduling;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace JollySchoolTimetabler.Views;

public sealed class CurriculumWindow : Window
{
    private sealed class Row
    {
        public int YearGroupId { get; set; }
        public int SubjectId { get; set; }
        public string YearGroup { get; set; } = "";
        public string Subject { get; set; } = "";
        public int WeeklyPeriods { get; set; }
        public bool DoublePeriods { get; set; }
        public bool Compulsory { get; set; }
        public int PreferredClassSize { get; set; }
        public int MaximumClassSize { get; set; }
    }

    private readonly ObservableCollection<Row> _rows = new();
    private readonly DataGrid _grid;
    private readonly TextBlock _allocationSummary = new() { TextWrapping = TextWrapping.Wrap, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 10) };
    private readonly Dictionary<int, string> _subjectCodes = new();

    public CurriculumWindow()
    {
        Title = "Curriculum Configuration — 5 days × 6 periods";
        Width = 1100; Height = 720; MinWidth = 850; MinHeight = 450;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var root = new DockPanel { Margin = new Thickness(12) };
        Content = root;
        var help = new TextBlock
        {
            Text = "Configure weekly periods for each year and subject. Preferred class size is 30; up to 35 may be used to avoid tiny groups. " +
                   "Set Compulsory for subjects every student in that year must take. Save before generating. Core subjects plus four one-period options should total 30 periods per student.",
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10)
        };
        DockPanel.SetDock(help, Dock.Top); root.Children.Add(help);
        DockPanel.SetDock(_allocationSummary, Dock.Top); root.Children.Add(_allocationSummary);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var save = new Button { Content = "Save curriculum", Padding = new Thickness(16, 7, 16, 7), Margin = new Thickness(4) };
        save.Click += (_, _) => Save();
        var close = new Button { Content = "Close", Padding = new Thickness(16, 7, 16, 7), Margin = new Thickness(4) };
        close.Click += (_, _) => Close();
        actions.Children.Add(save); actions.Children.Add(close);
        DockPanel.SetDock(actions, Dock.Bottom); root.Children.Add(actions);
        _grid = new DataGrid { ItemsSource = _rows, AutoGenerateColumns = false, CanUserAddRows = false,
            CanUserDeleteRows = false, HeadersVisibility = DataGridHeadersVisibility.Column,
            IsReadOnly = false, SelectionMode = DataGridSelectionMode.Single };
        void Column(string title, string property, bool readOnly = false)
            => _grid.Columns.Add(new DataGridTextColumn { Header = title, Binding = new Binding(property) { Mode = readOnly ? BindingMode.OneWay : BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged }, IsReadOnly = readOnly });
        Column("Year", nameof(Row.YearGroup), true);
        Column("Subject", nameof(Row.Subject), true);
        Column("Periods/week", nameof(Row.WeeklyPeriods));
        _grid.Columns.Add(new DataGridCheckBoxColumn { Header = "Double periods", Binding = new Binding(nameof(Row.DoublePeriods)) });
        _grid.Columns.Add(new DataGridCheckBoxColumn { Header = "Compulsory", Binding = new Binding(nameof(Row.Compulsory)) });
        Column("Preferred size", nameof(Row.PreferredClassSize));
        Column("Maximum size", nameof(Row.MaximumClassSize));
        _grid.CellEditEnding += (_, _) => Dispatcher.BeginInvoke(new Action(UpdateAllocationSummary));
        root.Children.Add(_grid);
        Loaded += (_, _) => LoadRows();
    }

    private void LoadRows()
    {
        using var db = new SchoolContext();
        var subjects = db.Subjects.AsNoTracking().OrderBy(s => s.Name).ToList();
        var years = db.YearGroups.AsNoTracking().OrderBy(y => y.YearGroupId).ToList();
        var settings = CurriculumSettings.Load().ToDictionary(x => (x.YearGroupId, x.SubjectId));
        _subjectCodes.Clear();
        foreach (var subject in subjects) _subjectCodes[subject.SubjectId] = subject.Code ?? "";
        _rows.Clear();
        foreach (var year in years)
            foreach (var subject in subjects)
            {
                settings.TryGetValue((year.YearGroupId, subject.SubjectId), out var rule);
                _rows.Add(new Row { YearGroupId = year.YearGroupId, SubjectId = subject.SubjectId,
                    YearGroup = year.Name ?? $"Year {year.YearGroupId}", Subject = subject.Name,
                    WeeklyPeriods = rule?.WeeklyPeriods ?? subject.RequiredPeriods,
                    DoublePeriods = rule?.DoublePeriods ?? subject.RequiresDoublePeriod,
                    Compulsory = rule?.Compulsory ?? false,
                    PreferredClassSize = rule?.PreferredClassSize ?? 30,
                    MaximumClassSize = rule?.MaximumClassSize ?? 35 });
            }
        UpdateAllocationSummary();
    }

    private void UpdateAllocationSummary()
    {
        var core = CurriculumSettings.CoreCodes.ToHashSet();
        var option = CurriculumSettings.OptionCodes.ToHashSet();
        var summaries = _rows.GroupBy(r => r.YearGroupId).OrderBy(g => g.Key).Select(g =>
        {
            int corePeriods = g.Where(r => _subjectCodes.TryGetValue(r.SubjectId, out var code) && core.Contains(code)).Sum(r => r.WeeklyPeriods);
            bool optionsValid = g.Where(r => _subjectCodes.TryGetValue(r.SubjectId, out var code) && option.Contains(code)).All(r => r.WeeklyPeriods == 1);
            int total = corePeriods + 4;
            return $"Year {g.Key}: {total}/30 (core {corePeriods} + four options)" + (total == 30 && optionsValid ? " ✓" : " — check allocation");
        });
        _allocationSummary.Text = string.Join("     |     ", summaries);
    }

    private void Save()
    {
        _grid.CommitEdit(DataGridEditingUnit.Cell, true);
        _grid.CommitEdit(DataGridEditingUnit.Row, true);
        UpdateAllocationSummary();
        try
        {
            CurriculumSettings.Save(_rows.Select(r => new CurriculumRule
            {
                YearGroupId = r.YearGroupId, SubjectId = r.SubjectId,
                WeeklyPeriods = r.WeeklyPeriods, DoublePeriods = r.DoublePeriods,
                Compulsory = r.Compulsory, PreferredClassSize = r.PreferredClassSize,
                MaximumClassSize = r.MaximumClassSize
            }));
            MessageBox.Show("Curriculum saved. Changes apply to the next timetable generation.",
                "Curriculum", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Invalid curriculum", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
