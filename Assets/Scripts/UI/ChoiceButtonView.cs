using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NGames.UI
{
    /// <summary>
    /// A single choice button with staggered slide-in entrance and hover/press animations.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ChoiceButtonView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private Button          _button;

        private CanvasGroup   _cg;
        private RectTransform _rt;
        private Image         _bg;
        private Color         _normalColor;
        private Coroutine     _hoverRoutine;
        private Coroutine     _scaleRoutine;

        private void Awake()
        {
            if (_button == null) _button = GetComponent<Button>();
            _cg = gameObject.AddComponent<CanvasGroup>();
            _rt = GetComponent<RectTransform>();
            _bg = GetComponent<Image>();
            if (_bg != null) _normalColor = _bg.color;
        }

        public void Setup(int choiceIndex, string text, Action<int> onSelected)
        {
            if (_label != null) _label.text = text;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onSelected(choiceIndex));
        }

        // ── Entrance animation ────────────────────────────────────────────────
        public void AnimateIn(int staggerIndex)
        {
            _cg.alpha = 0f;
            StartCoroutine(EntranceRoutine(staggerIndex * 0.09f));
        }

        private IEnumerator EntranceRoutine(float delay)
        {
            var basePos = _rt.anchoredPosition;
            _rt.anchoredPosition = basePos + new Vector2(0f, -28f);

            yield return new WaitForSeconds(delay);

            float e = 0f, dur = 0.32f;
            var startPos = _rt.anchoredPosition;
            while (e < dur)
            {
                e += Time.deltaTime;
                float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(e / dur), 3f); // ease-out cubic
                _cg.alpha            = t;
                _rt.anchoredPosition = Vector2.Lerp(startPos, basePos, t);
                yield return null;
            }
            _cg.alpha            = 1f;
            _rt.anchoredPosition = basePos;
        }

        // ── Hover / press ─────────────────────────────────────────────────────
        public void OnPointerEnter(PointerEventData _)
        {
            if (_hoverRoutine != null) StopCoroutine(_hoverRoutine);
            _hoverRoutine = StartCoroutine(TintTo(new Color(
                Mathf.Min(_normalColor.r + 0.18f, 1f),
                Mathf.Min(_normalColor.g + 0.12f, 1f),
                Mathf.Min(_normalColor.b + 0.22f, 1f),
                _normalColor.a), 0.12f));
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
            _scaleRoutine = StartCoroutine(ScaleTo(new Vector3(1.03f, 1.03f, 1f), 0.12f));
        }

        public void OnPointerExit(PointerEventData _)
        {
            if (_hoverRoutine != null) StopCoroutine(_hoverRoutine);
            _hoverRoutine = StartCoroutine(TintTo(_normalColor, 0.18f));
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
            _scaleRoutine = StartCoroutine(ScaleTo(Vector3.one, 0.15f));
        }

        public void OnPointerDown(PointerEventData _)
        {
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
            _scaleRoutine = StartCoroutine(ScaleTo(new Vector3(0.96f, 0.96f, 1f), 0.07f));
        }

        public void OnPointerUp(PointerEventData _)
        {
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
            _scaleRoutine = StartCoroutine(ScaleTo(new Vector3(1.03f, 1.03f, 1f), 0.08f));
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private IEnumerator TintTo(Color target, float dur)
        {
            if (_bg == null) yield break;
            var start = _bg.color;
            float e = 0f;
            while (e < dur)
            {
                e += Time.deltaTime;
                _bg.color = Color.Lerp(start, target, e / dur);
                yield return null;
            }
            _bg.color = target;
        }

        private IEnumerator ScaleTo(Vector3 target, float dur)
        {
            var start = _rt.localScale;
            float e = 0f;
            while (e < dur)
            {
                e += Time.deltaTime;
                _rt.localScale = Vector3.Lerp(start, target, e / dur);
                yield return null;
            }
            _rt.localScale = target;
        }
    }
}
