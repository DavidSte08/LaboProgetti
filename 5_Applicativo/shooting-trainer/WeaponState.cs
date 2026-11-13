// Stato dell'arma durante la partita (struttura dati, REQ-002_2, REQ-002_3, REQ-002_5).
public class WeaponState
{
    public WeaponData Data;
    public int Ammo;                 // munizioni nel caricatore
    public bool IsReloading;
    public double ReloadElapsed;     // secondi trascorsi dall'inizio della ricarica
    public double LastShotTime = -1; // istante dell'ultimo colpo (secondi)

    public WeaponState(WeaponData data)
    {
        Data = data;
        Ammo = data.MagazineSize;
    }
}
