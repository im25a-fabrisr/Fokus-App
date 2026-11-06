using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Fokus.Core;
using Microsoft.Win32;

namespace Fokus.App;

public record BlockedProgram(string Path)
{
    public string DisplayName => System.IO.Path.GetFileNameWithoutExtension(Path);
}
public sealed class SavedState
{
    public Dictionary<string, List<string>> Programs { get; set; } = new();
    public Dictionary<string, int> Durations { get; set; } = new();
    public string SelectedMode { get; set; } = "Lernen";
    public FocusSession? Session { get; set; }
}

public partial class MainWindow : Window
{
    private readonly string statePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fokus-App", "state.json");
    private readonly DispatcherTimer ticker = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private SavedState state = new();
    private readonly List<BlockedProgram> programs = [];
    private string currentMode = "Lernen";
    private bool ready;
    private bool storageFailed;
    private static readonly HashSet<string> ProtectedPrograms = new(StringComparer.OrdinalIgnoreCase)
    { "explorer", "taskmgr", "applicationframehost", "shellexperiencehost", "startmenuexperiencehost", "searchhost", "lockapp", "systemsettings", "fokus-app" };
    private static Mutex? instanceMutex;

    public MainWindow()
    {
        instanceMutex = new Mutex(true, "Local\\FokusApp.SingleInstance", out bool firstInstance);
        if (!firstInstance)
        {
            MessageBox.Show("Fokus ist bereits geöffnet.", "Fokus");
            Application.Current.Shutdown();
            return;
        }
        InitializeComponent();
        try
        {
            if (File.Exists(statePath)) state = JsonSerializer.Deserialize<SavedState>(File.ReadAllText(statePath)) ?? new();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            MessageBox.Show("Die gespeicherten Einstellungen konnten nicht geladen werden. Bitte prüfe deine Fokusphase.\n" + ex.Message, "Fokus");
        }
        if (state.Session is { } session)
        {
            if (session.IsActive(DateTimeOffset.UtcNow)) state.SelectedMode = session.Mode;
            else state.Session = null;
        }
        currentMode = state.SelectedMode is "Schule" or "Eigener Modus" ? state.SelectedMode : "Lernen";
        ModeBox.SelectedIndex = currentMode == "Schule" ? 1 : currentMode == "Eigener Modus" ? 2 : 0;
        LoadMode();
        ready = true;
        ticker.Tick += (_, _) => Tick();
        ticker.Start();
        Refresh();
    }

