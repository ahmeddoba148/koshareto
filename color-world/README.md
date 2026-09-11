# COLOR WORLD

Native Godot 4.4.1 implementation candidate. **Not a verified final release.**

Read [README_AR.md](README_AR.md) for the complete Arabic handoff.

The project includes explicit data for 100 connected areas, 1500 levels and 1500 composed paint groups; native UI, color/scoring, economy, helpers, save/backup, world rendering, music, and test sources.

First run `python tools/prepare_assets.py` (NumPy and DejaVu Sans required) to generate the committed deterministic recipes into game assets. Then open `project.godot` in Godot 4.4.1 Standard. Native compilation and runtime tests could not run in the authoring environment because the engine was not installed and could not be retrieved. No APK or IPA is supplied. The source must pass the included engine tests before it can be considered runnable.

```sh
python3 tools/validate_content.py
python3 tools/build.py --platform android
```

The first command audits actual data and independent mathematics. It does not execute GDScript. The second fails explicitly if native build dependencies are absent.

See `docs/IMPLEMENTATION_STATUS.md`, `docs/content_validation_report.json`, and `docs/native_build_report.json`. Do not interpret source presence or generated item counts as production art approval, measured mobile performance, or a successful build.
