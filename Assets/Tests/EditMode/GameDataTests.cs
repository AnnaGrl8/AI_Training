using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GameSystemsCookbook;
using GameSystemsCookbook.Demos.PaddleBall;

namespace GameSystemsCookbook.Tests
{
    /// <summary>
    /// Tests for GameDataSO: default values, property exposure, player identification,
    /// and physics parameter constraints.
    /// </summary>
    public class GameDataTests
    {
        private GameDataSO m_GameData;
        private PlayerIDSO m_Player1;
        private PlayerIDSO m_Player2;

        [SetUp]
        public void SetUp()
        {
            m_Player1 = ScriptableObject.CreateInstance<PlayerIDSO>();
            m_Player1.name = "Player1_SO";
            m_Player2 = ScriptableObject.CreateInstance<PlayerIDSO>();
            m_Player2.name = "Player2_SO";

            LogAssert.ignoreFailingMessages = true;
            m_GameData = ScriptableObject.CreateInstance<GameDataSO>();
            LogAssert.ignoreFailingMessages = false;

            var so = new UnityEditor.SerializedObject(m_GameData);
            so.FindProperty("m_Player1").objectReferenceValue = m_Player1;
            so.FindProperty("m_Player2").objectReferenceValue = m_Player2;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_GameData);
            Object.DestroyImmediate(m_Player1);
            Object.DestroyImmediate(m_Player2);
        }

        #region Default Values

        [Test]
        public void DefaultPaddleSpeed_Is80()
        {
            Assert.AreEqual(80f, m_GameData.PaddleSpeed);
        }

        [Test]
        public void DefaultPaddleDrag_Is10()
        {
            Assert.AreEqual(10f, m_GameData.PaddleLinearDrag);
        }

        [Test]
        public void DefaultPaddleMass_Is05()
        {
            Assert.AreEqual(0.5f, m_GameData.PaddleMass);
        }

        [Test]
        public void DefaultBallSpeed_Is200()
        {
            Assert.AreEqual(200f, m_GameData.BallSpeed);
        }

        [Test]
        public void DefaultBallMaxSpeed_Is300()
        {
            Assert.AreEqual(300f, m_GameData.MaxSpeed);
        }

        [Test]
        public void DefaultBounceMultiplier_Is11()
        {
            Assert.AreEqual(1.1f, m_GameData.BounceMultiplier);
        }

        [Test]
        public void DefaultDelay_Is1()
        {
            Assert.AreEqual(1f, m_GameData.DelayBetweenPoints);
        }

        #endregion

        #region Player Identification

        [Test]
        public void IsPlayer1_WithPlayer1ID_ReturnsTrue()
        {
            Assert.IsTrue(m_GameData.IsPlayer1(m_Player1));
        }

        [Test]
        public void IsPlayer1_WithPlayer2ID_ReturnsFalse()
        {
            Assert.IsFalse(m_GameData.IsPlayer1(m_Player2));
        }

        [Test]
        public void IsPlayer2_WithPlayer2ID_ReturnsTrue()
        {
            Assert.IsTrue(m_GameData.IsPlayer2(m_Player2));
        }

        [Test]
        public void IsPlayer2_WithPlayer1ID_ReturnsFalse()
        {
            Assert.IsFalse(m_GameData.IsPlayer2(m_Player1));
        }

        [Test]
        public void IsPlayer1_WithNull_ReturnsFalse()
        {
            Assert.IsFalse(m_GameData.IsPlayer1(null));
        }

        [Test]
        public void IsPlayer2_WithNull_ReturnsFalse()
        {
            Assert.IsFalse(m_GameData.IsPlayer2(null));
        }

        [Test]
        public void IsPlayer1_WithUnknownID_ReturnsFalse()
        {
            var unknown = ScriptableObject.CreateInstance<PlayerIDSO>();
            Assert.IsFalse(m_GameData.IsPlayer1(unknown));
            Object.DestroyImmediate(unknown);
        }

        [Test]
        public void Player1And2_AreDifferentInstances()
        {
            Assert.AreNotSame(m_GameData.Player1, m_GameData.Player2);
        }

        #endregion

        #region Property Exposure

        [Test]
        public void Player1Property_ReturnsAssignedID()
        {
            Assert.AreEqual(m_Player1, m_GameData.Player1);
        }

        [Test]
        public void Player2Property_ReturnsAssignedID()
        {
            Assert.AreEqual(m_Player2, m_GameData.Player2);
        }

        [Test]
        public void OptionalSprites_DefaultToNull()
        {
            Assert.IsNull(m_GameData.P1Sprite);
            Assert.IsNull(m_GameData.P2Sprite);
        }

        [Test]
        public void LevelLayout_DefaultsToNull()
        {
            Assert.IsNull(m_GameData.LevelLayout);
        }

        #endregion

        #region Custom Values

        [Test]
        public void CustomPaddleSpeed_ReturnsSetValue()
        {
            var so = new UnityEditor.SerializedObject(m_GameData);
            so.FindProperty("m_PaddleSpeed").floatValue = 120f;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(120f, m_GameData.PaddleSpeed);
        }

        [Test]
        public void CustomBallSpeed_ReturnsSetValue()
        {
            var so = new UnityEditor.SerializedObject(m_GameData);
            so.FindProperty("m_BallSpeed").floatValue = 350f;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(350f, m_GameData.BallSpeed);
        }

        [Test]
        public void BallMaxSpeed_GreaterThanOrEqualToBallSpeed()
        {
            // Verify the default invariant holds
            Assert.GreaterOrEqual(m_GameData.MaxSpeed, m_GameData.BallSpeed);
        }

        [Test]
        public void BounceMultiplier_IsPositive()
        {
            Assert.Greater(m_GameData.BounceMultiplier, 0f);
        }

        [Test]
        public void PaddleMass_IsPositive()
        {
            Assert.Greater(m_GameData.PaddleMass, 0f);
        }

        #endregion
    }
}
