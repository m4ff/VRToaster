using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace VRToaster
{
    public enum ToastPosition { Top, Bottom }
    public enum ToastGroup { LeftHand = 0, RightHand = 1, Frontal = 2, Count = 3 }

    public class Toaster : MonoBehaviour
    {
        [System.Serializable]
        public struct Style
        {
            public string Name;

            [Header("Text")]

            public Font Font;
            public Color TextColor;
            public TextAnchor Anchor;

            [Header("Text Backgroung")]

            public Sprite Background;
            [ColorUsage(true, true)] public Color BackgroundColor;

            public Style(string name)
            {
                Name = name;
                Font = null;
                TextColor = Color.white;
                Anchor = TextAnchor.MiddleCenter;
                BackgroundColor = new Color(0, 0, 0, 0.5f);
                Background = null;
            }
        }

        [Tooltip("Padding expressed as fraction of the toast width")]
        [SerializeField] [Range(0, 0.5f)] float toastPadding = 0.05f;
        [Tooltip("Spacing between toasts in world units")]
        [SerializeField] float toastSpacing = 0.03f;
        [Tooltip("The center transform of the play area")]
        [SerializeField] Transform playAreaTransform;
        [SerializeField] Transform headTransform;
        [SerializeField] Transform leftHandTransform;
        [SerializeField] Transform rightHandTransform;


        [Header("Hand Toast")]

        [Tooltip("Width of the toast in world units.")]
        [SerializeField] float handToastWidth = 0.25f;
        [Tooltip("Pixels per unit.")]
        [SerializeField] int handToastDensity = 1000;
        [SerializeField] int handFontSize = 16;
        [Tooltip("Horizontal angle between the hand and the toast. Increase or decrease to move the toast further from, or closer to the hand.")]
        [SerializeField] float handToastAngle = 30;
        [SerializeField] [Range(0, 55)] float maxHeadAngle = 45;
        [SerializeField] float minHeadDistance = 1;

        [Header("Front Toast")]

        [Tooltip("Width of the toast in world units.")]
        [SerializeField] float frontalToastWidth = 0.5f;
        [Tooltip("Pixels per unit.")]
        [SerializeField] int frontalToastDensity = 500;
        [SerializeField] int frontalFontSize = 16;
        [Tooltip("Horizontal (x) and vertical (y) components of the head-toast distance vector.")]
        [SerializeField] Vector2 frontalToastPosition = new Vector2(1.5f, 0.1f);

        [Header("Styles")]

        [SerializeField] Style[] styles = new Style[] { new Style("default") };


        //RectTransform leftTransform;
        //RectTransform rightTransform;
        //RectTransform frontalTransform;

        RectTransform[] toastRoots = new RectTransform[3];
        int[] visibleToastCount = new int[3];

        //int leftVisibleCount;
        //int rightVisibleCount;
        //int frontalVisibleCount;

        readonly System.Type[] WrapperComponentList = new System.Type[] { typeof(Canvas), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter) };
        readonly System.Type[] CardWrapperComponentList = new System.Type[] { typeof(CanvasGroup), typeof(ScalableHorizontalLayoutGroup) };
        readonly System.Type[] CardComponentList = new System.Type[] { typeof(Image), typeof(HorizontalLayoutGroup) };
        readonly System.Type[] InnerTextComponentList = new System.Type[] { typeof(Text) };

        static Toaster singleton;

        internal const float AnimationTime = 0.2f;
        const float HeadMessageSmoothing = 0.05f;

        int handTextPadding, frontalTextPadding;

        Transform currentToastWrapper;
        Style currentStyle;
        int currentFontSize;
        int currentPadding;
        int currentDensity;

        //internal void UpdateVisibility(RectTransform t, int i)
        //{
        //    var parent = t.parent;
        //    if (parent == frontalTransform)
        //    {
        //        frontalVisibleCount += i;
        //    }
        //    else if(parent == leftTransform)
        //    {
        //        leftVisibleCount += i;
        //    }
        //    else
        //    {
        //        rightVisibleCount += i;
        //    }
        //}

        void OnValidate()
        {
            if (styles.Length == 0)
            {
                Debug.LogError(GetType().Name + ": the styles array needs to contain at least one element");

                styles = new Style[] { new Style("default") };
            }
        }

        void Awake()
        {
            if (singleton)
            {
                DestroyImmediate(this, false);
                return;
            }

            singleton = this;

            toastRoots[(int)ToastGroup.LeftHand] = new GameObject("VRToaster_LeftText", WrapperComponentList).GetComponent<RectTransform>();
            toastRoots[(int)ToastGroup.RightHand] = new GameObject("VRToaster_RightText", WrapperComponentList).GetComponent<RectTransform>();
            toastRoots[(int)ToastGroup.Frontal] = new GameObject("VRToaster_FrontText", WrapperComponentList).GetComponent<RectTransform>();

            InitWrapper(toastRoots[(int)ToastGroup.LeftHand], handToastWidth, handToastDensity);
            InitWrapper(toastRoots[(int)ToastGroup.RightHand], handToastWidth, handToastDensity);
            InitWrapper(toastRoots[(int)ToastGroup.Frontal], frontalToastWidth, frontalToastDensity);

            handTextPadding = Mathf.RoundToInt(handToastWidth * handToastDensity * toastPadding);
            frontalTextPadding = Mathf.RoundToInt(frontalToastWidth * frontalToastDensity * toastPadding);
        }

        void OnDestroy()
        {
            if (singleton == this)
            {
                singleton = null;

                for (int i = 0; i < (int)ToastGroup.Count; i++)
                {
                    if (toastRoots[i]) Destroy(toastRoots[i].gameObject);
                }

                //if (leftTransform) Destroy(leftTransform.gameObject);
                //if (rightTransform) Destroy(rightTransform.gameObject);
                //if (frontalTransform) Destroy(frontalTransform.gameObject);
            }
        }

        void InitWrapper(RectTransform wrapper, float width, float density)
        {
            DontDestroyOnLoad(wrapper.gameObject);

            var canvas = wrapper.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            wrapper.sizeDelta = new Vector2(width * density, 0);

            wrapper.localScale = Vector3.one / density;

            var fitter = wrapper.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layout = wrapper.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 0;
        }

        GameObject CreateToast(string textString)
        {
            var textObject = new GameObject("Text", InnerTextComponentList);
            var panelObject = new GameObject("Card", CardComponentList);
            var toastObject = new GameObject("Toast", CardWrapperComponentList);

            // Set hierarchy chain
            textObject.transform.SetParent(panelObject.transform, false);
            panelObject.transform.SetParent(toastObject.transform, false);
            toastObject.transform.SetParent(currentToastWrapper.transform, false);

            toastObject.transform.localScale = new Vector3(1, 0, 1);
            toastObject.GetComponent<CanvasGroup>().alpha = 0;

            var text = textObject.GetComponent<Text>();
            if (currentStyle.Font) text.font = currentStyle.Font;
            text.color = currentStyle.TextColor;
            text.fontSize = currentFontSize;
            text.text = textString;
            text.supportRichText = true;
            text.alignment = currentStyle.Anchor;

            var image = panelObject.GetComponent<Image>();
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.sprite = currentStyle.Background;
            image.color = currentStyle.BackgroundColor;

            var layout = panelObject.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(currentPadding, currentPadding, currentPadding, currentPadding);
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            layout = toastObject.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, Mathf.RoundToInt(toastSpacing * currentDensity));
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            //UpdateToastTransforms(true);

            return toastObject;
        }

        void SetCurrentToastFields(ToastGroup group, string style)
        {
            int styleIndex = -1;
            int styleCount = styles.Length;
            for (int i = 0; i < styleCount; i++)
            {
                if (styles[i].Name == style)
                {
                    styleIndex = i;
                    break;
                }
            }

            if (styleIndex == -1)
            {
                throw new System.ArgumentException("The style name '" + style + "' does not exist");
            }

            currentStyle = styles[styleIndex];

            if (group == ToastGroup.Frontal)
            {
                currentPadding = frontalTextPadding;
                currentFontSize = frontalFontSize;
                currentDensity = frontalToastDensity;
            }
            else
            {
                currentPadding = handTextPadding;
                currentFontSize = handFontSize;
                currentDensity = handToastDensity;
            }

            currentToastWrapper = toastRoots[(int)group];
        }

        void Clear(Transform t)
        {
            int count = t.childCount;

            for (int i = 0; i < count; i++)
            {
                Destroy(t.GetChild(i).gameObject);
            }
        }

        public static void Clear()
        {
            for (int i = 0; i < (int)ToastGroup.Count; i++)
            {
                singleton.Clear(singleton.toastRoots[i]);
            }
        }

        public static Toast MakeToast(ToastGroup group, string text, string style, ToastPosition position)
        {
            singleton.SetCurrentToastFields(group, style);
            var toast = singleton.CreateToast(text);

            var ret = new Toast(toast.GetComponent<RectTransform>(), singleton);

            var rect = toast.GetComponent<RectTransform>();

            if (position == ToastPosition.Top)
            {
                rect.SetAsFirstSibling();
            }

            return ret;
        }

        public static Toast MakeToast(ToastGroup group, string style, ToastPosition position)
        {
            return MakeToast(group, null, style, position);
        }

        public static Toast MakeToast(ToastGroup group, string text, string style)
        {
            return MakeToast(group, text, style, ToastPosition.Bottom);
        }

        public static Toast MakeToast(ToastGroup group, string style)
        {
            return MakeToast(group, null, style, ToastPosition.Bottom);
        }

        //public static Toast MakeToast(Group group, string text, Position position)
        //{
        //    return MakeToast(group, text, singleton.styles[0].Name, position);
        //}

        //public static Toast MakeToast(Group group, string text)
        //{
        //    return MakeToast(group, text, singleton.styles[0].Name, Position.Bottom);
        //}

        public static void TimedToast(ToastGroup group, string text, float time, string style, ToastPosition position)
        {
            if (time <= 0)
            {
                return;
            }

            singleton.SetCurrentToastFields(group, style);
            var toast = singleton.CreateToast(text);

            var rect = toast.GetComponent<RectTransform>();

            if (position == ToastPosition.Top)
            {
                rect.SetAsFirstSibling();
            }

            singleton.StartCoroutine(CoTimedToast(rect, time));
        }

        public static void TimedToast(ToastGroup group, string text, float time, string style)
        {
            TimedToast(group, text, time, style, ToastPosition.Bottom);
        }

        //public static void TimedToast(Group group, string text, float time, Position position)
        //{
        //    TimedToast(group, text, time, singleton.styles[0].Name, position);
        //}

        //public static void TimedToast(Group group, string text, float time)
        //{
        //    TimedToast(group, text, time, singleton.styles[0].Name, Position.Bottom);
        //}

        internal static IEnumerator CoAnimateToast(RectTransform toast, float target, System.Action cb = null)
        {
            var parent = toast.parent as RectTransform;
            var group = toast.GetComponent<CanvasGroup>();
            var scale = toast.localScale;

            float start = scale.y;
            float delta = Mathf.Abs(target - start);
            float elapsed = 0;

            while (elapsed <= delta)
            {
                elapsed += Time.deltaTime / AnimationTime;
                float t = Mathf.Lerp(start, target, elapsed);

                scale.y = t;
                group.alpha = t;

                toast.localScale = scale;
                toast.ForceUpdateRectTransforms();

                LayoutRebuilder.MarkLayoutForRebuild(toast);

                yield return null;

                if (!group) break;
            }

            if(start != target)
            {
                singleton.UpdateVisibility(parent, target == 1 ? 1 : 0);
            }

            if (cb != null) cb.Invoke();
        }

        void UpdateVisibility(RectTransform root, int increment)
        {
            int rootIndex = System.Array.IndexOf(toastRoots, root);

            if (rootIndex == (int)ToastGroup.Frontal && increment == 1 && visibleToastCount[rootIndex] == 0)
            {
                UpdateFrontTransform(true);
            }

            visibleToastCount[rootIndex] += increment;
        }

        //static IEnumerator CoShowToast(RectTransform toast)
        //{
        //    var parent = toast.parent as RectTransform;
        //    var group = toast.GetComponent<CanvasGroup>();
        //    var scale = toast.localScale;
        //    float elapsed = AnimationTime * scale.y;

        //    while (elapsed < AnimationTime)
        //    {
        //        elapsed += Time.deltaTime;
        //        float t = elapsed / AnimationTime;
        //        scale.y = Mathf.Clamp01(t);
        //        group.alpha = t;

        //        toast.localScale = scale;
        //        toast.ForceUpdateRectTransforms();

        //        LayoutRebuilder.MarkLayoutForRebuild(toast);

        //        yield return null;
        //    }
        //}

        //static IEnumerator CoHideToast(RectTransform toast)
        //{
        //    var parent = toast.parent as RectTransform;
        //    var group = toast.GetComponent<CanvasGroup>();
        //    var scale = toast.localScale;
        //    float elapsed = AnimationTime * scale.y;

        //    while (elapsed > 0)
        //    {
        //        elapsed -= Time.deltaTime;
        //        float t = elapsed / AnimationTime;
        //        scale.y = Mathf.Clamp01(t);
        //        group.alpha = t;

        //        toast.localScale = scale;
        //        toast.ForceUpdateRectTransforms();

        //        LayoutRebuilder.MarkLayoutForRebuild(toast);

        //        yield return null;
        //    }
        //}

        static IEnumerator CoTimedToast(RectTransform toast, float time)
        {
            yield return singleton.StartCoroutine(CoAnimateToast(toast, 1));

            if (time > 0)
            {
                yield return new WaitForSecondsRealtime(time);
            }

            yield return singleton.StartCoroutine(CoAnimateToast(toast, 0));

            Destroy(toast.gameObject);
        }

        void UpdateHandTransform(RectTransform msg, Transform controller, float sign)
        {
            var headPos = headTransform.position;
            var dir = controller.position - headPos;

            var up = playAreaTransform.up;
            var step = Quaternion.AngleAxis(sign * handToastAngle, up);

            dir = step * dir;

            dir = Vector3.RotateTowards(headTransform.forward.normalized * minHeadDistance, dir, maxHeadAngle * Mathf.Deg2Rad, Mathf.Infinity);

            var pos = headPos + dir;
            var rot = Quaternion.LookRotation(dir, up);

            msg.SetPositionAndRotation(pos, rot);
            msg.ForceUpdateRectTransforms();
        }

        void UpdateHandTransform1(RectTransform msg, Transform controller, float sign)
        {
            var headPos = headTransform.position;
            var dir = controller.position - headPos;

            var up = playAreaTransform.up;
            var step = Quaternion.AngleAxis(sign * handToastAngle, up);

            var newDir = step * dir;
            var pos = headPos + newDir;
            var rot = Quaternion.LookRotation(newDir, up);

            msg.SetPositionAndRotation(pos, rot);
            msg.ForceUpdateRectTransforms();
        }

        void UpdateFrontTransform(bool reset)
        {
            RectTransform msg = toastRoots[(int)ToastGroup.Frontal];

            var locFwd = playAreaTransform.InverseTransformDirection(headTransform.forward);
            locFwd.y = frontalToastPosition.y;

            var fwd = playAreaTransform.TransformDirection(locFwd).normalized * frontalToastPosition.x;

            var currFwd = msg.position - headTransform.position;

            Vector3 smoothFwd;

            if (reset)
            {
                smoothFwd = fwd;
            }
            else
            {
                smoothFwd = Vector3.Slerp(currFwd, fwd, HeadMessageSmoothing);
            }

            var newPos = headTransform.position + smoothFwd;
            var newRot = Quaternion.LookRotation(smoothFwd, playAreaTransform.up);

            msg.SetPositionAndRotation(newPos, newRot);
            msg.ForceUpdateRectTransforms();
        }

        void Update()
        {
            if (visibleToastCount[(int)ToastGroup.LeftHand] > 0)
            {
                UpdateHandTransform(toastRoots[(int)ToastGroup.LeftHand], leftHandTransform, -1);
            }
            if (visibleToastCount[(int)ToastGroup.RightHand] > 0)
            {
                UpdateHandTransform(toastRoots[(int)ToastGroup.RightHand], rightHandTransform, 1);
            }
            if (visibleToastCount[(int)ToastGroup.Frontal] > 0)
            {
                UpdateFrontTransform(false);
            }
        }

        //void UpdateToastTransforms(bool reset)
        //{

        //}
    }

    class ScalableHorizontalLayoutGroup : HorizontalLayoutGroup
    {
        public override float preferredHeight
        {
            get
            {
                return base.preferredHeight * transform.localScale.y;
            }
        }

        public override float flexibleHeight
        {
            get
            {
                return base.flexibleHeight * transform.localScale.y;
            }
        }

        public override float minHeight
        {
            get
            {
                return base.minHeight * transform.localScale.y;
            }
        }
    }
}
