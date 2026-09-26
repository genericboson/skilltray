using System.Globalization;
using System.Text.Json;

namespace SkillTray;

/// <summary>
/// Claude Code에 등록된 스킬을 디스크에서 찾아 모은다.
/// 찾는 위치: ~/.claude/skills, ~/.claude/commands, 작업 폴더의 .claude/skills·commands,
/// 설치된 플러그인의 skills·commands, 그리고 CLI에 내장된 스킬.
/// 내장·플러그인·설치형 프레임워크 스킬은 system, 나머지는 사용자가 만든 custom으로 분류한다.
/// </summary>
internal static class SkillCatalog
{
    // Claude Code CLI에 번들된 스킬 (디스크에 파일이 없다)
    private static readonly SkillInfo[] BuiltInSkills =
    [
        BuiltIn("code-review", "현재 변경사항 코드 리뷰"),
        BuiltIn("simplify", "변경된 코드의 재사용·단순화·효율 정리"),
        BuiltIn("security-review", "현재 브랜치 변경사항 보안 리뷰"),
        BuiltIn("init", "CLAUDE.md 초기화"),
        BuiltIn("loop", "프롬프트를 주기적으로 반복 실행"),
        BuiltIn("schedule", "예약 에이전트 생성·관리"),
        BuiltIn("claude-api", "Claude API / Anthropic SDK 레퍼런스"),
        BuiltIn("fewer-permission-prompts", "자주 쓰는 읽기 전용 명령 허용 목록 추가"),
        BuiltIn("update-config", "settings.json 설정 변경"),
        BuiltIn("keybindings-help", "키 바인딩 사용자 설정"),
        BuiltIn("run", "프로젝트 앱 실행해서 확인"),
    ];

    // 설치기가 ~/.claude/commands 아래에 넣는 프레임워크: (설치 표식 파일, commands 하위 폴더)
    private static readonly (string MarkerFile, string CommandsFolder)[] InstalledFrameworks =
    [
        (".superclaude-metadata.json", "sc"),
    ];

    private static SkillInfo BuiltIn(string name, string description) =>
        new(name, description, "built-in", SkillCategory.System);

    public static List<SkillInfo> Load(string? projectDirectory)
    {
        var claudeHome = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude");

        var found = new Dictionary<string, SkillInfo>(StringComparer.OrdinalIgnoreCase);
        void Add(SkillInfo skill) => found.TryAdd(skill.Name, skill);

        var frameworkFolders = InstalledFrameworks
            .Where(f => File.Exists(Path.Combine(claudeHome, f.MarkerFile)))
            .Select(f => f.CommandsFolder)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(projectDirectory))
        {
            var projectClaude = Path.Combine(projectDirectory, ".claude");
            ScanSkillsDir(Path.Combine(projectClaude, "skills"), null, "project", SkillCategory.Custom).ForEach(Add);
            ScanCommandsDir(Path.Combine(projectClaude, "commands"), null, "project", new HashSet<string>()).ForEach(Add);
        }

        ScanSkillsDir(Path.Combine(claudeHome, "skills"), null, "user", SkillCategory.Custom).ForEach(Add);
        ScanCommandsDir(Path.Combine(claudeHome, "commands"), null, "user", frameworkFolders).ForEach(Add);

        foreach (var (pluginName, installPath) in InstalledPlugins(claudeHome))
        {
            ScanSkillsDir(Path.Combine(installPath, "skills"), pluginName, pluginName, SkillCategory.System).ForEach(Add);
            ScanCommandsDir(Path.Combine(installPath, "commands"), pluginName, pluginName, null).ForEach(Add);
        }

        foreach (var skill in BuiltInSkills)
            Add(skill);

