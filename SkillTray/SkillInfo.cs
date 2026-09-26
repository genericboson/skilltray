namespace SkillTray;

internal enum SkillCategory
{
    /// <summary>사용자가 만들지 않은 스킬: CLI 내장, 플러그인, 설치형 프레임워크(SuperClaude 등)</summary>
    System,
    /// <summary>사용자가 직접 만든 스킬</summary>
    Custom,
}

/// <summary>Claude Code에서 슬래시 명령으로 호출할 수 있는 스킬 하나.</summary>
/// <param name="Name">슬래시 없이 쓴 호출 이름 (예: sc:analyze)</param>
/// <param name="Description">frontmatter의 description, 없으면 빈 문자열</param>
/// <param name="Source">어디서 찾았는지 (user, project, plugin 이름, built-in)</param>
/// <param name="Category">system / custom 분류</param>
internal sealed record SkillInfo(string Name, string Description, string Source, SkillCategory Category);
