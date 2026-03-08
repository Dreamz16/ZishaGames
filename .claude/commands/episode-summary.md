Summarise an NGames episode Ink script for writers and designers.

If the user provides a file path or episode name, read that file.
Otherwise list available episodes under `Assets/Ink/Episodes/` and ask which one.

Produce a structured plain-English summary with these sections:

**Overview**
- Total line count, choice count, knot count
- Characters who speak (and how many lines each)
- Scenes visited (from `# scene:` tags)

**Story Beats**
List each knot as a numbered beat: knot name → 1–2 sentence description of what happens.

**Choices**
List every choice point: beat name → the choice options → which flags or variables they affect.

**Flags & State Changes**
List every `# flag:` set and every Ink variable modified, with the knot where it happens.

**Mood Arc**
If `# expression:` tags are present, describe the emotional journey of the episode.

Keep the summary concise — aim for something a writer could read in 2 minutes to get the full picture of the episode.
