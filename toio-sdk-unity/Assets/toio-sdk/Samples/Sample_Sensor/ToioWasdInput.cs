using Cysharp.Threading.Tasks;
using UnityEngine;
using System;

namespace toio.Samples.Sample_Sensor
{
    public class ToioWasdInput : MonoBehaviour
    {
        public event Action<KeyCode> VirtualKeyInjected;

        [Header("Connection")]
        [SerializeField] private ConnectType connectType = ConnectType.Real;
        [SerializeField] private bool connectOnStart = true;
        [SerializeField] private bool useKeyboardFallback = true;

        [Header("Forward / Backward")]
        [SerializeField] private bool invertForwardBackward = true;
        [SerializeField] private float forwardBackwardTiltThresholdDeg = 8f;

        [Header("Left / Right")]
        [SerializeField] private bool invertTurnDirection = false;
        [SerializeField] private float leftRightTiltThresholdDeg = 8f;

        [Header("Timing")]
        [SerializeField] private int attitudeIntervalMs = 50;
        [SerializeField] private float holdSeconds = 0.2f;

        [Header("Debug")]
        [SerializeField] private bool logStateChanges = false;

        private Cube cube;
        private string listenerKey;

        private int lastLeftSpeed;
        private int lastRightSpeed;

        private float wUntil;
        private float aUntil;
        private float sUntil;
        private float dUntil;
        private Vector3 lastEulers;

        private int lastLoggedHorizontal;
        private int lastLoggedVertical;

        public Cube Cube => cube;
        public ConnectType ConnectType => connectType;
        public bool InvertForwardBackward => invertForwardBackward;
        public float ForwardBackwardTiltThresholdDeg => forwardBackwardTiltThresholdDeg;
        public bool InvertTurnDirection => invertTurnDirection;
        public float LeftRightTiltThresholdDeg => leftRightTiltThresholdDeg;
        public bool IsConnected => cube != null && cube.isConnected;
        public int LastLeftSpeed => lastLeftSpeed;
        public int LastRightSpeed => lastRightSpeed;
        public Vector3 LastEulers => lastEulers;

        public bool WPressed => GetVirtualKey(KeyCode.W);
        public bool APressed => GetVirtualKey(KeyCode.A);
        public bool SPressed => GetVirtualKey(KeyCode.S);
        public bool DPressed => GetVirtualKey(KeyCode.D);

        public float Horizontal => GetHorizontal();
        public float Vertical => GetVertical();

        private void Awake()
        {
            listenerKey = $"{nameof(ToioWasdInput)}_{GetInstanceID()}";
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
            if (!logStateChanges)
            {
                return;
            }

            var horizontal = (int)Horizontal;
            var vertical = (int)Vertical;
            if (horizontal == lastLoggedHorizontal && vertical == lastLoggedVertical)
            {
                return;
            }

            lastLoggedHorizontal = horizontal;
            lastLoggedVertical = vertical;
            Debug.Log($"toio WASD => H:{horizontal} V:{vertical}  (W:{WPressed} A:{APressed} S:{SPressed} D:{DPressed})");
        }

        private void OnDestroy()
        {
            RemoveListeners();
        }

        public async UniTask Connect()
        {
            if (cube != null && cube.isConnected)
            {
                return;
            }

            var peripheral = await new CubeScanner(connectType).NearestScan();
            cube = await new CubeConnecter(connectType).Connect(peripheral);

            RemoveListeners();
            cube.motorSpeedCallback.AddListener(listenerKey, OnMotorSpeed);
            cube.attitudeCallback.AddListener(listenerKey, OnAttitude);

            await cube.ConfigMotorRead(true);
            await cube.ConfigAttitudeSensor(
                Cube.AttitudeFormat.Eulers,
                attitudeIntervalMs,
                Cube.AttitudeNotificationType.OnChanged
            );
            cube.RequestAttitudeSensor(Cube.AttitudeFormat.Eulers);
        }

        public bool GetVirtualKey(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.W:
                    return GetVertical() > 0f;
                case KeyCode.S:
                    return GetVertical() < 0f;
                case KeyCode.A:
                    return GetHorizontal() < 0f;
                case KeyCode.D:
                    return GetHorizontal() > 0f;
                default:
                    return false;
            }
        }

        public void InjectVirtualKey(KeyCode keyCode, float seconds = -1f)
        {
            var duration = seconds > 0f ? seconds : holdSeconds;
            switch (keyCode)
            {
                case KeyCode.W:
                    wUntil = Time.time + duration;
                    break;
                case KeyCode.A:
                    aUntil = Time.time + duration;
                    break;
                case KeyCode.S:
                    sUntil = Time.time + duration;
                    break;
                case KeyCode.D:
                    dUntil = Time.time + duration;
                    break;
            }

            VirtualKeyInjected?.Invoke(keyCode);
        }

        public void ClearVirtualKeys()
        {
            wUntil = 0f;
            aUntil = 0f;
            sUntil = 0f;
            dUntil = 0f;
        }

        private void OnMotorSpeed(Cube currentCube)
        {
            lastLeftSpeed = currentCube.leftSpeed;
            lastRightSpeed = currentCube.rightSpeed;
        }

        private void OnAttitude(Cube currentCube)
        {
            lastEulers = currentCube.eulers;
            var vertical = GetToioVertical();
            if (vertical > 0f) PressW();
            else if (vertical < 0f) PressS();

            var horizontal = GetToioHorizontal();
            if (horizontal > 0f) PressD();
            else if (horizontal < 0f) PressA();
        }

        private void PressW() => InjectVirtualKey(KeyCode.W);
        private void PressA() => InjectVirtualKey(KeyCode.A);
        private void PressS() => InjectVirtualKey(KeyCode.S);
        private void PressD() => InjectVirtualKey(KeyCode.D);

        private float GetVertical()
        {
            var toio = GetToioVertical();
            var positive = toio > 0f || IsActive(wUntil) || (useKeyboardFallback && Input.GetKey(KeyCode.W));
            var negative = toio < 0f || IsActive(sUntil) || (useKeyboardFallback && Input.GetKey(KeyCode.S));

            if (positive == negative)
            {
                return 0f;
            }

            return positive ? 1f : -1f;
        }

        private float GetHorizontal()
        {
            var toio = GetToioHorizontal();
            var negative = toio < 0f || IsActive(aUntil) || (useKeyboardFallback && Input.GetKey(KeyCode.A));
            var positive = toio > 0f || IsActive(dUntil) || (useKeyboardFallback && Input.GetKey(KeyCode.D));

            if (positive == negative)
            {
                return 0f;
            }

            return positive ? 1f : -1f;
        }

        private static bool IsActive(float until)
        {
            return Time.time <= until;
        }

        private float GetToioVertical()
        {
            if (!IsConnected || Mathf.Abs(lastEulers.y) < forwardBackwardTiltThresholdDeg)
            {
                return 0f;
            }

            var forward = lastEulers.y < 0f;
            if (invertForwardBackward)
            {
                forward = !forward;
            }

            return forward ? 1f : -1f;
        }

        private float GetToioHorizontal()
        {
            if (!IsConnected || Mathf.Abs(lastEulers.x) < leftRightTiltThresholdDeg)
            {
                return 0f;
            }

            var turnRight = lastEulers.x > 0f;
            if (invertTurnDirection)
            {
                turnRight = !turnRight;
            }

            return turnRight ? 1f : -1f;
        }

        private void RemoveListeners()
        {
            if (cube == null)
            {
                return;
            }

            cube.motorSpeedCallback.RemoveListener(listenerKey);
            cube.attitudeCallback.RemoveListener(listenerKey);
        }
    }
}
