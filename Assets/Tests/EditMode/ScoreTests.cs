using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GameSystemsCookbook;
using GameSystemsCookbook.Demos.PaddleBall;

namespace GameSystemsCookbook.Tests
{
    /// <summary>
    /// EditMode tests for the scoring system: Score value object, ScoreObjectiveSO win condition.
    /// </summary>
    public class ScoreTests
    {
        #region Score

        [Test]
        public void Score_InitialValue_IsZero()
        {
            var score = new Score();
            Assert.AreEqual(0, score.Value);
        }

        [Test]
        public void Score_Increment_IncreasesValueByOne()
        {
            var score = new Score();
            score.IncrementScore();
            Assert.AreEqual(1, score.Value);
        }

        [Test]
        public void Score_MultipleIncrements_AccumulatesCorrectly()
        {
            var score = new Score();
            for (int i = 0; i < 5; i++)
                score.IncrementScore();

            Assert.AreEqual(5, score.Value);
        }

        [Test]
        public void Score_Reset_SetsValueToZero()
        {
            var score = new Score();
            score.IncrementScore();
            score.IncrementScore();
            score.ResetScore();
            Assert.AreEqual(0, score.Value);
        }

        [Test]
        public void Score_ResetThenIncrement_StartsFromZero()
        {
            var score = new Score();
            score.IncrementScore();
            score.IncrementScore();
            score.IncrementScore();
            score.ResetScore();
            score.IncrementScore();
            Assert.AreEqual(1, score.Value);
        }

        #endregion

        #region ScoreObjectiveSO

        private ScoreObjectiveSO m_Objective;
        private ScoreListEventChannelSO m_ScoreUpdatedChannel;
        private PlayerScoreEventChannelSO m_TargetScoreReachedChannel;
        private VoidEventChannelSO m_ObjectiveCompletedChannel;

        /// <summary>
        /// Creates a ScoreObjectiveSO with all dependencies wired up.
        /// ScriptableObject.CreateInstance calls OnEnable automatically before fields are set,
        /// so we suppress the expected errors, set fields via SerializedObject, then manually
        /// invoke OnEnable via reflection to subscribe to the event channel.
        /// </summary>
        private ScoreObjectiveSO CreateObjective(int targetScore)
        {
            m_ScoreUpdatedChannel = ScriptableObject.CreateInstance<ScoreListEventChannelSO>();
            m_TargetScoreReachedChannel = ScriptableObject.CreateInstance<PlayerScoreEventChannelSO>();
            m_ObjectiveCompletedChannel = ScriptableObject.CreateInstance<VoidEventChannelSO>();

            // CreateInstance triggers OnEnable before fields are set — expect the NullRef and validation errors
            LogAssert.ignoreFailingMessages = true;
            var objective = ScriptableObject.CreateInstance<ScoreObjectiveSO>();
            LogAssert.ignoreFailingMessages = false;

            // Set private serialized fields via SerializedObject
            var so = new UnityEditor.SerializedObject(objective);
            so.FindProperty("m_TargetScore").intValue = targetScore;
            so.FindProperty("m_ScoreManagerUpdated").objectReferenceValue = m_ScoreUpdatedChannel;
            so.FindProperty("m_TargetScoreReached").objectReferenceValue = m_TargetScoreReachedChannel;
            so.FindProperty("m_ObjectiveCompleted").objectReferenceValue = m_ObjectiveCompletedChannel;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Now that fields are set, invoke OnEnable to subscribe to the event channel
            MethodInfo onEnable = typeof(ScoreObjectiveSO).GetMethod("OnEnable",
                BindingFlags.Instance | BindingFlags.NonPublic);
            onEnable.Invoke(objective, null);

            return objective;
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Objective != null)
            {
                // Unsubscribe before destroying
                MethodInfo onDisable = typeof(ScoreObjectiveSO).GetMethod("OnDisable",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                onDisable.Invoke(m_Objective, null);
                Object.DestroyImmediate(m_Objective);
            }
            if (m_ScoreUpdatedChannel != null) Object.DestroyImmediate(m_ScoreUpdatedChannel);
            if (m_TargetScoreReachedChannel != null) Object.DestroyImmediate(m_TargetScoreReachedChannel);
            if (m_ObjectiveCompletedChannel != null) Object.DestroyImmediate(m_ObjectiveCompletedChannel);
        }

        [Test]
        public void ScoreObjective_BelowTarget_DoesNotComplete()
        {
            m_Objective = CreateObjective(3);

            var score = new Score();
            score.IncrementScore(); // 1 of 3

            bool wasReached = false;
            m_TargetScoreReachedChannel.OnEventRaised += _ => wasReached = true;

            var playerScores = new List<PlayerScore>
            {
                new PlayerScore { score = score }
            };

            m_ScoreUpdatedChannel.RaiseEvent(playerScores);

            Assert.IsFalse(wasReached);
            Assert.IsFalse(m_Objective.IsCompleted);
        }

        [Test]
        public void ScoreObjective_ReachesTarget_RaisesEvent()
        {
            m_Objective = CreateObjective(2);

            var score = new Score();
            score.IncrementScore();
            score.IncrementScore(); // 2 of 2

            PlayerScore receivedPlayerScore = default;
            m_TargetScoreReachedChannel.OnEventRaised += ps => receivedPlayerScore = ps;

            var playerScores = new List<PlayerScore>
            {
                new PlayerScore { score = score }
            };

            m_ScoreUpdatedChannel.RaiseEvent(playerScores);

            Assert.AreEqual(2, receivedPlayerScore.score.Value);
            Assert.IsTrue(m_Objective.IsCompleted);
        }

        [Test]
        public void ScoreObjective_ExceedsTarget_StillCompletes()
        {
            m_Objective = CreateObjective(2);

            var score = new Score();
            for (int i = 0; i < 5; i++)
                score.IncrementScore(); // 5, well past target of 2

            bool wasReached = false;
            m_TargetScoreReachedChannel.OnEventRaised += _ => wasReached = true;

            var playerScores = new List<PlayerScore>
            {
                new PlayerScore { score = score }
            };

            m_ScoreUpdatedChannel.RaiseEvent(playerScores);

            Assert.IsTrue(wasReached);
            Assert.IsTrue(m_Objective.IsCompleted);
        }

        [Test]
        public void ScoreObjective_MultiplePlayersOneWins_RaisesForWinner()
        {
            m_Objective = CreateObjective(3);

            var scoreP1 = new Score();
            scoreP1.IncrementScore(); // 1

            var scoreP2 = new Score();
            for (int i = 0; i < 3; i++)
                scoreP2.IncrementScore(); // 3 — winner

            PlayerScore winner = default;
            m_TargetScoreReachedChannel.OnEventRaised += ps => winner = ps;

            var playerScores = new List<PlayerScore>
            {
                new PlayerScore { score = scoreP1 },
                new PlayerScore { score = scoreP2 }
            };

            m_ScoreUpdatedChannel.RaiseEvent(playerScores);

            Assert.AreEqual(3, winner.score.Value);
        }

        [Test]
        public void ScoreObjective_ResetThenCheck_IsNotCompleted()
        {
            m_Objective = CreateObjective(1);

            var score = new Score();
            score.IncrementScore();

            var playerScores = new List<PlayerScore>
            {
                new PlayerScore { score = score }
            };

            m_ScoreUpdatedChannel.RaiseEvent(playerScores);
            Assert.IsTrue(m_Objective.IsCompleted);

            m_Objective.ResetObjective();
            Assert.IsFalse(m_Objective.IsCompleted);
        }

        #endregion
    }
}
