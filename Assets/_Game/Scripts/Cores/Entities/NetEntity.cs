using UnityEngine;
using UnityEngine.Assertions;
using ProjectAI.Movements;
using Unity.Netcode;

namespace ProjectAI.Core.Entities
{
    /// <summary>
    /// 생명체(캐릭터) 및 투사체(마법탄) 등 모든 상호작용 가능한 독립 객체의 최상위 기반 클래스입니다.
    /// 공통적인 상태 이벤트(EntityEvents)를 필수로 가집니다.
    /// </summary>
    public class NetEntity : NetworkBehaviour
    {
        public EntityEvents Events { get; private set; }
        
        /// <summary>
        /// 물리 이동/동기화를 담당하는 컴포넌트입니다. 투사체 등 이동이 없는 엔티티의 경우 null일 수 있습니다.
        /// </summary>
        public ANetMovement Movement { get; private set; }

        protected virtual void Awake()
        {
            Events = GetComponentInChildren<EntityEvents>();
            Assert.IsNotNull(Events, "NetEntity는 EntityEvents 오너가 필요합니다.");
            
            Movement = GetComponentInChildren<ANetMovement>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            Animator animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.cullingMode = GameStatics.IsServerAuthorized ? AnimatorCullingMode.AlwaysAnimate : AnimatorCullingMode.CullCompletely;
            }
        }
    }
}
