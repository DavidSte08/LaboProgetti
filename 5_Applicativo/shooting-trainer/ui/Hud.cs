using Godot;

// HUD durante la partita (mockup 2 e 5, REQ-005, REQ-011_4).
public partial class Hud : CanvasLayer
{
    private Label _scoreValue, _modeLabel, _modeValue, _weaponLabel, _ammoValue, _ammoSize;
    private Label _noticeLabel, _prompt, _debugLabel;
    private Control _modePanel, _notice, _debugPanel;
    private GridContainer _ammoBar;
    private ProgressBar _reloadBar;
    private int _barSize = -1;

    private static readonly Color AmmoFull = new(0.91f, 0.76f, 0.35f);
    private static readonly Color AmmoEmpty = new(1f, 1f, 1f, 0.15f);
    private static readonly Color Danger = new(0.9f, 0.33f, 0.24f);

    public override void _Ready()
    {
        _scoreValue = GetNode<Label>("Root/ScorePanel/VBox/ScoreValue");
        _modePanel = GetNode<Control>("Root/ModePanel");
        _modeLabel = GetNode<Label>("Root/ModePanel/VBox/ModeLabel");
        _modeValue = GetNode<Label>("Root/ModePanel/VBox/ModeValue");
        _weaponLabel = GetNode<Label>("Root/AmmoPanel/HBox/VBox/WeaponLabel");
        _ammoValue = GetNode<Label>("Root/AmmoPanel/HBox/VBox/Counter/AmmoValue");
        _ammoSize = GetNode<Label>("Root/AmmoPanel/HBox/VBox/Counter/AmmoSize");
        _ammoBar = GetNode<GridContainer>("Root/AmmoPanel/HBox/AmmoBar");
        _notice = GetNode<Control>("Root/Notice");
        _noticeLabel = GetNode<Label>("Root/Notice/VBox/NoticeLabel");
        _reloadBar = GetNode<ProgressBar>("Root/Notice/VBox/ReloadBar");
        _prompt = GetNode<Label>("Root/Prompt");
        _debugPanel = GetNode<Control>("Root/DebugPanel");
        _debugLabel = GetNode<Label>("Root/DebugPanel/DebugLabel");
    }

    // Punteggio corrente (REQ-005_3).
    public void UpdateScore(int score) => _scoreValue.Text = score.ToString();

    // Munizioni nel caricatore, in cifre e in barra (REQ-005_2).
    public void UpdateAmmo(int ammo, int size)
    {
        if (size != _barSize) BuildAmmoBar(size);
        _ammoValue.Text = ammo.ToString();
        _ammoValue.Modulate = ammo == 0 ? Danger : Colors.White;
        _ammoSize.Text = $"/ {size}";
        for (int i = 0; i < _ammoBar.GetChildCount(); i++)
            _ammoBar.GetChild<ColorRect>(i).Color = i < ammo ? AmmoFull : AmmoEmpty;
        _prompt.Visible = ammo == 0;   // caricatore vuoto: invito a premere R
    }

    // Countdown della modalità A tempo (REQ-005_4).
    public void UpdateTimer(float seconds)
    {
        int s = Mathf.CeilToInt(Mathf.Max(seconds, 0));
        _modeValue.Text = $"{s / 60}:{s % 60:00}";
    }

    // Precisione attuale, mostrata nella modalità Precisione (REQ-006_3).
    public void UpdateAccuracy(float accuracy) => _modeValue.Text = $"{accuracy:0.0} %".Replace('.', ',');

    // Pannello in alto al centro: timer in A tempo, precisione in Precisione, nascosto in Classica.
    public void SetMode(GameMode mode)
    {
        _modeLabel.Text = mode.DisplayName();
        _modePanel.Visible = mode != GameMode.Classica;
        if (mode == GameMode.ATempo) UpdateTimer(60);
        if (mode == GameMode.Precisione) UpdateAccuracy(0);
    }

    // Arma in uso (REQ-011_4).
    public void SetWeapon(WeaponType weapon) => _weaponLabel.Text = weapon.DisplayName();

    // Ricarica in corso con barra di avanzamento (0..1) (REQ-002_3).
    public void ShowReload(float progress)
    {
        _noticeLabel.Text = "RICARICA…";
        _noticeLabel.Modulate = Colors.White;
        _reloadBar.Visible = true;
        _reloadBar.Value = progress;
        _notice.Visible = true;
    }

    // Avviso testuale (es. "CARICATORE VUOTO", "RICARICA NON DISPONIBILE").
    public void ShowWarning(string text)
    {
        _noticeLabel.Text = text;
        _noticeLabel.Modulate = Danger;
        _reloadBar.Visible = false;
        _notice.Visible = true;
    }

    public void HideNotice() => _notice.Visible = false;

    // Overlay di debug (F3) usato nei test (TC-002, TC-015, TC-016).
    public void SetDebugText(string text) => _debugLabel.Text = text;

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.F3)
            _debugPanel.Visible = !_debugPanel.Visible;
    }

    private void BuildAmmoBar(int size)
    {
        foreach (Node child in _ammoBar.GetChildren()) { _ammoBar.RemoveChild(child); child.QueueFree(); }
        _ammoBar.Columns = Mathf.Min(size, 15);
        bool twoRows = size > 15;
        for (int i = 0; i < size; i++)
            _ammoBar.AddChild(new ColorRect { CustomMinimumSize = new Vector2(twoRows ? 6 : 7, twoRows ? 30 : 40), Color = AmmoFull });
        _barSize = size;
    }
}
