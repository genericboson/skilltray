using System.Text.Json;

namespace SkillTray;

/// <summary>%APPDATA%\SkillTray\settings.json 에 저장되는 사용자 설정.</summary>
internal sealed class AppSettings
{
    /// <summary>claude를 실행할 폴더. 이 폴더의 .claude 프로젝트 스킬도 목록에 포함된다.</summary>
    public string WorkingDirectory { get; set; } =
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SkillTray", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch (Exception) { }
        return new AppSettings();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
