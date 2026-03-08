Add a new character to the NGames project.

The user will provide: character name, and optionally an accent colour (R,G,B floats).

Steps to perform:
1. Add the character to `CharacterColors` in `Assets/Scripts/UI/CharacterDisplayManager.cs` using the provided name (lowercased as the key) and colour. If no colour given, pick one that doesn't clash with existing characters.
2. Create stub PNG meta files for all 6 expressions by copying the format from an existing `marcus_{expression}.png.meta` file in `Assets/Resources/Characters/`. The actual PNG files don't need to exist yet — just the .meta stubs so Unity won't show missing-asset errors when the art arrives. Name them `{key}_{expression}.png.meta` for: neutral, happy, angry, sad, surprised, speaking.
3. Print a summary of what was added and remind the user to drop the actual `{key}_{expression}.png` artwork into `Assets/Resources/Characters/`.

Do NOT create placeholder PNG files — only the .meta files. Do NOT modify any scene files.
