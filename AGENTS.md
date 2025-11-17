# Repository Guidelines

## Project Structure & Module Organization
PoC3 is a Unity 6 URP project targeting PC. Scenes live in `Assets/Scenes`, with entry workflow across `Scene1-3`, `GameOver`, and `GameClear`. Gameplay code is split into domain folders under `Assets/Scripts`: `Core` hosts the shared `StateMachine`/`IState`, `Managers` orchestrate turn, player, and enemy flow, and feature folders (`Ball`, `Board`, `Tile`, `UISystem`) isolate behaviours. Configurable data and tuning knobs sit inside `Assets/SOs` as ScriptableObjects, reusable visuals in `Assets/Prefabs` and `Assets/Shaders`. Keep any new tooling assets beside their `.meta` partners and update `Assembly-CSharp.csproj` through the Unity editor only.

## Build, Test, and Development Commands
- `Unity -projectPath "$(pwd)"` launches the project in the editor for iteration.
- `Unity -batchmode -quit -projectPath "$(pwd)" -runTests -testPlatform EditMode` executes Edit Mode tests headlessly; swap `PlayMode` for runtime checks.
- `Unity -batchmode -quit -projectPath "$(pwd)" -buildPlayer "Builds/Win64/PoC3.exe"` produces a Windows build; ensure the `Builds/Win64` directory exists.
- `dotnet format Assembly-CSharp.csproj` validates spacing and directive order before pushing.

## Coding Style & Naming Conventions
Code is C# 10 with four-space indentation, `PascalCase` types/methods/events, and `_camelCase` serialized or private fields. Follow SRP aggressively (one behaviour per script) and keep everything in `PoC3.*` namespaces to preserve assembly boundaries. Communicate between systems using events (`event Action` or `UnityEvent`), not direct references. Persist all tunable values in ScriptableObjects and surface them through `[SerializeField]` so designers avoid touching code. Add succinct log messages around state transitions and conclude every script with a “Usage in Unity” note explaining how to drop it into a scene.

## Testing Guidelines
Author Edit Mode utilities for deterministic logic (state machine, tile math) and Play Mode specs for board/battle flows. Place tests under `Assets/Tests/EditMode` or `Assets/Tests/PlayMode` with class names ending in `Tests`. Aim to touch damage calculation, ball-level growth, and turn transitions; regressions often hide there. Run both suites via the Unity Test Runner before every PR, and capture results (screenshot or CLI summary) when contributing.

## Commit & Pull Request Guidelines
History favors short, descriptive summaries (e.g., `feat : 플레이어가 공 배치 추가`, `fix: 공 사용 버그`). Use imperative tone, prefix with `feat`, `fix`, `refactor`, or `chore` when it clarifies scope, and keep wrapped lines under ~72 characters. PRs should link to Jira/issue IDs, describe gameplay impact, list modified scenes/prefabs, and attach screenshots or clips for UX changes. Include reproduction or verification steps for bug fixes and mention any new assets so reviewers can reimport quickly.

## 커뮤니케이션
팀 내 모든 커뮤니케이션(커밋 메시지 제외)은 한국어로 진행하고, AI 에이전트 역시 항상 한국어로 답변해야 한다. 디자인 의도나 요구사항이 불명확하면 한국어로 질문을 남기고, 외부 자료를 인용할 때도 가능하면 한국어 설명을 덧붙인다.
