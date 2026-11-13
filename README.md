# SubGen

Offline subtitle generator for Windows. Pick a video, and SubGen saves a
matching `.srt` file in the same folder, so players like VLC load it
automatically. No internet or API key needed.

## Features
- Drag a video onto the window, or click "Choose video..."
- Two speech models: Accurate (base.en) and Fast (tiny.en)
- Benchmark mode: times the first 5 minutes and shows the real-time factor
- Optional right-click menu for mp4, mkv, avi, mov, m4v and webm files
- Runs separately from your video player, so playback isn't slowed down

## Install
1. Open the **Actions** tab and open the latest successful run.
2. Download the **SubGen-Setup** artifact and unzip it.
3. Run `SubGen-Setup.exe`.

## How it works
ffmpeg extracts the audio, then whisper.cpp turns the speech into text and
writes the `.srt`. Everything runs on your own computer.

## Build
Pushing to `main` triggers the GitHub Actions workflow in
`.github/workflows/build.yml`. It builds the app, compiles whisper.cpp,
downloads ffmpeg and the models, and packages everything with Inno Setup.

## Credits
- [whisper.cpp](https://github.com/ggml-org/whisper.cpp) (MIT)
- [ffmpeg](https://ffmpeg.org) (LGPL build)
- OpenAI Whisper models