    private void LoadMode()
    {
        programs.Clear();
        if (state.Programs.TryGetValue(currentMode, out var paths)) programs.AddRange(paths.Select(p => new BlockedProgram(p)));
        BlockedList.ItemsSource = programs.ToList();
        MinutesBox.Text = (state.Durations.TryGetValue(currentMode, out var minutes) ? minutes : currentMode == "Schule" ? 45 : 40).ToString();
    }
    private void StoreMode()
    {
        state.Programs[currentMode] = programs.Select(p => p.Path).ToList();
        if (int.TryParse(MinutesBox.Text, out int minutes) && minutes is >= 1 and <= 480) state.Durations[currentMode] = minutes;
    }
    private void ModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ready || state.Session != null) return;
        StoreMode();
        currentMode = ((ComboBoxItem)ModeBox.SelectedItem).Content.ToString()!;
        state.SelectedMode = currentMode;
        LoadMode();
        Save();
        Refresh();
    }
    private void DurationChanged(object sender, TextChangedEventArgs e) { if (ready && state.Session == null) Refresh(); }
    private void AddProgram(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog { Filter = "Windows-Programme (*.exe)|*.exe", Title = "Programm für die Fokusphase auswählen" };
        if (picker.ShowDialog() != true) return;
        string name = Path.GetFileNameWithoutExtension(picker.FileName);
        string windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + Path.DirectorySeparatorChar;
        if (ProtectedPrograms.Contains(name) || picker.FileName.StartsWith(windowsDirectory, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Windows-Komponenten und Fokus selbst können nicht gesperrt werden.", "Programm nicht verfügbar");
            return;
        }
        if (!programs.Any(p => p.Path.Equals(picker.FileName, StringComparison.OrdinalIgnoreCase))) programs.Add(new(picker.FileName));
        BlockedList.ItemsSource = programs.ToList();
        StoreMode();
        Save();
    }
    private void RemoveProgram(object sender, RoutedEventArgs e)
    {
        if (BlockedList.SelectedItem is not BlockedProgram selected) return;
        programs.Remove(selected);
        BlockedList.ItemsSource = programs.ToList();
        StoreMode();
        Save();
    }
    private void StartFocus(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(MinutesBox.Text, out int minutes) || minutes is < 1 or > 480)
        {
            MessageBox.Show("Bitte gib eine Dauer zwischen 1 und 480 Minuten ein.", "Dauer prüfen");
            return;
        }
        StoreMode();
        state.Session = new() { Mode = currentMode, EndsAt = DateTimeOffset.UtcNow.AddMinutes(minutes), BlockedExecutables = programs.Select(p => p.Path).ToList() };
        if (!Save())
        {
            state.Session = null;
            return;
        }
        Refresh();
    }
    private void CancelFocus(object sender, RoutedEventArgs e)
    {
        if (state.Session is not { } session) return;
        if (session.CanCancel(DateTimeOffset.UtcNow))
        {
            state.Session = null;
            Save();
            Refresh("Fokusphase beendet. Du kannst neu starten.");
            return;
        }
        session.RequestCancel(DateTimeOffset.UtcNow);
        Save();
        Refresh();
    }
    private void KeepFocus(object sender, RoutedEventArgs e)
    {
        state.Session?.KeepFocusing();
        Save();
        Refresh();
    }
    private void Tick()
    {
        if (state.Session is not { } session) return;
        if (!session.IsActive(DateTimeOffset.UtcNow))
        {
            state.Session = null;
            Save();
            Refresh("Geschafft! Deine Fokusphase ist abgeschlossen. Zeit für eine Pause.");
            System.Media.SystemSounds.Asterisk.Play();
            return;
        }
        GuardForeground(session);
        Refresh();
    }
    private void Refresh(string? message = null)
    {
        bool active = state.Session != null;
        SettingsPanel.IsEnabled = !active;
        StartButton.Visibility = active ? Visibility.Collapsed : Visibility.Visible;
        CancelButton.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
        KeepButton.Visibility = state.Session?.CancelAvailableAt != null ? Visibility.Visible : Visibility.Collapsed;
        SessionLabel.Text = active ? $"{state.Session!.Mode} · Fokus läuft" : "Bereit, wenn du es bist";
        var remaining = state.Session?.Remaining(DateTimeOffset.UtcNow) ?? TimeSpan.FromMinutes(int.TryParse(MinutesBox.Text, out int minutes) && minutes is >= 1 and <= 480 ? minutes : 40);
        TimerLabel.Text = $"{(int)Math.Ceiling(remaining.TotalSeconds) / 60:00}:{(int)Math.Ceiling(remaining.TotalSeconds) % 60:00}";
        StatusLabel.Text = message ?? (active ? "Eine Sache nach der anderen. Du bist dran." : "Deine nächste konzentrierte Lernphase.");
        if (state.Session?.CancelAvailableAt is { } deadline)
        {
            int seconds = Math.Max(0, (int)Math.Ceiling((deadline - DateTimeOffset.UtcNow).TotalSeconds));
            CancelButton.Content = seconds > 0 ? $"Beenden in {seconds} s" : "Jetzt Fokus beenden";
            StatusLabel.Text = seconds > 0 ? "Nimm dir einen Moment. Möchtest du wirklich aufhören?" : "Du kannst jetzt beenden oder weiter fokussieren.";
        }
        else CancelButton.Content = "Vorzeitig beenden";
    }
    private bool Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(statePath)!);
            string temp = statePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, statePath, true);
            storageFailed = false;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (!storageFailed) MessageBox.Show("Die Sitzung konnte nicht gespeichert werden.\n" + ex.Message, "Speicherproblem");
            storageFailed = true;
            return false;
        }
    }
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!ready) return;
        if (state.Session?.IsActive(DateTimeOffset.UtcNow) == true)
        {
            e.Cancel = true;
            HintLabel.Text = "Dein Fokus läuft noch. Nutze „Vorzeitig beenden“ mit 30 Sekunden Bedenkzeit.";
            return;
        }
        StoreMode();
        Save();
        ticker.Stop();
    }
    private static void GuardForeground(FocusSession session)
    {
        nint window = GetForegroundWindow();
        if (window == 0) return;
        GetWindowThreadProcessId(window, out uint pid);
        if (pid == Environment.ProcessId || pid == 0) return;
        try
        {
            using var process = Process.GetProcessById((int)pid);
            string? executable = process.MainModule?.FileName;
            if (executable != null && session.BlockedExecutables.Any(p => p.Equals(executable, StringComparison.OrdinalIgnoreCase)))
                ShowWindowAsync(window, 6);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or ArgumentException or NotSupportedException) { /* Exited or inaccessible process: leave it untouched. */ }
    }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint processId);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ShowWindowAsync(nint window, int command);
}
