using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace toio.Experiments.ToioLeftHandLab
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(toio.Samples.Sample_Sensor.ToioWasdInput))]
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
        [SerializeField] private toio.Samples.Sample_Sensor.ToioWasdInput inputSource;

        [Header("Output")]
        [SerializeField] private OutputMode outputMode = OutputMode.TapRepeat;
        [SerializeField] private bool onlyWhenUnityIsNotFocused = true;

        [Header("Enabled Keys")]
        [SerializeField] private bool sendW = true;
        [SerializeField] private bool sendA = true;
        [SerializeField] private bool sendS = true;
        [SerializeField] private bool sendD = true;

        [Header("Repeat")]
        [SerializeField] private float firstRepeatDelaySeconds = 0.35f;
        [SerializeField] private float repeatIntervalSeconds = 0.08f;

        [Header("Debug")]
        [SerializeField] private bool logKeyOutput = false;

        private KeyRepeatState wState;
        private KeyRepeatState aState;
        private KeyRepeatState sState;
        private KeyRepeatState dState;

        private void Awake()
        {
            Application.runInBackground = true;

            if (inputSource == null)
            {
                inputSource = GetComponent<toio.Samples.Sample_Sensor.ToioWasdInput>();
            }
        }

        private void Update()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (inputSource == null || outputMode == OutputMode.Disabled)
            {
                ReleaseAllHeldKeys();
                ResetRepeatStates();
                return;
            }

            if (onlyWhenUnityIsNotFocused && Application.isFocused)
            {
                ReleaseAllHeldKeys();
                ResetRepeatStates();
                return;
            }

            var now = Time.unscaledTime;
            UpdateKey(KeyCode.W, sendW && inputSource.WPressed, ref wState, now);
            UpdateKey(KeyCode.A, sendA && inputSource.APressed, ref aState, now);
            UpdateKey(KeyCode.S, sendS && inputSource.SPressed, ref sState, now);
            UpdateKey(KeyCode.D, sendD && inputSource.DPressed, ref dState, now);
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

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private void SendKeyEvent(KeyCode keyCode, bool keyUp)
        {
            var virtualKey = ToVirtualKey(keyCode);
            if (virtualKey == 0)
            {
                return;
            }

            var input = new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = virtualKey,
                        wScan = 0,
                        dwFlags = keyUp ? KEYEVENTF_KEYUP : 0,
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

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
#endif
    }
}
