using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using toio;

namespace toio.Experiments.ToioLeftHandLab
{
    public class ToioLeftHandLabController : MonoBehaviour
    {
        private const string VersionLabel = "ver1.4";

        private enum ControlMode
        {
            OneStick = 0,
            TwinStick = 1
        }

        private enum HorizontalTiltState
        {
            Left = -1,
            Neutral = 0,
            Right = 1
        }

        [Header("Single Stick")]
        [SerializeField] private toio.Samples.Sample_Sensor.ToioWasdInput inputSource;
        [SerializeField] private bool showKeyboardFallbackHint = true;

        [Header("Twin Stick")]
        [SerializeField] private ControlMode selectedMode = ControlMode.OneStick;
        [SerializeField] private float twinTiltThresholdDeg = 8f;
        [SerializeField] private bool twinInvertTurnDirection = true;
        [SerializeField] private int twinAttitudeIntervalMs = 50;
        [SerializeField] private float twinMotionSensorRefreshSeconds = 0.5f;
        [SerializeField] private float shiftHoldSeconds = 2f;
        [SerializeField] private float ctrlHoldSeconds = 2f;
        [SerializeField] private int twinConnectMaxAttempts = 3;
        [SerializeField] private int twinRetryDelayMs = 1200;
        [SerializeField] private int twinConnectCleanupDelayMs = 600;

        [Header("UI")]
        [SerializeField] private int maxLogLength = 64;

        private Text textBattery;
        private Text textFlat;
        private Text textCollision;
        private Text textButton;
        private Text textPositionID;
        private Text textStandardID;
        private Text textAngle;
        private Text textDoubleTap;
        private Text textPose;
        private Text textShake;
        private Text textSpeed;
        private Text textMag;
        private Text textAttitude;
        private InputField keyLogInputField;
        private Text keyLogLabel;
        private readonly StringBuilder keyLogBuilder = new StringBuilder();

        private Button connectButton;
        private Text connectButtonLabel;
        private Button oneStickModeButton;
        private Button twinStickModeButton;
        private Button launcherButton;
        private Text modeHintLabel;

        private CubeManager twinCubeManager;
        private Cube leftTwinCube;
        private Cube rightTwinCube;
        private string leftTwinListenerKey;
        private string rightTwinListenerKey;
        private Vector3 leftTwinEulers;
        private Vector3 rightTwinEulers;
        private Cube.PoseType leftTwinPose;
        private Cube.PoseType rightTwinPose;
        private bool leftTwinButtonPressed;
        private bool rightTwinButtonPressed;
        private HorizontalTiltState leftTwinTiltState = HorizontalTiltState.Neutral;
        private HorizontalTiltState rightTwinTiltState = HorizontalTiltState.Neutral;
        private bool twinWActive;
        private bool twinAActive;
        private bool twinSActive;
        private bool twinDActive;
        private bool twinSpaceActive;
        private bool twinLeftShiftActive;
        private int twinTurnAxis;
        private bool lastTwinAActive;
        private bool lastTwinDActive;
        private bool lastTwinWActive;
        private bool lastTwinSActive;
        private bool lastTwinSpaceActive;
        private bool lastTwinLeftShiftActive;
        private bool lastTwinCtrlActive;
        private bool isConnecting;
        private float nextTwinMotionSensorRequestAt;

        private string footerMessage =
            "toioLeftHandLab ver1.4 within toio左手ガジェット化計画 / ToioJetHand. Twin flow: connect two upright cubes together. Shared pitch gives W/S, shared roll gives A/D, differential pitch drives Minecraft turn mouse, inner tilt shows LeftShift, outer tilt shows Space, and either button shows LeftCtrl.";

        public bool IsConnected
        {
            get
            {
                if (selectedMode == ControlMode.TwinStick)
                {
                    return AreTwinCubesConnected;
                }

                return inputSource != null && inputSource.IsConnected;
            }
        }

        public bool WPressed => (selectedMode == ControlMode.OneStick && inputSource != null) ? inputSource.WPressed : twinWActive;
        public bool APressed => (selectedMode == ControlMode.OneStick && inputSource != null) ? inputSource.APressed : twinAActive;
        public bool SPressed => (selectedMode == ControlMode.OneStick && inputSource != null) ? inputSource.SPressed : twinSActive;
        public bool DPressed => (selectedMode == ControlMode.OneStick && inputSource != null) ? inputSource.DPressed : twinDActive;
        public bool SpacePressed => selectedMode == ControlMode.TwinStick && twinSpaceActive;
        public bool LeftShiftPressed => selectedMode == ControlMode.TwinStick && twinLeftShiftActive;
        public bool LeftControlPressed => selectedMode == ControlMode.TwinStick && (leftTwinButtonPressed || rightTwinButtonPressed);
        public int TwinTurnAxis => selectedMode == ControlMode.TwinStick ? twinTurnAxis : 0;

        private bool AreTwinCubesConnected =>
            leftTwinCube != null && leftTwinCube.isConnected &&
            rightTwinCube != null && rightTwinCube.isConnected;

