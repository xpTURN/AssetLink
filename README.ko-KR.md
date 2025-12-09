# AssetLink

Unity Addressables 시스템을 위한 경량 래퍼 라이브러리입니다. 에셋 로딩, 프리팹 인스턴스화, 씬 관리를 간편하고 안전하게 처리할 수 있습니다. Unity Addressables 시스템에서 제공하는 AssetReference 대신 사용할 수 있는 Addressable 에셋 참조 타입입니다.

## 특징

- AssetLink는 Addressable Name 기반으로 관리하므로 런타임에 데이터 기반 제어가 가능 (AssetReference는 GUID 기반으로 에셋을 참조하여 에디터 환경에서만 에셋 연결 가능)
- `AddressableManager.ReleaseAllDeletedObjects()`를 호출하면 Release가 누락된 에셋을 일괄 정리
- **DEBUG 모드**: Release가 누락된 에셋 목록을 리포트 해주므로 해당 위치를 파악하여 코드를 보완할 수 있도록 유도
- **Inspector 통합**: 에디터에서 에셋을 드래그앤드롭으로 연결 가능

## 요구 사항

- Unity 2021.3 이상
- Addressables 2.6.0 이상

## 설치

### Unity Package Manager (Git URL)

1. Window > Package Manager 열기
2. '+' 버튼 클릭 > "Add package from git URL..." 선택
3. 다음 URL 입력:
```
https://github.com/xpTURN/AssetLink.git?path=Assets/AssetLink
```

### 수동 설치

`Assets/AssetLink` 폴더를 프로젝트의 Packages 폴더에 복사합니다.

## 사용법

### ObjectLink - 에셋 로딩

일반 에셋(텍스처, 오디오, ScriptableObject 등)을 비동기로 로드합니다.

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
        // 텍스처 사용
        Debug.Log($"Loaded: {texture.name}");
    }
    
    void OnDestroy()
    {
        textureLink.Release();
    }
}
```

### PrefabLink - 프리팹 인스턴스화

프리팹을 비동기로 로드하고 인스턴스화합니다. 인스턴스가 파괴되면 자동으로 참조가 해제됩니다.

```csharp
using xpTURN.Link;
using UnityEngine;

public class SpawnExample : MonoBehaviour
{
    [SerializeField] private PrefabLink prefabLink;
    
    void Start()
    {
        // 기본 인스턴스화
        prefabLink.InstantiateAsync(OnSpawned);
        
        // 위치와 회전 지정
        prefabLink.InstantiateAsync(OnSpawned, 
            Vector3.zero, 
            Quaternion.identity, 
            transform);  // parent
    }
    
    void OnSpawned(GameObject instance)
    {
        Debug.Log($"Spawned: {instance.name}");
        // 인스턴스가 파괴되면 자동으로 참조 해제됨
    }
}
```

### SceneLink - 씬 로딩

Addressables로 등록된 씬을 비동기로 로드/언로드합니다.

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

## 고급 기능

### 동적 키 할당

런타임에 Addressable 키를 동적으로 할당할 수 있습니다.

```csharp
ObjectLink<Sprite> spriteLink = new ObjectLink<Sprite>();
spriteLink.AssignKey("Sprites/icon_01");
spriteLink.LoadAssetAsync(OnSpriteLoaded);
```

### 미사용 에셋 정리

파괴된 오브젝트와 연결된 에셋 참조를 일괄 정리합니다.

```csharp
AddressableManager.ReleaseAllDeletedObjects(report: true);
```

- **DEBUG** 모드에서 실행된 경우 리포트 로그가 생성됩니다. 이를 참고하여 누락된 Release 항목을 보완하시면 됩니다.
- 부득이하게 누락된 에셋 정리를 위해 씬 전환 시점 등에서 `ReleaseAllDeletedObjects()`를 호출하면 불필요한 번들 참조를 제거할 수 있습니다. (Unity Console 창에 "[AddressableManager] NullLink:"으로 리포트)

## 아키텍처

```
AssetLink (추상 기본 클래스)
├── ObjectLink<T>  - 일반 에셋 로딩
├── PrefabLink     - 프리팹 인스턴스화
└── SceneLink      - 씬 로딩/언로드

AddressableManager (내부 관리자)
├── AsyncHandler      - 비동기 작업 핸들러
│   └── Item          - 개별 참조 아이템
├── DanglingRefManager - 댕글링 참조 처리
└── ObjectPool        - 핸들러/아이템 풀링
```

## 라이선스

[LICENSE.md](LICENSE.md) 파일을 참조하세요.

## 변경 이력

[CHANGELOG.md](CHANGELOG.md) 파일을 참조하세요.
