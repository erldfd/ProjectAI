using UnityEngine.Assertions;
using UnityEngine;
using Unity.Netcode;
using ProjectAI.Characters;
using ProjectAI.Core.Skills;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using ProjectAI.Core;
using ProjectAI.SOs;

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
            
            myCharacter.Events.OnDeathTriggered += HandleDeathTriggered;

            if (!IsOwner)
            {
                Debug.Log($"[NetPlayerController] 로컬 플레이어가 아니므로 초기화 생략. (ID: {NetworkObjectId})");
                return;
            }

            inputReader.EnableInput();
            inputReader.OnMoveInputChanged += HandleMoveInputChanged;
            inputReader.OnInteractInputChanged += HandleInteractInputChanged;
            inputReader.OnAttackInputChanged += HandleAttackInputChanged;
            inputReader.OnSkill1InputChanged += HandleSkill1InputChanged;
            inputReader.OnSkill2InputChanged += HandleSkill2InputChanged;
            inputReader.OnSkill3InputChanged += HandleSkill3InputChanged;
            
            playerCamera.InitCamera();
            if (GameStatics.NetworkManager != null && GameStatics.NetworkManager.SceneManager != null)
            {
                GameStatics.NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoadEventCompleted;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            myCharacter.Events.OnDeathTriggered -= HandleDeathTriggered;

            if (IsOwner)
            {
                inputReader.OnMoveInputChanged -= HandleMoveInputChanged;
                inputReader.OnInteractInputChanged -= HandleInteractInputChanged;
                inputReader.OnAttackInputChanged -= HandleAttackInputChanged;
                inputReader.OnSkill1InputChanged -= HandleSkill1InputChanged;
                inputReader.OnSkill2InputChanged -= HandleSkill2InputChanged;
                inputReader.OnSkill3InputChanged -= HandleSkill3InputChanged;
                inputReader.DisableInput();
            }
            
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

            if (myCharacter.SkillComponent != null && myCharacter.SkillComponent.OwnedSkills.Count > 0)
            {
                BaseSkillConfig skillToUse = myCharacter.SkillComponent.OwnedSkills[0];
                if (skillToUse != null)
                {
                    myCharacter.TryActivateSkill(skillToUse.SkillId);
                }
            }
        }

        private void HandleSkill1InputChanged(bool isSkill1Pressed)
        {
            if (!isSkill1Pressed)
            {
                return;
            }

            if (myCharacter.SkillComponent != null && myCharacter.SkillComponent.OwnedSkills.Count > 1)
            {
                BaseSkillConfig skillToUse = myCharacter.SkillComponent.OwnedSkills[1];
                if (skillToUse != null)
                {
                    myCharacter.TryActivateSkill(skillToUse.SkillId);
                }
            }
        }

        private void HandleSkill2InputChanged(bool isSkill2Pressed)
        {
            if (!isSkill2Pressed)
            {
                return;
            }

            if (myCharacter.SkillComponent != null && myCharacter.SkillComponent.OwnedSkills.Count > 2)
            {
                BaseSkillConfig skillToUse = myCharacter.SkillComponent.OwnedSkills[2];
                if (skillToUse != null)
                {
                    myCharacter.TryActivateSkill(skillToUse.SkillId);
                }
            }
        }

        private void HandleSkill3InputChanged(bool isSkill3Pressed)
        {
            if (!isSkill3Pressed)
            {
                return;
            }

            if (myCharacter.SkillComponent != null && myCharacter.SkillComponent.OwnedSkills.Count > 3)
            {
                BaseSkillConfig skillToUse = myCharacter.SkillComponent.OwnedSkills[3];
                if (skillToUse != null)
                {
                    myCharacter.TryActivateSkill(skillToUse.SkillId);
                }
            }
        }

        private void OnSceneLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            playerCamera.InitCamera();
        }

        private void HandleDeathTriggered()
        {
            if (!IsOwner)
            {
                Debug.Log($"[NetPlayerController] 로컬 플레이어가 아니므로 사망 시 입력 차단 무시. (ID: {NetworkObjectId})");
                return;
            }

            inputReader.DisableInput();
        }
        #endregion
    }
}
