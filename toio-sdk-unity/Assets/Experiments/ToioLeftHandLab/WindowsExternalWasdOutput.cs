using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace toio.Experiments.ToioLeftHandLab
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ToioLeftHandLabController))]
    public class WindowsExternalWasdOutput : MonoBehaviour
    {
        private enum OutputMode
        {
            Disabled = 0,
            TapRepeat = 1,
            HoldWhileTilted = 2
        }

        [Serializable]
        private struct KeyRepeatState
        {
            public bool isActive;
            public float nextRepeatAt;
        }

        [Header("Source")]
        [SerializeField] private ToioLeftHandLabController controller;
        [SerializeField] private toio.Samples.Sample_Sensor.ToioWasdInput inputSource;

        [Header("Output")]
        [SerializeField] private OutputMode outputMode = OutputMode.TapRepeat;
        [SerializeField] private bool onlyWhenUnityIsNotFocused = true;

        [Header("Target Window")]
        [SerializeField] private bool requireForegroundWindowTitleMatch = false;
        [SerializeField] private string requiredForegroundWindowTitleFragment = "Minecraft";
        [SerializeField] private bool ignoreCaseInWindowTitleMatch = true;

        [Header("Enabled Keys")]
        [SerializeField] private bool sendW = true;
        [SerializeField] private bool sendA = true;
        [SerializeField] private bool sendS = true;
        [SerializeField] private bool sendD = true;
        [SerializeField] private bool sendSpace = true;
        [SerializeField] private bool sendLeftShift = true;
        [SerializeField] private bool sendLeftControl = true;

        [Header("Mouse Turn")]
        [SerializeField] private bool sendTwinTurnMouse = true;
        [SerializeField] private float twinTurnPixelsPerSecond = 700f;

        [Header("Repeat")]
        [SerializeField] private float firstRepeatDelaySeconds = 0.35f;
        [SerializeField] private float repeatIntervalSeconds = 0.08f;

        [Header("Debug")]
        [SerializeField] private bool logKeyOutput = false;

        private KeyRepeatState wState;
        private KeyRepeatState aState;
        private KeyRepeatState sState;
        private KeyRepeatState dState;
        private KeyRepeatState spaceState;
        private KeyRepeatState leftShiftState;
        private KeyRepeatState leftControlState;
        private float mouseTurnResidualX;

        private void Awake()
        {
            Application.runInBackground = true;

            if (controller == null)
            {
                controller = GetComponent<ToioLeftHandLabController>();
            }

            if (inputSource == null)
            {
                inputSource = GetComponent<toio.Samples.Sample_Sensor.ToioWasdInput>();
            }
        }

        private void Update()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if ((controller == null && inputSource == null) || outputMode == OutputMode.Disabled)
            {
                ReleaseAllHeldKeys();
                ResetRepeatStates();
                mouseTurnResidualX = 0f;
                return;
            }

            if (onlyWhenUnityIsNotFocused && Application.isFocused)
            {
                ReleaseAllHeldKeys();
                ResetRepeatStates();
                mouseTurnResidualX = 0f;
                return;
            }

            if (!IsTargetWindowActive())
            {
                ReleaseAllHeldKeys();
                ResetRepeatStates();
                mouseTurnResidualX = 0f;
                return;
            }

            var now = Time.unscaledTime;
            UpdateKey(KeyCode.W, sendW && GetVirtualKey(KeyCode.W), ref wState, now);
            UpdateKey(KeyCode.A, sendA && GetVirtualKey(KeyCode.A), ref aState, now);
            UpdateKey(KeyCode.S, sendS && GetVirtualKey(KeyCode.S), ref sState, now);
            UpdateKey(KeyCode.D, sendD && GetVirtualKey(KeyCode.D), ref dState, now);
            UpdateKey(KeyCode.Space, sendSpace && GetVirtualKey(KeyCode.Space), ref spaceState, now);
            UpdateKey(KeyCode.LeftShift, sendLeftShift && GetVirtualKey(KeyCode.LeftShift), ref leftShiftState, now);
            UpdateKey(KeyCode.LeftControl, sendLeftControl && GetVirtualKey(KeyCode.LeftControl), ref leftControlState, now);
            UpdateTwinTurnMouse(Time.unscaledDeltaTime);
#endif
        }

        private void OnDisable()
        {
            ReleaseAllHeldKeys();
            ResetRepeatStates();
        }

        private void OnDestroy()
        {
            ReleaseAllHeldKeys();
            ResetRepeatStates();
        }

        private void UpdateKey(KeyCode keyCode, bool isPressed, ref KeyRepeatState state, float now)
        {
            switch (outputMode)
            {
                case OutputMode.TapRepeat:
                    UpdateTapRepeatKey(keyCode, isPressed, ref state, now);
                    break;
                case OutputMode.HoldWhileTilted:
                    UpdateHeldKey(keyCode, isPressed, ref state);
                    break;
            }
        }

        private void UpdateTapRepeatKey(KeyCode keyCode, bool isPressed, ref KeyRepeatState state, float now)
        {
            if (!isPressed)
            {
                state.isActive = false;
                state.nextRepeatAt = 0f;
                return;
            }

            if (!state.isActive)
            {
                SendTap(keyCode);
                state.isActive = true;
                state.nextRepeatAt = now + Mathf.Max(0.01f, firstRepeatDelaySeconds);
                return;
            }

            if (now < state.nextRepeatAt)
            {
                return;
            }

            SendTap(keyCode);
            state.nextRepeatAt = now + Mathf.Max(0.01f, repeatIntervalSeconds);
        }

        private void UpdateHeldKey(KeyCode keyCode, bool isPressed, ref KeyRepeatState state)
        {
            if (isPressed == state.isActive)
            {
                return;
            }

            state.isActive = isPressed;
            if (isPressed)
            {
                SendDown(keyCode);
            }
            else
            {
                SendUp(keyCode);
            }
        }

        private void ResetRepeatStates()
        {
            wState = default;
            aState = default;
            sState = default;
            dState = default;
            spaceState = default;
            leftShiftState = default;
            leftControlState = default;
        }

        private void ReleaseAllHeldKeys()
        {
            if (outputMode != OutputMode.HoldWhileTilted)
            {
                return;
            }

            ReleaseHeldKey(KeyCode.W, ref wState);
            ReleaseHeldKey(KeyCode.A, ref aState);
            ReleaseHeldKey(KeyCode.S, ref sState);
            ReleaseHeldKey(KeyCode.D, ref dState);
            ReleaseHeldKey(KeyCode.Space, ref spaceState);
            ReleaseHeldKey(KeyCode.LeftShift, ref leftShiftState);
            ReleaseHeldKey(KeyCode.LeftControl, ref leftControlState);
        }

        private void ReleaseHeldKey(KeyCode keyCode, ref KeyRepeatState state)
        {
            if (!state.isActive)
            {
                return;
            }

            SendUp(keyCode);
            state.isActive = false;
            state.nextRepeatAt = 0f;
        }

        private void SendTap(KeyCode keyCode)
        {
            SendDown(keyCode);
            SendUp(keyCode);
        }

        private void SendDown(KeyCode keyCode)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            SendKeyEvent(keyCode, false);
