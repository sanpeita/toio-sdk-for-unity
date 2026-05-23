using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using toio;

namespace toio.Experiments.ToioDistanceUnityLab
{
    [DisallowMultipleComponent]
    public class ToioDistanceUnityLabController : MonoBehaviour
    {
        private const string LauncherSceneName = "ToioLauncher";
        private const string BlenderLabSceneName = "ToioBlenderLab";
        private const string RootName = "ToioDistanceUnityLabRoot";
        private const string VersionLabel = "ver0.1";

        private static readonly Color BackgroundColor = new Color(0.07f, 0.09f, 0.11f, 1f);
        private static readonly Color PanelColor = new Color(0.13f, 0.17f, 0.21f, 0.96f);
        private static readonly Color CardColor = new Color(0.1f, 0.14f, 0.18f, 0.96f);
        private static readonly Color AccentAColor = new Color(0.3f, 0.78f, 1f, 1f);
        private static readonly Color AccentBColor = new Color(1f, 0.66f, 0.32f, 1f);
        private static readonly Color DistanceColor = new Color(0.52f, 1f, 0.72f, 1f);
        private static readonly Color TextColor = new Color(0.95f, 0.98f, 1f, 1f);
        private static readonly Color MutedTextColor = new Color(0.73f, 0.81f, 0.88f, 1f);

        [Header("Connection")]
        [SerializeField] private ConnectType connectType = ConnectType.Real;
        [SerializeField] private int connectMaxAttempts = 3;
        [SerializeField] private int retryDelayMs = 1200;
        [SerializeField] private int idNotificationIntervalMs = 50;

        [Header("Mat To Unity View")]
        [SerializeField] private Vector2 matCenter = new Vector2(250f, 250f);
        [SerializeField] private float matToUnityScale = 0.025f;
        [SerializeField] private float markerHeight = 0.35f;
        [SerializeField] private float markerDiameter = 0.72f;
        [SerializeField] private float distanceBarHeight = 0.22f;
        [SerializeField] private float distanceBarThickness = 0.28f;
        [SerializeField] private float liveCubeMarkerDiameter = 0.36f;
        [SerializeField] private bool useFallbackPointsWhenMatIdMissing = true;

        private CubeManager cubeManager;
        private Cube cubeA;
        private Cube cubeB;
        private string cubeAListenerKey;
        private string cubeBListenerKey;
        private bool isConnecting;
        private string connectionMessage = "Not connected. Press Connect Cubes.";
        private bool cubeAButtonPressed;
        private bool cubeBButtonPressed;
        private bool hasLiveA;
        private bool hasLiveB;
        private bool hasPointA;
        private bool hasPointB;
        private Vector2 livePointA;
        private Vector2 livePointB;
        private Vector2 pointA;
        private Vector2 pointB;
        private float capturedDistanceDots;
        private string pointASource = "--";
        private string pointBSource = "--";

        private Button connectButton;
        private Text connectButtonLabel;
        private Text connectionStatusLabel;
        private Text pointStatusLabel;
        private Text distanceStatusLabel;
        private Text captureStatusLabel;

        private GameObject worldRoot;
        private GameObject liveMarkerA;
        private GameObject liveMarkerB;
        private GameObject pointMarkerA;
        private GameObject pointMarkerB;
        private GameObject distanceBar;
        private GameObject distanceGlowBar;
        private GameObject matPlane;

        private Material matMaterial;
        private Material liveAMaterial;
        private Material liveBMaterial;
        private Material pointAMaterial;
        private Material pointBMaterial;
        private Material distanceMaterial;
        private Material distanceGlowMaterial;

        private bool AreCubesConnected =>
            cubeA != null && cubeA.isConnected &&
            cubeB != null && cubeB.isConnected;

        private void Awake()
        {
            Application.runInBackground = true;
            cubeAListenerKey = $"{nameof(ToioDistanceUnityLabController)}_A_{GetInstanceID()}";
            cubeBListenerKey = $"{nameof(ToioDistanceUnityLabController)}_B_{GetInstanceID()}";

            EnsureEventSystem();
            EnsureCamera();
            EnsureCanvas();
            EnsureWorld();
        }

