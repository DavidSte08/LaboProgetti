using Godot;

// Giocatore in prima persona: movimento, visuale e comandi dell'arma (REQ-001, REQ-002).
public partial class Player : CharacterBody3D
{
    [Export] public float MoveSpeed = 5f;              // metri al secondo
    [Export] public float Acceleration = 12f;          // quanto velocemente raggiunge MoveSpeed
    [Export] public float MouseSensitivity = 0.002f;   // radianti per pixel
    [Export] public float Gravity = 9.8f;
    [Export] public float BobAmount = 0.03f;           // oscillazione della camera camminando
    [Export] public float BobSpeed = 9f;

    public Weapon Weapon { get; private set; }
    public float PitchDegrees => Mathf.RadToDeg(_pitch);   // angolo verticale (overlay di debug, TC-002)

    private Camera3D _camera;
    private float _pitch;
    private float _bobTime;
    private float _cameraHeight;
    private bool _inputEnabled = true;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");
        Weapon = GetNode<Weapon>("Camera3D/Weapon");
        _cameraHeight = _camera.Position.Y;
        Input.MouseMode = Input.MouseModeEnum.Captured;   // REQ-008_1
    }

    // Attiva o disattiva movimento, visuale e sparo (chiamato dal GameManager a ogni cambio di stato).
    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
        if (!enabled) Weapon.SetTrigger(false);
    }

    public override void _Input(InputEvent ev)
    {
        if (!_inputEnabled) return;

        if (ev is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            Look(motion.Relative);
        }
        else if (ev.IsActionPressed("shoot"))
        {
            // Provvisorio fino al GameManager: il primo click dopo ESC ricattura il mouse.
            if (Input.MouseMode != Input.MouseModeEnum.Captured)
                Input.MouseMode = Input.MouseModeEnum.Captured;
            else
                Weapon.SetTrigger(true);
        }
        else if (ev.IsActionReleased("shoot"))
        {
            Weapon.SetTrigger(false);
        }
        else if (ev.IsActionPressed("reload"))
        {
            Weapon.Reload();
        }
        else if (ev.IsActionPressed("ui_cancel"))
        {
            // Provvisorio: ESC libera il mouse. La pausa vera arriva con il GameManager.
            Weapon.SetTrigger(false);
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Move(delta);
        UpdateHeadBob(delta);
    }

    // Ruota il corpo in orizzontale e la camera in verticale, limitata a ±90° (REQ-001_3, REQ-001_4).
    public void Look(Vector2 relative)
    {
        RotateY(-relative.X * MouseSensitivity);
        _pitch = Mathf.Clamp(_pitch - relative.Y * MouseSensitivity, Mathf.DegToRad(-90), Mathf.DegToRad(90));
        _camera.Rotation = new Vector3(_pitch, 0, 0);
    }

    // Direzione dai tasti W/A/S/D rispetto all'orientamento del giocatore (REQ-001_1, REQ-001_2).
    public void Move(double delta)
    {
        Vector2 input = _inputEnabled
            ? Input.GetVector("move_left", "move_right", "move_forward", "move_back")
            : Vector2.Zero;
        Vector3 direction = (Transform.Basis * new Vector3(input.X, 0, input.Y)).Normalized();

        Vector3 velocity = Velocity;
        Vector3 target = direction * MoveSpeed;
        float t = Mathf.Min(1f, Acceleration * (float)delta);
        velocity.X = Mathf.Lerp(velocity.X, target.X, t);
        velocity.Z = Mathf.Lerp(velocity.Z, target.Z, t);
        velocity.Y = IsOnFloor() ? 0 : velocity.Y - Gravity * (float)delta;

        Velocity = velocity;
        MoveAndSlide();
    }

    // Leggera oscillazione verticale della camera mentre il giocatore cammina.
    private void UpdateHeadBob(double delta)
    {
        float speed = new Vector2(Velocity.X, Velocity.Z).Length();
        if (speed > 0.5f && IsOnFloor())
            _bobTime += (float)delta * BobSpeed * (speed / MoveSpeed);
        else
            _bobTime = Mathf.Lerp(_bobTime, Mathf.Round(_bobTime / Mathf.Pi) * Mathf.Pi, (float)delta * 10f);

        var pos = _camera.Position;
        pos.Y = _cameraHeight + Mathf.Sin(_bobTime) * BobAmount;
        _camera.Position = pos;
    }
}
