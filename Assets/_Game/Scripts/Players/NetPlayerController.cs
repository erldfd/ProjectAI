using UnityEngine.Assertions;
using UnityEngine;
using Unity.Netcode;
using ProjectAI.Characters;
using ProjectAI.Core.Skills;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using ProjectAI.Core;

namespace ProjectAI.Players
{
    /// <summary>
    /// 플레이어의 입력, 이동, 카메라 등의 컴포넌트를 조율하는 로컬 플레이어 전용 뇌(Brain) 컨트롤러입니다.
    /// </summary>
    public class NetPlayerController : NetworkBehaviour
    {
        private PlayerInputReader inputReader;
        private PlayerCamera playerCamera;
        private NetPlayerCharacter myCharacter;

        #region Unity Lifecycle
        private void Awake()
        {
            inputReader = GetComponentInChildren<PlayerInputReader>();
            playerCamera = GetComponentInChildren<PlayerCamera>();
            myCharacter = GetComponentInChildren<NetPlayerCharacter>();
            
            Assert.IsNotNull(inputReader, "PlayerInputReader is missing.");
            Assert.IsNotNull(playerCamera, "PlayerCamera is missing.");
            Assert.IsNotNull(myCharacter, "NetPlayerCharacter is missing.");
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner)
            {
                return;
            }

            inputReader.EnableInput();
            inputReader.OnMoveInputChanged += HandleMoveInputChanged;
            inputReader.OnInteractInputChanged += HandleInteractInputChanged;
            inputReader.OnAttackInputChanged += HandleAttackInputChanged;
            
            playerCamera.InitCamera();
            if (GameStatics.NetworkManager != null && GameStatics.NetworkManager.SceneManager != null)
            {
                GameStatics.NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoadEventCompleted;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            inputReader.OnMoveInputChanged -= HandleMoveInputChanged;
            inputReader.OnInteractInputChanged -= HandleInteractInputChanged;
            inputReader.OnAttackInputChanged -= HandleAttackInputChanged;
            inputReader.DisableInput();
            
            // 게임 종료 시 NetworkManager가 먼저 파괴될 수 있으므로, 예외(Assert) 대신 부드러운 널 체크로 이벤트 해제
            if (GameStatics.NetworkManager != null && GameStatics.NetworkManager.SceneManager != null)
            {
                GameStatics.NetworkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoadEventCompleted;
            }
        }
        #endregion

        #region Private Methods
        private void HandleMoveInputChanged(Vector2 moveInput)
        {
            myCharacter.Move(moveInput);
        }

        private void HandleInteractInputChanged(bool isInteracting)
        {
            if (isInteracting)
            {
                Debug.Log("Try Interact");
                myCharacter.TryInteract();
            }
            else
            {
                // TODO: 나중에 꾹 누르기(Hold) 게이지 캔슬 등의 로직을 여기에 추가할 수 있습니다.
            }
        }

        private void HandleAttackInputChanged(bool isAttacking)
        {
            if (!isAttacking)
            {
                return;
            }

            myCharacter.TryActivateSkill(ESkillType.BasicAttack);
        }

        private void OnSceneLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            playerCamera.InitCamera();
        }
        #endregion
    }
}
