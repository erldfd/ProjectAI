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
        private ProjectAI.Core.Skills.NetSkillComponent skillComponent;

        #region Unity Lifecycle
        private void Awake()
        {
            inputReader = GetComponentInChildren<PlayerInputReader>();
            playerMovement = GetComponentInChildren<NetPlayerMovement>();
            playerCamera = GetComponentInChildren<PlayerCamera>();
            netInteractor = GetComponentInChildren<NetInteractor>();
            skillComponent = GetComponentInChildren<ProjectAI.Core.Skills.NetSkillComponent>();
            
            UnityEngine.Assertions.Assert.IsNotNull(inputReader, "PlayerInputReader is missing.");
            UnityEngine.Assertions.Assert.IsNotNull(inputReader, "PlayerInputReader is missing.");
            UnityEngine.Assertions.Assert.IsNotNull(playerMovement, "NetPlayerMovement is missing.");
            UnityEngine.Assertions.Assert.IsNotNull(playerCamera, "PlayerCamera is missing.");
            UnityEngine.Assertions.Assert.IsNotNull(netInteractor, "NetInteractor is missing.");
            UnityEngine.Assertions.Assert.IsNotNull(skillComponent, "NetSkillComponent is missing.");
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
            inputReader.OnAttackInputChanged += HandleAttackInputChanged;
            
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
            inputReader.OnAttackInputChanged -= HandleAttackInputChanged;
            inputReader.DisableInput();
            
            // 게임 종료 시 NetworkManager가 먼저 파괴될 수 있으므로, 예외(Assert) 대신 부드러운 널 체크로 이벤트 해제
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadEventCompleted;
            }
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

        private void HandleAttackInputChanged(bool isAttacking)
        {
            skillComponent.SetAttackInput(isAttacking);
        }

        private void OnSceneLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut)
        {
            playerCamera.InitCamera();
        }
        #endregion
    }
}
