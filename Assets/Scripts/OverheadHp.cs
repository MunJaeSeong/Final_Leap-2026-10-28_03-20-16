using UnityEngine;

public class OverheadHp : MonoBehaviour
{
    const float Width = 0.92f;
    Transform fill;
    int maxHp = 1;

    public static OverheadHp Attach(Transform host, int hp, float headLocalY)
    {
        var root = new GameObject("HpBar");
        root.transform.SetParent(host, false);
        float parentScale = Mathf.Max(0.01f, host.lossyScale.x);
        root.transform.localPosition = new Vector3(0f, headLocalY, 0f);
        root.transform.localScale = Vector3.one / parentScale;
        var bar = root.AddComponent<OverheadHp>();
        bar.maxHp = Mathf.Max(1, hp);
        bar.Piece(new Color(0.04f, 0.05f, 0.07f, 0.92f), Width + 0.06f, 0.12f, 0f);
        bar.fill = bar.Piece(new Color(0.92f, 0.28f, 0.24f, 1f), Width, 0.07f, 0f);
        bar.Set(hp);
        return bar;
    }

    public void Set(int hp)
    {
        if (fill == null)
            return;
        float ratio = Mathf.Clamp01(hp / (float)maxHp);
        var renderer = fill.GetComponent<SpriteRenderer>();
        Vector2 unit = renderer.sprite.bounds.size;
        float width = Width * ratio;
        float height = fill.localScale.y * unit.y;
        fill.localScale = new Vector3(Mathf.Max(0.001f, width) / unit.x, height / unit.y, 1f);
        fill.localPosition = new Vector3((width - Width) * 0.5f, 0f, 0f);
    }

    Transform Piece(Color color, float width, float height, float x)
    {
        var piece = new GameObject("Bar");
        piece.transform.SetParent(transform, false);
        var renderer = piece.AddComponent<SpriteRenderer>();
        renderer.sprite = ArtBank.White;
        renderer.color = color;
        renderer.sortingOrder = 40;
        Vector2 unit = renderer.sprite.bounds.size;
        piece.transform.localScale = new Vector3(width / unit.x, height / unit.y, 1f);
        piece.transform.localPosition = new Vector3(x, 0f, 0f);
        return piece.transform;
    }
}
