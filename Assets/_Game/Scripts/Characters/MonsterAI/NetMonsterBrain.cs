using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using ProjectAI.Core;
using ProjectAI.Core.Attributes;
using ProjectAI.Movements;
using ProjectAI.Core.Skills;
using ProjectAI.Characters;
using ProjectAI.SOs;

namespace ProjectAI.Characters.MonsterAI
{
    /// <summary>
    /// 몬스터의 FSM(상태 기계)을 돌리며 판단을 내리고 몸체(NetCharacter)를 조종하는 두뇌 클래스입니다.
    /// 모든 연산은 서버(Host)에서만 구동됩니다.
    /// </summary>
    [RequireComponent(typeof(NetCharacter))]
    public class NetMonsterBrain : NetworkBehaviour
    {
        private const float SENSOR_UPDATE_INTERVAL = 0.15f;
        private const int MAX_COLLIDER_RESULTS = 10;
        private const float LOST_TARGET_MULTIPLIER = 1.5f;
        private const float DEFENSIVE_DETECT_RADIUS_MULTIPLIER = 0.25f;

        public NetCharacter Character { get; private set; }
        public AIStateMachine StateMachine { get; private set; }

        /// <summary>
        /// 소환수일 경우 주인이 스킬로 지정해준 타겟
        /// </summary>
        public Transform PriorityTarget { get; set; }
        public Transform Target { get; private set; }
        
        [Tooltip("소환수인 경우 주인(Owner) 할당")]
        public Transform Owner { get; set; }
        public float AttackRadius => attackRadius;
        public float TetherRadius => tetherRadius;
        public bool IsSensorEnabled 
        { 
            get => isSensorEnabled; 
            set => isSensorEnabled = value; 
        }

        [Header("AI Sensors")]
        [Tooltip("초기 시작 상태 (비워두면 첫 번째 상태가 기본값이 됩니다)")]
        [SerializeField]
        private AMonsterState startingState;

        [Tooltip("센서(탐지) 작동 여부")]
        [SerializeField]
        private bool isSensorEnabled = true;

        [Tooltip("적군을 식별할 기본 레이어 마스크")]
        [SerializeField]
        private LayerMask detectLayer;

        [Tooltip("비워두면 태그 검사를 무시합니다.")]
        [TagSelector]
        [SerializeField]
        private string detectTag = ObjectTags.NONE;

        [Tooltip("탐지 반경")]
        [SerializeField]
        private float detectionRadius = 8f;

        [Tooltip("소환수가 주인을 벗어날 수 있는 최대 거리 (테더링 반경)")]
        [SerializeField]
        private float tetherRadius = 15f;

        [Tooltip("공격 사거리")]
        [SerializeField]
        private float attackRadius = 1.5f;

        [Tooltip("최우선 타겟(마킹) 최대 추적 거리 배수")]
        [SerializeField]
        private float priorityChaseMultiplier = 3f;

        private ESummonStance currentStance = ESummonStance.Aggressive;

        private AMonsterState[] stateComponents;
        private Collider2D[] hitColliders = new Collider2D[MAX_COLLIDER_RESULTS];
        private ContactFilter2D enemyFilter;

        private float sensorTimer = 0f;

        private LayerMask currentDetectLayer;
        private string currentDetectTag;
        private float currentDetectRadius;

        private void Awake()
        {
            Character = GetComponent<NetCharacter>();
            Assert.IsNotNull(Character, "[NetMonsterBrain] NetCharacter를 찾을 수 없습니다.");

            enemyFilter = new ContactFilter2D();
            enemyFilter.useLayerMask = true;
            enemyFilter.useTriggers = false;

            StateMachine = new AIStateMachine();
            stateComponents = GetComponentsInChildren<AMonsterState>();

            foreach (AMonsterState state in stateComponents)
            {
                if (state.IsRootState)
                {
                    state.Initialize(this, StateMachine);
                    StateMachine.AddState(state);
                }
            }

            ResetSensor();
        }

        public void OverrideSensor(LayerMask newLayer, string newTag, float newRadius)
        {
            currentDetectLayer = newLayer;
            currentDetectTag = newTag;
            currentDetectRadius = newRadius;
            
            enemyFilter.layerMask = currentDetectLayer;
        }

