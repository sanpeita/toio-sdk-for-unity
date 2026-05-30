using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace toio.Experiments.ToioLauncher
{
    public class ToioLauncherController : MonoBehaviour
    {
        private const string LeftHandLabSceneName = "ToioLeftHandLab";
        private const string BlenderLabSceneName = "ToioBlenderLab";
        private const string DistanceUnityLabSceneName = "ToioDistanceUnityLab";
        private const string TacticalFieldSceneName = "ToioTacticalField";
        private const string RootName = "ToioLauncherRoot";

        private static readonly Color BackgroundColor = new Color(0.09f, 0.11f, 0.16f, 1f);
        private static readonly Color PanelColor = new Color(0.12f, 0.16f, 0.22f, 0.94f);
        private static readonly Color AccentColor = new Color(0.32f, 0.73f, 0.83f, 1f);
        private static readonly Color AccentSecondaryColor = new Color(0.96f, 0.72f, 0.36f, 1f);
        private static readonly Color TextColor = new Color(0.94f, 0.97f, 1f, 1f);
        private static readonly Color MutedTextColor = new Color(0.77f, 0.83f, 0.9f, 1f);

        private void Awake()
        {
            Application.runInBackground = true;
            EnsureEventSystem();
            EnsureCamera();
            EnsureCanvas();
        }

        private void Start()
        {
            BuildUi();
        }

        private void BuildUi()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            var existing = GameObject.Find(RootName);
            if (existing != null)
            {
                return;
            }

            var root = CreateUiObject(RootName, canvas.transform);
            var rootRect = root.GetComponent<RectTransform>();
            StretchFull(rootRect);

            var background = root.AddComponent<Image>();
            background.color = BackgroundColor;

            var header = CreatePanel("HeaderPanel", root.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(920f, 180f), PanelColor);
            CreateText("Eyebrow", header.transform, "toio device experiments | ToioJetHand", 22, FontStyle.Bold, TextAnchor.UpperCenter, AccentColor, new Vector2(0f, 52f), new Vector2(760f, 30f));
            CreateText("Title", header.transform, "ToioJetHand Launcher", 42, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor, new Vector2(0f, 8f), new Vector2(820f, 56f));
            CreateText("Subtitle", header.transform, "Choose a dedicated scene for Minecraft input, Blender input, Unity distance visualization, or Ordia tabletop tactics.", 22, FontStyle.Normal, TextAnchor.LowerCenter, MutedTextColor, new Vector2(0f, -42f), new Vector2(760f, 34f));

            var card = CreatePanel("CardPanel", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(920f, 520f), PanelColor);
            CreateText("CardHeading", card.transform, "Scene Entry Points", 28, FontStyle.Bold, TextAnchor.UpperLeft, TextColor, new Vector2(-320f, 160f), new Vector2(320f, 34f));
            CreateText("CardBody", card.transform, "The current split stays manageable inside ToioJetHand: launcher, Minecraft input, Blender input, and Unity distance visualization. If profiles grow later, share runtime input/output components instead of adding many entry scenes.", 22, FontStyle.Normal, TextAnchor.UpperLeft, MutedTextColor, new Vector2(-320f, 102f), new Vector2(640f, 132f));

            CreateButton(
                "ButtonLeftHandLab",
                card.transform,
                "Open toioLeftHandLab",
                new Vector2(-210f, -78f),
                new Vector2(320f, 76f),
                AccentColor,
                () => LoadScene(LeftHandLabSceneName)
            );
            CreateButton(
                "ButtonBlenderLab",
                card.transform,
                "Open toioBlenderLab",
                new Vector2(210f, -78f),
                new Vector2(320f, 76f),
                AccentSecondaryColor,
                () => LoadScene(BlenderLabSceneName)
            );
            CreateButton(
                "ButtonDistanceUnityLab",
                card.transform,
                "Open DistanceUnityLab",
                new Vector2(0f, -150f),
                new Vector2(360f, 64f),
                new Color(0.46f, 0.92f, 0.68f, 1f),
                () => LoadScene(DistanceUnityLabSceneName)
            );
            CreateButton(
                "ButtonTacticalField",
                card.transform,
                "Open TacticalField",
                new Vector2(0f, -212f),
                new Vector2(360f, 52f),
                new Color(0.58f, 0.76f, 0.66f, 1f),
                () => LoadScene(TacticalFieldSceneName)
            );

            CreateText("CardFooter", card.transform, "Project: ToioJetHand / toio device experiments", 18, FontStyle.Normal, TextAnchor.LowerLeft, MutedTextColor, new Vector2(-320f, -248f), new Vector2(420f, 24f));
        }

        private static void LoadScene(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"Scene '{sceneName}' is not available in Build Settings.");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = BackgroundColor;
                return;
            }

            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            cameraObject.AddComponent<AudioListener>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void EnsureCanvas()
        {
            if (FindObjectOfType<Canvas>() != null)
            {
                return;
            }

            var canvasObject = new GameObject("Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
        {
            var panel = CreateUiObject(name, parent);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var image = panel.AddComponent<Image>();
            image.color = color;
            return panel;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 sizeDelta, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var buttonObject = CreateUiObject(name, parent);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var image = buttonObject.AddComponent<Image>();
            image.color = color;

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            CreateText("Label", buttonObject.transform, label, 24, FontStyle.Bold, TextAnchor.MiddleCenter, BackgroundColor, Vector2.zero, sizeDelta - new Vector2(28f, 18f));
            return button;
        }

        private static Text CreateText(string name, Transform parent, string text, int fontSize, FontStyle fontStyle, TextAnchor anchor, Color color, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var textObject = CreateUiObject(name, parent);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var textComponent = textObject.AddComponent<Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = fontStyle;
            textComponent.alignment = anchor;
            textComponent.color = color;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Overflow;
            return textComponent;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