        private void Start()
        {
            BuildUi();
            RefreshRuntimeUi();
            RefreshVisualization();
        }

        private void Update()
        {
            RefreshLivePointsFromCubes();
            CaptureHeldButtonsIfNeeded();
            RefreshRuntimeUi();
            RefreshVisualization();
        }

        private void OnDestroy()
        {
            RemoveListeners();
            cubeManager?.DisconnectAll();
        }

        public async void OnConnectCubes()
        {
            if (isConnecting || AreCubesConnected)
            {
                return;
            }

            isConnecting = true;
            RefreshRuntimeUi();

            try
            {
                var cubes = await ConnectCubePair();
                if (cubes == null || cubes.Length < 2)
                {
                    connectionMessage = "Two cubes were not confirmed. Keep both cubes near the PC and press Connect again.";
                    return;
                }

                cubeA = cubes[0];
                cubeB = cubes[1];
                RegisterCube(cubeA, cubeAListenerKey, OnCubeAId, OnCubeAButton);
                RegisterCube(cubeB, cubeBListenerKey, OnCubeBId, OnCubeBButton);
                await UniTask.WhenAll(
                    cubeA.ConfigIDNotification(idNotificationIntervalMs, Cube.IDNotificationType.Balanced),
                    cubeB.ConfigIDNotification(idNotificationIntervalMs, Cube.IDNotificationType.Balanced)
                );

                RefreshLivePointsFromCubes();
                connectionMessage = $"Connected. Cube A={GetCubeLabel(cubeA, "cubeA")} captures A. Cube B={GetCubeLabel(cubeB, "cubeB")} captures B.";
            }
            catch (Exception ex)
            {
                connectionMessage = $"Connection failed: {ex.Message}";
                Debug.LogException(ex);
            }
            finally
            {
                isConnecting = false;
                RefreshRuntimeUi();
            }
        }

        public void OnClearPoints()
        {
            hasPointA = false;
            hasPointB = false;
            capturedDistanceDots = 0f;
            pointASource = "--";
            pointBSource = "--";
            RefreshVisualization();
            RefreshRuntimeUi();
        }

        public void OnBackToLauncher()
        {
            SceneManager.LoadScene(LauncherSceneName);
        }

        public void OnOpenBlenderLab()
        {
            SceneManager.LoadScene(BlenderLabSceneName);
        }

        private async UniTask<Cube[]> ConnectCubePair()
        {
            cubeManager ??= new CubeManager(ResolveConnectType());
            var attemptCount = Mathf.Max(1, connectMaxAttempts);

            for (var attempt = 1; attempt <= attemptCount; attempt++)
            {
                connectionMessage = attempt == 1
                    ? "Scanning and connecting two cubes..."
                    : $"Retrying twin connection ({attempt}/{attemptCount})...";
                RefreshRuntimeUi();

                try
                {
                    await cubeManager.MultiConnect(2);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                var connected = GetOrderedConnectedCubes();
                if (connected.Length >= 2)
                {
                    return connected;
                }

                DisconnectAllImmediate();
                if (attempt < attemptCount)
                {
                    await UniTask.Delay(retryDelayMs);
                }
            }

            return null;
        }

        private ConnectType ResolveConnectType()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (connectType == ConnectType.Auto)
            {
                return ConnectType.Real;
            }
#endif
            return connectType;
        }

        private Cube[] GetOrderedConnectedCubes()
        {
            if (cubeManager == null)
            {
                return Array.Empty<Cube>();
            }

            return cubeManager.connectedCubes
                .Where(cube => cube != null && cube.isConnected)
                .GroupBy(cube => cube.addr)
                .Select(group => group.First())
                .OrderBy(cube => cube.addr)
                .Take(2)
                .ToArray();
        }

