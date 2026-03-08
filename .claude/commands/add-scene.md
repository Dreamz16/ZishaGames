Add a new scene location to the NGames project.

The user will provide: a scene key (snake_case) and a mood/description.

Steps to perform:
1. Add a colour pair entry to the `Palette` dictionary in `Assets/Scripts/UI/SceneBackgroundController.cs`. Choose top/bottom gradient colours that match the mood. Dark, saturated palettes work best.

2. Add an atmosphere entry to the `SceneAtmosphere` array in `Assets/Scripts/UI/AmbientEffectsController.cs`. Choose a mote colour, count (8–40), and drift speed (5–40) that suit the scene mood.

3. Print the Ink tag to use:
   ```
   # scene: SCENE_KEY
   ```
   And remind the user they can optionally drop a background image at:
   `Assets/Resources/Backgrounds/{scene_key}.png`
   — if present it will be used instead of the gradient.

Do NOT modify any scene files or create any PNG files.
