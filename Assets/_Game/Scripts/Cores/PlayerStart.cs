using UnityEngine;

namespace ProjectAI.Core
{
    /// <summary>
    /// 플레이어 스폰 위치를 지정하는 고유 식별자 컴포넌트입니다.
    /// </summary>
    public class PlayerStart : MonoBehaviour
    {
        [Tooltip("씬 이동 후 특정 포탈에서 올 때 사용할 고유 스폰 식별자")]
        [SerializeField]
        private string spawnPointID = "";

        public string SpawnPointID => spawnPointID;
    }
}
