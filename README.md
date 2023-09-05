# Usage
1. Download the latest release [here](https://github.com/andtechstudios/spectrum/releases). Extract the download if necessary.
2. Navigate to the downloaded folder. You should see the following:
* `Build`
* `TemplateData`
* `audio.mp3`
* `config.json`
* `index.html`

3. Replace `audio.mp3` with *your* audio file.
4. Customize your tune's title and artist in `config.json`.
5. Create a `.zip` containing the updated files.
6. [Create a new game](https://itch.io/game/new) on itch.io.
7. Upload the `.zip` from step 5.

# Compatibility
|  | Windows | macOS | Linux | Android | iOS |
| --- | --- | --- | --- | --- | --- |
| Audio Clip (hardcoded) | ✅ | ✅ | ✅ | ✅ | ✅ |
| Load Audio from URL | ✅ | ✅ | ✅ | ✅ | Partial* |

> Due to CORS, some devices may reject loading audio files from external URLs.
