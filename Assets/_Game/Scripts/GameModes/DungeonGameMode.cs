using System.Collections;
using UnityEngine;
using PortalBroke.Core;

namespace PortalBroke.GameModes
{
    /// <summary>
    /// 실제 게임(던전) 씬을 관리하는 게임 매니저입니다.
    /// 던전 생성, 몬스터 스폰, 앵커 관리 등을 담당할 핵심 스크립트입니다.
    /// </summary>
    public class DungeonGameMode : GameModeBase
    {
        protected override void OnGameModeStart()
        {
            // 네트워크 연결 전의 오프라인 1차 초기화
            Debug.Log("[DungeonGameMode] 던전 씬 오프라인 초기화 완료.");
            
            // 네트워크 연결이 없는 에디터 단독 테스트 환경일 경우를 위한 코루틴 실행
            StartCoroutine(AutoStartHostRoutine());
        }

        private IEnumerator AutoStartHostRoutine()
        {
            // NGO의 내부 초기화 순서와 꼬이지 않도록 무조건 1프레임을 대기합니다.
            yield return null;

            if (GameStatics.NetworkManager != null)
            {
                if (!GameStatics.NetworkManager.IsClient && !GameStatics.NetworkManager.IsServer)
                {
                    Debug.LogWarning("[DungeonGameMode] 1프레임 지연 후 자동으로 호스트를 시작합니다. (에디터 테스트 모드)");
                    GameStatics.NetworkManager.StartHost();
                }
            }
        }

        protected override void OnGameModeNetworkSpawn()
        {
            // 네트워크 연결 성공 후의 온라인 2차 초기화
            Debug.Log("[DungeonGameMode] 네트워크 연결 완료! 던전 씬 온라인 초기화 시작.");
            
            if (IsServer)
            {
                // 서버 권한으로 이곳에서 던전 생성(시드 결정) 및 플레이어 초기 스폰 로직을 실행합니다.
                Debug.Log("[DungeonGameMode] 서버 권한 확인 완료. 던전 자동 생성 및 스폰 로직 대기 중...");
            }
        }
    }
}
