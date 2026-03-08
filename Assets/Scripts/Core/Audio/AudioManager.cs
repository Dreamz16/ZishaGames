using System.Collections;
using NGames.Core.Events;
using UnityEngine;

namespace NGames.Core.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private AudioSource _music;
        private AudioSource _sfx;
        private Coroutine   _fadeCoroutine;

        public float MusicVolume { get; private set; } = 0.7f;
        public float SfxVolume   { get; private set; } = 1.0f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _music        = gameObject.AddComponent<AudioSource>();
            _music.loop   = true;

            _sfx          = gameObject.AddComponent<AudioSource>();
            _sfx.loop     = false;

            MusicVolume   = PlayerPrefs.GetFloat("vol_music", 0.7f);
            SfxVolume     = PlayerPrefs.GetFloat("vol_sfx",   1.0f);
            _music.volume = MusicVolume;
            _sfx.volume   = SfxVolume;
        }

        private void OnEnable()  => GameEventBus.Subscribe<AudioCueEvent>(OnAudioCue);
        private void OnDisable() => GameEventBus.Unsubscribe<AudioCueEvent>(OnAudioCue);

        private void OnAudioCue(AudioCueEvent ev)
        {
            if (ev.IsMusic) PlayMusic(ev.CueName);
            else            PlaySfx(ev.CueName);
        }

        public void PlayMusic(string cueName)
        {
            var clip = Resources.Load<AudioClip>($"Audio/Music/{cueName}");
            if (clip == null) return;
            if (_music.clip == clip && _music.isPlaying) return;
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(CrossfadeMusic(clip));
        }

        public void PlaySfx(string cueName)
        {
            var clip = Resources.Load<AudioClip>($"Audio/SFX/{cueName}");
            if (clip == null) return;
            _sfx.PlayOneShot(clip, SfxVolume);
        }

        public void SetMusicVolume(float v)
        {
            MusicVolume   = Mathf.Clamp01(v);
            _music.volume = MusicVolume;
            PlayerPrefs.SetFloat("vol_music", MusicVolume);
        }

        public void SetSfxVolume(float v)
        {
            SfxVolume   = Mathf.Clamp01(v);
            _sfx.volume = SfxVolume;
            PlayerPrefs.SetFloat("vol_sfx", SfxVolume);
        }

        public void StopMusic()
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeOut(_music, 0.5f));
        }

        private IEnumerator CrossfadeMusic(AudioClip next)
        {
            const float dur = 0.6f;
            float e = 0, startVol = _music.volume;
            while (e < dur) { e += Time.unscaledDeltaTime; _music.volume = Mathf.Lerp(startVol, 0f, e / dur); yield return null; }
            _music.Stop(); _music.clip = next; _music.Play();
            e = 0;
            while (e < dur) { e += Time.unscaledDeltaTime; _music.volume = Mathf.Lerp(0f, MusicVolume, e / dur); yield return null; }
            _music.volume = MusicVolume;
        }

        private IEnumerator FadeOut(AudioSource src, float dur)
        {
            float s = src.volume, e = 0;
            while (e < dur) { e += Time.unscaledDeltaTime; src.volume = Mathf.Lerp(s, 0f, e / dur); yield return null; }
            src.Stop(); src.volume = s;
        }
    }
}