        public void ResetSensor()
        {
            currentDetectLayer = detectLayer;
            currentDetectTag = detectTag;
            currentDetectRadius = detectionRadius;
            
            enemyFilter.layerMask = currentDetectLayer;
        }

        public void SetStance(ESummonStance stance)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetMonsterBrain] SetStance는 서버에서만 호출되어야 합니다.");
            if (!GameStatics.IsServerAuthorized)
            {
                Debug.LogWarning("[NetMonsterBrain] SetStance: 클라이언트에서 실행 시도 (무시됨)");
                return;
            }

            currentStance = stance;
            if (currentStance == ESummonStance.Defensive)
            {
                // 즉시 전투 중단 및 타겟 해제
                Target = null;
                PriorityTarget = null;
            }

            Debug.Log($"[NetMonsterBrain] 태세 변경됨: {currentStance}");
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // 풀링 재소환 시 상태값들 안전하게 초기화
            currentStance = ESummonStance.Aggressive;
            PriorityTarget = null;
            Target = null;
            sensorTimer = 0f;
            ResetSensor();

            if (!GameStatics.IsServerAuthorized)
            {
                Debug.Log($"[NetMonsterBrain] 클라이언트이므로 AI 뇌를 비활성화합니다. (ID: {NetworkObjectId})");
                // 클라이언트는 무거운 AI FSM 연산을 수행하지 않습니다.
                enabled = false;
                return;
            }

            if (startingState != null)
            {
                Assert.IsTrue(startingState.IsRootState, "[NetMonsterBrain] startingState는 반드시 루트 상태(isRootState = true)여야 합니다.");
                StateMachine.Initialize(startingState.GetType());
            }
            else
            {
                foreach (AMonsterState state in stateComponents)
                {
                    if (state.IsRootState)
                    {
                        StateMachine.Initialize(state.GetType());
                        break;
                    }
                }
            }

