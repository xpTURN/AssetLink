# Changelog

All notable changes to this project will be documented in this file.

## [1.0.0] - 2024-12-09

### Added

- **ObjectLink<T>**: A class for asynchronously loading assets of generic types
  - `LoadAssetAsync()`: Asynchronously load an asset
  - `Release()`: Release the asset reference
  - `GetResult()`: Direct access to the loaded asset

- **PrefabLink**: A class for asynchronously instantiating prefabs
  - `InstantiateAsync()`: Asynchronously instantiate a prefab
  - Supports position, rotation, and parent Transform parameters
  - Automatic reference release on instance destruction (`ReleaseOnDestroy` component)

- **SceneLink**: A class for asynchronously managing Addressables scenes
  - `LoadSceneAsync()`: Asynchronously load a scene (supports Single/Additive modes)
  - `UnloadSceneAsync()`: Asynchronously unload a scene
  - `GetSceneResult()`: Access the loaded SceneInstance

- **AddressableManager**: Internal asset management system
  - Reference counting based memory management
  - Safe handling when objects are destroyed before loading completes
  - AsyncHandler/Item object pooling to minimize GC
  - Automatic dangling reference cleanup via DanglingRefManager
  - `ReleaseAllDeletedObjects()`: Batch cleanup of unused references

- **Editor Support**
  - Drag-and-drop Addressables asset linking in Inspector
  - Type filtering (only assets matching ObjectLink's generic type can be selected)

- **Utilities**
  - `ObjectPool<T>`: Reusable object pool
  - `DebugLogger`: Conditional logging system
  - `StackTraceParser`: Stack trace parsing for debugging

### Dependencies

- Unity 2021.3.45f1 or higher
- Addressables 2.6.0 or higher