        var list = found.Values.ToList();
        list.Sort(SkillNameComparer.Instance);
        return list;
    }

    /// <summary>skills/&lt;이름&gt;/SKILL.md 형태.</summary>
    private static List<SkillInfo> ScanSkillsDir(string dir, string? prefix, string source, SkillCategory category)
    {
        var result = new List<SkillInfo>();
        if (!Directory.Exists(dir))
            return result;

        foreach (var skillDir in Directory.EnumerateDirectories(dir))
        {
            var file = Path.Combine(skillDir, "SKILL.md");
            if (!File.Exists(file))
                continue;

            var meta = ReadFrontmatter(file);
            var name = meta.GetValueOrDefault("name") is { Length: > 0 } n ? n : Path.GetFileName(skillDir);
            result.Add(new SkillInfo(Qualify(prefix, name), meta.GetValueOrDefault("description") ?? "", source, category));
        }
        return result;
    }

    /// <summary>
    /// commands/**/&lt;이름&gt;.md 형태. 하위 폴더는 콜론으로 이어 붙는다 (sc/analyze.md → sc:analyze).
    /// systemFolders가 null이면 전부 system, 아니면 그 하위 폴더에 든 것만 system이고 나머지는 custom.
    /// </summary>
    private static List<SkillInfo> ScanCommandsDir(string dir, string? prefix, string source, IReadOnlySet<string>? systemFolders)
    {
        var result = new List<SkillInfo>();
        if (!Directory.Exists(dir))
            return result;

        foreach (var file in Directory.EnumerateFiles(dir, "*.md", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(dir, file);
            var parts = Path.ChangeExtension(relative, null)
                .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);
            var name = string.Join(':', parts);
            var category = systemFolders is null || (parts.Length > 1 && systemFolders.Contains(parts[0]))
                ? SkillCategory.System
                : SkillCategory.Custom;
            var meta = ReadFrontmatter(file);
            result.Add(new SkillInfo(Qualify(prefix, name), meta.GetValueOrDefault("description") ?? "", source, category));
        }
        return result;
    }

    private static string Qualify(string? prefix, string name) =>
        prefix is null ? name : $"{prefix}:{name}";

    /// <summary>installed_plugins.json에서 활성화된 플러그인의 (이름, 설치 경로)를 읽는다.</summary>
    private static IEnumerable<(string Name, string InstallPath)> InstalledPlugins(string claudeHome)
    {
        var file = Path.Combine(claudeHome, "plugins", "installed_plugins.json");
        if (!File.Exists(file))
            yield break;

        var disabled = DisabledPlugins(claudeHome);
        JsonDocument doc;
        try { doc = JsonDocument.Parse(File.ReadAllText(file)); }
        catch (Exception) { yield break; }

        using (doc)
        {
            if (!doc.RootElement.TryGetProperty("plugins", out var plugins) || plugins.ValueKind != JsonValueKind.Object)
                yield break;

            foreach (var plugin in plugins.EnumerateObject())
            {
                if (disabled.Contains(plugin.Name))
                    continue;

                var pluginName = plugin.Name.Split('@')[0];
                // v2: 키마다 설치 항목 배열, v1: 단일 객체
                var entries = plugin.Value.ValueKind == JsonValueKind.Array
                    ? plugin.Value.EnumerateArray().ToList()
                    : [plugin.Value];
                foreach (var entry in entries)
                {
                    if (entry.ValueKind == JsonValueKind.Object
                        && entry.TryGetProperty("installPath", out var path)
                        && path.GetString() is { Length: > 0 } installPath
                        && Directory.Exists(installPath))
                    {
                        yield return (pluginName, installPath);
                        break;
                    }
                }
            }
        }
    }

    private static HashSet<string> DisabledPlugins(string claudeHome)
    {
        var disabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var file = Path.Combine(claudeHome, "settings.json");
        if (!File.Exists(file))
            return disabled;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            if (doc.RootElement.TryGetProperty("enabledPlugins", out var enabled) && enabled.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in enabled.EnumerateObject())
                    if (p.Value.ValueKind == JsonValueKind.False)
                        disabled.Add(p.Name);
            }
        }
        catch (Exception) { }
        return disabled;
    }

    /// <summary>--- 로 감싼 YAML frontmatter에서 한 줄짜리 key: value만 읽는다.</summary>
    private static Dictionary<string, string> ReadFrontmatter(string file)
    {
        var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var reader = new StreamReader(file);
            if (reader.ReadLine()?.Trim() != "---")
                return meta;

            string? line;
            while ((line = reader.ReadLine()) != null && line.Trim() != "---")
            {
                int colon = line.IndexOf(':');
                if (colon <= 0 || char.IsWhiteSpace(line[0]))
                    continue;
                var key = line[..colon].Trim();
                var value = line[(colon + 1)..].Trim().Trim('"', '\'');
                meta[key] = value;
            }
        }
        catch (IOException) { }
        return meta;
    }
}

/// <summary>한글로 시작하는 이름을 먼저 가나다순, 그 다음 나머지를 abc순으로 정렬한다.</summary>
internal sealed class SkillNameComparer : IComparer<SkillInfo>
{
    public static readonly SkillNameComparer Instance = new();
    private static readonly CompareInfo Korean = CultureInfo.GetCultureInfo("ko-KR").CompareInfo;

    public int Compare(SkillInfo? x, SkillInfo? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        int group = GroupOf(x.Name).CompareTo(GroupOf(y.Name));
        if (group != 0)
            return group;
        return Korean.Compare(x.Name, y.Name, CompareOptions.IgnoreCase);
    }

    private static int GroupOf(string name) =>
        name.Length > 0 && IsHangul(name[0]) ? 0 : 1;

    private static bool IsHangul(char c) =>
        c is >= '가' and <= '힣'   // 완성형 음절
          or >= 'ᄀ' and <= 'ᇿ'   // 자모
          or >= '㄰' and <= '㆏';  // 호환 자모
}
