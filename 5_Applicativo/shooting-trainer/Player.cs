using Godot;

public partial class Player : CharacterBody3D
{
    [Export] public float MoveSpeed = 5f;
    [Export] public float MouseSensitivity = 0.002f;

    private Camera3D _camera;
    private float _pitch;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");
        Input.MouseMode = Input.MouseModeEnum.Captured;   // REQ-008_1
    }

    public override void _Input(InputEvent ev)
    {
        // Rotazione della visuale con il mouse (REQ-001_3, REQ-001_4)
        if (ev is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            RotateY(-motion.Relative.X * MouseSensitivity);
            _pitch = Mathf.Clamp(_pitch - motion.Relative.Y * MouseSensitivity,
                                 Mathf.DegToRad(-90), Mathf.DegToRad(90));
            _camera.Rotation = new Vector3(_pitch, 0, 0);
        }
        else if (ev.IsActionPressed("shoot"))
        {
            if (Input.MouseMode != Input.MouseModeEnum.Captured)
                Input.MouseMode = Input.MouseModeEnum.Captured;   // primo click: ricattura il mouse
            else
                Shoot();
        }
        else if (ev.IsActionPressed("ui_cancel"))   // ESC: libera il mouse (prova della futura pausa)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    // Movimento W/A/S/D (REQ-001_1, REQ-001_2)
    public override void _PhysicsProcess(double delta)
    {
        Vector2 input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        Vector3 direction = (Transform.Basis * new Vector3(input.X, 0, input.Y)).Normalized();

        Vector3 velocity = Velocity;
        velocity.X = direction.X * MoveSpeed;
        velocity.Z = direction.Z * MoveSpeed;
        velocity.Y = IsOnFloor() ? 0 : velocity.Y - 9.8f * (float)delta;   // gravità

        Velocity = velocity;
        MoveAndSlide();
    }

    // Sparo: raggio dal centro della camera (REQ-002_1, REQ-003)
    private void Shoot()
    {
        var space = GetWorld3D().DirectSpaceState;
        Vector3 from = _camera.GlobalPosition;
        Vector3 to = from - _camera.GlobalTransform.Basis.Z * 100f;   // 100 m in avanti

        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };   // ignora il Player
        var result = space.IntersectRay(query);

        if (result.Count > 0)
        {
            var collider = (Node)result["collider"];
            var point = (Vector3)result["position"];
            GD.Print($"Colpito: {collider.Name} nel punto {point}");
        }
        else
        {
            GD.Print("Mancato");
        }
    }
}