        private void RegisterCube(Cube cube, string listenerKey, Action<Cube> idHandler, Action<Cube> buttonHandler)
        {
            if (cube == null)
            {
                return;
            }

            cube.idCallback.RemoveListener(listenerKey);
            cube.buttonCallback.RemoveListener(listenerKey);
            cube.idCallback.AddListener(listenerKey, idHandler);
            cube.buttonCallback.AddListener(listenerKey, buttonHandler);
        }

        private void RemoveListeners()
        {
            if (cubeA != null)
            {
                cubeA.idCallback.RemoveListener(cubeAListenerKey);
                cubeA.buttonCallback.RemoveListener(cubeAListenerKey);
            }

            if (cubeB != null)
            {
                cubeB.idCallback.RemoveListener(cubeBListenerKey);
                cubeB.buttonCallback.RemoveListener(cubeBListenerKey);
            }
        }

        private void DisconnectAllImmediate()
        {
            RemoveListeners();
            cubeManager?.DisconnectAll();
            cubeA = null;
            cubeB = null;
            cubeAButtonPressed = false;
            cubeBButtonPressed = false;
            hasLiveA = false;
            hasLiveB = false;
            pointASource = "--";
            pointBSource = "--";
        }

        private void OnCubeAId(Cube cube)
        {
            CaptureLivePoint(cube, ref livePointA, ref hasLiveA);
        }

        private void OnCubeBId(Cube cube)
        {
            CaptureLivePoint(cube, ref livePointB, ref hasLiveB);
        }

        private void OnCubeAButton(Cube cube)
        {
            var wasPressed = cubeAButtonPressed;
            cubeAButtonPressed = cube.isPressed;
            if (cubeAButtonPressed && !wasPressed)
            {
                CapturePointA(cube);
            }
        }

        private void OnCubeBButton(Cube cube)
        {
            var wasPressed = cubeBButtonPressed;
            cubeBButtonPressed = cube.isPressed;
            if (cubeBButtonPressed && !wasPressed)
            {
                CapturePointB(cube);
            }
        }

        private void RefreshLivePointsFromCubes()
        {
            if (cubeA != null && cubeA.isConnected)
            {
                CaptureLivePoint(cubeA, ref livePointA, ref hasLiveA);
            }

            if (cubeB != null && cubeB.isConnected)
            {
                CaptureLivePoint(cubeB, ref livePointB, ref hasLiveB);
            }
        }

        private void CaptureHeldButtonsIfNeeded()
        {
            if (cubeAButtonPressed && !hasPointA)
            {
                CapturePointA(cubeA);
            }

            if (cubeBButtonPressed && !hasPointB)
            {
                CapturePointB(cubeB);
            }
        }

        private void CapturePointA(Cube cube)
        {
            if (CaptureLivePoint(cube, ref livePointA, ref hasLiveA))
            {
                pointA = livePointA;
                pointASource = "mat";
            }
            else if (useFallbackPointsWhenMatIdMissing)
            {
                pointA = new Vector2(matCenter.x - 120f, matCenter.y + 55f);
                pointASource = "fallback";
            }
            else
            {
                return;
            }

            hasPointA = true;
            RecalculateDistance();
        }

        private void CapturePointB(Cube cube)
        {
            if (CaptureLivePoint(cube, ref livePointB, ref hasLiveB))
            {
                pointB = livePointB;
                pointBSource = "mat";
            }
            else if (useFallbackPointsWhenMatIdMissing)
            {
                pointB = new Vector2(matCenter.x + 125f, matCenter.y - 80f);
                pointBSource = "fallback";
            }
            else
            {
                return;
            }

            hasPointB = true;
            RecalculateDistance();
        }

        private static bool CaptureLivePoint(Cube cube, ref Vector2 target, ref bool hasTarget)
        {
            if (cube == null)
            {
                return false;
            }

            var pos = cube.pos;
            if (pos.x <= 0 || pos.y <= 0)
            {
                return false;
            }

            target = pos;
            hasTarget = true;
            return true;
        }

        private void RecalculateDistance()
        {
            capturedDistanceDots = hasPointA && hasPointB ? Vector2.Distance(pointA, pointB) : 0f;
        }

