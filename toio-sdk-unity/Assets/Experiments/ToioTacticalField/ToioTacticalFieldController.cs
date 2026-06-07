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
        private const string VersionLabel = "Phase 4";
        private const int TacticalFieldColumns = 5;
        private const int TacticalFieldRows = 7;
        private const int FriendlyRoleCount = 3;

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
        [SerializeField] private float transporterStepToleranceMatDots = 20f;
        [SerializeField] private int transporterMaxSpeed = 70;

        [Header("Phase 4 Friendly Recognition")]
        [SerializeField] private int roleAppealSpeed = 35;
        [SerializeField] private int roleAppealTurnMs = 260;
        [SerializeField] private int roleAppealPauseMs = 110;

        private CubeManager cubeManager;
        private Cube observationCube;
        private Cube scoutCube;
        private Cube builderCube;
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
        private string connectionMessage = "Not connected. Press Connect Friendly Team.";
        private string observationMessage = "Awaiting start anchor observation.";
        private string tacticalFieldMessage = "Field: awaiting anchor conversion.";
        private string roleMessage = "Roles: Transporter / Scout / Builder awaiting connection.";

        private Text connectionStatusLabel;
        private Text roleStatusLabel;
        private Text observationStatusLabel;
        private Text anchorStatusLabel;
        private Text victoryStatusLabel;
        private Text fieldViewStatusLabel;
        private Image rootBackground;
        private GameObject controlView;
        private GameObject fieldView;
        private GameObject observationMarker;
        private GameObject startMarker;
        private GameObject goalMarker;
        private GameObject observationLine;
        private Transform tacticalFieldRoot;
        private readonly List<GameObject> tacticalFieldCells = new List<GameObject>();
        private Vector2[] tacticalCellPoints = Array.Empty<Vector2>();
        private Vector2[] transporterRoutePoints = Array.Empty<Vector2>();
        private int transporterRouteIndex = -1;

        private bool IsCubeConnected => observationCube != null && observationCube.isConnected;
        private bool IsFriendlyTeamConnected =>
            IsCubeConnected &&
            scoutCube != null && scoutCube.isConnected &&
            builderCube != null && builderCube.isConnected;

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
            if (isConnecting || IsFriendlyTeamConnected)
            {
                return;
            }

            isConnecting = true;
            RefreshRuntimeUi();
            try
            {
                var connectedCubes = await ConnectFriendlyTeam();
                if (connectedCubes.Count < FriendlyRoleCount)
                {
                    connectionMessage = "Friendly team was not confirmed. Keep three cubes near the PC and press Connect again.";
                    roleMessage = $"Roles: connected {connectedCubes.Count}/{FriendlyRoleCount}. Scout and Builder behavior remains out of scope.";
                    return;
                }

                observationCube = connectedCubes[0];
                scoutCube = connectedCubes[1];
                builderCube = connectedCubes[2];
                RegisterCube(observationCube);
                foreach (var cube in connectedCubes)
                {
                    await cube.ConfigIDNotification(idNotificationIntervalMs, Cube.IDNotificationType.Balanced);
                }

                RefreshLivePoint();
                connectionMessage = "Friendly team connected. Transporter will keep observation and grid-route duties.";
                roleMessage = FormatRoleMessage();
                await RunRoleAppeal("Transporter", observationCube);
                await RunRoleAppeal("Scout", scoutCube);
                await RunRoleAppeal("Builder", builderCube);
                observationMessage = "Move the Transporter cube to the start anchor, then press its button.";
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
            transporterRouteIndex = -1;
            transporterRoutePoints = Array.Empty<Vector2>();
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
            ResetTransporterRoute();
            tacticalFieldMessage = $"TACTICAL FIELD CONVERTED | {TacticalFieldColumns} x {TacticalFieldRows}";
            observationMessage = "Observed axis converted into the Ordia tactical field.";
            RefreshRuntimeUi();
            SetFieldView(true);
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

            if (tacticalCellPoints.Length == 0)
            {
                observationMessage = "Convert the tactical field before running the Phase 3 grid route.";
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

            transporterRoutePoints = GenerateTransporterRoutePoints();
            transporterRouteIndex = 0;
            isTransporterMoving = true;
            transporterGoalReached = false;
            observationMessage = FormatRouteMessage("GRID ROUTE STARTED");
            CommandCurrentRouteTarget();
            SetFieldView(true);
            RefreshRuntimeUi();
        }

        public void OnBackToLauncher()
        {
            SceneManager.LoadScene(LauncherSceneName);
        }

        public void OnShowFieldView()
        {
            SetFieldView(true);
        }

        public void OnShowControlView()
        {
            SetFieldView(false);
        }

        private async UniTask<IReadOnlyList<Cube>> ConnectFriendlyTeam()
        {
            cubeManager ??= new CubeManager(ResolveConnectType());
            var attemptCount = Mathf.Max(1, connectMaxAttempts);
            for (var attempt = 1; attempt <= attemptCount; attempt++)
            {
                connectionMessage = attempt == 1
                    ? "Scanning and connecting three friendly cubes..."
                    : $"Retrying friendly team connection ({attempt}/{attemptCount})...";
                roleMessage = $"Roles: waiting for Transporter / Scout / Builder ({attempt}/{attemptCount}).";
                RefreshRuntimeUi();

                try
                {
                    await cubeManager.MultiConnect(FriendlyRoleCount);
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
                    .Take(FriendlyRoleCount)
                    .ToList();
                if (connected.Count >= FriendlyRoleCount)
                {
                    return connected;
                }

                connectionMessage = $"Only {connected.Count}/{FriendlyRoleCount} friendly cubes were confirmed.";
                cubeManager.DisconnectAll();
                if (attempt < attemptCount)
                {
                    await UniTask.Delay(retryDelayMs);
                }
            }

            return Array.Empty<Cube>();
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

        private async UniTask RunRoleAppeal(string roleName, Cube cube)
        {
            if (cube == null || !cube.isConnected)
            {
                return;
            }

            roleMessage = $"{roleName}: connected as {GetCubeLabel(cube)}. Short left-right appeal.";
            RefreshRuntimeUi();
            cube.Move(roleAppealSpeed, -roleAppealSpeed, roleAppealTurnMs, Cube.ORDER_TYPE.Strong);
            await UniTask.Delay(roleAppealTurnMs + roleAppealPauseMs);
            cube.Move(-roleAppealSpeed, roleAppealSpeed, roleAppealTurnMs, Cube.ORDER_TYPE.Strong);
            await UniTask.Delay(roleAppealTurnMs + roleAppealPauseMs);
            cube.Move(0, 0, 80, Cube.ORDER_TYPE.Strong);
            await UniTask.Delay(roleAppealPauseMs);
            roleMessage = FormatRoleMessage();
            RefreshRuntimeUi();
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
                ResetTransporterRoute();
                observationMessage = "Goal anchor locked. Return the same cube to start, then press Run Transporter.";
            }

            RefreshRuntimeUi();
            RefreshVisualization();
        }

        private void RefreshTransporterArrival()
        {
            if (!isTransporterMoving || !hasLivePoint || transporterRoutePoints.Length == 0 || transporterRouteIndex < 0)
            {
                return;
            }

            var target = transporterRoutePoints[Mathf.Clamp(transporterRouteIndex, 0, transporterRoutePoints.Length - 1)];
            var tolerance = transporterRouteIndex >= transporterRoutePoints.Length - 1
                ? transporterGoalToleranceMatDots
                : transporterStepToleranceMatDots;
            if (Vector2.Distance(livePoint, target) > tolerance)
            {
                return;
            }

            if (transporterRouteIndex < transporterRoutePoints.Length - 1)
            {
                transporterRouteIndex++;
                observationMessage = FormatRouteMessage("GRID STEP LOCKED");
                CommandCurrentRouteTarget();
                return;
            }

            isTransporterMoving = false;
            transporterGoalReached = true;
            observationMessage = "GOAL REACHED. Phase 4 friendly recognition kept the Transporter route working.";
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

        private Vector2[] GenerateTransporterRoutePoints()
        {
            var route = new List<Vector2>();
            var centerColumn = TacticalFieldColumns / 2;
            for (var row = 0; row < TacticalFieldRows; row++)
            {
                var index = row * TacticalFieldColumns + centerColumn;
                if (index >= 0 && index < tacticalCellPoints.Length)
                {
                    route.Add(tacticalCellPoints[index]);
                }
            }

            route.Add(goalAnchor);
            return route.ToArray();
        }

        private void CommandCurrentRouteTarget()
        {
            if (!IsCubeConnected || transporterRoutePoints.Length == 0 || transporterRouteIndex < 0)
            {
                return;
            }

            var target = transporterRoutePoints[Mathf.Clamp(transporterRouteIndex, 0, transporterRoutePoints.Length - 1)];
            var nextPoint = transporterRouteIndex < transporterRoutePoints.Length - 1
                ? transporterRoutePoints[transporterRouteIndex + 1]
                : goalAnchor;
            observationCube.TargetMove(
                Mathf.RoundToInt(target.x),
                Mathf.RoundToInt(target.y),
                CalculateMatAngle(target, nextPoint),
                configID: Mathf.Clamp(transporterRouteIndex + 1, 1, 255),
                targetMoveType: Cube.TargetMoveType.RoundBeforeMove,
                maxSpd: transporterMaxSpeed
            );
        }

        private void ResetTransporterRoute()
        {
            isTransporterMoving = false;
            transporterGoalReached = false;
            transporterRouteIndex = -1;
            transporterRoutePoints = Array.Empty<Vector2>();
        }

        private string FormatRouteMessage(string prefix)
        {
            var stepCount = transporterRoutePoints.Length;
            var stepNumber = Mathf.Clamp(transporterRouteIndex + 1, 1, Mathf.Max(1, stepCount));
            return $"{prefix} | step {stepNumber}/{stepCount}";
        }

        private static int CalculateMatAngle(Vector2 from, Vector2 to)
        {
            var direction = to - from;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return 0;
            }

            var degrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return Mathf.RoundToInt(Mathf.Repeat(degrees, 360f));
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
            ResetTransporterRoute();
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
            rootBackground = root.AddComponent<Image>();
            rootBackground.color = new Color(BackgroundColor.r, BackgroundColor.g, BackgroundColor.b, 0.62f);

            controlView = CreateUiObject("ControlView", root.transform);
            StretchFull(controlView.GetComponent<RectTransform>());
            var header = CreatePanel("Header", controlView.transform, new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(900f, 104f), PanelColor);
            CreateText("Title", header.transform, "toio Tactical Field | Ordia Deskfront", 31, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor, new Vector2(-72f, 15f), new Vector2(676f, 40f));
            CreateText("Phase", header.transform, $"{VersionLabel} | Friendly 3-Piece Recognition", 18, FontStyle.Bold, TextAnchor.MiddleCenter, StartColor, new Vector2(-72f, -23f), new Vector2(676f, 26f));
            CreateButton("FieldView", header.transform, "Open Field View", new Vector2(260f, 0f), new Vector2(150f, 42f), GoalColor, OnShowFieldView);

            var status = CreatePanel("Status", controlView.transform, new Vector2(0f, 1f), new Vector2(24f, -184f), new Vector2(450f, 382f), CardColor, true);
            connectionStatusLabel = CreateText("Connection", status.transform, string.Empty, 16, FontStyle.Bold, TextAnchor.UpperLeft, TextColor, new Vector2(20f, -18f), new Vector2(410f, 58f), true);
            roleStatusLabel = CreateText("Roles", status.transform, string.Empty, 15, FontStyle.Bold, TextAnchor.UpperLeft, GoalColor, new Vector2(20f, -78f), new Vector2(410f, 62f), true);
            observationStatusLabel = CreateText("Observation", status.transform, string.Empty, 17, FontStyle.Bold, TextAnchor.UpperLeft, StartColor, new Vector2(20f, -148f), new Vector2(410f, 62f), true);
            anchorStatusLabel = CreateText("Anchors", status.transform, string.Empty, 15, FontStyle.Normal, TextAnchor.UpperLeft, MutedTextColor, new Vector2(20f, -222f), new Vector2(410f, 86f), true);
            victoryStatusLabel = CreateText("Victory", status.transform, string.Empty, 24, FontStyle.Bold, TextAnchor.UpperLeft, GoalColor, new Vector2(20f, -318f), new Vector2(410f, 40f), true);

            var actions = CreatePanel("Actions", controlView.transform, new Vector2(1f, 1f), new Vector2(-24f, -184f), new Vector2(330f, 382f), CardColor, false, true);
            CreateButton("Connect", actions.transform, "Connect Friendly Team", new Vector2(0f, -26f), new Vector2(276f, 44f), StartColor, OnConnectObservationCube, true);
            CreateButton("Capture", actions.transform, "Capture Current Anchor", new Vector2(0f, -78f), new Vector2(276f, 44f), GoalColor, OnCaptureCurrentAnchor, true);
            CreateButton("Convert", actions.transform, "Convert Tactical Field", new Vector2(0f, -130f), new Vector2(276f, 44f), GridStartColor, OnConvertTacticalField, true);
            CreateButton("Run", actions.transform, "Run Grid Route", new Vector2(0f, -182f), new Vector2(276f, 44f), StartColor, OnRunTransporter, true);
            CreateButton("Clear", actions.transform, "Clear Anchors", new Vector2(0f, -234f), new Vector2(276f, 40f), LineColor, OnClearAnchors, true);
            CreateButton("Back", actions.transform, "Back To Launcher", new Vector2(0f, -282f), new Vector2(276f, 38f), MutedTextColor, OnBackToLauncher, true);

            fieldView = CreateUiObject("FieldView", root.transform);
            StretchFull(fieldView.GetComponent<RectTransform>());
            var fieldViewBar = CreatePanel("FieldViewBar", fieldView.transform, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(780f, 54f), CardColor);
            fieldViewStatusLabel = CreateText("FieldViewStatus", fieldViewBar.transform, string.Empty, 18, FontStyle.Bold, TextAnchor.MiddleLeft, TextColor, new Vector2(-90f, 0f), new Vector2(560f, 38f));
            CreateButton("ReturnToControls", fieldViewBar.transform, "Return To Controls", new Vector2(285f, 0f), new Vector2(180f, 36f), GoalColor, OnShowControlView);
            fieldView.SetActive(false);
        }

        private void RefreshRuntimeUi()
        {
            if (connectionStatusLabel == null)
            {
                return;
            }

            connectionStatusLabel.text = connectionMessage;
            roleStatusLabel.text = roleMessage;
            observationStatusLabel.text = observationMessage;
            anchorStatusLabel.text =
                $"Start: {(hasStartAnchor ? FormatPoint(startAnchor, startSource) : "--")}\n" +
                $"Goal:  {(hasGoalAnchor ? FormatPoint(goalAnchor, goalSource) : "--")}\n" +
                $"Axis:  {(hasStartAnchor && hasGoalAnchor ? $"{Vector2.Distance(startAnchor, goalAnchor):F1} mat dots" : "--")}\n" +
                $"Route: {FormatRouteStatus()}\n" +
                tacticalFieldMessage;
            victoryStatusLabel.text = transporterGoalReached ? "GOAL REACHED" : string.Empty;
            if (fieldViewStatusLabel != null)
            {
                fieldViewStatusLabel.text = tacticalFieldMessage;
            }
        }

        private void SetFieldView(bool isFieldView)
        {
            if (controlView == null || fieldView == null || rootBackground == null)
            {
                return;
            }

            controlView.SetActive(!isFieldView);
            fieldView.SetActive(isFieldView);
            rootBackground.color = isFieldView
                ? new Color(BackgroundColor.r, BackgroundColor.g, BackgroundColor.b, 0f)
                : new Color(BackgroundColor.r, BackgroundColor.g, BackgroundColor.b, 0.62f);
        }

        private static string FormatPoint(Vector2 point, string source)
        {
            return $"({point.x:F1}, {point.y:F1}) [{source}]";
        }

        private string FormatRouteStatus()
        {
            if (transporterGoalReached)
            {
                return "complete";
            }

            if (isTransporterMoving && transporterRoutePoints.Length > 0 && transporterRouteIndex >= 0)
            {
                return $"{transporterRouteIndex + 1}/{transporterRoutePoints.Length}";
            }

            return tacticalCellPoints.Length > 0 ? "ready" : "--";
        }

        private string FormatRoleMessage()
        {
            return
                $"Transporter: {FormatRoleCube(observationCube)}\n" +
                $"Scout: {FormatRoleCube(scoutCube)} | Builder: {FormatRoleCube(builderCube)}";
        }

        private static string FormatRoleCube(Cube cube)
        {
            return cube != null && cube.isConnected ? GetCubeLabel(cube) : "--";
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
