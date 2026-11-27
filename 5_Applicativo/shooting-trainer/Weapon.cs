using Godot;

// Arma in mano al giocatore: sparo, munizioni, cadenza e ricarica (REQ-002, REQ-011).
// È figlia della Camera3D del Player, così il raggio parte sempre dal centro della visuale.
public partial class Weapon : Node3D
{
    [Signal] public delegate void ShotResolvedEventHandler(int zone, Vector2 localHitPoint, int targetId);
    [Signal] public delegate void AmmoChangedEventHandler(int ammo, int magazineSize, int reserve);   // reserve -1 = illimitate
    [Signal] public delegate void ReloadProgressEventHandler(float progress);   // 0..1
    [Signal] public delegate void ReloadFinishedEventHandler();
    [Signal] public delegate void ReloadDeniedEventHandler();                    // modalità Precisione
    [Signal] public delegate void EmptyTriggerEventHandler();                    // click a caricatore vuoto

    [Export] public float Range = 100f;
    [Export] public AudioStream EmptySound;

    // Impostati dal GameManager (per ora da Main.cs).
    public bool IsActive = true;    // false in pausa o fuori dalla partita
    public bool CanReload = true;   // false nella modalità Precisione (REQ-006_3)

    public WeaponState State { get; private set; }

    // Accesso rapido allo stato (usato dall'overlay di debug e dagli script di test).
    public int Ammo => State?.Ammo ?? 0;
    public int MagazineSize => State?.Data.MagazineSize ?? 0;
    public int Reserve => State?.Reserve ?? 0;
    public bool IsReloading => State?.IsReloading ?? false;
    public string WeaponName => State?.Data.Name ?? "";

    private Camera3D _camera;
    private Node3D _modelHolder, _model;
    private OmniLight3D _flash;
    private AudioStreamPlayer _shotPlayer, _reloadPlayer, _emptyPlayer;
    private bool _trigger;
    private Vector3 _restPosition;
    private Tween _recoilTween, _reloadTween;

    public override void _Ready()
    {
        _camera = GetParent<Camera3D>();
        _modelHolder = GetNode<Node3D>("ModelHolder");
        _flash = GetNode<OmniLight3D>("ModelHolder/Flash");
        _shotPlayer = GetNode<AudioStreamPlayer>("ShotPlayer");
        _reloadPlayer = GetNode<AudioStreamPlayer>("ReloadPlayer");
        _emptyPlayer = GetNode<AudioStreamPlayer>("EmptyPlayer");
        _emptyPlayer.Stream = EmptySound;
        _restPosition = _modelHolder.Position;
        _flash.Visible = false;
    }

    // Carica modello e caratteristiche dell'arma e riempie il caricatore (REQ-011).
    // reserve: munizioni di riserva oltre al caricatore (-1 = illimitate, 0 = nessuna).
    public void Equip(WeaponData data, int reserve = -1)
    {
        State = new WeaponState(data) { Reserve = reserve };
        _trigger = false;
        _reloadPlayer.Stop();
        _reloadTween?.Kill();

        _model?.QueueFree();
        _model = data.Model.Instantiate<Node3D>();
        _modelHolder.AddChild(_model);
        _modelHolder.Position = _restPosition;
        _modelHolder.Rotation = Vector3.Zero;

        // Il lampo di sparo si sposta sulla volata del modello (nodo "Muzzle").
        var muzzle = _model.GetNodeOrNull<Node3D>("Muzzle");
        _flash.Position = muzzle != null ? muzzle.Position : new Vector3(0, 0, -0.3f);

        _shotPlayer.Stream = data.ShotSound;
        _reloadPlayer.Stream = data.ReloadSound;
        EmitAmmo();
    }

    // Grilletto premuto o rilasciato (REQ-002_5, REQ-011_2, REQ-011_3).
    public void SetTrigger(bool pressed)
    {
        bool justPressed = pressed && !_trigger;
        _trigger = pressed;
        if (!justPressed || State == null || !IsActive) return;

        if (State.Ammo == 0 && !State.IsReloading)
        {
            _emptyPlayer.Play();
            EmitSignal(SignalName.EmptyTrigger);
            return;
        }
        Shoot();   // semiautomatica: un colpo per click; automatica: il primo colpo
    }

    public override void _Process(double delta)
    {
        if (State == null) return;

        // Fucile automatico: continua a sparare finché il grilletto resta premuto.
        if (_trigger && State.Data.Automatic) Shoot();

        if (State.IsReloading && IsActive)
        {
            State.ReloadElapsed += delta;
            float progress = Mathf.Clamp((float)(State.ReloadElapsed / State.Data.ReloadTime), 0f, 1f);
            EmitSignal(SignalName.ReloadProgress, progress);
            if (progress >= 1f) FinishReload();
        }
    }

