using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using toio;

namespace toio.Experiments.ToioTacticalField
{
    [DisallowMultipleComponent]
    public class ToioTacticalFieldController : MonoBehaviour
    {
        private const string LauncherSceneName = "ToioLauncher";
        private const string RootName = "ToioTacticalFieldRoot";
        private const string VersionLabel = "Phase 2.0";
        private const int TacticalFieldColumns = 5;
        private const int TacticalFieldRows = 7;

        private static readonly Color BackgroundColor = new Color(0.055f, 0.075f, 0.07f, 1f);
        private static readonly Color PanelColor = new Color(0.105f, 0.145f, 0.13f, 0.97f);
        private static readonly Color CardColor = new Color(0.075f, 0.11f, 0.1f, 0.97f);
        private static readonly Color StartColor = new Color(0.38f, 0.9f, 0.7f, 1f);
        private static readonly Color GoalColor = new Color(0.95f, 0.72f, 0.34f, 1f);
        private static readonly Color LineColor = new Color(0.58f, 0.76f, 0.66f, 1f);
        private static readonly Color GridColor = new Color(0.2f, 0.54f, 0.42f, 0.76f);
        private static readonly Color GridStartColor = new Color(0.28f, 0.78f, 0.58f, 0.9f);
        private static readonly Color GridGoalColor = new Color(0.92f, 0.62f, 0.24f, 0.9f);
        private static readonly Color TextColor = new Color(0.93f, 0.98f, 0.94f, 1f);
        private static readonly Color MutedTextColor = new Color(0.68f, 0.78f, 0.7f, 1f);

        [Header("Connection")]
        [SerializeField] private ConnectType connectType = ConnectType.Real;
        [SerializeField] private int connectMaxAttempts = 3;
        [SerializeField] private int retryDelayMs = 1200;
        [SerializeField] private int idNotificationIntervalMs = 50;

        [Header("Mat To Unity View")]
        [SerializeField] private Vector2 matCenter = new Vector2(250f, 250f);
        [SerializeField] private float matToUnityScale = 0.025f;
        [SerializeField] private float anchorHeight = 0.38f;
        [SerializeField] private float anchorDiameter = 0.82f;
        [SerializeField] private float observationDiameter = 0.38f;
        [SerializeField] private bool useFallbackPointsWhenMatIdMissing = true;

        [Header("Tactical Field Convert")]
        [SerializeField] private float tacticalCellHeight = 0.12f;
        [SerializeField] private float tacticalCellFillRatio = 0.88f;

        [Header("Straight Transporter Victory")]
        [SerializeField] private float transporterStartToleranceMatDots = 28f;
        [SerializeField] private float transporterGoalToleranceMatDots = 18f;
        [SerializeField] private int transporterMaxSpeed = 70;

        private CubeManager cubeManager;
        private Cube observationCube;
        private string observationListenerKey;
        private bool isConnecting;
        private bool cubeButtonPressed;
        private bool hasLivePoint;
        private bool hasStartAnchor;
        private bool hasGoalAnchor;
        private bool isTransporterMoving;
        private bool transporterGoalReached;
        private Vector2 livePoint;
        private Vector2 startAnchor;
        private Vector2 goalAnchor;
        private string startSource = "--";
        private string goalSource = "--";
        private string connectionMessage = "Not connected. Press Connect Observation Cube.";
        private string observationMessage = "Awaiting start anchor observation.";
        private string tacticalFieldMessage = "Field: awaiting anchor conversion.";

        private Text connectionStatusLabel;
        private Text observationStatusLabel;
        private Text anchorStatusLabel;
        private Text victoryStatusLabel;
        private GameObject observationMarker;
        private GameObject startMarker;
        private GameObject goalMarker;
        private GameObject observationLine;
        private Transform tacticalFieldRoot;
        private readonly List<GameObject> tacticalFieldCells = new List<GameObject>();
        private Vector2[] tacticalCellPoints = Array.Empty<Vector2>();

