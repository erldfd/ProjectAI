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
    public class RelayMatchmakingService : IMatchmakingService
    {
        private const int MaxPlayers = 4; // 호스트 포함 최대 인원

        public async Task<bool> StartHostAsync()
        {
            try
            {
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
                    return false;
                }

                UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport == null)
                {
                    Debug.LogError("[RelayMatchmakingService] UnityTransport component is missing on NetworkManager.");
                    return false;
                }

                transport.SetHostRelayData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData
                );

                // 4. 호스트 시작
                return NetworkManager.Singleton.StartHost();
            }
            catch (Exception e)
            {
                Debug.LogError($"[RelayMatchmakingService] StartHostAsync 실패: {e.Message}");
                return false;
            }
        }

        public async Task<bool> StartClientAsync(string joinData)
        {
            try
            {
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
