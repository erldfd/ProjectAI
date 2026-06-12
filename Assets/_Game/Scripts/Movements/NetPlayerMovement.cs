using UnityEngine.Scripting.APIUpdating;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.Assertions;
using ProjectAI.Core;

namespace ProjectAI.Movements
{
    /// <summary>
    /// 클라이언트에서 서버로 보내는 입력 데이터 페이로드
    /// </summary>
    public struct SInputPayload : INetworkSerializable
    {
        public int SequenceId;
        public Vector2 InputVector;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref SequenceId);
            serializer.SerializeValue(ref InputVector);
        }
    }

    /// <summary>
    /// 서버에서 클라이언트로 보내는 상태 데이터 페이로드
    /// </summary>
    public struct SStatePayload : INetworkSerializable
    {
        public int SequenceId;
        public Vector2 Position;
        public Vector2 Velocity;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref SequenceId);
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Velocity);
        }
    }

    /// <summary>
    /// Rigidbody2D를 사용한 캐릭터의 물리적 이동을 전담하는 클래스입니다.
    /// </summary>
    [MovedFrom(true, "ProjectAI.Player", "Assembly-CSharp", "NetPlayerMovement")]
    public class NetPlayerMovement : ANetMovement
    {
        private const int BUFFER_SIZE = 1024;

        [Header("Movement Settings")]
        [Tooltip("캐릭터의 기본 이동 속도")]
        [SerializeField]
        private float moveSpeed = 5f;

        [Header("Network Settings")]
        [Tooltip("오차가 이 값보다 작으면 무시 (아주 작은 오차)")]
        [SerializeField]
        private float verySmallReconciliationThreshold = 0.01f;

        [Tooltip("오차가 이 값보다 크면 텔레포트로 간주하고 강제 동기화만 수행 (ReSimulate 생략)")]
        [SerializeField]
        private float teleportReconciliationThreshold = 2.0f;

        // TODO: 향후 시각적 객체 분리 시 보간/스냅 분기 기준값으로 사용
        // [Tooltip("오차가 이 값보다 작으면 강제 동기화 (Snap)")]
        // [SerializeField]
        // private float smallReconciliationThreshold = 0.05f;

        // [Tooltip("오차가 이 값보다 크면 강제 동기화 (Snap)")]
        // [SerializeField]
        // private float largeReconciliationThreshold = 2.0f;

        private SInputPayload[] clientInputBuffer = new SInputPayload[BUFFER_SIZE];
        private SStatePayload[] clientStateBuffer = new SStatePayload[BUFFER_SIZE];

        private Queue<SInputPayload> serverInputQueue = new Queue<SInputPayload>();

        private int currentSequenceId = 0;

        private Vector2 currentMoveInput;
        private float currentMoveSpeedModifier = 1f;

        private ContactFilter2D physicsFilter;
        private RaycastHit2D[] physicsHits = new RaycastHit2D[1];

        public override Vector2 Velocity => base.Rb.linearVelocity;

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            Assert.IsNotNull(base.Rb, "Rigidbody2D component is missing in parent.");

            physicsFilter = new ContactFilter2D();
            physicsFilter.useTriggers = false;
            physicsFilter.useLayerMask = true;
            physicsFilter.layerMask = Physics2D.GetLayerCollisionMask(gameObject.layer);
        }

        private void OnEnable()
        {
            base._entityEvents.OnMoveSpeedModifierChanged += HandleMoveSpeedModifierChanged;
        }

        private void OnDisable()
        {
            base._entityEvents.OnMoveSpeedModifierChanged -= HandleMoveSpeedModifierChanged;
        }

        private void HandleMoveSpeedModifierChanged(float modifier)
        {
            currentMoveSpeedModifier = modifier;
        }

        private void FixedUpdate()
        {
            if (GameStatics.IsServerAuthorized)
            {
                HandleServerTick();
            }

            if (IsOwner)
            {
                HandleClientTick();
            }

            currentSequenceId++;
        }
        #endregion

        #region Client Prediction
        private void HandleClientTick()
        {
            int bufferIndex = (currentSequenceId % BUFFER_SIZE + BUFFER_SIZE) % BUFFER_SIZE;
            
            SInputPayload inputPayload = new SInputPayload
            {
                SequenceId = currentSequenceId,
                InputVector = currentMoveInput
            };
            
            clientInputBuffer[bufferIndex] = inputPayload;

            SendInputServerRpc(inputPayload);

            ApplyPhysics(inputPayload.InputVector);

            SStatePayload statePayload = new SStatePayload
            {
                SequenceId = currentSequenceId,
                Position = base.Rb.position,
                Velocity = base.Rb.linearVelocity
            };
            
            clientStateBuffer[bufferIndex] = statePayload;
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
        private void SendInputServerRpc(SInputPayload inputPayload)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetPlayerMovement] SendInputServerRpc는 서버에서만 실행되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }
            
            serverInputQueue.Enqueue(inputPayload);
        }
        #endregion

        #region Server Logic
        private void HandleServerTick()
        {
            int lastProcessedIndex = -1;
            int processedCount = 0;

            while (serverInputQueue.Count > 0)
            {
                SInputPayload inputPayload = serverInputQueue.Dequeue();
                
                if (processedCount > 0 && !IsOwner)
                {
                    // 이전 패킷의 속도를 수동으로 적용하여 중간 프레임 누락 방지 (물리 벽 뚫림 방지 적용)
                    ApplyManualVelocity();
                }
                
                int bufferIndex = (inputPayload.SequenceId % BUFFER_SIZE + BUFFER_SIZE) % BUFFER_SIZE;
                clientInputBuffer[bufferIndex] = inputPayload;
                
                if (!IsOwner)
                {
                    ApplyPhysics(inputPayload.InputVector);
                }

                SStatePayload statePayload = new SStatePayload
                {
                    SequenceId = inputPayload.SequenceId,
                    Position = base.Rb.position,
                    Velocity = base.Rb.linearVelocity
                };

                clientStateBuffer[bufferIndex] = statePayload;
                lastProcessedIndex = bufferIndex;
                processedCount++;
            }

            if (lastProcessedIndex != -1)
            {
                SendStateClientRpc(clientStateBuffer[lastProcessedIndex]);
            }
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        private void SendStateClientRpc(SStatePayload statePayload)
        {
            if (!IsOwner)
            {
                // 옵저버 처리: 서버 상태를 수신하는 즉시 적용.
                // 이후 FixedUpdate 사이클 동안 물리 엔진이 선형 속도를 기반으로 자연스럽게 외삽(Dead Reckoning)함.
                base.Rb.position = statePayload.Position;
                base.Rb.linearVelocity = statePayload.Velocity;
                return;
            }

            int bufferIndex = (statePayload.SequenceId % BUFFER_SIZE + BUFFER_SIZE) % BUFFER_SIZE;
            SStatePayload clientState = clientStateBuffer[bufferIndex];

            float sqrDistance = (clientState.Position - statePayload.Position).sqrMagnitude;

            if (sqrDistance < verySmallReconciliationThreshold * verySmallReconciliationThreshold)
            {
                // 매우 작은 오차 무시
                return;
            }
            
            // 시각적 객체(VisualBody)가 분리되었으므로, 물리 객체는
            // 오차 발생 시 무조건 서버 위치로 즉시 강제 이동(Snap)시킴.
            base.Rb.position = statePayload.Position;
            base.Rb.linearVelocity = statePayload.Velocity;
            
            if (sqrDistance > teleportReconciliationThreshold * teleportReconciliationThreshold)
            {
                // 서버 강제 텔레포트로 간주. ReSimulate 생략
                return;
            }

            ReSimulate(statePayload.SequenceId);
        }

        private void ReSimulate(int serverSequenceId)
        {
            // 서버 상태의 위치는 해당 시퀀스의 속도가 적용되기 '전' 상태이므로, 먼저 1프레임 가산해준다.
            base.Rb.position += base.Rb.linearVelocity * Time.fixedDeltaTime;

            int sequenceToReSimulate = serverSequenceId + 1;

            while (sequenceToReSimulate < currentSequenceId)
            {
                int index = (sequenceToReSimulate % BUFFER_SIZE + BUFFER_SIZE) % BUFFER_SIZE;
                SInputPayload input = clientInputBuffer[index];

                ApplyPhysics(input.InputVector);

                clientStateBuffer[index] = new SStatePayload
                {
                    SequenceId = sequenceToReSimulate,
                    Position = base.Rb.position,
                    Velocity = base.Rb.linearVelocity
                };
                
                // 재시뮬레이션 위치 갱신. (버퍼에 예측 상태를 기록한 후에 필수)
                // 1-Frame 오프셋 방지를 위해 HandleClientTick과 동일하게 가산 전 상태를 버퍼에 기록함.
                ApplyManualVelocity();

                sequenceToReSimulate++;
            }
        }
        #endregion

        #region Public Methods
        public void SetMoveInput(Vector2 input)
        {
            if (!IsOwner)
            {
                return;
            }

            // 스피드핵(비정상적으로 큰 입력값 주입) 및 대각선 가속(루트 2 배속) 방지용 안전장치
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            // 좌우 이동에 따른 바라보는 방향 갱신 (위/아래 이동시는 기존 방향 유지)
            if (input.x > 0.01f)
            {
                base.NetIsFacingRight.Value = true;
            }
            else if (input.x < -0.01f)
            {
                base.NetIsFacingRight.Value = false;
            }

            currentMoveInput = input;
            base.NetAnimVelocity.Value = input * (moveSpeed * currentMoveSpeedModifier);
        }
        #endregion

        #region Private Methods
        private void ApplyPhysics(Vector2 inputVector)
        {
            base.Rb.linearVelocity = inputVector * (moveSpeed * currentMoveSpeedModifier);
        }

        private void ApplyManualVelocity()
        {
            Vector2 moveAmount = base.Rb.linearVelocity * Time.fixedDeltaTime;

            if (moveAmount.sqrMagnitude < Mathf.Epsilon)
            {
                return;
            }

            int hitCount = base.Rb.Cast(moveAmount.normalized, physicsFilter, physicsHits, moveAmount.magnitude);

            if (hitCount == 0)
            {
                base.Rb.position += moveAmount;
            }
            else
            {
                float safeDistance = Mathf.Max(0f, physicsHits[0].distance - 0.01f);
                base.Rb.position += moveAmount.normalized * safeDistance;
            }
        }
        #endregion
    }
}