        private bool IsCubeConnected => observationCube != null && observationCube.isConnected;

        private void Awake()
        {
            Application.runInBackground = true;
            observationListenerKey = $"{nameof(ToioTacticalFieldController)}_{GetInstanceID()}";
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
            RefreshLivePoint();
            RefreshTransporterArrival();
            RefreshRuntimeUi();
            RefreshVisualization();
        }

        private void OnDestroy()
        {
            RemoveListeners();
            cubeManager?.DisconnectAll();
        }

        public async void OnConnectObservationCube()
        {
            if (isConnecting || IsCubeConnected)
            {
                return;
            }

            isConnecting = true;
            RefreshRuntimeUi();
            try
            {
                observationCube = await ConnectCube();
                if (observationCube == null)
                {
                    connectionMessage = "Observation cube was not confirmed. Keep one cube near the PC and press Connect again.";
                    return;
                }

                RegisterCube(observationCube);
                await observationCube.ConfigIDNotification(idNotificationIntervalMs, Cube.IDNotificationType.Balanced);
                RefreshLivePoint();
                connectionMessage = $"Connected observation cube: {GetCubeLabel(observationCube)}";
                observationMessage = "Move the cube to the start anchor, then press its button.";
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

        public void OnCaptureCurrentAnchor()
        {
            CaptureNextAnchor(observationCube);
        }

        public void OnClearAnchors()
        {
            hasStartAnchor = false;
            hasGoalAnchor = false;
            isTransporterMoving = false;
            transporterGoalReached = false;
            ClearTacticalField();
            startSource = "--";
            goalSource = "--";
            observationMessage = "Awaiting start anchor observation.";
            RefreshRuntimeUi();
            RefreshVisualization();
        }

        public void OnConvertTacticalField()
        {
            if (!hasStartAnchor || !hasGoalAnchor)
            {
                tacticalFieldMessage = "Field: capture start and goal anchors before conversion.";
                RefreshRuntimeUi();
                return;
            }

            var axis = goalAnchor - startAnchor;
            if (axis.sqrMagnitude <= Mathf.Epsilon)
            {
                tacticalFieldMessage = "Field: start and goal anchors must be different points.";
                RefreshRuntimeUi();
                return;
            }

            tacticalCellPoints = GenerateTacticalCellPoints(startAnchor, goalAnchor);
            RenderTacticalField(tacticalCellPoints);
            tacticalFieldMessage = $"TACTICAL FIELD CONVERTED | {TacticalFieldColumns} x {TacticalFieldRows}";
            observationMessage = "Observed axis converted into the Ordia tactical field.";
            RefreshRuntimeUi();
        }

        public void OnRunTransporter()
        {
            if (!IsCubeConnected)
            {
                observationMessage = "Connect the observation cube before running the Transporter.";
                return;
            }

            if (!hasStartAnchor || !hasGoalAnchor)
            {
                observationMessage = "Capture the start and goal anchors before running the Transporter.";
                return;
            }

            if (!CaptureLivePoint(observationCube))
            {
                observationMessage = "No readable mat position. Place the cube at the observed start anchor.";
                return;
            }

            var startDistance = Vector2.Distance(livePoint, startAnchor);
            if (startDistance > transporterStartToleranceMatDots)
            {
                observationMessage = $"Return the same cube to the start anchor before running. Distance: {startDistance:F1} mat dots.";
                return;
            }

            isTransporterMoving = true;
            transporterGoalReached = false;
            observationMessage = "Transporter advancing directly to the observed goal...";
            observationCube.TargetMove(
                Mathf.RoundToInt(goalAnchor.x),
                Mathf.RoundToInt(goalAnchor.y),
                observationCube.angle,
                configID: 1,
                targetMoveType: Cube.TargetMoveType.RoundBeforeMove,
                maxSpd: transporterMaxSpeed
            );
            RefreshRuntimeUi();
        }

        public void OnBackToLauncher()
        {
            SceneManager.LoadScene(LauncherSceneName);
        }

        private async UniTask<Cube> ConnectCube()
        {
            cubeManager ??= new CubeManager(ResolveConnectType());
            var attemptCount = Mathf.Max(1, connectMaxAttempts);
            for (var attempt = 1; attempt <= attemptCount; attempt++)
            {
                connectionMessage = attempt == 1
                    ? "Scanning and connecting one observation cube..."
                    : $"Retrying observation cube connection ({attempt}/{attemptCount})...";
                RefreshRuntimeUi();

                try
                {
                    await cubeManager.MultiConnect(1);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                var connected = cubeManager.connectedCubes
                    .Where(cube => cube != null && cube.isConnected)
                    .GroupBy(cube => cube.addr)
                    .Select(group => group.First())
                    .OrderBy(cube => cube.addr)
                    .FirstOrDefault();
                if (connected != null)
                {
                    return connected;
                }

                cubeManager.DisconnectAll();
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

        private void RegisterCube(Cube cube)
        {
            RemoveListeners();
            cube.idCallback.AddListener(observationListenerKey, OnCubeId);
            cube.buttonCallback.AddListener(observationListenerKey, OnCubeButton);
        }

        private void RemoveListeners()
        {
            if (observationCube == null)
            {
                return;
            }

            observationCube.idCallback.RemoveListener(observationListenerKey);
            observationCube.buttonCallback.RemoveListener(observationListenerKey);
        }

        private void OnCubeId(Cube cube)
        {
            CaptureLivePoint(cube);
        }

        private void OnCubeButton(Cube cube)
        {
            var wasPressed = cubeButtonPressed;
            cubeButtonPressed = cube.isPressed;
            if (cubeButtonPressed && !wasPressed)
            {
                CaptureNextAnchor(cube);
            }
        }

        private void RefreshLivePoint()
        {
            CaptureLivePoint(observationCube);
        }

        private bool CaptureLivePoint(Cube cube)
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

            livePoint = pos;
            hasLivePoint = true;
            return true;
        }

        private void CaptureNextAnchor(Cube cube)
        {
            var source = "mat";
            var point = livePoint;
            if (!CaptureLivePoint(cube))
            {
                if (!useFallbackPointsWhenMatIdMissing)
                {
                    observationMessage = "No readable mat position. Place the cube on a compatible mat and try again.";
                    return;
                }

                source = "fallback";
                point = !hasStartAnchor || hasGoalAnchor
                    ? new Vector2(matCenter.x - 120f, matCenter.y + 55f)
                    : new Vector2(matCenter.x + 125f, matCenter.y - 80f);
            }
            else
            {
                point = livePoint;
            }

            if (!hasStartAnchor || hasGoalAnchor)
            {
                ClearTacticalField();
                startAnchor = point;
                startSource = source;
                hasStartAnchor = true;
                hasGoalAnchor = false;
                goalSource = "--";
                observationMessage = "Start anchor locked. Move the cube to the goal anchor, then press its button.";
            }
            else
            {
                ClearTacticalField();
                goalAnchor = point;
                goalSource = source;
                hasGoalAnchor = true;
                isTransporterMoving = false;
                transporterGoalReached = false;
                observationMessage = "Goal anchor locked. Return the same cube to start, then press Run Transporter.";
            }

            RefreshRuntimeUi();
            RefreshVisualization();
        }

        private void RefreshTransporterArrival()
        {
            if (!isTransporterMoving || !hasLivePoint || !hasGoalAnchor)
            {
                return;
            }

            if (Vector2.Distance(livePoint, goalAnchor) > transporterGoalToleranceMatDots)
            {
                return;
            }

            isTransporterMoving = false;
            transporterGoalReached = true;
            observationMessage = "GOAL REACHED. Straight Transporter victory confirmed.";
        }

        private void EnsureWorld()
        {
            var worldRoot = new GameObject("TacticalFieldWorld");
            var mat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mat.name = "ObservationMat";
            mat.transform.SetParent(worldRoot.transform);
            mat.transform.position = Vector3.zero;
            mat.transform.localScale = new Vector3(12.5f, 0.08f, 12.5f);
            mat.GetComponent<Renderer>().material = CreateMaterial("MAT_TacticalField", new Color(0.1f, 0.15f, 0.14f, 1f));

            observationMarker = CreateMarker("ObservationCube", worldRoot.transform, new Color(0.72f, 0.9f, 0.84f, 0.7f), observationDiameter, 0.24f);
            startMarker = CreateMarker("StartAnchor", worldRoot.transform, StartColor, anchorDiameter, anchorHeight);
            goalMarker = CreateMarker("GoalAnchor", worldRoot.transform, GoalColor, anchorDiameter, anchorHeight);
            observationLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            observationLine.name = "ObservedAxis";
            observationLine.transform.SetParent(worldRoot.transform);
            observationLine.GetComponent<Renderer>().material = CreateMaterial("MAT_ObservedAxis", LineColor);
            tacticalFieldRoot = new GameObject("ConvertedTacticalField").transform;
            tacticalFieldRoot.SetParent(worldRoot.transform);
        }

        private Vector2[] GenerateTacticalCellPoints(Vector2 start, Vector2 goal)
        {
            var forward = (goal - start).normalized;
            var right = new Vector2(-forward.y, forward.x);
            var cellSize = Vector2.Distance(start, goal) / TacticalFieldRows;
            var points = new Vector2[TacticalFieldColumns * TacticalFieldRows];
            var index = 0;
            for (var row = 0; row < TacticalFieldRows; row++)
            {
                for (var column = 0; column < TacticalFieldColumns; column++)
                {
                    var centeredColumn = column - (TacticalFieldColumns - 1) * 0.5f;
                    points[index++] =
                        start +
                        forward * ((row + 0.5f) * cellSize) +
                        right * (centeredColumn * cellSize);
                }
            }

            return points;
        }

        private void RenderTacticalField(Vector2[] points)
        {
            DestroyTacticalFieldCells();
            var cellSize = Vector2.Distance(startAnchor, goalAnchor) / TacticalFieldRows;
            var worldCellSize = cellSize * matToUnityScale * tacticalCellFillRatio;
            var worldForward = MatToWorld(goalAnchor, tacticalCellHeight) - MatToWorld(startAnchor, tacticalCellHeight);
            var rotation = Quaternion.LookRotation(worldForward);
            for (var index = 0; index < points.Length; index++)
            {
                var row = index / TacticalFieldColumns;
                var cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cell.name = $"TacticalCell_{index % TacticalFieldColumns}_{row}";
                cell.transform.SetParent(tacticalFieldRoot);
                cell.transform.position = MatToWorld(points[index], tacticalCellHeight);
                cell.transform.rotation = rotation;
                cell.transform.localScale = new Vector3(worldCellSize, tacticalCellHeight, worldCellSize);
                var color = row == 0 ? GridStartColor : row == TacticalFieldRows - 1 ? GridGoalColor : GridColor;
                cell.GetComponent<Renderer>().material = CreateMaterial($"MAT_{cell.name}", color);
                tacticalFieldCells.Add(cell);
            }
        }

        private void ClearTacticalField()
        {
            DestroyTacticalFieldCells();
            tacticalCellPoints = Array.Empty<Vector2>();
            tacticalFieldMessage = "Field: awaiting anchor conversion.";
        }

        private void DestroyTacticalFieldCells()
        {
            foreach (var cell in tacticalFieldCells)
            {
                if (cell != null)
                {
                    Destroy(cell);
                }
            }

            tacticalFieldCells.Clear();
        }

        private void RefreshVisualization()
        {
            observationMarker.SetActive(hasLivePoint);
            if (hasLivePoint)
            {
                observationMarker.transform.position = MatToWorld(livePoint, 0.28f);
            }

            startMarker.SetActive(hasStartAnchor);
            goalMarker.SetActive(hasGoalAnchor);
            observationLine.SetActive(hasStartAnchor && hasGoalAnchor);
            if (hasStartAnchor)
            {
                startMarker.transform.position = MatToWorld(startAnchor, anchorHeight);
            }

            if (!hasGoalAnchor)
            {
                return;
            }

            goalMarker.transform.position = MatToWorld(goalAnchor, anchorHeight);
            var worldStart = MatToWorld(startAnchor, 0.18f);
            var worldGoal = MatToWorld(goalAnchor, 0.18f);
            var midpoint = (worldStart + worldGoal) * 0.5f;
            var distance = Vector3.Distance(worldStart, worldGoal);
            observationLine.transform.position = midpoint;
            observationLine.transform.localScale = new Vector3(0.16f, 0.1f, distance);
            observationLine.transform.rotation = Quaternion.LookRotation(worldGoal - worldStart);
        }

        private Vector3 MatToWorld(Vector2 matPoint, float height)
        {
            return new Vector3(
                (matPoint.x - matCenter.x) * matToUnityScale,
                height,
                (matCenter.y - matPoint.y) * matToUnityScale
            );
        }

        private void BuildUi()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null || GameObject.Find(RootName) != null)
            {
                return;
            }

            var root = CreateUiObject(RootName, canvas.transform);
            StretchFull(root.GetComponent<RectTransform>());
            root.AddComponent<Image>().color = new Color(BackgroundColor.r, BackgroundColor.g, BackgroundColor.b, 0.62f);

            var header = CreatePanel("Header", root.transform, new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(900f, 104f), PanelColor);
            CreateText("Title", header.transform, "toio Tactical Field | Ordia Deskfront", 31, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor, new Vector2(0f, 15f), new Vector2(820f, 40f));
            CreateText("Phase", header.transform, $"{VersionLabel} | Tactical Field Convert", 18, FontStyle.Bold, TextAnchor.MiddleCenter, StartColor, new Vector2(0f, -23f), new Vector2(820f, 26f));

            var status = CreatePanel("Status", root.transform, new Vector2(0f, 1f), new Vector2(24f, -184f), new Vector2(450f, 382f), CardColor, true);
            connectionStatusLabel = CreateText("Connection", status.transform, string.Empty, 16, FontStyle.Bold, TextAnchor.UpperLeft, TextColor, new Vector2(20f, -18f), new Vector2(410f, 58f), true);
            observationStatusLabel = CreateText("Observation", status.transform, string.Empty, 18, FontStyle.Bold, TextAnchor.UpperLeft, StartColor, new Vector2(20f, -96f), new Vector2(410f, 72f), true);
            anchorStatusLabel = CreateText("Anchors", status.transform, string.Empty, 16, FontStyle.Normal, TextAnchor.UpperLeft, MutedTextColor, new Vector2(20f, -190f), new Vector2(410f, 112f), true);
            victoryStatusLabel = CreateText("Victory", status.transform, string.Empty, 24, FontStyle.Bold, TextAnchor.UpperLeft, GoalColor, new Vector2(20f, -318f), new Vector2(410f, 40f), true);

            var actions = CreatePanel("Actions", root.transform, new Vector2(1f, 1f), new Vector2(-24f, -184f), new Vector2(330f, 382f), CardColor, false, true);
            CreateButton("Connect", actions.transform, "Connect Observation Cube", new Vector2(0f, -26f), new Vector2(276f, 44f), StartColor, OnConnectObservationCube, true);
            CreateButton("Capture", actions.transform, "Capture Current Anchor", new Vector2(0f, -78f), new Vector2(276f, 44f), GoalColor, OnCaptureCurrentAnchor, true);
            CreateButton("Convert", actions.transform, "Convert Tactical Field", new Vector2(0f, -130f), new Vector2(276f, 44f), GridStartColor, OnConvertTacticalField, true);
            CreateButton("Run", actions.transform, "Run Transporter", new Vector2(0f, -182f), new Vector2(276f, 44f), StartColor, OnRunTransporter, true);
            CreateButton("Clear", actions.transform, "Clear Anchors", new Vector2(0f, -234f), new Vector2(276f, 40f), LineColor, OnClearAnchors, true);
            CreateButton("Back", actions.transform, "Back To Launcher", new Vector2(0f, -282f), new Vector2(276f, 38f), MutedTextColor, OnBackToLauncher, true);
        }

        private void RefreshRuntimeUi()
        {
            if (connectionStatusLabel == null)
            {
                return;
            }

            connectionStatusLabel.text = connectionMessage;
            observationStatusLabel.text = observationMessage;
            anchorStatusLabel.text =
                $"Start: {(hasStartAnchor ? FormatPoint(startAnchor, startSource) : "--")}\n" +
                $"Goal:  {(hasGoalAnchor ? FormatPoint(goalAnchor, goalSource) : "--")}\n" +
                $"Axis:  {(hasStartAnchor && hasGoalAnchor ? $"{Vector2.Distance(startAnchor, goalAnchor):F1} mat dots" : "--")}\n" +
                tacticalFieldMessage;
            victoryStatusLabel.text = transporterGoalReached ? "GOAL REACHED" : string.Empty;
        }

        private static string FormatPoint(Vector2 point, string source)
        {
            return $"({point.x:F1}, {point.y:F1}) [{source}]";
        }

        private static string GetCubeLabel(Cube cube)
        {
            return cube == null || string.IsNullOrEmpty(cube.addr) ? "cube" : cube.addr;
        }

        private static GameObject CreateMarker(string name, Transform parent, Color color, float diameter, float height)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = name;
            marker.transform.SetParent(parent);
            marker.transform.localScale = new Vector3(diameter, height, diameter);
            marker.GetComponent<Renderer>().material = CreateMaterial($"MAT_{name}", color);
            return marker;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            var material = new Material(shader) { name = name, color = color };
            return material;
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var target = new GameObject("EventSystem");
            target.AddComponent<EventSystem>();
            target.AddComponent<StandaloneInputModule>();
        }

        private static void EnsureCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var target = new GameObject("Main Camera");
                camera = target.AddComponent<Camera>();
                target.AddComponent<AudioListener>();
                camera.tag = "MainCamera";
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            camera.orthographic = true;
            camera.orthographicSize = 7.3f;
            camera.transform.position = new Vector3(0f, 11f, -8f);
            camera.transform.rotation = Quaternion.Euler(56f, 0f, 0f);
        }

