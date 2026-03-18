using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Text;

namespace toio.Experiments.ToioLeftHandLab
{
    public class ToioLeftHandLabController : MonoBehaviour
    {
        private const string VersionLabel = "ver1.0";

        [SerializeField] private toio.Samples.Sample_Sensor.ToioWasdInput inputSource;
        [SerializeField] private bool showKeyboardFallbackHint = true;
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

        private string footerMessage = "Toio Left Hand Lab ver1.0. W/S uses pitch tilt. A/D uses roll tilt.";

        private void Awake()
        {
            if (inputSource == null)
            {
                inputSource = GetComponent<toio.Samples.Sample_Sensor.ToioWasdInput>();
            }

            if (inputSource != null)
            {
                inputSource.VirtualKeyInjected += OnVirtualKeyInjected;
            }

            this.textBattery = FindText("TextBattery");
            this.textCollision = FindText("TextCollision");
            this.textFlat = FindText("TextFlat");
            this.textPositionID = FindText("TextPositionID");
            this.textStandardID = FindText("TextStandardID");
            this.textButton = FindText("TextButton");
            this.textAngle = FindText("TextAngle");
            this.textDoubleTap = FindText("TextDoubleTap");
            this.textPose = FindText("TextPose");
            this.textShake = FindText("TextShake");
            this.textSpeed = FindText("TextSpeed");
            this.textMag = FindText("TextMag");
            this.textAttitude = FindText("TextAttitude");
            EnsureKeyLogUi();
        }

        private void Start()
        {
            RefreshTexts();
        }

        private void OnDestroy()
        {
            if (inputSource != null)
            {
                inputSource.VirtualKeyInjected -= OnVirtualKeyInjected;
            }
        }

        private void Update()
        {
            RefreshTexts();
        }

        public async void OnBtnConnect()
        {
            if (inputSource == null)
            {
                return;
            }

            footerMessage = "Connecting to nearest toio core cube...";
            RefreshTexts();
            await inputSource.Connect();
            footerMessage = "Connected. ver1.0 is ready. Tilt forward/back for W/S, tilt left/right for A/D.";
        }

        public void Forward()
        {
            inputSource?.InjectVirtualKey(KeyCode.W);
            footerMessage = "Debug inject: W";
        }

        public void Backward()
        {
            inputSource?.InjectVirtualKey(KeyCode.S);
            footerMessage = "Debug inject: S";
        }

        public void TurnRight()
        {
            inputSource?.InjectVirtualKey(KeyCode.D);
            footerMessage = "Debug inject: D";
        }

        public void TurnLeft()
        {
            inputSource?.InjectVirtualKey(KeyCode.A);
            footerMessage = "Debug inject: A";
        }

        public void Stop()
        {
            inputSource?.ClearVirtualKeys();
            ClearInputLog();
            footerMessage = "Virtual key state cleared.";
        }

        public void OnSwitchMag()
        {
            footerMessage = "Magnetic sensor is not used in this experiment.";
        }

        public void OnSwitchAttitude()
        {
            footerMessage = "Attitude sensing is always used here for A/D detection.";
        }

        private void RefreshTexts()
        {
            if (inputSource == null)
            {
                return;
            }

            var connected = inputSource.IsConnected;
            var horizontal = inputSource.Horizontal;
            var vertical = inputSource.Vertical;

            SetText(textBattery, connected ? "Connect: Connected" : "Connect: Not connected");
            SetText(textFlat, $"W: {(inputSource.WPressed ? "ON" : "off")}");
            SetText(textButton, $"S: {(inputSource.SPressed ? "ON" : "off")}");
            SetText(textCollision, $"A: {(inputSource.APressed ? "ON" : "off")}");
            SetText(textDoubleTap, $"D: {(inputSource.DPressed ? "ON" : "off")}");
            SetText(textPose, $"Vertical Axis: {vertical:+0;-0;0}");
            SetText(textShake, $"Horizontal Axis: {horizontal:+0;-0;0}");
            SetText(textPositionID, $"Intent: left-hand toio input gadget experiment {VersionLabel}.");
            SetText(textStandardID, "Detected keys are typed into the on-screen text box. W/S uses pitch, A/D uses roll.");
            SetText(textAngle, connected ? "Cube: ready" : "Press Connect to start.");
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
            ConfigureRect(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(860f, 160f));
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

            var labelRect = CreateUiObject("ToioKeyInputLabel", panel);
            ConfigureRect(labelRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(-24f, 28f));
            keyLogLabel = labelRect.gameObject.AddComponent<Text>();
            keyLogLabel.font = font;
            keyLogLabel.fontSize = 24;
            keyLogLabel.alignment = TextAnchor.MiddleLeft;
            keyLogLabel.color = Color.white;
            keyLogLabel.text = $"toio key input box {VersionLabel}";

            var inputRoot = CreateUiObject("ToioKeyInputField", panel);
            ConfigureRect(inputRoot, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(-24f, 92f));
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
            placeholderText.text = "Detected W/A/S/D will be typed here...";

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
