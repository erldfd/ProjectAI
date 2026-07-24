using UnityEngine;
using Unity.Netcode;
using ProjectAI.Characters;
using UnityEngine.Assertions;
using ProjectAI.SOs;

namespace ProjectAI.Core.Skills.Abilities
{
    /// <summary>
    /// 소환수들의 전술 태세(자유 공격 / 방어 호위)를 전환하는 스킬 로직입니다.
    /// </summary>
    public class ToggleStanceSkillLogic : ISkillLogic
    {
        public ESkillType SkillType => ESkillType.ToggleStance;

        public void Initialize(SkillManager manager)
        {
        }

        public bool CanExecute(NetCharacter caster, BaseSkillConfig config)
        {
            if (caster.HasState(EStateTag.Silenced) || caster.HasState(EStateTag.Stunned))
            {
                return false;
            }

            Assert.IsNotNull(GameStatics.NetworkManager, "[ToggleStanceSkillLogic] NetworkManager is null.");
            if (GameStatics.NetworkManager == null)
            {
                return false;
            }

            if (GameStatics.NetworkManager.ServerTime.Time < caster.SkillComponent.GetServerActivationTime(config.SkillId) + config.BaseCooldown)
            {
                return false;
            }

            return true;
        }

        public void Execute(NetCharacter caster, BaseSkillConfig config)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[ToggleStanceSkillLogic] Execute는 서버에서만 실행되어야 합니다.");

            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            caster.SkillComponent.SetServerActivationTime(config.SkillId, GameStatics.NetworkManager.ServerTime.Time);

            int animHash = caster.SkillComponent.GetSkillAnimHash(config.SkillId);
            if (animHash != 0)
            {
                caster.SkillComponent.BroadcastPlayAnimationClientRpc(animHash, 0f);
            }
        }

        public void Action(NetCharacter caster, BaseSkillConfig config)
        {
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            if (caster is NetPlayerCharacter playerCaster)
            {
                if (playerCaster.SummonController != null)
                {
                    playerCaster.SummonController.ToggleStance();
                }
                else
                {
                    Debug.LogWarning("[ToggleStanceSkillLogic] 플레이어에게 SummonController가 없습니다.");
                }
            }
            else
            {
                Debug.LogWarning("[ToggleStanceSkillLogic] 캐스터가 NetPlayerCharacter가 아닙니다.");
            }
        }

        public void End(NetCharacter caster, BaseSkillConfig config)
        {
        }
    }
}
