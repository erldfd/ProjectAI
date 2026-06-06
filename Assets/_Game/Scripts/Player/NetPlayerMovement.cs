using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace ProjectAI.Player
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
    [RequireComponent(typeof(Rigidbody2D))]
    public class NetPlayerMovement : NetworkBehaviour
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

        // 옵저버 보간용 변수
        private Vector2 observerTargetPosition;
        // private Vector2 observerTargetVelocity; // 향후 옵저버 외삽(Extrapolation) 기능 구현 시 사용 예정

        private Rigidbody2D rb;
        private Vector2 currentMoveInput;

        #region Unity Lifecycle
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            // 첫 RPC 수신 전까지 옵저버가 (0,0)으로 이동하는 현상 방지
            observerTargetPosition = rb.position;
        }

        private void FixedUpdate()
        {
            if (IsServer)
            {
                HandleServerTick();
            }

            if (IsOwner)
            {
                HandleClientTick();
            }
            else if (!IsServer)
            {
                HandleObserverTick();
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
                Position = rb.position,
                Velocity = rb.linearVelocity
            };
            
            clientStateBuffer[bufferIndex] = statePayload;
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
        private void SendInputServerRpc(SInputPayload inputPayload)
        {
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
                    // 이전 패킷의 속도를 수동으로 적용하여 중간 프레임 누락 방지
                    // TODO: 수동 가산 시 물리 벽 뚫림 방지를 위해 향후 Raycast(또는 BoxCast) 기반 충돌 검사 로직 추가 필요
                    rb.position += rb.linearVelocity * Time.fixedDeltaTime;
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
                    Position = rb.position,
                    Velocity = rb.linearVelocity
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

        private void HandleObserverTick()
        {
            // 시각적 보간은 자식의 VisualInterpolator가 전담하므로,
            // 물리 객체(콜라이더)는 서버가 알려준 최신 위치로 즉시 동기화(Snap).
            rb.position = observerTargetPosition;
            rb.linearVelocity = Vector2.zero; // 옵저버는 자체 물리 이동 금지
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        private void SendStateClientRpc(SStatePayload statePayload)
        {
            if (!IsOwner)
            {
                observerTargetPosition = statePayload.Position;
                // observerTargetVelocity = statePayload.Velocity;
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
            rb.position = statePayload.Position;
            rb.linearVelocity = statePayload.Velocity;
            
            ReSimulate(statePayload.SequenceId);
        }

        private void ReSimulate(int serverSequenceId)
        {
            // 서버 상태의 위치는 해당 시퀀스의 속도가 적용되기 '전' 상태이므로, 먼저 1프레임 가산해준다.
            rb.position += rb.linearVelocity * Time.fixedDeltaTime;

            int sequenceToReSimulate = serverSequenceId + 1;

            while (sequenceToReSimulate < currentSequenceId)
            {
                int index = (sequenceToReSimulate % BUFFER_SIZE + BUFFER_SIZE) % BUFFER_SIZE;
                SInputPayload input = clientInputBuffer[index];

                ApplyPhysics(input.InputVector);

                clientStateBuffer[index] = new SStatePayload
                {
                    SequenceId = sequenceToReSimulate,
                    Position = rb.position,
                    Velocity = rb.linearVelocity
                };
                
                // 재시뮬레이션 위치 갱신. (버퍼에 예측 상태를 기록한 후에 필수)
                // 1-Frame 오프셋 방지를 위해 HandleClientTick과 동일하게 가산 전 상태를 버퍼에 기록함.
                // TODO: 수동 가산 시 물리 벽 뚫림 방지를 위해 향후 Raycast(또는 BoxCast) 기반 충돌 검사 로직 추가 필요
                rb.position += rb.linearVelocity * Time.fixedDeltaTime;

                sequenceToReSimulate++;
            }
        }
        #endregion

        #region Public Methods
        public void SetMoveInput(Vector2 input)
        {
            currentMoveInput = input;
        }
        #endregion

        #region Private Methods
        private void ApplyPhysics(Vector2 inputVector)
        {
            rb.linearVelocity = inputVector * moveSpeed;
        }
        #endregion
    }
}
