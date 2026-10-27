using UnityEngine;

public static class GameFont
{
    static Font cached;

    public static Font Get()
    {
        if (cached != null)
            return cached;

        cached = Resources.Load<Font>("Fonts/NanumGothic");
        if (cached == null)
            cached = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Gulim", "Arial" }, 64);
        if (cached == null)
            cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return cached;
    }
}
