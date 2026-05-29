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
            Debug.Log("[DungeonGameMode] 던전 씬 게임 모드 초기화 완료.");

            // 에디터 테스트의 편의를 위해, 네트워크 연결 없이 시작되었다면 자동으로 호스트를 열어줍니다.
            if (GameStatics.NetworkManager != null)
            {
                if (!GameStatics.NetworkManager.IsClient && !GameStatics.NetworkManager.IsServer)
                {
                    Debug.LogWarning("[DungeonGameMode] 네트워크 연결 없이 시작되었습니다! 자동으로 호스트를 시작합니다. (에디터 테스트 모드)");
                    GameStatics.NetworkManager.StartHost();
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsServer)
            {
                // 서버 권한으로 이곳에서 던전 생성(시드 결정) 및 플레이어 초기 스폰 로직을 실행할 예정입니다.
                Debug.Log("[DungeonGameMode] 서버: 던전 초기화 로직 대기 중...");
            }
        }
    }
}
