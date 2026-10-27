using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.Video;

public class GameRoot : MonoBehaviour
{
    public static GameRoot Instance { get; private set; }
    public bool Paused => state == Flow.Pause;

    enum Flow { Title, Video, Play, Pause, Over, Clear }

    Flow state = Flow.Title;
    Font font;
    Canvas canvas;
    CameraFollow follow;
    Transform world;
    PlayerKnight player;
    VideoPlayer video;
    RenderTexture videoTexture;
    RawImage videoImage;
    float videoSkipLock;

    GameObject titlePanel;
    GameObject videoPanel;
    GameObject hud;
    GameObject pausePanel;
    GameObject overPanel;
    GameObject clearPanel;
    GameObject banner;
    Text bannerText;
    GameObject phaseLine;
    Text phaseLineText;
    Coroutine phaseRoutine;
    GameObject story;
    RectTransform storyGroup;
    Text storyMain;
    Text storyGlow;
    Image storyLight;
    Coroutine storyRoutine;
    Image hpFill;
    Image dashFill;
    Image potionFill;
    Image riseFill;
    Text riseLabel;
    Text hpText;
    Text dashLabel;
    Text potionLabel;
    Text overReason;
    Text clearBody;
    Text locationText;
    GameObject bossBar;
    Image bossFill;
    Text bossHpText;
    Text bossPhaseText;
    RectTransform mapContent;
    RectTransform playerBlip;
    float mapBlipY;
    bool mapBlipReady;
    readonly System.Collections.Generic.List<GameObject> mapMarks = new System.Collections.Generic.List<GameObject>();
    RoomId currentRoom = RoomId.Entrance;
    GameObject worldMap;
    Text worldMapStatus;
    Image[] roomNodes;
    RectTransform youPin;
    RectTransform youLabel;
    bool mapOpen;
    public bool MapOpen => mapOpen;
    bool arriveByPortal;
    RoomId arriveFrom;
    public bool PortalHold { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindAnyObjectByType<GameRoot>() != null)
            return;
        var go = new GameObject("GameRoot");
        go.AddComponent<GameRoot>();
    }

    void Awake()
    {
        Instance = this;
        Physics2D.gravity = new Vector2(0f, -36f);
        font = GameFont.Get();

        SetupCamera();
        gameObject.AddComponent<AudioFx>();
        BuildCanvas();
        ShowTitle();
    }

    void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }

        follow = cam.GetComponent<CameraFollow>();
        if (follow == null)
            follow = cam.gameObject.AddComponent<CameraFollow>();

        var extra = cam.GetUniversalAdditionalCameraData();
        extra.renderPostProcessing = false;
        foreach (var volume in FindObjectsByType<Volume>())
            volume.enabled = false;

        var backdrop = new GameObject("CaveBackdrop");
        var renderer = backdrop.AddComponent<SpriteRenderer>();
        renderer.sprite = ArtBank.Background;
        renderer.sortingOrder = -100;
        renderer.color = new Color(0.62f, 0.78f, 0.88f, 1f);
        backdrop.AddComponent<BackdropFollow>().Cam = cam;
    }

    void BuildCanvas()
    {
        var canvasGo = new GameObject("Canvas");
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var events = new GameObject("EventSystem");
            events.AddComponent<EventSystem>();
            var module = events.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        titlePanel = BuildTitle();
        videoPanel = BuildVideo();
        hud = BuildHud();
        worldMap = BuildWorldMap();
        pausePanel = BuildSimplePanel("일시정지", "동굴의 숨이 멎었다.", out _, "이어하기", Resume, "타이틀로", GoTitle);
        overPanel = BuildSimplePanel("빛이 꺼졌다", "", out overReason, "다시 도전", Retry, "타이틀로", GoTitle);
        clearPanel = BuildSimplePanel("어둠을 넘었다", "", out clearBody, "다시 하기", RetryFromStart, "타이틀로", GoTitle);
        banner = new GameObject("Banner");
        banner.transform.SetParent(canvas.transform, false);
        var bannerRect = banner.AddComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.5f, 0.72f);
        bannerRect.anchorMax = new Vector2(0.5f, 0.72f);
        bannerRect.sizeDelta = new Vector2(1100f, 80f);
        bannerText = Label(banner.transform, 34, TextAnchor.MiddleCenter, new Color(0.85f, 0.96f, 1f));
        banner.SetActive(false);
        BuildPhaseLine();
        BuildStoryLine();
    }

    void BuildPhaseLine()
    {
        phaseLine = new GameObject("PhaseLine");
        phaseLine.transform.SetParent(canvas.transform, false);
        var root = phaseLine.AddComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(1500f, 160f);

        phaseLineText = Label(phaseLine.transform, 54, TextAnchor.MiddleCenter, new Color(1f, 0.16f, 0.12f));
        phaseLineText.fontStyle = FontStyle.Bold;
        Stretch(phaseLineText.rectTransform);
        var outline = phaseLineText.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.35f, 0.02f, 0.02f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);
        phaseLine.SetActive(false);
    }

    void BuildStoryLine()
    {
        story = new GameObject("StoryLine");
        story.transform.SetParent(canvas.transform, false);
        var root = story.AddComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(1560f, 280f);

        storyLight = MakeImage(story.transform, ArtBank.SoftCircle, new Color(0.25f, 0.65f, 1f, 0.35f));
        storyLight.raycastTarget = false;
        var lightRect = storyLight.rectTransform;
        lightRect.anchorMin = new Vector2(0.5f, 0.5f);
        lightRect.anchorMax = new Vector2(0.5f, 0.5f);
        lightRect.sizeDelta = new Vector2(1500f, 220f);

        storyGroup = new GameObject("Words").AddComponent<RectTransform>();
        storyGroup.SetParent(story.transform, false);
        storyGroup.anchorMin = Vector2.zero;
        storyGroup.anchorMax = Vector2.one;
        storyGroup.offsetMin = Vector2.zero;
        storyGroup.offsetMax = Vector2.zero;

        storyGlow = Label(storyGroup, 62, TextAnchor.MiddleCenter, new Color(0.15f, 0.45f, 1f, 0.9f));
        storyGlow.fontStyle = FontStyle.Bold;
        Stretch(storyGlow.rectTransform);

        storyMain = Label(storyGroup, 58, TextAnchor.MiddleCenter, new Color(0.75f, 0.95f, 1f));
        storyMain.fontStyle = FontStyle.Bold;
        Stretch(storyMain.rectTransform);
        var outline = storyMain.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.35f, 0.75f, 1f, 1f);
        outline.effectDistance = new Vector2(2.5f, -2.5f);
        story.SetActive(false);
    }

    GameObject BuildTitle()
    {
        var panel = FullPanel("Title");
        var art = MakeImage(panel.transform, ArtBank.Title, Color.white);
        art.raycastTarget = false;
        Stretch(art.rectTransform);
        var fit = art.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = 1f;

        var shade = MakeImage(panel.transform, ArtBank.White, new Color(0.01f, 0.04f, 0.07f, 0.55f));
        shade.raycastTarget = false;
        var shadeRect = shade.rectTransform;
        shadeRect.anchorMin = new Vector2(0f, 0f);
        shadeRect.anchorMax = new Vector2(1f, 0.42f);
        shadeRect.offsetMin = Vector2.zero;
        shadeRect.offsetMax = Vector2.zero;

        var title = Label(panel.transform, 72, TextAnchor.MiddleCenter, new Color(0.9f, 0.97f, 1f));
        title.text = "마지막 도약";
        var titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, -230f);
        titleRect.sizeDelta = new Vector2(900f, 90f);

        var sub = Label(panel.transform, 28, TextAnchor.MiddleCenter, new Color(0.7f, 0.88f, 0.95f));
        sub.text = "Final Leap    ·    끝이 사라지기 전에, 앞으로";
        var subRect = sub.rectTransform;
        subRect.anchorMin = new Vector2(0.5f, 0.5f);
        subRect.anchorMax = new Vector2(0.5f, 0.5f);
        subRect.anchoredPosition = new Vector2(0f, -290f);
        subRect.sizeDelta = new Vector2(1100f, 40f);

        Button(panel.transform, "게임 시작", new Vector2(0f, -420f), BeginFromTitle);
        Button(panel.transform, "게임 종료", new Vector2(0f, -505f), QuitGame);

        var hint = Label(panel.transform, 22, TextAnchor.MiddleCenter, new Color(0.8f, 0.9f, 0.95f, 0.9f));
        hint.text = "A D 이동    Space 점프    ↑ 포탈    M 전체맵";
        var hintRect = hint.rectTransform;
        hintRect.anchorMin = new Vector2(0.5f, 0.5f);
        hintRect.anchorMax = new Vector2(0.5f, 0.5f);
        hintRect.anchoredPosition = new Vector2(0f, -338f);
        hintRect.sizeDelta = new Vector2(1500f, 40f);
        return panel;
    }

    GameObject BuildVideo()
    {
        var panel = FullPanel("Video");
        MakeImage(panel.transform, ArtBank.White, Color.black);
        var shown = ImageRaw(panel.transform);
        Stretch(shown.rectTransform);
        shown.rectTransform.offsetMin = new Vector2(80f, 80f);
        shown.rectTransform.offsetMax = new Vector2(-80f, -80f);
        videoImage = shown;
        var fit = shown.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 16f / 9f;

        var skip = Label(panel.transform, 24, TextAnchor.MiddleCenter, new Color(0.85f, 0.93f, 1f, 0.85f));
        skip.text = "Space 또는 클릭으로 건너뛰기";
        var skipRect = skip.rectTransform;
        skipRect.anchorMin = new Vector2(0.5f, 0.04f);
        skipRect.anchorMax = new Vector2(0.5f, 0.04f);
        skipRect.sizeDelta = new Vector2(700f, 40f);
        panel.SetActive(false);
        return panel;
    }

    GameObject BuildHud()
    {
        var panel = FullPanel("Hud");

        locationText = Label(panel.transform, 24, TextAnchor.UpperLeft, new Color(0.86f, 0.96f, 1f));
        locationText.text = "현재 위치\n제1구역  안개의 동굴";
        var locationRect = locationText.rectTransform;
        locationRect.anchorMin = new Vector2(0.02f, 0.9f);
        locationRect.anchorMax = new Vector2(0.24f, 0.985f);
        locationRect.offsetMin = Vector2.zero;
        locationRect.offsetMax = Vector2.zero;

        var controls = Label(panel.transform, 22, TextAnchor.MiddleLeft, new Color(0.9f, 0.96f, 1f, 0.92f));
        controls.text = "A D 이동    Space 점프    Z 공격    X 대시    V 상승베기    C 포션    ↓+Space 통과    ↑ 포탈    M 맵    Esc 정지";
        var controlsRect = controls.rectTransform;
        controlsRect.anchorMin = new Vector2(0.02f, 0.845f);
        controlsRect.anchorMax = new Vector2(0.7f, 0.895f);
        controlsRect.offsetMin = Vector2.zero;
        controlsRect.offsetMax = Vector2.zero;

        BuildMinimap(panel.transform);

        var back = MakeImage(panel.transform, ArtBank.White, new Color(0.02f, 0.05f, 0.08f, 0.72f));
        back.raycastTarget = false;
        SetBar(back.rectTransform, 0.02f, 0.02f, 0.72f, 0.13f);

        hpText = Label(panel.transform, 22, TextAnchor.MiddleLeft, Color.white);
        var hpRect = hpText.rectTransform;
        hpRect.anchorMin = new Vector2(0.035f, 0.085f);
        hpRect.anchorMax = new Vector2(0.32f, 0.12f);
        hpRect.offsetMin = Vector2.zero;
        hpRect.offsetMax = Vector2.zero;
        hpText.text = "체력  1000 / 1000";
        var hpTextMax = hpRect.anchorMax;
        hpRect.anchorMax = new Vector2(0.24f, hpTextMax.y);

        var barBg = MakeImage(panel.transform, ArtBank.White, new Color(0.08f, 0.12f, 0.16f, 0.95f));
        SetBar(barBg.rectTransform, 0.035f, 0.04f, 0.32f, 0.075f);
        hpFill = MakeImage(panel.transform, ArtBank.White, new Color(0.45f, 0.9f, 0.85f));
        hpFill.type = UnityEngine.UI.Image.Type.Filled;
        hpFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        SetBar(hpFill.rectTransform, 0.035f, 0.04f, 0.32f, 0.075f);

        dashFill = MakeCooldownBar(panel.transform, "대시", 0.34f, 0.46f, new Color(0.45f, 0.78f, 1f), out dashLabel);
        potionFill = MakeCooldownBar(panel.transform, "포션", 0.47f, 0.59f, new Color(0.45f, 0.95f, 0.7f), out potionLabel);
        riseFill = MakeCooldownBar(panel.transform, "상승", 0.60f, 0.72f, new Color(0.72f, 0.78f, 1f), out riseLabel);
        BuildBossBar(panel.transform);

        panel.SetActive(false);
        return panel;
    }

    void BuildMinimap(Transform parent)
    {
        var frame = MakeImage(parent, ArtBank.White, new Color(0.02f, 0.06f, 0.09f, 0.82f));
        frame.raycastTarget = false;
        var frameRect = frame.rectTransform;
        frameRect.anchorMin = new Vector2(1f, 1f);
        frameRect.anchorMax = new Vector2(1f, 1f);
        frameRect.pivot = new Vector2(1f, 1f);
        frameRect.anchoredPosition = new Vector2(-28f, -22f);
        frameRect.sizeDelta = new Vector2(360f, 112f);

        var caption = Label(frame.transform, 20, TextAnchor.UpperLeft, new Color(0.75f, 0.92f, 1f));
        caption.text = "구역";
        var captionRect = caption.rectTransform;
        captionRect.anchorMin = new Vector2(0f, 1f);
        captionRect.anchorMax = new Vector2(1f, 1f);
        captionRect.pivot = new Vector2(0f, 1f);
        captionRect.anchoredPosition = new Vector2(12f, -6f);
        captionRect.sizeDelta = new Vector2(120f, 28f);

        var content = new GameObject("MapContent");
        content.transform.SetParent(frame.transform, false);
        mapContent = content.AddComponent<RectTransform>();
        mapContent.anchorMin = new Vector2(0f, 0f);
        mapContent.anchorMax = new Vector2(1f, 1f);
        mapContent.offsetMin = new Vector2(10f, 8f);
        mapContent.offsetMax = new Vector2(-10f, -28f);

        playerBlip = MapIcon(mapContent, new Color(0.95f, 0.98f, 1f), new Vector2(10f, 10f));
    }

    void RebuildMinimap()
    {
        for (int i = 0; i < mapMarks.Count; i++)
        {
            if (mapMarks[i] != null)
                Destroy(mapMarks[i]);
        }
        mapMarks.Clear();
        if (mapContent == null)
            return;

        float spanX = Mathf.Max(1f, StageBuilder.LevelMaxX - StageBuilder.LevelMinX);
        float spanY = Mathf.Max(1f, StageBuilder.LevelMaxY - StageBuilder.LevelMinY);
        var marks = StageBuilder.Marks;
        for (int i = 0; i < marks.Count; i++)
        {
            StageBuilder.MapMark mark = marks[i];
            Vector2 start = MapPoint(mark.X, mark.Y, spanX, spanY);
            Vector2 end = MapPoint(mark.X + mark.Width, mark.Y, spanX, spanY);
            float width = Mathf.Max(8f, end.x - start.x);
            var bar = MapIcon(mapContent, new Color(0.35f, 0.72f, 0.78f, 0.95f), new Vector2(width, 5f));
            bar.anchoredPosition = new Vector2(start.x + width * 0.5f, start.y);
            mapMarks.Add(bar.gameObject);

            if (mark.Checkpoint)
                mapMarks.Add(PlaceMapDot(new Vector2(start.x + width * 0.5f, start.y + 8f), new Color(0.55f, 1f, 0.75f), 8f));
            if (mark.Goal)
                mapMarks.Add(PlaceMapDot(end, new Color(1f, 0.92f, 0.55f), 11f));
        }

        var portals = StageBuilder.PortalMarks;
        for (int i = 0; i < portals.Count; i++)
        {
            Vector2 point = MapPoint(portals[i].X, portals[i].Y, spanX, spanY);
            mapMarks.Add(PlaceMapDot(point, new Color(1f, 0.18f, 0.22f), 12f));
        }

        if (StageBuilder.ShowEmber)
        {
            Vector2 ember = MapPoint(StageBuilder.EmberX, StageBuilder.EmberY, spanX, spanY);
            mapMarks.Add(PlaceMapDot(ember, new Color(1f, 0.55f, 0.15f), 12f));
        }

        playerBlip.SetAsLastSibling();
        mapBlipReady = false;
    }

    GameObject PlaceMapDot(Vector2 point, Color color, float size)
    {
        var image = MakeImage(mapContent, ArtBank.SoftCircle, color);
        image.raycastTarget = false;
        var dot = image.rectTransform;
        dot.anchorMin = Vector2.zero;
        dot.anchorMax = Vector2.zero;
        dot.pivot = new Vector2(0.5f, 0.5f);
        dot.sizeDelta = new Vector2(size, size);
        dot.anchoredPosition = point;
        return dot.gameObject;
    }

    RectTransform MapIcon(RectTransform parent, Color color, Vector2 size)
    {
        var image = MakeImage(parent, ArtBank.White, color);
        image.raycastTarget = false;
        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        return rect;
    }

    Vector2 MapPoint(float x, float y, float spanX, float spanY)
    {
        float nx = (x - StageBuilder.LevelMinX) / spanX;
        float ny = (y - StageBuilder.LevelMinY) / spanY;
        float width = mapContent.rect.width;
        float height = mapContent.rect.height;
        if (width < 10f)
            width = 340f;
        if (height < 10f)
            height = 76f;
        return new Vector2(Mathf.Clamp01(nx) * width, Mathf.Clamp01(ny) * height);
    }

    void RefreshMinimap()
    {
        if (player == null || playerBlip == null || mapContent == null)
            return;
        float spanX = Mathf.Max(1f, StageBuilder.LevelMaxX - StageBuilder.LevelMinX);
        float spanY = Mathf.Max(1f, StageBuilder.LevelMaxY - StageBuilder.LevelMinY);
        Vector2 point = MapPoint(player.transform.position.x, player.transform.position.y, spanX, spanY);
        if (!mapBlipReady)
        {
            mapBlipY = point.y;
            mapBlipReady = true;
        }
        mapBlipY = Mathf.Lerp(mapBlipY, point.y, 1f - Mathf.Exp(-Time.deltaTime * 2.2f));
        playerBlip.anchoredPosition = new Vector2(point.x, mapBlipY);
    }

    GameObject BuildSimplePanel(string title, string body, out Text bodyText, string aLabel, UnityEngine.Events.UnityAction a, string bLabel, UnityEngine.Events.UnityAction b)
    {
        var panel = FullPanel(title);
        MakeImage(panel.transform, ArtBank.White, new Color(0.01f, 0.03f, 0.06f, 0.72f));
        var card = MakeImage(panel.transform, ArtBank.White, new Color(0.04f, 0.1f, 0.14f, 0.94f));
        var cardRect = card.rectTransform;
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(760f, 460f);

        var heading = Label(card.transform, 48, TextAnchor.MiddleCenter, new Color(0.88f, 0.97f, 1f));
        heading.text = title;
        var headingRect = heading.rectTransform;
        headingRect.anchorMin = new Vector2(0.5f, 0.78f);
        headingRect.anchorMax = new Vector2(0.5f, 0.78f);
        headingRect.sizeDelta = new Vector2(680f, 70f);

        bodyText = Label(card.transform, 26, TextAnchor.MiddleCenter, new Color(0.78f, 0.9f, 0.95f));
        bodyText.text = body;
        var bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0.5f, 0.52f);
        bodyRect.anchorMax = new Vector2(0.5f, 0.52f);
        bodyRect.sizeDelta = new Vector2(640f, 120f);

        var buttonA = Button(card.transform, aLabel, new Vector2(0f, -40f), a);
        var buttonB = Button(card.transform, bLabel, new Vector2(0f, -130f), b);
        buttonA.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -40f);
        buttonB.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -130f);
        panel.SetActive(false);
        return panel;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (state == Flow.Title && keyboard != null && keyboard.enterKey.wasPressedThisFrame)
            BeginFromTitle();

        if (state == Flow.Video)
        {
            videoSkipLock -= Time.unscaledDeltaTime;
            bool skip = videoSkipLock <= 0f && keyboard != null &&
                        (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame);
            bool click = videoSkipLock <= 0f && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (skip || click)
                FinishVideo();
        }

        if (state == Flow.Play && storyRoutine == null && keyboard != null && keyboard.mKey.wasPressedThisFrame)
            ToggleWorldMap();

        if (PortalHold && keyboard != null && !keyboard.upArrowKey.isPressed)
            PortalHold = false;

        if (state == Flow.Play && storyRoutine == null && !mapOpen && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            Pause();
        else if (mapOpen && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            CloseWorldMap();
        else if (state == Flow.Pause && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            Resume();

        if (state == Flow.Play && player != null)
            RefreshHud();
    }

    void ShowTitle()
    {
        state = Flow.Title;
        Time.timeScale = 1f;
        if (storyRoutine != null)
            StopCoroutine(storyRoutine);
        storyRoutine = null;
        if (story != null)
            story.SetActive(false);
        ClearWorld();
        titlePanel.SetActive(true);
        videoPanel.SetActive(false);
        hud.SetActive(false);
        pausePanel.SetActive(false);
        overPanel.SetActive(false);
        clearPanel.SetActive(false);
        banner.SetActive(false);
        HidePhaseLine();
        if (worldMap != null)
            worldMap.SetActive(false);
        mapOpen = false;
        if (follow != null)
            follow.Target = null;
    }

    void BeginFromTitle()
    {
        if (state != Flow.Title)
            return;
        AudioFx.Play("click");
        titlePanel.SetActive(false);
        PlayIntro();
    }

    void PlayIntro()
    {
        state = Flow.Video;
        videoSkipLock = 0.45f;
        videoPanel.SetActive(true);
        bool web = Application.platform == RuntimePlatform.WebGLPlayer;
        string path = web
            ? Application.streamingAssetsPath + "/intro.mp4"
            : Path.Combine(Application.streamingAssetsPath, "intro.mp4");
        if (!web && !File.Exists(path))
        {
            FinishVideo();
            return;
        }

        if (video == null)
        {
            var go = new GameObject("IntroVideo");
            go.transform.SetParent(transform, false);
            video = go.AddComponent<VideoPlayer>();
            var audio = go.AddComponent<AudioSource>();
            video.playOnAwake = false;
            video.renderMode = VideoRenderMode.RenderTexture;
            video.audioOutputMode = VideoAudioOutputMode.AudioSource;
            video.EnableAudioTrack(0, true);
            video.SetTargetAudioSource(0, audio);
            video.prepareCompleted += OnVideoPrepared;
            video.loopPointReached += OnVideoEnded;
            video.errorReceived += OnVideoError;
        }

        video.url = path;
        video.Prepare();
    }

    void OnVideoPrepared(VideoPlayer source)
    {
        int w = Mathf.Max(16, (int)source.width);
        int h = Mathf.Max(16, (int)source.height);
        if (videoTexture != null)
            videoTexture.Release();
        videoTexture = new RenderTexture(w, h, 0);
        source.targetTexture = videoTexture;
        videoImage.texture = videoTexture;
        var fit = videoImage.GetComponent<AspectRatioFitter>();
        if (fit != null)
            fit.aspectRatio = w / (float)h;
        source.Play();
    }

    void OnVideoEnded(VideoPlayer source)
    {
        FinishVideo();
    }

    void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogWarning("인트로 영상을 재생하지 못했습니다. " + message);
        FinishVideo();
    }

    void FinishVideo()
    {
        if (state != Flow.Video)
            return;
        if (video != null && video.isPlaying)
            video.Stop();
        videoPanel.SetActive(false);
        StartRun(false, true);
    }

    void StartRun(bool fromCheckpoint, bool newGame, int keptHp = -1, float keptDash = 0f, float keptPotion = 0f, float keptRise = 0f)
    {
        if (storyRoutine != null)
            StopCoroutine(storyRoutine);
        storyRoutine = null;
        if (story != null)
            story.SetActive(false);
        HidePhaseLine();
        if (mapOpen)
            CloseWorldMap();
        if (newGame)
        {
            currentRoom = RoomId.Entrance;
            WorldProgress.HasEmber = false;
            StageBuilder.HasCheckpoint = false;
            arriveByPortal = false;
        }

        bool portalArrival = arriveByPortal && !newGame && !fromCheckpoint;
        RoomId fromRoom = arriveFrom;
        arriveByPortal = false;

        ClearWorld();
        world = new GameObject("World").transform;
        player = StageBuilder.Build(world, follow, fromCheckpoint, currentRoom, portalArrival, fromRoom);
        if (keptHp >= 0)
            player.RestoreVitals(keptHp, keptDash, keptPotion, keptRise);
        player.Damaged += OnDamaged;
        player.Healed += OnHealed;
        player.Died += OnPlayerDied;
        state = Flow.Play;
        Time.timeScale = 1f;
        hud.SetActive(true);
        pausePanel.SetActive(false);
        overPanel.SetActive(false);
        clearPanel.SetActive(false);
        var info = WorldCatalog.Get(currentRoom);
        locationText.text = "현재 위치\n" + info.Code + "  " + info.Name;
        RebuildMinimap();
        RefreshHud();
        if (newGame)
            storyRoutine = StartCoroutine(PlayOpeningLine());
    }

    public void EnterRoom(RoomId target, bool needsEmber)
    {
        if (state != Flow.Play || storyRoutine != null || mapOpen)
            return;
        if (needsEmber && !WorldProgress.HasEmber)
        {
            ShowBanner("심연의 불씨가 없다. 1-7 불의 심연에서 얻은 뒤 이곳에서 사용한다.", 2.4f);
            return;
        }

        int keptHp = player != null ? player.Hp : PlayerKnight.MaxHp;
        float keptDash = player != null ? player.DashCooldown : 0f;
        float keptPotion = player != null ? player.PotionCooldown : 0f;
        float keptRise = player != null ? player.RiseCooldown : 0f;
        RoomId from = currentRoom;
        currentRoom = target;
        StageBuilder.HasCheckpoint = false;
        arriveFrom = from;
        arriveByPortal = true;
        PortalHold = true;
        bool opened = needsEmber;
        StartRun(false, false, keptHp, keptDash, keptPotion, keptRise);
        if (opened)
            ShowBanner("심연의 불씨로 최후의 전장에 들어선다.", 2.2f);
    }

    void OnDamaged(int amount, bool unused)
    {
        Float("-" + amount, new Color(1f, 0.45f, 0.4f));
        RefreshHud();
    }

    void OnHealed(int amount, bool potion)
    {
        if (potion || amount >= 100)
            Float("+" + amount, new Color(0.55f, 1f, 0.75f));
        RefreshHud();
    }

    void OnPlayerDied()
    {
        HidePhaseLine();
        StartCoroutine(ShowOverSoon());
    }

    IEnumerator ShowOverSoon()
    {
        yield return new WaitForSeconds(0.85f);
        if (player == null || state != Flow.Play)
            yield break;
        state = Flow.Over;
        overReason.text = player.DeathReason + "\n마지막 기록에서 다시 도전할 수 있다.";
        overPanel.SetActive(true);
    }

    public void ClearStage()
    {
        if (state != Flow.Play)
            return;
        state = Flow.Clear;
        HidePhaseLine();
        if (player != null)
            player.InputLocked = true;
        AudioFx.Play("clear");
        clearBody.text = "최후의 전장에서 어둠의 근원을 넘어섰다.\n희미한 빛이 동굴을 다시 비춘다.";
        clearPanel.SetActive(true);
    }

    IEnumerator PlayOpeningLine()
    {
        const string line = "희미한 빛만 남은 동굴에서 깨어났다.\n끝을 향해 나아가라.";
        storyMain.text = line;
        storyGlow.text = line;
        story.SetActive(true);
        story.transform.SetAsLastSibling();

        Rigidbody2D body = player != null ? player.GetComponent<Rigidbody2D>() : null;
        if (player != null)
            player.InputLocked = true;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.gravityScale = 0f;
        }

        float duration = 3f;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float fadeIn = Mathf.SmoothStep(0f, 1f, time / 0.28f);
            float fadeOut = Mathf.SmoothStep(0f, 1f, (duration - time) / 0.4f);
            float alpha = fadeIn * fadeOut;
            float pulse = 0.55f + 0.45f * Mathf.Sin(time * 8f);
            storyMain.color = new Color(0.72f, 0.94f, 1f, alpha);
            storyGlow.color = new Color(0.15f + 0.25f * pulse, 0.45f, 1f, alpha * (0.45f + 0.55f * pulse));
            if (storyLight != null)
                storyLight.color = new Color(0.2f, 0.55f, 1f, alpha * (0.18f + 0.28f * pulse));
            float scale = Mathf.Lerp(0.82f, 1f, fadeIn) * (1f + 0.025f * Mathf.Sin(time * 5.5f));
            storyGroup.localScale = new Vector3(scale, scale, 1f);
            if (body != null)
                body.linearVelocity = Vector2.zero;
            yield return null;
        }

        story.SetActive(false);
        if (body != null)
            body.gravityScale = 1f;
        if (player != null && !player.Dead && state == Flow.Play)
            player.InputLocked = false;
        storyRoutine = null;
    }

    public void ShowBanner(string message, float seconds)
    {
        StopCoroutine(nameof(HideBanner));
        banner.SetActive(true);
        bannerText.text = message;
        StartCoroutine(HideBanner(seconds));
    }

    public void ShowPhaseLine(string message)
    {
        if (phaseRoutine != null)
            StopCoroutine(phaseRoutine);
        phaseLineText.text = message;
        phaseLineText.color = new Color(1f, 0.16f, 0.12f, 1f);
        phaseLine.SetActive(true);
        phaseLine.transform.SetAsLastSibling();
        phaseRoutine = StartCoroutine(HidePhaseLineSoon(2.8f));
    }

    IEnumerator HidePhaseLineSoon(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        phaseRoutine = null;
        if (phaseLine != null)
            phaseLine.SetActive(false);
    }

    void HidePhaseLine()
    {
        if (phaseRoutine != null)
            StopCoroutine(phaseRoutine);
        phaseRoutine = null;
        if (phaseLine != null)
            phaseLine.SetActive(false);
    }

    IEnumerator HideBanner(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        banner.SetActive(false);
    }

    void Pause()
    {
        state = Flow.Pause;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
    }

    void Resume()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        state = Flow.Play;
    }

    void Retry()
    {
        AudioFx.Play("click");
        Time.timeScale = 1f;
        StartRun(true, false);
    }

    void RetryFromStart()
    {
        AudioFx.Play("click");
        StageBuilder.HasCheckpoint = false;
        Time.timeScale = 1f;
        StartRun(false, true);
    }

    void GoTitle()
    {
        AudioFx.Play("click");
        ShowTitle();
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void RefreshHud()
    {
        if (player == null)
            return;
        float ratio = Mathf.Clamp01(player.Hp / (float)PlayerKnight.MaxHp);
        hpFill.fillAmount = ratio;
        hpFill.color = ratio > 0.45f
            ? new Color(0.45f, 0.9f, 0.85f)
            : ratio > 0.2f ? new Color(0.95f, 0.78f, 0.35f) : new Color(0.95f, 0.38f, 0.35f);
        hpText.text = "체력  " + Mathf.Max(0, player.Hp) + " / " + PlayerKnight.MaxHp;
        SetCooldown(dashFill, dashLabel, "대시", player.DashCooldown, PlayerKnight.DashCooldownMax, new Color(0.45f, 0.78f, 1f));
        SetCooldown(potionFill, potionLabel, "포션", player.PotionCooldown, PlayerKnight.PotionInterval, new Color(0.45f, 0.95f, 0.7f));
        SetCooldown(riseFill, riseLabel, "상승", player.RiseCooldown, PlayerKnight.RiseCooldownMax, new Color(0.72f, 0.78f, 1f));
        RefreshBossBar();
        RefreshMinimap();
    }

    void BuildBossBar(Transform parent)
    {
        bossBar = new GameObject("BossBar");
        bossBar.transform.SetParent(parent, false);
        var root = bossBar.AddComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 1f);
        root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = new Vector2(0f, -8f);
        root.sizeDelta = new Vector2(980f, 78f);

        var plate = MakeImage(bossBar.transform, ArtBank.White, new Color(0.05f, 0.04f, 0.04f, 0.92f));
        plate.raycastTarget = false;
        var plateRect = plate.rectTransform;
        plateRect.anchorMin = new Vector2(0.5f, 0f);
        plateRect.anchorMax = new Vector2(0.5f, 0f);
        plateRect.pivot = new Vector2(0.5f, 0f);
        plateRect.anchoredPosition = Vector2.zero;
        plateRect.sizeDelta = new Vector2(860f, 34f);

        var track = MakeImage(bossBar.transform, ArtBank.White, new Color(0.16f, 0.07f, 0.05f, 1f));
        track.raycastTarget = false;
        var trackRect = track.rectTransform;
        trackRect.anchorMin = new Vector2(0.5f, 0f);
        trackRect.anchorMax = new Vector2(0.5f, 0f);
        trackRect.pivot = new Vector2(0.5f, 0f);
        trackRect.anchoredPosition = new Vector2(-28f, 4f);
        trackRect.sizeDelta = new Vector2(780f, 22f);

        bossFill = MakeImage(bossBar.transform, ArtBank.White, new Color(0.95f, 0.42f, 0.12f, 1f));
        bossFill.raycastTarget = false;
        bossFill.type = Image.Type.Filled;
        bossFill.fillMethod = Image.FillMethod.Horizontal;
        bossFill.fillAmount = 1f;
        bossFill.rectTransform.anchorMin = trackRect.anchorMin;
        bossFill.rectTransform.anchorMax = trackRect.anchorMax;
        bossFill.rectTransform.pivot = trackRect.pivot;
        bossFill.rectTransform.anchoredPosition = trackRect.anchoredPosition;
        bossFill.rectTransform.sizeDelta = trackRect.sizeDelta;

        var name = Label(bossBar.transform, 22, TextAnchor.MiddleLeft, new Color(1f, 0.9f, 0.72f));
        name.fontStyle = FontStyle.Bold;
        name.text = "드래곤";
        var nameRect = name.rectTransform;
        nameRect.anchorMin = new Vector2(0.5f, 0f);
        nameRect.anchorMax = new Vector2(0.5f, 0f);
        nameRect.pivot = new Vector2(0f, 0f);
        nameRect.anchoredPosition = new Vector2(-400f, 30f);
        nameRect.sizeDelta = new Vector2(220f, 32f);

        bossHpText = Label(bossBar.transform, 20, TextAnchor.MiddleCenter, Color.white);
        bossHpText.fontStyle = FontStyle.Bold;
        bossHpText.text = DragonBoss.MaxHp + " / " + DragonBoss.MaxHp;
        var hpRect = bossHpText.rectTransform;
        hpRect.anchorMin = new Vector2(0.5f, 0f);
        hpRect.anchorMax = new Vector2(0.5f, 0f);
        hpRect.pivot = new Vector2(0.5f, 0f);
        hpRect.anchoredPosition = new Vector2(-28f, 4f);
        hpRect.sizeDelta = new Vector2(760f, 22f);

        var badge = MakeImage(bossBar.transform, ArtBank.White, new Color(0.22f, 0.08f, 0.05f, 0.98f));
        badge.raycastTarget = false;
        var badgeRect = badge.rectTransform;
        badgeRect.anchorMin = new Vector2(0.5f, 0f);
        badgeRect.anchorMax = new Vector2(0.5f, 0f);
        badgeRect.pivot = new Vector2(0f, 0f);
        badgeRect.anchoredPosition = new Vector2(372f, 2f);
        badgeRect.sizeDelta = new Vector2(54f, 38f);

        bossPhaseText = Label(badge.transform, 26, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.45f));
        bossPhaseText.fontStyle = FontStyle.Bold;
        bossPhaseText.text = "1";
        Stretch(bossPhaseText.rectTransform);

        bossBar.SetActive(false);
    }

    void RefreshBossBar()
    {
        var boss = DragonBoss.Active;
        bool show = boss != null && !boss.Dead && hud != null && hud.activeSelf;
        if (bossBar != null)
            bossBar.SetActive(show);
        if (!show)
            return;

        float ratio = Mathf.Clamp01(boss.Hp / (float)DragonBoss.MaxHp);
        bossFill.fillAmount = ratio;
        bossFill.color = boss.Phase >= 3
            ? new Color(0.95f, 0.18f, 0.14f, 1f)
            : boss.Phase == 2
                ? new Color(0.96f, 0.32f, 0.1f, 1f)
                : new Color(0.95f, 0.48f, 0.12f, 1f);
        bossHpText.text = Mathf.Max(0, boss.Hp) + " / " + DragonBoss.MaxHp;
        bossPhaseText.text = boss.Phase.ToString();
    }

    Image MakeCooldownBar(Transform parent, string caption, float x0, float x1, Color color, out Text label)
    {
        label = Label(parent, 18, TextAnchor.MiddleLeft, color);
        var labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(x0, 0.082f);
        labelRect.anchorMax = new Vector2(x1, 0.12f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        label.text = caption;

        var background = MakeImage(parent, ArtBank.White, new Color(0.08f, 0.12f, 0.16f, 0.95f));
        SetBar(background.rectTransform, x0, 0.04f, x1, 0.075f);
        var fill = MakeImage(parent, ArtBank.White, color);
        fill.type = UnityEngine.UI.Image.Type.Filled;
        fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        fill.fillAmount = 1f;
        SetBar(fill.rectTransform, x0, 0.04f, x1, 0.075f);
        return fill;
    }

    static void SetCooldown(Image fill, Text label, string name, float remaining, float duration, Color ready)
    {
        float left = Mathf.Max(0f, remaining);
        float ratio = duration <= 0f ? 1f : 1f - Mathf.Clamp01(left / duration);
        fill.fillAmount = ratio;
        bool charged = left <= 0.05f;
        fill.color = charged ? ready : new Color(ready.r * 0.45f, ready.g * 0.45f, ready.b * 0.45f, 0.9f);
        label.text = charged ? name : name + "  " + Mathf.CeilToInt(left);
        label.color = charged ? ready : new Color(0.75f, 0.82f, 0.86f);
    }

    GameObject BuildWorldMap()
    {
        var panel = FullPanel("WorldMap");
        var dim = MakeImage(panel.transform, ArtBank.White, new Color(0.02f, 0.04f, 0.07f, 0.86f));
        Stretch(dim.rectTransform);

        var card = new GameObject("Card");
        card.transform.SetParent(panel.transform, false);
        var cardRect = card.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(1560f, 880f);
        var cardImage = card.AddComponent<Image>();
        cardImage.sprite = ArtBank.White;
        cardImage.color = new Color(0.04f, 0.09f, 0.14f, 0.97f);

        var title = Label(card.transform, 40, TextAnchor.MiddleCenter, new Color(0.78f, 0.93f, 1f));
        title.fontStyle = FontStyle.Bold;
        title.text = "월드 1. 잊혀진 동굴";
        var titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -46f);
        titleRect.sizeDelta = new Vector2(900f, 52f);

        var sub = Label(card.transform, 22, TextAnchor.MiddleCenter, new Color(0.62f, 0.8f, 0.9f));
        sub.text = "어둠 속에서 빛나는 작은 생명들, 그리고... 더 깊은 곳의 비밀.";
        var subRect = sub.rectTransform;
        subRect.anchorMin = new Vector2(0.5f, 1f);
        subRect.anchorMax = new Vector2(0.5f, 1f);
        subRect.anchoredPosition = new Vector2(0f, -96f);
        subRect.sizeDelta = new Vector2(1100f, 36f);

        var board = new GameObject("Board");
        board.transform.SetParent(card.transform, false);
        var boardRect = board.AddComponent<RectTransform>();
        boardRect.anchorMin = new Vector2(0.5f, 0.5f);
        boardRect.anchorMax = new Vector2(0.5f, 0.5f);
        boardRect.sizeDelta = new Vector2(1460f, 600f);
        boardRect.anchoredPosition = new Vector2(0f, -24f);

        var rooms = WorldCatalog.Rooms;
        var seen = new System.Collections.Generic.HashSet<long>();
        for (int i = 0; i < rooms.Length; i++)
        {
            for (int j = 0; j < rooms[i].Links.Length; j++)
            {
                int a = (int)rooms[i].Id;
                int b = (int)rooms[i].Links[j].Target;
                long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                if (!seen.Add(key))
                    continue;
                DrawMapLine(boardRect, MapPoint(rooms[i].MapAnchor), MapPoint(WorldCatalog.Get(rooms[i].Links[j].Target).MapAnchor), rooms[i].Links[j].NeedsEmber);
            }
        }

        roomNodes = new Image[rooms.Length];
        for (int i = 0; i < rooms.Length; i++)
        {
            var room = rooms[i];
            var node = new GameObject(room.Code);
            node.transform.SetParent(board.transform, false);
            var rect = node.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(176f, 96f);
            rect.anchoredPosition = MapPoint(room.MapAnchor);
            var image = node.AddComponent<Image>();
            image.sprite = ArtBank.White;
            image.color = new Color(0.1f, 0.2f, 0.28f, 0.96f);
            roomNodes[i] = image;

            var code = Label(node.transform, 16, TextAnchor.MiddleCenter, new Color(0.7f, 0.88f, 1f));
            code.text = room.Code + "   " + room.Kind;
            var codeRect = code.rectTransform;
            codeRect.anchorMin = new Vector2(0.04f, 0.58f);
            codeRect.anchorMax = new Vector2(0.96f, 0.94f);
            codeRect.offsetMin = Vector2.zero;
            codeRect.offsetMax = Vector2.zero;

            var name = Label(node.transform, 22, TextAnchor.MiddleCenter, Color.white);
            name.fontStyle = FontStyle.Bold;
            name.text = room.Name;
            var nameRect = name.rectTransform;
            nameRect.anchorMin = new Vector2(0.04f, 0.08f);
            nameRect.anchorMax = new Vector2(0.96f, 0.58f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
        }

        var pin = new GameObject("You");
        pin.transform.SetParent(board.transform, false);
        youPin = pin.AddComponent<RectTransform>();
        youPin.anchorMin = new Vector2(0.5f, 0.5f);
        youPin.anchorMax = new Vector2(0.5f, 0.5f);
        youPin.sizeDelta = new Vector2(22f, 22f);
        var pinImage = pin.AddComponent<Image>();
        pinImage.sprite = ArtBank.SoftCircle;
        pinImage.color = new Color(0.45f, 1f, 0.85f, 1f);

        var pinLabel = Label(board.transform, 16, TextAnchor.MiddleCenter, new Color(0.55f, 1f, 0.9f));
        pinLabel.text = "현재 위치";
        youLabel = pinLabel.rectTransform;
        youLabel.anchorMin = new Vector2(0.5f, 0.5f);
        youLabel.anchorMax = new Vector2(0.5f, 0.5f);
        youLabel.sizeDelta = new Vector2(140f, 24f);

        worldMapStatus = Label(card.transform, 22, TextAnchor.MiddleCenter, new Color(0.75f, 0.9f, 1f));
        var statusRect = worldMapStatus.rectTransform;
        statusRect.anchorMin = new Vector2(0.5f, 0f);
        statusRect.anchorMax = new Vector2(0.5f, 0f);
        statusRect.anchoredPosition = new Vector2(0f, 58f);
        statusRect.sizeDelta = new Vector2(1200f, 36f);

        var hint = Label(card.transform, 18, TextAnchor.MiddleCenter, new Color(0.55f, 0.72f, 0.82f));
        hint.text = "M 또는 Esc로 닫기    ·    1-6 잊혀진 제단    ·    1-7 불의 심연에서 심연의 불씨를 얻어 보스 진입에서 사용";
        var hintRect = hint.rectTransform;
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.anchoredPosition = new Vector2(0f, 26f);
        hintRect.sizeDelta = new Vector2(1400f, 30f);

        panel.SetActive(false);
        return panel;
    }

    static Vector2 MapPoint(Vector2 anchor)
    {
        return new Vector2((anchor.x - 0.5f) * 1260f, (anchor.y - 0.42f) * 520f);
    }

    void DrawMapLine(RectTransform parent, Vector2 a, Vector2 b, bool ember)
    {
        var go = new GameObject("Line");
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.sprite = ArtBank.White;
        image.color = ember ? new Color(0.72f, 0.45f, 1f, 0.9f) : new Color(0.45f, 0.82f, 0.95f, 0.75f);
        var rect = image.rectTransform;
        Vector2 delta = b - a;
        rect.sizeDelta = new Vector2(delta.magnitude, 4f);
        rect.anchoredPosition = (a + b) * 0.5f;
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    void ToggleWorldMap()
    {
        if (mapOpen)
            CloseWorldMap();
        else
            OpenWorldMap();
    }

    void OpenWorldMap()
    {
        mapOpen = true;
        Time.timeScale = 0f;
        RefreshWorldMap();
        worldMap.SetActive(true);
        worldMap.transform.SetAsLastSibling();
    }

    void CloseWorldMap()
    {
        mapOpen = false;
        if (worldMap != null)
            worldMap.SetActive(false);
        if (state == Flow.Play)
            Time.timeScale = 1f;
    }

    void RefreshWorldMap()
    {
        var rooms = WorldCatalog.Rooms;
        for (int i = 0; i < rooms.Length; i++)
        {
            bool here = rooms[i].Id == currentRoom;
            bool gate = rooms[i].Id == RoomId.BossGate;
            bool boss = rooms[i].Id == RoomId.Boss;
            roomNodes[i].color = here
                ? new Color(0.2f, 0.55f, 0.62f, 1f)
                : boss ? new Color(0.42f, 0.16f, 0.62f, 1f)
                : gate ? new Color(0.28f, 0.18f, 0.48f, 1f)
                : new Color(0.1f, 0.2f, 0.3f, 0.96f);
        }

        Vector2 point = MapPoint(WorldCatalog.Get(currentRoom).MapAnchor);
        youPin.anchoredPosition = point + new Vector2(0f, 62f);
        youLabel.anchoredPosition = point + new Vector2(0f, 84f);
        worldMapStatus.text = WorldProgress.HasEmber
            ? "심연의 불씨 보유    ·    보스 진입 포탈에서 최후의 전장으로 들어갈 수 있다"
            : "심연의 불씨 없음    ·    1-7 불의 심연에서 획득";
    }

    void Float(string message, Color color)
    {
        if (player == null || Camera.main == null)
            return;
        var text = Label(canvas.transform, 32, TextAnchor.MiddleCenter, color);
        text.text = message;
        text.raycastTarget = false;
        var rect = text.rectTransform;
        rect.sizeDelta = new Vector2(220f, 50f);
        Vector2 screen = Camera.main.WorldToScreenPoint(player.transform.position + Vector3.up * 1.7f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, screen, null, out var local);
        rect.anchoredPosition = local;
        StartCoroutine(Rise(rect));
    }

    IEnumerator Rise(RectTransform rect)
    {
        float time = 0f;
        Vector2 start = rect.anchoredPosition;
        var graphic = rect.GetComponent<Text>();
        while (time < 0.7f && rect != null)
        {
            time += Time.deltaTime;
            rect.anchoredPosition = start + Vector2.up * (time * 70f);
            if (graphic != null)
            {
                var color = graphic.color;
                color.a = 1f - time / 0.7f;
                graphic.color = color;
            }
            yield return null;
        }
        if (rect != null)
            Destroy(rect.gameObject);
    }

    void ClearWorld()
    {
        if (world != null)
            Destroy(world.gameObject);
        world = null;
        player = null;
    }

    GameObject FullPanel(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(canvas.transform, false);
        Stretch(go.AddComponent<RectTransform>());
        return go;
    }

    Image MakeImage(Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject("Image");
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.rectTransform.sizeDelta = new Vector2(100f, 100f);
        return image;
    }

    RawImage ImageRaw(Transform parent)
    {
        var go = new GameObject("VideoImage");
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<RawImage>();
        image.color = Color.white;
        return image;
    }

    Text Label(Transform parent, int size, TextAnchor anchor, Color color)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    GameObject Button(Transform parent, string caption, Vector2 anchored, UnityEngine.Events.UnityAction click)
    {
        var image = MakeImage(parent, ArtBank.White, Color.white);
        var rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(320f, 64f);
        rect.anchoredPosition = anchored;
        var button = image.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = new Color(0.08f, 0.28f, 0.34f, 0.95f);
        colors.highlightedColor = new Color(0.16f, 0.5f, 0.58f, 1f);
        colors.pressedColor = new Color(0.45f, 0.85f, 0.9f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.targetGraphic = image;
        button.onClick.AddListener(click);
        var label = Label(image.transform, 28, TextAnchor.MiddleCenter, Color.white);
        label.text = caption;
        Stretch(label.rectTransform);
        return image.gameObject;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void SetBar(RectTransform rect, float x0, float y0, float x1, float y1)
    {
        rect.anchorMin = new Vector2(x0, y0);
        rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        Time.timeScale = 1f;
        if (videoTexture != null)
            videoTexture.Release();
    }
}

public class BackdropFollow : MonoBehaviour
{
    public Camera Cam;
    SpriteRenderer view;

    void Awake()
    {
        view = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (Cam == null)
            Cam = Camera.main;
        if (Cam == null || view.sprite == null)
            return;

        float worldH = Cam.orthographicSize * 2f;
        float worldW = worldH * Cam.aspect;
        Vector2 size = view.sprite.bounds.size;
        float scale = Mathf.Max(worldW / size.x, worldH / size.y) * 1.08f;
        transform.localScale = new Vector3(scale, scale, 1f);
        transform.position = new Vector3(Cam.transform.position.x, Cam.transform.position.y * 0.25f, 2f);
        view.color = StageBuilder.BossVoid
            ? new Color(0.015f, 0.02f, 0.04f, 1f)
            : new Color(0.62f, 0.78f, 0.88f, 1f);
    }
}
