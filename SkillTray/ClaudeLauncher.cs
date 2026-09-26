using System.Diagnostics;

namespace SkillTray;

/// <summary>새 터미널 창에서 claude CLI를 띄워 스킬을 바로 실행한다.</summary>
internal static class ClaudeLauncher
{
    public static void Run(SkillInfo skill, string workingDirectory)
    {
        if (!Directory.Exists(workingDirectory))
            workingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var prompt = "/" + skill.Name;
        var windowsTerminal = FindWindowsTerminal();

        ProcessStartInfo psi;
        if (windowsTerminal != null)
        {
            psi = new ProcessStartInfo(windowsTerminal) { UseShellExecute = false };
            psi.ArgumentList.Add("-d");
            psi.ArgumentList.Add(workingDirectory);
            psi.ArgumentList.Add("--title");
            psi.ArgumentList.Add(prompt);
            psi.ArgumentList.Add("claude");
            psi.ArgumentList.Add(prompt);
        }
        else
        {
            psi = new ProcessStartInfo("cmd.exe")
            {
                UseShellExecute = true,
                Arguments = $"/k title {prompt} & claude \"{prompt}\"",
            };
        }
        psi.WorkingDirectory = workingDirectory;
        Process.Start(psi);
    }

    private static string? FindWindowsTerminal()
    {
        var alias = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "WindowsApps", "wt.exe");
        return File.Exists(alias) ? alias : null;
    }
}
