using System.Runtime.InteropServices;

namespace SkillTray;

/// <summary>
/// 트레이 아이콘을 더블 클릭하면 뜨는 메뉴. system / custom 두 카테고리가 있고,
/// 카테고리에 마우스를 올리면 해당 스킬 목록이 서브 메뉴로 뜬다.
/// 서브 메뉴 크기는 현재 모니터 너비의 1/10, 높이는 최대 1/4이며 넘치면 스크롤된다.
/// </summary>
internal sealed class SkillMenu : ContextMenuStrip
{
    private readonly ToolStripMenuItem _systemItem = new("system");
    private readonly ToolStripMenuItem _customItem = new("custom");

    public event Action<SkillInfo>? SkillChosen;

    public SkillMenu()
    {
        ShowImageMargin = false;
        Items.Add(_systemItem);
        Items.Add(_customItem);
    }

    public void ShowAt(Point cursor, IReadOnlyList<SkillInfo> skills)
    {
        var screen = Screen.FromPoint(cursor).Bounds;
        var listMaxSize = new Size(Math.Max(1, screen.Width / 10), Math.Max(1, screen.Height / 4));
        MinimumSize = new Size(listMaxSize.Width, 0);

        Fill(_systemItem, "system", skills.Where(s => s.Category == SkillCategory.System).ToList(), listMaxSize);
        Fill(_customItem, "custom", skills.Where(s => s.Category == SkillCategory.Custom).ToList(), listMaxSize);

        // 트레이가 있는 화면 가장자리 반대쪽으로 펼쳐서 작업 표시줄을 덮지 않게 한다
        var area = Screen.FromPoint(cursor).WorkingArea;
        bool above = cursor.Y > area.Top + area.Height / 2;
        bool left = cursor.X > area.Left + area.Width / 2;
        var direction = (above, left) switch
        {
            (true, true) => ToolStripDropDownDirection.AboveLeft,
            (true, false) => ToolStripDropDownDirection.AboveRight,
            (false, true) => ToolStripDropDownDirection.BelowLeft,
            _ => ToolStripDropDownDirection.BelowRight,
        };
        Show(cursor, direction);
        // 트레이 앱이 전경 창을 가져야 메뉴 바깥을 클릭했을 때 메뉴가 닫힌다
        SetForegroundWindow(Handle);
    }

    private void Fill(ToolStripMenuItem item, string label, List<SkillInfo> skills, Size listMaxSize)
    {
        item.Text = $"{label} ({skills.Count})";

        var old = item.DropDown;
        if (skills.Count == 0)
        {
            var empty = new ToolStripDropDownMenu { ShowImageMargin = false };
            empty.Items.Add(new ToolStripMenuItem("(없음)") { Enabled = false });
            item.DropDown = empty;
        }
        else
        {
            var list = new SkillListBox(skills);
            list.SkillChosen += skill =>
            {
                Close(ToolStripDropDownCloseReason.ItemClicked);
                SkillChosen?.Invoke(skill);
            };
            // 1px 테두리가 위아래로 들어가므로 그만큼 빼고, 항목이 적으면 높이를 맞춘다
            int height = Math.Min(listMaxSize.Height - 2, list.ItemHeight * skills.Count);
            var host = new ToolStripControlHost(list)
            {
                AutoSize = false,
                Size = new Size(listMaxSize.Width - 2, height),
                Margin = Padding.Empty,
                Padding = Padding.Empty,
            };
            var dropDown = new ToolStripDropDown { Padding = Padding.Empty };
            dropDown.Items.Add(host);
            item.DropDown = dropDown;
        }

        if (old != null && !ReferenceEquals(old, item.DropDown))
            old.Dispose();
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
