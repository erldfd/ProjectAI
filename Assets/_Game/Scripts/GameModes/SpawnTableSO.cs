using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Netcode;

namespace ProjectAI.GameModes
{
    /// <summary>
    /// 스폰할 프리팹과 그 가중치(확률)를 매핑하는 데이터 클래스입니다.
    /// </summary>
    [Serializable]
    public class SpawnWeightInfo
    {
        [Tooltip("스폰할 몬스터 프리팹")]
        public NetworkObject Prefab;
        
        [Range(1, 100)]
        [Tooltip("스폰 가중치 (확률)")]
        public int Weight = 1;
    }

    /// <summary>
    /// 단일 스폰 슬롯(타입)에 배정될 여러 몬스터 후보군을 정의하는 데이터 클래스입니다.
    /// </summary>
    [Serializable]
    public class SpawnMonsterSlot
    {
        [Tooltip("추상화된 스폰 슬롯 타입")]
        public ESpawnMonsterType SlotType;
        
        [Tooltip("해당 슬롯에 배정된 몬스터 프리팹 후보들 (가중치 기반 랜덤)")]
        public List<SpawnWeightInfo> Candidates = new List<SpawnWeightInfo>();
    }

    /// <summary>
    /// 던전/스테이지별로 각 스폰 슬롯에 어떤 몬스터가 등장할지를 확률 기반으로 관리하는 데이터베이스(SO)입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnTable", menuName = "ProjectAI/GameModes/SpawnTable")]
    public class SpawnTableSO : ScriptableObject
    {
        [Tooltip("해당 던전/스테이지에서 슬롯별로 등장할 몬스터 매핑 정보")]
        public List<SpawnMonsterSlot> MonsterSlots = new List<SpawnMonsterSlot>();

        /// <summary>
        /// 주어진 슬롯 타입에 맞는 프리팹을 가중치 기반으로 무작위 선택하여 반환합니다.
        /// </summary>
        public NetworkObject GetRandomPrefab(ESpawnMonsterType type)
        {
            if (MonsterSlots == null)
            {
                Debug.LogWarning($"[SpawnTableSO] MonsterSlots 리스트가 null입니다.");
                return null;
            }

            foreach (SpawnMonsterSlot slot in MonsterSlots)
            {
                if (slot == null || slot.SlotType != type)
                {
                    continue;
                }

                if (slot.Candidates == null || slot.Candidates.Count == 0)
                {
                    Debug.LogWarning($"[SpawnTableSO] {type} 슬롯에 등록된 몬스터 후보가 없습니다.");
                    return null;
                }

                int totalWeight = 0;
                foreach (SpawnWeightInfo c in slot.Candidates)
                {
                    if (c == null || c.Prefab == null)
                    {
                        Debug.LogWarning($"[SpawnTableSO] {this.name}의 {type} 슬롯에 프리팹이 비어있는 후보가 있습니다. 인스펙터를 확인하세요.");
                        continue;
                    }

                    totalWeight += c.Weight;
                    Debug.Log($"[SpawnTableSO] {type} 슬롯 후보: {c.Prefab.name}, 가중치: {c.Weight}");
                }

                if (totalWeight <= 0)
                {
                    Debug.LogWarning($"[SpawnTableSO] {type} 슬롯의 총 가중치가 0 이하입니다.");
                    return null;
                }

                int randomVal = UnityEngine.Random.Range(0, totalWeight);
                int currentWeight = 0;
                
                foreach (SpawnWeightInfo c in slot.Candidates)
                {
                    if (c == null || c.Prefab == null)
                    {
                        continue;
                    }

                    currentWeight += c.Weight;
                    if (randomVal < currentWeight)
                    {
                        return c.Prefab;
                    }
                }
                
                // Fallback
                foreach (SpawnWeightInfo c in slot.Candidates)
                {
                    if (c != null && c.Prefab != null)
                    {
                        return c.Prefab;
                    }
                }
                
                return null;
            }
            
            Debug.LogWarning($"[SpawnTableSO] {type} 슬롯을 데이터베이스에서 찾을 수 없습니다.");
            return null;
        }
    }
}
