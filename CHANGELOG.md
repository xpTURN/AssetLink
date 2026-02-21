# Changelog

## [2.0.5] - 2026-02-21

### Changed

- **Samples / Tests**: Folder and package setup changed.

## [2.0.4] - 2026-02-19

### Changed

- **AssetLinkSpawner / AssetRefSpawner**: Logic improved. Serialize first load so only one LoadAssetAsync runs; concurrent callers reuse and await the same OperationHandle.
- **SpawnAsync**: Added overloads that take a `count` argument to spawn multiple instances at once (`SpawnAsync(int count, ...)` → `GameObject[]`).
- **Tests**: Added AssetLinkSpawnerTests, AssetRefSpawnerTests test cases.

---

## [2.0.3] - 2026-02-19

### Changed

- **AddressablesTracker (Runtime)**
  - **UnityEngine.Object IsMissing check**: Treats `owner` as missing when it is a `UnityEngine.Object` that has been destroyed (`unityObj == null`).

- **AddressablesTrackerWindow (Editor)**
  - **CaptureSnapshotCoroutine improvements**: Call `Resources.UnloadUnusedAssets()` before capturing the snapshot (avoids blocking the editor).

- **README**: Added AssetLinkScene(AssetRefScene) Usage section, Memory Leak Detection (LeakScenario/DoTrack) examples, and notes.
- **Samples**: Added ExampleSimple.cs, ExampleSpawner.cs, LeakScenario.cs; updated DoTrack (DetectAndReportLeaks, DoLeakScenario, scene unload), DeclaringLinks, DoTrackEditor.

---

## [2.0.1] - 2026-02-13

### Changed

- **AddressablesTracker**
  - Reduced GC allocation when calling `ReleaseUnreferencedHandles`.

- **AssetRef**, **AssetLink**
  - Reduced GC allocation when accessing the `RuntimeKey` property.

### Fixed

- **Editor**: Renamed `AddressablesTrackeWindow` to `AddressablesTrackerWindow` (typo fix).

### Removed

- `AddressablesTrackeWindow.cs` (replaced by `AddressablesTrackerWindow.cs`).
- Sample scene `SampleScene.unity` (replaced by `AssetLinkSamples.unity`).

### Dependencies

- Unity 2023.1 or higher
- Addressables 2.8.1 or higher
- Editor Coroutines 1.0.1 or higher
- UniTask 2.5.0 or higher (optional)

---

## [2.0.0] - 2026-02-08

- All changes from 1.0.0
- Code refactoring used in AssetLink
- AssetRef added
- AssetLinkSpawner / AssetRefSpawner added
- AssetLinkSettings added
- AddressablesTracker added

### Dependencies

- Unity 2023.1 or higher
- Addressables 2.8.1 or higher
- Editor Coroutines 1.0.1 or higher
- UniTask 2.5.0 or higher (optional)

## [1.0.0] - 2024-12-09

- First release

### Dependencies

- Unity 2021.3.45f1 or higher
- Addressables 2.6.0 or higher
