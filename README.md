![Movie_012](https://github.com/andtechstudios/spectrum/assets/48300131/68140c77-61e6-46f8-8366-280f5bb372e4)

A WebGL audio visualizer. Just drop in your audio file, and Spectrum will do the rest.

# Usage
## Deploy Spectrum to itch.io
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

# Credits
* [SimpleSpectrum](https://assetstore.unity.com/packages/tools/audio/simplespectrum-free-audio-spectrum-generator-webgl-85294)
