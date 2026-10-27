using System.Collections.Generic;
using UnityEngine;

public static class ArtBank
{
    public const float Ppu = 80f;

    static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
    static Sprite circle;
    static Sprite white;

    public static readonly string[] KnightIdle =
    {
        "k_r00_01", "k_r00_02", "k_r00_03", "k_r00_04", "k_r00_05", "k_r00_06", "k_r00_07"
    };

    public static readonly string[] KnightRun =
    {
        "k_r02_01", "k_r02_02", "k_r02_03", "k_r02_04", "k_r02_05",
        "k_r02_06", "k_r02_07", "k_r02_08", "k_r02_09", "k_r02_10"
    };

    public static readonly string[] KnightJump =
    {
        "k_r03_00", "k_r03_01", "k_r03_02", "k_r03_03", "k_r04_00"
    };

    public static readonly string[] KnightLand =
    {
        "k_r04_01", "k_r04_02", "k_r04_03", "k_r04_04", "k_r04_05", "k_r04_06"
    };

    public static readonly string[] KnightAttack =
    {
        "k_r05_03", "k_r05_04", "k_r05_05", "k_r05_06"
    };

    public static readonly string[] KnightHit =
    {
        "k_r05_08", "k_r05_09", "k_r05_10", "k_r05_11"
    };

    public static readonly string[] KnightDie =
    {
        "k_r06_00", "k_r06_01", "k_r06_02", "k_r06_03", "k_r06_04"
    };

    public static readonly string[] OwlIdle =
    {
        "s_r00_00", "s_r00_01", "s_r00_02", "s_r00_03", "s_r00_04",
        "s_r00_05", "s_r00_06", "s_r00_07", "s_r00_08"
    };

    public static readonly string[] OwlHop =
    {
        "s_r01_00", "s_r01_01", "s_r01_02", "s_r01_03", "s_r01_04",
        "s_r01_05", "s_r01_06", "s_r01_07", "s_r01_08", "s_r01_09"
    };

    public static readonly string[] OwlFly =
    {
        "s_r02_00", "s_r02_01", "s_r02_02", "s_r02_03", "s_r02_04",
        "s_r02_05", "s_r02_06", "s_r02_07", "s_r02_08"
    };

    public static readonly string[] OwlAttack =
    {
        "s_r03_00", "s_r03_01", "s_r03_02", "s_r03_03", "s_r03_05", "s_r03_06", "s_r03_07"
    };

    public static readonly string[] OwlHit =
    {
        "s_r04_00", "s_r04_01", "s_r04_02"
    };

    public static readonly string[] OwlDie =
    {
        "s_r04_05", "s_r04_06", "s_r04_07"
    };

    public static Sprite Title => LoadRaw("Art/title");
    public static Sprite Background => LoadRaw("Art/background");

    public static Sprite Knight(string name)
    {
        return LoadFeet("Art/Knight/" + name);
    }

    public static Sprite Owl(string name)
    {
        return LoadFeet("Art/Owl/" + name);
    }

    public static readonly string[] DragonFly =
    {
        "d_fly_00", "d_fly_01", "d_fly_02", "d_fly_03", "d_fly_04",
        "d_fly_05", "d_fly_06", "d_fly_07", "d_fly_08"
    };

    public static readonly string[] DragonDash =
    {
        "d_dash_00", "d_dash_01", "d_dash_02", "d_dash_03",
        "d_dash_04", "d_dash_05", "d_dash_06", "d_dash_07"
    };

    public static readonly string[] DragonRoar =
    {
        "d_roar_00", "d_roar_01", "d_roar_02", "d_roar_03",
        "d_roar_04", "d_roar_05", "d_roar_06", "d_roar_07"
    };

    public static readonly string[] DragonBreath =
    {
        "d_breath_00", "d_breath_01", "d_breath_02",
        "d_breath_03", "d_breath_04", "d_breath_05"
    };

    public static Sprite Dragon(string name)
    {
        return LoadPivot("Art/Dragon/" + name, 80f, new Vector2(0.5f, 0.5f));
    }

    public static Sprite[] Dragons(string[] names)
    {
        var sprites = new Sprite[names.Length];
        for (int i = 0; i < names.Length; i++)
            sprites[i] = Dragon(names[i]);
        return sprites;
    }

    public static Sprite Tile(string name)
    {
        return LoadRaw("Art/Tiles/" + name);
    }

