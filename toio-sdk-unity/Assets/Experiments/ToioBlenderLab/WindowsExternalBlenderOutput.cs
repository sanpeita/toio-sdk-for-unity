using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace toio.Experiments.ToioBlenderLab
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ToioBlenderCubeInput))]
    public class WindowsExternalBlenderOutput : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private ToioBlenderCubeInput inputSource;

        [Header("Target Window")]
        [SerializeField] private bool onlyWhenUnityIsNotFocused = true;
        [SerializeField] private bool requireForegroundWindowTitleMatch = true;
        [SerializeField] private string requiredForegroundWindowTitleFragment = "Blender";
        [SerializeField] private bool ignoreCaseInWindowTitleMatch = true;

        [Header("Orbit")]
        [SerializeField] private bool sendOrbit = true;
        [SerializeField] private float orbitPixelsPerSecond = 300f;

        [Header("Zoom")]
        [SerializeField] private bool sendZoom = true;
        [SerializeField] private float zoomWheelStepsPerSecond = 10f;

        [Header("Mode Toggle")]
        [SerializeField] private bool sendTabForModeToggle = true;

        [Header("Debug")]
        [SerializeField] private bool logOutput = false;

        private bool middleButtonHeld;
        private float orbitResidualX;
        private float zoomResidualSteps;
        private string lastForegroundWindowTitle = string.Empty;

        public string RuntimeStatus { get; private set; } = "Waiting for cube connection.";
        public string RequiredForegroundWindowTitleFragment => requiredForegroundWindowTitleFragment;

        private void Awake()
        {
            Application.runInBackground = true;
            if (inputSource == null)
            {
                inputSource = GetComponent<ToioBlenderCubeInput>();
            }
        }

        private void Update()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (inputSource == null)
            {
                RuntimeStatus = "Input source missing.";
                ReleaseTransientInputs();
                return;
            }

            if (!inputSource.IsConnected)
            {
                RuntimeStatus = "Cube not connected yet.";
                ReleaseTransientInputs();
                inputSource.ClearPendingModeToggles();
                return;
            }

            if (onlyWhenUnityIsNotFocused && Application.isFocused)
            {
                RuntimeStatus = "Unity is focused. Switch focus to Blender.";
                ReleaseTransientInputs();
                return;
            }

            if (!IsTargetWindowActive())
            {
                var windowLabel = string.IsNullOrEmpty(lastForegroundWindowTitle) ? "No foreground window." : $"Foreground window: {lastForegroundWindowTitle}";
                RuntimeStatus = $"{windowLabel} Waiting for '{requiredForegroundWindowTitleFragment}'.";
                ReleaseTransientInputs();
                return;
            }

            UpdateModeToggle();
            UpdateOrbit(Time.unscaledDeltaTime);
            UpdateZoom(Time.unscaledDeltaTime);
            RuntimeStatus = BuildActiveStatus();
#else
            RuntimeStatus = "Windows external output is only available in Unity Editor or Standalone Windows.";
