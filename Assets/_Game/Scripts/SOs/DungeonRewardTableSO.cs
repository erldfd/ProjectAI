using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace ProjectAI.SOs
{
    public enum ERewardType : byte
    {
        Summon,
        SummonUpgrade,
        PlayerUpgrade
    }

    /// <summary>
    /// 개별 보상(소환수 교체, 강화 옵션 등)의 세부 정보를 담는 데이터 구조체입니다.
    /// </summary>
    [Serializable]
    public struct SRewardItemData
    {

        public string RewardName;

        [Tooltip("고유 식별자 (네트워크 동기화나 디버깅에 사용될 수 있음)")]
        public string Id;
        
        [TextArea] 
        public string Description;
        
        public Sprite Icon;

        [Header("소환수 전용")]
        public NetworkObject SummonPrefab;

        [Header("강화 옵션 전용 (임시)")]
        public float UpgradeValue;
    }

    /// <summary>
    /// 던전에서 획득 가능한 각종 보상 데이터(소환수, 소환수 강화, 플레이어 코어 강화 등)를 관리하는 개별 테이블(DB)입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "DungeonRewardTable", menuName = "ProjectAI/SOs/DungeonRewardTable")]
    public class DungeonRewardTableSO : ScriptableObject
    {
        public List<SRewardItemData> SummonPool = new List<SRewardItemData>();
        public List<SRewardItemData> SummonUpgradePool = new List<SRewardItemData>();
        public List<SRewardItemData> PlayerUpgradePool = new List<SRewardItemData>();

        /// <summary>
        /// 풀에서 인덱스로 데이터 반환 (RPC 통신 시 대역폭 절감을 위해 인덱스 전송 방식 채택)
        /// </summary>
        public bool TryGetRewardData(ERewardType type, int index, out SRewardItemData data)
        {
            List<SRewardItemData> pool = GetPool(type);

            if (pool == null || index < 0 || index >= pool.Count)
            {
                Debug.LogWarning($"[DungeonRewardTableSO] 보상 데이터를 찾을 수 없습니다. (Type: {type}, Index: {index})");
                data = default;
                return false;
            }

            data = pool[index];
            return true;
        }

        public List<SRewardItemData> GetPool(ERewardType type)
        {
            return type switch
            {
                ERewardType.Summon => SummonPool,
                ERewardType.SummonUpgrade => SummonUpgradePool,
                ERewardType.PlayerUpgrade => PlayerUpgradePool,
                _ => null
            };
        }
    }
}
