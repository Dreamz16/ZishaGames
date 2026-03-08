// ────────────────────────────────────────────────────────────────────────────
// Episode 001 — "The Signal"
// Author:  [Your Name]
// Release: Week 1
//
// Synopsis: A late-night radio operator receives a distress signal from a
//           frequency that shouldn't exist. The first encounter with Alex.
// ────────────────────────────────────────────────────────────────────────────

INCLUDE ../../Shared/globals.ink
INCLUDE ../../Shared/functions.ink

// ── Episode-local variables ────────────────────────────────────────────────
VAR listened_to_full_signal = false
VAR gave_name_to_alex       = false

// External function declarations (implemented in NarrativeManager.cs)
EXTERNAL get_player_name()
EXTERNAL has_completed_episode(episode_id)
EXTERNAL get_flag(flag_name)
EXTERNAL get_counter(counter_name)
EXTERNAL increment_counter(counter_name)

// ────────────────────────────────────────────────────────────────────────────
-> ep01_intro

=== ep01_intro ===
# music: ep01_ambient_static
# scene: radio_room
The clock on the wall reads 2:47 AM.

The room smells of stale coffee and warm electronics. Your shift shouldn't have started for another hour, but sleep stopped coming three weeks ago — around the same time the letters started.
# speaker: Narrator
You scan the dial out of habit. Static. White noise. The comfortable meaninglessness of dead air.

Then — something.

A pattern buried in the hiss. Too regular to be random. Too fragile to be a broadcast.
# audio: ep01_signal_burst
*[Listen carefully.]*
    ~ listened_to_full_signal = true
    -> ep01_signal_close
*[Turn the dial away.]*
    -> ep01_signal_ignore

=== ep01_signal_close ===
# speaker: Narrator
You lean in. The signal pulses — three short, three long, three short — then dissolves back into static.

Morse. Someone is out there, broadcasting on a frequency that, according to every chart you own, doesn't exist.
-> ep01_signal_response

=== ep01_signal_ignore ===
# speaker: Narrator
You reach for the dial.

But your hand doesn't move.

Something keeps you exactly where you are. The signal pulses — three short, three long, three short — before fading.
~ listened_to_full_signal = true
-> ep01_signal_response

=== ep01_signal_response ===
# speaker: Narrator
SOS.

You grab your log and write down the frequency: 156.925 MHz. The maritime emergency band — except you're four hundred kilometres from the nearest ocean.
find_clue("ghost_frequency")

Before you can reach for the transmitter, a voice cuts through.
# speaker: Alex
# audio: ep01_alex_voice
"Hello? Is someone there? Please — I don't have much time."

The voice is young. Frightened. Real.
# speaker: Narrator
What do you do?

*[Respond immediately.]*
    -> ep01_respond_fast
*[Record the signal first.]*
    -> ep01_respond_careful
*[Stay silent and listen.]*
    -> ep01_respond_silent

=== ep01_respond_fast ===
~ track_choice()
~ change_rapport(rapport_with_alex, 1)
# speaker: {get_player_name()}
"I'm here. I can hear you. What's happening?"
-> ep01_alex_replies

=== ep01_respond_careful ===
~ track_choice()
You hit record on the old tape deck before picking up the transmitter.
# speaker: {get_player_name()}
"Signal received. Identifying myself — I'm at the Harrow Point relay station. Who is this?"
-> ep01_alex_replies

=== ep01_respond_silent ===
~ track_choice()
~ change_rapport(rapport_with_alex, -1)
# speaker: Narrator
You press record and say nothing. The signal is evidence. Witnesses make mistakes; recordings don't.

Thirty seconds pass. Then:
# speaker: Alex
"I know someone is there. The frequency doesn't allow for echo. Please."
# speaker: Narrator
You relent.
# speaker: {get_player_name()}
"...I'm here."
-> ep01_alex_replies

=== ep01_alex_replies ===
# speaker: Alex
"Thank God. Listen — my name is Alex. Alex Voss. I'm sending this from— "

Static swallows the rest of the sentence. When the voice returns, it has changed. Colder. Faster.
# speaker: Alex
"They're going to find this channel soon. I need you to write down what I'm about to tell you. Every word. Can you do that?"

*[I'm ready. Go ahead.]* -> ep01_alex_message_yes
*[Who are 'they'? I need context first.] -> ep01_alex_message_who
*[I need your full name and location before I proceed.] -> ep01_alex_message_formal

=== ep01_alex_message_yes ===
~ gave_name_to_alex = true
~ change_rapport(rapport_with_alex, 1)
# speaker: {get_player_name()}
"I'm ready."
-> ep01_alex_transmits

=== ep01_alex_message_who ===
~ track_choice()
# speaker: {get_player_name()}
"Before I write anything down — who are 'they'?"

A pause. Long enough to feel deliberate.
# speaker: Alex
"If I tell you that now, you'll think I'm unstable. Write first. Judge later. Please."
-> ep01_alex_transmits

=== ep01_alex_message_formal ===
~ track_choice()
~ change_rapport(rapport_with_alex, -1)
# speaker: {get_player_name()}
"Protocol requires full identification before I can acknowledge an emergency signal."
# speaker: Alex
"Protocol." A short, humourless sound. "All right. Alex Voss. Location — I genuinely cannot tell you that safely. Now. Please."
-> ep01_alex_transmits

=== ep01_alex_transmits ===
# speaker: Alex
"The Meridian Project is not what it claims to be. The data they published — the trial results — they've been altered. I have the original files. If something happens to me, look for the name 'Halcyon' in the Voss Institute records. That's where the truth is buried."

The channel erupts with interference.
# audio: ep01_signal_cutout
# speaker: Alex
"I'm almost out of time. One last thing — {get_player_name()}, is it? Don't trust anyone who mentions the Lighthouse Protocol first. Anyone. Not even—"

Dead air.
# speaker: Narrator
The frequency goes silent.

You sit alone in the pre-dawn dark with a notebook full of words that feel like a lit fuse.
# flag: met_the_stranger = true

-> ep01_ending

=== ep01_ending ===
# music: ep01_outro_theme
# speaker: Narrator
Outside, the first grey light of morning is beginning to separate the sky from the fields.

You look at what you've written.

{ listened_to_full_signal:
    - true: You caught every word of it — the signal, the message, the name. Whatever comes next, you have a head start.
    - false: Most of it, anyway. There's a gap in the middle that nags at you.
}

{ rapport_with_alex >= 1:
    The voice felt real to you. Frightened, yes — but truthful. That counts for something.
}
{ rapport_with_alex < 0:
    You can't shake the feeling you handled that poorly. If Alex Voss is genuine, you may have cost yourself an ally.
}

The name on the page: Halcyon.

You don't know yet whether finding that name will save you or destroy you.

But you're going to look.

-> ep01_end_card

=== ep01_end_card ===
# scene: black_title_card
// The episode select screen will display the "Next Episode" prompt.
-> END
