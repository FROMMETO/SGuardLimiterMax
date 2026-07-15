using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace SGuardLimiterMax.Services;

/// <summary>
/// A Windows power plan entry returned by powercfg /list.
/// </summary>
public record PowerPlanInfo(string Guid, string Name, bool IsActive);

/// <summary>
/// Manages Windows power plans via powercfg.exe.
/// All operations are fire-and-forget with no visible console window.
/// </summary>
public static class PowerManager
{
    private const string GuidUltimatePerformance = "e9a42b02-d5df-448d-aa00-03f14749eb61";
    private const string GuidHighPerformance     = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
    private const string GuidBalanced            = "381b4222-f694-41f0-9685-ff5bb260df2e";

    private static string? _originalGuid;
    private static string? _startupGuid; // captured at app startup; fallback for restore

    /// <summary>True if the power plan was switched and not yet restored.</summary>
    public static bool IsActivated => _originalGuid != null;

    /// <summary>Capture the current power plan at startup as a safe fallback for restore.</summary>
    public static void CaptureStartupPlan()
    {
        if (_startupGuid != null) return;
        var guid = GetActivePlanGuid();
        if (guid == null) return;
        // Never treat a performance plan as the "original" to restore to.
        if (guid == GuidUltimatePerformance || guid == GuidHighPerformance) { DiagLog("STARTUP skipped (performance plan: " + guid + ")"); return; }
        _startupGuid = guid;
        DiagLog("STARTUP captured: " + _startupGuid);
    }

    /// <summary>
    /// Queries all power plans installed on the system via powercfg /list.
    /// Returns an empty list on failure.
    /// </summary>
    public static List<PowerPlanInfo> GetAllPlans()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName               = "powercfg",
                Arguments              = "/list",
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
            };
            using var proc = Process.Start(psi);
            if (proc == null) return [];
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(3000);
            return ParsePlansFromOutput(output);
        }
        catch { }
        return [];
    }

    /// <summary>
    /// Parses the output of <c>powercfg /list</c> into a list of <see cref="PowerPlanInfo"/>.
    /// Exposed for unit testing.
    /// </summary>
    internal static List<PowerPlanInfo> ParsePlansFromOutput(string output)
    {
        var result = new List<PowerPlanInfo>();
        if (string.IsNullOrWhiteSpace(output)) return result;

        // Line format: "Power Scheme GUID: <guid>  (<name>) *"  (* = active)
        foreach (Match m in Regex.Matches(output,
            @"GUID:\s*([0-9a-fA-F\-]{36})\s+\(([^)]+)\)(\s+\*)?",
            RegexOptions.IgnoreCase))
        {
            string guid     = m.Groups[1].Value.Trim().ToLowerInvariant();
            string name     = m.Groups[2].Value.Trim();
            bool   isActive = m.Groups[3].Value.Trim() == "*";
            result.Add(new PowerPlanInfo(guid, name, isActive));
        }

        return result;
    }

    /// <summary>
    /// Returns the currently active power plan, or null on failure.
    /// </summary>
    public static PowerPlanInfo? GetActivePlan()
    {
        var plans = GetAllPlans();
        return plans.FirstOrDefault(p => p.IsActive);
    }

    /// <summary>
    /// Captures the current active plan, then activates the best available
    /// performance plan. If <paramref name="targetGuid"/> is provided, that
    /// specific plan is used; otherwise falls back to Ultimate �� High Performance.
    /// </summary>
    public static void ActivatePerformancePlan(string? targetGuid = null)
    {
        if (_originalGuid == null)
        {
            var current = GetActivePlanGuid();
            // Never treat a performance plan as the "original" to restore to.
            if (current != null && current != GuidUltimatePerformance && current != GuidHighPerformance)
                _originalGuid = current;
            DiagLog("ACTIVATE capture orig=" + (_originalGuid ?? "null<perf-skipped>") + " | rawActive=" + (current ?? "null"));
        }

        if (!string.IsNullOrWhiteSpace(targetGuid))
        {
            TrySetPlan(targetGuid);
            DiagLog("ACTIVATE target=" + targetGuid);
            return;
        }

        if (!TrySetPlan(GuidUltimatePerformance))
            TrySetPlan(GuidHighPerformance);
        DiagLog("ACTIVATE auto | triedUltimate first");
    }

    private const long MaxDiagLogSize = 1 * 1024 * 1024; // 1 MB

    private static void DiagLog(string msg)
    {
        try
        {
            string logPath = Path.Combine(AppContext.BaseDirectory, "power_diag.log");
            string backupPath = logPath + ".1";
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | " + msg;

            // Rotate if the log has grown beyond the size cap.
            if (File.Exists(logPath) && new FileInfo(logPath).Length > MaxDiagLogSize)
            {
                if (File.Exists(backupPath)) File.Delete(backupPath);
                File.Move(logPath, backupPath);
            }

            File.AppendAllText(logPath, line + Environment.NewLine);
        }
        catch { }
    }

    /// <summary>
    /// Restores the power plan that was active before ActivatePerformancePlan was called.
    /// Falls back to Balanced if the original plan could not be captured.
    /// </summary>
    public static void RestoreOriginalPlan()
    {
        // Priority: captured original �� startup capture �� balanced (always safe)
        string? guidToRestore = _originalGuid ?? _startupGuid ?? GuidBalanced;
        bool ok = TrySetPlan(guidToRestore);
        DiagLog("RESTORE called | guid=" + guidToRestore + " | success=" + ok + " | origWas=" + (_originalGuid ?? "null") + " | startup=" + (_startupGuid ?? "null"));
        _originalGuid = null;
    }

    /// <summary>
    /// Discards the captured original plan without restoring it.
    /// Call this when the user explicitly chooses to keep the current plan on exit.
    /// </summary>
    public static void DiscardRestore() => _originalGuid = null;

    /// <summary>
    /// Runs ipconfig /flushdns silently.
    /// </summary>
    public static void FlushDns()
    {
        RunSilent("ipconfig", "/flushdns");
    }

    private static string? GetActivePlanGuid()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName               = "powercfg",
                Arguments              = "/getactivescheme",
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
            };
            using var proc = Process.Start(psi);
            if (proc == null) return null;
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(3000);
            var match = Regex.Match(output,
                @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
            return match.Success ? match.Value.ToLowerInvariant() : null;
        }
        catch { return null; }
    }

    private static bool TrySetPlan(string guid)
    {
        using var process = RunSilent("powercfg", $"/setactive {guid}");
        process?.WaitForExit(3000);
        return process?.ExitCode == 0;
    }

    private static Process? RunSilent(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName               = fileName,
                Arguments              = arguments,
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = false,
                RedirectStandardError  = false,
                WindowStyle            = ProcessWindowStyle.Hidden,
            };
            return Process.Start(psi);
        }
        catch
        {
            return null;
        }
    }
}
