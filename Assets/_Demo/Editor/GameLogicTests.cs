using NUnit.Framework;
using UnityEngine;
using Unity.Netcode;
using ProjectAI.Core.Stats;
using ProjectAI.Core.Entities;
using ProjectAI.Characters;

namespace ProjectAI.Tests
{
    public class GameLogicTests
    {
        [Test]
        public void LateJoiner_OnNetworkSpawn_Order_Issue_Test()
        {
            // NGO 환경에서 동일 프리팹 내 NetworkBehaviour들의 OnNetworkSpawn 실행 순서 미보장으로 인한
            // 지연 접속자 사망 이벤트 누락 시나리오 검증
            
            // 1. 컴포넌트 구성
            GameObject go = new GameObject("TestCharacter");
            EntityEvents entityEvents = go.AddComponent<EntityEvents>();
            NetHealthComponent healthComp = go.AddComponent<NetHealthComponent>();
            NetStatComponent statComp = go.AddComponent<NetStatComponent>();
            NetCharacter character = go.AddComponent<NetCharacter>();
            
            // 2. Awake 시뮬레이션 (Unity 생명주기)
            // 보통 Awake는 Instantiate 시점에 자동으로 호출됨
            
            // 3. Late Joiner 시나리오: 초기 체력이 0이하로 동기화된 상태 가정
            // (NetworkVariable 직접 수정은 런타임 이슈가 있을 수 있어 리플렉션 등으로 우회하거나 초기값으로 세팅)
            healthComp.CurrentHealth.Value = 0; 
            
            bool isDeathTriggered = false;
            entityEvents.OnDeathTriggered += () => { isDeathTriggered = true; };
            
            // 4. 시나리오: NetHealthComponent가 NetStatComponent나 EntityEvents 구독보다 먼저 OnNetworkSpawn 될 경우
            // (Unity 인스펙터 순서에 따라 NetHealthComponent가 먼저 스폰된다고 가정)
            
            // NetHealthComponent OnNetworkSpawn 직접 호출
            healthComp.OnNetworkSpawn(); 
            
            // 이 시점에 healthComp는 OnDeath를 Invoke 했지만, 아직 statComp가 구독하지 않은 상태임
            
            // 나중에 NetStatComponent와 NetCharacter가 OnNetworkSpawn을 호출하여 구독을 시도함
            statComp.OnNetworkSpawn();
            character.OnNetworkSpawn();
            
            // 검증: 사망 이벤트가 전달되었어야 하나(Late Joiner의 경우 죽어있는 상태로 보여야 함),
            // 구독 타이밍 이슈로 인해 entityEvents.OnDeathTriggered는 호출되지 않았을 것이다.
            Assert.IsFalse(isDeathTriggered, "현재 구조에서는 실행 순서에 따라 사망 이벤트가 누락되는 취약점이 있음이 확인되어야 합니다.");
            
            // 만약 여기서 이 테스트가 성공한다면(Assert.IsFalse가 통과한다면), 현재 구조에 버그(누락)가 있다는 뜻입니다.
            // 즉, 리뷰어가 제안한 대로 "지연 접속자의 사망 이벤트 구독이 누락될 수 있다"는 점이 런타임 모의로 증명된 것.
            
            Object.DestroyImmediate(go);
        }
    }
}