        private bool HasAnyTwinCubeConnection =>
            (leftTwinCube != null && leftTwinCube.isConnected) ||
            (rightTwinCube != null && rightTwinCube.isConnected);

        private void Awake()
        {
            leftTwinListenerKey = $"{nameof(ToioLeftHandLabController)}_TwinLeft_{GetInstanceID()}";
            rightTwinListenerKey = $"{nameof(ToioLeftHandLabController)}_TwinRight_{GetInstanceID()}";

            if (inputSource == null)
            {
                inputSource = GetComponent<toio.Samples.Sample_Sensor.ToioWasdInput>();
            }

            if (inputSource != null)
            {
                inputSource.VirtualKeyInjected += OnVirtualKeyInjected;
            }

            textBattery = FindText("TextBattery");
            textCollision = FindText("TextCollision");
            textFlat = FindText("TextFlat");
            textPositionID = FindText("TextPositionID");
            textStandardID = FindText("TextStandardID");
            textButton = FindText("TextButton");
            textAngle = FindText("TextAngle");
            textDoubleTap = FindText("TextDoubleTap");
            textPose = FindText("TextPose");
            textShake = FindText("TextShake");
            textSpeed = FindText("TextSpeed");
            textMag = FindText("TextMag");
            textAttitude = FindText("TextAttitude");
            EnsureStatusDashboardUi();

            var connectObject = GameObject.Find("ButtonConnect");
            if (connectObject != null)
            {
                connectButton = connectObject.GetComponent<Button>();
                connectButtonLabel = connectObject.GetComponentInChildren<Text>();
            }

            EnsureKeyLogUi();
            EnsureModeSelectionUi();
            UpdateModeSelectionUi();
        }

        private void Start()
        {
            RefreshTexts();
        }

        private void Update()
        {
            if (selectedMode == ControlMode.TwinStick)
            {
                UpdateTwinStickState();
            }

            RefreshTexts();
        }

        private void OnDestroy()
        {
            if (inputSource != null)
            {
                inputSource.VirtualKeyInjected -= OnVirtualKeyInjected;
            }

            RemoveTwinListeners();
            twinCubeManager?.DisconnectAll();
        }

        public bool GetVirtualKey(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.W:
                    return WPressed;
                case KeyCode.A:
                    return APressed;
                case KeyCode.S:
                    return SPressed;
                case KeyCode.D:
                    return DPressed;
                case KeyCode.Space:
                    return SpacePressed;
                case KeyCode.LeftShift:
                    return LeftShiftPressed;
                case KeyCode.LeftControl:
                    return LeftControlPressed;
                default:
                    return false;
            }
        }

        public void OnSelectOneStickMode()
        {
            TrySelectMode(ControlMode.OneStick);
        }

        public void OnSelectTwinStickMode()
        {
            TrySelectMode(ControlMode.TwinStick);
        }

        public void OnBackToLauncher()
        {
            SceneManager.LoadScene("ToioLauncher");
        }

        public async void OnBtnConnect()
        {
            if (isConnecting)
            {
                footerMessage = "A connection is already in progress.";
                return;
            }

            isConnecting = true;
            UpdateModeSelectionUi();

            try
            {
                if (selectedMode == ControlMode.OneStick)
                {
                    await ConnectOneStickMode();
                }
                else
                {
                    await ConnectTwinStickMode();
                }
            }
            finally
            {
                isConnecting = false;
                UpdateModeSelectionUi();
                RefreshTexts();
            }
        }

        public void Forward()
        {
            if (selectedMode != ControlMode.OneStick)
            {
                footerMessage = "Debug arrow buttons are reserved for 1stick mode in ver1.3.";
                return;
            }

            inputSource?.InjectVirtualKey(KeyCode.W);
            footerMessage = "Debug inject: W";
        }

        public void Backward()
        {
            if (selectedMode != ControlMode.OneStick)
            {
                footerMessage = "Debug arrow buttons are reserved for 1stick mode in ver1.3.";
                return;
            }

            inputSource?.InjectVirtualKey(KeyCode.S);
            footerMessage = "Debug inject: S";
        }

        public void TurnRight()
        {
            if (selectedMode != ControlMode.OneStick)
            {
                footerMessage = "Debug arrow buttons are reserved for 1stick mode in ver1.3.";
                return;
            }

            inputSource?.InjectVirtualKey(KeyCode.D);
            footerMessage = "Debug inject: D";
        }

        public void TurnLeft()
        {
            if (selectedMode != ControlMode.OneStick)
            {
                footerMessage = "Debug arrow buttons are reserved for 1stick mode in ver1.3.";
                return;
            }

            inputSource?.InjectVirtualKey(KeyCode.A);
            footerMessage = "Debug inject: A";
        }

        public void Stop()
        {
            if (selectedMode == ControlMode.OneStick)
            {
                inputSource?.ClearVirtualKeys();
            }
            else
            {
                ClearTwinTransientState();
            }

            ClearInputLog();
            footerMessage = "Virtual key state cleared.";
        }

