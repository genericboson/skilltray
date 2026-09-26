namespace SkillTray;

/// <summary>창 없이 트레이 아이콘만으로 상주하는 애플리케이션 컨텍스트.</summary>
internal sealed class TrayContext : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly SkillMenuForm _menu;
    private readonly AppSettings _settings = AppSettings.Load();
    private readonly Icon _icon;
    private readonly ToolStripMenuItem _folderItem;
    private readonly ToolStripMenuItem _startupItem;

    public TrayContext()
    {
        _icon = LoadIcon();

        _menu = new SkillMenuForm();
        _menu.SkillChosen += RunSkill;

        _folderItem = new ToolStripMenuItem("", null, (_, _) => ChooseWorkingDirectory());
        _startupItem = new ToolStripMenuItem("Windows 시작 시 실행", null, (_, _) => ToggleStartup());

        var options = new ContextMenuStrip();
        options.Items.Add("스킬 목록 열기", null, (_, _) => ShowSkillMenu());
        options.Items.Add(new ToolStripSeparator());
        options.Items.Add(_folderItem);
        options.Items.Add(_startupItem);
        options.Items.Add(new ToolStripSeparator());
        options.Items.Add("종료", null, (_, _) => ExitThread());
        options.Opening += (_, _) => RefreshOptionLabels();

        _tray = new NotifyIcon
        {
            Icon = _icon,
            Text = "SkillTray - 더블 클릭해서 Claude 스킬 실행",
            ContextMenuStrip = options,
            Visible = true,
        };
        _tray.MouseDoubleClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                ShowSkillMenu();
        };
    }

    private void ShowSkillMenu()
    {
        List<SkillInfo> skills;
        try
        {
            skills = SkillCatalog.Load(_settings.WorkingDirectory);
        }
        catch (Exception ex)
        {
            _tray.ShowBalloonTip(5000, "SkillTray", "스킬 목록을 읽지 못했습니다: " + ex.Message, ToolTipIcon.Error);
            return;
        }
        _menu.ShowAt(Cursor.Position, skills);
    }

    private void RunSkill(SkillInfo skill)
    {
        try
        {
            ClaudeLauncher.Run(skill, _settings.WorkingDirectory);
        }
        catch (Exception ex)
        {
            _tray.ShowBalloonTip(5000, "SkillTray", $"/{skill.Name} 실행 실패: {ex.Message}", ToolTipIcon.Error);
        }
    }

    private void RefreshOptionLabels()
    {
        _folderItem.Text = $"작업 폴더: {_settings.WorkingDirectory}";
        _startupItem.Checked = StartupRegistration.IsEnabled;
    }

    private void ChooseWorkingDirectory()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "claude를 실행할 작업 폴더",
            UseDescriptionForTitle = true,
            InitialDirectory = _settings.WorkingDirectory,
        };
        if (dialog.ShowDialog() != DialogResult.OK)
            return;
        _settings.WorkingDirectory = dialog.SelectedPath;
        _settings.Save();
    }

    private void ToggleStartup()
    {
        try
        {
            StartupRegistration.Set(!StartupRegistration.IsEnabled);
        }
        catch (Exception ex)
        {
            _tray.ShowBalloonTip(5000, "SkillTray", "시작 프로그램 등록 실패: " + ex.Message, ToolTipIcon.Error);
        }
    }

    private static Icon LoadIcon()
    {
        using var stream = typeof(TrayContext).Assembly.GetManifestResourceStream("SkillTray.skilltray.ico");
        return stream != null ? new Icon(stream, SystemInformation.SmallIconSize) : SystemIcons.Application;
    }

    protected override void ExitThreadCore()
    {
        _tray.Visible = false;
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tray.Dispose();
            _menu.Dispose();
            _icon.Dispose();
        }
        base.Dispose(disposing);
    }
}
