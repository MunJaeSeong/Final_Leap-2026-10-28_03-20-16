using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageBuilder : MonoBehaviour
{
    public static Vector2 Checkpoint;
    public static bool HasCheckpoint;
    public static readonly List<MapMark> Marks = new List<MapMark>();
    public static readonly List<PortalMark> PortalMarks = new List<PortalMark>();
    public static float LevelMinX;
    public static float LevelMaxX;
    public static float LevelMinY;
    public static float LevelMaxY;
    public static bool ShowEmber;
    public static float EmberX;
    public static float EmberY;
    public static bool FallWrap;
    public static float WrapMinY;
    public static float WrapMaxY;
    public static float WrapMinX;
    public static float WrapMaxX;
    public static bool BossVoid;

    public struct MapMark
    {
        public float X;
        public float Y;
        public float Width;
        public bool Checkpoint;
        public bool Goal;
    }

    public struct PortalMark
    {
        public float X;
        public float Y;
    }

    struct Step
    {
        public int Kind;
        public float Y;
        public float Gap;
        public int Owl;
        public bool Checkpoint;
        public bool Goal;
        public bool Falling;
    }

    static readonly string[] Wide =
    {
        "s_r00_00", "s_r06_01"
    };

    static readonly string[] Medium =
    {
        "s_r01_00", "s_r01_01", "s_r01_02", "s_r01_03",
        "s_r02_00", "s_r02_01", "s_r02_02", "s_r02_03", "s_r02_04", "s_r02_05",
        "s_r05_01", "s_r05_02", "s_r05_03"
    };

    static readonly string[] Small =
    {
        "s_r00_01", "s_r00_02", "s_r00_03", "s_r00_04", "s_r01_04"
    };

    static readonly string[] Props =
    {
        "s_r03_00", "s_r03_01", "s_r03_02", "s_r03_03",
        "s_r03_04", "s_r03_05", "s_r03_07", "s_r03_08",
        "s_r04_05", "s_r04_06", "s_r04_07", "s_r04_08"
    };

    static readonly string[] Hangers =
    {
        "s_r04_00", "s_r04_01", "s_r04_02", "s_r04_03", "s_r04_04", "s_r03_06"
    };

    public static PlayerKnight Build(Transform parent, CameraFollow camera, bool useCheckpoint, RoomId room, bool arriveByPortal, RoomId fromRoom)
    {
        var steps = StepsFor(room);

        Marks.Clear();
        PortalMarks.Clear();
        ShowEmber = false;
        FallWrap = false;
        BossVoid = false;
        int medium = 0;
        int small = 0;
        float cursor = 0f;
        Vector3 spawn = Vector3.zero;
        float lastLeft = 0f;
        float lastWidth = 8f;
        float lastY = 0f;
        Vector3 goal = Vector3.zero;
        bool spawnSet = false;
        bool jumpRoom = room == RoomId.Platforms || room == RoomId.Hidden;
        var stray = FindObjectsByType<OwlEnemy>();
        for (int s = 0; s < stray.Length; s++)
        {
            if (stray[s] != null)
                Destroy(stray[s].gameObject);
        }
        var dragons = FindObjectsByType<DragonBoss>();
        for (int s = 0; s < dragons.Length; s++)
        {
            if (dragons[s] != null)
                Destroy(dragons[s].gameObject);
        }
        var root = new GameObject("Stage").transform;
        root.SetParent(parent, false);

        var spots = new List<PortalSpot>();
        float jumpMinY = -0.4f;
        float jumpMaxY = 4.4f;
        bool bossRoom = room == RoomId.Boss;
        if (jumpRoom)
            BuildJumpField(root, room, spots, out spawn, out cursor, out jumpMinY, out jumpMaxY);
        else if (bossRoom)
            BuildBossArena(root, out spawn, out cursor, out jumpMinY, out jumpMaxY);
        else
        for (int i = 0; i < steps.Length; i++)
        {
            Step step = steps[i];
            if (jumpRoom)
                step.Owl = 0;
            else
                step.Falling = false;
            string name = step.Kind == 0
                ? Wide[i % Wide.Length]
                : step.Kind == 2 ? Small[small++ % Small.Length] : Medium[medium++ % Medium.Length];
            var sprite = ArtBank.Tile(name);
            if (sprite == null)
                continue;

            float width = sprite.bounds.size.x * 0.9f;
            float left = cursor + step.Gap;
            float centerX = left + width * 0.5f;
            float surfaceFromCenter = ArtBank.SurfaceFromCenter(sprite);
            float centerY = step.Y - surfaceFromCenter;

            var platform = new GameObject(name);
            platform.layer = 6;
            platform.transform.SetParent(root, false);
            platform.transform.position = new Vector3(centerX, centerY, 0f);
            var renderer = platform.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 1;

            var box = platform.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width, 0.46f);
            box.offset = new Vector2(0f, surfaceFromCenter - 0.23f);
            var effector = platform.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 150f;
            box.usedByEffector = true;
            if (!jumpRoom && step.Y <= 0.5f)
                box.enabled = false;

            if (step.Falling)
                platform.AddComponent<CrumblePlatform>();

            if (!spawnSet)
            {
                spawn = new Vector3(left + 1.7f, step.Y + 0.15f, 0f);
                spawnSet = true;
            }

            lastLeft = left;
            lastWidth = width;
            lastY = step.Y;

            if (step.Checkpoint)
                PlaceMarker(root, new Vector3(centerX, step.Y, 0f), false);

            if (step.Goal)
            {
                goal = new Vector3(centerX + width * 0.18f, step.Y, 0f);
                PlaceMarker(root, goal, true);
            }

            Marks.Add(new MapMark
            {
                X = left,
                Y = step.Y,
                Width = width,
                Checkpoint = step.Checkpoint,
                Goal = step.Goal
            });

            if (step.Owl == 1)
                SpawnOwl(root, new Vector3(centerX, step.Y + 0.02f, 0f), false);
            else if (step.Owl == 2)
                SpawnOwl(root, new Vector3(centerX, step.Y + 2.15f, 0f), true);

            ScatterProps(root, centerX, step.Y, width, i);

            cursor = left + width;
        }

        if (!jumpRoom && room != RoomId.Boss)
            PlaceSolidFloor(root, -2f, cursor + 3f);

        var portals = new List<StagePortal>();
        PlaceRoomPortals(root, room, spawn, lastLeft, lastWidth, lastY, spots, portals);
        Vector3 stand = spawn;
        if (arriveByPortal)
        {
            for (int i = 0; i < portals.Count; i++)
            {
                if (portals[i].Target != fromRoom)
                    continue;
                stand = portals[i].Feet + Vector3.up * 0.15f;
                break;
            }
        }
        if (room == RoomId.Abyss && !WorldProgress.HasEmber)
        {
            var emberFeet = new Vector3(lastLeft + 1.2f, lastY, 0f);
            EmberPickup.Create(root, emberFeet);
            ShowEmber = true;
            EmberX = emberFeet.x;
            EmberY = emberFeet.y;
        }
        if (room == RoomId.BossGate)
            PlaceSign(root, new Vector3(lastLeft + lastWidth * 0.5f, lastY + 2.6f, 0f), "모든 준비가 완료되면\n보스가 나타난다");

        if (room != RoomId.Boss)
        {
            var wall = new GameObject("StartWall");
            wall.layer = 6;
            wall.transform.SetParent(root, false);
            wall.transform.position = new Vector3(spawn.x - 3.2f, 3f, 0f);
            var wallBox = wall.AddComponent<BoxCollider2D>();
            wallBox.size = new Vector2(1f, 14f);
        }

        if (!useCheckpoint || !HasCheckpoint)
        {
            Checkpoint = spawn;
            HasCheckpoint = true;
        }

        var player = PlayerKnight.Spawn(useCheckpoint ? (Vector3)Checkpoint : stand);
        player.transform.SetParent(parent, false);

        if (room != RoomId.Boss)
        {
            LevelMinX = spawn.x - 1.5f;
            LevelMaxX = Mathf.Max(goal.x + 3f, cursor);
            LevelMinY = jumpRoom ? jumpMinY : -0.4f;
            LevelMaxY = jumpRoom ? jumpMaxY : 4.4f;
        }

        if (camera != null)
        {
            if (room == RoomId.Boss)
                camera.Focus(player.transform, -0.2f, 0.2f, 2.4f, 2.9f, true);
            else if (jumpRoom)
                camera.Focus(player.transform, spawn.x - 1f, Mathf.Max(spawn.x + 6f, cursor - 2f), jumpMinY + 1.2f, jumpMaxY - 0.4f, true);
            else
                camera.Focus(player.transform, spawn.x + 3.5f, Mathf.Max(goal.x, cursor - 4f), -1.1f, 3.2f, false);
        }

        return player;
    }

    static Step[] StepsFor(RoomId room)
    {
        switch (room)
        {
            case RoomId.Passage:
                return Join(
                    Stretch(new[]
                    {
                        StepOf(0, 0f, 0f), StepOf(1, 0f, 1.5f, owl: 1), StepOf(2, 1.4f, 1.8f),
                        StepOf(1, 0.35f, 1.6f), StepOf(0, 0f, 1.4f, checkpoint: true, owl: 1)
                    }, 2, 1.5f),
                    Stretch(new[]
                    {
                        StepOf(1, 1.25f, 1.6f), StepOf(2, 2.1f, 1.7f, owl: 2), StepOf(1, 0f, 1.5f, owl: 1),
                        StepOf(2, 1.6f, 1.8f), StepOf(0, 0f, 1.4f, checkpoint: true)
                    }, 2, 1.6f));
            case RoomId.Plaza:
                return Join(
                    Stretch(new[]
                    {
                        StepOf(0, 0f, 0f, owl: 1), StepOf(1, 0f, 1.4f), StepOf(1, 1.2f, 1.6f, owl: 1),
                        StepOf(2, 0.4f, 1.5f), StepOf(0, 0f, 1.3f, checkpoint: true)
                    }, 2, 1.4f),
                    Stretch(new[]
                    {
                        StepOf(1, 0f, 1.5f, owl: 1), StepOf(2, 1.7f, 1.7f), StepOf(1, 0.8f, 1.5f, owl: 2),
                        StepOf(0, 0f, 1.4f, owl: 1), StepOf(0, 0f, 1.2f, checkpoint: true)
                    }, 2, 1.4f));
            case RoomId.Platforms:
                return Join(
                    Stretch(new[]
                    {
                        StepOf(0, 0f, 0f), StepOf(2, 1.45f, 2.15f), StepOf(1, 2.4f, 1.95f),
                        StepOf(2, 1.7f, 2.25f, falling: true), StepOf(1, 2.95f, 2f),
                        StepOf(2, 1.2f, 2.3f), StepOf(0, 0f, 2.5f, checkpoint: true)
                    }, 2, 2.5f),
                    Stretch(new[]
                    {
                        StepOf(1, 1.8f, 2.2f), StepOf(2, 2.7f, 1.9f), StepOf(1, 3.35f, 1.85f),
                        StepOf(2, 1.5f, 2.35f, falling: true), StepOf(1, 2.2f, 2.05f),
                        StepOf(2, 0.85f, 2.2f), StepOf(0, 0f, 2.55f, checkpoint: true)
                    }, 2, 2.6f));
            case RoomId.Hidden:
                return Join(
                    Stretch(new[]
                    {
                        StepOf(0, 0f, 0f), StepOf(2, 1.7f, 2.2f), StepOf(2, 2.85f, 1.9f),
                        StepOf(1, 1.15f, 2.3f, falling: true), StepOf(2, 2.4f, 2f),
                        StepOf(1, 3.25f, 1.85f), StepOf(2, 1.55f, 2.15f), StepOf(0, 0f, 2.5f, checkpoint: true)
                    }, 2, 2.55f),
                    Stretch(new[]
                    {
                        StepOf(1, 1.35f, 2.25f), StepOf(2, 2.55f, 2f), StepOf(1, 1.9f, 2.2f, falling: true),
                        StepOf(2, 3.15f, 1.8f), StepOf(1, 2.05f, 2.15f), StepOf(2, 0.95f, 2.3f),
                        StepOf(0, 0f, 2.6f, checkpoint: true)
                    }, 2, 2.6f));
            case RoomId.Altar:
                return Join(
                    Stretch(new[]
                    {
                        StepOf(0, 0f, 0f), StepOf(1, 1.1f, 1.7f, owl: 1), StepOf(2, 1.85f, 1.6f),
                        StepOf(1, 0.4f, 1.5f, owl: 1), StepOf(0, 0f, 1.4f, checkpoint: true)
                    }, 2, 1.5f),
                    Stretch(new[]
                    {
                        StepOf(2, 1.5f, 1.7f), StepOf(1, 2.2f, 1.6f, owl: 2), StepOf(1, 0f, 1.5f, owl: 1),
                        StepOf(2, 1.25f, 1.7f), StepOf(0, 0f, 1.4f, checkpoint: true)
                    }, 2, 1.5f));
            case RoomId.Abyss:
                return Join(
                    Stretch(new[]
                    {
                        StepOf(0, 0f, 0f, owl: 1), StepOf(2, 1.4f, 1.8f), StepOf(1, 0.85f, 1.6f, owl: 1),
                        StepOf(0, 0f, 1.4f, checkpoint: true)
                    }, 2, 1.5f),
                    Stretch(new[]
                    {
                        StepOf(1, 1.2f, 1.6f, owl: 2), StepOf(2, 2.15f, 1.7f), StepOf(1, 0.5f, 1.5f, owl: 1),
                        StepOf(2, 1.55f, 1.6f), StepOf(0, 0f, 1.4f, checkpoint: true)
                    }, 2, 1.5f));
            case RoomId.BossGate:
                return Stretch(new[]
                {
                    StepOf(0, 0f, 0f), StepOf(1, 0.9f, 1.4f), StepOf(0, 0f, 1.3f), StepOf(2, 1.35f, 1.5f)
                }, 2, 1.3f);
            case RoomId.Boss:
                return Stretch(new[]
                {
                    StepOf(0, 0f, 0f), StepOf(1, 0.7f, 1.3f), StepOf(0, 0f, 1.2f), StepOf(2, 1.2f, 1.4f)
                }, 2, 1.2f);
            default:
                return Join(
                    Stretch(new[]
                    {
                        StepOf(0, 0f, 0f), StepOf(1, 0f, 1.6f, owl: 1), StepOf(2, 1.45f, 1.8f),
                        StepOf(1, 0.3f, 1.5f), StepOf(0, 0f, 1.4f, checkpoint: true, owl: 1)
                    }, 2, 1.5f),
                    Stretch(new[]
                    {
                        StepOf(2, 1.7f, 1.7f), StepOf(1, 0f, 1.5f, owl: 1), StepOf(1, 2.05f, 1.6f, owl: 2),
                        StepOf(2, 0.9f, 1.5f), StepOf(0, 0f, 1.4f, checkpoint: true)
                    }, 2, 1.5f));
        }
    }

    static Step[] Join(params Step[][] groups)
    {
        int count = 0;
        for (int i = 0; i < groups.Length; i++)
            count += groups[i].Length;
        var all = new Step[count];
        int write = 0;
        for (int i = 0; i < groups.Length; i++)
        {
            groups[i].CopyTo(all, write);
            write += groups[i].Length;
        }
        return all;
    }

    static Step[] Stretch(Step[] pattern, int copies, float joinGap)
    {
        var list = new List<Step>(pattern.Length * copies);
        for (int c = 0; c < copies; c++)
        {
            for (int i = 0; i < pattern.Length; i++)
            {
                Step step = pattern[i];
                if (c > 0 && i == 0)
                    step.Gap = joinGap;
                list.Add(step);
            }
        }
        return list.ToArray();
    }

    static void PlaceSolidFloor(Transform root, float start, float end)
    {
        var sprite = ArtBank.Tile("s_r00_00");
        if (sprite == null)
            return;
        float x = start;
        int tile = 0;
        while (x < end)
        {
            var ground = ArtBank.Tile(Wide[tile++ % Wide.Length]);
            if (ground == null)
                ground = sprite;
            float piece = ground.bounds.size.x * 0.9f;
            float top = ArtBank.SurfaceFromCenter(ground);
            float centerX = x + piece * 0.5f;
            var floor = new GameObject("Floor");
            floor.layer = 6;
            floor.transform.SetParent(root, false);
            floor.transform.position = new Vector3(centerX, -top, 0f);
            var renderer = floor.AddComponent<SpriteRenderer>();
            renderer.sprite = ground;
            renderer.sortingOrder = 0;
            var box = floor.AddComponent<BoxCollider2D>();
            box.size = new Vector2(piece + 0.35f, 0.46f);
            box.offset = new Vector2(0f, top - 0.23f);
            floor.AddComponent<SolidFloor>();
            Marks.Add(new MapMark { X = x, Y = 0f, Width = piece, Checkpoint = false, Goal = false });
            x += piece - 0.55f;
        }
    }

    struct PortalSpot
    {
        public RoomId Target;
        public Vector3 Feet;
    }

    static void BuildJumpField(Transform root, RoomId room, List<PortalSpot> spots, out Vector3 spawn, out float cursor, out float minY, out float maxY)
    {
        spawn = Vector3.zero;
        cursor = 20f;
        minY = 0f;
        maxY = 8f;
        if (room == RoomId.Hidden)
            BuildSplitJump(root, spots, out spawn, out cursor, out minY, out maxY);
        else
            BuildSkyField(root, spots, out spawn, out cursor, out minY, out maxY);
    }

    static void BuildSkyField(Transform root, List<PortalSpot> spots, out Vector3 spawn, out float cursor, out float minY, out float maxY)
    {
        float x = -1.2f;
        while (x < 44f)
        {
            float width = PlaceIsle(root, "s_r00_00", x, 0f, false, true);
            x += Mathf.Max(4f, width - 0.45f);
        }

        spawn = new Vector3(2.4f, 0.15f, 0f);
        spots.Add(new PortalSpot { Target = RoomId.Plaza, Feet = new Vector3(6.2f, 0f, 0f) });
        PlaceMarker(root, new Vector3(3.2f, 0f, 0f), false);

        float[] cols = { 8f, 11.2f, 14.4f, 17.6f, 20.8f, 24f, 27.2f, 30.4f, 33.6f, 36.8f };
        for (int i = 0; i < cols.Length; i++)
        {
            float low = 2.05f + (i % 3) * 0.16f;
            float mid = low + 2.2f;
            float high = mid + 2.1f;
            DressIsle(root, PlaceIsle(root, Small[i % Small.Length], cols[i] - 1.1f, low, false, false), cols[i], low, i);
            DressIsle(root, 0f, cols[i] + 0.35f, mid, i + 1);
            PlaceIsle(root, Small[(i + 2) % Small.Length], cols[i] - 0.7f, mid, false, false);
            DressIsle(root, 0f, cols[i] + 0.15f, high, i + 2);
            PlaceIsle(root, Small[(i + 1) % Small.Length], cols[i] - 0.9f, high, i % 5 == 3, false);
        }

        float endX = 40.4f;
        float endY = 8.55f;
        PlaceIsle(root, Small[0], endX - 1.1f, endY, false, false);
        spots.Add(new PortalSpot { Target = RoomId.Hidden, Feet = new Vector3(endX, endY, 0f) });
        cursor = 46f;
        minY = -0.6f;
        maxY = endY + 1.6f;
        LevelMinX = -1.5f;
        LevelMaxX = cursor;
    }

    static void BuildSplitJump(Transform root, List<PortalSpot> spots, out Vector3 spawn, out float cursor, out float minY, out float maxY)
    {
        PlaceIsle(root, Medium[0], 1.2f, 4.05f, false, false);
        spawn = new Vector3(3.1f, 4.2f, 0f);
        spots.Add(new PortalSpot { Target = RoomId.Platforms, Feet = new Vector3(4.6f, 4.05f, 0f) });
        PlaceMarker(root, new Vector3(2.4f, 4.05f, 0f), false);

        float[] upperX = { 8.2f, 11.5f, 14.8f, 18.1f, 21.4f, 24.6f };
        float[] upperY = { 5.7f, 6.7f, 7.55f, 8.35f, 9.05f, 9.55f };
        float[] lowerX = { 8.2f, 11.5f, 14.8f, 18.1f, 21.4f, 24.6f };
        float[] lowerY = { 2.15f, 1.55f, 2.05f, 1.15f, 1.7f, 0.75f };
        for (int i = 0; i < upperX.Length; i++)
        {
            PlaceIsle(root, Small[i % Small.Length], upperX[i] - 1f, upperY[i], false, false);
            DressIsle(root, 0f, upperX[i], upperY[i], i);
            PlaceIsle(root, Small[(i + 1) % Small.Length], lowerX[i] - 1f, lowerY[i], false, false);
            if (i % 2 == 0)
                PlaceIsle(root, Small[(i + 3) % Small.Length], upperX[i] + 1.5f, upperY[i] + 0.35f, false, false);
            else
                PlaceIsle(root, Small[(i + 2) % Small.Length], lowerX[i] + 1.45f, lowerY[i] - 0.35f, false, false);
        }

        float topX = 28.2f;
        float topY = 10.15f;
        float botX = 28.2f;
        float botY = 0.45f;
        PlaceIsle(root, Small[1], topX - 1f, topY, false, false);
        PlaceIsle(root, Small[2], botX - 1f, botY, false, false);
        spots.Add(new PortalSpot { Target = RoomId.BossGate, Feet = new Vector3(topX, topY, 0f) });
        spots.Add(new PortalSpot { Target = RoomId.Altar, Feet = new Vector3(botX, botY, 0f) });
        cursor = 32f;
        minY = -0.8f;
        maxY = topY + 1.6f;
        LevelMinX = 0f;
        LevelMaxX = cursor;
    }

    static void DressIsle(Transform root, float unused, float x, float y, int index)
    {
        if (index % 3 == 0)
            PlaceFlame(root, new Vector3(x, y, 0f));
        else if (index % 3 == 1)
            PlaceDecor(root, index % 2 == 0 ? "s_r04_07" : "s_r04_08", new Vector3(x, y + 0.85f, 0f), 0.55f, 8, false);
    }

    static void BuildBossArena(Transform root, out Vector3 spawn, out float cursor, out float minY, out float maxY)
    {
        BossVoid = true;
        FallWrap = true;
        Color cyan = new Color(0.45f, 0.95f, 1f);
        Color gold = new Color(1f, 0.82f, 0.35f);
        Color pink = new Color(1f, 0.45f, 0.72f);
        Color purple = new Color(0.72f, 0.48f, 1f);
        Color mint = new Color(0.55f, 1f, 0.72f);

        PlaceIsle(root, "s_r01_00", -2.8f, -1.95f, false, false, cyan);
        PlaceLightWell(root, new Vector3(-1.15f, -1.95f, 0f));
        PlaceIsle(root, Small[0], 3.59f, -0.73f, false, false, purple);
        PlaceIsle(root, Small[1], -4.99f, 0.24f, false, false, gold);
        PlaceIsle(root, Small[2], 2.01f, 1.29f, false, false, pink);
        PlaceIsle(root, Small[3], -1.49f, 2.34f, false, false, cyan);
        PlaceIsle(root, Small[0], 4.11f, 3.21f, false, false, gold);
        PlaceIsle(root, Small[1], -4.46f, 4.0f, false, false, pink);
        PlaceIsle(root, Small[2], 0.61f, 5.14f, false, false, purple);
        PlaceIsle(root, "s_r01_01", -2.54f, 6.54f, false, false, mint);
        PlaceIsle(root, Small[1], 3.06f, 7.06f, false, false, cyan);

        PlaceDecor(root, "s_r03_02", new Vector3(-6.1f, 1.9f, 0f), 0.38f, 2, false);
        PlaceDecor(root, "s_r04_06", new Vector3(5.9f, 4.35f, 0f), 0.34f, 2, true);

        spawn = new Vector3(-1.15f, -1.75f, 0f);
        cursor = 8f;
        minY = -2.6f;
        maxY = 8.2f;
        WrapMinY = -5.2f;
        WrapMaxY = 9.2f;
        WrapMinX = -7.2f;
        WrapMaxX = 7.2f;
        LevelMinX = -6.4f;
        LevelMaxX = 7.2f;
        LevelMinY = -2.6f;
        LevelMaxY = 8.2f;
        DragonBoss.Spawn(root, new Vector3(0.4f, 3.2f, 0f), -5.6f, 5.8f, -0.4f, 7.6f);
    }

    static void PlaceLightWell(Transform parent, Vector3 feet)
    {
        var go = new GameObject("LightWell");
        go.transform.SetParent(parent, false);
        go.transform.position = feet;
        WellDisc(go.transform, new Vector3(0f, 0.15f, 0f), new Vector3(1.1f, 0.35f, 1f), new Color(0.75f, 0.95f, 1f, 0.9f), 4);
        WellDisc(go.transform, new Vector3(0f, 1.15f, 0f), new Vector3(0.55f, 2.2f, 1f), new Color(0.7f, 0.92f, 1f, 0.45f), 3);
        WellDisc(go.transform, new Vector3(0f, 2.4f, 0f), new Vector3(0.28f, 1.4f, 1f), new Color(0.9f, 0.98f, 1f, 0.35f), 3);
    }

    static void WellDisc(Transform parent, Vector3 local, Vector3 scale, Color color, int order)
    {
        var go = new GameObject("Well");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        go.transform.localScale = scale;
        var view = go.AddComponent<SpriteRenderer>();
        view.sprite = ArtBank.SoftCircle;
        view.color = color;
        view.sortingOrder = order;
    }

    static float PlaceIsle(Transform root, string name, float left, float surfaceY, bool falling, bool solid)
    {
        return PlaceIsle(root, name, left, surfaceY, falling, solid, Color.white);
    }

    static float PlaceIsle(Transform root, string name, float left, float surfaceY, bool falling, bool solid, Color tint)
    {
        var sprite = ArtBank.Tile(name);
        if (sprite == null)
            return 3f;
        float width = sprite.bounds.size.x * 0.9f;
        float surface = ArtBank.SurfaceFromCenter(sprite);
        var platform = new GameObject(name);
        platform.layer = 6;
        platform.transform.SetParent(root, false);
        platform.transform.position = new Vector3(left + width * 0.5f, surfaceY - surface, 0f);
        var renderer = platform.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = tint;
        renderer.sortingOrder = solid ? 0 : 1;
        var box = platform.AddComponent<BoxCollider2D>();
        box.size = new Vector2(width, 0.46f);
        box.offset = new Vector2(0f, surface - 0.23f);
        if (solid)
            platform.AddComponent<SolidFloor>();
        else
        {
            var effector = platform.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 150f;
            box.usedByEffector = true;
            if (falling)
                platform.AddComponent<CrumblePlatform>();
        }
        Marks.Add(new MapMark { X = left, Y = surfaceY, Width = width, Checkpoint = false, Goal = false });
        return width;
    }

    static void PlaceFlame(Transform parent, Vector3 feet)
    {
        var go = new GameObject("Flame");
        go.transform.SetParent(parent, false);
        go.transform.position = feet + Vector3.up * 0.15f;
        FlameBlob(go.transform, new Vector3(0f, 0.12f, 0f), new Vector3(0.42f, 0.2f, 1f), new Color(0.45f, 0.16f, 0.05f, 0.95f), 7);
        FlameBlob(go.transform, new Vector3(0f, 0.42f, 0f), new Vector3(0.28f, 0.42f, 1f), new Color(1f, 0.42f, 0.08f, 0.95f), 8);
        FlameBlob(go.transform, new Vector3(0f, 0.72f, 0f), new Vector3(0.16f, 0.28f, 1f), new Color(1f, 0.86f, 0.35f, 0.95f), 9);
    }

    static void FlameBlob(Transform parent, Vector3 local, Vector3 scale, Color color, int order)
    {
        var go = new GameObject("Fire");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        go.transform.localScale = scale;
        var view = go.AddComponent<SpriteRenderer>();
        view.sprite = ArtBank.SoftCircle;
        view.color = color;
        view.sortingOrder = order;
    }

    static void PlaceRoomPortals(Transform root, RoomId room, Vector3 spawn, float lastLeft, float lastWidth, float lastY, List<PortalSpot> spots, List<StagePortal> created)
    {
        var info = WorldCatalog.Get(room);
        int back = 0;
        int forward = 0;
        int forwardCount = 0;
        for (int i = 0; i < info.Links.Length; i++)
            if (!info.Links[i].Back)
                forwardCount++;

        for (int i = 0; i < info.Links.Length; i++)
        {
            RoomLink link = info.Links[i];
            Vector3 feet;
            int spot = -1;
            if (spots != null)
            {
                for (int s = 0; s < spots.Count; s++)
                {
                    if (spots[s].Target != link.Target)
                        continue;
                    spot = s;
                    break;
                }
            }
            if (spot >= 0)
                feet = spots[spot].Feet;
            else if (link.Back)
            {
                feet = new Vector3(spawn.x + 2.6f + back * 2.4f, spawn.y - 0.15f, 0f);
                back++;
            }
            else
            {
                float span = Mathf.Max(2.2f, (lastWidth - 2f) / Mathf.Max(1, forwardCount));
                feet = new Vector3(lastLeft + 1.2f + forward * span, lastY, 0f);
                forward++;
            }
            var portal = StagePortal.Create(root, feet, link);
            created.Add(portal);
            PortalMarks.Add(new PortalMark { X = feet.x, Y = feet.y });
        }
    }

    static void PlaceSign(Transform parent, Vector3 position, string message)
    {
        var go = new GameObject("Sign");
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var text = go.AddComponent<TextMesh>();
        text.font = GameFont.Get();
        text.text = message;
        text.characterSize = 0.09f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = new Color(0.75f, 0.88f, 1f);
        text.fontSize = 48;
        go.GetComponent<MeshRenderer>().sortingOrder = 30;
    }

    static Step StepOf(int kind, float y, float gap, int owl = 0, bool checkpoint = false, bool goal = false, bool falling = false)
    {
        return new Step
        {
            Kind = kind,
            Y = y,
            Gap = gap,
            Owl = owl,
            Checkpoint = checkpoint,
            Goal = goal,
            Falling = falling
        };
    }

    static void SpawnOwl(Transform root, Vector3 feet, bool flyer, bool boss = false)
    {
        var owl = OwlEnemy.Spawn(feet, flyer, boss);
        owl.transform.SetParent(root, true);
    }

    static void ScatterProps(Transform root, float centerX, float surfaceY, float width, int index)
    {
        int count = 2 + (index % 3);
        for (int n = 0; n < count; n++)
        {
            int pick = (index * 3 + n * 5) % (Props.Length + Hangers.Length);
            bool hang = pick >= Props.Length;
            string name = hang ? Hangers[pick - Props.Length] : Props[pick % Props.Length];
            float along = ((index * 17 + n * 29) % 100) / 100f - 0.5f;
            float x = centerX + along * width * 0.72f;
            float y = hang ? surfaceY + 2.2f + (n % 3) * 0.5f : surfaceY + 0.02f;
            float scale = hang ? 0.65f + (n % 3) * 0.16f : 0.58f + ((index + n) % 4) * 0.14f;
            PlaceDecor(root, name, new Vector3(x, y, 0f), scale, hang ? -3 : 3, ((index + n) & 1) == 0);
        }
    }

    static void PlaceDecor(Transform parent, string name, Vector3 position, float scale, int order, bool flip)
    {
        var sprite = ArtBank.Tile(name);
        if (sprite == null)
            return;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * scale;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        renderer.flipX = flip;
    }

    static void PlaceMarker(Transform parent, Vector3 feet, bool goal)
    {
        var go = new GameObject(goal ? "Goal" : "Checkpoint");
        go.transform.SetParent(parent, false);
        go.transform.position = feet + Vector3.up * 0.15f;

        var glow = new GameObject("Glow");
        glow.transform.SetParent(go.transform, false);
        glow.transform.localPosition = new Vector3(0f, goal ? 1.7f : 0.9f, 0f);
        glow.transform.localScale = goal ? new Vector3(1.4f, 3.6f, 1f) : new Vector3(0.7f, 0.7f, 1f);
        var glowRenderer = glow.AddComponent<SpriteRenderer>();
        glowRenderer.sprite = ArtBank.SoftCircle;
        glowRenderer.color = goal
            ? new Color(0.65f, 0.95f, 1f, 0.55f)
            : new Color(0.45f, 0.95f, 0.85f, 0.85f);
        glowRenderer.sortingOrder = 6;

        var triggerGo = new GameObject("Trigger");
        triggerGo.transform.SetParent(go.transform, false);
        var box = triggerGo.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = goal ? new Vector2(1.8f, 3.2f) : new Vector2(1.1f, 1.8f);
        box.offset = new Vector2(0f, 1.1f);
        if (goal)
            triggerGo.AddComponent<GoalMarker>();
        else
            triggerGo.AddComponent<CheckpointMarker>();
    }
}

