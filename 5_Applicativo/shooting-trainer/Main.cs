using Godot;

// Script provvisorio della scena principale.
// Verrà sostituito da GameManager nella fase "Integrazione" (Gantt: 04.12):
// per ora equipaggia l'arma e collega i segnali dell'arma all'HUD.
public partial class Main : Node3D
{
    private static readonly WeaponData Pistola = GD.Load<WeaponData>("res://weapons/pistola.tres");
    private static readonly WeaponData Fucile = GD.Load<WeaponData>("res://weapons/fucile.tres");

    private Hud _hud;
    private Player _player;
    private Weapon _weapon;
    private int _shotsFired;
    private double _lastShotTime = -1, _lastInterval;

    public override void _Ready()
    {
        _hud = GetNode<Hud>("Hud");
        _player = GetNode<Player>("Mondo/Player");
        _weapon = _player.Weapon;

        _weapon.AmmoChanged += (ammo, size) => _hud.UpdateAmmo(ammo, size);
        _weapon.ReloadProgress += progress => _hud.ShowReload(progress);
        _weapon.ReloadFinished += () => _hud.HideNotice();
        _weapon.ReloadDenied += () => _hud.ShowWarning("RICARICA NON DISPONIBILE");
        _weapon.EmptyTrigger += () => _hud.ShowWarning("CARICATORE VUOTO");
        _weapon.ShotResolved += OnShotResolved;

        _hud.SetMode(GameMode.Classica);
        _hud.UpdateScore(0);
        EquipWeapon(Pistola);
    }

    private void EquipWeapon(WeaponData data)
    {
        _hud.HideNotice();
        _hud.SetWeapon(data.Type);
        _weapon.Equip(data);
        _shotsFired = 0;
    }

    // Per ora il colpo viene solo scritto nella console: il punteggio arriva con ScoreManager (20.11).
    private void OnShotResolved(int zone, Vector2 localHitPoint, int targetId)
    {
        double now = Time.GetTicksUsec() / 1_000_000.0;
        if (_lastShotTime >= 0) _lastInterval = now - _lastShotTime;
        _lastShotTime = now;
        _shotsFired++;

        var hitZone = (HitZone)zone;
        GD.Print(targetId < 0 || hitZone == HitZone.Miss
            ? "Mancato"
            : $"Bersaglio {targetId}: {hitZone} = {zone} punti ({localHitPoint.X:0.00}; {localHitPoint.Y:0.00})");
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
            $"Colpi sparati: {_shotsFired}\n" +
            $"Intervallo ultimo colpo: {_lastInterval:0.000} s");
    }
}
