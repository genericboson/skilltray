# SkillTray

Windows 트레이에 상주하면서 Claude Code 스킬을 클릭 한 번으로 실행하는 도구.

## 사용법

- **트레이 아이콘 더블 클릭**: `system`, `custom` 두 카테고리 메뉴가 뜬다.
  - 카테고리에 마우스를 올리면 그 카테고리의 스킬 목록이 서브 메뉴로 뜬다 (모니터 너비의 1/10, 높이 최대 1/4, 스크롤 가능).
  - 한글 이름이 먼저 가나다순, 그 뒤에 영문 abc순으로 정렬된다.
  - 항목에 마우스를 올리면 설명이 툴팁으로 나온다.
  - 스킬을 클릭하면 새 터미널 창(Windows Terminal, 없으면 cmd)에서 `claude "/<스킬>"`이 바로 실행된다.
  - Esc 또는 바깥 클릭으로 닫힌다.
- **트레이 아이콘 우클릭**: 작업 폴더 변경, Windows 시작 시 실행 켜기/끄기, 종료.

## 스킬을 찾는 위치와 분류

| 위치 | 호출 이름 | 카테고리 |
|---|---|---|
| `<작업 폴더>/.claude/skills/<이름>/SKILL.md` | `이름` (frontmatter `name` 우선) | custom |
| `<작업 폴더>/.claude/commands/**/*.md` | `폴더:파일명` | custom |
| `~/.claude/skills/<이름>/SKILL.md` | `이름` | custom |
| `~/.claude/commands/**/*.md` | `폴더:파일명` | custom |
| `~/.claude/commands/sc/*.md` (SuperClaude가 설치된 경우) | `sc:이름` | system |
| 설치된 플러그인의 `skills/`, `commands/` | `플러그인:이름` | system |
| Claude Code 내장 스킬 | `code-review`, `simplify`, `init` 등 | system |

system은 사용자가 만들지 않은 스킬(내장, 플러그인, 설치형 프레임워크)이고, custom은 사용자가 직접 만든 스킬이다.
설치형 프레임워크는 `SkillCatalog.InstalledFrameworks`에 (설치 표식 파일, commands 하위 폴더)로 등록한다.

같은 이름이면 위 표에서 먼저 나온 쪽이 쓰인다. 메뉴를 열 때마다 다시 읽는다.

설정은 `%APPDATA%\SkillTray\settings.json`에 저장된다.

## 빌드

.NET 10 SDK 필요.

```bash
dotnet build SkillTray/SkillTray.csproj -c Release
```

실행 파일: `SkillTray/bin/Release/net10.0-windows/SkillTray.exe`

아이콘은 `python tools/make_icon.py` (Pillow 필요)로 다시 만든다.
