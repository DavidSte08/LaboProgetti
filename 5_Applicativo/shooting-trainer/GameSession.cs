using System.Collections.Generic;

// Dati della partita corrente (struttura dati): unica fonte per punteggio, colpi e tempo.
// La crea il GameManager all'avvio della partita (per ora Main.cs).
public class GameSession
{
    public GameMode Mode;
    public WeaponType Weapon;
    public GameState State = GameState.InGame;
    public int Score;
    public int ShotsFired;
    public int ShotsHit;
    public float TimeLeft;                                   // solo modalità A tempo
    public List<ShotResult> Shots { get; } = new();          // per la mappa dei colpi (REQ-010)

    public GameSession(GameMode mode, WeaponType weapon)
    {
        Mode = mode;
        Weapon = weapon;
    }
}
