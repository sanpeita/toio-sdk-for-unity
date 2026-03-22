using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using toio;

namespace toio.Experiments.ToioBlenderLab
{
    [DisallowMultipleComponent]
    public class ToioBlenderCubeInput : MonoBehaviour
    {
        public enum EditMacroAction
        {
            None = 0,
            AddCube = 1,
            AddPlane = 2,
            MaterialPreview = 3,
            Solid = 4
        }

        private enum LatchedAction
        {
            None = 0,
            Orbit = 1,
            Zoom = 2
        }

        private enum EditTiltState
        {
            Neutral = 0,
            Forward = 1,
            Backward = 2,
            Left = 3,
            Right = 4
        }

        [Header("Connection")]
        [SerializeField] private ConnectType connectType = ConnectType.Real;
        [SerializeField] private bool connectOnStart = false;
        [SerializeField] private int connectMaxAttempts = 3;
        [SerializeField] private int retryDelayMs = 1200;
        [SerializeField] private int connectCleanupDelayMs = 600;

        [Header("Cube 1 View Controls")]
        [SerializeField] private float orbitStartThresholdDeg = 7f;
        [SerializeField] private float orbitReleaseThresholdDeg = 4f;
        [SerializeField] private float zoomStartThresholdDeg = 7f;
        [SerializeField] private float zoomReleaseThresholdDeg = 4f;
        [SerializeField] private float maxInputAngleDeg = 24f;
        [SerializeField] private bool invertOrbitDirection = false;
        [SerializeField] private bool invertZoomDirection = false;

        [Header("Cube 1 Button")]
        [SerializeField] private bool requireUpPoseForModeToggle = false;
        [SerializeField] private float modeToggleNeutralThresholdDeg = 9f;
        [SerializeField] private float modeToggleCooldownSeconds = 0.2f;

        [Header("Cube 2 Edit Macros")]
        [SerializeField] private bool useEditPoseForMacros = true;
        [SerializeField] private float editPoseActivationHoldSeconds = 0.08f;
        [SerializeField] private float editMacroStartThresholdDeg = 10f;
        [SerializeField] private float editMacroReleaseThresholdDeg = 5f;
        [SerializeField] private float editMacroNeutralHoldSeconds = 0.15f;
        [SerializeField] private float editMacroCooldownSeconds = 0.35f;
        [SerializeField] private float editButtonExecuteCooldownSeconds = 2.0f;
        [SerializeField] private float editButtonPressDebounceSeconds = 0.05f;
        [SerializeField] private float editButtonReleaseDebounceSeconds = 0.08f;

        [Header("Sensors")]
        [SerializeField] private int attitudeIntervalMs = 50;
        [SerializeField] private float motionSensorRefreshSeconds = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool logStateChanges = false;

        private CubeManager cubeManager;
        private Cube viewCube;
        private Cube editCube;
        private string viewListenerKey;
        private string editListenerKey;
        private bool isConnecting;
        private bool hasViewPose;
        private bool hasEditPose;
        private bool viewButtonPressed;
        private bool editButtonPressed;
        private int pendingModeToggleCount;
        private float nextMotionSensorRequestAt;
        private float lastModeToggleAt = -999f;
        private Vector3 viewEulers;
        private Vector3 editEulers;
        private Cube.PoseType viewPose = Cube.PoseType.Up;
        private Cube.PoseType editPose = Cube.PoseType.Up;
        private string connectionMessage = "Not connected. Press Connect Cubes.";
        private LatchedAction latchedAction;
        private EditTiltState editTiltState;
        private EditTiltState observedEditPoseTiltState;
        private float observedEditPoseTiltSince = -1f;
        private float lastEditMacroQueuedAt = -999f;
        private float lastEditExecuteAt = -999f;
        private float lastEditButtonStateChangedAt = -999f;
        private bool editButtonExecutedForCurrentPress;
        private float editNeutralSince = -1f;
        private float orbitAxis;
        private float zoomAxis;
        private readonly Queue<EditMacroAction> pendingEditMacroQueue = new Queue<EditMacroAction>();
        private EditMacroAction lastQueuedEditMacro = EditMacroAction.None;
        private EditMacroAction selectedAddMacro = EditMacroAction.AddPlane;