    public static Sprite[] Knights(string[] names)
    {
        var sprites = new Sprite[names.Length];
        for (int i = 0; i < names.Length; i++)
            sprites[i] = Knight(names[i]);
        return sprites;
    }

    public static Sprite[] Owls(string[] names)
    {
        var sprites = new Sprite[names.Length];
        for (int i = 0; i < names.Length; i++)
            sprites[i] = Owl(names[i]);
        return sprites;
    }

    public static Sprite White
    {
        get
        {
            if (white != null)
                return white;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            white = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), Ppu);
            return white;
        }
    }

    public static Sprite SoftCircle
    {
        get
        {
            if (circle != null)
                return circle;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var center = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                float a = Mathf.Clamp01(1f - d);
                a = a * a;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), Ppu);
            return circle;
        }
    }

    public static float SurfaceFromCenter(Sprite sprite)
    {
        if (sprite == null)
            return 0.4f;
        try
        {
            return SurfaceFromCenterUnchecked(sprite);
        }
        catch (System.Exception)
        {
            return sprite.bounds.extents.y - 0.35f;
        }
    }

    static float SurfaceFromCenterUnchecked(Sprite sprite)
    {
        var tex = sprite.texture;
        if (tex == null || !tex.isReadable)
            return sprite.bounds.extents.y - 0.35f;

        var pixels = tex.GetPixels32();
        int w = tex.width;
        int h = tex.height;
        int need = Mathf.Max(8, (int)(w * 0.42f));
        int surface = h - 1;
        for (int y = h - 1; y >= 0; y--)
        {
            int count = 0;
            int row = y * w;
            for (int x = 0; x < w; x++)
            {
                if (pixels[row + x].a > 80)
                    count++;
            }
            if (count >= need)
            {
                surface = y;
                break;
            }
        }

        return (surface - h * 0.5f) / Ppu;
    }

    static Sprite LoadRaw(string path)
    {
        if (Cache.TryGetValue(path, out var cached))
            return cached;
        var sprite = Resources.Load<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning("스프라이트를 찾지 못했습니다: " + path);
        Cache[path] = sprite;
        return sprite;
    }

    static Sprite LoadPivot(string path, float ppu, Vector2 pivot)
    {
        string key = path + "#p" + ppu;
        if (Cache.TryGetValue(key, out var cached))
            return cached;
        var tex = Resources.Load<Texture2D>(path);
        if (tex == null)
        {
            Cache[key] = null;
            return null;
        }
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, ppu, 0, SpriteMeshType.FullRect);
        sprite.name = tex.name;
        Cache[key] = sprite;
        return sprite;
    }

    static Sprite LoadFeet(string path)
    {
        string key = path + "#feet";
        if (Cache.TryGetValue(key, out var cached))
            return cached;

        var tex = Resources.Load<Texture2D>(path);
        if (tex == null)
        {
            Debug.LogWarning("텍스처를 찾지 못했습니다: " + path);
            Cache[key] = null;
            return null;
        }

        Vector2 pivot;
        try
        {
            pivot = FeetPivot(tex);
        }
        catch (System.Exception)
        {
            pivot = new Vector2(0.5f, 0.02f);
        }
        var sprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            pivot,
            Ppu,
            0,
            SpriteMeshType.FullRect);
        sprite.name = tex.name;
        Cache[key] = sprite;
        return sprite;
    }

    static Vector2 FeetPivot(Texture2D tex)
    {
        var pixels = tex.GetPixels32();
        int w = tex.width;
        int h = tex.height;
        int minY = h;
        for (int y = 0; y < h; y++)
        {
            int row = y * w;
            for (int x = 0; x < w; x++)
            {
                if (pixels[row + x].a > 28)
                {
                    minY = y;
                    y = h;
                    break;
                }
            }
        }

        if (minY >= h)
            return new Vector2(0.5f, 0f);

        int band = Mathf.Max(3, (int)(h * 0.1f));
        long sumX = 0;
        int count = 0;
        int yMax = Mathf.Min(h - 1, minY + band);
        for (int y = minY; y <= yMax; y++)
        {
            int row = y * w;
            for (int x = 0; x < w; x++)
            {
                if (pixels[row + x].a <= 28)
                    continue;
                sumX += x;
                count++;
            }
        }

        float px = count > 0 ? sumX / (float)count : w * 0.5f;
        return new Vector2(px / w, minY / (float)h);
    }
}