        private void EnsureWorld()
        {
            worldRoot = new GameObject("DistanceWorld");
            matMaterial = CreateMaterial("MAT_Distance_Mat", new Color(0.12f, 0.15f, 0.16f, 1f));
            liveAMaterial = CreateMaterial("MAT_Live_A", new Color(0.3f, 0.78f, 1f, 0.55f));
            liveBMaterial = CreateMaterial("MAT_Live_B", new Color(1f, 0.66f, 0.32f, 0.55f));
            pointAMaterial = CreateMaterial("MAT_Point_A", AccentAColor);
            pointBMaterial = CreateMaterial("MAT_Point_B", AccentBColor);
            distanceMaterial = CreateMaterial("MAT_Distance_Bar", DistanceColor);
            distanceGlowMaterial = CreateMaterial("MAT_Distance_Glow", new Color(0.72f, 1f, 0.86f, 0.74f));

            matPlane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            matPlane.name = "MatReference";
            matPlane.transform.SetParent(worldRoot.transform, false);
            matPlane.transform.position = new Vector3(0f, -0.05f, 0f);
            matPlane.transform.localScale = new Vector3(10.5f, 0.05f, 10.5f);
            SetMaterial(matPlane, matMaterial);

            liveMarkerA = CreateMarker("LiveCubeA", PrimitiveType.Sphere, liveAMaterial, liveCubeMarkerDiameter);
            liveMarkerB = CreateMarker("LiveCubeB", PrimitiveType.Sphere, liveBMaterial, liveCubeMarkerDiameter);
            pointMarkerA = CreateMarker("PointA", PrimitiveType.Cylinder, pointAMaterial, markerDiameter);
            pointMarkerB = CreateMarker("PointB", PrimitiveType.Cylinder, pointBMaterial, markerDiameter);

            distanceBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            distanceBar.name = "DistanceBar";
            distanceBar.transform.SetParent(worldRoot.transform, false);
            SetMaterial(distanceBar, distanceMaterial);

            distanceGlowBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            distanceGlowBar.name = "DistanceBarHighlight";
            distanceGlowBar.transform.SetParent(worldRoot.transform, false);
            SetMaterial(distanceGlowBar, distanceGlowMaterial);

            var lightObject = new GameObject("KeyLight");
            lightObject.transform.SetParent(worldRoot.transform, false);
            lightObject.transform.position = new Vector3(-4f, 8f, -6f);
            lightObject.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            light.color = new Color(0.9f, 0.98f, 1f, 1f);
        }

        private GameObject CreateMarker(string name, PrimitiveType primitiveType, Material material, float diameter)
        {
            var marker = GameObject.CreatePrimitive(primitiveType);
            marker.name = name;
            marker.transform.SetParent(worldRoot.transform, false);
            marker.transform.localScale = new Vector3(diameter, markerHeight, diameter);
            SetMaterial(marker, material);
            return marker;
        }

        private Material CreateMaterial(string name, Color color)
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = name;
            material.color = color;
            material.SetFloat("_Glossiness", 0.42f);
            return material;
        }

