using Godot;

// Menu principale: scelta di modalità e arma (mockup 1, REQ-006, REQ-007_1, REQ-011_1).
public partial class MainMenu : Control
{
    [Signal] public delegate void StartRequestedEventHandler(GameMode mode, WeaponType weapon);

    public GameMode SelectedMode { get; private set; } = GameMode.Classica;
    public WeaponType SelectedWeapon { get; private set; } = WeaponType.Pistola;

    public override void _Ready()
    {
        GetNode<Button>("Center/Layout/Modes/Classica").Pressed += () => OnModeSelected(GameMode.Classica);
        GetNode<Button>("Center/Layout/Modes/ATempo").Pressed += () => OnModeSelected(GameMode.ATempo);
        GetNode<Button>("Center/Layout/Modes/Precisione").Pressed += () => OnModeSelected(GameMode.Precisione);
        GetNode<Button>("Center/Layout/Weapons/Pistola").Pressed += () => OnWeaponSelected(WeaponType.Pistola);
        GetNode<Button>("Center/Layout/Weapons/FucileAssalto").Pressed += () => OnWeaponSelected(WeaponType.FucileAssalto);
        GetNode<Button>("Center/Layout/StartButton").Pressed += OnStartPressed;
        GetNode<Button>("Center/Layout/QuitButton").Pressed += OnQuitPressed;
    }

    public void OnModeSelected(GameMode mode) => SelectedMode = mode;

    public void OnWeaponSelected(WeaponType weapon) => SelectedWeapon = weapon;

    // Avvia la partita con modalità e arma scelte (UC-01): il GameManager riceve il segnale.
    public void OnStartPressed() => EmitSignal(SignalName.StartRequested, (int)SelectedMode, (int)SelectedWeapon);

    // Chiude l'applicazione (REQ-008_3, UC-12).
    public void OnQuitPressed() => GetTree().Quit();
}
