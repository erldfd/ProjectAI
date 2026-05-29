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
            // 씬이 켜질 때 자신을 전역 Gateway에 등록
            GameStatics.RegisterMode(this);
        }

        protected virtual void Start()
        {
            OnGameModeStart();
        }

        /// <summary>
        /// 자식 클래스들이 Start() 대신 구현해야 하는 초기화 메서드입니다.
        /// </summary>
        protected abstract void OnGameModeStart();
    }
}
