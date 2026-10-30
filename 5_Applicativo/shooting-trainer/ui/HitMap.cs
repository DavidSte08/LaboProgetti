using System.Collections.Generic;
using Godot;

// Disegna il bersaglio di carta con le tre zone e i colpi a segno (REQ-010).
public partial class HitMap : Control
{
    [Export] public float TargetRadius = 0.36f;   // raggio reale della zona esterna (m)

    private List<Vector2> _hits = new();

    public void SetHits(List<Vector2> hits)
    {
        _hits = hits;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float side = Mathf.Min(Size.X, Size.Y) - 10;
        Vector2 center = Size / 2;
        float r = side / 2 * 0.86f;

        // foglio di carta e cornice
        var paper = new Rect2(center - new Vector2(side, side) / 2, new Vector2(side, side));
        DrawRect(paper, new Color(0.93f, 0.9f, 0.84f));
        DrawRect(paper, new Color(0.42f, 0.36f, 0.27f), false, 5);

        // tre zone: esterna (2), intermedia (5), centrale (10)
        DrawCircle(center, r, new Color(0.95f, 0.94f, 0.92f));
        DrawCircle(center, r * 2 / 3, new Color(0.18f, 0.36f, 0.54f));
        DrawCircle(center, r / 3, new Color(0.78f, 0.21f, 0.17f));
        DrawArc(center, r, 0, Mathf.Tau, 96, new Color(0.15f, 0.15f, 0.15f), 2);
        DrawArc(center, r * 2 / 3, 0, Mathf.Tau, 96, new Color(0.15f, 0.15f, 0.15f), 2);

        // colpi: la Y del bersaglio va verso l'alto, quella dello schermo verso il basso
        foreach (Vector2 p in _hits)
        {
            Vector2 pos = center + new Vector2(p.X, -p.Y) / TargetRadius * r;
            DrawCircle(pos, 7, new Color(0.11f, 0.11f, 0.11f));
            DrawCircle(pos, 5.5f, new Color(1f, 0.82f, 0.25f));
            DrawCircle(pos, 2, new Color(0.11f, 0.11f, 0.11f));
        }
    }
}
