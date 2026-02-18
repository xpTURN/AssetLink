# Changelog

## [2.0.2] - 2026-02-18

### Changed

- **AddressablesTracker (Runtime)**
  - **UnityEngine.Object IsMissing check**: Treats `owner` as missing when it is a `UnityEngine.Object` that has been destroyed (`unityObj == null`).

- **AddressablesTrackerWindow (Editor)**
  - **CaptureSnapshotCoroutine improvements**: Capture snapshot after calling `Resources.UnloadUnusedAssets()` before taking the snapshot.

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