#endif
        }

        private void SendUp(KeyCode keyCode)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            SendKeyEvent(keyCode, true);
#endif
        }

        private bool GetVirtualKey(KeyCode keyCode)
        {
            if (controller != null)
            {
                return controller.GetVirtualKey(keyCode);
            }

            return inputSource != null && inputSource.GetVirtualKey(keyCode);
        }

        private void UpdateTwinTurnMouse(float deltaTime)
        {
            if (!sendTwinTurnMouse || controller == null || deltaTime <= 0f)
            {
                mouseTurnResidualX = 0f;
                return;
            }

            var turnAxis = controller.TwinTurnAxis;
            if (turnAxis == 0)
            {
                mouseTurnResidualX = 0f;
                return;
            }

            mouseTurnResidualX += turnAxis * twinTurnPixelsPerSecond * deltaTime;
            var moveX = Mathf.RoundToInt(mouseTurnResidualX);
            if (moveX == 0)
            {
                return;
            }

            mouseTurnResidualX -= moveX;
            SendMouseMove(moveX, 0);
        }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private void SendKeyEvent(KeyCode keyCode, bool keyUp)
        {
            var virtualKey = ToVirtualKey(keyCode);
            if (virtualKey == 0)
            {
                return;
            }

            var scanCode = (ushort)MapVirtualKey(virtualKey, MAPVK_VK_TO_VSC);
            var useScanCode = scanCode != 0;
            var flags = keyUp ? KEYEVENTF_KEYUP : 0;
            if (useScanCode)
            {
                flags |= KEYEVENTF_SCANCODE;
            }

            var input = new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = useScanCode ? (ushort)0 : virtualKey,
                        wScan = useScanCode ? scanCode : (ushort)0,
                        dwFlags = flags,
                        dwExtraInfo = IntPtr.Zero,
                        time = 0
                    }
                }
            };

            var sent = SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
            if (logKeyOutput)
            {
                if (sent == 0)
                {
                    Debug.LogWarning($"External key send failed for {keyCode}. Win32Error={Marshal.GetLastWin32Error()}");
                }
                else
                {
                    Debug.Log($"External key {(keyUp ? "up" : "down")}: {keyCode}");
                }
            }
        }

        private void SendMouseMove(int deltaX, int deltaY)
        {
            var input = new INPUT
            {
                type = INPUT_MOUSE,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dx = deltaX,
                        dy = deltaY,
                        mouseData = 0,
                        dwFlags = MOUSEEVENTF_MOVE,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            var sent = SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
            if (logKeyOutput)
            {
                if (sent == 0)
                {
                    Debug.LogWarning($"External mouse move failed. Win32Error={Marshal.GetLastWin32Error()}");
                }
                else
                {
                    Debug.Log($"External mouse move: dx={deltaX} dy={deltaY}");
                }
            }
        }

        private bool IsTargetWindowActive()
        {
            if (!requireForegroundWindowTitleMatch)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(requiredForegroundWindowTitleFragment))
            {
                return true;
            }

            var title = GetForegroundWindowTitle();
            if (string.IsNullOrEmpty(title))
            {
                return false;
            }

            var comparison = ignoreCaseInWindowTitleMatch
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            return title.IndexOf(requiredForegroundWindowTitleFragment, comparison) >= 0;
        }

        private static string GetForegroundWindowTitle()
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(256);
            var length = GetWindowText(hwnd, builder, builder.Capacity);
            if (length <= 0)
            {
                return string.Empty;
            }

            return builder.ToString();
        }

        private static ushort ToVirtualKey(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.W:
                    return 0x57;
                case KeyCode.A:
                    return 0x41;
                case KeyCode.S:
                    return 0x53;
                case KeyCode.D:
                    return 0x44;
                case KeyCode.Space:
                    return 0x20;
                case KeyCode.LeftShift:
                    return 0xA0;
                case KeyCode.LeftControl:
                    return 0xA2;
                default:
                    return 0;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        private const uint INPUT_KEYBOARD = 1;
        private const uint INPUT_MOUSE = 0;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_SCANCODE = 0x0008;
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MAPVK_VK_TO_VSC = 0;
#endif
    }
}
