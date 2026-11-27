using Godot;

// Script provvisorio della scena principale.
// Verrà sostituito da GameManager nella fase "Integrazione" (Gantt: 04.12):
// per ora avvia la partita con modalità e arma, gestisce timer e fine partita
// e collega arma, ScoreManager e HUD.
public partial class Main : Node3D
{
    private static readonly WeaponData Pistola = GD.Load<WeaponData>("res://weapons/pistola.tres");
    private static readonly WeaponData Fucile = GD.Load<WeaponData>("res://weapons/fucile.tres");

    private Hud _hud;
    private Player _player;
    private Weapon _weapon;
    private ScoreManager _score;
    private GameSession _session;
    private WeaponData _weaponData = Pistola;
    private GameMode _mode = GameMode.Classica;
    private double _lastShotTime = -1, _lastInterval;

    public override void _Ready()
    {
        _hud = GetNode<Hud>("Hud");
        _player = GetNode<Player>("Mondo/Player");
        _weapon = _player.Weapon;
        _score = GetNode<ScoreManager>("ScoreManager");

        _weapon.AmmoChanged += (ammo, size, reserve) => _hud.UpdateAmmo(ammo, size, reserve);
        _weapon.ReloadProgress += progress => _hud.ShowReload(progress);
        _weapon.ReloadFinished += () => _hud.HideNotice();
        _weapon.ReloadDenied += () => _hud.ShowWarning("RICARICA NON DISPONIBILE");
        _weapon.EmptyTrigger += () => _hud.ShowWarning("CARICATORE VUOTO");
        _weapon.ShotResolved += OnShotResolved;
        _score.ScoreChanged += OnScoreChanged;

        StartGame(_mode, _weaponData);
    }

    // Avvia una partita con la modalità e l'arma scelte (REQ-006, REQ-011).
    public void StartGame(GameMode mode, WeaponData data)
    {
        _mode = mode;
        _weaponData = data;

        _session = new GameSession(mode, data.Type)
        {
            MaxShots = mode.Magazines() * data.MagazineSize,   // 0 = illimitati (A tempo)
            TimeLeft = mode == GameMode.ATempo ? GameModeExtensions.TimeLimit : 0f
        };
        _score.Reset(_session);
        foreach (Target target in GetTree().GetNodesInGroup("targets")) target.ClearBulletHoles();

        // Munizioni di riserva: i caricatori oltre a quello inserito (Classica 2, Precisione 0, A tempo illimitate).
        int reserve = mode.Magazines() > 0 ? (mode.Magazines() - 1) * data.MagazineSize : -1;

        _hud.HideNotice();
        _hud.ReloadHint = mode.AllowsReload();
        _hud.SetMode(mode);
        _hud.SetWeapon(data.Type);

        _weapon.CanReload = mode.AllowsReload();
        _weapon.IsActive = true;
        _weapon.Equip(data, reserve);
        _player.SetInputEnabled(true);
    }

    // Fine partita: tempo scaduto o colpi esauriti (REQ-005_4, REQ-006).
    public void EndGame()
    {
        if (_session.State == GameState.Ended) return;
        _session.State = GameState.Ended;

        _weapon.SetTrigger(false);
        _weapon.IsActive = false;
        _hud.ReloadHint = false;

        // Per ora i risultati compaiono nell'HUD; la schermata finale viene collegata il 04.12.
        string accuracy = $"{_score.GetAccuracy():0.0}".Replace('.', ',');
        _hud.ShowInfo($"PARTITA TERMINATA · {_session.Score} punti · {_session.ShotsHit}/{_session.ShotsFired} a segno · {accuracy} %\nINVIO per ricominciare");
        GD.Print($"Fine partita ({_session.Mode.DisplayName()}, {_session.Weapon.DisplayName()}): " +
                 $"{_session.Score} punti, {_session.ShotsHit}/{_session.ShotsFired}, precisione {accuracy} %");
    }

    // Ogni colpo va allo ScoreManager; raggiunti i colpi massimi la partita finisce.
    private void OnShotResolved(int zone, Vector2 localHitPoint, int targetId)
    {
        double now = Time.GetTicksUsec() / 1_000_000.0;
        if (_lastShotTime >= 0) _lastInterval = now - _lastShotTime;
        _lastShotTime = now;

        _score.RegisterShot(new ShotResult((HitZone)zone, localHitPoint, targetId));

        if (_session.MaxShots > 0 && _session.ShotsFired >= _session.MaxShots)
            EndGame();
    }

    // Punteggio (e precisione nella modalità Precisione) nell'HUD (REQ-005_3).
    private void OnScoreChanged(int score, float accuracy)
    {
        _hud.UpdateScore(score);
        if (_session.Mode == GameMode.Precisione) _hud.UpdateAccuracy(accuracy);
    }

    public override void _Process(double delta)
    {
        // Timer della modalità A tempo (REQ-005_4, REQ-006_2).
        if (_session.State == GameState.InGame && _session.Mode == GameMode.ATempo)
        {
            _session.TimeLeft = Mathf.Max(0f, _session.TimeLeft - (float)delta);
            _hud.UpdateTimer(_session.TimeLeft);
            if (_session.TimeLeft <= 0f) EndGame();
        }

        UpdateDebug();
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        // Provvisorio per i test (la scelta vera è nel menu):
        // 1 = pistola, 2 = fucile d'assalto, M = modalità successiva, INVIO = ricomincia.
        if (ev is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.Key1) StartGame(_mode, Pistola);
            else if (key.Keycode == Key.Key2) StartGame(_mode, Fucile);
            else if (key.Keycode == Key.M) StartGame((GameMode)(((int)_mode + 1) % 3), _weaponData);
            else if (key.Keycode == Key.Enter || key.Keycode == Key.KpEnter) StartGame(_mode, _weaponData);
            else if (key.Keycode == Key.F11) ToggleFullscreen();
        }
    }

    // F11: passa da schermo intero a finestra e viceversa.
    private static void ToggleFullscreen()
    {
        bool full = DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen;
        DisplayServer.WindowSetMode(full ? DisplayServer.WindowMode.Windowed : DisplayServer.WindowMode.Fullscreen);
    }

    // Overlay di debug (F3) usato nei test TC-002, TC-015 e TC-016.
    private void UpdateDebug()
    {
        var s = _weapon.State;
        string shots = _session.MaxShots > 0 ? $"{_session.ShotsFired}/{_session.MaxShots}" : $"{_session.ShotsFired}";
        _hud.SetDebugText(
            $"FPS: {Engine.GetFramesPerSecond()}\n" +
            $"Angolo verticale: {_player.PitchDegrees:0.0}°\n" +
            $"Posizione: {_player.GlobalPosition.X:0.0}; {_player.GlobalPosition.Z:0.0}\n" +
            $"Modalità: {_session.Mode.DisplayName()}  Stato: {_session.State}\n" +
            $"Arma: {s.Data.Name}  {s.Ammo}/{s.Data.MagazineSize}  riserva {(s.Reserve < 0 ? "∞" : s.Reserve.ToString())}{(s.IsReloading ? "  (ricarica)" : "")}\n" +
            $"Colpi sparati: {shots}  A segno: {_session.ShotsHit}  Precisione: {_score.GetAccuracy():0.0} %\n" +
            $"Punteggio: {_session.Score}  Tempo: {_session.TimeLeft:0.0} s\n" +
            $"Intervallo ultimo colpo: {_lastInterval:0.000} s");
    }
}