        public void OnSwitchMag()
        {
            footerMessage = "Magnetic sensor is not used in this experiment.";
        }

        public void OnSwitchAttitude()
        {
            footerMessage = selectedMode == ControlMode.OneStick
                ? "Attitude sensing is always used here for A/D detection."
                : "Twin stick mode uses shared pitch for W/S, shared roll for A/D, differential pitch for Minecraft turn mouse, inner tilt for LeftShift, and outer tilt for Space.";
        }

        private void TrySelectMode(ControlMode mode)
        {
            if (selectedMode == mode)
            {
                footerMessage = $"Mode already selected: {GetModeLabel(mode)}.";
                UpdateModeSelectionUi();
                return;
            }

            if (isConnecting || IsConnected || HasAnyTwinCubeConnection)
            {
                footerMessage = "Change the mode before connecting. Reconnect after switching modes.";
                return;
            }

            selectedMode = mode;
            footerMessage = mode == ControlMode.OneStick
                ? "1stick mode selected. Press Connect to use the current single-cube setup."
                : "twin stick mode selected. Keep two cubes upright, then press Connect. Shared pitch gives W/S, shared roll gives A/D, differential pitch drives Minecraft turn mouse, inner tilt gives LeftShift, outer tilt gives Space, and either button shows LeftCtrl.";
            UpdateModeSelectionUi();
            RefreshTexts();
        }

        private async UniTask ConnectOneStickMode()
        {
            if (inputSource == null)
            {
                footerMessage = "Single-stick input source is missing.";
                return;
            }

            footerMessage = "Connecting to the nearest toio core cube for 1stick mode...";
            RefreshTexts();
            await inputSource.Connect();
            footerMessage = "Connected. ver1.3 1stick mode is ready. Tilt forward/back for W/S, tilt left/right for A/D.";
        }

        private async UniTask ConnectTwinStickMode()
        {
            if (inputSource == null)
            {
                footerMessage = "Base input source is missing, so twin stick mode cannot start.";
                return;
            }

            if (inputSource.IsConnected)
            {
                footerMessage = "1stick mode is already connected. Restart the scene before switching to twin stick mode.";
                return;
            }

            if (AreTwinCubesConnected)
            {
                footerMessage = "Twin stick mode is already connected.";
                return;
            }

            if (!HasAnyTwinCubeConnection)
            {
                RemoveTwinListeners();
                ClearTwinTransientState();
            }
            else if (!AreTwinCubesConnected)
            {
                await DisconnectTwinCubeSelection();
            }

            if (!AreTwinCubesConnected)
            {
                leftTwinCube = null;
                rightTwinCube = null;
                footerMessage = "Connecting two cubes together. This now follows the CubeManager multi-connect flow used in the official samples.";
                RefreshTexts();
                var cubes = await ConnectTwinCubePair();
                if (cubes == null || cubes.Length < 2)
                {
                    footerMessage = "Twin connection failed. Two connected cubes were not confirmed after retrying. Please keep both cubes nearby and press Connect again.";
                    return;
                }

                leftTwinCube = cubes[0];
                rightTwinCube = cubes[1];
                await UniTask.WhenAll(
                    RegisterTwinCube(leftTwinCube, leftTwinListenerKey, OnLeftTwinAttitude, OnLeftTwinButton, OnLeftTwinPose),
                    RegisterTwinCube(rightTwinCube, rightTwinListenerKey, OnRightTwinAttitude, OnRightTwinButton, OnRightTwinPose)
                );
                RefreshTwinPoseState(forceSensorRequest: true);
            }

            footerMessage =
                $"Connected. Twin upright mode is ready. Cube1={GetCubeDebugName(leftTwinCube, "cube1")} Cube2={GetCubeDebugName(rightTwinCube, "cube2")}. Cube labels are kept stable by BLE address order. Shared pitch gives W/S, shared roll gives A/D, differential pitch drives Minecraft turn mouse, inner tilt gives LeftShift, outer tilt gives Space, and either button gives LeftCtrl.";
        }

        private async UniTask<Cube[]> ConnectTwinCubePair()
        {
            var cubeManager = GetOrCreateTwinCubeManager();
            var attemptCount = Mathf.Max(1, twinConnectMaxAttempts);

            for (var attempt = 1; attempt <= attemptCount; attempt++)
            {
                footerMessage = attempt == 1
                    ? "Scanning and connecting for twin stick mode..."
                    : $"Retrying twin connection ({attempt}/{attemptCount})...";
                RefreshTexts();

                try
                {
                    await cubeManager.MultiConnect(2);
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception);
                }

                var connected = GetOrderedTwinCubesFromManager(cubeManager);
                if (connected.Length >= 2)
                {
                    return connected;
                }

                await DisconnectTwinCubeSelection();
                if (attempt < attemptCount)
                {
                    await UniTask.Delay(twinRetryDelayMs);
                }
            }

