using Godot;

// Menu di pausa (mockup 3, REQ-008_2). Funziona anche con il gioco in pausa (Process Mode: Always).
public partial class PauseMenu : Control
{
    [Signal] public delegate void ResumeRequestedEventHandler();
    [Signal] public delegate void MenuRequestedEventHandler();

    public override void _Ready()
    {
        GetNode<Button>("Center/Panel/VBox/ResumeButton").Pressed += OnResumePressed;
        GetNode<Button>("Center/Panel/VBox/MenuButton").Pressed += OnMenuPressed;
    }

    public void Open() => Visible = true;

    public void Close() => Visible = false;

    public void OnResumePressed() => EmitSignal(SignalName.ResumeRequested);

    public void OnMenuPressed() => EmitSignal(SignalName.MenuRequested);
}
