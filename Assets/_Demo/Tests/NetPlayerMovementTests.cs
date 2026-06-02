using NUnit.Framework;
using UnityEngine;
using Unity.Netcode;
using System.Reflection;

namespace PortalBroke.Player.Tests
{
    /// <summary>
    /// NetPlayerMovement의 핵심 로직을 검증하기 위한 EditMode 기반의 정적/단위 테스트입니다.
    /// PlayMode 전환 및 NetworkManager 세팅 없이 독립적 로직 검증에 집중합니다.
    /// </summary>
    public class NetPlayerMovementTests
    {
        private GameObject playerGo;
        private NetPlayerMovement netPlayerMovement;
        private Rigidbody2D rb;

        [SetUp]
        public void Setup()
        {
            playerGo = new GameObject("TestPlayer");
            rb = playerGo.AddComponent<Rigidbody2D>();
            netPlayerMovement = playerGo.AddComponent<NetPlayerMovement>();

            // 리플렉션을 통해 private 설정값 초기화 (MoveSpeed)
            FieldInfo moveSpeedField = typeof(NetPlayerMovement).GetField("moveSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
            moveSpeedField?.SetValue(netPlayerMovement, 5f);
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void SetMoveInput_UpdatesCurrentMoveInput_Successfully()
        {
            // Arrange
            Vector2 expectedInput = new Vector2(1f, 1f).normalized;

            // Act
            netPlayerMovement.SetMoveInput(expectedInput);

            // Assert
            FieldInfo inputField = typeof(NetPlayerMovement).GetField("currentMoveInput", BindingFlags.NonPublic | BindingFlags.Instance);
            Vector2 actualInput = (Vector2)inputField.GetValue(netPlayerMovement);
            Assert.AreEqual(expectedInput, actualInput, "입력된 방향 벡터가 올바르게 저장되어야 합니다.");
        }

        [Test]
        public void ApplyPhysics_SetsVelocityCorrectly()
        {
            // Arrange
            Vector2 inputVector = new Vector2(0f, 1f);
            MethodInfo applyPhysicsMethod = typeof(NetPlayerMovement).GetMethod("ApplyPhysics", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            applyPhysicsMethod.Invoke(netPlayerMovement, new object[] { inputVector });

            // Assert
            Assert.AreEqual(new Vector2(0f, 5f), rb.linearVelocity, "ApplyPhysics 호출 시 velocity가 moveSpeed에 비례하게 설정되어야 합니다.");
        }
    }
}
