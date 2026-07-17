using UnityEngine;
using UnityEngine.Assertions;
using Unity.Netcode;
using System;
using System.Collections.Generic;
using ProjectAI.Core;
using ProjectAI.Core.Skills;
using ProjectAI.Characters.MonsterAI;

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

        public NetworkVariable<ESummonStance> CurrentStance = new NetworkVariable<ESummonStance>(
            ESummonStance.Aggressive,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public Transform CurrentPriorityTarget { get; private set; }

        public void ToggleStance()
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetSummonController] ToggleStance는 서버에서만 호출되어야 합니다.");
            if (!GameStatics.IsServerAuthorized)
            {
                Debug.LogWarning("[NetSummonController] ToggleStance: 클라이언트에서 실행 시도 (무시됨)");
                return;
            }

            Assert.IsNotNull(GameStatics.NetworkManager, "[NetSummonController] ToggleStance: NetworkManager가 null입니다.");

            CurrentStance.Value = (CurrentStance.Value == ESummonStance.Aggressive) 
                ? ESummonStance.Defensive 
                : ESummonStance.Aggressive;

            if (CurrentStance.Value == ESummonStance.Defensive)
            {
                CurrentPriorityTarget = null;
            }

            for (int i = 0; i < ActiveSummons.Count; i++)
            {
                if (!GameStatics.TryGetSpawnedObject(ActiveSummons[i].SummonNetworkObjectId, out NetworkObject summonNetObj))
                {
                    continue;
                }

                if (!summonNetObj.TryGetComponent(out NetSummonBrain brain))
                {
                    continue;
                }

                brain.SetStance(CurrentStance.Value);
            }

            Debug.Log($"[NetSummonController] 소환수 태세 변경됨: {CurrentStance.Value}");
        }

        public void AddSummon(NetworkObject summonObj, float duration)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetSummonController] AddSummon은 서버에서만 호출되어야 합니다.");
            Assert.IsNotNull(summonObj, "[NetSummonController] AddSummon: summonObj 인자가 null입니다.");
            if (!GameStatics.IsServerAuthorized)
            {
                Debug.LogWarning("[NetSummonController] AddSummon: 클라이언트에서 실행 시도 (무시됨)");
                return;
            }

            Assert.IsNotNull(GameStatics.NetworkManager, "[NetSummonController] AddSummon: NetworkManager가 null입니다.");

            ActiveSummons.Add(new SSummonData
            {
                SummonNetworkObjectId = summonObj.NetworkObjectId,
                EndTime = (float)GameStatics.NetworkManager.ServerTime.Time + duration
            });

            if (CurrentPriorityTarget != null && !CurrentPriorityTarget.gameObject.activeInHierarchy)
            {
                CurrentPriorityTarget = null;
            }

            if (!summonObj.TryGetComponent(out NetSummonBrain brain))
            {
                if (CurrentPriorityTarget != null)
                {
                    Debug.LogWarning($"[NetSummonController] 소환수({summonObj.name})에 NetSummonBrain 컴포넌트가 없어 마킹 지시가 불가능합니다.");
                }
                
                return;
            }

            brain.SetStance(CurrentStance.Value);
            if (CurrentPriorityTarget != null)
            {
                brain.PriorityTarget = CurrentPriorityTarget;
            }
        }

        private void Update()
        {
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }
            Assert.IsNotNull(GameStatics.NetworkManager, "[NetSummonController] Update: NetworkManager가 null입니다.");

            float currentTime = (float)GameStatics.NetworkManager.ServerTime.Time;
            
            // 역순 순회하여 안전하게 제거
            for (int i = ActiveSummons.Count - 1; i >= 0; i--)
            {
                if (currentTime >= ActiveSummons[i].EndTime)
                {
                    DespawnSummon(ActiveSummons[i].SummonNetworkObjectId);
                    ActiveSummons.RemoveAt(i);
                    continue;
                }

                // 시간이 안 끝났더라도 외부에서 이미 파괴되거나 Despawn 되었는지 검증
                if (!GameStatics.TryGetSpawnedObject(ActiveSummons[i].SummonNetworkObjectId, out _))
                {
                    ActiveSummons.RemoveAt(i);
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
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetSummonController] DespawnSummon은 서버에서만 호출되어야 합니다.");

            Assert.IsNotNull(GameStatics.NetworkManager, "[NetSummonController] DespawnSummon: NetworkManager가 null입니다.");
            Assert.IsNotNull(GameStatics.NetworkManager.SpawnManager, "[NetSummonController] DespawnSummon: SpawnManager가 null입니다.");

            if (!GameStatics.TryGetSpawnedObject(objId, out NetworkObject netObj))
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
                Debug.LogWarning("[NetSummonController] SetPriorityTarget: 클라이언트에서 실행 시도 (무시됨)");
                return;
            }

            Assert.IsNotNull(GameStatics.NetworkManager, "[NetSummonController] SetPriorityTarget: NetworkManager가 null입니다.");

            CurrentPriorityTarget = target;

            for (int i = 0; i < ActiveSummons.Count; i++)
            {
                if (!GameStatics.TryGetSpawnedObject(ActiveSummons[i].SummonNetworkObjectId, out NetworkObject summonNetObj))
                {
                    continue;
                }

                if (!summonNetObj.TryGetComponent(out NetSummonBrain brain))
                {
                    continue;
                }

                brain.PriorityTarget = target;
            }
        }

        public void ReplaceSummon(NetworkObject summonPrefab, float duration)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetSummonController] ReplaceSummon은 서버에서만 호출되어야 합니다.");
            Assert.IsNotNull(summonPrefab, "[NetSummonController] ReplaceSummon: summonPrefab 인자가 null입니다.");
            Assert.IsNotNull(GameStatics.ObjectPool, "[NetSummonController] GameStatics.ObjectPool이 등록되어 있지 않습니다!");

            // 1. 기존 소환수들 일괄 폭파
            for (int i = ActiveSummons.Count - 1; i >= 0; i--)
            {
                DespawnSummon(ActiveSummons[i].SummonNetworkObjectId);
            }
            
            ActiveSummons.Clear();

            // 2. 새 소환수 생성 및 스폰 (플레이어 우측 1.5f 임시 위치)
            Vector3 spawnPos = transform.position + (Vector3.right * 1.5f);
            
            NetworkObject newSummonObj = GameStatics.ObjectPool.GetNetworkObject(summonPrefab, spawnPos, Quaternion.identity);
            Assert.IsNotNull(newSummonObj, "[NetSummonController] ObjectPool에서 소환수를 가져오지 못했습니다.");
            
            if (!newSummonObj.IsSpawned)
            {
                newSummonObj.Spawn(true);
            }
            
            // 3. 소환수 컨트롤러에 등록
            AddSummon(newSummonObj, duration);
        }
    }
}
