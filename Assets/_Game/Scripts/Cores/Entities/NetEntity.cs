using UnityEngine;
using UnityEngine.Assertions;

namespace ProjectAI.Core.Entities
{
    /// <summary>
    /// 생명체(캐릭터) 및 투사체(마법탄) 등 모든 상호작용 가능한 독립 객체의 최상위 기반 클래스입니다.
    /// 공통적인 상태 이벤트(EntityEvents)를 필수로 가집니다.
    /// </summary>
    public class NetEntity : Unity.Netcode.NetworkBehaviour
    {
        public EntityEvents Events { get; private set; }

        protected virtual void Awake()
        {
            Events = GetComponentInChildren<EntityEvents>();
            Assert.IsNotNull(Events, "NetEntity는 EntityEvents 오너가 필요합니다.");
        }
    }
}
