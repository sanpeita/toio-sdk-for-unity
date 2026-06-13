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
        private const string VersionLabel = "Phase 5";
        private const int TacticalFieldColumns = 7;
        private const int TacticalFieldRows = 5;
        private const int FriendlyRoleCount = 3;
        private const int MinGridX = -3;
        private const int MaxGridX = 3;
        private const int MinGridY = -2;
        private const int MaxGridY = 2;
        private const int PlayerStartLineX = -3;
        private const int EnemyGoalLineX = 2;
        private const string JapaneseFontResourcePath = "Fonts/NotoSansJP-VF";

        private static readonly Color BackgroundColor = new Color(0.055f, 0.075f, 0.07f, 1f);
        private static readonly Color PanelColor = new Color(0.105f, 0.145f, 0.13f, 0.97f);
        private static readonly Color CardColor = new Color(0.075f, 0.11f, 0.1f, 0.97f);
        private static readonly Color StartColor = new Color(0.38f, 0.9f, 0.7f, 1f);
        private static readonly Color GoalColor = new Color(0.95f, 0.72f, 0.34f, 1f);
        private static readonly Color LineColor = new Color(0.58f, 0.76f, 0.66f, 1f);
        private static readonly Color GridColor = new Color(0.2f, 0.54f, 0.42f, 0.76f);
        private static readonly Color GridStartColor = new Color(0.28f, 0.78f, 0.58f, 0.9f);
        private static readonly Color GridGoalColor = new Color(0.92f, 0.62f, 0.24f, 0.9f);
        private static readonly Color ScoutColor = new Color(0.36f, 0.72f, 1f, 0.95f);
        private static readonly Color ScanColor = new Color(0.5f, 0.84f, 1f, 0.26f);
        private static readonly Color ObstacleColor = new Color(0.96f, 0.32f, 0.28f, 0.96f);
        private static readonly Color EnemyColor = new Color(0.78f, 0.38f, 0.94f, 0.96f);
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
        [SerializeField] private Vector2 fixedPlayerLineCenter = new Vector2(125f, 250f);
        [SerializeField] private Vector2 fixedEnemyGoalLineCenter = new Vector2(375f, 250f);

        [Header("Straight Transporter Victory")]
        [SerializeField] private float transporterStartToleranceMatDots = 28f;
        [SerializeField] private float transporterGoalToleranceMatDots = 18f;
        [SerializeField] private float transporterStepToleranceMatDots = 20f;
        [SerializeField] private int transporterMaxSpeed = 70;

        [Header("Phase 4 Friendly Recognition")]
        [SerializeField] private int roleAppealSpeed = 28;
        [SerializeField] private int roleAppealTurnMs = 240;
        [SerializeField] private int roleAppealPauseMs = 180;
        [SerializeField] private int roleAppealReadyTimeoutMs = 2200;

        [Header("Phase 5 Scout Discovery")]
        [SerializeField] private int phase5ObstacleCount = 5;
        [SerializeField] private int phase5RandomSeed = 503;
        [SerializeField] private int scoutSearchRadiusCells = 2;
        [SerializeField] private int scoutMoveMaxSpeed = 45;

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
        private string observationMessage = "Step 1: power on three Core Cubes, then connect the friendly team.";
        private string tacticalFieldMessage = "Field: fixed map ready. Press Convert Tactical Field.";
        private string roleMessage = "Assign order: 1 Transporter -> 2 Scout -> 3 Builder. Awaiting connection.";
        private string setupGuideMessage = "Cubes are assigned by BLE address order. Start line x=-3.";
        private string scoutMessage = "Scout: field conversion required before discovery.";

        private Text connectionStatusLabel;
        private Text roleStatusLabel;
        private Text observationStatusLabel;
        private Text anchorStatusLabel;
        private Text setupGuideLabel;
        private Text scoutStatusLabel;
        private Text victoryStatusLabel;
        private Text fieldViewStatusLabel;
        private static Font cachedUiFont;
        private Image rootBackground;
        private GameObject controlView;
        private GameObject fieldView;
        private GameObject observationMarker;
        private GameObject startMarker;
        private GameObject goalMarker;
        private GameObject observationLine;
        private Transform tacticalFieldRoot;
        private readonly List<GameObject> tacticalFieldCells = new List<GameObject>();
        private readonly List<GameObject> scoutDiscoveryMarkers = new List<GameObject>();
        private readonly HashSet<Vector2Int> obstacleCells = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> detectedObstacleCells = new HashSet<Vector2Int>();
        private Vector2[] tacticalCellPoints = Array.Empty<Vector2>();
        private Vector2[] transporterRoutePoints = Array.Empty<Vector2>();
        private Vector2Int scoutGridPosition = new Vector2Int(-1, MaxGridY);
        private Vector2Int enemyGridPosition = new Vector2Int(0, MinGridY);
        private int transporterRouteIndex = -1;
        private bool enemyDetected;

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
                    roleMessage = $"Assign order: 1 Transporter -> 2 Scout -> 3 Builder. Connected {connectedCubes.Count}/{FriendlyRoleCount}.";
                    return;
                }

                observationCube = connectedCubes[0];
                scoutCube = connectedCubes[1];
                builderCube = connectedCubes[2];
                RegisterFriendlyCubeListeners();
                await observationCube.ConfigIDNotification(idNotificationIntervalMs, Cube.IDNotificationType.Balanced);
                await scoutCube.ConfigIDNotification(idNotificationIntervalMs, Cube.IDNotificationType.Balanced);

                RefreshLivePoint();
                connectionMessage = "Friendly team connected. Next: press Convert Tactical Field.";
                roleMessage = FormatRoleMessage();
                setupGuideMessage = "BLE address order decides roles. After conversion: Scout(-3,1), Transporter(-3,0), Builder(-3,-1).";
                RefreshRuntimeUi();
                await UniTask.Delay(roleAppealPauseMs);
                await RunRoleAppeal("Transporter", observationCube);
                await RunRoleAppeal("Scout", scoutCube);
                await RunRoleAppeal("Builder", builderCube);
                observationMessage = "Step 2: press Convert Tactical Field. Capture anchors are no longer required.";
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
            ResetScoutDiscovery();
            ClearTacticalField();
            startSource = "--";
            goalSource = "--";
            observationMessage = "Fixed field cleared. Press Convert Tactical Field to rebuild it.";
            RefreshRuntimeUi();
            RefreshVisualization();
        }

        public void OnUseFixedFieldLines()
        {
            ApplyFixedFieldLines();
            tacticalFieldMessage = $"Field lines fixed | player x={PlayerStartLineX}, goal/enemy x={EnemyGoalLineX}";
            observationMessage = "Fixed lines are ready. Press Convert Tactical Field.";
            RefreshRuntimeUi();
            RefreshVisualization();
        }

        public void OnConvertTacticalField()
        {
            ApplyFixedFieldLines();

            var axis = goalAnchor - startAnchor;
            if (axis.sqrMagnitude <= Mathf.Epsilon)
            {
                tacticalFieldMessage = "Field: start and goal anchors must be different points.";
                RefreshRuntimeUi();
                return;
            }

            tacticalCellPoints = GenerateTacticalCellPoints(startAnchor, goalAnchor);
            GeneratePhase5MapFeatures();
            RenderTacticalField(tacticalCellPoints);
            ResetTransporterRoute();
            tacticalFieldMessage = $"TACTICAL FIELD CONVERTED | player x={PlayerStartLineX}, goal/enemy x={EnemyGoalLineX}";
            observationMessage = "Step 3: place Scout / Transporter / Builder on the player start line.";
            scoutMessage = FormatScoutMessage("Scout ready");
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
                observationMessage = "Convert the fixed tactical field before running the Transporter.";
                return;
            }

            if (tacticalCellPoints.Length == 0)
            {
                observationMessage = "Convert the tactical field before running the Phase 3 grid route.";
                return;
            }

            if (!CaptureLivePoint(observationCube))
            {
                observationMessage = "No readable mat position. Place the Transporter on the player start line.";
                return;
            }

            if (!TryGetCellPoint(new Vector2Int(PlayerStartLineX, 0), out var transporterStartPoint))
            {
                observationMessage = "Fixed Transporter start cell is missing. Convert the tactical field again.";
                return;
            }

            var startDistance = Vector2.Distance(livePoint, transporterStartPoint);
            if (startDistance > transporterStartToleranceMatDots)
            {
                observationMessage = $"Return the Transporter to (-3,0) before running. Distance: {startDistance:F1} mat dots.";
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

        public void OnScoutForward()
        {
            MoveScoutBy(new Vector2Int(1, 0), "forward");
        }

        public void OnScoutBack()
        {
            MoveScoutBy(new Vector2Int(-1, 0), "back");
        }

        public void OnScoutLeft()
        {
            MoveScoutBy(new Vector2Int(0, 1), "left");
        }

        public void OnScoutRight()
        {
            MoveScoutBy(new Vector2Int(0, -1), "right");
        }

        public void OnScoutScan()
        {
            if (tacticalCellPoints.Length == 0)
            {
                scoutMessage = "Scout: Convert Tactical Field first.";
                RefreshRuntimeUi();
                return;
            }

            var beforeObstacleCount = detectedObstacleCells.Count;
            foreach (var obstacle in obstacleCells)
            {
                if (IsWithinScoutSearch(obstacle))
                {
                    detectedObstacleCells.Add(obstacle);
                }
            }

            if (IsWithinScoutSearch(enemyGridPosition))
            {
                enemyDetected = true;
            }

            RenderTacticalField(tacticalCellPoints);
            var detectedDelta = detectedObstacleCells.Count - beforeObstacleCount;
            scoutMessage = FormatScoutMessage($"SCAN radius {scoutSearchRadiusCells}: +{detectedDelta} obstacle(s), enemy {(enemyDetected ? "detected" : "unknown")}");
            tacticalFieldMessage = $"SCOUT SCAN UPDATED | obstacles {detectedObstacleCells.Count}/{obstacleCells.Count}";
            RefreshRuntimeUi();
            SetFieldView(true);
        }

        private async UniTask<IReadOnlyList<Cube>> ConnectFriendlyTeam()
        {
            cubeManager ??= new CubeManager(ResolveConnectType());
            var attemptCount = Mathf.Max(1, connectMaxAttempts);
            for (var attempt = 1; attempt <= attemptCount; attempt++)
            {
                connectionMessage = attempt == 1
                    ? "Scanning and connecting three friendly cubes one by one..."
                    : $"Retrying friendly team connection ({attempt}/{attemptCount})...";
                roleMessage = $"Assign order: 1 Transporter -> 2 Scout -> 3 Builder ({attempt}/{attemptCount}).";
                RefreshRuntimeUi();

                try
                {
                    await ConnectFriendlyCubesOneByOne();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                var connected = GetOrderedConnectedCubes();
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

        private async UniTask ConnectFriendlyCubesOneByOne()
        {
            var guard = 0;
            while (GetOrderedConnectedCubes().Count < FriendlyRoleCount && guard < FriendlyRoleCount + 2)
            {
                guard++;
                connectionMessage = $"Connecting friendly cube {Mathf.Min(guard, FriendlyRoleCount)}/{FriendlyRoleCount}...";
                RefreshRuntimeUi();
                await cubeManager.SingleConnect();
                await UniTask.Delay(roleAppealPauseMs);
            }
        }

        private IReadOnlyList<Cube> GetOrderedConnectedCubes()
        {
            if (cubeManager == null)
            {
                return Array.Empty<Cube>();
            }

            return cubeManager.connectedCubes
                .Where(cube => cube != null && cube.isConnected)
                .GroupBy(cube => string.IsNullOrEmpty(cube.addr) ? cube.GetHashCode().ToString() : cube.addr)
                .Select(group => group.First())
                .OrderBy(cube => cube.addr)
                .Take(FriendlyRoleCount)
                .ToList();
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

        private void RegisterFriendlyCubeListeners()
        {
            RemoveListeners();
            RegisterCube(observationCube);
            RegisterCube(scoutCube);
            RegisterCube(builderCube);
        }

        private void RegisterCube(Cube cube)
        {
            if (cube == null)
            {
                return;
            }

            cube.idCallback.AddListener(observationListenerKey, OnCubeId);
            cube.buttonCallback.AddListener(observationListenerKey, OnCubeButton);
        }

        private void RemoveListeners()
        {
            RemoveCubeListeners(observationCube);
            RemoveCubeListeners(scoutCube);
            RemoveCubeListeners(builderCube);
        }

        private void RemoveCubeListeners(Cube cube)
        {
            if (cube == null)
            {
                return;
            }

            cube.idCallback.RemoveListener(observationListenerKey);
            cube.buttonCallback.RemoveListener(observationListenerKey);
        }

        private async UniTask RunRoleAppeal(string roleName, Cube cube)
        {
            if (cube == null || !cube.isConnected)
            {
                return;
            }

            roleMessage = $"{roleName}: connected as {GetCubeLabel(cube)}. Short left-right appeal.";
            RefreshRuntimeUi();
            var ready = await WaitUntilCubeControllable(cube);
            if (!ready)
            {
                roleMessage = $"{roleName}: connected as {GetCubeLabel(cube)}, but appeal skipped because the cube was not ready for motor orders.";
                RefreshRuntimeUi();
                await UniTask.Delay(roleAppealPauseMs);
                return;
            }

            cube.Move(roleAppealSpeed, -roleAppealSpeed, roleAppealTurnMs, Cube.ORDER_TYPE.Strong);
            await UniTask.Delay(roleAppealTurnMs + roleAppealPauseMs);
            await WaitUntilCubeControllable(cube);
            cube.Move(-roleAppealSpeed, roleAppealSpeed, roleAppealTurnMs, Cube.ORDER_TYPE.Strong);
            await UniTask.Delay(roleAppealTurnMs + roleAppealPauseMs);
            await WaitUntilCubeControllable(cube);
            cube.Move(0, 0, 80, Cube.ORDER_TYPE.Strong);
            await UniTask.Delay(roleAppealPauseMs);
            roleMessage = FormatRoleMessage();
            RefreshRuntimeUi();
        }

        private async UniTask<bool> WaitUntilCubeControllable(Cube cube)
        {
            if (cube == null || cubeManager == null)
            {
                return false;
            }

            var elapsedMs = 0;
            while (elapsedMs < roleAppealReadyTimeoutMs)
            {
                if (cube.isConnected && cubeManager.IsControllable(cube))
                {
                    return true;
                }

                await UniTask.Delay(50);
                elapsedMs += 50;
            }

            return cube.isConnected && cubeManager.IsControllable(cube);
        }

        private void OnCubeId(Cube cube)
        {
            if (cube == observationCube)
            {
                CaptureLivePoint(cube);
            }
        }

        private void OnCubeButton(Cube cube)
        {
            if (cube != observationCube)
            {
                return;
            }

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
            observationMessage = "GOAL REACHED. Phase 5 scout discovery kept the Transporter route working.";
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
            var up = new Vector2(-forward.y, forward.x);
            var cellSize = Vector2.Distance(start, goal) / (EnemyGoalLineX - PlayerStartLineX);
            var points = new Vector2[TacticalFieldColumns * TacticalFieldRows];
            var index = 0;
            for (var row = 0; row < TacticalFieldRows; row++)
            {
                var logicalY = MaxGridY - row;
                for (var column = 0; column < TacticalFieldColumns; column++)
                {
                    var logicalX = MinGridX + column;
                    points[index++] =
                        start +
                        forward * ((logicalX - PlayerStartLineX) * cellSize) +
                        up * (logicalY * cellSize);
                }
            }

            return points;
        }

        private Vector2[] GenerateTransporterRoutePoints()
        {
            var route = new List<Vector2>();
            for (var x = PlayerStartLineX; x <= EnemyGoalLineX; x++)
            {
                if (TryGetCellPoint(new Vector2Int(x, 0), out var point))
                {
                    route.Add(point);
                }
            }

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
                var column = index % TacticalFieldColumns;
                var logical = IndexToLogical(column, row);
                var cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cell.name = $"TacticalCell_{logical.x}_{logical.y}";
                cell.transform.SetParent(tacticalFieldRoot);
                cell.transform.position = MatToWorld(points[index], tacticalCellHeight);
                cell.transform.rotation = rotation;
                cell.transform.localScale = new Vector3(worldCellSize, tacticalCellHeight, worldCellSize);
                var color = ResolveCellColor(logical, row);
                cell.GetComponent<Renderer>().material = CreateMaterial($"MAT_{cell.name}", color);
                tacticalFieldCells.Add(cell);
            }

            RenderScoutDiscoveryMarkers(rotation, worldCellSize);
        }

        private void ClearTacticalField()
        {
            DestroyTacticalFieldCells();
            tacticalCellPoints = Array.Empty<Vector2>();
            ResetScoutDiscovery();
            ResetTransporterRoute();
            tacticalFieldMessage = "Field: fixed map ready. Press Convert Tactical Field.";
        }

        private void ApplyFixedFieldLines()
        {
            ClearTacticalField();
            startAnchor = fixedPlayerLineCenter;
            goalAnchor = fixedEnemyGoalLineCenter;
            startSource = "fixed x=-3";
            goalSource = "fixed x=2";
            hasStartAnchor = true;
            hasGoalAnchor = true;
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
            DestroyScoutDiscoveryMarkers();
        }

        private void DestroyScoutDiscoveryMarkers()
        {
            foreach (var marker in scoutDiscoveryMarkers)
            {
                if (marker != null)
                {
                    Destroy(marker);
                }
            }

            scoutDiscoveryMarkers.Clear();
        }

        private void GeneratePhase5MapFeatures()
        {
            ResetScoutDiscovery();
            var candidates = new List<Vector2Int>();
            for (var y = MaxGridY - 1; y > MinGridY; y--)
            {
                for (var x = MinGridX; x <= MaxGridX; x++)
                {
                    var logical = new Vector2Int(x, y);
                    if (x == PlayerStartLineX || x == EnemyGoalLineX || y == 0 || logical == scoutGridPosition || logical == enemyGridPosition)
                    {
                        continue;
                    }

                    candidates.Add(logical);
                }
            }

            var seed = phase5RandomSeed +
                Mathf.RoundToInt(startAnchor.x + startAnchor.y + goalAnchor.x + goalAnchor.y);
            var random = new System.Random(seed);
            while (obstacleCells.Count < phase5ObstacleCount && candidates.Count > 0)
            {
                var index = random.Next(candidates.Count);
                obstacleCells.Add(candidates[index]);
                candidates.RemoveAt(index);
            }

            scoutMessage = FormatScoutMessage($"Map generated: {obstacleCells.Count} hidden obstacles");
        }

        private void ResetScoutDiscovery()
        {
            obstacleCells.Clear();
            detectedObstacleCells.Clear();
            scoutGridPosition = new Vector2Int(PlayerStartLineX, 1);
            enemyGridPosition = new Vector2Int(EnemyGoalLineX, 0);
            enemyDetected = false;
            scoutMessage = "Scout: field conversion required before discovery.";
        }

        private void MoveScoutBy(Vector2Int delta, string directionName)
        {
            if (tacticalCellPoints.Length == 0)
            {
                scoutMessage = "Scout: Convert Tactical Field before moving.";
                RefreshRuntimeUi();
                return;
            }

            if (scoutCube == null || !scoutCube.isConnected)
            {
                scoutMessage = "Scout: connect the friendly team first.";
                RefreshRuntimeUi();
                return;
            }

            var next = scoutGridPosition + delta;
            if (!IsInsideGrid(next))
            {
                scoutMessage = FormatScoutMessage($"blocked at field edge ({directionName})");
                RefreshRuntimeUi();
                return;
            }

            if (!TryGetCellPoint(next, out var target))
            {
                scoutMessage = FormatScoutMessage($"target cell missing ({directionName})");
                RefreshRuntimeUi();
                return;
            }

            var previousPoint = TryGetCellPoint(scoutGridPosition, out var current)
                ? current
                : target;
            scoutCube.TargetMove(
                Mathf.RoundToInt(target.x),
                Mathf.RoundToInt(target.y),
                CalculateMatAngle(previousPoint, target),
                configID: 90,
                targetMoveType: Cube.TargetMoveType.RoundBeforeMove,
                maxSpd: scoutMoveMaxSpeed
            );
            scoutGridPosition = next;
            scoutMessage = FormatScoutMessage($"move {directionName}");
            RenderTacticalField(tacticalCellPoints);
            RefreshRuntimeUi();
            SetFieldView(true);
        }

        private bool IsWithinScoutSearch(Vector2Int target)
        {
            var distance = Mathf.Abs(target.x - scoutGridPosition.x) + Mathf.Abs(target.y - scoutGridPosition.y);
            return distance <= scoutSearchRadiusCells;
        }

        private bool TryGetCellPoint(Vector2Int logical, out Vector2 point)
        {
            var index = LogicalToIndex(logical);
            if (index >= 0 && index < tacticalCellPoints.Length)
            {
                point = tacticalCellPoints[index];
                return true;
            }

            point = Vector2.zero;
            return false;
        }

        private static int LogicalToIndex(Vector2Int logical)
        {
            if (!IsInsideGrid(logical))
            {
                return -1;
            }

            var column = logical.x - MinGridX;
            var row = MaxGridY - logical.y;
            return row * TacticalFieldColumns + column;
        }

        private static Vector2Int IndexToLogical(int column, int row)
        {
            return new Vector2Int(MinGridX + column, MaxGridY - row);
        }

        private static bool IsInsideGrid(Vector2Int logical)
        {
            return logical.x >= MinGridX && logical.x <= MaxGridX && logical.y >= MinGridY && logical.y <= MaxGridY;
        }

        private Color ResolveCellColor(Vector2Int logical, int row)
        {
            if (detectedObstacleCells.Contains(logical))
            {
                return ObstacleColor;
            }

            if (enemyDetected && logical == enemyGridPosition)
            {
                return EnemyColor;
            }

            if (logical.x == PlayerStartLineX)
            {
                return GridStartColor;
            }

            if (logical.x == EnemyGoalLineX)
            {
                return GridGoalColor;
            }

            return GridColor;
        }

        private void RenderScoutDiscoveryMarkers(Quaternion rotation, float worldCellSize)
        {
            DestroyScoutDiscoveryMarkers();
            if (tacticalCellPoints.Length == 0 || !TryGetCellPoint(scoutGridPosition, out var scoutPoint))
            {
                return;
            }

            var scoutMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            scoutMarker.name = "ScoutLogicalPosition";
            scoutMarker.transform.SetParent(tacticalFieldRoot);
            scoutMarker.transform.position = MatToWorld(scoutPoint, tacticalCellHeight + 0.2f);
            scoutMarker.transform.rotation = rotation;
            scoutMarker.transform.localScale = new Vector3(worldCellSize * 0.46f, 0.22f, worldCellSize * 0.46f);
            scoutMarker.GetComponent<Renderer>().material = CreateMaterial("MAT_ScoutLogicalPosition", ScoutColor);
            scoutDiscoveryMarkers.Add(scoutMarker);

            for (var y = MaxGridY; y >= MinGridY; y--)
            {
                for (var x = MinGridX; x <= MaxGridX; x++)
                {
                    var logical = new Vector2Int(x, y);
                    if (logical == scoutGridPosition || !IsWithinScoutSearch(logical) || !TryGetCellPoint(logical, out var point))
                    {
                        continue;
                    }

                    var scanMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    scanMarker.name = $"ScoutScanRadius_{logical.x}_{logical.y}";
                    scanMarker.transform.SetParent(tacticalFieldRoot);
                    scanMarker.transform.position = MatToWorld(point, tacticalCellHeight + 0.05f);
                    scanMarker.transform.rotation = rotation;
                    scanMarker.transform.localScale = new Vector3(worldCellSize * 0.28f, 0.05f, worldCellSize * 0.28f);
                    scanMarker.GetComponent<Renderer>().material = CreateMaterial($"MAT_{scanMarker.name}", ScanColor);
                    scoutDiscoveryMarkers.Add(scanMarker);
                }
            }
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
            CreateText("Phase", header.transform, $"{VersionLabel} | Scout Discovery Effect", 18, FontStyle.Bold, TextAnchor.MiddleCenter, StartColor, new Vector2(-72f, -23f), new Vector2(676f, 26f));
            CreateButton("FieldView", header.transform, "Open Field View", new Vector2(260f, 0f), new Vector2(150f, 42f), GoalColor, OnShowFieldView);

            var status = CreatePanel("Status", controlView.transform, new Vector2(0f, 1f), new Vector2(24f, -184f), new Vector2(450f, 382f), CardColor, true);
            connectionStatusLabel = CreateText("Connection", status.transform, string.Empty, 15, FontStyle.Bold, TextAnchor.UpperLeft, TextColor, new Vector2(20f, -16f), new Vector2(410f, 48f), true);
            roleStatusLabel = CreateText("Roles", status.transform, string.Empty, 13, FontStyle.Bold, TextAnchor.UpperLeft, GoalColor, new Vector2(20f, -66f), new Vector2(410f, 70f), true);
            setupGuideLabel = CreateText("SetupGuide", status.transform, string.Empty, 13, FontStyle.Bold, TextAnchor.UpperLeft, TextColor, new Vector2(20f, -140f), new Vector2(410f, 50f), true);
            observationStatusLabel = CreateText("Observation", status.transform, string.Empty, 15, FontStyle.Bold, TextAnchor.UpperLeft, StartColor, new Vector2(20f, -190f), new Vector2(410f, 42f), true);
            scoutStatusLabel = CreateText("Scout", status.transform, string.Empty, 15, FontStyle.Bold, TextAnchor.UpperLeft, ScoutColor, new Vector2(20f, -240f), new Vector2(410f, 46f), true);
            anchorStatusLabel = CreateText("Anchors", status.transform, string.Empty, 13, FontStyle.Normal, TextAnchor.UpperLeft, MutedTextColor, new Vector2(20f, -286f), new Vector2(410f, 58f), true);
            victoryStatusLabel = CreateText("Victory", status.transform, string.Empty, 22, FontStyle.Bold, TextAnchor.UpperLeft, GoalColor, new Vector2(20f, -344f), new Vector2(410f, 30f), true);

            var actions = CreatePanel("Actions", controlView.transform, new Vector2(1f, 1f), new Vector2(-24f, -184f), new Vector2(330f, 548f), CardColor, false, true);
            CreateButton("Connect", actions.transform, "Connect Friendly Team", new Vector2(0f, -26f), new Vector2(276f, 44f), StartColor, OnConnectObservationCube, true);
            CreateButton("FixedLines", actions.transform, "Set Fixed Lines", new Vector2(0f, -78f), new Vector2(276f, 44f), GoalColor, OnUseFixedFieldLines, true);
            CreateButton("Convert", actions.transform, "Convert Tactical Field", new Vector2(0f, -130f), new Vector2(276f, 44f), GridStartColor, OnConvertTacticalField, true);
            CreateButton("Run", actions.transform, "Run Grid Route", new Vector2(0f, -182f), new Vector2(276f, 44f), StartColor, OnRunTransporter, true);
            CreateButton("Clear", actions.transform, "Reset Field", new Vector2(0f, -234f), new Vector2(276f, 40f), LineColor, OnClearAnchors, true);
            CreateButton("Back", actions.transform, "Back To Launcher", new Vector2(0f, -282f), new Vector2(276f, 38f), MutedTextColor, OnBackToLauncher, true);

            CreateText("ScoutTitle", actions.transform, "Scout Controls | move 1 / scan 2", 14, FontStyle.Bold, TextAnchor.MiddleCenter, ScoutColor, new Vector2(0f, -332f), new Vector2(286f, 24f), true);
            CreateButton("ScoutForward", actions.transform, "Forward", new Vector2(0f, -366f), new Vector2(132f, 32f), ScoutColor, OnScoutForward, true);
            CreateButton("ScoutLeft", actions.transform, "Left", new Vector2(-72f, -404f), new Vector2(132f, 32f), ScoutColor, OnScoutLeft, true);
            CreateButton("ScoutScan", actions.transform, "Scan", new Vector2(72f, -404f), new Vector2(132f, 32f), GoalColor, OnScoutScan, true);
            CreateButton("ScoutRight", actions.transform, "Right", new Vector2(-72f, -442f), new Vector2(132f, 32f), ScoutColor, OnScoutRight, true);
            CreateButton("ScoutBack", actions.transform, "Back", new Vector2(72f, -442f), new Vector2(132f, 32f), ScoutColor, OnScoutBack, true);

            fieldView = CreateUiObject("FieldView", root.transform);
            StretchFull(fieldView.GetComponent<RectTransform>());
            var fieldViewBar = CreatePanel("FieldViewBar", fieldView.transform, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(780f, 54f), CardColor);
            fieldViewStatusLabel = CreateText("FieldViewStatus", fieldViewBar.transform, string.Empty, 18, FontStyle.Bold, TextAnchor.MiddleLeft, TextColor, new Vector2(-90f, 0f), new Vector2(560f, 38f));
            CreateButton("ReturnToControls", fieldViewBar.transform, "Return To Controls", new Vector2(285f, 0f), new Vector2(180f, 36f), GoalColor, OnShowControlView);
            var fieldScoutPanel = CreatePanel("FieldScoutControls", fieldView.transform, new Vector2(1f, 0f), new Vector2(-190f, 122f), new Vector2(326f, 190f), CardColor);
            CreateText("FieldScoutTitle", fieldScoutPanel.transform, "Scout Controls", 15, FontStyle.Bold, TextAnchor.MiddleCenter, ScoutColor, new Vector2(0f, 66f), new Vector2(286f, 24f));
            CreateButton("FieldScoutForward", fieldScoutPanel.transform, "Forward", new Vector2(0f, 32f), new Vector2(132f, 32f), ScoutColor, OnScoutForward);
            CreateButton("FieldScoutLeft", fieldScoutPanel.transform, "Left", new Vector2(-72f, -6f), new Vector2(132f, 32f), ScoutColor, OnScoutLeft);
            CreateButton("FieldScoutScan", fieldScoutPanel.transform, "Scan", new Vector2(72f, -6f), new Vector2(132f, 32f), GoalColor, OnScoutScan);
            CreateButton("FieldScoutRight", fieldScoutPanel.transform, "Right", new Vector2(-72f, -44f), new Vector2(132f, 32f), ScoutColor, OnScoutRight);
            CreateButton("FieldScoutBack", fieldScoutPanel.transform, "Back", new Vector2(72f, -44f), new Vector2(132f, 32f), ScoutColor, OnScoutBack);
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
            setupGuideLabel.text = setupGuideMessage;
            observationStatusLabel.text = observationMessage;
            scoutStatusLabel.text = scoutMessage;
            anchorStatusLabel.text =
                $"Player line: {(hasStartAnchor ? FormatPoint(startAnchor, startSource) : "--")}\n" +
                $"Goal/enemy:  {(hasGoalAnchor ? FormatPoint(goalAnchor, goalSource) : "--")}\n" +
                $"Grid:  x {MinGridX}..{MaxGridX} / y {MaxGridY}..{MinGridY}\n" +
                $"Route: {FormatRouteStatus()} | " +
                tacticalFieldMessage;
            victoryStatusLabel.text = transporterGoalReached ? "GOAL REACHED" : string.Empty;
            if (fieldViewStatusLabel != null)
            {
                fieldViewStatusLabel.text = $"{tacticalFieldMessage} | {scoutMessage}";
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

        private string FormatScoutMessage(string prefix)
        {
            return $"{prefix} | Scout ({scoutGridPosition.x},{scoutGridPosition.y}) | detected obstacles {detectedObstacleCells.Count}/{obstacleCells.Count}";
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
            text.font = ResolveUiFont();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            return text;
        }

        private static Font ResolveUiFont()
        {
            if (cachedUiFont != null)
            {
                return cachedUiFont;
            }

            cachedUiFont = Resources.Load<Font>(JapaneseFontResourcePath) ??
                Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return cachedUiFont;
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
