using UnityEngine;
using UnityEngine.Assertions;
using Unity.Netcode;
using System;
using System.Collections.Generic;
using ProjectAI.Core;

namespace ProjectAI.Characters.Summons
{
    /// <summary>
    /// 개별 소환수의 네트워크 ID와 만료 시간을 저장하는 구조체입니다.
    /// </summary>
    public struct SSummonData : INetworkSerializable, IEquatable<SSummonData>
    {
        public ulong SummonNetworkObjectId;
        public float EndTime;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref SummonNetworkObjectId);
            serializer.SerializeValue(ref EndTime);
        }

        public bool Equals(SSummonData other)
        {
            return SummonNetworkObjectId == other.SummonNetworkObjectId && EndTime == other.EndTime;
        }
    }

    /// <summary>
    /// 플레이어 캐릭터가 보유한 다중 소환수의 지속 시간 및 생명주기를 담당하는 독립 컴포넌트입니다.
    /// </summary>
    public class NetSummonController : NetworkBehaviour
    {
        public NetworkList<SSummonData> ActiveSummons = new NetworkList<SSummonData>(
            new List<SSummonData>(),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public Transform CurrentPriorityTarget { get; private set; }

        public void AddSummon(NetworkObject summonObj, float duration)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetSummonController] AddSummon은 서버에서만 호출되어야 합니다.");
            Assert.IsNotNull(summonObj, "[NetSummonController] AddSummon: summonObj 인자가 null입니다.");
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            ActiveSummons.Add(new SSummonData
            {
                SummonNetworkObjectId = summonObj.NetworkObjectId,
                EndTime = (float)GameStatics.NetworkManager.ServerTime.Time + duration
            });

            if (CurrentPriorityTarget != null && !CurrentPriorityTarget.gameObject.activeInHierarchy)
            {
                CurrentPriorityTarget = null;
            }

            if (CurrentPriorityTarget != null)
            {
                if (summonObj.TryGetComponent(out ProjectAI.Characters.MonsterAI.NetMonsterBrain brain))
                {
                    brain.PriorityTarget = CurrentPriorityTarget;
                }
                else
                {
                    Debug.LogWarning($"[NetSummonController] 소환수({summonObj.name})에 NetMonsterBrain 컴포넌트가 없어 마킹 지시가 불가능합니다.");
                }
            }
        }

        private void Update()
        {
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            float currentTime = (float)GameStatics.NetworkManager.ServerTime.Time;
            
            // 역순 순회하여 안전하게 제거
            for (int i = ActiveSummons.Count - 1; i >= 0; i--)
            {
                if (currentTime >= ActiveSummons[i].EndTime)
                {
                    DespawnSummon(ActiveSummons[i].SummonNetworkObjectId);
                    ActiveSummons.RemoveAt(i);
                }
                else
                {
                    // 시간이 안 끝났더라도 외부에서 이미 파괴되거나 Despawn 되었는지 검증
                    if (!GameStatics.NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(ActiveSummons[i].SummonNetworkObjectId))
                    {
                        ActiveSummons.RemoveAt(i);
                    }
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            // 플레이어 접속 종료/파괴 시 맵에 남은 소환수 고아 방지
            if (GameStatics.IsServerAuthorized)
            {
                for (int i = ActiveSummons.Count - 1; i >= 0; i--)
                {
                    DespawnSummon(ActiveSummons[i].SummonNetworkObjectId);
                }
            }

            base.OnNetworkDespawn();
        }

        private void DespawnSummon(ulong objId)
        {
            if (!GameStatics.NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(objId, out NetworkObject netObj))
            {
                Debug.LogWarning($"[NetSummonController] DespawnSummon 실패: ID {objId}에 해당하는 네트워크 객체를 찾을 수 없습니다.");
                return;
            }

            if (GameStatics.ObjectPool != null)
            {
                GameStatics.ObjectPool.ReturnNetworkObject(netObj);
            }
            else
            {
                netObj.Despawn();
            }
        }

        public void SetPriorityTarget(Transform target)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetSummonController] SetPriorityTarget은 서버에서만 호출되어야 합니다.");
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            CurrentPriorityTarget = target;

            for (int i = 0; i < ActiveSummons.Count; i++)
            {
                if (GameStatics.NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(ActiveSummons[i].SummonNetworkObjectId, out NetworkObject summonNetObj))
                {
                    if (summonNetObj.TryGetComponent(out ProjectAI.Characters.MonsterAI.NetMonsterBrain brain))
                    {
                        brain.PriorityTarget = target;
                    }
                }
            }
        }
    }
}