            return null;
        }

        private CubeManager GetOrCreateTwinCubeManager()
        {
            var connectType = ResolveTwinConnectType();
            if (twinCubeManager == null)
            {
                twinCubeManager = new CubeManager(connectType);
            }

            return twinCubeManager;
        }

        private ConnectType ResolveTwinConnectType()
        {
            var connectType = inputSource != null ? inputSource.ConnectType : ConnectType.Real;
#if UNITY_EDITOR || UNITY_STANDALONE
            if (connectType == ConnectType.Auto)
            {
                return ConnectType.Real;
            }
#endif
            return connectType;
        }

        private Cube[] GetOrderedTwinCubesFromManager(CubeManager cubeManager)
        {
            if (cubeManager == null)
            {
                return new Cube[0];
            }

            return cubeManager.connectedCubes
                .Where(cube => cube != null && cube.isConnected)
                .GroupBy(cube => cube.addr)
                .Select(group => group.First())
                .OrderBy(cube => cube.addr)
                .Take(2)
                .ToArray();
        }

        private async UniTask DisconnectTwinCubeSelection()
        {
            RemoveTwinListeners();

            if (twinCubeManager != null)
            {
                foreach (var cube in GetOrderedTwinCubesFromManager(twinCubeManager))
                {
                    twinCubeManager.Disconnect(cube);
                }
            }

            leftTwinCube = null;
            rightTwinCube = null;
            ClearTwinTransientState();

            if (twinConnectCleanupDelayMs > 0)
            {
                await UniTask.Delay(twinConnectCleanupDelayMs);
            }
        }

        private async UniTask RegisterTwinCube(
            Cube cube,
            string listenerKey,
            System.Action<Cube> attitudeHandler,
            System.Action<Cube> buttonHandler,
            System.Action<Cube> poseHandler
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
                twinAttitudeIntervalMs,
                Cube.AttitudeNotificationType.OnChanged
            );
            cube.RequestAttitudeSensor(Cube.AttitudeFormat.Eulers);
            cube.RequestMotionSensor();
            await UniTask.Delay(100);
        }

        private void OnLeftTwinAttitude(Cube cube)
        {
            leftTwinEulers = cube.eulers;
        }

        private void OnRightTwinAttitude(Cube cube)
        {
            rightTwinEulers = cube.eulers;
        }

        private void OnLeftTwinPose(Cube cube)
        {
            leftTwinPose = cube.pose;
        }

        private void OnRightTwinPose(Cube cube)
        {
            rightTwinPose = cube.pose;
        }

        private void OnLeftTwinButton(Cube cube)
        {
            leftTwinButtonPressed = cube.isPressed;
        }

        private void OnRightTwinButton(Cube cube)
        {
            rightTwinButtonPressed = cube.isPressed;
        }

        private void UpdateTwinStickState()
        {
            if (!AreTwinCubesConnected)
            {
                twinWActive = false;
                twinAActive = false;
                twinSActive = false;
                twinDActive = false;
                twinSpaceActive = false;
                twinLeftShiftActive = false;
                twinTurnAxis = 0;
                leftTwinTiltState = HorizontalTiltState.Neutral;
                rightTwinTiltState = HorizontalTiltState.Neutral;
                return;
            }

            RefreshTwinPoseState();
            var leftHorizontal = EvaluateHorizontalAxis(leftTwinEulers);
            var rightHorizontal = EvaluateHorizontalAxis(rightTwinEulers);
            var leftVertical = EvaluateVerticalAxis(leftTwinEulers);
            var rightVertical = EvaluateVerticalAxis(rightTwinEulers);

            leftTwinTiltState = ToHorizontalTiltState(leftHorizontal);
            rightTwinTiltState = ToHorizontalTiltState(rightHorizontal);

            var combinedHorizontal = Mathf.Clamp(leftHorizontal + rightHorizontal, -1, 1);
            var combinedVertical = Mathf.Clamp(leftVertical + rightVertical, -1, 1);
            var differentialVertical = Mathf.Clamp(leftVertical - rightVertical, -1, 1);

            twinWActive = combinedVertical > 0;
            twinSActive = combinedVertical < 0;
            twinDActive = combinedHorizontal > 0;
            twinAActive = combinedHorizontal < 0;
            twinLeftShiftActive = leftHorizontal < 0 && rightHorizontal > 0;
            twinSpaceActive = leftHorizontal > 0 && rightHorizontal < 0;
            twinTurnAxis = -differentialVertical;

            if (LeftControlPressed && !lastTwinCtrlActive)
            {
                footerMessage = "Twin button OK: LeftCtrl";
            }

            UpdateTwinActionFooter();
        }

        private int EvaluateHorizontalAxis(Vector3 eulers)
        {
            var threshold = inputSource != null ? inputSource.LeftRightTiltThresholdDeg : twinTiltThresholdDeg;
            if (Mathf.Abs(eulers.x) < threshold)
            {
                return 0;
            }

            var turnRight = eulers.x > 0f;
            var invert = inputSource != null ? inputSource.InvertTurnDirection : twinInvertTurnDirection;
            if (invert)
            {
                turnRight = !turnRight;
            }

            return turnRight ? 1 : -1;
        }

        private int EvaluateVerticalAxis(Vector3 eulers)
        {
            var threshold = inputSource != null ? inputSource.ForwardBackwardTiltThresholdDeg : twinTiltThresholdDeg;
            if (Mathf.Abs(eulers.y) < threshold)
            {
                return 0;
            }

            var forward = eulers.y < 0f;
            var invert = inputSource != null ? inputSource.InvertForwardBackward : true;
            if (invert)
            {
                forward = !forward;
            }

            return forward ? 1 : -1;
        }

        private static HorizontalTiltState ToHorizontalTiltState(int axis)
        {
            if (axis > 0)
            {
                return HorizontalTiltState.Right;
            }

            if (axis < 0)
            {
                return HorizontalTiltState.Left;
            }

            return HorizontalTiltState.Neutral;
        }

        private void UpdateTwinActionFooter()
        {
            TrackTwinActionEdge(twinWActive, ref lastTwinWActive, "Twin stick action: W", "W");
            TrackTwinActionEdge(twinAActive, ref lastTwinAActive, "Twin stick action: A", "A");
            TrackTwinActionEdge(twinSActive, ref lastTwinSActive, "Twin stick action: S", "S");
            TrackTwinActionEdge(twinDActive, ref lastTwinDActive, "Twin stick action: D", "D");
            TrackTwinActionEdge(twinLeftShiftActive, ref lastTwinLeftShiftActive, "Twin stick action: LeftShift (inner tilt)", "[Shift]");
            TrackTwinActionEdge(twinSpaceActive, ref lastTwinSpaceActive, "Twin stick action: Space (outer tilt)", "[Space]");
            TrackTwinActionEdge(LeftControlPressed, ref lastTwinCtrlActive, "Twin stick action: LeftCtrl", "[Ctrl]");
        }

        private void TrackTwinActionEdge(bool isActive, ref bool previousState, string message, string logToken)
        {
            if (isActive && !previousState)
            {
                footerMessage = message;
                if (!string.IsNullOrEmpty(logToken))
                {
                    AppendInputChar(logToken);
                }
            }

            previousState = isActive;
        }

        private void ClearTwinTransientState()
        {
            leftTwinEulers = Vector3.zero;
            rightTwinEulers = Vector3.zero;
            leftTwinPose = 0;
            rightTwinPose = 0;
            nextTwinMotionSensorRequestAt = 0f;
            leftTwinButtonPressed = false;
            rightTwinButtonPressed = false;
            leftTwinTiltState = HorizontalTiltState.Neutral;
            rightTwinTiltState = HorizontalTiltState.Neutral;
            twinWActive = false;
            twinAActive = false;
            twinSActive = false;
            twinDActive = false;
            twinSpaceActive = false;
            twinLeftShiftActive = false;
            twinTurnAxis = 0;
            lastTwinWActive = false;
            lastTwinAActive = false;
            lastTwinSActive = false;
            lastTwinDActive = false;
            lastTwinSpaceActive = false;
            lastTwinLeftShiftActive = false;
            lastTwinCtrlActive = false;
        }

        private void RemoveTwinListeners()
        {
            RemoveTwinListeners(leftTwinCube);
            RemoveTwinListeners(rightTwinCube);
        }

        private void RemoveTwinListeners(Cube cube)
        {
            if (cube == null)
            {
                return;
            }

            cube.attitudeCallback.RemoveListener(leftTwinListenerKey);
            cube.attitudeCallback.RemoveListener(rightTwinListenerKey);
            cube.buttonCallback.RemoveListener(leftTwinListenerKey);
            cube.buttonCallback.RemoveListener(rightTwinListenerKey);
            cube.poseCallback.RemoveListener(leftTwinListenerKey);
            cube.poseCallback.RemoveListener(rightTwinListenerKey);
        }

        private void RefreshTexts()
        {
            if (selectedMode == ControlMode.TwinStick)
            {
                RefreshTwinStickTexts();
                return;
            }

            RefreshOneStickTexts();
        }

        private void RefreshOneStickTexts()
        {
            if (inputSource == null)
            {
                return;
            }

            var connected = inputSource.IsConnected;
            var horizontal = inputSource.Horizontal;
            var vertical = inputSource.Vertical;

            SetText(textBattery, connected ? "Connect: Connected (1stick)" : "Connect: Not connected (1stick)");
            SetText(textFlat, $"W: {(inputSource.WPressed ? "ON" : "off")}");
            SetText(textButton, $"S: {(inputSource.SPressed ? "ON" : "off")}");
            SetText(textCollision, $"A: {(inputSource.APressed ? "ON" : "off")}");
            SetText(textDoubleTap, $"D: {(inputSource.DPressed ? "ON" : "off")}");
            SetText(textPose, $"Vertical Axis: {vertical:+0;-0;0}");
            SetText(textShake, $"Horizontal Axis: {horizontal:+0;-0;0}");
            SetText(textPositionID, $"Intent: toio左手ガジェット化計画 / ToioJetHand - toioLeftHandLab {VersionLabel} ({GetModeLabel(selectedMode)}).");
            SetText(textStandardID, "1stick: W/S uses pitch, A/D uses roll.");
            SetText(textAngle, connected ? "Cube: ready" : "Select a mode, then press Connect.");
            SetText(textSpeed, $"Speed raw: L={inputSource.LastLeftSpeed} R={inputSource.LastRightSpeed}");
            var e = inputSource.LastEulers;
            SetText(textMag, $"Euler raw: x={e.x:F1} y={e.y:F1} z={e.z:F1}  (A/D uses x, W/S uses y)");

            var fallback = showKeyboardFallbackHint ? " Keyboard fallback is ON." : string.Empty;
            SetText(textAttitude, footerMessage + fallback);
            if (keyLogLabel != null)
            {
                keyLogLabel.text = $"toio key input box {VersionLabel}";
            }
        }

        private void RefreshTwinStickTexts()
        {
            SetText(textBattery, AreTwinCubesConnected ? "Connect: Connected (twin stick)" : "Connect: Waiting for twin cube pair");
            SetText(textFlat, $"W: {(twinWActive ? "ON" : "off")}");
            SetText(textButton, $"A: {(twinAActive ? "ON" : "off")}");
            SetText(textCollision, $"Cube1 {GetCubeDebugName(leftTwinCube, "not connected")}: H={FormatTiltState(leftTwinTiltState)} V={FormatAxisState(EvaluateVerticalAxis(leftTwinEulers))} Btn={(leftTwinButtonPressed ? "ON" : "off")}");
            SetText(textDoubleTap, $"Cube2 {GetCubeDebugName(rightTwinCube, "not connected")}: H={FormatTiltState(rightTwinTiltState)} V={FormatAxisState(EvaluateVerticalAxis(rightTwinEulers))} Btn={(rightTwinButtonPressed ? "ON" : "off")}");
            SetText(textPose, $"S: {(twinSActive ? "ON" : "off")}");
            SetText(textShake, $"D: {(twinDActive ? "ON" : "off")}");
            SetText(textPositionID, $"Intent: toio左手ガジェット化計画 / ToioJetHand - toioLeftHandLab {VersionLabel} ({GetModeLabel(selectedMode)}).");
            SetText(textStandardID, "TwinStick upright mode: W/S uses shared pitch, A/D uses shared roll, differential pitch -> Minecraft turn mouse, inner tilt -> LeftShift, outer tilt -> Space, and either button -> LeftCtrl.");
            SetText(textAngle, GetTwinSetupStatusText());
            SetText(textSpeed, $"Turn(mouse): {FormatTurnAxis(TwinTurnAxis)}  Shift(inner): {(LeftShiftPressed ? "ON" : "off")}  Space(outer): {(SpacePressed ? "ON" : "off")}  Ctrl: {(LeftControlPressed ? "ON" : "off")}");
            SetText(
                textMag,
                $"Euler raw: L x={leftTwinEulers.x:F1} y={leftTwinEulers.y:F1} | R x={rightTwinEulers.x:F1} y={rightTwinEulers.y:F1}  Pair order: BLE address"
            );
            SetText(textAttitude, footerMessage);
            if (keyLogLabel != null)
            {
                keyLogLabel.text = $"toio key input box {VersionLabel}";
            }
        }

        private void EnsureModeSelectionUi()
        {
            if (oneStickModeButton != null && twinStickModeButton != null && launcherButton != null)
            {
                return;
            }

            var canvasObject = GameObject.Find("Canvas");
            if (canvasObject == null)
            {
                return;
            }

            var canvasTransform = canvasObject.transform as RectTransform;
            if (canvasTransform == null)
            {
                return;
            }

            var existingPanel = GameObject.Find("ToioModePanel");
            if (existingPanel != null)
            {
                oneStickModeButton = GameObject.Find("ModeButtonOneStick")?.GetComponent<Button>();
                twinStickModeButton = GameObject.Find("ModeButtonTwinStick")?.GetComponent<Button>();
                launcherButton = GameObject.Find("ModeButtonLauncher")?.GetComponent<Button>();
                modeHintLabel = GameObject.Find("ToioModeHint")?.GetComponent<Text>();
                return;
            }

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var panel = CreateUiObject("ToioModePanel", canvasTransform);
            ConfigureRect(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(1180f, 84f));
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.08f, 0.08f, 0.82f);

            var titleRect = CreateUiObject("ToioModeHint", panel);
            ConfigureRect(titleRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(-24f, 22f));
            modeHintLabel = titleRect.gameObject.AddComponent<Text>();
            modeHintLabel.font = font;
            modeHintLabel.fontSize = 22;
            modeHintLabel.alignment = TextAnchor.MiddleLeft;
            modeHintLabel.color = Color.white;

            oneStickModeButton = CreateModeButton(
                "ModeButtonOneStick",
                panel,
                font,
                "1stick mode",
                new Vector2(-340f, 10f),
                OnSelectOneStickMode
            );
            twinStickModeButton = CreateModeButton(
                "ModeButtonTwinStick",
                panel,
                font,
                "twin stick mode",
                new Vector2(0f, 10f),
                OnSelectTwinStickMode
            );
            launcherButton = CreateModeButton(
                "ModeButtonLauncher",
                panel,
                font,
                "Back To Launcher",
                new Vector2(340f, 10f),
                OnBackToLauncher
            );
        }

        private Button CreateModeButton(
            string name,
            RectTransform parent,
            Font font,
            string label,
            Vector2 anchoredPosition,
            UnityEngine.Events.UnityAction onClick
        )
        {
            var rect = CreateUiObject(name, parent);
            ConfigureRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, new Vector2(300f, 42f));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.18f, 0.18f, 0.96f);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var labelRect = CreateUiObject($"{name}_Label", rect);
            ConfigureRect(labelRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var text = labelRect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = label;

            return button;
        }

        private void UpdateModeSelectionUi()
        {
            SetModeButtonVisual(oneStickModeButton, selectedMode == ControlMode.OneStick);
            SetModeButtonVisual(twinStickModeButton, selectedMode == ControlMode.TwinStick);

            if (modeHintLabel != null)
            {
                modeHintLabel.text = $"Mode Select: {GetModeLabel(selectedMode)}";
            }

            if (connectButton != null)
            {
                connectButton.interactable = !isConnecting;
            }

            if (connectButtonLabel != null)
            {
                connectButtonLabel.text = isConnecting
                    ? "Connecting..."
                    : selectedMode == ControlMode.OneStick ? "Connect 1stick" : "Connect twin stick";
            }
        }

        private static void SetModeButtonVisual(Button button, bool isSelected)
        {
            if (button == null)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = isSelected
                    ? new Color(0.16f, 0.42f, 0.76f, 0.96f)
                    : new Color(0.18f, 0.18f, 0.18f, 0.96f);
            }
        }

        private static string GetModeLabel(ControlMode mode)
        {
            return mode == ControlMode.OneStick ? "1stick mode" : "twin stick mode";
        }

        private static string FormatTiltState(HorizontalTiltState state)
        {
            switch (state)
            {
                case HorizontalTiltState.Left:
                    return "Left";
                case HorizontalTiltState.Right:
                    return "Right";
                default:
                    return "Neutral";
            }
        }

        private static string FormatAxisState(int axis)
        {
            if (axis > 0)
            {
                return "Forward";
            }

            if (axis < 0)
            {
                return "Backward";
            }

            return "Neutral";
        }

        private static string FormatTurnAxis(int axis)
        {
            if (axis > 0)
            {
                return "Right";
            }

            if (axis < 0)
            {
                return "Left";
            }

            return "Neutral";
        }

        private void RefreshTwinPoseState(bool forceSensorRequest = false)
        {
            if (leftTwinCube != null && leftTwinCube.isConnected)
            {
                leftTwinEulers = leftTwinCube.eulers;
                leftTwinPose = leftTwinCube.pose;
                leftTwinButtonPressed = leftTwinCube.isPressed;
            }

            if (rightTwinCube != null && rightTwinCube.isConnected)
            {
                rightTwinEulers = rightTwinCube.eulers;
                rightTwinPose = rightTwinCube.pose;
                rightTwinButtonPressed = rightTwinCube.isPressed;
            }

            if (!forceSensorRequest && Time.time < nextTwinMotionSensorRequestAt)
            {
                return;
            }

            nextTwinMotionSensorRequestAt = Time.time + twinMotionSensorRefreshSeconds;
            leftTwinCube?.RequestAttitudeSensor(Cube.AttitudeFormat.Eulers);
            rightTwinCube?.RequestAttitudeSensor(Cube.AttitudeFormat.Eulers);
            leftTwinCube?.RequestMotionSensor();
            rightTwinCube?.RequestMotionSensor();
        }

        private string GetTwinSetupStatusText()
        {
            if (!AreTwinCubesConnected)
            {
                return "Setup: keep two cubes upright and press Connect. Twin connect retries now use CubeManager multi-connect.";
            }

            return "Setup: twin upright mode ready. Cube1/Cube2 stay in BLE address order. Differential pitch drives Minecraft turn mouse. Inner tilt shows LeftShift, outer tilt shows Space, and either button shows LeftCtrl.";
        }

        private static string GetCubeDebugName(Cube cube, string fallback)
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
            return $"{label}[{suffix}]";
        }

        private void EnsureStatusDashboardUi()
        {
            var canvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            var dashboard = new ToioLeftHandLabDashboardLayout();
            var refs = dashboard.Ensure(
                canvas,
                new[]
                {
                    textBattery,
                    textFlat,
                    textCollision,
                    textButton,
                    textPositionID,
                    textStandardID,
                    textAngle,
                    textDoubleTap,
                    textPose,
                    textShake,
                    textSpeed,
                    textMag,
                    textAttitude
                }
            );

            textBattery = refs.TextBattery;
            textFlat = refs.TextFlat;
            textCollision = refs.TextCollision;
            textButton = refs.TextButton;
            textPositionID = refs.TextPositionID;
            textStandardID = refs.TextStandardID;
            textAngle = refs.TextAngle;
            textDoubleTap = refs.TextDoubleTap;
            textPose = refs.TextPose;
            textShake = refs.TextShake;
            textSpeed = refs.TextSpeed;
            textMag = refs.TextMag;
            textAttitude = refs.TextAttitude;
        }

        private static Text FindText(string name)
        {
            var go = GameObject.Find(name);
            return go != null ? go.GetComponent<Text>() : null;
        }

        private void OnVirtualKeyInjected(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.W:
                case KeyCode.A:
                case KeyCode.S:
                case KeyCode.D:
                    AppendInputChar(keyCode.ToString());
                    footerMessage = $"Typed '{keyCode}' into the on-screen input box.";
                    break;
            }
        }

        private void AppendInputChar(string text)
        {
            EnsureKeyLogUi();
            if (keyLogInputField == null)
            {
                return;
            }

            keyLogBuilder.Append(text);
            if (keyLogBuilder.Length > maxLogLength)
            {
                keyLogBuilder.Remove(0, keyLogBuilder.Length - maxLogLength);
            }

            keyLogInputField.text = keyLogBuilder.ToString();
            keyLogInputField.caretPosition = keyLogInputField.text.Length;
        }

        private void ClearInputLog()
        {
            keyLogBuilder.Clear();
            if (keyLogInputField != null)
            {
                keyLogInputField.text = string.Empty;
            }
        }

        private void EnsureKeyLogUi()
        {
            if (keyLogInputField != null)
            {
                return;
            }

            var canvasObject = GameObject.Find("Canvas");
            if (canvasObject == null)
            {
                return;
            }

            var canvasTransform = canvasObject.transform as RectTransform;
            if (canvasTransform == null)
            {
                return;
            }

            var existingField = GameObject.Find("ToioKeyInputField");
            if (existingField != null)
            {
                keyLogInputField = existingField.GetComponent<InputField>();
                var existingLabel = GameObject.Find("ToioKeyInputLabel");
                if (existingLabel != null)
                {
                    keyLogLabel = existingLabel.GetComponent<Text>();
                }
                return;
            }

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var panel = CreateUiObject("ToioKeyInputPanel", canvasTransform);
            ConfigureRect(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(140f, 32f), new Vector2(1040f, 110f));
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

            var labelRect = CreateUiObject("ToioKeyInputLabel", panel);
            ConfigureRect(labelRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(-24f, 24f));
            keyLogLabel = labelRect.gameObject.AddComponent<Text>();
            keyLogLabel.font = font;
            keyLogLabel.fontSize = 24;
            keyLogLabel.alignment = TextAnchor.MiddleLeft;
            keyLogLabel.color = Color.white;
            keyLogLabel.text = $"toio key input box {VersionLabel}";

            var inputRoot = CreateUiObject("ToioKeyInputField", panel);
            ConfigureRect(inputRoot, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(-24f, 62f));
            var inputImage = inputRoot.gameObject.AddComponent<Image>();
            inputImage.color = new Color(1f, 1f, 1f, 0.97f);

            keyLogInputField = inputRoot.gameObject.AddComponent<InputField>();
            keyLogInputField.targetGraphic = inputImage;
            keyLogInputField.lineType = InputField.LineType.MultiLineNewline;
            keyLogInputField.readOnly = true;

            var textViewport = CreateUiObject("Text Area", inputRoot);
            ConfigureRect(textViewport, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20f, -20f));

            var textObject = CreateUiObject("Text", textViewport);
            ConfigureRect(textObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var textComponent = textObject.gameObject.AddComponent<Text>();
            textComponent.font = font;
            textComponent.fontSize = 30;
            textComponent.alignment = TextAnchor.UpperLeft;
            textComponent.color = Color.black;
            textComponent.supportRichText = false;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Truncate;

            var placeholderObject = CreateUiObject("Placeholder", textViewport);
            ConfigureRect(placeholderObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var placeholderText = placeholderObject.gameObject.AddComponent<Text>();
            placeholderText.font = font;
            placeholderText.fontSize = 26;
            placeholderText.alignment = TextAnchor.UpperLeft;
            placeholderText.color = new Color(0.4f, 0.4f, 0.4f, 1f);
            placeholderText.text = "Detected keys will be typed here...";

            keyLogInputField.textComponent = textComponent;
            keyLogInputField.placeholder = placeholderText;
        }

        private static RectTransform CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void ConfigureRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta
        )
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }
    }
}