public class SolidFloor : MonoBehaviour
{
}

public class CrumblePlatform : MonoBehaviour
{
    bool started;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (started || collision.collider.GetComponent<PlayerKnight>() == null)
            return;
        started = true;
        StartCoroutine(Fall());
    }

    IEnumerator Fall()
    {
        var renderer = GetComponent<SpriteRenderer>();
        Vector3 origin = transform.position;
        float time = 0f;
        while (time < 2f)
        {
            time += Time.deltaTime;
            float k = time / 2f;
            transform.position = origin + Vector3.right * Mathf.Sin(time * 28f) * (0.035f + 0.09f * k);
            if (renderer != null)
                renderer.color = Color.Lerp(Color.white, new Color(1f, 0.62f, 0.55f), k);
            yield return null;
        }

        var box = GetComponent<Collider2D>();
        if (box != null)
            box.enabled = false;

        while (transform.position.y > -12f)
        {
            transform.position += Vector3.down * 10f * Time.deltaTime;
            yield return null;
        }

        gameObject.SetActive(false);
    }
}

public class CheckpointMarker : MonoBehaviour
{
    bool used;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used || other.GetComponent<PlayerKnight>() == null)
            return;
        used = true;
        StageBuilder.Checkpoint = transform.parent.position;
        StageBuilder.HasCheckpoint = true;
        AudioFx.Play("checkpoint");
        var glow = transform.parent.Find("Glow");
        if (glow != null)
        {
            var renderer = glow.GetComponent<SpriteRenderer>();
            if (renderer != null)
                renderer.color = new Color(0.8f, 1f, 0.75f, 0.95f);
        }
    }
}

public class GoalMarker : MonoBehaviour
{
    bool used;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used || other.GetComponent<PlayerKnight>() == null)
            return;
        var player = other.GetComponent<PlayerKnight>();
        if (player.Dead)
            return;
        used = true;
        GameRoot.Instance?.ClearStage();
    }
}
