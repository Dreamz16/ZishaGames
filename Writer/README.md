# NGames — Writer's Guide

Write your episodes in `.twee` files here in this `Writer/` folder.
The pipeline watches for changes and automatically converts them to Ink
so Unity can recompile and hot-reload.

---

## Quickstart

1. Copy `TEMPLATE.twee` to `MyEpisode.twee` (filename = episode ID)
2. Edit in [Twine](https://twinery.org/) or any text editor
3. Run the pipeline once (see below) and keep it running while you write

The pipeline converts your `.twee` → `Assets/Ink/Episodes/MyEpisode/MyEpisode.ink`
Unity recompiles `.ink.json` automatically on next editor focus.

---

## Running the Pipeline

```bash
cd Tools/pipeline
npm install          # first time only
npm start            # watches Writer/ for .twee changes
```

---

## Twee Format

### Passage Headers

```twee
:: PassageName
```

Each passage becomes an Ink knot (`=== passage_name ===`).

### NGames Tags (put at the top of any passage, before dialogue)

```twee
scene: forest_path       # changes the background image
mood: tense              # affects lighting, particles, character tint
music: fas_et_nefas_building   # plays a music track
audio: wolf_howl         # plays a one-shot sound effect
flag: met_fang=true      # sets a game flag
```

### Dialogue

Use `Speaker: text` format. The speaker name controls TTS voice and portrait.

```twee
Lawrence: "We need to move."
Fang: "Agreed."
```

### Stage Directions (emotion / delivery cue)

Put them in parentheses at the start of dialogue — they drive the TTS voice emotion:

```twee
Lawrence: (quietly, eyes scanning the dark) "Stay close."
Fang: (with a low growl) "Something's wrong."
```

### Choices

Use Twee 3 arrow syntax:

```twee
[[Choice label->PassageName]]
[[Another choice->OtherPassage]]
```

Multiple links on separate lines become a choice menu.
A single link becomes a plain divert (no choice shown).

SugarCube pipe syntax also works: `[[Choice label|PassageName]]`

### Variables

```twee
<<set $bond_fang to 0>>        # declare/set
<<set $bond_fang += 1>>        # increment
<<set $choice_made to true>>   # boolean
```

### Conditional text (single-line)

```twee
<<if $bond_fang gt 2>>Fang smiles warmly.<<endif>>
```

Multi-line conditionals need to be written in Ink directly (see `Assets/Ink/`).

---

## Available Moods

`neutral` `tense` `action` `sad` `mysterious` `romantic` `dark` `joyful`

## Available Characters

| Speaker name | Voice |
|---|---|
| Lawrence | Korean male (Yuna voice, lowered) |
| Fang | Female werewolf (Zoe voice, husky) |
| Marcus | Man-bear (Fred voice, very deep) |
| Tiberius | Bartender (Tom voice, warm) |
| Stone | Ancient entity (Fred voice, sub-bass) |
| Ishani | Default female |
| Kira | Default female |
| Almas | Default female |
| Batu | Default male |
| Jiwon | Default male |
| Nadia | Default female |

---

## Tips

- Passage names become Ink knot names — keep them short and descriptive
- Use `StoryVars` passage to declare all your variables at the top
- The `Start` passage is the episode entry point
- End a passage with `-> END` if it has no outgoing links
- If you need complex Ink features (tunnels, threads, stitches), write them
  directly in the `.ink` file and leave a `// TODO` comment in the `.twee`
