using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace PortalBroke.Network
{
    /// <summary>
    /// Unity Relay 서버를 통해 호스트 생성 및 클라이언트 접속을 처리하는 실제 서비스 구현체입니다.
    /// </summary>
    public class RelayMatchmakingService : IMatchmakingService
    {
        private const int MaxPlayers = 4; // 호스트 포함 최대 인원
        private const int ShutdownTimeoutMs = 3000; // 셧다운 대기 타임아웃(밀리초)

        public async Task<string> StartHostAsync()
        {
            try
            {
                UnityEngine.Assertions.Assert.IsNotNull(NetworkManager.Singleton, "NetworkManager.Singleton is null");

                if (NetworkManager.Singleton.IsListening)
                {
                    Debug.LogWarning("[RelayMatchmakingService] 이미 네트워크에 연결되어 있습니다. (Host 또는 Client 실행 중)");
                    return null;
                }

                await EnsureAuthenticatedAsync();

                // 1. 릴레이 서버 할당 요청 (호스트 제외 인원수 전달)
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers - 1);

                // 2. 조인 코드 발급
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                Debug.Log($"[RelayMatchmakingService] 방 생성 성공! Join Code: {joinCode}");

                // 3. NetworkManager에 Relay 서버 정보 설정
                if (NetworkManager.Singleton == null)
                {
                    Debug.LogError("[RelayMatchmakingService] NetworkManager.Singleton is null.");
                    return null;
                }

                UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport == null)
                {
                    Debug.LogError("[RelayMatchmakingService] UnityTransport component is missing on NetworkManager.");
                    return null;
                }

                transport.SetHostRelayData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData
                );

                // 4. 호스트 시작
                if (NetworkManager.Singleton.StartHost())
                {
                    return joinCode;
                }
                
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RelayMatchmakingService] StartHostAsync 실패: {e.Message}");
                return null;
            }
        }

        public async Task<bool> StartClientAsync(string joinData)
        {
            try
            {
                UnityEngine.Assertions.Assert.IsNotNull(NetworkManager.Singleton, "NetworkManager.Singleton is null");

                if (NetworkManager.Singleton.IsListening)
                {
                    Debug.Log("[RelayMatchmakingService] 기존 방 연결을 종료하고 새로운 방에 접속합니다.");
                    LeaveGame();

                    // NGO의 Shutdown이 완전히 끝날 때까지 대기해야 다음 연결이 꼬이지 않습니다.
                    // 다만 Shutdown 버그로 인해 무한 루프(Soft Lock)에 빠지는 것을 막기 위해 CancellationToken을 사용합니다.
                    // ShutdownTimeoutMs(3초) 뒤에 자동으로 '취소 신호'를 발생시키는 토큰 객체를 생성합니다.
                    using (System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource(ShutdownTimeoutMs))
                    {
                        try
                        {
                            while (NetworkManager.Singleton != null && 
                                   (NetworkManager.Singleton.IsListening || NetworkManager.Singleton.ShutdownInProgress))
                            {
                                // 매 프레임마다 취소 신호가 왔는지(3초가 지났는지) 확인합니다.
                                // 만약 3초가 지났다면 즉시 OperationCanceledException 에러를 던져 루프 탈출합니다.
                                cts.Token.ThrowIfCancellationRequested();
                                await System.Threading.Tasks.Task.Yield();
                            }
                        }
                        catch (System.OperationCanceledException)
                        {
                            // 3초가 지나 타임아웃 예외가 발생했을 때 이 곳으로 빠져나옵니다.
                            Debug.LogError("[RelayMatchmakingService] 기존 방 종료 대기 타임아웃(3초) 발생. 접속을 취소합니다.");
                            return false;
                        }
                    }
                }

                await EnsureAuthenticatedAsync();

                // 1. 조인 코드로 릴레이 서버 접속 요청
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinData);

                // 2. NetworkManager에 Relay 서버 정보 설정
                if (NetworkManager.Singleton == null)
                {
                    Debug.LogError("[RelayMatchmakingService] NetworkManager.Singleton is null.");
                    return false;
                }

                UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport == null)
                {
                    Debug.LogError("[RelayMatchmakingService] UnityTransport component is missing on NetworkManager.");
                    return false;
                }

                transport.SetClientRelayData(
                    joinAllocation.RelayServer.IpV4,
                    (ushort)joinAllocation.RelayServer.Port,
                    joinAllocation.AllocationIdBytes,
                    joinAllocation.Key,
                    joinAllocation.ConnectionData,
                    joinAllocation.HostConnectionData
                );

                // 3. 클라이언트 시작
                return NetworkManager.Singleton.StartClient();
            }
            catch (Exception e)
            {
                Debug.LogError($"[RelayMatchmakingService] StartClientAsync 실패: {e.Message}");
                return false;
            }
        }

        public void LeaveGame()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }

        private async Task EnsureAuthenticatedAsync()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[RelayMatchmakingService] 익명 로그인 완료. Player ID: {AuthenticationService.Instance.PlayerId}");
            }
        }
    }
}
