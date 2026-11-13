using Godot;

// Caratteristiche fisse di un'arma (struttura dati, REQ-011).
// Ogni arma è un file .tres nella cartella weapons.
[GlobalClass]
public partial class WeaponData : Resource
{
    [Export] public WeaponType Type = WeaponType.Pistola;
    [Export] public int MagazineSize = 15;          // colpi per caricatore
    [Export] public float FireInterval = 0.2f;      // secondi minimi tra due colpi
    [Export] public bool Automatic = false;         // true: spara tenendo premuto
    [Export] public float ReloadTime = 1.5f;        // durata della ricarica in secondi
    [Export] public float Recoil = 0.05f;           // spostamento del modello a ogni colpo
    [Export] public PackedScene Model;              // modello 3D in mano al giocatore
    [Export] public AudioStream ShotSound;
    [Export] public AudioStream ReloadSound;

    public string Name => Type.DisplayName();
}
