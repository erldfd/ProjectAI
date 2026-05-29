using UnityEngine;
using Unity.Netcode;
using PortalBroke.Core;

namespace PortalBroke.GameModes
{
    /// <summary>
    /// 모든 씬의 게임 모드가 상속받아야 하는 최상위 추상 클래스입니다.
    /// 공통적인 생명주기 관리와 Gateway 등록을 책임집니다.
    /// </summary>
    public abstract class GameModeBase : NetworkBehaviour
    {
        protected virtual void Awake()
        {
            // 씬이 켜질 때 자신을 전역 Gateway에 즉시 등록하여 접근성을 보장합니다.
            GameStatics.RegisterMode(this);
        }

        protected virtual void Start()
        {
            // 유니티 기본 Start 주기 (네트워크 오프라인 상태에서도 실행됨)
            OnGameModeStart();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            // 네트워크 연결이 완료된 직후 실행됨
            OnGameModeNetworkSpawn();
        }

        /// <summary>
        /// 로컬/오프라인 초기화용 가상 메서드입니다. (로비 UI 세팅 등에 적합)
        /// </summary>
        protected virtual void OnGameModeStart() { }

        /// <summary>
        /// 네트워크/온라인 초기화용 가상 메서드입니다. (서버 권한 로직, 몬스터 스폰 등에 적합)
        /// </summary>
        protected virtual void OnGameModeNetworkSpawn() { }
    }
}
