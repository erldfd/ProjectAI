using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace ProjectAI.Core
{
    /// <summary>
    /// 로컬 플레이어의 소환수 도감(해금 내역) 데이터를 로컬 스토리지에 저장하고 불러오는 정적 매니저 클래스입니다.
    /// (주의: 서버 권한의 글로벌 데이터가 아닌, 각 클라이언트 기기의 로컬 세이브용입니다.)
    /// </summary>
    public static class CodexManager
    {
        private const string CODEX_SAVE_KEY = "Player_Summon_Codex";
        
        // 해금된 소환수 스킬 ID 목록 (O(1) 검색을 위해 HashSet 사용)
        private static HashSet<int> unlockedSkillIds = new HashSet<int>();
        private static List<int> selectedLoadoutMemory = new List<int>();
        private static bool isInitialized = false;

        /// <summary>
        /// 도감 데이터를 로드합니다. 게임 시작 시 한 번 호출되어야 합니다.
        /// </summary>
        public static void Initialize()
        {
            if (isInitialized)
            {
                Debug.LogWarning("[CodexManager] 이미 초기화되었습니다. 불필요한 호출을 무시합니다.");
                return;
            }

            unlockedSkillIds.Clear();
            
            string savedData = PlayerPrefs.GetString(CODEX_SAVE_KEY, string.Empty);
            
            if (!string.IsNullOrWhiteSpace(savedData))
            {
                string[] idStrings = savedData.Split(',');
                for (int i = 0; i < idStrings.Length; i++)
                {
                    if (int.TryParse(idStrings[i], out int skillId))
                    {
                        unlockedSkillIds.Add(skillId);
                    }
                }
            }
            
            // 데이터 파싱 후에도 도감이 비어있다면 (최초 실행 또는 비정상 데이터) 기본 지급
            if (unlockedSkillIds.Count == 0)
            {
                // GameManager(MonoBehaviour) 인스펙터에서 설정한 기본 스킬 설정 SO를 가져옵니다. 
                Assert.IsNotNull(GameStatics.GameManager, "[CodexManager] GameStatics.GameManager가 null입니다. 기본 지급 소환수 SO를 가져올 수 없습니다.");
                Assert.IsNotNull(GameStatics.GameManager.DefaultUnlockSkillConfig, "[CodexManager] GameManager.DefaultUnlockSkillConfig가 할당되지 않았습니다.");
                
                int defaultId = GameStatics.GameManager.DefaultUnlockSkillConfig.SkillId;
                
                Debug.Log($"[CodexManager] 유효한 도감 데이터가 없습니다. 기본 소환수 SO({GameStatics.GameManager.DefaultUnlockSkillConfig.name}, ID:{defaultId})를 지급합니다.");
                
                // UnlockSkill을 호출하기 전 무한루프 방지를 위해 isInitialized를 먼저 true로 만듭니다.
                isInitialized = true;
                UnlockSkill(defaultId);
                
                Debug.Log($"[CodexManager] 총 {unlockedSkillIds.Count}개의 소환수 도감 데이터 로드 완료.(기본 지급: {defaultId})");
                return;
            }

            isInitialized = true;
            Debug.Log($"[CodexManager] 총 {unlockedSkillIds.Count}개의 소환수 도감 데이터 로드 완료.");
        }

        /// <summary>
        /// 특정 스킬 ID가 도감에 해금되어 있는지 확인합니다.
        /// </summary>
        public static bool IsSkillUnlocked(int skillId)
        {
            if (!isInitialized)
            {
                Initialize();
            }
            
            return unlockedSkillIds.Contains(skillId);
        }

        /// <summary>
        /// 새로운 소환수 스킬을 도감에 영구 해금합니다.
        /// </summary>
        public static bool UnlockSkill(int skillId)
        {
            if (!isInitialized)
            {
                Initialize();
            }
            
            if (unlockedSkillIds.Contains(skillId))
            {
                Debug.Log($"[CodexManager] 소환수 스킬(ID: {skillId})은 이미 도감에 해금되어 있습니다.");
                return false;
            }

            unlockedSkillIds.Add(skillId);
            SaveData();
            Debug.Log($"[CodexManager] 소환수 스킬(ID: {skillId}) 영구 해금 완료!");
            return true;
        }

        /// <summary>
        /// 해금된 모든 스킬 ID의 리스트를 반환합니다. (UI 출력용)
        /// </summary>
        public static List<int> GetUnlockedSkillIds()
        {
            if (!isInitialized)
            {
                Initialize();
            }
            
            return new List<int>(unlockedSkillIds);
        }

        /// <summary>
        /// 선택한 소환수 로드아웃 스킬 ID 목록을 런타임 메모리에 저장합니다. (게임 재시작 시 초기화됨)
        /// </summary>
        public static void SaveSelectedLoadout(List<int> skillIds)
        {
            Assert.IsNotNull(skillIds, "[CodexManager] SaveSelectedLoadout: skillIds가 null입니다.");
            
            if (skillIds == null)
            {
                Debug.LogWarning("[CodexManager] SaveSelectedLoadout: skillIds가 null이므로 저장을 취소합니다.");
                return;
            }

            selectedLoadoutMemory.Clear();
            for (int i = 0; i < skillIds.Count; i++)
            {
                selectedLoadoutMemory.Add(skillIds[i]);
            }

            Debug.Log($"[CodexManager] 소환수 로드아웃 선택 정보 런타임 메모리 저장 완료 ({selectedLoadoutMemory.Count}개 스킬)");
        }

        /// <summary>
        /// 런타임 메모리에 저장된 소환수 로드아웃 스킬 ID 목록을 불러옵니다.
        /// </summary>
        public static List<int> GetSavedLoadout()
        {
            return new List<int>(selectedLoadoutMemory);
        }

        /// <summary>
        /// 런타임 메모리에 저장된 유효한 소환수 로드아웃 데이터 보유 여부를 확인합니다.
        /// </summary>
        public static bool HasSavedLoadout()
        {
            return selectedLoadoutMemory.Count > 0;
        }

        /// <summary>
        /// 런타임 메모리에 저장된 소환수 로드아웃 선택 정보를 초기화합니다. (로비 상호작용 재선택 시 활용)
        /// </summary>
        public static void ClearSelectedLoadout()
        {
            if (selectedLoadoutMemory.Count > 0)
            {
                selectedLoadoutMemory.Clear();
                Debug.Log("[CodexManager] 런타임 메모리에 저장된 소환수 로드아웃 정보가 초기화되었습니다.");
            }
        }

        private static void SaveData()
        {
            string saveData = string.Join(",", unlockedSkillIds);
            PlayerPrefs.SetString(CODEX_SAVE_KEY, saveData);
            PlayerPrefs.Save();
        }
    }
}
