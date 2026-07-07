using UnityEngine;
using UnityEngine.Assertions;
using ProjectAI.Core;

namespace ProjectAI.GameModes
{
    /// <summary>
    /// 던전 씬 전용 게임 모드 클래스입니다.
    /// 특수 기믹이나 몬스터 스폰 등을 관리합니다.
    /// </summary>
    public class NetDungeonGameMode : ANetGameModeBase
    {
        [Header("Dungeon Settings")]
        [Tooltip("현재 던전의 테마입니다. 테마에 맞는 몬스터 스폰 테이블을 DB에서 자동으로 가져옵니다.")]
        [SerializeField]
        private EDungeonTheme currentTheme = EDungeonTheme.Forest;

        public EDungeonTheme CurrentTheme => currentTheme;

        public SpawnTableSO CurrentSpawnTable { get; private set; }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            SetupSpawnTable();
        }

        private void SetupSpawnTable()
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetDungeonGameMode] SetupSpawnTable은 서버에서만 호출되어야 합니다.");

            Assert.IsNotNull(GameStatics.SpawnTableDB, "[NetDungeonGameMode] GameStatics.SpawnTableDB가 null입니다.");
            if (GameStatics.SpawnTableDB == null)
            {
                Debug.LogWarning("[NetDungeonGameMode] 전역 SpawnTableDatabaseSO를 불러오지 못했습니다.");
                return;
            }

            CurrentSpawnTable = GameStatics.SpawnTableDB.GetTable(currentTheme);

            if (CurrentSpawnTable == null)
            {
                Debug.LogWarning($"[NetDungeonGameMode] {currentTheme} 테마의 스폰 테이블을 찾지 못했습니다! DB 세팅을 확인하세요.");
                return;
            }

            if (CurrentSpawnTable.MonsterSlots == null)
            {
                Debug.LogWarning($"[NetDungeonGameMode] {CurrentSpawnTable.name}의 MonsterSlots 리스트가 비정상적으로 비어있습니다. 인스펙터를 확인하세요.");
                return;
            }

            Assert.IsNotNull(GameStatics.ObjectPool, "[NetDungeonGameMode] GameStatics.ObjectPool이 null입니다.");
            if (GameStatics.ObjectPool == null)
            {
                Debug.LogWarning("[NetDungeonGameMode] 전역 ObjectPool을 찾을 수 없습니다.");
                return;
            }

            foreach (SpawnMonsterSlot slot in CurrentSpawnTable.MonsterSlots)
            {
                if (slot == null || slot.Candidates == null)
                {
                    Debug.LogWarning($"[NetDungeonGameMode] {CurrentSpawnTable.name} 내부에 비어있는 슬롯이나 Candidates가 존재합니다. 올바르게 설정해주세요.");
                    continue;
                }

                foreach (SpawnWeightInfo candidate in slot.Candidates)
                {
                    if (candidate == null || candidate.Prefab == null)
                    {
                        Debug.LogWarning($"[NetDungeonGameMode] {CurrentSpawnTable.name}의 {slot.SlotType} 슬롯에 프리팹이 할당되지 않은 후보가 있습니다!");
                        continue;
                    }

                    GameStatics.ObjectPool.SetupPool(candidate.Prefab, 5, true);
                }
            }
        }
    }
}
