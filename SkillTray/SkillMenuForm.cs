namespace SkillTray;

/// <summary>
/// 트레이 아이콘을 더블 클릭하면 뜨는 스크롤 가능한 스킬 메뉴.
/// 크기는 현재 모니터의 너비 1/10, 높이 1/4.
/// </summary>
internal sealed class SkillMenuForm : Form
{
    private static readonly Color MenuBack = SystemColors.Menu;
    private static readonly Color MenuText = SystemColors.MenuText;
    private static readonly Color HoverBack = SystemColors.MenuHighlight;
    private static readonly Color HoverText = SystemColors.HighlightText;
    private static readonly Color SubtleText = SystemColors.GrayText;

    private readonly ListBox _list;
    private readonly Label _header;
    private readonly ToolTip _toolTip = new() { InitialDelay = 500, ReshowDelay = 100, AutoPopDelay = 15000 };
    private int _hoverIndex = -1;
    private int _toolTipIndex = -1;

    public event Action<SkillInfo>? SkillChosen;

    public SkillMenuForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = SystemColors.ControlDark;   // 1px 테두리 색
        Padding = new Padding(1);
        KeyPreview = true;

        _header = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 0, 0),
            BackColor = MenuBack,
            ForeColor = SubtleText,
            Font = new Font(SystemFonts.MenuFont!.FontFamily, SystemFonts.MenuFont.Size * 0.9f),
        };
        _header.Height = _header.Font.Height + 8;

        _list = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            DrawMode = DrawMode.OwnerDrawFixed,
            IntegralHeight = false,
            BackColor = MenuBack,
            ForeColor = MenuText,
            Font = SystemFonts.MenuFont!,
            ScrollAlwaysVisible = false,
        };
        _list.ItemHeight = _list.Font.Height + 8;
        _list.DrawItem += OnDrawItem;
        _list.MouseMove += OnListMouseMove;
        _list.MouseLeave += (_, _) => SetHover(-1);
        _list.MouseClick += OnListMouseClick;

        Controls.Add(_list);
        Controls.Add(_header);
    }

    /// <summary>목록을 채우고 커서 근처에 메뉴를 띄운다.</summary>
    public void ShowAt(Point cursor, IReadOnlyList<SkillInfo> skills)
    {
        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var skill in skills)
            _list.Items.Add(skill);
        _list.EndUpdate();
        _header.Text = $"Claude Code 스킬 {skills.Count}개";
        _hoverIndex = -1;
        _list.SelectedIndex = -1;
        _list.TopIndex = 0;

        var screen = Screen.FromPoint(cursor);
        var size = new Size(
            Math.Max(1, screen.Bounds.Width / 10),
            Math.Max(1, screen.Bounds.Height / 4));
        Bounds = new Rectangle(PlaceNear(cursor, size, screen.WorkingArea), size);

        Show();
        Activate();
        _list.Focus();
    }

    /// <summary>일반 컨텍스트 메뉴처럼 커서 오른쪽 아래에, 넘치면 반대쪽으로 뒤집는다.</summary>
    private static Point PlaceNear(Point cursor, Size size, Rectangle area)
    {
        int x = cursor.X + size.Width <= area.Right ? cursor.X : cursor.X - size.Width;
        int y = cursor.Y + size.Height <= area.Bottom ? cursor.Y : cursor.Y - size.Height;
        x = Math.Clamp(x, area.Left, Math.Max(area.Left, area.Right - size.Width));
        y = Math.Clamp(y, area.Top, Math.Max(area.Top, area.Bottom - size.Height));
        return new Point(x, y);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int CS_DROPSHADOW = 0x00020000;
            const int WS_EX_TOOLWINDOW = 0x00000080;
            var cp = base.CreateParams;
            cp.ClassStyle |= CS_DROPSHADOW;
            cp.ExStyle |= WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        Hide();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape)
        {
            Hide();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Enter && _list.SelectedItem is SkillInfo skill)
        {
            Choose(skill);
            e.Handled = true;
        }
    }

    private void OnListMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;
        int index = _list.IndexFromPoint(e.Location);
        if (index >= 0 && _list.GetItemRectangle(index).Contains(e.Location) && _list.Items[index] is SkillInfo skill)
            Choose(skill);
    }

    private void Choose(SkillInfo skill)
    {
        Hide();
        SkillChosen?.Invoke(skill);
    }

    private void OnListMouseMove(object? sender, MouseEventArgs e)
    {
        int index = _list.IndexFromPoint(e.Location);
        if (index >= 0 && !_list.GetItemRectangle(index).Contains(e.Location))
            index = -1;
        SetHover(index);

        if (index != _toolTipIndex)
        {
            _toolTipIndex = index;
            var tip = index >= 0 && _list.Items[index] is SkillInfo s
                ? (string.IsNullOrEmpty(s.Description) ? $"/{s.Name}  [{s.Source}]" : $"/{s.Name}  [{s.Source}]\n{s.Description}")
                : null;
            _toolTip.SetToolTip(_list, tip);
        }
    }

    private void SetHover(int index)
    {
        if (index == _hoverIndex)
            return;
        int old = _hoverIndex;
        _hoverIndex = index;
        if (old >= 0 && old < _list.Items.Count) _list.Invalidate(_list.GetItemRectangle(old));
        if (index >= 0) _list.Invalidate(_list.GetItemRectangle(index));
        if (index < 0) _toolTip.SetToolTip(_list, null);
    }

    private void OnDrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || _list.Items[e.Index] is not SkillInfo skill)
            return;

        bool highlighted = e.Index == _hoverIndex || (e.State & DrawItemState.Selected) != 0;
        using (var back = new SolidBrush(highlighted ? HoverBack : MenuBack))
            e.Graphics.FillRectangle(back, e.Bounds);

        var textBounds = Rectangle.FromLTRB(e.Bounds.Left + 8, e.Bounds.Top, e.Bounds.Right - 4, e.Bounds.Bottom);
        TextRenderer.DrawText(e.Graphics, skill.Name, _list.Font, textBounds,
            highlighted ? HoverText : MenuText,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _toolTip.Dispose();
        base.Dispose(disposing);
    }
}
