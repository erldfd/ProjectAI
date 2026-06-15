using UnityEngine;
using UnityEngine.Assertions;
using Unity.Netcode;
using ProjectAI.Core;

namespace ProjectAI.Characters.Summons
{
    /// <summary>
    /// 소환수의 생명주기(지속시간)를 관리하고 만료 시 풀로 반환(Despawn)합니다.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetSummonDespawnTimer : NetworkBehaviour
    {
        private float timer;
        private bool isRunning;
        private NetworkObject netObj;

        private void Awake()
        {
            netObj = GetComponent<NetworkObject>();
        }

        public void StartTimer(float duration)
        {
            Assert.IsTrue(IsServer, "[NetSummonDespawnTimer] StartTimer는 서버에서만 호출되어야 합니다.");
            if (!IsServer) return;

            timer = duration;
            isRunning = true;
        }

        private void Update()
        {
            if (!IsServer || !isRunning) return;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                isRunning = false;
                if (GameStatics.ObjectPool != null)
                {
                    // 풀 매니저를 통해 안전하게 반환
                    GameStatics.ObjectPool.ReturnNetworkObject(netObj);
                }
                else
                {
                    netObj.Despawn();
                }
            }
        }
    }
}
