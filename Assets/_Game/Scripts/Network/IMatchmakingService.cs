using System.Threading.Tasks;

namespace ProjectAI.Network
{
    /// <summary>
    /// 방 생성 및 참가 관련 네트워크 서비스를 추상화한 인터페이스입니다.
    /// </summary>
    public interface IMatchmakingService
    {
        Task<string> StartHostAsync();
        Task<bool> StartClientAsync(string joinData);
        void LeaveGame();
    }
}
