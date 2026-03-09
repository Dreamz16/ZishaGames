mergeInto(LibraryManager.library, {
    JS_SpeakWebGL: function(textPtr, rate, pitch) {
        if (!window.speechSynthesis) return;
        window.speechSynthesis.cancel();
        var text = UTF8ToString(textPtr);
        var utt  = new SpeechSynthesisUtterance(text);
        // rate: wpm ~80–300 → speechSynthesis.rate 0.5–2.0
        utt.rate  = Math.max(0.5, Math.min(2.0, rate / 150.0));
        // pitch: 0.40–1.60 VoiceConfig range → Web Speech API 0.0–2.0
        utt.pitch = Math.max(0.0, Math.min(2.0, pitch));
        window.speechSynthesis.speak(utt);
    },

    JS_StopWebGLSpeech: function() {
        if (window.speechSynthesis) window.speechSynthesis.cancel();
    }
});
