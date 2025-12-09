# AssetLink

A lightweight wrapper library for Unity Addressables system. It provides simple and safe handling of asset loading, prefab instantiation, and scene management. This is an Addressable asset reference type that can be used instead of AssetReference provided by Unity Addressables.

## Features

- AssetLink manages assets based on Addressable Name, enabling data-driven control at runtime (AssetReference uses GUID-based references, which only allows asset linking in the editor environment)
- Call `AddressableManager.ReleaseAllDeletedObjects()` to batch cleanup assets with missing Release calls
- **DEBUG Mode**: Reports a list of assets with missing Release calls, helping you identify and fix the relevant code
- **Inspector Integration**: Drag-and-drop asset linking in the editor

## Requirements

- Unity 2021.3 or higher
- Addressables 2.6.0 or higher

## Installation

### Unity Package Manager (Git URL)

1. Open Window > Package Manager
2. Click '+' button > Select "Add package from git URL..."
3. Enter the following URL:
```
https://github.com/xpTURN/AssetLink.git?path=Assets/AssetLink
```

### Manual Installation

Copy the `Assets/AssetLink` folder to your project's Packages folder.

## Usage

### ObjectLink - Asset Loading

Asynchronously loads general assets (textures, audio, ScriptableObjects, etc.).

```csharp
using xpTURN.Link;
using UnityEngine;

public class Example : MonoBehaviour
{
    [SerializeField] private ObjectLink<Texture2D> textureLink;
    
    void Start()
    {
        textureLink.LoadAssetAsync(OnTextureLoaded);
    }
    
    void OnTextureLoaded(Texture2D texture)
    {
        // Use the texture
        Debug.Log($"Loaded: {texture.name}");
    }
    
    void OnDestroy()
    {
        textureLink.Release();
    }
}
```

### PrefabLink - Prefab Instantiation

Asynchronously loads and instantiates prefabs. References are automatically released when the instance is destroyed.

```csharp
using xpTURN.Link;
using UnityEngine;

public class SpawnExample : MonoBehaviour
{
    [SerializeField] private PrefabLink prefabLink;
    
    void Start()
    {
        // Basic instantiation
        prefabLink.InstantiateAsync(OnSpawned);
        
        // With position and rotation
        prefabLink.InstantiateAsync(OnSpawned, 
            Vector3.zero, 
            Quaternion.identity, 
            transform);  // parent
    }
    
    void OnSpawned(GameObject instance)
    {
        Debug.Log($"Spawned: {instance.name}");
        // Reference is automatically released when instance is destroyed
    }
}
```

### SceneLink - Scene Loading

Asynchronously loads/unloads scenes registered with Addressables.

```csharp
using xpTURN.Link;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.ResourceProviders;

public class SceneLoadExample : MonoBehaviour
{
    [SerializeField] private SceneLink sceneLink;
    
    public void LoadScene()
    {
        sceneLink.LoadSceneAsync(OnSceneLoaded, LoadSceneMode.Additive);
    }
    
    void OnSceneLoaded(SceneInstance sceneInstance)
    {
        Debug.Log($"Scene loaded: {sceneInstance.Scene.name}");
    }
    
    public void UnloadScene()
    {
        sceneLink.UnloadSceneAsync(OnSceneUnloaded);
    }
    
    void OnSceneUnloaded(string key)
    {
        Debug.Log($"Scene unloaded: {key}");
    }
}
```

## Advanced Features

### Dynamic Key Assignment

You can dynamically assign Addressable keys at runtime.

```csharp
ObjectLink<Sprite> spriteLink = new ObjectLink<Sprite>();
spriteLink.AssignKey("Sprites/icon_01");
spriteLink.LoadAssetAsync(OnSpriteLoaded);
```

### Cleanup Unused Assets

Batch cleanup asset references associated with destroyed objects.

```csharp
AddressableManager.ReleaseAllDeletedObjects(report: true);
```

- In **DEBUG** mode, a report log is generated. Use this to identify and fix missing Release calls.
- Call `ReleaseAllDeletedObjects()` at scene transitions to remove unnecessary bundle references for any unavoidably missed assets. (Reports as "[AddressableManager] NullLink:" in Unity Console)

## Architecture

```
AssetLink (Abstract Base Class)
├── ObjectLink<T>  - General asset loading
├── PrefabLink     - Prefab instantiation
└── SceneLink      - Scene loading/unloading

AddressableManager (Internal Manager)
├── AsyncHandler      - Async operation handler
│   └── Item          - Individual reference item
├── DanglingRefManager - Dangling reference handling
└── ObjectPool        - Handler/Item pooling
```

## License

See [LICENSE.md](LICENSE.md) for details.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for details.