    // Vero se si può sparare in questo momento (REQ-002_2, REQ-002_5).
    public bool CanShoot()
    {
        if (State == null || !IsActive || State.IsReloading || State.Ammo <= 0) return false;
        return State.LastShotTime < 0 || Now() - State.LastShotTime >= State.Data.FireInterval - 0.001;
    }

    // Sparo: raggio dal centro della camera, zona colpita con GetZone (REQ-002_1, REQ-003).
    public void Shoot()
    {
        if (!CanShoot()) return;

        State.Ammo--;
        // Fucile in tiro continuo: il colpo successivo si conta dall'istante previsto, non dal frame,
        // così la cadenza resta di 10 colpi al secondo anche se i frame non coincidono.
        double now = Now();
        bool continuous = State.Data.Automatic && State.LastShotTime >= 0 && now - State.LastShotTime < State.Data.FireInterval * 1.5;
        State.LastShotTime = continuous ? State.LastShotTime + State.Data.FireInterval : now;
        _shotPlayer.Play();
        PlayShotEffects();

        var result = CastRay();
        ShotResult shot;
        if (result.Count > 0 && result["collider"].As<Node>() is Target target)
        {
            var point = (Vector3)result["position"];
            shot = new ShotResult(target.GetZone(point), target.ToLocal2D(point), target.Id);
            target.AddBulletHole(point);
            target.ShowPoints(shot.Zone);
        }
        else
        {
            shot = new ShotResult(HitZone.Miss, Vector2.Zero, -1);
        }

        EmitSignal(SignalName.ShotResolved, (int)shot.Zone, shot.LocalHitPoint, shot.TargetId);
        EmitAmmo();
    }

    // Ricarica con il tasto R, circa 1,5 s (REQ-002_3, REQ-002_4).
    public void Reload()
    {
        if (State == null || !IsActive || State.IsReloading) return;
        if (!CanReload) { EmitSignal(SignalName.ReloadDenied); return; }
        if (State.Ammo == State.Data.MagazineSize) return;   // caricatore pieno
        if (State.Reserve == 0) return;                      // riserva finita

        State.IsReloading = true;
        State.ReloadElapsed = 0;
        _reloadPlayer.Play();
        EmitSignal(SignalName.ReloadProgress, 0f);

        // Animazione: l'arma si abbassa e si inclina, poi torna in posizione.
        float t = State.Data.ReloadTime;
        var down = _restPosition + new Vector3(0, -0.12f, 0.04f);
        var tilt = new Vector3(Mathf.DegToRad(-8), 0, Mathf.DegToRad(25));
        _recoilTween?.Kill();
        var tween = CreateTween().SetParallel();
        tween.TweenProperty(_modelHolder, "position", down, t * 0.25f).SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(_modelHolder, "rotation", tilt, t * 0.25f).SetTrans(Tween.TransitionType.Sine);
        tween.Chain().TweenInterval(t * 0.45f);
        tween.Chain().TweenProperty(_modelHolder, "position", _restPosition, t * 0.25f).SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(_modelHolder, "rotation", Vector3.Zero, t * 0.25f).SetTrans(Tween.TransitionType.Sine);
        _reloadTween = tween;
    }

    private void FinishReload()
    {
        State.IsReloading = false;

        // Dalla riserva si prendono solo i colpi che mancano nel caricatore.
        int missing = State.Data.MagazineSize - State.Ammo;
        int taken = State.Reserve < 0 ? missing : Mathf.Min(missing, State.Reserve);
        State.Ammo += taken;
        if (State.Reserve > 0) State.Reserve -= taken;

        EmitSignal(SignalName.ReloadFinished);
        EmitAmmo();
    }

    private void EmitAmmo() => EmitSignal(SignalName.AmmoChanged, State.Ammo, State.Data.MagazineSize, State.Reserve);

    private Godot.Collections.Dictionary CastRay()
    {
        Vector3 from = _camera.GlobalPosition;
        Vector3 to = from - _camera.GlobalTransform.Basis.Z * Range;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        if (Owner is CollisionObject3D player)
            query.Exclude = new Godot.Collections.Array<Rid> { player.GetRid() };   // ignora il Player
        return GetWorld3D().DirectSpaceState.IntersectRay(query);
    }

    // Rinculo del modello e lampo alla volata.
    private void PlayShotEffects()
    {
        _recoilTween?.Kill();
        _modelHolder.Position = _restPosition + new Vector3(0, State.Data.Recoil * 0.3f, State.Data.Recoil);
        _recoilTween = CreateTween();
        _recoilTween.TweenProperty(_modelHolder, "position", _restPosition, Mathf.Min(State.Data.FireInterval * 0.9f, 0.12f))
                    .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

        _flash.Visible = true;
        GetTree().CreateTimer(0.04).Timeout += () => _flash.Visible = false;
    }

    private static double Now() => Time.GetTicksUsec() / 1_000_000.0;
}
