# Repository Guidelines

## Project Structure & Module Organization
- Unity 2022.3.62f3 project; open the root folder in Unity Hub with that editor version to avoid upgrade prompts.
- `Assets/` holds gameplay code and content: controllers such as `CityBuilderController.cs`, rail/road logic (`RailMapGenerator.cs`, `Track.cs`, `RoadSystem/`), UI, and scenes (see `Assets/Scenes/` for play targets like `Train Test.unity` and `Road Rebuild Test.unity`).
- `Packages/manifest.json` defines engine packages (TextMeshPro, Newtonsoft JSON). `ProjectSettings/` captures editor and platform settings—commit edits so teammates share them.
- `Library/` and `Temp/` are editor-generated; keep them untracked. Respect `.meta` files—they link assets and GUIDs.

## Build, Test, and Development Commands
- Open in Unity Hub → select Unity 2022.3.62f3 → load this folder, then press Play to iterate.
- Quick launch from terminal on macOS: `open -a "Unity Hub" .`.
- To run headless checks (no custom build script yet), you can smoke-test imports with:  
  `Unity -batchmode -projectPath "$(pwd)" -quit -logFile unity.log`  
  (outputs editor import/logs to `unity.log`; fails fast on missing assets).
- Builds are created via Unity’s Build Settings (File → Build Settings). Place any future automation under `Assets/Editor/` and gate it behind `-batchmode` entry points.

## Coding Style & Naming Conventions
- C# with 4-space indentation; one public class per file named to match the filename.
- PascalCase for classes/methods, camelCase for locals/fields; prefer `[SerializeField] private` over public fields when exposing to the Inspector.
- Keep MonoBehaviour responsibilities narrow (e.g., input, camera follow, rail generation in their own scripts). Use `nameof(...)` for event names and `TryGetComponent` when fetching dependencies.

## Testing Guidelines
- No automated test suite is present; rely on Play Mode in scenes under `Assets/Scenes/`.
- If adding tests, place Edit Mode tests under `Assets/Tests/EditMode/` and Play Mode tests under `Assets/Tests/PlayMode/` using Unity Test Framework/NUnit. Name files `*Tests.cs`.
- For CI-style runs, use `Unity -batchmode -projectPath "$(pwd)" -runTests -testResults TestResults.xml -quit`.

## Commit & Pull Request Guidelines
- Use concise, imperative commit subjects (e.g., `Add track switching cooldown`, `Fix camera pan bounds`); include a short body when the change is non-trivial.
- PRs should summarize the gameplay impact, list test steps (scene name + actions), and attach relevant screenshots or clips for UI/visual changes.
- Link issues or tasks when applicable and call out known follow-ups to keep scope clear.

## Security & Configuration Tips
- Do not store secrets in the repo; prefer environment variables or Unity’s Cloud secrets if introduced later.
- Before committing, ensure generated logs and caches (`Library/`, `Temp/`, build outputs) remain ignored; keep `.meta` files and `ProjectSettings/` changes so asset references stay stable.
