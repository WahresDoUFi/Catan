using System;
using System.Collections;
using UnityEngine;

public static class UITween
{
    public static Ease<float> DefaultEasing = (float start, float end, float progress) => Mathf.Lerp(start, end, progress);
    public delegate T Ease<T>(T start, T end, float progress);

    public static void AnimatePosition(RectTransform rect, Vector2 start, Vector2 end, float duration, Ease<Vector2> easingFunction)
    {
        rect.GetComponent<MonoBehaviour>().StartCoroutine(Animate(duration, progress =>
        {
            rect.anchoredPosition = easingFunction(start, end, progress);
        }));
    }
    public static void AnimateScale(MonoBehaviour target, float from, float to, float duration, Ease<float> easingFunction = null)
    {
        if (easingFunction == null) easingFunction = DefaultEasing;
        var transform = target.transform;
        target.StartCoroutine(Animate(duration, progress =>
        {
            transform.localScale = Vector3.one * easingFunction(from, to, progress);
        }));
    }

    public static void AnimateAlpha(CanvasGroup canvasGroup, float from, float to, float duration, Ease<float> easingFunction = null)
    {
        if (easingFunction == null) easingFunction = DefaultEasing;
        canvasGroup.GetComponent<MonoBehaviour>().StartCoroutine(Animate(duration, progress =>
        {
            canvasGroup.alpha = easingFunction(from, to, progress);
        }));
    }

    public static void DelayAction(MonoBehaviour target, float delay, Action callback)
    {
        target.StartCoroutine(DelayedCall(delay, callback));
    }

    private static IEnumerator DelayedCall(float delay, Action callback)
    {
        yield return new WaitForSeconds(delay);
        callback.Invoke();
    } 

    private static IEnumerator Animate(float time, Action<float> animationCallback)
    {
        var t = 0f;
        animationCallback.Invoke(0);
        while (t < time)
        {
            t += Time.deltaTime;
            animationCallback.Invoke(t / time);
            yield return null;
        }
        animationCallback.Invoke(1);
    }
}

public class CustomEasingFunction<T>
{
    public delegate T Lerp(T from, T to, float progress);
    private Lerp _lerpFunction;
    private AnimationCurve _curve;
    public CustomEasingFunction(Lerp lerpFunction, AnimationCurve curve)
    {
        _lerpFunction = lerpFunction;
        _curve = curve;
    }

    public T GetValue(T start, T end, float progress)
    {
        return _lerpFunction(start, end, _curve.Evaluate(progress));
    }
}