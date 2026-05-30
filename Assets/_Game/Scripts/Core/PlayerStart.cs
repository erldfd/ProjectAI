using UnityEngine;

namespace PortalBroke.Core
{
    public class PlayerStart : MonoBehaviour
    {
        [Tooltip("씬 이동 후 특정 포탈에서 올 때 사용할 고유 스폰 식별자")]
        [SerializeField]
        private string spawnPointID = "";

        public string SpawnPointID => spawnPointID;
    }
}
