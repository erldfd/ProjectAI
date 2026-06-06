namespace ProjectAI.Core
{
    /// <summary>
    /// 씬 이동 간에 서버(방장)가 임시로 보관해야 할 메타데이터를 담는 정적 클래스입니다.
    /// </summary>
    public static class SceneTransitionData
    {
        /// <summary>
        /// 다음 씬이 로드되었을 때, 파티원들이 텔레포트할 목표 PlayerStart의 ID입니다.
        /// 빈 문자열일 경우 기본(첫 번째) 스폰 포인트를 사용합니다.
        /// </summary>
        public static string NextSpawnPointID { get; set; } = "";

        /// <summary>
        /// ID 매칭 대신 강제로 좌표(Raw Coordinates)를 사용할지 여부입니다.
        /// </summary>
        public static bool UseRawCoordinates { get; set; } = false;

        /// <summary>
        /// UseRawCoordinates가 true일 때 플레이어들이 스폰될 월드 좌표입니다.
        /// </summary>
        public static UnityEngine.Vector2 RawTargetPosition { get; set; } = UnityEngine.Vector2.zero;
    }
}
