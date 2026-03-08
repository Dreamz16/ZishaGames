using System.Collections;
using NGames.Core.Events;
using UnityEngine;

namespace NGames.UI
{
    /// <summary>
    /// Since the current characters are 2D Sprites, this script procedurally animates 
    /// them (breathing and talking) by manipulating scale and position dynamically.
    /// When you replace these with actual rigged 3D models (.fbx), you can replace 
    /// this script with an Animator component and BlendShapes for lip-sync!
    /// </summary>
    public class ProceduralCharacterAnimator : MonoBehaviour
    {
        private Vector3 _originalScale;
        private Vector3 _originalPos;
        
        private Coroutine _talkingCoroutine;
        private bool _isMyTurnToSpeak;
        private bool _isTalking;

        private void Start()
        {
            _originalScale = transform.localScale;
            _originalPos = transform.localPosition;
            StartCoroutine(BreathingRoutine());
        }

        private void OnEnable()
        {
            GameEventBus.Subscribe<SpeakerChangedEvent>(OnSpeakerChanged);
            GameEventBus.Subscribe<StoryLineReadEvent>(OnLineRead);
            GameEventBus.Subscribe<ChoicePresentedEvent>(OnChoicePresented);
        }

        private void OnDisable()
        {
            GameEventBus.Unsubscribe<SpeakerChangedEvent>(OnSpeakerChanged);
            GameEventBus.Unsubscribe<StoryLineReadEvent>(OnLineRead);
            GameEventBus.Unsubscribe<ChoicePresentedEvent>(OnChoicePresented);
        }

        private void OnSpeakerChanged(SpeakerChangedEvent ev)
        {
            if (string.IsNullOrEmpty(ev.SpeakerName))
            {
                _isMyTurnToSpeak = false;
                return;
            }

            // Check if this prefab's name matches the current speaker
            _isMyTurnToSpeak = ev.SpeakerName.Equals(gameObject.name, System.StringComparison.OrdinalIgnoreCase);
        }

        private void OnLineRead(StoryLineReadEvent ev)
        {
            if (_isMyTurnToSpeak)
            {
                if (_talkingCoroutine != null) StopCoroutine(_talkingCoroutine);
                
                // Estimate speaking duration based on text length (approx 3 words per second)
                int words = ev.Text.Split(new[] { ' ', '\n' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
                float duration = Mathf.Clamp(words / 3.0f, 1.5f, 5.0f);
                
                _talkingCoroutine = StartCoroutine(TalkingRoutine(duration));
            }
        }

        private void OnChoicePresented(ChoicePresentedEvent ev)
        {
            _isTalking = false;
        }

        private IEnumerator BreathingRoutine()
        {
            while (true)
            {
                if (!_isTalking)
                {
                    // Gentle breathing when idle
                    float t = Mathf.Sin(Time.time * 2f);
                    float scaleY = Mathf.Lerp(_originalScale.y * 0.99f, _originalScale.y * 1.01f, (t + 1) / 2f);
                    float scaleX = Mathf.Lerp(_originalScale.x * 1.005f, _originalScale.x * 0.995f, (t + 1) / 2f);
                    
                    transform.localScale = new Vector3(scaleX, scaleY, _originalScale.z);
                    transform.localPosition = new Vector3(_originalPos.x, _originalPos.y + (t * 0.05f), _originalPos.z);
                }
                yield return null;
            }
        }

        private IEnumerator TalkingRoutine(float duration)
        {
            _isTalking = true;
            float elapsed = 0f;

            while (elapsed < duration && _isTalking)
            {
                elapsed += Time.deltaTime;

                // Fast bouncy speaking animation
                float t = Mathf.PingPong(Time.time * 16f, 1f);
                float scaleY = Mathf.Lerp(_originalScale.y * 0.96f, _originalScale.y * 1.05f, t);
                float scaleX = Mathf.Lerp(_originalScale.x * 1.02f, _originalScale.x * 0.95f, t);
                
                transform.localScale = new Vector3(scaleX, scaleY, _originalScale.z);
                transform.localPosition = new Vector3(_originalPos.x, _originalPos.y + (t * 0.1f), _originalPos.z);
                
                // Reset slightly randomly to create "syllable" bounces
                if (Random.value > 0.9f)
                {
                    yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
                    elapsed += 0.1f;
                }
                
                yield return null;
            }

            _isTalking = false;
            
            // Return to normal before resuming idle breathing
            float returnTime = 0.2f;
            float returnElapsed = 0f;
            Vector3 startScale = transform.localScale;
            Vector3 startPos = transform.localPosition;
            
            while (returnElapsed < returnTime)
            {
                returnElapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(startScale, _originalScale, returnElapsed / returnTime);
                transform.localPosition = Vector3.Lerp(startPos, _originalPos, returnElapsed / returnTime);
                yield return null;
            }
        }
    }
}
