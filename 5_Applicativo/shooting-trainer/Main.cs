using Godot;

// Script provvisorio della scena principale.
// Verrà sostituito da GameManager nella fase "Integrazione" (Gantt: 04.12):
// per ora equipaggia l'arma, crea la GameSession e collega arma, ScoreManager e HUD.
public partial class Main : Node3D
{
    private static readonly WeaponData Pistola = GD.Load<WeaponData>("res://weapons/pistola.tres");
    private static readonly WeaponData Fucile = GD.Load<WeaponData>("res://weapons/fucile.tres");

    private Hud _hud;
    private Player _player;
    private Weapon _weapon;
    private ScoreManager _score;
    private GameSession _session;
    private double _lastShotTime = -1, _lastInterval;

    public override void _Ready()
    {
        _hud = GetNode<Hud>("Hud");
        _player = GetNode<Player>("Mondo/Player");
        _weapon = _player.Weapon;
        _score = GetNode<ScoreManager>("ScoreManager");

        _weapon.AmmoChanged += (ammo, size) => _hud.UpdateAmmo(ammo, size);
        _weapon.ReloadProgress += progress => _hud.ShowReload(progress);
        _weapon.ReloadFinished += () => _hud.HideNotice();
        _weapon.ReloadDenied += () => _hud.ShowWarning("RICARICA NON DISPONIBILE");
        _weapon.EmptyTrigger += () => _hud.ShowWarning("CARICATORE VUOTO");
        _weapon.ShotResolved += OnShotResolved;
        _score.ScoreChanged += OnScoreChanged;

        EquipWeapon(Pistola);
    }

    // Provvisorio: cambiare arma fa partire una nuova "partita" in modalità Classica.
    private void EquipWeapon(WeaponData data)
    {
        _session = new GameSession(GameMode.Classica, data.Type);
        _score.Reset(_session);
        foreach (Target target in GetTree().GetNodesInGroup("targets")) target.ClearBulletHoles();

        _hud.HideNotice();
        _hud.SetMode(_session.Mode);
        _hud.SetWeapon(data.Type);
        _weapon.Equip(data);
    }

    // Ogni colpo va allo ScoreManager, che aggiorna la GameSession (REQ-004).
    private void OnShotResolved(int zone, Vector2 localHitPoint, int targetId)
    {
        double now = Time.GetTicksUsec() / 1_000_000.0;
        if (_lastShotTime >= 0) _lastInterval = now - _lastShotTime;
        _lastShotTime = now;

        _score.RegisterShot(new ShotResult((HitZone)zone, localHitPoint, targetId));
    }

    // Punteggio (e precisione nella modalità Precisione) nell'HUD (REQ-005_3).
    private void OnScoreChanged(int score, float accuracy)
    {
        _hud.UpdateScore(score);
        if (_session.Mode == GameMode.Precisione) _hud.UpdateAccuracy(accuracy);
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        // Provvisorio per i test: 1 = pistola, 2 = fucile d'assalto (la scelta vera è nel menu).
        if (ev is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.Key1) EquipWeapon(Pistola);
            else if (key.Keycode == Key.Key2) EquipWeapon(Fucile);
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
    public override void _Process(double delta)
    {
        var s = _weapon.State;
        _hud.SetDebugText(
            $"FPS: {Engine.GetFramesPerSecond()}\n" +
            $"Angolo verticale: {_player.PitchDegrees:0.0}°\n" +
            $"Posizione: {_player.GlobalPosition.X:0.0}; {_player.GlobalPosition.Z:0.0}\n" +
            $"Arma: {s.Data.Name}  {s.Ammo}/{s.Data.MagazineSize}{(s.IsReloading ? "  (ricarica)" : "")}\n" +
            $"Punteggio: {_session.Score}  Colpi: {_session.ShotsHit}/{_session.ShotsFired}  Precisione: {_score.GetAccuracy():0.0} %\n" +
            $"Intervallo ultimo colpo: {_lastInterval:0.000} s");
    }
}
