# SubGen

Offline subtitle generator for Windows. Pick a video, and SubGen saves a
matching `.srt` file in the same folder, so players like VLC load it
automatically. No internet or API key needed.

## Features
- Drag a video onto the window, or click "Choose video..."
- Three speech models: Accurate (base.en), Fast (tiny.en), and a multilingual
  option that translates foreign speech such as Korean into English
- Benchmark mode: times the first 5 minutes and shows the real-time factor
- Optional right-click menu for mp4, mkv, avi, mov, m4v and webm files
- Keeps your old `.srt` as a `.bak` copy if one already exists
- Runs separately from your video player, so playback isn't slowed down

## Install
1. Go to the [**Releases**](../../releases/latest) page.
2. Under **Assets**, download `SubGen-Setup.exe`.
3. Run it. If Windows shows "Windows protected your PC", click
   **More info**, then **Run anyway** (the installer is not signed).
4. Follow the steps. You can choose to add a desktop shortcut and the
   right-click menu.

Requires Windows 10 or 11 (64-bit). Everything is bundled, so nothing else
needs to be installed.

## Use
1. Open SubGen and choose or drag in a video.
2. Pick a model.
3. Click **Generate subtitles**. The `.srt` is saved next to the video.

Tick **Benchmark only** to test your PC's speed without saving anything.

## How it works
ffmpeg extracts the audio, then whisper.cpp turns the speech into text and
writes the `.srt`. Everything runs on your own computer.

## Build
Pushing to `main` triggers the GitHub Actions workflow in
`.github/workflows/build.yml`. It builds the app, compiles whisper.cpp,
downloads ffmpeg and the models, and packages everything with Inno Setup.
The installer appears as a build artifact on each run.

## License
MIT. See [LICENSE](LICENSE).

## Credits
- [whisper.cpp](https://github.com/ggml-org/whisper.cpp) (MIT)
- [ffmpeg](https://ffmpeg.org) (LGPL build)
- OpenAI Whisper models
