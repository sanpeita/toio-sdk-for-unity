using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace toio.Experiments.ToioBlenderLab
{
    [DisallowMultipleComponent]
    public class ToioBlenderLabController : MonoBehaviour
    {
        private const string LauncherSceneName = "ToioLauncher";
        private const string LeftHandLabSceneName = "ToioLeftHandLab";
        private const string RootName = "ToioBlenderLabRoot";

        private static readonly Color BackgroundColor = new Color(0.08f, 0.09f, 0.13f, 1f);
        private static readonly Color PanelColor = new Color(0.15f, 0.18f, 0.24f, 0.95f);
        private static readonly Color AccentColor = new Color(0.38f, 0.85f, 0.74f, 1f);
        private static readonly Color AccentSecondaryColor = new Color(0.98f, 0.62f, 0.41f, 1f);
        private static readonly Color TextColor = new Color(0.95f, 0.98f, 1f, 1f);
        private static readonly Color MutedTextColor = new Color(0.78f, 0.83f, 0.9f, 1f);
        private static readonly Color CardColor = new Color(0.12f, 0.16f, 0.22f, 0.96f);
        private static readonly Color CardSecondaryColor = new Color(0.11f, 0.14f, 0.2f, 0.96f);

        private ToioBlenderCubeInput inputSource;
        private WindowsExternalBlenderOutput outputBridge;

        private Button connectButton;
        private Text connectButtonLabel;
        private Text connectionStatusLabel;
        private Text cubeStatusLabel;
        private Text actionStatusLabel;
        private Text outputStatusLabel;

        private void Awake()
        {
            Application.runInBackground = true;
            EnsureRuntimeComponents();
            EnsureEventSystem();
            EnsureCamera();
            EnsureCanvas();
        }

        private void Start()
        {
            BuildUi();
            RefreshRuntimeUi();
        }

        private void Update()
        {
            RefreshRuntimeUi();
        }

        public async void OnConnectCube()
        {
            if (inputSource == null)
            {
                return;
            }

            await inputSource.Connect();
            RefreshRuntimeUi();
        }

        private void EnsureRuntimeComponents()
        {
            inputSource = GetComponent<ToioBlenderCubeInput>();
            if (inputSource == null)
            {
                inputSource = gameObject.AddComponent<ToioBlenderCubeInput>();
            }

            outputBridge = GetComponent<WindowsExternalBlenderOutput>();
            if (outputBridge == null)
            {
                outputBridge = gameObject.AddComponent<WindowsExternalBlenderOutput>();
            }
        }

        private void BuildUi()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null || GameObject.Find(RootName) != null)
            {
                return;
            }

            var root = CreateUiObject(RootName, canvas.transform);
            var rootRect = root.GetComponent<RectTransform>();
            StretchFull(rootRect);
            root.AddComponent<Image>().color = BackgroundColor;

            var hero = CreatePanel(
                "HeroPanel",
                root.transform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -74f),
                new Vector2(960f, 164f),
                PanelColor
            );
            CreateText("Eyebrow", hero.transform, "toio左手ガジェット化計画 | ToioJetHand", 22, FontStyle.Bold, TextAnchor.UpperCenter, AccentColor, new Vector2(0f, 40f), new Vector2(760f, 28f));
            CreateText("Title", hero.transform, "toioBlenderLab", 42, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor, new Vector2(0f, 0f), new Vector2(820f, 56f));
            CreateText("Subtitle", hero.transform, "Connect two cubes. Cube 1 keeps Orbit / Zoom / Tab, and Cube 2 selects edit targets, preview modes, and executes add on button press.", 20, FontStyle.Normal, TextAnchor.LowerCenter, MutedTextColor, new Vector2(0f, -36f), new Vector2(880f, 30f));

            var infoPanel = CreatePanel(
                "InfoPanel",
                root.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 20f),
                new Vector2(980f, 540f),
                new Color(0.15f, 0.18f, 0.24f, 0.98f)
            );

            var controlsCard = CreatePanel(
                "ControlsCard",
                infoPanel.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 152f),
                new Vector2(880f, 208f),
                CardColor
            );
            CreateText("SectionTitle", controlsCard.transform, "toioBlenderLab ver1.1 Minimal Control Set", 28, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor, new Vector2(0f, 70f), new Vector2(760f, 36f));
            CreateText("Cube1Line", controlsCard.transform, "Cube 1: roll = Orbit, pitch = Zoom, button = Tab.", 22, FontStyle.Normal, TextAnchor.MiddleCenter, TextColor, new Vector2(0f, 26f), new Vector2(840f, 36f));
            CreateText("Cube2Line1", controlsCard.transform, "Cube 2: forward = Select Plane, backward = Select Cube, button = Add selected.", 22, FontStyle.Normal, TextAnchor.MiddleCenter, TextColor, new Vector2(0f, -16f), new Vector2(840f, 36f));
            CreateText("Cube2Line2", controlsCard.transform, "Cube 2: left = Solid, right = Material Preview.", 22, FontStyle.Normal, TextAnchor.MiddleCenter, TextColor, new Vector2(0f, -58f), new Vector2(840f, 36f));
            CreateText("GuardLine", controlsCard.transform, "Cube 2 selection changes on tilt; preview mode and add execute as one-shot actions.", 18, FontStyle.Normal, TextAnchor.MiddleCenter, MutedTextColor, new Vector2(0f, -92f), new Vector2(860f, 32f));

            var runtimeCard = CreatePanel(
                "RuntimeCard",
                infoPanel.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -24f),
                new Vector2(880f, 208f),
                CardSecondaryColor
            );
            CreateText("RuntimeTitle", runtimeCard.transform, "Cube Connection & Live Status", 28, FontStyle.Bold, TextAnchor.MiddleCenter, AccentSecondaryColor, new Vector2(0f, 68f), new Vector2(760f, 36f));
            connectButton = CreateButton("ButtonConnectCube", runtimeCard.transform, "Connect Cubes", new Vector2(0f, 22f), new Vector2(260f, 56f), AccentColor, OnConnectCube);
            connectButtonLabel = connectButton.GetComponentInChildren<Text>();
            connectionStatusLabel = CreateText("ConnectionStatus", runtimeCard.transform, string.Empty, 20, FontStyle.Normal, TextAnchor.MiddleCenter, TextColor, new Vector2(0f, -20f), new Vector2(840f, 28f));
            cubeStatusLabel = CreateText("CubeStatus", runtimeCard.transform, string.Empty, 19, FontStyle.Normal, TextAnchor.MiddleCenter, MutedTextColor, new Vector2(0f, -54f), new Vector2(860f, 42f));
            actionStatusLabel = CreateText("ActionStatus", runtimeCard.transform, string.Empty, 19, FontStyle.Normal, TextAnchor.MiddleCenter, MutedTextColor, new Vector2(0f, -92f), new Vector2(860f, 38f));
            outputStatusLabel = CreateText("OutputStatus", runtimeCard.transform, string.Empty, 19, FontStyle.Normal, TextAnchor.MiddleCenter, MutedTextColor, new Vector2(0f, -128f), new Vector2(860f, 42f));

            CreateButton("ButtonLauncher", infoPanel.transform, "Back To Launcher", new Vector2(-184f, -220f), new Vector2(280f, 70f), AccentColor, () => LoadScene(LauncherSceneName));
            CreateButton("ButtonLeftHandLab", infoPanel.transform, "Open toioLeftHandLab", new Vector2(184f, -220f), new Vector2(320f, 70f), AccentSecondaryColor, () => LoadScene(LeftHandLabSceneName));
        }

        private void RefreshRuntimeUi()
        {
            if (inputSource == null)
            {
                return;
            }

            if (connectButton != null)
            {
                connectButton.interactable = !inputSource.IsConnecting && !inputSource.IsConnected;
            }

            if (connectButtonLabel != null)
            {
                connectButtonLabel.text = inputSource.IsConnecting
                    ? "Connecting..."
                    : inputSource.IsConnected ? "Cubes Connected" : "Connect Cubes";
            }

            if (connectionStatusLabel != null)
            {
                connectionStatusLabel.text = $"Connection: {inputSource.ConnectionMessage}";
            }

            if (cubeStatusLabel != null)
            {
                var viewPoseLabel = inputSource.HasViewCubePose ? inputSource.ViewCubePose.ToString() : "Waiting";
                var editPoseLabel = inputSource.HasEditCubePose ? inputSource.EditCubePose.ToString() : "Waiting";
                cubeStatusLabel.text =
                    $"Cube 1 {ToioBlenderCubeInput.GetCubeDebugName(inputSource.ViewCube, "not connected")}: pose={viewPoseLabel} button={(inputSource.ViewCubeButtonPressed ? "ON" : "off")} neutral={(inputSource.IsReadyForModeToggle ? "yes" : "no")} | " +
                    $"Cube 2 {ToioBlenderCubeInput.GetCubeDebugName(inputSource.EditCube, "not connected")}: pose={editPoseLabel} button={(inputSource.EditCubeButtonPressed ? "ON" : "off")}";
            }

            if (actionStatusLabel != null)
            {
                var viewEulers = inputSource.ViewCubeEulers;
                var editEulers = inputSource.EditCubeEulers;
                actionStatusLabel.text =
                    $"Input: orbit={inputSource.OrbitAxis:+0.00;-0.00;0.00} zoom={inputSource.ZoomAxis:+0.00;-0.00;0.00} action={inputSource.CurrentActionSummary} | " +
                    $"Cube1 x={viewEulers.x:F1} y={viewEulers.y:F1} | Cube2 x={editEulers.x:F1} y={editEulers.y:F1} | selected={inputSource.SelectedAddMacroLabel} queued={inputSource.PendingEditMacroCount} last={inputSource.LastQueuedEditMacroLabel}";
            }

            if (outputStatusLabel != null)
            {
                outputStatusLabel.text = $"Output: {outputBridge?.RuntimeStatus ?? "Output bridge missing."}";
            }
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
            camera.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
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
            panel.AddComponent<Image>().color = color;
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

            CreateText("Label", buttonObject.transform, label, 22, FontStyle.Bold, TextAnchor.MiddleCenter, BackgroundColor, Vector2.zero, sizeDelta - new Vector2(24f, 18f));
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
            textComponent.raycastTarget = false;
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
