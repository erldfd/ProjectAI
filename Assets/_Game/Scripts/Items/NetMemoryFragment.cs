using UnityEngine;
using Unity.Netcode;
using ProjectAI.Core;
using ProjectAI.Environments;

namespace ProjectAI.Items
{
    /// <summary>
    /// 방 클리어 시 임시로 드롭되는 '기억의 파편(재화)' 아이템입니다.
    /// 플레이어와 충돌 시 로그를 출력하고 즉시 파괴(Despawn)됩니다.
    /// 차후 기획에 따라 파티 공용 재화 시스템과 연동될 예정입니다.
    /// </summary>
    public class NetMemoryFragment : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!IsServer)
            {
                return;
            }
            
            if (collision.CompareTag(ObjectTags.PLAYER))
            {
                Debug.Log("[MemoryFragment] 파티 공용 재화(기억의 파편) 획득! (임시 로직)");
                NetworkObject.Despawn(true);
            }
        }
    }
}
