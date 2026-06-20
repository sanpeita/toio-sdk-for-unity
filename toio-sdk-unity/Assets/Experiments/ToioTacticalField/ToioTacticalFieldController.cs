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
        private const string VersionLabel = "Phase 5.3";
        private const int TacticalFieldColumns = 7;
        private const int TacticalFieldRows = 5;
        private const int FriendlyRoleCount = 3;
        private const int MinGridX = -3;
        private const int MaxGridX = 3;
        private const int MinGridY = -2;
        private const int MaxGridY = 2;
        private const int PlayerStartLineX = -3;
        private const int EnemyGoalLineX = 3;
        private const string JapaneseFontResourcePath = "Fonts/NotoSansJP-VF";

        private static readonly Vector2Int ScoutStartCell = new Vector2Int(PlayerStartLineX, 1);
        private static readonly Vector2Int TransporterStartCell = new Vector2Int(PlayerStartLineX, 0);
        private static readonly Vector2Int BuilderStartCell = new Vector2Int(PlayerStartLineX, -1);
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
        private static readonly Color UnknownCellColor = new Color(0.12f, 0.15f, 0.15f, 0.78f);
        private static readonly Color PlainCellColor = new Color(0.2f, 0.54f, 0.42f, 0.76f);
        private static readonly Color RoughCellColor = new Color(0.58f, 0.47f, 0.26f, 0.9f);
        private static readonly Color ObstacleColor = new Color(0.96f, 0.32f, 0.28f, 0.96f);
        private static readonly Color EnemyColor = new Color(0.78f, 0.38f, 0.94f, 0.96f);
        private static readonly Color TextColor = new Color(0.93f, 0.98f, 0.94f, 1f);
        private static readonly Color MutedTextColor = new Color(0.68f, 0.78f, 0.7f, 1f);

        private enum TerrainKind
        {
            Plain,
            Rough,
            Debris
        }

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
        [SerializeField] private Vector2 fixedMapTopLeft = new Vector2(98f, 142f);
        [SerializeField] private Vector2 fixedMapBottomRight = new Vector2(402f, 358f);

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
        [SerializeField] private bool runRoleAppealBeforeStartLineMove;

        [Header("Phase 5 Scout Discovery")]
        [SerializeField] private int phase5ObstacleCount = 5;
        [SerializeField] private int phase51RoughCellCount = 6;
        [SerializeField] private int phase5RandomSeed = 503;
        [SerializeField] private bool randomizePhase5TerrainEachConvert = true;
        [SerializeField] private int scoutSearchRadiusCells = 2;
        [SerializeField] private int scoutMoveMaxSpeed = 45;
        [SerializeField] private float roughCellSpeedMultiplier = 0.5f;
        [SerializeField] private int startLineMoveMaxSpeed = 40;
        [SerializeField] private int startLineMoveCommandSpacingMs = 450;
        [SerializeField] private float startLineArrivalToleranceMatDots = 24f;

        [Header("Phase 5.3 Automation")]
        [SerializeField] private bool autoTransporterEnabled = true;
        [SerializeField] private int scoutAutoMoveDelayMs = 1150;
        [SerializeField] private int scoutAutoMaxSteps = 28;

        private CubeManager cubeManager;
        private Cube observationCube;
        private Cube scoutCube;
        private Cube builderCube;
        private string observationListenerKey;
        private readonly List<Cube> friendlyRoleCubes = new List<Cube>();
        private bool isSceneActive = true;
        private bool isConnecting;
        private bool isStartLineRetrying;
        private bool isAutoScoutMoving;
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
        private string connectionMessage = "未接続。まず味方3台を接続してください。";
        private string observationMessage = "Step 1: Core Cubeを3台起動し、味方チームを接続します。";
        private string tacticalFieldMessage = "Field: 固定マップ待機中。戦域コンバートを実行してください。";
        private string roleMessage = "接続順: 1 Transporter -> 2 Scout -> 3 Builder。接続待ち。";
        private string setupGuideMessage = "役割は接続順で決まります。開始ラインは x=-3。";
        private string scoutMessage = "Scout: 戦域コンバート後にscanできます。";

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
        private readonly Dictionary<Vector2Int, TerrainKind> terrainByCell = new Dictionary<Vector2Int, TerrainKind>();
        private readonly HashSet<Vector2Int> scannedCells = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> obstacleCells = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> detectedObstacleCells = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> roughCells = new HashSet<Vector2Int>();
        private Vector2[] tacticalCellPoints = Array.Empty<Vector2>();
        private Vector2[] transporterRoutePoints = Array.Empty<Vector2>();
        private Vector2Int[] transporterRouteCells = Array.Empty<Vector2Int>();
        private Vector2Int scoutGridPosition = new Vector2Int(-1, MaxGridY);
        private Vector2Int enemyGridPosition = new Vector2Int(0, MinGridY);
        private int transporterRouteIndex = -1;
        private bool enemyDetected;

        private static readonly Vector2Int[] ScoutAutoWaypoints =
        {
            ScoutStartCell,
            new Vector2Int(1, 1),
            new Vector2Int(1, -2),
            new Vector2Int(-2, -2)
        };

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
            isSceneActive = false;
            RemoveListeners();
            if (Application.isPlaying)
            {
                cubeManager?.DisconnectAll();
            }
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
                if (!isSceneActive)
                {
                    return;
                }

                if (connectedCubes.Count < FriendlyRoleCount)
                {
                    connectionMessage = "味方3台を確認できませんでした。PC近くに置き、もう一度接続してください。";
                    roleMessage = $"接続順: 1 Transporter -> 2 Scout -> 3 Builder。接続 {connectedCubes.Count}/{FriendlyRoleCount}。";
                    return;
                }

                observationCube = connectedCubes[0];
                scoutCube = connectedCubes[1];
                builderCube = connectedCubes[2];
                RegisterFriendlyCubeListeners();
                await observationCube.ConfigIDNotification(idNotificationIntervalMs, Cube.IDNotificationType.Balanced);
                if (!isSceneActive)
                {
                    return;
                }

                await scoutCube.ConfigIDNotification(idNotificationIntervalMs, Cube.IDNotificationType.Balanced);
                if (!isSceneActive)
                {
                    return;
                }

                await builderCube.ConfigIDNotification(idNotificationIntervalMs, Cube.IDNotificationType.Balanced);
                if (!isSceneActive)
                {
                    return;
                }

                RefreshLivePoint();
                connectionMessage = "味方チーム接続完了。戦域を生成し、開始ラインへ移動します。";
                roleMessage = FormatRoleMessage();
                setupGuideMessage = "接続順で役割決定。Scout(-3,1), Transporter(-3,0), Builder(-3,-1)。";
                ConvertFixedTacticalField(false, "固定戦域を生成。味方チームを x=-3 開始ラインへ移動します。");
                RefreshRuntimeUi();
                await UniTask.Delay(roleAppealPauseMs);
                if (!isSceneActive)
                {
                    return;
                }

                if (runRoleAppealBeforeStartLineMove)
                {
                    await RunRoleAppeal("Transporter", observationCube);
                    await RunRoleAppeal("Scout", scoutCube);
                    await RunRoleAppeal("Builder", builderCube);
                }

                await MoveFriendlyTeamToStartLine();
            }
            catch (Exception ex)
            {
                connectionMessage = $"接続失敗: {ex.Message}";
                Debug.LogException(ex);
            }
            finally
            {
                isConnecting = false;
                if (isSceneActive)
                {
                    RefreshRuntimeUi();
                }
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
            transporterRouteCells = Array.Empty<Vector2Int>();
            ResetScoutDiscovery();
            ClearTacticalField();
            startSource = "--";
            goalSource = "--";
            observationMessage = "固定戦域をクリアしました。戦域コンバートで再生成できます。";
            RefreshRuntimeUi();
            RefreshVisualization();
        }

        public void OnUseFixedFieldLines()
        {
            ApplyFixedFieldLines();
            tacticalFieldMessage = $"固定ライン確認済み | player x={PlayerStartLineX}, goal/enemy x={EnemyGoalLineX}";
            observationMessage = "固定ライン準備完了。戦域コンバートを実行してください。";
            RefreshRuntimeUi();
            RefreshVisualization();
        }

        public void OnConvertTacticalField()
        {
            ConvertFixedTacticalField(true, "Step 3: Scout / Transporter / Builder を開始ラインで確認します。");
        }

        public async void OnRetryStartLineMoves()
        {
            if (isStartLineRetrying)
            {
                return;
            }

            if (!IsFriendlyTeamConnected)
            {
                observationMessage = "開始ライン再配置の前に味方チームを接続してください。";
                RefreshRuntimeUi();
                return;
            }

            isStartLineRetrying = true;
            try
            {
                if (tacticalCellPoints.Length == 0 && !ConvertFixedTacticalField(false, "固定戦域を生成。開始ライン再配置を再試行します。"))
                {
                    return;
                }

                observationMessage = "開始ライン再配置: 役割位置を確認し、不足分を再送信します。";
                RefreshRuntimeUi();
                var resent = await MoveMissingFriendlyRolesToStartLine();
                if (!isSceneActive)
                {
                    return;
                }

                observationMessage = resent > 0
                    ? $"開始ライン再配置: {resent} 役割へ移動指示。x=-3を確認してください。"
                    : "開始ライン再配置: 読み取れる役割はすでに x=-3 上です。";
                tacticalFieldMessage = $"戦域コンバート済み | 再配置 {resent}";
                scoutMessage = FormatScoutMessage("Scout待機中");
                RefreshRuntimeUi();
            }
            finally
            {
                isStartLineRetrying = false;
            }
        }

        private bool ConvertFixedTacticalField(bool switchToFieldView, string convertedObservationMessage)
        {
            ApplyFixedFieldLines();

            var axis = goalAnchor - startAnchor;
            if (axis.sqrMagnitude <= Mathf.Epsilon)
            {
                tacticalFieldMessage = "Field: 開始点とゴール点は別位置にしてください。";
                RefreshRuntimeUi();
                return false;
            }

            tacticalCellPoints = GenerateTacticalCellPoints(startAnchor, goalAnchor);
            GeneratePhase5MapFeatures();
            RenderTacticalField(tacticalCellPoints);
            ResetTransporterRoute();
            tacticalFieldMessage = $"戦域コンバート済み | 未スキャンは進入不可 / goal x={EnemyGoalLineX}";
            observationMessage = convertedObservationMessage;
            scoutMessage = FormatScoutMessage("Scout待機中: まずscanで道を開きます");
            RefreshRuntimeUi();
            if (switchToFieldView)
            {
                SetFieldView(true);
            }

            return true;
        }

        public void OnRunTransporter()
        {
            TryStartTransporterRoute(true, "Transporter移動開始");
        }

        private bool TryStartTransporterRoute(bool reportErrors, string startMessagePrefix)
        {
            if (!IsCubeConnected)
            {
                if (reportErrors)
                {
                    observationMessage = "Transporter開始前に味方チームを接続してください。";
                    RefreshRuntimeUi();
                }

                return false;
            }

            if (!hasStartAnchor || !hasGoalAnchor)
            {
                if (reportErrors)
                {
                    observationMessage = "Transporter開始前に固定戦域をコンバートしてください。";
                    RefreshRuntimeUi();
                }

                return false;
            }

            if (tacticalCellPoints.Length == 0)
            {
                if (reportErrors)
                {
                    observationMessage = "Transporter開始前に戦域コンバートが必要です。";
                    RefreshRuntimeUi();
                }

                return false;
            }

            if (!CaptureLivePoint(observationCube))
            {
                if (reportErrors)
                {
                    observationMessage = "Transporter位置を読めません。開始ラインに置いてください。";
                    RefreshRuntimeUi();
                }

                return false;
            }

            if (!TryGetCellPoint(new Vector2Int(PlayerStartLineX, 0), out var transporterStartPoint))
            {
                if (reportErrors)
                {
                    observationMessage = "Transporter開始セルが見つかりません。戦域を再コンバートしてください。";
                    RefreshRuntimeUi();
                }

                return false;
            }

            var startDistance = Vector2.Distance(livePoint, transporterStartPoint);
            if (startDistance > transporterStartToleranceMatDots)
            {
                if (reportErrors)
                {
                    observationMessage = $"Transporterを (-3,0) に戻してください。距離: {startDistance:F1} mat dots。";
                    RefreshRuntimeUi();
                }

                return false;
            }

            if (!TryBuildScannedTransporterRoute(TransporterStartCell, out var routeCells, out var routePoints, out var routeError))
            {
                if (reportErrors)
                {
                    observationMessage = routeError;
                    RefreshRuntimeUi();
                    SetFieldView(true);
                }

                return false;
            }

            transporterRouteCells = routeCells;
            transporterRoutePoints = routePoints;
            transporterRouteIndex = 0;
            isTransporterMoving = true;
            transporterGoalReached = false;
            observationMessage = FormatRouteMessage(startMessagePrefix);
            CommandCurrentRouteTarget();
            SetFieldView(true);
            RefreshRuntimeUi();
            return true;
        }

        public void OnStopTransporter()
        {
            if (observationCube != null && observationCube.isConnected)
            {
                observationCube.Move(0, 0, 80, Cube.ORDER_TYPE.Strong);
            }

            isTransporterMoving = false;
            transporterRouteIndex = -1;
            observationMessage = "Transporter移動を中断しました。";
            RefreshRuntimeUi();
        }

        public void OnToggleAutoTransporter()
        {
            autoTransporterEnabled = !autoTransporterEnabled;
            observationMessage = autoTransporterEnabled
                ? "Auto搬送 ON: 道が開いたらTransporterを自動発進します。"
                : "Auto搬送 OFF: Transporterは手動開始に戻します。";
            if (autoTransporterEnabled)
            {
                TryAutoStartTransporter("Auto搬送開始");
            }

            RefreshRuntimeUi();
        }

        public async void OnAutoScoutRoute()
        {
            if (isAutoScoutMoving)
            {
                isAutoScoutMoving = false;
                scoutMessage = FormatScoutMessage("Scout自動移動を停止します");
                RefreshRuntimeUi();
                return;
            }

            if (tacticalCellPoints.Length == 0)
            {
                scoutMessage = "Scout: 自動移動前に戦域コンバートしてください。";
                RefreshRuntimeUi();
                return;
            }

            if (scoutCube == null || !scoutCube.isConnected)
            {
                scoutMessage = "Scout: 自動移動前に味方チームを接続してください。";
                RefreshRuntimeUi();
                return;
            }

            isAutoScoutMoving = true;
            scoutMessage = FormatScoutMessage("Scout自動スキャン開始");
            RefreshRuntimeUi();
            SetFieldView(true);

            try
            {
                OnScoutScan();
                if (isTransporterMoving)
                {
                    scoutMessage = FormatScoutMessage("Scout自動完了: Transporter発進");
                    return;
                }

                await UniTask.Delay(scoutAutoMoveDelayMs);
                var waypointIndex = ResolveScoutAutoWaypointIndex();
                for (var step = 0; isSceneActive && isAutoScoutMoving && step < scoutAutoMaxSteps; step++)
                {
                    if (waypointIndex >= ScoutAutoWaypoints.Length)
                    {
                        scoutMessage = FormatScoutMessage("Scout自動スキャン完了");
                        break;
                    }

                    var target = ScoutAutoWaypoints[waypointIndex];
                    if (scoutGridPosition == target)
                    {
                        waypointIndex++;
                        continue;
                    }

                    if (!TryBuildScoutRoute(scoutGridPosition, target, out var route) || route.Length < 2)
                    {
                        if (!TryFindScoutExplorationStep(out var explorationStep))
                        {
                            scoutMessage = FormatScoutMessage($"Scout自動待機: {target.x},{target.y} への既知ルートなし");
                            break;
                        }

                        MoveScoutTo(explorationStep, "auto-scan");
                    }
                    else
                    {
                        MoveScoutTo(route[1], "auto-route");
                    }

                    await UniTask.Delay(scoutAutoMoveDelayMs);
                    if (isTransporterMoving)
                    {
                        scoutMessage = FormatScoutMessage("Scout自動完了: Transporter発進");
                        break;
                    }

                    if (!isSceneActive || !isAutoScoutMoving)
                    {
                        break;
                    }

                    OnScoutScan();
                    TryAutoStartTransporter("Auto搬送開始");
                    if (isTransporterMoving)
                    {
                        scoutMessage = FormatScoutMessage("Scout自動完了: Transporter発進");
                        break;
                    }

                    await UniTask.Delay(scoutAutoMoveDelayMs);
                }
            }
            finally
            {
                isAutoScoutMoving = false;
                RefreshRuntimeUi();
            }
        }

        public async void OnBuilderAppeal()
        {
            if (builderCube == null || !builderCube.isConnected)
            {
                roleMessage = "Builder未接続。味方チーム接続後に自己アピールできます。";
                RefreshRuntimeUi();
                return;
            }

            roleMessage = "Builder: 自己アピール中（Phase5.1では機能未実装）。";
            RefreshRuntimeUi();
            await RunRoleAppeal("Builder", builderCube);
            roleMessage = FormatRoleMessage();
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
                scoutMessage = "Scout: 先に戦域コンバートしてください。";
                RefreshRuntimeUi();
                return;
            }

            var beforeScannedCount = scannedCells.Count;
            var beforeObstacleCount = detectedObstacleCells.Count;
            foreach (var cell in EnumerateAllGridCells())
            {
                if (!IsWithinScoutSearch(cell))
                {
                    continue;
                }

                scannedCells.Add(cell);
                if (GetTerrainKind(cell) == TerrainKind.Debris)
                {
                    detectedObstacleCells.Add(cell);
                }
            }

            if (IsWithinScoutSearch(enemyGridPosition))
            {
                enemyDetected = true;
            }

            RenderTacticalField(tacticalCellPoints);
            var scannedDelta = scannedCells.Count - beforeScannedCount;
            var detectedDelta = detectedObstacleCells.Count - beforeObstacleCount;
            scoutMessage = FormatScoutMessage($"scan半径{scoutSearchRadiusCells}: +{scannedDelta}マス / デブリ+{detectedDelta}");
            tacticalFieldMessage = $"scan更新 | 解明 {scannedCells.Count}/{TacticalFieldColumns * TacticalFieldRows} | デブリ {detectedObstacleCells.Count}/{obstacleCells.Count}";
            TryAutoStartTransporter("Auto搬送開始");
            RefreshRuntimeUi();
            SetFieldView(true);
        }

        private async UniTask<IReadOnlyList<Cube>> ConnectFriendlyTeam()
        {
            cubeManager ??= new CubeManager(ResolveConnectType());
            var attemptCount = Mathf.Max(1, connectMaxAttempts);
            for (var attempt = 1; attempt <= attemptCount; attempt++)
            {
                friendlyRoleCubes.Clear();
                connectionMessage = attempt == 1
                    ? "味方キューブ3台を1台ずつ接続中..."
                    : $"味方チーム接続を再試行中 ({attempt}/{attemptCount})...";
                roleMessage = $"接続順: 1 Transporter -> 2 Scout -> 3 Builder ({attempt}/{attemptCount})。";
                RefreshRuntimeUi();

                try
                {
                    await ConnectFriendlyCubesOneByOne();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                var connected = GetRoleAssignedConnectedCubes();
                if (connected.Count >= FriendlyRoleCount)
                {
                    return connected;
                }

                connectionMessage = $"確認できた味方キューブは {connected.Count}/{FriendlyRoleCount} 台です。";
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
            while (GetRoleAssignedConnectedCubes().Count < FriendlyRoleCount && guard < FriendlyRoleCount + 2)
            {
                guard++;
                connectionMessage = $"味方キューブ接続中 {Mathf.Min(guard, FriendlyRoleCount)}/{FriendlyRoleCount}...";
                RefreshRuntimeUi();
                var cube = await cubeManager.SingleConnect();
                RegisterFriendlyRoleCandidate(cube);
                await UniTask.Delay(roleAppealPauseMs);
            }
        }

        private IReadOnlyList<Cube> GetRoleAssignedConnectedCubes()
        {
            if (cubeManager == null)
            {
                return Array.Empty<Cube>();
            }

            var assigned = new List<Cube>();
            foreach (var cube in friendlyRoleCubes)
            {
                AddDistinctConnectedCube(assigned, cube);
            }

            foreach (var cube in cubeManager.connectedCubes)
            {
                AddDistinctConnectedCube(assigned, cube);
            }

            return assigned.Take(FriendlyRoleCount).ToList();
        }

        private void RegisterFriendlyRoleCandidate(Cube cube)
        {
            if (cube == null)
            {
                return;
            }

            AddDistinctConnectedCube(friendlyRoleCubes, cube);
            roleMessage = FormatConnectionOrderProgress();
            RefreshRuntimeUi();
        }

        private static void AddDistinctConnectedCube(List<Cube> target, Cube cube)
        {
            if (cube == null || !cube.isConnected)
            {
                return;
            }

            var cubeKey = GetCubeIdentity(cube);
            if (target.Any(existing => GetCubeIdentity(existing) == cubeKey))
            {
                return;
            }

            target.Add(cube);
        }

        private string FormatConnectionOrderProgress()
        {
            var roles = GetRoleAssignedConnectedCubes();
            return
                $"接続順: 1 Transporter -> 2 Scout -> 3 Builder\n" +
                $"1 Transporter: {FormatRoleCube(roles.Count > 0 ? roles[0] : null)}\n" +
                $"2 Scout: {FormatRoleCube(roles.Count > 1 ? roles[1] : null)} | 3 Builder: {FormatRoleCube(roles.Count > 2 ? roles[2] : null)}";
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

            roleMessage = $"{roleName}: {GetCubeLabel(cube)} として接続。左右に自己アピールします。";
            RefreshRuntimeUi();
            var ready = await WaitUntilCubeControllable(cube);
            if (!isSceneActive)
            {
                return;
            }

            if (!ready)
            {
                roleMessage = $"{roleName}: {GetCubeLabel(cube)} は接続済みですが、モーター指示準備ができずアピールをスキップしました。";
                RefreshRuntimeUi();
                await UniTask.Delay(roleAppealPauseMs);
                return;
            }

            cube.Move(roleAppealSpeed, -roleAppealSpeed, roleAppealTurnMs, Cube.ORDER_TYPE.Strong);
            await UniTask.Delay(roleAppealTurnMs + roleAppealPauseMs);
            if (!isSceneActive)
            {
                return;
            }

            await WaitUntilCubeControllable(cube);
            cube.Move(-roleAppealSpeed, roleAppealSpeed, roleAppealTurnMs, Cube.ORDER_TYPE.Strong);
            await UniTask.Delay(roleAppealTurnMs + roleAppealPauseMs);
            if (!isSceneActive)
            {
                return;
            }

            await WaitUntilCubeControllable(cube);
            cube.Move(0, 0, 80, Cube.ORDER_TYPE.Strong);
            await UniTask.Delay(roleAppealPauseMs);
            if (!isSceneActive)
            {
                return;
            }

            roleMessage = FormatRoleMessage();
            RefreshRuntimeUi();
        }

        private async UniTask MoveFriendlyTeamToStartLine()
        {
            if (!isSceneActive)
            {
                return;
            }

            if (tacticalCellPoints.Length == 0 && !ConvertFixedTacticalField(false, "固定戦域を生成。味方チームを x=-3 開始ラインへ移動します。"))
            {
                return;
            }

            observationMessage = "自動開始ライン移動: Transporter / Scout / Builder を x=-3 へ送ります。";
            scoutMessage = FormatScoutMessage("Scout開始ライン移動を予約");
            RefreshRuntimeUi();

            await MoveRoleToStartCell("Transporter", observationCube, TransporterStartCell, 71);
            await MoveRoleToStartCell("Scout", scoutCube, ScoutStartCell, 72);
            await MoveRoleToStartCell("Builder", builderCube, BuilderStartCell, 73);

            if (!isSceneActive)
            {
                return;
            }

            scoutGridPosition = ScoutStartCell;
            RenderTacticalField(tacticalCellPoints);
            connectionMessage = "味方チーム接続完了。開始ラインへの移動指示を送信しました。";
            observationMessage = "Step 3: 3台が x=-3 にいるか確認し、準備できたら盤面を見てください。";
            tacticalFieldMessage = $"戦域コンバート済み | player x={PlayerStartLineX}, goal/enemy x={EnemyGoalLineX} | 自動開始送信";
            scoutMessage = FormatScoutMessage("Scout待機中");
            RefreshRuntimeUi();
        }

        private async UniTask<int> MoveMissingFriendlyRolesToStartLine()
        {
            var resent = 0;
            if (await MoveRoleToStartCellIfMissing("Transporter", observationCube, TransporterStartCell, 81))
            {
                resent++;
            }

            if (await MoveRoleToStartCellIfMissing("Scout", scoutCube, ScoutStartCell, 82))
            {
                resent++;
            }

            if (await MoveRoleToStartCellIfMissing("Builder", builderCube, BuilderStartCell, 83))
            {
                resent++;
            }

            return resent;
        }

        private async UniTask<bool> MoveRoleToStartCellIfMissing(string roleName, Cube cube, Vector2Int logicalCell, int configId)
        {
            if (IsRoleAtStartCell(cube, logicalCell, out var distance))
            {
                roleMessage = $"{roleName}: 開始セル ({logicalCell.x},{logicalCell.y}) にいます。距離 {distance:F1}。";
                RefreshRuntimeUi();
                await UniTask.Delay(startLineMoveCommandSpacingMs);
                return false;
            }

            return await MoveRoleToStartCell(roleName, cube, logicalCell, configId);
        }

        private async UniTask<bool> MoveRoleToStartCell(string roleName, Cube cube, Vector2Int logicalCell, int configId)
        {
            if (!isSceneActive)
            {
                return false;
            }

            if (cube == null || !cube.isConnected)
            {
                roleMessage = $"{roleName}: キューブ未接続のため開始ライン移動をスキップ。";
                RefreshRuntimeUi();
                return false;
            }

            if (!TryGetCellPoint(logicalCell, out var target))
            {
                roleMessage = $"{roleName}: 開始セル ({logicalCell.x},{logicalCell.y}) が見つかりません。";
                RefreshRuntimeUi();
                return false;
            }

            var moveSent = false;
            for (var attempt = 1; attempt <= 2; attempt++)
            {
                roleMessage = $"{roleName}: 開始セル ({logicalCell.x},{logicalCell.y}) へ移動中 [{attempt}/2]。";
                RefreshRuntimeUi();
                var ready = await WaitUntilCubeControllable(cube);
                if (!isSceneActive)
                {
                    return false;
                }

                if (!ready)
                {
                    continue;
                }

                cube.TargetMove(
                    Mathf.RoundToInt(target.x),
                    Mathf.RoundToInt(target.y),
                    CalculateMatAngle(target, goalAnchor),
                    configID: configId,
                    targetMoveType: Cube.TargetMoveType.RoundBeforeMove,
                    maxSpd: startLineMoveMaxSpeed
                );
                moveSent = true;
                await UniTask.Delay(startLineMoveCommandSpacingMs);
            }

            if (!moveSent)
            {
                roleMessage = $"{roleName}: モーター指示準備ができず、開始ライン移動をスキップしました。";
                RefreshRuntimeUi();
            }

            return moveSent;
        }

        private bool IsRoleAtStartCell(Cube cube, Vector2Int logicalCell, out float distance)
        {
            distance = float.PositiveInfinity;
            if (cube == null || !cube.isConnected || !TryGetCellPoint(logicalCell, out var target))
            {
                return false;
            }

            if (!HasReadableMatPosition(cube))
            {
                return false;
            }

            distance = Vector2.Distance(cube.pos, target);
            return distance <= startLineArrivalToleranceMatDots;
        }

        private bool HasReadableMatPosition(Cube cube)
        {
            if (cube == null || !cube.isConnected)
            {
                return false;
            }

            var minX = Mathf.Min(fixedMapTopLeft.x, fixedMapBottomRight.x) - startLineArrivalToleranceMatDots;
            var maxX = Mathf.Max(fixedMapTopLeft.x, fixedMapBottomRight.x) + startLineArrivalToleranceMatDots;
            var minY = Mathf.Min(fixedMapTopLeft.y, fixedMapBottomRight.y) - startLineArrivalToleranceMatDots;
            var maxY = Mathf.Max(fixedMapTopLeft.y, fixedMapBottomRight.y) + startLineArrivalToleranceMatDots;
            return cube.x >= minX && cube.x <= maxX && cube.y >= minY && cube.y <= maxY;
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
                if (!isSceneActive)
                {
                    return false;
                }

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
                observationMessage = "始点アンカー取得。次にゴール側へ動かしてボタンを押してください。";
            }
            else
            {
                ClearTacticalField();
                goalAnchor = point;
                goalSource = source;
                hasGoalAnchor = true;
                ResetTransporterRoute();
                observationMessage = "ゴールアンカー取得。同じキューブを始点へ戻し、Transporterを開始してください。";
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
                observationMessage = FormatRouteMessage("Transporter 1マス到達");
                CommandCurrentRouteTarget();
                return;
            }

            isTransporterMoving = false;
            transporterGoalReached = true;
            observationMessage = "GOAL REACHED。スキャン済みルートでTransporterがゴールへ到達しました。";
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
            var points = new Vector2[TacticalFieldColumns * TacticalFieldRows];
            var index = 0;
            for (var row = 0; row < TacticalFieldRows; row++)
            {
                var logicalY = MaxGridY - row;
                for (var column = 0; column < TacticalFieldColumns; column++)
                {
                    var logicalX = MinGridX + column;
                    points[index++] = GetFixedMapCellCenter(new Vector2Int(logicalX, logicalY));
                }
            }

            return points;
        }

        private Vector2 GetFixedMapCellCenter(Vector2Int logical)
        {
            var column = logical.x - MinGridX;
            var row = MaxGridY - logical.y;
            var cellWidth = Mathf.Abs(fixedMapBottomRight.x - fixedMapTopLeft.x) / TacticalFieldColumns;
            var cellHeight = Mathf.Abs(fixedMapBottomRight.y - fixedMapTopLeft.y) / TacticalFieldRows;
            return new Vector2(
                Mathf.Min(fixedMapTopLeft.x, fixedMapBottomRight.x) + (column + 0.5f) * cellWidth,
                Mathf.Min(fixedMapTopLeft.y, fixedMapBottomRight.y) + (row + 0.5f) * cellHeight
            );
        }

        private bool TryBuildScannedTransporterRoute(
            Vector2Int start,
            out Vector2Int[] routeCells,
            out Vector2[] routePoints,
            out string errorMessage)
        {
            routeCells = Array.Empty<Vector2Int>();
            routePoints = Array.Empty<Vector2>();
            errorMessage = string.Empty;

            if (!IsKnownPassableCell(start))
            {
                errorMessage = "Transporter開始セルが未スキャン、または通行不可です。Scoutで開始周辺をscanしてください。";
                return false;
            }

            var queue = new Queue<Vector2Int>();
            var previous = new Dictionary<Vector2Int, Vector2Int>();
            var visited = new HashSet<Vector2Int> { start };
            queue.Enqueue(start);

            Vector2Int? goal = null;
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.x == EnemyGoalLineX)
                {
                    goal = current;
                    break;
                }

                foreach (var next in EnumerateNeighborCells(current))
                {
                    if (visited.Contains(next) || !IsKnownPassableCell(next) || IsFriendlyOccupiedForTransporterRoute(next, start))
                    {
                        continue;
                    }

                    visited.Add(next);
                    previous[next] = current;
                    queue.Enqueue(next);
                }
            }

            if (!goal.HasValue)
            {
                var scannedGoalCells = scannedCells.Count(cell => cell.x == EnemyGoalLineX);
                errorMessage = scannedGoalCells == 0
                    ? "Transporter停止: ゴールラインまでscan済みの道がありません。Scoutで先を解明してください。"
                    : "Transporter停止: scan済みですがデブリで経路が切れています。別ルートをscanしてください。";
                return false;
            }

            var cells = new List<Vector2Int>();
            var cursor = goal.Value;
            cells.Add(cursor);
            while (cursor != start)
            {
                cursor = previous[cursor];
                cells.Add(cursor);
            }

            cells.Reverse();
            var points = new List<Vector2>();
            foreach (var cell in cells)
            {
                if (!TryGetCellPoint(cell, out var point))
                {
                    errorMessage = "Transporter経路上のセル座標を取得できません。戦域を再コンバートしてください。";
                    return false;
                }

                points.Add(point);
            }

            routeCells = cells.ToArray();
            routePoints = points.ToArray();
            return routePoints.Length > 0;
        }

        private void TryAutoStartTransporter(string startMessagePrefix)
        {
            if (!autoTransporterEnabled || isTransporterMoving || transporterGoalReached)
            {
                return;
            }

            if (TryStartTransporterRoute(false, startMessagePrefix))
            {
                return;
            }

            if (tacticalCellPoints.Length > 0)
            {
                tacticalFieldMessage = $"Auto搬送待機 | goal x={EnemyGoalLineX} までscan済みルート待ち";
            }
        }

        private int ResolveScoutAutoWaypointIndex()
        {
            for (var i = 0; i < ScoutAutoWaypoints.Length; i++)
            {
                if (scoutGridPosition == ScoutAutoWaypoints[i])
                {
                    return Mathf.Min(i + 1, ScoutAutoWaypoints.Length);
                }
            }

            return 1;
        }

        private bool TryBuildScoutRoute(Vector2Int start, Vector2Int target, out Vector2Int[] route)
        {
            route = Array.Empty<Vector2Int>();
            if (!IsKnownPassableCell(start) || !IsKnownPassableCell(target))
            {
                return false;
            }

            var queue = new Queue<Vector2Int>();
            var previous = new Dictionary<Vector2Int, Vector2Int>();
            var visited = new HashSet<Vector2Int> { start };
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == target)
                {
                    var cells = new List<Vector2Int> { current };
                    while (current != start)
                    {
                        current = previous[current];
                        cells.Add(current);
                    }

                    cells.Reverse();
                    route = cells.ToArray();
                    return route.Length > 0;
                }

                foreach (var next in EnumerateNeighborCells(current))
                {
                    if (visited.Contains(next) || IsFriendlyReservedCell(next) || !IsKnownPassableCell(next))
                    {
                        continue;
                    }

                    visited.Add(next);
                    previous[next] = current;
                    queue.Enqueue(next);
                }
            }

            return false;
        }

        private bool TryFindScoutExplorationStep(out Vector2Int step)
        {
            step = scoutGridPosition;
            var bestScore = -1;
            foreach (var next in EnumerateNeighborCells(scoutGridPosition))
            {
                if (IsFriendlyReservedCell(next) || !IsKnownPassableCell(next))
                {
                    continue;
                }

                var score = CountUnknownCellsInScoutRange(next);
                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                step = next;
            }

            return bestScore >= 0;
        }

        private int CountUnknownCellsInScoutRange(Vector2Int scoutPosition)
        {
            var count = 0;
            foreach (var cell in EnumerateAllGridCells())
            {
                var distance = Mathf.Abs(cell.x - scoutPosition.x) + Mathf.Abs(cell.y - scoutPosition.y);
                if (distance <= scoutSearchRadiusCells && !scannedCells.Contains(cell))
                {
                    count++;
                }
            }

            return count;
        }

        private static IEnumerable<Vector2Int> EnumerateNeighborCells(Vector2Int current)
        {
            yield return current + new Vector2Int(1, 0);
            yield return current + new Vector2Int(-1, 0);
            yield return current + new Vector2Int(0, 1);
            yield return current + new Vector2Int(0, -1);
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
            var speed = ResolveMoveSpeedForCell(
                transporterRouteCells.Length > transporterRouteIndex ? transporterRouteCells[transporterRouteIndex] : TransporterStartCell,
                transporterMaxSpeed);
            observationCube.TargetMove(
                Mathf.RoundToInt(target.x),
                Mathf.RoundToInt(target.y),
                CalculateMatAngle(target, nextPoint),
                configID: Mathf.Clamp(transporterRouteIndex + 1, 1, 255),
                targetMoveType: Cube.TargetMoveType.RoundBeforeMove,
                maxSpd: speed
            );
        }

        private void ResetTransporterRoute()
        {
            isTransporterMoving = false;
            transporterGoalReached = false;
            transporterRouteIndex = -1;
            transporterRoutePoints = Array.Empty<Vector2>();
            transporterRouteCells = Array.Empty<Vector2Int>();
        }

        private string FormatRouteMessage(string prefix)
        {
            var stepCount = transporterRoutePoints.Length;
            var stepNumber = Mathf.Clamp(transporterRouteIndex + 1, 1, Mathf.Max(1, stepCount));
            var terrain = transporterRouteCells.Length > transporterRouteIndex && transporterRouteIndex >= 0
                ? FormatTerrainName(GetTerrainKind(transporterRouteCells[transporterRouteIndex]))
                : "--";
            return $"{prefix} | step {stepNumber}/{stepCount} | {terrain}";
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
            var cellWidth = Mathf.Abs(fixedMapBottomRight.x - fixedMapTopLeft.x) / TacticalFieldColumns;
            var cellHeight = Mathf.Abs(fixedMapBottomRight.y - fixedMapTopLeft.y) / TacticalFieldRows;
            var cellSize = Mathf.Min(cellWidth, cellHeight);
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
            tacticalFieldMessage = "Field: 固定マップ待機中。戦域コンバートを実行してください。";
        }

        private void ApplyFixedFieldLines()
        {
            ClearTacticalField();
            startAnchor = GetFixedMapCellCenter(TransporterStartCell);
            goalAnchor = GetFixedMapCellCenter(new Vector2Int(EnemyGoalLineX, 0));
            startSource = "fixed x=-3";
            goalSource = $"fixed x={EnemyGoalLineX}";
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
            foreach (var cell in EnumerateAllGridCells())
            {
                terrainByCell[cell] = TerrainKind.Plain;
            }

            var candidates = new List<Vector2Int>();
            for (var y = MaxGridY; y >= MinGridY; y--)
            {
                for (var x = MinGridX; x <= MaxGridX; x++)
                {
                    var logical = new Vector2Int(x, y);
                    if (x == PlayerStartLineX || x == EnemyGoalLineX || logical == scoutGridPosition || logical == TransporterStartCell || logical == BuilderStartCell)
                    {
                        continue;
                    }

                    candidates.Add(logical);
                }
            }

            var seed = randomizePhase5TerrainEachConvert
                ? Guid.NewGuid().GetHashCode()
                : phase5RandomSeed + Mathf.RoundToInt(startAnchor.x + startAnchor.y + goalAnchor.x + goalAnchor.y);
            var random = new System.Random(seed);
            while (obstacleCells.Count < phase5ObstacleCount && candidates.Count > 0)
            {
                var index = random.Next(candidates.Count);
                var cell = candidates[index];
                terrainByCell[cell] = TerrainKind.Debris;
                obstacleCells.Add(cell);
                candidates.RemoveAt(index);
            }

            var roughTargetCount = Mathf.Max(0, phase51RoughCellCount);
            while (roughCells.Count < roughTargetCount && candidates.Count > 0)
            {
                var index = random.Next(candidates.Count);
                var cell = candidates[index];
                terrainByCell[cell] = TerrainKind.Rough;
                roughCells.Add(cell);
                candidates.RemoveAt(index);
            }

            MarkScanned(ScoutStartCell);
            MarkScanned(TransporterStartCell);
            MarkScanned(BuilderStartCell);
            scoutMessage = FormatScoutMessage($"マップ生成: デブリ{obstacleCells.Count} / 荒れ地{roughCells.Count}");
        }

        private void ResetScoutDiscovery()
        {
            terrainByCell.Clear();
            scannedCells.Clear();
            obstacleCells.Clear();
            detectedObstacleCells.Clear();
            roughCells.Clear();
            scoutGridPosition = ScoutStartCell;
            enemyGridPosition = new Vector2Int(EnemyGoalLineX, 0);
            enemyDetected = false;
            scoutMessage = "Scout: 戦域コンバート後にscanできます。";
        }

        private void MoveScoutBy(Vector2Int delta, string directionName)
        {
            if (tacticalCellPoints.Length == 0)
            {
                scoutMessage = "Scout: 移動前に戦域コンバートしてください。";
                RefreshRuntimeUi();
                return;
            }

            if (scoutCube == null || !scoutCube.isConnected)
            {
                scoutMessage = "Scout: 先に味方チームを接続してください。";
                RefreshRuntimeUi();
                return;
            }

            var next = scoutGridPosition + delta;
            if (!IsInsideGrid(next))
            {
                scoutMessage = FormatScoutMessage($"移動不可: 戦域外 ({directionName})");
                RefreshRuntimeUi();
                return;
            }

            if (!TryGetCellPoint(next, out var target))
            {
                scoutMessage = FormatScoutMessage($"移動不可: セル座標なし ({directionName})");
                RefreshRuntimeUi();
                return;
            }

            if (IsFriendlyReservedCell(next))
            {
                scoutMessage = FormatScoutMessage($"移動不可: 味方駒あり ({directionName})");
                RefreshRuntimeUi();
                return;
            }

            if (!IsKnownPassableCell(next))
            {
                scoutMessage = FormatScoutMessage(FormatBlockedMoveReason(next, directionName));
                RefreshRuntimeUi();
                return;
            }

            MoveScoutTo(next, directionName);
        }

        private void MoveScoutTo(Vector2Int next, string directionName)
        {
            if (!TryGetCellPoint(next, out var target))
            {
                scoutMessage = FormatScoutMessage($"移動不可: セル座標なし ({directionName})");
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
                maxSpd: ResolveMoveSpeedForCell(next, scoutMoveMaxSpeed)
            );
            scoutGridPosition = next;
            scoutMessage = FormatScoutMessage($"Scout移動: {directionName} / {FormatTerrainName(GetTerrainKind(next))}");
            RenderTacticalField(tacticalCellPoints);
            RefreshRuntimeUi();
            SetFieldView(true);
            TryAutoStartTransporter("Auto搬送開始");
        }

        private bool IsFriendlyReservedCell(Vector2Int logical)
        {
            if (logical == TransporterStartCell && observationCube != null && observationCube.isConnected)
            {
                return true;
            }

            return logical == BuilderStartCell && builderCube != null && builderCube.isConnected;
        }

        private bool IsFriendlyOccupiedForTransporterRoute(Vector2Int logical, Vector2Int transporterStart)
        {
            if (logical == transporterStart)
            {
                return false;
            }

            if (scoutCube != null && scoutCube.isConnected && logical == scoutGridPosition)
            {
                return true;
            }

            return builderCube != null && builderCube.isConnected && logical == BuilderStartCell;
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

        private static IEnumerable<Vector2Int> EnumerateAllGridCells()
        {
            for (var y = MaxGridY; y >= MinGridY; y--)
            {
                for (var x = MinGridX; x <= MaxGridX; x++)
                {
                    yield return new Vector2Int(x, y);
                }
            }
        }

        private void MarkScanned(Vector2Int logical)
        {
            if (!IsInsideGrid(logical))
            {
                return;
            }

            scannedCells.Add(logical);
            if (GetTerrainKind(logical) == TerrainKind.Debris)
            {
                detectedObstacleCells.Add(logical);
            }
        }

        private TerrainKind GetTerrainKind(Vector2Int logical)
        {
            return terrainByCell.TryGetValue(logical, out var terrain) ? terrain : TerrainKind.Plain;
        }

        private bool IsKnownPassableCell(Vector2Int logical)
        {
            return IsInsideGrid(logical) && scannedCells.Contains(logical) && GetTerrainKind(logical) != TerrainKind.Debris;
        }

        private int ResolveMoveSpeedForCell(Vector2Int logical, int baseSpeed)
        {
            if (GetTerrainKind(logical) != TerrainKind.Rough)
            {
                return baseSpeed;
            }

            return Mathf.Max(12, Mathf.RoundToInt(baseSpeed * Mathf.Clamp01(roughCellSpeedMultiplier)));
        }

        private string FormatBlockedMoveReason(Vector2Int logical, string directionName)
        {
            if (!scannedCells.Contains(logical))
            {
                return $"移動不可: 未スキャン ({directionName})";
            }

            if (GetTerrainKind(logical) == TerrainKind.Debris)
            {
                return $"移動不可: デブリ ({directionName})";
            }

            return $"移動不可: {directionName}";
        }

        private static string FormatTerrainName(TerrainKind terrain)
        {
            return terrain switch
            {
                TerrainKind.Rough => "荒れ地",
                TerrainKind.Debris => "デブリ",
                _ => "平地"
            };
        }

        private Color ResolveCellColor(Vector2Int logical, int row)
        {
            if (!scannedCells.Contains(logical))
            {
                return UnknownCellColor;
            }

            var terrain = GetTerrainKind(logical);
            if (terrain == TerrainKind.Debris)
            {
                return ObstacleColor;
            }

            if (terrain == TerrainKind.Rough)
            {
                return RoughCellColor;
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

            return PlainCellColor;
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
            CreateText("Title", header.transform, "toio Tactical Field | オルディア机上戦線", 30, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor, new Vector2(-72f, 15f), new Vector2(676f, 40f));
            CreateText("Phase", header.transform, $"{VersionLabel} | スキャン済みルート制御", 18, FontStyle.Bold, TextAnchor.MiddleCenter, StartColor, new Vector2(-72f, -23f), new Vector2(676f, 26f));
            CreateButton("FieldView", header.transform, "盤面を見る", new Vector2(260f, 0f), new Vector2(150f, 42f), GoalColor, OnShowFieldView);

            var status = CreatePanel("Status", controlView.transform, new Vector2(0f, 1f), new Vector2(24f, -184f), new Vector2(450f, 382f), CardColor, true);
            connectionStatusLabel = CreateText("Connection", status.transform, string.Empty, 15, FontStyle.Bold, TextAnchor.UpperLeft, TextColor, new Vector2(20f, -16f), new Vector2(410f, 48f), true);
            roleStatusLabel = CreateText("Roles", status.transform, string.Empty, 13, FontStyle.Bold, TextAnchor.UpperLeft, GoalColor, new Vector2(20f, -66f), new Vector2(410f, 70f), true);
            setupGuideLabel = CreateText("SetupGuide", status.transform, string.Empty, 13, FontStyle.Bold, TextAnchor.UpperLeft, TextColor, new Vector2(20f, -140f), new Vector2(410f, 50f), true);
            observationStatusLabel = CreateText("Observation", status.transform, string.Empty, 15, FontStyle.Bold, TextAnchor.UpperLeft, StartColor, new Vector2(20f, -190f), new Vector2(410f, 42f), true);
            scoutStatusLabel = CreateText("Scout", status.transform, string.Empty, 15, FontStyle.Bold, TextAnchor.UpperLeft, ScoutColor, new Vector2(20f, -240f), new Vector2(410f, 46f), true);
            anchorStatusLabel = CreateText("Anchors", status.transform, string.Empty, 13, FontStyle.Normal, TextAnchor.UpperLeft, MutedTextColor, new Vector2(20f, -286f), new Vector2(410f, 58f), true);
            victoryStatusLabel = CreateText("Victory", status.transform, string.Empty, 22, FontStyle.Bold, TextAnchor.UpperLeft, GoalColor, new Vector2(20f, -344f), new Vector2(410f, 30f), true);

            var actions = CreatePanel("Actions", controlView.transform, new Vector2(1f, 1f), new Vector2(-24f, -184f), new Vector2(330f, 588f), CardColor, false, true);
            CreateButton("Connect", actions.transform, "味方3台を接続", new Vector2(0f, -26f), new Vector2(276f, 44f), StartColor, OnConnectObservationCube, true);
            CreateButton("FixedLines", actions.transform, "固定ライン確認", new Vector2(0f, -78f), new Vector2(276f, 40f), GoalColor, OnUseFixedFieldLines, true);
            CreateButton("Convert", actions.transform, "戦域コンバート", new Vector2(0f, -126f), new Vector2(276f, 44f), GridStartColor, OnConvertTacticalField, true);

            CreateText("TransporterTitle", actions.transform, "Transporter | スキャン済み最短ルート", 13, FontStyle.Bold, TextAnchor.MiddleCenter, StartColor, new Vector2(0f, -168f), new Vector2(286f, 22f), true);
            CreateButton("Run", actions.transform, "移動開始", new Vector2(-72f, -200f), new Vector2(132f, 34f), StartColor, OnRunTransporter, true);
            CreateButton("StopRun", actions.transform, "移動中断", new Vector2(72f, -200f), new Vector2(132f, 34f), ObstacleColor, OnStopTransporter, true);
            CreateButton("RetryStart", actions.transform, "開始ライン再配置", new Vector2(0f, -242f), new Vector2(276f, 34f), GoalColor, OnRetryStartLineMoves, true);
            CreateButton("BuilderAppeal", actions.transform, "Builder自己アピール", new Vector2(0f, -282f), new Vector2(276f, 34f), LineColor, OnBuilderAppeal, true);
            CreateButton("Clear", actions.transform, "戦域リセット", new Vector2(-72f, -322f), new Vector2(132f, 32f), LineColor, OnClearAnchors, true);
            CreateButton("Back", actions.transform, "Launcherへ", new Vector2(72f, -322f), new Vector2(132f, 32f), MutedTextColor, OnBackToLauncher, true);

            CreateText("ScoutTitle", actions.transform, "Scout | 1マス移動 / 半径2 scan", 14, FontStyle.Bold, TextAnchor.MiddleCenter, ScoutColor, new Vector2(0f, -366f), new Vector2(286f, 24f), true);
            CreateButton("ScoutForward", actions.transform, "前へ", new Vector2(0f, -398f), new Vector2(132f, 30f), ScoutColor, OnScoutForward, true);
            CreateButton("ScoutLeft", actions.transform, "左へ", new Vector2(-72f, -432f), new Vector2(132f, 30f), ScoutColor, OnScoutLeft, true);
            CreateButton("ScoutScan", actions.transform, "scan", new Vector2(72f, -432f), new Vector2(132f, 30f), GoalColor, OnScoutScan, true);
            CreateButton("ScoutRight", actions.transform, "右へ", new Vector2(-72f, -466f), new Vector2(132f, 30f), ScoutColor, OnScoutRight, true);
            CreateButton("ScoutBack", actions.transform, "後ろへ", new Vector2(72f, -466f), new Vector2(132f, 30f), ScoutColor, OnScoutBack, true);
            CreateButton("ScoutAuto", actions.transform, "Scout自動", new Vector2(-72f, -506f), new Vector2(132f, 30f), ScoutColor, OnAutoScoutRoute, true);
            CreateButton("AutoTransporter", actions.transform, "Auto搬送", new Vector2(72f, -506f), new Vector2(132f, 30f), StartColor, OnToggleAutoTransporter, true);

            fieldView = CreateUiObject("FieldView", root.transform);
            StretchFull(fieldView.GetComponent<RectTransform>());
            var fieldViewBar = CreatePanel("FieldViewBar", fieldView.transform, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(780f, 54f), CardColor);
            fieldViewStatusLabel = CreateText("FieldViewStatus", fieldViewBar.transform, string.Empty, 18, FontStyle.Bold, TextAnchor.MiddleLeft, TextColor, new Vector2(-90f, 0f), new Vector2(560f, 38f));
            CreateButton("ReturnToControls", fieldViewBar.transform, "操作へ戻る", new Vector2(285f, 0f), new Vector2(180f, 36f), GoalColor, OnShowControlView);
            var fieldTransportPanel = CreatePanel("FieldTransportControls", fieldView.transform, new Vector2(1f, 0f), new Vector2(-190f, 268f), new Vector2(326f, 160f), CardColor);
            CreateText("FieldTransportTitle", fieldTransportPanel.transform, "Transporter", 15, FontStyle.Bold, TextAnchor.MiddleCenter, StartColor, new Vector2(0f, 62f), new Vector2(286f, 24f));
            CreateButton("FieldTransportRun", fieldTransportPanel.transform, "移動開始", new Vector2(-72f, 26f), new Vector2(132f, 32f), StartColor, OnRunTransporter);
            CreateButton("FieldTransportStop", fieldTransportPanel.transform, "移動中断", new Vector2(72f, 26f), new Vector2(132f, 32f), ObstacleColor, OnStopTransporter);
            CreateButton("FieldBuilderAppeal", fieldTransportPanel.transform, "Builderアピール", new Vector2(0f, -12f), new Vector2(276f, 30f), LineColor, OnBuilderAppeal);
            CreateButton("FieldAutoTransporter", fieldTransportPanel.transform, "Auto搬送", new Vector2(0f, -50f), new Vector2(276f, 30f), StartColor, OnToggleAutoTransporter);
            var fieldScoutPanel = CreatePanel("FieldScoutControls", fieldView.transform, new Vector2(1f, 0f), new Vector2(-190f, 60f), new Vector2(326f, 202f), CardColor);
            CreateText("FieldScoutTitle", fieldScoutPanel.transform, "Scout", 15, FontStyle.Bold, TextAnchor.MiddleCenter, ScoutColor, new Vector2(0f, 84f), new Vector2(286f, 24f));
            CreateButton("FieldScoutForward", fieldScoutPanel.transform, "前へ", new Vector2(0f, 52f), new Vector2(132f, 30f), ScoutColor, OnScoutForward);
            CreateButton("FieldScoutLeft", fieldScoutPanel.transform, "左へ", new Vector2(-72f, 16f), new Vector2(132f, 30f), ScoutColor, OnScoutLeft);
            CreateButton("FieldScoutScan", fieldScoutPanel.transform, "scan", new Vector2(72f, 16f), new Vector2(132f, 30f), GoalColor, OnScoutScan);
            CreateButton("FieldScoutRight", fieldScoutPanel.transform, "右へ", new Vector2(-72f, -20f), new Vector2(132f, 30f), ScoutColor, OnScoutRight);
            CreateButton("FieldScoutBack", fieldScoutPanel.transform, "後ろへ", new Vector2(72f, -20f), new Vector2(132f, 30f), ScoutColor, OnScoutBack);
            CreateButton("FieldScoutAuto", fieldScoutPanel.transform, "Scout自動", new Vector2(0f, -58f), new Vector2(276f, 30f), ScoutColor, OnAutoScoutRoute);
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
                $"開始ライン: {(hasStartAnchor ? FormatPoint(startAnchor, startSource) : "--")}\n" +
                $"ゴール側:   {(hasGoalAnchor ? FormatPoint(goalAnchor, goalSource) : "--")}\n" +
                $"Grid: x {MinGridX}..{MaxGridX} / y {MaxGridY}..{MinGridY}\n" +
                $"経路: {FormatRouteStatus()} | " +
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
                return "完了";
            }

            if (isTransporterMoving && transporterRoutePoints.Length > 0 && transporterRouteIndex >= 0)
            {
                return $"{transporterRouteIndex + 1}/{transporterRoutePoints.Length}";
            }

            return tacticalCellPoints.Length > 0 ? "待機" : "--";
        }

        private string FormatRoleMessage()
        {
            return
                $"Transporter: {FormatRoleCube(observationCube)}\n" +
                $"Scout: {FormatRoleCube(scoutCube)} | Builder: {FormatRoleCube(builderCube)}";
        }

        private string FormatScoutMessage(string prefix)
        {
            return $"{prefix} | Scout ({scoutGridPosition.x},{scoutGridPosition.y}) | 解明 {scannedCells.Count}/{TacticalFieldColumns * TacticalFieldRows} | デブリ {detectedObstacleCells.Count}/{obstacleCells.Count}";
        }

        private static string FormatRoleCube(Cube cube)
        {
            return cube != null && cube.isConnected ? GetCubeLabel(cube) : "--";
        }

        private static string GetCubeLabel(Cube cube)
        {
            return cube == null || string.IsNullOrEmpty(cube.addr) ? "cube" : cube.addr;
        }

        private static string GetCubeIdentity(Cube cube)
        {
            if (cube == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(cube.addr))
            {
                return cube.addr;
            }

            if (!string.IsNullOrEmpty(cube.id))
            {
                return cube.id;
            }

            return cube.GetHashCode().ToString();
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