        public bool IsConnected =>
            viewCube != null && viewCube.isConnected &&
            editCube != null && editCube.isConnected;

        public bool IsConnecting => isConnecting;
        public string ConnectionMessage => connectionMessage;
        public Vector3 LastEulers => viewEulers;
        public Cube.PoseType LastPose => viewPose;
        public bool HasPose => hasViewPose;
        public bool ButtonPressed => viewButtonPressed;
        public float OrbitAxis => orbitAxis;
        public float ZoomAxis => zoomAxis;
        public bool IsReadyForModeToggle => EvaluateModeToggleReady();
        public Cube ViewCube => viewCube;
        public Cube EditCube => editCube;
        public Vector3 ViewCubeEulers => viewEulers;
        public Vector3 EditCubeEulers => editEulers;
        public Cube.PoseType ViewCubePose => viewPose;
        public Cube.PoseType EditCubePose => editPose;
        public bool HasViewCubePose => hasViewPose;
        public bool HasEditCubePose => hasEditPose;
        public bool ViewCubeButtonPressed => viewButtonPressed;
        public bool EditCubeButtonPressed => editButtonPressed;
        public int PendingEditMacroCount => pendingEditMacroQueue.Count;
        public string LastQueuedEditMacroLabel => GetEditMacroLabel(lastQueuedEditMacro);
        public string SelectedAddMacroLabel => GetEditMacroLabel(selectedAddMacro);

        public string CurrentActionSummary
        {
            get
            {
                if (Mathf.Abs(orbitAxis) > 0.001f)
                {
                    return orbitAxis > 0f ? "Orbit Right" : "Orbit Left";
                }

                if (Mathf.Abs(zoomAxis) > 0.001f)
                {
                    return zoomAxis > 0f ? "Zoom In" : "Zoom Out";
                }

                if (pendingModeToggleCount > 0)
                {
                    return "Tab Queued";
                }

                if (pendingEditMacroQueue.Count > 0)
                {
                    return $"Macro Queued: {GetEditMacroLabel(pendingEditMacroQueue.Peek())}";
                }

                if (editTiltState != EditTiltState.Neutral)
                {
                    return $"Cube 2 Hold: {GetEditTiltLabel(editTiltState)}";
                }

                return $"Cube 2 Ready: {SelectedAddMacroLabel}";
            }
        }

        private void Awake()
        {
            viewListenerKey = $"{nameof(ToioBlenderCubeInput)}_View_{GetInstanceID()}";
            editListenerKey = $"{nameof(ToioBlenderCubeInput)}_Edit_{GetInstanceID()}";
        }

        private async void Start()
        {
            if (connectOnStart)
            {
                await Connect();
            }
        }

        private void Update()
        {
            UpdateLatchedAction();
            UpdateEditMacroState();
            UpdateEditButtonExecution();

            if (!IsConnected)
            {
                return;
            }

            if (motionSensorRefreshSeconds > 0f && Time.unscaledTime >= nextMotionSensorRequestAt)
            {
                viewCube?.RequestMotionSensor();
                editCube?.RequestMotionSensor();
                nextMotionSensorRequestAt = Time.unscaledTime + motionSensorRefreshSeconds;
            }
        }

        private void OnDestroy()
        {
            RemoveListeners();
            DisconnectAllImmediate();
        }

