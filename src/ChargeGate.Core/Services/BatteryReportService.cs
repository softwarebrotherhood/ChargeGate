using System.Diagnostics;

namespace ChargeGate.Core.Services;

public static class BatteryReportService
{
    public static async Task<string?> GenerateReportAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), $"ChargeGate_BatteryReport_{DateTime.Now:yyyyMMdd_HHmmss}.html");
                var psi = new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    Arguments = $"/batteryreport /output \"{tempPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(psi);
                process?.WaitForExit(5000);

                if (File.Exists(tempPath))
                {
                    return tempPath;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BatteryReportService] Error generating report: {ex.Message}");
            }

            return null;
        });
    }

    public static void OpenReport(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BatteryReportService] Error opening report: {ex.Message}");
        }
    }
}