#endif
        }

        private void OnDisable()
        {
            ReleaseTransientInputs();
        }

        private void OnDestroy()
        {
            ReleaseTransientInputs();
        }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private void UpdateModeToggle()
        {
            if (!sendTabForModeToggle || inputSource == null)
            {
                return;
            }

            while (inputSource.ConsumeModeToggleRequested())
            {
                SendKeyTap(0x09);
                if (logOutput)
                {
                    Debug.Log("Blender output => Tab");
                }
            }
        }

        private void UpdateOrbit(float deltaTime)
        {
            if (!sendOrbit || deltaTime <= 0f || inputSource == null)
            {
                ReleaseMiddleMouse();
                orbitResidualX = 0f;
                return;
            }

            var orbitAxis = inputSource.OrbitAxis;
            if (Mathf.Abs(orbitAxis) <= 0.001f)
            {
                ReleaseMiddleMouse();
                orbitResidualX = 0f;
                return;
            }

            EnsureMiddleMouseHeld();
            orbitResidualX += orbitAxis * orbitPixelsPerSecond * deltaTime;
            var moveX = Mathf.RoundToInt(orbitResidualX);
            if (moveX == 0)
            {
                return;
            }

            orbitResidualX -= moveX;
            SendMouseEvent(MOUSEEVENTF_MOVE, moveX, 0, 0);
        }

        private void UpdateZoom(float deltaTime)
        {
            if (!sendZoom || deltaTime <= 0f || inputSource == null)
            {
                zoomResidualSteps = 0f;
                return;
            }

            var zoomAxis = inputSource.ZoomAxis;
            if (Mathf.Abs(zoomAxis) <= 0.001f)
            {
                zoomResidualSteps = 0f;
                return;
            }

            zoomResidualSteps += zoomAxis * zoomWheelStepsPerSecond * deltaTime;
            var wholeSteps = zoomResidualSteps > 0f
                ? Mathf.FloorToInt(zoomResidualSteps)
                : Mathf.CeilToInt(zoomResidualSteps);

            if (wholeSteps == 0)
            {
                return;
            }

            zoomResidualSteps -= wholeSteps;
            SendMouseEvent(MOUSEEVENTF_WHEEL, 0, 0, (uint)(wholeSteps * WHEEL_DELTA));
        }

        private string BuildActiveStatus()
        {
            var statusBuilder = new StringBuilder();
            statusBuilder.Append("Target ready");
            if (Mathf.Abs(inputSource.OrbitAxis) > 0.001f)
            {
                statusBuilder.Append(inputSource.OrbitAxis > 0f ? " | Orbit Right" : " | Orbit Left");
            }

            if (Mathf.Abs(inputSource.ZoomAxis) > 0.001f)
            {
                statusBuilder.Append(inputSource.ZoomAxis > 0f ? " | Zoom In" : " | Zoom Out");
            }

            if (Mathf.Abs(inputSource.OrbitAxis) <= 0.001f && Mathf.Abs(inputSource.ZoomAxis) <= 0.001f)
            {
                statusBuilder.Append(" | Idle");
            }

            return statusBuilder.ToString();
        }

        private void ReleaseTransientInputs()
        {
            ReleaseMiddleMouse();
            orbitResidualX = 0f;
            zoomResidualSteps = 0f;
        }

        private void EnsureMiddleMouseHeld()
        {
            if (middleButtonHeld)
            {
                return;
            }

            SendMouseEvent(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0);
            middleButtonHeld = true;
        }

        private void ReleaseMiddleMouse()
        {
            if (!middleButtonHeld)
            {
                return;
            }

            SendMouseEvent(MOUSEEVENTF_MIDDLEUP, 0, 0, 0);
            middleButtonHeld = false;
        }

        private void SendKeyTap(ushort virtualKey)
        {
            SendKeyEvent(virtualKey, false);
            SendKeyEvent(virtualKey, true);
        }

        private void SendKeyEvent(ushort virtualKey, bool keyUp)
        {
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
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            var sent = SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
            if (sent == 0 && logOutput)
            {
                Debug.LogWarning($"Blender key send failed. Win32Error={Marshal.GetLastWin32Error()}");
            }
        }

        private void SendMouseEvent(uint flags, int deltaX, int deltaY, uint mouseData)
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
                        mouseData = mouseData,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            var sent = SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
            if (sent == 0 && logOutput)
            {
                Debug.LogWarning($"Blender mouse send failed. Win32Error={Marshal.GetLastWin32Error()}");
            }
        }

        private bool IsTargetWindowActive()
        {
            if (!requireForegroundWindowTitleMatch || string.IsNullOrWhiteSpace(requiredForegroundWindowTitleFragment))
            {
                return true;
            }

            lastForegroundWindowTitle = GetForegroundWindowTitle();
            if (string.IsNullOrEmpty(lastForegroundWindowTitle))
            {
                return false;
            }

            var comparison = ignoreCaseInWindowTitleMatch ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return lastForegroundWindowTitle.IndexOf(requiredForegroundWindowTitleFragment, comparison) >= 0;
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
            return length > 0 ? builder.ToString() : string.Empty;
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

        private const uint INPUT_MOUSE = 0;
        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_SCANCODE = 0x0008;
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint MAPVK_VK_TO_VSC = 0;
        private const int WHEEL_DELTA = 120;
#endif
    }
}
