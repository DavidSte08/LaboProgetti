using Godot;

// Bersaglio fisso con tre zone concentriche (REQ-003).
// Il disco è rivolto verso +Z locale, cioè verso il giocatore.
public partial class Target : StaticBody3D
{
    [Export] public int Id;                       // identificativo del bersaglio
    [Export] public float RadiusCenter = 0.12f;   // zona centrale: 10 punti
    [Export] public float RadiusMiddle = 0.24f;   // anello intermedio: 5 punti
    [Export] public float RadiusOuter = 0.36f;    // anello esterno: 2 punti

    // Converte il punto d'impatto (coordinate globali) in coordinate 2D
    // sul piano del bersaglio, con l'origine nel centro (usato per la mappa dei colpi).
    public Vector2 ToLocal2D(Vector3 hitPoint)
    {
        Vector3 local = ToLocal(hitPoint);
        return new Vector2(local.X, local.Y);
    }

    // Determina la zona colpita in base alla distanza dal centro.
    public HitZone GetZone(Vector3 hitPoint)
    {
        float distance = ToLocal2D(hitPoint).Length();

        if (distance <= RadiusCenter) return HitZone.Center;
        if (distance <= RadiusMiddle) return HitZone.Middle;
        if (distance <= RadiusOuter) return HitZone.Outer;
        return HitZone.Miss;
    }

    // Foro del proiettile nel punto colpito, per vedere dove si è sparato.
    private const int MaxHoles = 60;
    private static CylinderMesh _holeMesh;

    public void AddBulletHole(Vector3 hitPoint)
    {
        _holeMesh ??= new CylinderMesh
        {
            TopRadius = 0.012f, BottomRadius = 0.012f, Height = 0.001f, RadialSegments = 12, Rings = 1,
            Material = new StandardMaterial3D { AlbedoColor = new Color(0.08f, 0.07f, 0.06f), Roughness = 1f }
        };

        Vector2 p = ToLocal2D(hitPoint);
        var hole = new MeshInstance3D { Mesh = _holeMesh, Name = "Foro" };
        hole.Position = new Vector3(p.X, p.Y, 0.013f);              // appena davanti ai dischi
        hole.Rotation = new Vector3(Mathf.DegToRad(90), 0, 0);      // disco rivolto verso +Z
        hole.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        AddChild(hole);
        _holes.Enqueue(hole);
        if (_holes.Count > MaxHoles) _holes.Dequeue().QueueFree();
    }

    // Mostra i punti ottenuti sopra il bersaglio ("+10", "+5", "+2"), che salgono e svaniscono.
    public void ShowPoints(HitZone zone)
    {
        if (zone == HitZone.Miss) return;
        var color = zone switch
        {
            HitZone.Center => new Color(0.95f, 0.3f, 0.25f),
            HitZone.Middle => new Color(0.4f, 0.65f, 1f),
            _ => new Color(0.95f, 0.95f, 0.95f)
        };
        var label = new Label3D
        {
            Text = $"+{(int)zone}",
            Modulate = color,
            OutlineModulate = new Color(0, 0, 0, 0.8f),
            OutlineSize = 8,
            FontSize = 48,
            PixelSize = 0.0007f,
            FixedSize = true,                                   // stessa grandezza a 5 e a 25 m
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true,
            Position = new Vector3(0.3f, 0.5f, 0.05f)
        };
        AddChild(label);
        var tween = label.CreateTween().SetParallel();
        tween.TweenProperty(label, "position", label.Position + new Vector3(0, 0.25f, 0), 0.8f);
        tween.TweenProperty(label, "modulate:a", 0f, 0.8f).SetDelay(0.3f);
        tween.TweenProperty(label, "outline_modulate:a", 0f, 0.8f).SetDelay(0.3f);
        tween.Chain().TweenCallback(Callable.From(label.QueueFree));
    }

    // Rimuove i fori (all'inizio di una nuova partita).
    public void ClearBulletHoles()
    {
        while (_holes.Count > 0) _holes.Dequeue().QueueFree();
    }

    private readonly System.Collections.Generic.Queue<MeshInstance3D> _holes = new();
}
