using Godot;

// Script provvisorio della scena principale.
// Verrà sostituito da GameManager nella fase "Integrazione" (Gantt: 04.12).
public partial class Main : Node3D
{
    public override void _Ready()
    {
        // Valori iniziali dell'HUD finché non c'è la logica di partita.
        var hud = GetNode<Hud>("Hud");
        hud.SetMode(GameMode.Classica);
        hud.SetWeapon(WeaponType.Pistola);
        hud.UpdateScore(0);
        hud.UpdateAmmo(15, 15);
    }
}