        public async UniTask Connect()
        {
            if (IsConnected || isConnecting)
            {
                return;
            }

            isConnecting = true;
            connectionMessage = "Scanning for two cubes...";

            try
            {
                var cubes = await ConnectCubePair();
                if (cubes == null || cubes.Length < 2)
                {
                    connectionMessage = "Two cubes were not confirmed. Press Connect Cubes again.";
                    return;
                }

                viewCube = cubes[0];
                editCube = cubes[1];
                await UniTask.WhenAll(
                    RegisterCube(viewCube, viewListenerKey, OnViewAttitude, OnViewButton, OnViewPose),
                    RegisterCube(editCube, editListenerKey, OnEditAttitude, OnEditButton, OnEditPose)
                );

                nextMotionSensorRequestAt = Time.unscaledTime + motionSensorRefreshSeconds;
                connectionMessage =
                    $"Connected. Cube 1={GetCubeDebugName(viewCube, "cube1")} keeps Orbit/Zoom/Tab. Cube 2={GetCubeDebugName(editCube, "cube2")} selects add target on tilt and executes on button.";
            }
            catch (Exception ex)
            {
                connectionMessage = $"Connection failed: {ex.Message}";
                Debug.LogException(ex);
            }
            finally
            {
                isConnecting = false;
            }
        }

        public bool ConsumeModeToggleRequested()
        {
            if (pendingModeToggleCount <= 0)
            {
                return false;
            }

            pendingModeToggleCount--;
            return true;
        }

        public void ClearPendingModeToggles()
        {
            pendingModeToggleCount = 0;
        }

        public bool ConsumePendingEditMacro(out EditMacroAction action)
        {
            if (pendingEditMacroQueue.Count <= 0)
            {
                action = EditMacroAction.None;
                return false;
            }

            action = pendingEditMacroQueue.Dequeue();
            return true;
        }

