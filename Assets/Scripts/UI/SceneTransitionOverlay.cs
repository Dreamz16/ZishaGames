using System.Collections;
using NGames.Core.Events;
using UnityEngine;
using UnityEngine.UI;

namespace NGames.UI
{
    /// <summary>
    /// Full-screen black overlay that fades in on every SceneTransitionEvent,
    /// holds briefly, then fades out — masking background swaps for a clean cut.
    /// Self-bootstraps; no scene wiring required.
    /// </summary>
    public class SceneTransitionOverlay : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<SceneTransitionOverlay>() != null) return;
            var go = new GameObject("[SceneTransitionOverlay]");
            DontDestroyOnLoad(go);
            go.AddComponent<SceneTransitionOverlay>();
        }

        private CanvasGroup _group;
        private Coroutine   _routine;

        private void Awake()
        {
            var cgo    = new GameObject("TransitionCanvas");
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 98;
            cgo.AddComponent<CanvasScaler>();

            var panel = new GameObject("Overlay");
            panel.transform.SetParent(cgo.transform, false);
            var img = panel.AddComponent<Image>();
            img.color         = Color.black;
            img.raycastTarget = false;
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            _group       = panel.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
        }

        private void OnEnable()  => GameEventBus.Subscribe<SceneTransitionEvent>(OnScene);
        private void OnDisable() => GameEventBus.Unsubscribe<SceneTransitionEvent>(OnScene);

        private void OnScene(SceneTransitionEvent _)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Transition());
        }

        private IEnumerator Transition()
        {
            // Snap to black
            float e = 0f, dur = 0.28f;
            while (e < dur)
            {
                e += Time.deltaTime;
                _group.alpha = Mathf.SmoothStep(0f, 1f, e / dur);
                yield return null;
            }
            _group.alpha = 1f;

            yield return new WaitForSeconds(0.12f);

            // Slow reveal
            e = 0f; dur = 0.85f;
            while (e < dur)
            {
                e += Time.deltaTime;
                _group.alpha = Mathf.SmoothStep(1f, 0f, e / dur);
                yield return null;
            }
            _group.alpha = 0f;
        }
    }
}
