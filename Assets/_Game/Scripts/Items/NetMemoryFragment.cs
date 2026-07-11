using ProjectAI.Characters;
using ProjectAI.Core;
using ProjectAI.Environments;
using ProjectAI.SOs;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace ProjectAI.Items
{
    /// <summary>
    /// 방 클리어 시 임시로 드롭되는 '기억의 파편(재화)' 아이템입니다.
    /// 플레이어와 충돌 시 로그를 출력하고 즉시 파괴(Despawn)됩니다.
    /// 차후 기획에 따라 파티 공용 재화 시스템과 연동될 예정입니다.
    /// </summary>
    public class NetMemoryFragment : NetworkBehaviour
    {


        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!IsSpawned || !GameStatics.IsServerAuthorized)
            {
                // 충돌은 클라이언트에서도 감지될 수 있으나 서버 로직이 아니면 무시합니다. (과도한 로그 방지용 생략)
                return;
            }
            
            if (!collision.CompareTag(ObjectTags.PLAYER))
            {
                return;
            }

            DungeonRewardTableSO rewardTable = GameStatics.CurrentRewardTable;
            if (rewardTable == null)
            {
                Debug.LogError("[NetMemoryFragment] GameStatics.CurrentRewardTable이 null입니다.");
                return;
            }

            Debug.Log("[NetMemoryFragment] 코어 정화 완료! 서버에서 보상 난수를 생성하고 유저들에게 UI 팝업을 지시합니다.");

            int summonRewardIndex = -1;
            int summonUpgradeRewardIndex = -1;
            int playerUpgradeRewardIndex = -1;

            if (rewardTable.SummonPool?.Count > 0)
            {
                summonRewardIndex = Random.Range(0, rewardTable.SummonPool.Count);
            }

            if (rewardTable.SummonUpgradePool?.Count > 0)
            {
                summonUpgradeRewardIndex = Random.Range(0, rewardTable.SummonUpgradePool.Count);
            }

            if (rewardTable.PlayerUpgradePool?.Count > 0)
            {
                playerUpgradeRewardIndex = Random.Range(0, rewardTable.PlayerUpgradePool.Count);
            }

            if (summonRewardIndex < 0 && summonUpgradeRewardIndex < 0 && playerUpgradeRewardIndex < 0)
            {
                Debug.LogWarning("[NetMemoryFragment] 모든 보상 풀이 비어있어 보상 UI를 띄우지 않습니다.");
                NetworkObject.Despawn(true);
                return;
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClientsList != null)
            {
                foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    if (client.PlayerObject != null && client.PlayerObject.TryGetComponent(out NetPlayerCharacter player))
                    {
                        if (player.IsSpawned)
                        {
                            player.IncrementPendingRewardCount();
                            player.ShowRewardPopupRpc(summonRewardIndex, summonUpgradeRewardIndex, playerUpgradeRewardIndex);
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("[NetMemoryFragment] NetworkManager.ConnectedClientsList를 찾을 수 없습니다.");
            }

            NetworkObject.Despawn(true);
        }
    }
}
