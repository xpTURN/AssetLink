using UnityEngine;
using Unity.Cinemachine;
using Object = UnityEngine.Object;

using xpTURN.Link;

public class Warmup : MonoBehaviour
{
    [SerializeField]
    private CinemachineVirtualCamera _virtualCamera;

    [SerializeField]
    private PrefabLink _playerPrefabLink = new PrefabLink();

    void Start()
    {
        _playerPrefabLink.InstantiateAsync(OnLoaded, Vector3.zero, Quaternion.identity);
    }

    void OnLoaded(GameObject player)
    {
        Debug.Log("Player prefab loaded");
        var playerController = player.GetComponent<StarterAssets.ThirdPersonController>();
        if (_virtualCamera)
            _virtualCamera.Follow = playerController.CinemachineCameraTarget.transform;
    }
}
