# SkillTray

Windows 트레이에 상주하면서 Claude Code 스킬을 클릭 한 번으로 실행하는 도구.

## 사용법

- **트레이 아이콘 더블 클릭**: 스킬 메뉴가 뜬다 (모니터 너비의 1/10, 높이의 1/4, 스크롤 가능).
  - 한글 이름이 먼저 가나다순, 그 뒤에 영문 abc순으로 정렬된다.
  - 항목에 마우스를 올리면 설명이 툴팁으로 나온다.
  - 항목을 클릭하면 새 터미널 창(Windows Terminal, 없으면 cmd)에서 `claude "/<스킬>"`이 바로 실행된다.
  - Esc 또는 바깥 클릭으로 닫힌다.
- **트레이 아이콘 우클릭**: 작업 폴더 변경, Windows 시작 시 실행 켜기/끄기, 종료.

## 스킬을 찾는 위치

| 위치 | 호출 이름 |
|---|---|
| `<작업 폴더>/.claude/skills/<이름>/SKILL.md` | `이름` (frontmatter `name` 우선) |
| `<작업 폴더>/.claude/commands/**/*.md` | `폴더:파일명` |
| `~/.claude/skills/<이름>/SKILL.md` | `이름` |
| `~/.claude/commands/**/*.md` | `폴더:파일명` (예: `sc:analyze`) |
| 설치된 플러그인의 `skills/`, `commands/` | `플러그인:이름` |
| Claude Code 내장 스킬 | `code-review`, `simplify`, `init` 등 |

같은 이름이면 위 표에서 먼저 나온 쪽이 쓰인다. 메뉴를 열 때마다 다시 읽는다.

설정은 `%APPDATA%\SkillTray\settings.json`에 저장된다.

## 빌드

.NET 10 SDK 필요.

```bash
dotnet build SkillTray/SkillTray.csproj -c Release
```

실행 파일: `SkillTray/bin/Release/net10.0-windows/SkillTray.exe`

아이콘은 `python tools/make_icon.py` (Pillow 필요)로 다시 만든다.
