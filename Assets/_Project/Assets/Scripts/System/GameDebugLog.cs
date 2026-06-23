using System;
using System.IO;
using System.Diagnostics;
using UnityEngine;

public class GameDebugLog : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD

    [SerializeField] private bool autoCloseOnStop = false;

    private string _logFilePath;
    private StreamWriter _writer;
    private Process _terminalProcess;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        _logFilePath = Path.Combine(
            Path.GetTempPath(),
            $"unity_log_{DateTime.Now:yyyyMMdd_HHmmss}.log"
        );

        _writer = new StreamWriter(_logFilePath, append: false) { AutoFlush = true };
        _writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] === Unity Debug Log Started ===");

        Application.logMessageReceived += OnLogReceived;
        OpenTerminal();
    }

    void OpenTerminal()
    {
        string safePath = _logFilePath.Replace("'", "''");
        string ps = $@"$host.UI.RawUI.WindowTitle = 'Unity Debug Log'
$host.UI.RawUI.BackgroundColor = 'Black'
Clear-Host
Get-Content -Path '{safePath}' -Wait | ForEach-Object {{
    if ($_ -match '\[ERROR\]|\[EXCEPTION\]') {{ Write-Host $_ -ForegroundColor Red }}
    elseif ($_ -match '\[WARN\]') {{ Write-Host $_ -ForegroundColor Yellow }}
    elseif ($_ -match '===') {{ Write-Host $_ -ForegroundColor Cyan }}
    else {{ Write-Host $_ -ForegroundColor Gray }}
}}";

        string encoded = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(ps));

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoExit -EncodedCommand {encoded}",
            UseShellExecute = true
        };

        _terminalProcess = Process.Start(psi);
    }

    void OnLogReceived(string logString, string stackTrace, LogType type)
    {
        string tag = type switch
        {
            LogType.Error     => "[ERROR]    ",
            LogType.Warning   => "[WARN]     ",
            LogType.Exception => "[EXCEPTION]",
            LogType.Assert    => "[ASSERT]   ",
            _                 => "[LOG]      "
        };

        _writer?.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {tag} {logString}");

        if ((type == LogType.Error || type == LogType.Exception) && !string.IsNullOrEmpty(stackTrace))
        {
            foreach (string line in stackTrace.TrimEnd().Split('\n'))
                _writer?.WriteLine($"                        {line}");
        }
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= OnLogReceived;
        _writer?.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] === Session Ended ===");
        _writer?.Dispose();

        if (autoCloseOnStop)
        {
            try { _terminalProcess?.Kill(); } catch { }
        }
    }

#endif
}
