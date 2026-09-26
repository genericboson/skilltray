namespace SkillTray;

/// <summary>메뉴처럼 보이는 스크롤 가능한 스킬 목록. 마우스를 올리면 강조되고, 클릭하면 SkillChosen이 발생한다.</summary>
internal sealed class SkillListBox : ListBox
{
    private static readonly Color MenuBack = ProfessionalColors.ToolStripDropDownBackground;
    private static readonly Color HoverBack = ProfessionalColors.MenuItemSelected;
    private static readonly Color HoverBorder = ProfessionalColors.MenuItemBorder;

    private readonly ToolTip _toolTip = new() { InitialDelay = 500, ReshowDelay = 100, AutoPopDelay = 15000 };
    private int _hoverIndex = -1;
    private int _toolTipIndex = -1;

    public event Action<SkillInfo>? SkillChosen;

    public SkillListBox(IEnumerable<SkillInfo> skills)
    {
        BorderStyle = BorderStyle.None;
        DrawMode = DrawMode.OwnerDrawFixed;
        IntegralHeight = false;
        BackColor = MenuBack;
        ForeColor = SystemColors.MenuText;
        Font = SystemFonts.MenuFont!;
        ItemHeight = Font.Height + 8;

        BeginUpdate();
        foreach (var skill in skills)
            Items.Add(skill);
        EndUpdate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int index = ItemAt(e.Location);
        SetHover(index);

        if (index != _toolTipIndex)
        {
            _toolTipIndex = index;
            var tip = index >= 0 && Items[index] is SkillInfo s
                ? (string.IsNullOrEmpty(s.Description) ? $"/{s.Name}  [{s.Source}]" : $"/{s.Name}  [{s.Source}]\n{s.Description}")
                : null;
            _toolTip.SetToolTip(this, tip);
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        SetHover(-1);
        _toolTipIndex = -1;
        _toolTip.SetToolTip(this, null);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button == MouseButtons.Left && ItemAt(e.Location) is var index and >= 0 && Items[index] is SkillInfo skill)
            SkillChosen?.Invoke(skill);
    }

    private int ItemAt(Point location)
    {
        int index = IndexFromPoint(location);
        return index >= 0 && GetItemRectangle(index).Contains(location) ? index : -1;
    }

    private void SetHover(int index)
    {
        if (index == _hoverIndex)
            return;
        int old = _hoverIndex;
        _hoverIndex = index;
        if (old >= 0 && old < Items.Count) Invalidate(GetItemRectangle(old));
        if (index >= 0) Invalidate(GetItemRectangle(index));
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Index < 0 || Items[e.Index] is not SkillInfo skill)
            return;

        bool hover = e.Index == _hoverIndex;
        using (var back = new SolidBrush(MenuBack))
            e.Graphics.FillRectangle(back, e.Bounds);
        if (hover)
        {
            var r = Rectangle.FromLTRB(e.Bounds.Left + 2, e.Bounds.Top, e.Bounds.Right - 3, e.Bounds.Bottom - 1);
            using var fill = new SolidBrush(HoverBack);
            using var border = new Pen(HoverBorder);
            e.Graphics.FillRectangle(fill, r);
            e.Graphics.DrawRectangle(border, r);
        }

        var textBounds = Rectangle.FromLTRB(e.Bounds.Left + 10, e.Bounds.Top, e.Bounds.Right - 4, e.Bounds.Bottom);
        TextRenderer.DrawText(e.Graphics, skill.Name, Font, textBounds, SystemColors.MenuText,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _toolTip.Dispose();
        base.Dispose(disposing);
    }
}
