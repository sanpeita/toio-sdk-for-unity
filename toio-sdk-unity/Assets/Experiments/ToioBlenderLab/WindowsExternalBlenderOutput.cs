using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace toio.Experiments.ToioBlenderLab
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ToioBlenderCubeInput))]
    public class WindowsExternalBlenderOutput : MonoBehaviour
    {
        private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);

        private struct QueuedInputStep
        {
            public ushort virtualKey;
            public bool withShift;
            public string label;
            public float delayAfterSeconds;
        }

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

        [Header("Button")]
        [SerializeField] private bool sendTabForModeToggle = true;

        [Header("Cube 2 Macros")]
        [SerializeField] private bool sendAddCubeMacro = true;
        [SerializeField] private bool sendAddPlaneMacro = true;
        [SerializeField] private bool sendMaterialPreviewMacro = true;
        [SerializeField] private bool sendSolidMacro = true;
        [SerializeField] private bool useCommandFileBridgeForMeshMacros = true;
        [SerializeField] private float meshBridgeDuplicateSuppressionSeconds = 2.5f;
        [SerializeField] private float queuedInputStepIntervalSeconds = 0.05f;
        [SerializeField] private int addMenuRootResetUpStepCount = 6;
        [SerializeField] private float addMenuStepDelaySeconds = 0.08f;
        [SerializeField] private float addMacroSettleSeconds = 0.18f;
        [SerializeField] private int meshMenuResetUpStepCount = 4;
        [SerializeField] private int cubeMenuDownStepCount = 1;

        [Header("Viewport Anchor")]
        [SerializeField] private bool repositionCursorBeforeEditMacro = true;
        [SerializeField] [Range(0.05f, 0.95f)] private float viewportAnchorNormalizedX = 0.42f;
        [SerializeField] [Range(0.05f, 0.95f)] private float viewportAnchorNormalizedY = 0.38f;
        [SerializeField] private bool restoreCursorAfterEditMacro = true;

        [Header("Debug")]
        [SerializeField] private bool logOutput = false;

        private bool middleButtonHeld;
        private float orbitResidualX;
        private float zoomResidualSteps;
        private string lastForegroundWindowTitle = string.Empty;
        private readonly Queue<QueuedInputStep> queuedInputSteps = new Queue<QueuedInputStep>();
        private float nextQueuedInputAt;
        private string lastMacroStatus = "Idle";
        private bool hasAnchoredCursorForQueuedMacro;
        private bool hasStoredCursorPosition;
        private POINT storedCursorPosition;
        private string meshCommandBridgePath = string.Empty;
        private string lastBridgeCommand = string.Empty;
        private float lastBridgeCommandAt = -999f;
        private int nextBridgeCommandId = 1;

        public string RuntimeStatus { get; private set; } = "Waiting for cube pair connection.";
        public string RequiredForegroundWindowTitleFragment => requiredForegroundWindowTitleFragment;

        private void Awake()
        {
            Application.runInBackground = true;
            if (inputSource == null)
            {
                inputSource = GetComponent<ToioBlenderCubeInput>();
            }

            meshCommandBridgePath = ResolveMeshCommandBridgePath();
            EnsureMeshCommandBridgeDirectory();
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
                RuntimeStatus = "Cube pair not connected yet.";
                ReleaseTransientInputs();
                ClearQueuedSteps();
                inputSource.ClearPendingModeToggles();
                inputSource.ClearPendingEditMacros();
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

            EnqueuePendingCommands();
            ProcessQueuedInputSteps(Time.unscaledTime);

            var hasQueuedSteps = queuedInputSteps.Count > 0;
            if (hasQueuedSteps)
            {
                ReleaseTransientInputs();
            }

            UpdateOrbit(Time.unscaledDeltaTime, hasQueuedSteps);
            UpdateZoom(Time.unscaledDeltaTime, hasQueuedSteps);
            RuntimeStatus = BuildActiveStatus();
#else
            RuntimeStatus = "Windows external output is only available in Unity Editor or Standalone Windows.";
#endif
        }

        private void OnDisable()
        {
            ReleaseTransientInputs();
            ClearQueuedSteps();
            RestoreCursorAfterMacroIfNeeded();
        }

        private void OnDestroy()
        {
            ReleaseTransientInputs();
            ClearQueuedSteps();
            RestoreCursorAfterMacroIfNeeded();
        }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private void EnqueuePendingCommands()
        {
            if (sendTabForModeToggle && inputSource != null)
            {
                while (inputSource.ConsumeModeToggleRequested())
                {
                    EnqueueKeyTap(0x09, false, "Tab");
                }
            }

            while (inputSource != null && inputSource.ConsumePendingEditMacro(out var action))
            {
                EnqueueEditMacro(action);
            }
        }

        private void EnqueueEditMacro(ToioBlenderCubeInput.EditMacroAction action)
        {
            PrepareCursorForEditMacro();

            switch (action)
            {
                case ToioBlenderCubeInput.EditMacroAction.AddCube:
                    if (!sendAddCubeMacro)
                    {
                        return;
                    }

                    if (useCommandFileBridgeForMeshMacros)
                    {
                        WriteMeshCommandBridgeCommand("add_cube", "Bridge Add Cube");
                        return;
                    }

                    EnqueueKeyTap(0x41, true, "Shift+A");
                    for (var i = 0; i < Mathf.Max(1, addMenuRootResetUpStepCount); i++)
                    {
                        EnqueueKeyTap(VK_UP, false, $"Reset Add Root Top {i + 1}", addMenuStepDelaySeconds);
                    }
                    EnqueueKeyTap(VK_RETURN, false, "Enter Mesh", addMenuStepDelaySeconds);
                    for (var i = 0; i < Mathf.Max(1, meshMenuResetUpStepCount); i++)
                    {
                        EnqueueKeyTap(VK_UP, false, $"Reset Mesh Top {i + 1}", addMenuStepDelaySeconds);
                    }
                    for (var i = 0; i < Mathf.Max(1, cubeMenuDownStepCount); i++)
                    {
                        EnqueueKeyTap(VK_DOWN, false, $"Down to Cube {i + 1}", addMenuStepDelaySeconds);
                    }
                    EnqueueKeyTap(VK_RETURN, false, "Confirm Cube", addMacroSettleSeconds);
                    EnqueueKeyTap(VK_ESCAPE, false, "Esc");
                    lastMacroStatus = "Queued Add Cube";
                    break;
                case ToioBlenderCubeInput.EditMacroAction.AddPlane:
                    if (!sendAddPlaneMacro)
                    {
                        return;
                    }

                    if (useCommandFileBridgeForMeshMacros)
                    {
                        WriteMeshCommandBridgeCommand("add_plane", "Bridge Add Plane");
                        return;
                    }

                    EnqueueKeyTap(0x41, true, "Shift+A");
                    for (var i = 0; i < Mathf.Max(1, addMenuRootResetUpStepCount); i++)
                    {
                        EnqueueKeyTap(VK_UP, false, $"Reset Add Root Top {i + 1}", addMenuStepDelaySeconds);
                    }
                    EnqueueKeyTap(VK_RETURN, false, "Enter Mesh", addMenuStepDelaySeconds);
                    for (var i = 0; i < Mathf.Max(1, meshMenuResetUpStepCount); i++)
                    {
                        EnqueueKeyTap(VK_UP, false, $"Reset Mesh Top {i + 1}", addMenuStepDelaySeconds);
                    }
                    EnqueueKeyTap(VK_RETURN, false, "Confirm Plane", addMacroSettleSeconds);
                    EnqueueKeyTap(VK_ESCAPE, false, "Esc");
                    lastMacroStatus = "Queued Add Plane";
                    break;
                case ToioBlenderCubeInput.EditMacroAction.MaterialPreview:
                    if (!sendMaterialPreviewMacro)
                    {
                        return;
                    }

                    EnqueueKeyTap(0x5A, false, "Z");
                    EnqueueKeyTap(0x4D, false, "M");
                    lastMacroStatus = "Queued Material Preview";
                    break;
                case ToioBlenderCubeInput.EditMacroAction.Solid:
                    if (!sendSolidMacro)
                    {
                        return;
                    }

                    EnqueueKeyTap(0x5A, false, "Z");
                    EnqueueKeyTap(0x53, false, "S");
                    lastMacroStatus = "Queued Solid";
                    break;
            }
        }

        private void EnqueueKeyTap(ushort virtualKey, bool withShift, string label, float delayAfterSeconds = 0f)
        {
            queuedInputSteps.Enqueue(new QueuedInputStep
            {
                virtualKey = virtualKey,
                withShift = withShift,
                label = label,
                delayAfterSeconds = delayAfterSeconds
            });
        }

        private void ProcessQueuedInputSteps(float now)
        {
            if (queuedInputSteps.Count <= 0 || now < nextQueuedInputAt)
            {
                return;
            }

            var step = queuedInputSteps.Dequeue();
            SendKeyTap(step.virtualKey, step.withShift);
            nextQueuedInputAt = now + Mathf.Max(0.01f, Mathf.Max(queuedInputStepIntervalSeconds, step.delayAfterSeconds));

            if (logOutput)
            {
                Debug.Log($"Blender output => {step.label}");
            }

            if (queuedInputSteps.Count == 0 && !string.IsNullOrEmpty(step.label))
            {
                lastMacroStatus = $"Sent {step.label}";
                RestoreCursorAfterMacroIfNeeded();
            }
        }

        private void UpdateOrbit(float deltaTime, bool suspendContinuousInput)
        {
            if (suspendContinuousInput || !sendOrbit || deltaTime <= 0f || inputSource == null)
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

        private void UpdateZoom(float deltaTime, bool suspendContinuousInput)
        {
            if (suspendContinuousInput || !sendZoom || deltaTime <= 0f || inputSource == null)
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
            if (useCommandFileBridgeForMeshMacros)
            {
                statusBuilder.Append(" | Mesh bridge");
            }
            if (queuedInputSteps.Count > 0)
            {
                statusBuilder.Append($" | Macro queue={queuedInputSteps.Count}");
            }
            else if (!string.IsNullOrEmpty(lastMacroStatus))
            {
                statusBuilder.Append($" | {lastMacroStatus}");
            }

            if (Mathf.Abs(inputSource.OrbitAxis) > 0.001f)
            {
                statusBuilder.Append(inputSource.OrbitAxis > 0f ? " | Orbit Right" : " | Orbit Left");
            }

            if (Mathf.Abs(inputSource.ZoomAxis) > 0.001f)
            {
                statusBuilder.Append(inputSource.ZoomAxis > 0f ? " | Zoom In" : " | Zoom Out");
            }

            if (
                Mathf.Abs(inputSource.OrbitAxis) <= 0.001f &&
                Mathf.Abs(inputSource.ZoomAxis) <= 0.001f &&
                queuedInputSteps.Count == 0 &&
                string.Equals(lastMacroStatus, "Idle", StringComparison.Ordinal)
            )
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

        private void ClearQueuedSteps()
        {
            queuedInputSteps.Clear();
            nextQueuedInputAt = 0f;
            lastMacroStatus = "Idle";
            lastBridgeCommand = string.Empty;
            lastBridgeCommandAt = -999f;
            hasAnchoredCursorForQueuedMacro = false;
        }

        private string ResolveMeshCommandBridgePath()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                return string.Empty;
            }

            return Path.Combine(projectRoot, "BlenderBridge", "toio_blender_bridge_commands.jsonl");
        }

        private void EnsureMeshCommandBridgeDirectory()
        {
            if (string.IsNullOrEmpty(meshCommandBridgePath))
            {
                return;
            }

            try
            {
                var directory = Path.GetDirectoryName(meshCommandBridgePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception ex)
            {
                lastMacroStatus = $"Bridge dir failed: {ex.Message}";
            }
        }

        private void WriteMeshCommandBridgeCommand(string command, string statusLabel)
        {
            if (string.IsNullOrEmpty(meshCommandBridgePath))
            {
                lastMacroStatus = "Bridge path missing";
                return;
            }

            try
            {
                var now = Time.unscaledTime;
                if (
                    string.Equals(lastBridgeCommand, command, StringComparison.Ordinal) &&
                    now - lastBridgeCommandAt < meshBridgeDuplicateSuppressionSeconds
                )
                {
                    lastMacroStatus = $"Suppressed duplicate {statusLabel}";
                    if (logOutput)
                    {
                        Debug.Log($"Blender bridge duplicate suppressed => {command}");
                    }

                    return;
                }

                var commandId = nextBridgeCommandId++;
                var line =
                    $"{{\"id\":{commandId},\"command\":\"{command}\",\"issuedAtUtc\":\"{DateTime.UtcNow:O}\",\"unityTime\":{now:F3},\"frame\":{Time.frameCount}}}{Environment.NewLine}";
                File.AppendAllText(meshCommandBridgePath, line, Utf8WithoutBom);
                lastMacroStatus = statusLabel;
                lastBridgeCommand = command;
                lastBridgeCommandAt = now;
                if (logOutput)
                {
                    Debug.Log($"Blender bridge => #{commandId} {command} ({meshCommandBridgePath})");
                }
            }
            catch (Exception ex)
            {
                lastMacroStatus = $"Bridge write failed: {ex.Message}";
                Debug.LogException(ex);
            }
        }

        private void PrepareCursorForEditMacro()
        {
            if (!repositionCursorBeforeEditMacro || hasAnchoredCursorForQueuedMacro)
            {
                return;
            }

            var foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
            {
                return;
            }

            if (!GetCursorPos(out storedCursorPosition))
            {
                hasStoredCursorPosition = false;
            }
            else
            {
                hasStoredCursorPosition = true;
            }

            if (!TryMoveCursorToViewportAnchor(foregroundWindow))
            {
                hasStoredCursorPosition = false;
                return;
            }

            hasAnchoredCursorForQueuedMacro = true;
        }

        private void RestoreCursorAfterMacroIfNeeded()
        {
            if (!restoreCursorAfterEditMacro || !hasAnchoredCursorForQueuedMacro || !hasStoredCursorPosition)
            {
                hasAnchoredCursorForQueuedMacro = false;
                hasStoredCursorPosition = false;
                return;
            }

            SetCursorPos(storedCursorPosition.X, storedCursorPosition.Y);
            hasAnchoredCursorForQueuedMacro = false;
            hasStoredCursorPosition = false;
        }

        private bool TryMoveCursorToViewportAnchor(IntPtr foregroundWindow)
        {
            if (!GetWindowRect(foregroundWindow, out var rect))
            {
                return false;
            }

            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;
            if (width <= 0 || height <= 0)
            {
                return false;
            }

            var anchorX = rect.Left + Mathf.RoundToInt(width * viewportAnchorNormalizedX);
            var anchorY = rect.Top + Mathf.RoundToInt(height * viewportAnchorNormalizedY);
            return SetCursorPos(anchorX, anchorY);
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

        private void SendKeyTap(ushort virtualKey, bool withShift)
        {
            if (withShift)
            {
                SendKeyEvent(VK_SHIFT, false);
            }

            SendKeyEvent(virtualKey, false);
            SendKeyEvent(virtualKey, true);

            if (withShift)
            {
                SendKeyEvent(VK_SHIFT, true);
            }
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

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetCursorPos(int x, int y);

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
        private const ushort VK_SHIFT = 0x10;
        private const ushort VK_RETURN = 0x0D;
        private const ushort VK_UP = 0x26;
        private const ushort VK_DOWN = 0x28;
        private const ushort VK_ESCAPE = 0x1B;
#endif
    }
}
