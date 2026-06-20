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
    /// 몬스터의 FSM(상태 기계)을 돌리며 판단을 내리고 몸체(NetCharacter)를 조종하는 기본 두뇌 클래스입니다.
    /// 모든 연산은 서버(Host)에서만 구동됩니다.
    /// </summary>
    [RequireComponent(typeof(NetCharacter))]
    public class NetMonsterBrain : NetworkBehaviour
    {
        protected const float SENSOR_UPDATE_INTERVAL = 0.15f;
        protected const int MAX_COLLIDER_RESULTS = 10;
        protected const float LOST_TARGET_MULTIPLIER = 1.5f;

        public NetCharacter Character { get; protected set; }
        public AIStateMachine StateMachine { get; protected set; }

        public Transform Target { get; set; }
        
        public float AttackRadius => attackRadius;
        public bool IsSensorEnabled 
        { 
            get => isSensorEnabled; 
            set => isSensorEnabled = value; 
        }

        [Header("AI Sensors")]
        [Tooltip("초기 시작 상태 (비워두면 첫 번째 상태가 기본값이 됩니다)")]
        [SerializeField]
        protected AMonsterState startingState;

        [Tooltip("센서(탐지) 작동 여부")]
        [SerializeField]
        protected bool isSensorEnabled = true;

        [Tooltip("적군을 식별할 기본 레이어 마스크")]
        [SerializeField]
        protected LayerMask detectLayer;

        [Tooltip("비워두면 모든 태그(All)를 감지합니다.")]
        [TagSelector]
        [SerializeField]
        protected string detectTag = ObjectTags.ALL;

        [Tooltip("탐지 반경")]
        [SerializeField]
        protected float detectionRadius = 8f;

        [Tooltip("공격 사거리")]
        [SerializeField]
        protected float attackRadius = 1.5f;

        protected AMonsterState[] stateComponents;
        protected Collider2D[] hitColliders = new Collider2D[MAX_COLLIDER_RESULTS];
        protected ContactFilter2D enemyFilter;

        protected float sensorTimer = 0f;

        protected LayerMask currentDetectLayer;
        protected string currentDetectTag;
        protected float currentDetectRadius;

        protected virtual void Awake()
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

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            Target = null;
            sensorTimer = 0f;
            ResetSensor();

            if (!GameStatics.IsServerAuthorized)
            {
                Debug.Log($"[NetMonsterBrain] 클라이언트이므로 AI 뇌를 비활성화합니다. (ID: {NetworkObjectId})");
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

        protected virtual void Update()
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

        protected virtual void UpdateSensors()
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetMonsterBrain] UpdateSensors는 서버에서만 호출되어야 합니다.");

            if (Target != null)
            {
                if (!Target.gameObject.activeInHierarchy)
                {
                    Target = null;
                }
                else
                {
                    Vector2 diff = (Vector2)transform.position - (Vector2)Target.position;
                    float sqrDist = GameStatics.GetPerspectiveSqrMagnitude(diff);
                    float threshold = currentDetectRadius * LOST_TARGET_MULTIPLIER;
                    if (sqrDist > threshold * threshold)
                    {
                        Target = null;
                    }
                }
            }

            if (Target != null)
            {
                return;
            }

            int count = Physics2D.OverlapCircle(transform.position, currentDetectRadius, enemyFilter, hitColliders);
            if (count > 0)
            {
                float minSqrDist = float.MaxValue;
                Transform closestTarget = null;
                Vector2 myPos = transform.position;

                for (int i = 0; i < count; i++)
                {
                    if (hitColliders[i].gameObject == gameObject)
                    {
                        continue;
                    }

                    // 태그가 All이 아니면서 대상 태그와 일치하지 않을 경우에만 타겟에서 제외(무시)
                    if (!string.IsNullOrEmpty(currentDetectTag) && currentDetectTag != ObjectTags.ALL && !hitColliders[i].CompareTag(currentDetectTag))
                    {
                        continue;
                    }

                    Vector2 diff = myPos - (Vector2)hitColliders[i].transform.position;
                    float sqrDist = GameStatics.GetPerspectiveSqrMagnitude(diff);
                    if (sqrDist < minSqrDist)
                    {
                        minSqrDist = sqrDist;
                        closestTarget = hitColliders[i].transform;
                    }
                }
                
                Target = closestTarget;
            }
        }

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

        /// <summary>
        /// 목적지를 향해 이동 방향(벡터)을 반환하는 조향(Steering) 헬퍼 함수입니다.
        /// 도착 거리 내에 진입하면 서서히 감속하는 자연스러운 멈춤(Arrive) 효과를 제공합니다.
        /// 반환된 벡터의 길이가 1보다 작아지면 NetServerMovement에서 이를 받아 느리게 이동시킵니다.
        /// </summary>
        public Vector2 GetArriveDirection(Vector2 myPos, Vector2 targetPos, float stopDistance, float slowDownRadius)
        {
            Vector2 diff = targetPos - myPos;
            // 거리 계산에만 왜곡(DepthScale)을 적용하여 Arrive 여부를 판단
            float dist = GameStatics.GetPerspectiveMagnitude(diff);

            if (dist <= stopDistance)
            {
                return Vector2.zero;
            }

            Vector2 desiredVelocity = diff.normalized;

            // 감속 반경 안에 들어왔다면, 거리에 비례하여 벡터 길이를 1에서 0으로 부드럽게 줄임
            if (dist < slowDownRadius)
            {
                float speedRatio = (dist - stopDistance) / (slowDownRadius - stopDistance);
                desiredVelocity *= Mathf.Clamp01(speedRatio);
            }

            return desiredVelocity;
        }
    }
}
