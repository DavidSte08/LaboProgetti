using Godot;

// Esito di un singolo colpo (struttura dati, REQ-003, REQ-004, REQ-010).
public struct ShotResult
{
    public HitZone Zone;            // zona colpita (il valore corrisponde ai punti)
    public Vector2 LocalHitPoint;   // punto d'impatto sul piano del bersaglio
    public int TargetId;            // -1 se il colpo è andato a vuoto

    public ShotResult(HitZone zone, Vector2 localHitPoint, int targetId)
    {
        Zone = zone;
        LocalHitPoint = localHitPoint;
        TargetId = targetId;
    }
}
