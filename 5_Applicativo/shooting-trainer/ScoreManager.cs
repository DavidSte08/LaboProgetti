using Godot;

// Punteggio, colpi a segno e precisione (REQ-004).
// Aggiorna la GameSession a ogni colpo e avvisa l'HUD con il segnale ScoreChanged.
public partial class ScoreManager : Node
{
    [Signal] public delegate void ScoreChangedEventHandler(int score, float accuracy);

    public GameSession Session { get; private set; }

    // Azzera punteggio e contatori all'inizio di ogni partita (REQ-004).
    public void Reset(GameSession session)
    {
        Session = session;
        Session.Score = 0;
        Session.ShotsFired = 0;
        Session.ShotsHit = 0;
        Session.Shots.Clear();
        EmitSignal(SignalName.ScoreChanged, 0, 0f);
    }

    // Registra un colpo: conta i colpi sparati e, se a segno, quelli a segno e i punti (REQ-004_1, REQ-004_2).
    public void RegisterShot(ShotResult shot)
    {
        if (Session == null) return;

        Session.ShotsFired++;
        if (shot.Zone != HitZone.Miss)
        {
            Session.ShotsHit++;
            Session.Score += (int)shot.Zone;   // il valore dell'enum corrisponde ai punti
        }
        Session.Shots.Add(shot);
        EmitSignal(SignalName.ScoreChanged, Session.Score, GetAccuracy());
    }

    // Precisione in percentuale, 0 se non è stato sparato nessun colpo (REQ-004_3).
    public float GetAccuracy()
    {
        if (Session == null || Session.ShotsFired == 0) return 0f;
        return Session.ShotsHit * 100f / Session.ShotsFired;
    }
}