        public void ClearPendingEditMacros()
        {
            pendingEditMacroQueue.Clear();
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

                await DisconnectAll();
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

        private async UniTask DisconnectAll()
        {
            DisconnectAllImmediate();

            if (connectCleanupDelayMs > 0)
            {
                await UniTask.Delay(connectCleanupDelayMs);
            }
        }

        private void DisconnectAllImmediate()
        {
            if (cubeManager != null)
            {
                foreach (var cube in GetOrderedConnectedCubes())
                {
                    cubeManager.Disconnect(cube);
                }
            }

            viewCube = null;
            editCube = null;
            ResetTransientState();
        }

        private async UniTask RegisterCube(
            Cube cube,
            string listenerKey,
            Action<Cube> attitudeHandler,
            Action<Cube> buttonHandler,
            Action<Cube> poseHandler
        )
        {
            if (cube == null)
            {
                return;
            }

            cube.attitudeCallback.RemoveListener(listenerKey);
            cube.buttonCallback.RemoveListener(listenerKey);
            cube.poseCallback.RemoveListener(listenerKey);

            cube.attitudeCallback.AddListener(listenerKey, attitudeHandler);
            cube.buttonCallback.AddListener(listenerKey, buttonHandler);
            cube.poseCallback.AddListener(listenerKey, poseHandler);

            await cube.ConfigAttitudeSensor(
                Cube.AttitudeFormat.Eulers,
                attitudeIntervalMs,
                Cube.AttitudeNotificationType.OnChanged
            );
            cube.RequestAttitudeSensor(Cube.AttitudeFormat.Eulers);
            cube.RequestMotionSensor();
            await UniTask.Delay(100);
        }

        private void OnViewAttitude(Cube cube)
        {
            viewEulers = cube.eulers;
            if (logStateChanges)
            {
                Debug.Log($"Blender cube1 attitude => {viewEulers}");
            }
        }

        private void OnEditAttitude(Cube cube)
        {
            editEulers = cube.eulers;
            if (logStateChanges)
            {
                Debug.Log($"Blender cube2 attitude => {editEulers}");
            }
        }

        private void OnViewPose(Cube cube)
        {
            viewPose = cube.pose;
            hasViewPose = true;
        }

        private void OnEditPose(Cube cube)
        {
            editPose = cube.pose;
            hasEditPose = true;
        }

        private void OnViewButton(Cube cube)
        {
            var wasPressed = viewButtonPressed;
            viewButtonPressed = cube.isPressed;

            if (!viewButtonPressed || wasPressed)
            {
                return;
            }

            if (!EvaluateModeToggleReady())
            {
                return;
            }

            if (Time.unscaledTime - lastModeToggleAt < modeToggleCooldownSeconds)
            {
                return;
            }

            pendingModeToggleCount++;
            lastModeToggleAt = Time.unscaledTime;
        }

        private void OnEditButton(Cube cube)
        {
            if (editButtonPressed == cube.isPressed)
            {
                return;
            }

            editButtonPressed = cube.isPressed;
            lastEditButtonStateChangedAt = Time.unscaledTime;
        }

        private void UpdateLatchedAction()
        {
            var orbitValue = EvaluateOrbitAxis();
            var zoomValue = EvaluateZoomAxis();
            var orbitMagnitude = Mathf.Abs(orbitValue);
            var zoomMagnitude = Mathf.Abs(zoomValue);

            switch (latchedAction)
            {
                case LatchedAction.None:
                    if (orbitMagnitude > 0f || zoomMagnitude > 0f)
                    {
                        latchedAction = orbitMagnitude >= zoomMagnitude ? LatchedAction.Orbit : LatchedAction.Zoom;
                    }
                    break;
                case LatchedAction.Orbit:
                    if (orbitMagnitude <= 0f)
                    {
                        latchedAction = zoomMagnitude > 0f ? LatchedAction.Zoom : LatchedAction.None;
                    }
                    break;
                case LatchedAction.Zoom:
                    if (zoomMagnitude <= 0f)
                    {
                        latchedAction = orbitMagnitude > 0f ? LatchedAction.Orbit : LatchedAction.None;
                    }
                    break;
            }

            orbitAxis = latchedAction == LatchedAction.Orbit ? orbitValue : 0f;
            zoomAxis = latchedAction == LatchedAction.Zoom ? zoomValue : 0f;
        }

        private void UpdateEditMacroState()
        {
            if (!IsConnected)
            {
                editTiltState = EditTiltState.Neutral;
                editNeutralSince = -1f;
                return;
            }

            if (useEditPoseForMacros && hasEditPose)
            {
                UpdateEditMacroStateFromPose();
                return;
            }

            UpdateEditMacroStateFromEulers();
        }

        private void UpdateEditMacroStateFromPose()
        {
            var nextTiltState = EvaluateEditTiltStateFromPose();
            if (nextTiltState != observedEditPoseTiltState)
            {
                observedEditPoseTiltState = nextTiltState;
                observedEditPoseTiltSince = Time.unscaledTime;

                if (logStateChanges)
                {
                    Debug.Log($"Blender cube2 pose candidate => {GetEditTiltLabel(nextTiltState)}");
                }
            }

            var observedForSeconds = observedEditPoseTiltSince < 0f
                ? 0f
                : Time.unscaledTime - observedEditPoseTiltSince;

            if (nextTiltState == EditTiltState.Neutral)
            {
                if (editTiltState != EditTiltState.Neutral && observedForSeconds >= editMacroNeutralHoldSeconds)
                {
                    editTiltState = EditTiltState.Neutral;
                    editNeutralSince = Time.unscaledTime;
                }

                return;
            }

            if (observedForSeconds < editPoseActivationHoldSeconds)
            {
                return;
            }

            if (Time.unscaledTime - lastEditMacroQueuedAt < editMacroCooldownSeconds)
            {
                return;
            }

            if (editTiltState != EditTiltState.Neutral)
            {
                return;
            }

            editNeutralSince = -1f;
            editTiltState = nextTiltState;
            lastEditMacroQueuedAt = Time.unscaledTime;
            HandleEditTiltTriggered(nextTiltState);
        }

        private void UpdateEditMacroStateFromEulers()
        {
            var activeThreshold = editTiltState == EditTiltState.Neutral
                ? editMacroStartThresholdDeg
                : editMacroReleaseThresholdDeg;

            var nextTiltState = EvaluateEditTiltState(activeThreshold);
            if (nextTiltState == EditTiltState.Neutral)
            {
                if (editTiltState != EditTiltState.Neutral)
                {
                    if (editNeutralSince < 0f)
                    {
                        editNeutralSince = Time.unscaledTime;
                    }

                    if (Time.unscaledTime - editNeutralSince >= editMacroNeutralHoldSeconds)
                    {
                        editTiltState = EditTiltState.Neutral;
                    }
                }

                return;
            }

            editNeutralSince = -1f;
            if (editTiltState != EditTiltState.Neutral)
            {
                return;
            }

            if (Time.unscaledTime - lastEditMacroQueuedAt < editMacroCooldownSeconds)
            {
                return;
            }

            editTiltState = nextTiltState;
            lastEditMacroQueuedAt = Time.unscaledTime;
            HandleEditTiltTriggered(nextTiltState);
        }

        private EditTiltState EvaluateEditTiltStateFromPose()
        {
            switch (editPose)
            {
                case Cube.PoseType.Front:
                    return EditTiltState.Forward;
                case Cube.PoseType.Back:
                    return EditTiltState.Backward;
                case Cube.PoseType.Left:
                    return EditTiltState.Left;
                case Cube.PoseType.Right:
                    return EditTiltState.Right;
                default:
                    return EditTiltState.Neutral;
            }
        }

        private void UpdateEditButtonExecution()
        {
            if (!IsConnected)
            {
                editButtonExecutedForCurrentPress = false;
                return;
            }

            var timeSinceButtonChanged = Time.unscaledTime - lastEditButtonStateChangedAt;
            if (editButtonPressed)
            {
                if (editButtonExecutedForCurrentPress || timeSinceButtonChanged < editButtonPressDebounceSeconds)
                {
                    return;
                }

                if (Time.unscaledTime - lastEditExecuteAt < editButtonExecuteCooldownSeconds)
                {
                    return;
                }

                QueueEditMacroAction(selectedAddMacro);
                lastEditExecuteAt = Time.unscaledTime;
                editButtonExecutedForCurrentPress = true;
                return;
            }

            if (timeSinceButtonChanged >= editButtonReleaseDebounceSeconds)
            {
                editButtonExecutedForCurrentPress = false;
            }
        }

        private float EvaluateOrbitAxis()
        {
            return EvaluateSignedAxis(
                viewEulers.x,
                latchedAction == LatchedAction.Orbit ? orbitReleaseThresholdDeg : orbitStartThresholdDeg,
                invertOrbitDirection
            );
        }

        private float EvaluateZoomAxis()
        {
            return EvaluateSignedAxis(
                viewEulers.y,
                latchedAction == LatchedAction.Zoom ? zoomReleaseThresholdDeg : zoomStartThresholdDeg,
                invertZoomDirection
            );
        }

        private float EvaluateSignedAxis(float value, float threshold, bool invert)
        {
            if (!IsConnected)
            {
                return 0f;
            }

            var absolute = Mathf.Abs(value);
            if (absolute < threshold)
            {
                return 0f;
            }

            var normalized = Mathf.InverseLerp(threshold, Mathf.Max(threshold + 0.01f, maxInputAngleDeg), absolute);
            var signed = Mathf.Sign(value) * normalized;
            if (invert)
            {
                signed = -signed;
            }

            return Mathf.Clamp(signed, -1f, 1f);
        }

        private EditTiltState EvaluateEditTiltState(float threshold)
        {
            var horizontalAbs = Mathf.Abs(editEulers.x);
            var verticalAbs = Mathf.Abs(editEulers.y);
            if (horizontalAbs < threshold && verticalAbs < threshold)
            {
                return EditTiltState.Neutral;
            }

            if (verticalAbs >= horizontalAbs)
            {
                return editEulers.y <= 0f ? EditTiltState.Forward : EditTiltState.Backward;
            }

            return editEulers.x >= 0f ? EditTiltState.Right : EditTiltState.Left;
        }

        private void HandleEditTiltTriggered(EditTiltState tiltState)
        {
            switch (tiltState)
            {
                case EditTiltState.Forward:
                    selectedAddMacro = EditMacroAction.AddPlane;
                    break;
                case EditTiltState.Backward:
                    selectedAddMacro = EditMacroAction.AddCube;
                    break;
                case EditTiltState.Left:
                    QueueEditMacroAction(EditMacroAction.Solid);
                    break;
                case EditTiltState.Right:
                    QueueEditMacroAction(EditMacroAction.MaterialPreview);
                    break;
            }

            if (logStateChanges)
            {
                Debug.Log($"Blender cube2 tilt triggered => {GetEditTiltLabel(tiltState)}");
            }
        }

        private void QueueEditMacroAction(EditMacroAction action)
        {
            if (action == EditMacroAction.None)
            {
                return;
            }

            pendingEditMacroQueue.Enqueue(action);
            lastQueuedEditMacro = action;

            if (logStateChanges)
            {
                Debug.Log($"Blender cube2 macro queued => {GetEditMacroLabel(action)}");
            }
        }

        private bool EvaluateModeToggleReady()
        {
            var uprightEnough = !requireUpPoseForModeToggle || !hasViewPose || viewPose == Cube.PoseType.Up;
            var neutralEnough = Mathf.Abs(viewEulers.x) <= modeToggleNeutralThresholdDeg && Mathf.Abs(viewEulers.y) <= modeToggleNeutralThresholdDeg;
            return uprightEnough && neutralEnough;
        }

        private void RemoveListeners()
        {
            RemoveListeners(viewCube, viewListenerKey);
            RemoveListeners(editCube, editListenerKey);
        }

        private static void RemoveListeners(Cube cube, string listenerKey)
        {
            if (cube == null)
            {
                return;
            }

            cube.attitudeCallback.RemoveListener(listenerKey);
            cube.buttonCallback.RemoveListener(listenerKey);
            cube.poseCallback.RemoveListener(listenerKey);
        }

        private void ResetTransientState()
        {
            hasViewPose = false;
            hasEditPose = false;
            viewButtonPressed = false;
            editButtonPressed = false;
            pendingModeToggleCount = 0;
            nextMotionSensorRequestAt = 0f;
            viewEulers = Vector3.zero;
            editEulers = Vector3.zero;
            viewPose = Cube.PoseType.Up;
            editPose = Cube.PoseType.Up;
            latchedAction = LatchedAction.None;
            editTiltState = EditTiltState.Neutral;
            observedEditPoseTiltState = EditTiltState.Neutral;
            observedEditPoseTiltSince = -1f;
            lastEditMacroQueuedAt = -999f;
            lastEditExecuteAt = -999f;
            lastEditButtonStateChangedAt = -999f;
            editButtonExecutedForCurrentPress = false;
            editNeutralSince = -1f;
            orbitAxis = 0f;
            zoomAxis = 0f;
            pendingEditMacroQueue.Clear();
            lastQueuedEditMacro = EditMacroAction.None;
            selectedAddMacro = EditMacroAction.AddPlane;
        }

        public static string GetEditMacroLabel(EditMacroAction action)
        {
            switch (action)
            {
                case EditMacroAction.AddCube:
                    return "Add Cube";
                case EditMacroAction.AddPlane:
                    return "Add Plane";
                case EditMacroAction.MaterialPreview:
                    return "Material Preview";
                case EditMacroAction.Solid:
                    return "Solid";
                default:
                    return "None";
            }
        }

        public static string GetCubeDebugName(Cube cube, string fallback)
        {
            if (cube == null)
            {
                return fallback;
            }

            var label = string.IsNullOrEmpty(cube.localName) ? "cube" : cube.localName;
            if (string.IsNullOrEmpty(cube.addr))
            {
                return label;
            }

            var suffix = cube.addr.Length <= 4 ? cube.addr : cube.addr.Substring(cube.addr.Length - 4);
            return $"{label}#{suffix}";
        }

        private static string GetEditTiltLabel(EditTiltState state)
        {
            switch (state)
            {
                case EditTiltState.Forward:
                    return "Forward -> Select Plane";
                case EditTiltState.Backward:
                    return "Backward -> Select Cube";
                case EditTiltState.Left:
                    return "Left -> Solid";
                case EditTiltState.Right:
                    return "Right -> Material Preview";
                default:
                    return "Neutral";
            }
        }
    }
}
