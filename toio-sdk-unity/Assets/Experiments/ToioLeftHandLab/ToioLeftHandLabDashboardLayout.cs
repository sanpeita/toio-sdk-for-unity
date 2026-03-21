using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace toio.Experiments.ToioLeftHandLab
{
    public sealed class DashboardTextRefs
    {
        public Text TextBattery { get; internal set; }
        public Text TextFlat { get; internal set; }
        public Text TextCollision { get; internal set; }
        public Text TextButton { get; internal set; }
        public Text TextPositionID { get; internal set; }
        public Text TextStandardID { get; internal set; }
        public Text TextAngle { get; internal set; }
        public Text TextDoubleTap { get; internal set; }
        public Text TextPose { get; internal set; }
        public Text TextShake { get; internal set; }
        public Text TextSpeed { get; internal set; }
        public Text TextMag { get; internal set; }
        public Text TextAttitude { get; internal set; }

        public IEnumerable<Text> AllTexts
        {
            get
            {
                yield return TextBattery;
                yield return TextFlat;
                yield return TextCollision;
                yield return TextButton;
                yield return TextPositionID;
                yield return TextStandardID;
                yield return TextAngle;
                yield return TextDoubleTap;
                yield return TextPose;
                yield return TextShake;
                yield return TextSpeed;
                yield return TextMag;
                yield return TextAttitude;
            }
        }
    }

    public sealed class ToioLeftHandLabDashboardLayout
    {
        private const string RootName = "ToioLeftHandLabDashboard";
        private const string LeftColumnName = "ToioLeftHandLabDashboardLeftColumn";
        private const string RightColumnName = "ToioLeftHandLabDashboardRightColumn";

        private static readonly string[] LeftColumnLabels =
        {
            "TextBattery",
            "TextFlat",
            "TextCollision",
            "TextButton",
            "TextPositionID",
            "TextStandardID",
            "TextAngle"
        };

        private static readonly string[] RightColumnLabels =
        {
            "TextDoubleTap",
            "TextPose",
            "TextShake",
            "TextSpeed",
            "TextMag",
            "TextAttitude"
        };

        public DashboardTextRefs Ensure(Canvas canvas, IEnumerable<Text> legacyTexts)
        {
            if (canvas == null)
            {
                throw new ArgumentNullException(nameof(canvas));
            }

            HideLegacyTexts(legacyTexts);

            RectTransform root = EnsureRoot(canvas.transform);
            RectTransform leftColumn = EnsureColumn(root, LeftColumnName);
            RectTransform rightColumn = EnsureColumn(root, RightColumnName);

            ApplyRootStyle(root);
            ApplyColumnStyle(leftColumn);
            ApplyColumnStyle(rightColumn);

            DashboardTextRefs refs = new DashboardTextRefs();
            refs.TextBattery = EnsureText(leftColumn, LeftColumnLabels[0], 0);
            refs.TextFlat = EnsureText(leftColumn, LeftColumnLabels[1], 1);
            refs.TextCollision = EnsureText(leftColumn, LeftColumnLabels[2], 2);
            refs.TextButton = EnsureText(leftColumn, LeftColumnLabels[3], 3);
            refs.TextPositionID = EnsureText(leftColumn, LeftColumnLabels[4], 4);
            refs.TextStandardID = EnsureText(leftColumn, LeftColumnLabels[5], 5);
            refs.TextAngle = EnsureText(leftColumn, LeftColumnLabels[6], 6);

            refs.TextDoubleTap = EnsureText(rightColumn, RightColumnLabels[0], 0);
            refs.TextPose = EnsureText(rightColumn, RightColumnLabels[1], 1);
            refs.TextShake = EnsureText(rightColumn, RightColumnLabels[2], 2);
            refs.TextSpeed = EnsureText(rightColumn, RightColumnLabels[3], 3);
            refs.TextMag = EnsureText(rightColumn, RightColumnLabels[4], 4);
            refs.TextAttitude = EnsureText(rightColumn, RightColumnLabels[5], 5);

            root.SetAsLastSibling();
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            return refs;
        }

        private static void HideLegacyTexts(IEnumerable<Text> legacyTexts)
        {
            if (legacyTexts == null)
            {
                return;
            }

            foreach (Text legacyText in legacyTexts)
            {
                if (legacyText == null)
                {
                    continue;
                }

                legacyText.gameObject.SetActive(false);
            }
        }

        private static RectTransform EnsureRoot(Transform canvasTransform)
        {
            Transform found = FindDescendant(canvasTransform, RootName);
            GameObject rootObject = found != null ? found.gameObject : null;
            if (rootObject == null)
            {
                rootObject = new GameObject(
                    RootName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(HorizontalLayoutGroup)
                );
                rootObject.transform.SetParent(canvasTransform, false);
            }

            RectTransform root = rootObject.GetComponent<RectTransform>();
            if (root == null)
            {
                root = rootObject.AddComponent<RectTransform>();
            }

            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = new Vector2(140f, -36f);
            root.sizeDelta = new Vector2(1180f, 620f);

            Image image = GetOrAddComponent<Image>(rootObject);
            image.color = new Color32(180, 230, 245, 235);
            image.raycastTarget = false;

            HorizontalLayoutGroup layout = GetOrAddComponent<HorizontalLayoutGroup>(rootObject);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.spacing = 18f;
            layout.padding = new RectOffset(24, 24, 24, 24);

            return root;
        }

        private static RectTransform EnsureColumn(RectTransform root, string name)
        {
            Transform existing = root.Find(name);
            GameObject columnObject = existing != null ? existing.gameObject : null;
            if (columnObject == null)
            {
                columnObject = new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(VerticalLayoutGroup),
                    typeof(LayoutElement)
                );
                columnObject.transform.SetParent(root, false);
            }

            RectTransform column = columnObject.GetComponent<RectTransform>();
            if (column == null)
            {
                column = columnObject.AddComponent<RectTransform>();
            }

            LayoutElement layoutElement = GetOrAddComponent<LayoutElement>(columnObject);
            layoutElement.flexibleWidth = 1f;
            layoutElement.flexibleHeight = 1f;

            return column;
        }

        private static void ApplyRootStyle(RectTransform root)
        {
            if (root != null)
            {
                root.SetAsLastSibling();
            }
        }

        private static void ApplyColumnStyle(RectTransform column)
        {
            if (column == null)
            {
                return;
            }

            VerticalLayoutGroup layout = GetOrAddComponent<VerticalLayoutGroup>(column.gameObject);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 6f;
            layout.padding = new RectOffset(0, 0, 0, 0);
        }

        private static Text EnsureText(RectTransform parent, string name, int siblingIndex)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            Transform existing = parent.Find(name);
            GameObject textObject = existing != null ? existing.gameObject : null;
            if (textObject == null)
            {
                textObject = new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text),
                    typeof(LayoutElement)
                );
                textObject.transform.SetParent(parent, false);
            }

            textObject.transform.SetSiblingIndex(siblingIndex);

            Text text = GetOrAddComponent<Text>(textObject);
            ConfigureText(text, name);

            LayoutElement layoutElement = GetOrAddComponent<LayoutElement>(textObject);
            layoutElement.minHeight = 40f;
            layoutElement.preferredHeight = GetPreferredHeight(name);
            layoutElement.flexibleWidth = 1f;
            layoutElement.flexibleHeight = 0f;

            return text;
        }

        private static void ConfigureText(Text text, string name)
        {
            if (text == null)
            {
                return;
            }

            text.gameObject.SetActive(true);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20;
            text.fontStyle = FontStyle.Normal;
            text.color = new Color32(18, 48, 70, 255);
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = false;
            text.raycastTarget = false;
            text.supportRichText = false;
            text.text = DisplayNameFromTextObject(name);
        }

        private static float GetPreferredHeight(string name)
        {
            switch (name)
            {
                case "TextCollision":
                case "TextDoubleTap":
                case "TextPositionID":
                case "TextStandardID":
                case "TextAngle":
                case "TextMag":
                case "TextAttitude":
                    return 72f;
                default:
                    return 40f;
            }
        }

        private static string DisplayNameFromTextObject(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            if (name.StartsWith("Text", StringComparison.Ordinal) && name.Length > 4)
            {
                return name.Substring(4);
            }

            return name;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                Transform found = FindDescendant(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }
    }
}