        private static void EnsureCanvas()
        {
            if (FindObjectOfType<Canvas>() != null)
            {
                return;
            }

            var target = new GameObject("Canvas");
            target.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            target.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            target.AddComponent<GraphicRaycaster>();
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color, bool topLeft = false, bool topRight = false)
        {
            var target = CreateUiObject(name, parent);
            var rect = target.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = topLeft ? new Vector2(0f, 1f) : topRight ? new Vector2(1f, 1f) : new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            target.AddComponent<Image>().color = color;
            return target;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size, Color color, UnityEngine.Events.UnityAction onClick, bool topCenter = false)
        {
            var target = CreateUiObject(name, parent);
            var rect = target.GetComponent<RectTransform>();
            rect.anchorMin = topCenter ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0.5f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = topCenter ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = target.AddComponent<Image>();
            image.color = color;
            var button = target.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            CreateText("Label", target.transform, label, 15, FontStyle.Bold, TextAnchor.MiddleCenter, BackgroundColor, Vector2.zero, size - new Vector2(12f, 8f));
            return button;
        }

        private static Text CreateText(string name, Transform parent, string value, int size, FontStyle style, TextAnchor alignment, Color color, Vector2 position, Vector2 dimensions, bool topLeft = false)
        {
            var target = CreateUiObject(name, parent);
            var rect = target.GetComponent<RectTransform>();
            rect.anchorMin = topLeft ? new Vector2(0f, 1f) : new Vector2(0.5f, 0.5f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = topLeft ? new Vector2(0f, 1f) : new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            var text = target.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            return text;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            return target;
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
