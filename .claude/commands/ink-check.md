Validate an Ink script against NGames conventions.

If the user provides a file path or episode name, check that specific file.
Otherwise check all .ink files under `Assets/Ink/`.

For each file, report:

1. **Speaker tags** — every `# speaker: NAME` where NAME (lowercased) is NOT a key in the `CharacterColors` dictionary in `CharacterDisplayManager.cs`. List unknown speakers.

2. **Expression tags** — every `# expression: VALUE` where VALUE is not one of: neutral, happy, angry, sad, surprised, speaking.

3. **Missing scene tags** — knots (lines starting with `===`) that contain dialogue but no `# scene:` tag anywhere in that knot.

4. **Unpaired flags** — `# flag: NAME` set to true that is never read back via `get_flag("NAME")` anywhere in any .ink file (potential unused flags).

5. **Empty choices** — choice branches (`*` or `+`) with no dialogue or only whitespace.

Output a clean report grouped by file, with line numbers. If everything passes, say so clearly.
