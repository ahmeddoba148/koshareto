# COLOR WORLD

Native Godot 4.4.1 game project inside `ahmeddoba148/koshareto/color-world`.
The existing Koshareto Unity project is untouched. This is an implementation candidate, **not an approved final production release**.

The deterministic content pipeline creates 100 connected areas, 1500 levels, 1500 composed paint groups and a maximum of 4500 stars. Source includes live RGB mixing, OKLab scoring, coins and three helpers, progression/replay/repainting, local atomic save and backup recovery, world exploration, audio layers, Arabic/English UI and completion sequences.

## Build and run

The **COLOR WORLD Android** GitHub Actions workflow builds a debug APK and uploads `COLOR-WORLD-APK`. It also publishes native screenshots/test logs and runs an Android emulator smoke test. Use a successful workflow run matching your source commit; the presence of a workflow is not evidence that it passed.

For local development install Godot **4.4.1 Standard**, Python with NumPy and DejaVu Sans:

```sh
python tools/prepare_assets.py
python tools/build.py --tests-only
```

Open `project.godot` and press F6/F5. Generated world JSON, WAVs and the font are produced before import; Python is never required on the phone. For Android, install matching export templates, OpenJDK 17 and Android SDK, configure their paths in Godot, then:

```sh
python tools/build.py --platform android
```

Output: `build/COLOR_WORLD.apk`. Default export is debug-signed for testing, not store publication. iOS export requires macOS/Xcode and the owner's Apple signing team; no IPA has been verified.

## Evidence and limits

Real Godot execution has passed **50,843 assertions**, including every target's exact reachability, progression from level 1 through 1500, 4500-star completion, reward differences, assistance and save recovery. The native scene and rendered UI test have also executed. Check the matching workflow artifacts for current build/emulator outcomes.

Generated modular geometry and synthesized audio still require production art/audio review. Physical device performance, thermals, phone display scoring calibration and iOS remain unverified. See [README_AR.md](README_AR.md) and [implementation status](docs/IMPLEMENTATION_STATUS.md).