            Assert.IsNotNull(StateMachine.CurrentState, "[NetMonsterBrain] 상태머신 초기화 실패: 루트 상태를 찾을 수 없습니다.");
        }

        public override void OnNetworkDespawn()
        {
            if (GameStatics.IsServerAuthorized && StateMachine != null && StateMachine.CurrentState != null)
            {
                StateMachine.CurrentState.Exit();
            }

            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            if (isSensorEnabled)
            {
                sensorTimer += Time.deltaTime;
                if (sensorTimer >= SENSOR_UPDATE_INTERVAL)
                {
                    sensorTimer = 0f;
                    UpdateSensors();
                }
            }

            StateMachine.Tick();
        }

        private void UpdateSensors()
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetMonsterBrain] UpdateSensors는 서버에서만 호출되어야 합니다.");

            // 호위 태세 페널티 적용
            float effectiveDetectRadius = (currentStance == ESummonStance.Defensive) 
                ? currentDetectRadius * DEFENSIVE_DETECT_RADIUS_MULTIPLIER 
                : currentDetectRadius;

            float effectiveTetherRadius = (currentStance == ESummonStance.Defensive)
                ? tetherRadius * DEFENSIVE_DETECT_RADIUS_MULTIPLIER
                : tetherRadius;

            float maxReach = effectiveTetherRadius + attackRadius;
            float sqrMaxReach = maxReach * maxReach;

            // 타겟 기반 테더링 검사: 타겟이 한계선을 벗어났는지 확인
            if (Owner != null)
            {
                if (Target != null)
                {
                    float sqrDistTargetToOwner = ((Vector2)Owner.position - (Vector2)Target.position).sqrMagnitude;
                    if (sqrDistTargetToOwner > sqrMaxReach)
                    {
                        Debug.Log("[NetMonsterBrain] 타겟이 테더 범위를 벗어나 포기합니다.");
                        Target = null;
                    }
                }

                if (PriorityTarget != null)
                {
                    float sqrDistPriorityToOwner = ((Vector2)Owner.position - (Vector2)PriorityTarget.position).sqrMagnitude;
                    if (sqrDistPriorityToOwner > sqrMaxReach)
                    {
                        PriorityTarget = null;
                    }
                }
            }

            if (PriorityTarget != null)
            {
                if (!PriorityTarget.gameObject.activeInHierarchy)
                {
                    PriorityTarget = null;
                }
                else
                {
                    float sqrDist = ((Vector2)transform.position - (Vector2)PriorityTarget.position).sqrMagnitude;
                    float priorityThreshold = effectiveDetectRadius * priorityChaseMultiplier;
                    
                    if (sqrDist > priorityThreshold * priorityThreshold)
                    {
                        PriorityTarget = null;
                    }
                    else
                    {
                        Target = PriorityTarget;
                        return;
                    }
                }
            }

            if (Target != null)
            {
                if (!Target.gameObject.activeInHierarchy)
                {
                    Target = null;
                }
                else
                {
                    float sqrDist = ((Vector2)transform.position - (Vector2)Target.position).sqrMagnitude;
                    float threshold = effectiveDetectRadius * LOST_TARGET_MULTIPLIER;
                    if (sqrDist > threshold * threshold) // 탐지 거리 밖으로 벗어남
                    {
                        Target = null;
                    }
                }
            }

            if (Target != null)
            {
                return;
            }

            // ContactFilter2D를 이용한 최신 표준 탐색 API
            int count = Physics2D.OverlapCircle(transform.position, effectiveDetectRadius, enemyFilter, hitColliders);
            if (count > 0)
            {
                float minSqrDist = float.MaxValue;
                Transform closestTarget = null;
                Vector2 myPos = transform.position;

                for (int i = 0; i < count; i++)
                {
                    if (hitColliders[i].gameObject == gameObject)
                    {
                        continue; // 자기 자신 제외 (표준)
                    }

                    if (!string.IsNullOrEmpty(currentDetectTag) && !hitColliders[i].CompareTag(currentDetectTag))
                    {
                        continue; // 태그 교집합 필터링
                    }

                    if (Owner != null)
                    {
                        float sqrDistTargetToOwner = ((Vector2)Owner.position - (Vector2)hitColliders[i].transform.position).sqrMagnitude;
                        
                        if (sqrDistTargetToOwner > sqrMaxReach)
                        {
                            continue; // 테더 한계선 + 공격 사거리 밖의 적은 아예 무시
                        }
                    }

                    float sqrDist = (myPos - (Vector2)hitColliders[i].transform.position).sqrMagnitude;
                    if (sqrDist < minSqrDist)
                    {
                        minSqrDist = sqrDist;
                        closestTarget = hitColliders[i].transform;
                    }
                }
                
                Target = closestTarget;
            }
        }

        /// <summary>
        /// 몬스터 몸체의 이동 컴포넌트(NetServerMovement)에 명령을 하달합니다.
        /// </summary>
        public void SetMoveDirection(Vector2 direction)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetMonsterBrain] SetMoveDirection은 서버에서만 호출되어야 합니다.");

            if (!(Character.Movement is NetServerMovement serverMovement))
            {
                Debug.LogWarning("[NetMonsterBrain] SetMoveDirection: Character.Movement가 NetServerMovement가 아닙니다.");
                return;
            }

            serverMovement.SetDirection(direction);
        }

        /// <summary>
        /// 몬스터 몸체의 스킬(기본 공격 등) 발동을 지시합니다.
        /// </summary>
        public void ExecuteAttack()
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetMonsterBrain] ExecuteAttack은 서버에서만 호출되어야 합니다.");
            Assert.IsNotNull(Character, "[NetMonsterBrain] Character 컴포넌트가 null입니다.");
            
            if (Character.SkillComponent == null || Character.SkillComponent.OwnedSkills.Count == 0)
            {
                Debug.LogWarning("[NetMonsterBrain] ExecuteAttack: SkillComponent가 없거나 등록된 스킬이 없습니다.");
                return;
            }

            BaseSkillConfig skillToUse = Character.SkillComponent.OwnedSkills[0];
            if (skillToUse == null)
            {
                Debug.LogWarning("[NetMonsterBrain] ExecuteAttack: 첫 번째 스킬이 null입니다.");
                return;
            }

            Character.TryActivateSkill(skillToUse.SkillId);
        }
    }
}
