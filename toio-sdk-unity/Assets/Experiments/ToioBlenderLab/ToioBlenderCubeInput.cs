using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using toio;

namespace toio.Experiments.ToioBlenderLab
{
    [DisallowMultipleComponent]
    public class ToioBlenderCubeInput : MonoBehaviour
    {
        private enum LatchedAction
        {
            None = 0,
            Orbit = 1,
            Zoom = 2
        }

        [Header("Connection")]
        [SerializeField] private ConnectType connectType = ConnectType.Real;
        [SerializeField] private bool connectOnStart = false;

        [Header("Tilt Thresholds")]
        [SerializeField] private float orbitStartThresholdDeg = 7f;
        [SerializeField] private float orbitReleaseThresholdDeg = 4f;
        [SerializeField] private float zoomStartThresholdDeg = 7f;
        [SerializeField] private float zoomReleaseThresholdDeg = 4f;
        [SerializeField] private float maxInputAngleDeg = 24f;
        [SerializeField] private bool invertOrbitDirection = false;
        [SerializeField] private bool invertZoomDirection = true;

        [Header("Mode Toggle")]
        [SerializeField] private bool requireUpPoseForModeToggle = false;
        [SerializeField] private float modeToggleNeutralThresholdDeg = 9f;
        [SerializeField] private float modeToggleCooldownSeconds = 0.2f;

        [Header("Sensors")]
        [SerializeField] private int attitudeIntervalMs = 50;
        [SerializeField] private float motionSensorRefreshSeconds = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool logStateChanges = false;

        private Cube cube;
        private CubeConnecter connecter;
        private string listenerKey;
        private bool isConnecting;
        private bool hasPose;
        private bool buttonPressed;
        private int pendingModeToggleCount;
        private float nextMotionSensorRequestAt;
        private float lastModeToggleAt = -999f;
        private Vector3 lastEulers;
        private Cube.PoseType lastPose = Cube.PoseType.Up;
        private string connectionMessage = "Not connected. Press Connect Cube.";
        private LatchedAction latchedAction;
        private float orbitAxis;
        private float zoomAxis;

        public bool IsConnected => cube != null && cube.isConnected;
        public bool IsConnecting => isConnecting;
        public string ConnectionMessage => connectionMessage;
        public Vector3 LastEulers => lastEulers;
        public Cube.PoseType LastPose => lastPose;
        public bool HasPose => hasPose;
        public bool ButtonPressed => buttonPressed;
        public float OrbitAxis => orbitAxis;
        public float ZoomAxis => zoomAxis;
        public bool IsReadyForModeToggle => EvaluateModeToggleReady();

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

                return pendingModeToggleCount > 0 ? "Tab Queued" : "Idle";
            }
        }

        private void Awake()
        {
            listenerKey = $"{nameof(ToioBlenderCubeInput)}_{GetInstanceID()}";
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

            if (!IsConnected)
            {
                return;
            }

            if (motionSensorRefreshSeconds > 0f && Time.unscaledTime >= nextMotionSensorRequestAt)
            {
                cube.RequestMotionSensor();
                nextMotionSensorRequestAt = Time.unscaledTime + motionSensorRefreshSeconds;
            }
        }

        private void OnDestroy()
        {
            RemoveListeners();
            if (cube != null && connecter != null)
            {
                connecter.Disconnect(cube);
            }
        }

        public async UniTask Connect()
        {
            if (IsConnected || isConnecting)
            {
                return;
            }

            isConnecting = true;
            connectionMessage = "Scanning for a cube...";

            try
            {
                connecter = new CubeConnecter(connectType);
                var peripheral = await new CubeScanner(connectType).NearestScan();
                if (peripheral == null)
                {
                    connectionMessage = "No cube found.";
                    return;
                }

                cube = await connecter.Connect(peripheral);
                RemoveListeners();
                cube.attitudeCallback.AddListener(listenerKey, OnAttitude);
                cube.buttonCallback.AddListener(listenerKey, OnButton);
                cube.poseCallback.AddListener(listenerKey, OnPose);

                await cube.ConfigAttitudeSensor(
                    Cube.AttitudeFormat.Eulers,
                    attitudeIntervalMs,
                    Cube.AttitudeNotificationType.OnChanged
                );
                cube.RequestAttitudeSensor(Cube.AttitudeFormat.Eulers);
                cube.RequestMotionSensor();
                nextMotionSensorRequestAt = Time.unscaledTime + motionSensorRefreshSeconds;
                connectionMessage = "Connected. Switch focus to Blender to send controls.";
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

        private void OnAttitude(Cube currentCube)
        {
            lastEulers = currentCube.eulers;
            if (logStateChanges)
            {
                Debug.Log($"Blender input attitude => {lastEulers}");
            }
        }

        private void OnPose(Cube currentCube)
        {
            lastPose = currentCube.pose;
            hasPose = true;
        }

        private void OnButton(Cube currentCube)
        {
            var wasPressed = buttonPressed;
            buttonPressed = currentCube.isPressed;

            if (!buttonPressed || wasPressed)
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

        private float EvaluateOrbitAxis()
        {
            return EvaluateSignedAxis(
                lastEulers.x,
                latchedAction == LatchedAction.Orbit ? orbitReleaseThresholdDeg : orbitStartThresholdDeg,
                invertOrbitDirection
            );
        }

        private float EvaluateZoomAxis()
        {
            var signedForwardTilt = -lastEulers.y;
            return EvaluateSignedAxis(
                signedForwardTilt,
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

        private bool EvaluateModeToggleReady()
        {
            var uprightEnough = !requireUpPoseForModeToggle || !hasPose || lastPose == Cube.PoseType.Up;
            var neutralEnough = Mathf.Abs(lastEulers.x) <= modeToggleNeutralThresholdDeg && Mathf.Abs(lastEulers.y) <= modeToggleNeutralThresholdDeg;
            return uprightEnough && neutralEnough;
        }

        private void RemoveListeners()
        {
            if (cube == null)
            {
                return;
            }

            cube.attitudeCallback.RemoveListener(listenerKey);
            cube.buttonCallback.RemoveListener(listenerKey);
            cube.poseCallback.RemoveListener(listenerKey);
        }
    }
}
