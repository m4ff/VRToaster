using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRToaster
{
    public class Toast
    {
        RectTransform toast;
        Coroutine coroutine;
        Toaster toaster;

        public Toast(RectTransform w, Toaster t)
        {
            toast = w;
            toaster = t;
        }

        public void Hide()
        {
            if (coroutine != null) toaster.StopCoroutine(coroutine);
            coroutine = toaster.StartCoroutine(Toaster.CoAnimateToast(toast, 0));
        }

        public void Show(string text = null)
        {
            if (text as object != null)
            {
                toast.GetComponentInChildren<Text>().text = text;
            }

            if (coroutine != null) toaster.StopCoroutine(coroutine);
            coroutine = toaster.StartCoroutine(Toaster.CoAnimateToast(toast, 1));
        }

        public void Replace(string text)
        {
            if (coroutine != null) toaster.StopCoroutine(coroutine);
            coroutine = null;

            toaster.StartCoroutine(Toaster.CoAnimateToast(toast, 0, GetDestroyCallback(toast)));

            var clone = Object.Instantiate(toast, toast.parent, true);

            var rect = clone.GetComponent<RectTransform>();
            rect.SetSiblingIndex(toast.GetSiblingIndex() + 1);
            rect.localScale = new Vector3(1, 0, 1);

            clone.GetComponent<CanvasGroup>().alpha = 0;

            toast = clone;

            Show(text);
        }

        public void Destroy(float time = 0)
        {
            if (coroutine != null) toaster.StopCoroutine(coroutine);
            toaster.StartCoroutine(Toaster.CoAnimateToast(toast, 0, GetDestroyCallback(toast)));
            coroutine = null;
            toast = null;
        }

        static System.Action GetDestroyCallback(RectTransform t)
        {

            return () => { if (t) Object.Destroy(t.gameObject); };
        }
    }
}
