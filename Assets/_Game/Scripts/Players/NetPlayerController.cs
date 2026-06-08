using UnityEngine;
using Unity.Netcode;
using ProjectAI.Core;
using ProjectAI.Movements;

namespace ProjectAI.Players
{
    /// <summary>
    /// 플레이어의 입력, 이동, 카메라 등의 컴포넌트를 조율하는 로컬 플레이어 전용 뇌(Brain) 컨트롤러입니다.
    /// </summary>
    public class NetPlayerController : NetworkBehaviour
    {
        private PlayerInputReader inputReader;
        private NetPlayerMovement playerMovement;
        private PlayerCamera playerCamera;
        private NetInteractor netInteractor;

        #region Unity Lifecycle
        private void Awake()
        {
            inputReader = GetComponentInChildren<PlayerInputReader>();
            playerMovement = GetComponentInChildren<NetPlayerMovement>();
            playerCamera = GetComponentInChildren<PlayerCamera>();
            netInteractor = GetComponentInChildren<NetInteractor>();
            
            UnityEngine.Assertions.Assert.IsNotNull(inputReader, "PlayerInputReader is missing.");
            UnityEngine.Assertions.Assert.IsNotNull(playerMovement, "NetPlayerMovement is missing.");
            UnityEngine.Assertions.Assert.IsNotNull(playerCamera, "PlayerCamera is missing.");
            UnityEngine.Assertions.Assert.IsNotNull(netInteractor, "NetInteractor is missing.");
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!base.IsOwner)
            {
                return;
            }

            inputReader.EnableInput();
            inputReader.OnMoveInputChanged += HandleMoveInputChanged;
            inputReader.OnInteractInputChanged += HandleInteractInputChanged;
            
            playerCamera.InitCamera();
            
            UnityEngine.Assertions.Assert.IsNotNull(NetworkManager.Singleton, "NetworkManager must exist.");
            UnityEngine.Assertions.Assert.IsNotNull(NetworkManager.Singleton.SceneManager, "NetworkSceneManager must exist.");
            
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadEventCompleted;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (!base.IsOwner)
            {
                return;
            }

            inputReader.OnMoveInputChanged -= HandleMoveInputChanged;
            inputReader.OnInteractInputChanged -= HandleInteractInputChanged;
            inputReader.DisableInput();
            
            UnityEngine.Assertions.Assert.IsNotNull(NetworkManager.Singleton, "NetworkManager must exist.");
            UnityEngine.Assertions.Assert.IsNotNull(NetworkManager.Singleton.SceneManager, "NetworkSceneManager must exist.");
            
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadEventCompleted;
        }
        #endregion

        #region Private Methods
        private void HandleMoveInputChanged(Vector2 moveInput)
        {
            playerMovement.SetMoveInput(moveInput);
        }

        private void HandleInteractInputChanged(bool isInteracting)
        {
            if (isInteracting)
            {
                Debug.Log("Try Interact");
                netInteractor.TryInteract();
            }
            else
            {
                // TODO: 나중에 꾹 누르기(Hold) 게이지 캔슬 등의 로직을 여기에 추가할 수 있습니다.
            }
        }

        private void OnSceneLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut)
        {
            playerCamera.InitCamera();
        }
        #endregion
    }
}