        private static void SetMaterial(GameObject obj, Material material)
        {
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = material;
            }
        }

        private void RefreshVisualization()
        {
            UpdateMarker(liveMarkerA, hasLiveA, livePointA, 0.22f);
            UpdateMarker(liveMarkerB, hasLiveB, livePointB, 0.22f);
            UpdateMarker(pointMarkerA, hasPointA, pointA, 0.48f);
            UpdateMarker(pointMarkerB, hasPointB, pointB, 0.48f);
            UpdateDistanceBar();
        }

        private void UpdateMarker(GameObject marker, bool visible, Vector2 matPoint, float y)
        {
            if (marker == null)
            {
                return;
            }

            marker.SetActive(visible);
            if (visible)
            {
                marker.transform.position = MatToWorld(matPoint, y);
            }
        }

        private void UpdateDistanceBar()
        {
            var hasDistance = hasPointA && hasPointB && capturedDistanceDots > 0.01f;
            distanceBar.SetActive(hasDistance);
            distanceGlowBar.SetActive(hasDistance);
            if (!hasDistance)
            {
                return;
            }

            var worldA = MatToWorld(pointA, 0.6f);
            var worldB = MatToWorld(pointB, 0.6f);
            var delta = worldB - worldA;
            var length = delta.magnitude;
            var mid = (worldA + worldB) * 0.5f;
            var rotation = Quaternion.FromToRotation(Vector3.right, delta.normalized);

            distanceBar.transform.position = mid;
            distanceBar.transform.rotation = rotation;
            distanceBar.transform.localScale = new Vector3(length, distanceBarHeight, distanceBarThickness);

            distanceGlowBar.transform.position = mid + Vector3.up * 0.18f;
            distanceGlowBar.transform.rotation = rotation;
            distanceGlowBar.transform.localScale = new Vector3(length, 0.06f, distanceBarThickness * 1.35f);
        }

        private Vector3 MatToWorld(Vector2 matPoint, float y)
        {
            var centered = matPoint - matCenter;
            return new Vector3(centered.x * matToUnityScale, y, -centered.y * matToUnityScale);
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

            var header = CreatePanel("HeaderPanel", root.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(940f, 122f), PanelColor);
            CreateText("Eyebrow", header.transform, "toio x Unity | ToioJetHand", 19, FontStyle.Bold, TextAnchor.UpperCenter, AccentAColor, new Vector2(0f, 34f), new Vector2(760f, 26f));
            CreateText("Title", header.transform, "ToioDistanceUnityLab", 34, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor, new Vector2(0f, 2f), new Vector2(820f, 44f));
            CreateText("Subtitle", header.transform, "Press Cube A, press Cube B, and Unity turns the two points into a visible distance cube.", 18, FontStyle.Normal, TextAnchor.LowerCenter, MutedTextColor, new Vector2(0f, -32f), new Vector2(860f, 28f));

            var statusCard = CreatePanel("StatusCard", root.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 24f), new Vector2(542f, 246f), CardColor);
            CreateText("StatusTitle", statusCard.transform, $"Today: Unity distance visualization {VersionLabel}", 22, FontStyle.Bold, TextAnchor.UpperLeft, TextColor, new Vector2(24f, -20f), new Vector2(486f, 30f), true);
            connectButton = CreateButton("ButtonConnectCubes", statusCard.transform, "Connect Cubes", new Vector2(24f, -64f), new Vector2(210f, 48f), AccentAColor, OnConnectCubes, true);
            connectButtonLabel = connectButton.GetComponentInChildren<Text>();
            CreateButton("ButtonClearPoints", statusCard.transform, "Clear Points", new Vector2(254f, -64f), new Vector2(180f, 48f), AccentBColor, OnClearPoints, true);
            connectionStatusLabel = CreateText("ConnectionStatus", statusCard.transform, string.Empty, 17, FontStyle.Normal, TextAnchor.UpperLeft, TextColor, new Vector2(24f, -120f), new Vector2(494f, 40f), true);
            pointStatusLabel = CreateText("PointStatus", statusCard.transform, string.Empty, 17, FontStyle.Normal, TextAnchor.UpperLeft, MutedTextColor, new Vector2(24f, -166f), new Vector2(494f, 42f), true);
            distanceStatusLabel = CreateText("DistanceStatus", statusCard.transform, string.Empty, 22, FontStyle.Bold, TextAnchor.UpperLeft, DistanceColor, new Vector2(24f, -210f), new Vector2(494f, 30f), true);

            var guideCard = CreatePanel("GuideCard", root.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(420f, 246f), CardColor);
            CreateText("GuideTitle", guideCard.transform, "Capture Flow", 22, FontStyle.Bold, TextAnchor.UpperLeft, TextColor, new Vector2(22f, -20f), new Vector2(360f, 30f), true);
            captureStatusLabel = CreateText("CaptureStatus", guideCard.transform, string.Empty, 18, FontStyle.Normal, TextAnchor.UpperLeft, MutedTextColor, new Vector2(22f, -62f), new Vector2(360f, 110f), true);
            CreateButton("ButtonLauncher", guideCard.transform, "Back To Launcher", new Vector2(22f, -188f), new Vector2(176f, 44f), AccentAColor, OnBackToLauncher, true);
            CreateButton("ButtonBlenderLab", guideCard.transform, "Open BlenderLab", new Vector2(214f, -188f), new Vector2(176f, 44f), AccentBColor, OnOpenBlenderLab, true);
        }

        private void RefreshRuntimeUi()
        {
            if (connectButton != null)
            {
                connectButton.interactable = !isConnecting && !AreCubesConnected;
            }

            if (connectButtonLabel != null)
            {
                connectButtonLabel.text = isConnecting ? "Connecting..." : AreCubesConnected ? "Cubes Connected" : "Connect Cubes";
            }

            if (connectionStatusLabel != null)
            {
                connectionStatusLabel.text = $"Connection: {connectionMessage}";
            }

            if (pointStatusLabel != null)
            {
                pointStatusLabel.text =
                    $"Live A: {FormatPoint(livePointA, hasLiveA)} / captured A: {FormatPoint(pointA, hasPointA)} [{pointASource}]\n" +
                    $"Live B: {FormatPoint(livePointB, hasLiveB)} / captured B: {FormatPoint(pointB, hasPointB)} [{pointBSource}]";
            }

            if (distanceStatusLabel != null)
            {
                distanceStatusLabel.text = hasPointA && hasPointB
                    ? $"Distance: {capturedDistanceDots:F1} mat dots"
                    : "Distance: capture A and B";
            }

            if (captureStatusLabel != null)
            {
                captureStatusLabel.text =
                    "1. Put both cubes on the mat.\n" +
                    "2. Press Cube A to lock point A.\n" +
                    "3. Press Cube B to lock point B.\n" +
                    "4. The green cube shows the distance.\n\n" +
                    $"Buttons: A={(cubeAButtonPressed ? "ON" : "off")} / B={(cubeBButtonPressed ? "ON" : "off")}";
            }
        }

        private static string FormatPoint(Vector2 point, bool hasPoint)
        {
            return hasPoint ? $"({point.x:F0}, {point.y:F0})" : "--";
        }

        private static string GetCubeLabel(Cube cube, string fallback)
        {
            if (cube == null)
            {
                return fallback;
            }

            return string.IsNullOrEmpty(cube.addr) ? fallback : cube.addr;
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
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
                camera.tag = "MainCamera";
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            camera.orthographic = true;
            camera.orthographicSize = 7.2f;
            camera.transform.position = new Vector3(0f, 11f, -8f);
            camera.transform.rotation = Quaternion.Euler(56f, 0f, 0f);
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

        private static Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 sizeDelta, Color color, UnityEngine.Events.UnityAction onClick, bool anchorTopLeft = false)
        {
            var buttonObject = CreateUiObject(name, parent);
            var rect = buttonObject.GetComponent<RectTransform>();
            ConfigureRect(rect, anchorTopLeft, anchoredPosition, sizeDelta);
            var image = buttonObject.AddComponent<Image>();
            image.color = color;
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            CreateText("Label", buttonObject.transform, label, 18, FontStyle.Bold, TextAnchor.MiddleCenter, BackgroundColor, Vector2.zero, sizeDelta - new Vector2(18f, 12f));
            return button;
        }

        private static Text CreateText(string name, Transform parent, string text, int fontSize, FontStyle fontStyle, TextAnchor anchor, Color color, Vector2 anchoredPosition, Vector2 sizeDelta, bool anchorTopLeft = false)
        {
            var textObject = CreateUiObject(name, parent);
            var rect = textObject.GetComponent<RectTransform>();
            ConfigureRect(rect, anchorTopLeft, anchoredPosition, sizeDelta);
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

        private static void ConfigureRect(RectTransform rect, bool anchorTopLeft, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            if (anchorTopLeft)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
            }

            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
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
