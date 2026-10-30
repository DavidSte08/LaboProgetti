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
}
