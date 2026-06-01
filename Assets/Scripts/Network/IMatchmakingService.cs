using System.Threading.Tasks;

namespace PortalBroke.Network
{
    public interface IMatchmakingService
    {
        Task<string> StartHostAsync();
        Task<bool> StartClientAsync(string joinData);
        void LeaveGame();
    }
}
