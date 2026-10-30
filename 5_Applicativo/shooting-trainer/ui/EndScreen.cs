using System.Collections.Generic;
using System.Linq;
using Godot;

// Schermata di fine partita (mockup 4, REQ-004, REQ-007_2, REQ-010, REQ-011_4).
public partial class EndScreen : Control
{
    [Signal] public delegate void RestartRequestedEventHandler();
    [Signal] public delegate void MenuRequestedEventHandler();

    // Solo per l'anteprima nell'editor (F6): mostra dati di esempio.
    [Export] public bool ShowDemoData = false;

    private Label _modeWeapon, _score, _hits, _accuracy;
    private HitMap _hitMap;

    public override void _Ready()
    {
        _modeWeapon = GetNode<Label>("Center/Layout/PillRow/Pill/ModeWeapon");
        _score = GetNode<Label>("Center/Layout/Body/Left/ScorePanel/VBox/Value");
        _hits = GetNode<Label>("Center/Layout/Body/Left/HitsPanel/VBox/Value");
        _accuracy = GetNode<Label>("Center/Layout/Body/Left/AccuracyPanel/VBox/Value");
        _hitMap = GetNode<HitMap>("Center/Layout/Body/MapPanel/VBox/HitMap");
        GetNode<Button>("Center/Layout/Body/Left/Buttons/RestartButton").Pressed += OnRestartPressed;
        GetNode<Button>("Center/Layout/Body/Left/Buttons/MenuButton").Pressed += OnMenuPressed;

        if (ShowDemoData) ShowDemo();
    }

    // Mostra i risultati della partita (UC-09).
    public void ShowResults(int score, float accuracy, List<ShotResult> shots, GameMode mode, WeaponType weapon)
    {
        int hits = shots.Count(s => s.Zone != HitZone.Miss);
        _modeWeapon.Text = $"{mode.DisplayName()} · {weapon.DisplayName()}";
        _score.Text = score.ToString();
        _hits.Text = $"{hits} / {shots.Count}";
        _accuracy.Text = $"{accuracy:0.0} %".Replace('.', ',');   // virgola decimale
        DrawHitMap(shots);
    }

    // Mappa dei colpi: un punto per ogni colpo a segno (REQ-010, priorità 3).
    private void DrawHitMap(List<ShotResult> shots)
    {
        _hitMap.SetHits(shots.Where(s => s.Zone != HitZone.Miss).Select(s => s.LocalHitPoint).ToList());
    }

    public void OnRestartPressed() => EmitSignal(SignalName.RestartRequested);

    public void OnMenuPressed() => EmitSignal(SignalName.MenuRequested);

    private void ShowDemo()
    {
        var rng = new RandomNumberGenerator { Seed = 7 };
        var shots = new List<ShotResult>();
        int score = 0;
        for (int i = 0; i < 90; i++)
        {
            if (i >= 61) { shots.Add(new ShotResult(HitZone.Miss, Vector2.Zero, -1)); continue; }
            float r = Mathf.Min(Mathf.Abs(rng.Randfn(0, 0.12f)), 0.35f);
            float a = rng.Randf() * Mathf.Tau;
            HitZone zone = r <= 0.12f ? HitZone.Center : r <= 0.24f ? HitZone.Middle : HitZone.Outer;
            score += (int)zone;
            shots.Add(new ShotResult(zone, new Vector2(r * Mathf.Cos(a), r * Mathf.Sin(a)), 1));
        }
        ShowResults(score, 61f / 90f * 100f, shots, GameMode.Classica, WeaponType.FucileAssalto);
    }
}
