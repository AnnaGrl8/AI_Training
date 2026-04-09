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
    /// Tests for the victory detection chain: ObjectiveManager tracking completion,
    /// and the integrated flow from score → objective → all-objectives-complete.
    /// </summary>
    public class VictoryDetectionTests
    {
        private readonly List<Object> m_Cleanup = new List<Object>();

        private T CreateSO<T>() where T : ScriptableObject
        {
            var so = ScriptableObject.CreateInstance<T>();
            m_Cleanup.Add(so);
            return so;
        }

        private GameObject CreateGameObject(string name = "TestGO")
        {
            var go = new GameObject(name);
            m_Cleanup.Add(go);
            return go;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = m_Cleanup.Count - 1; i >= 0; i--)
            {
                if (m_Cleanup[i] != null)
                    Object.DestroyImmediate(m_Cleanup[i]);
            }
            m_Cleanup.Clear();
        }

        #region Helpers

        /// <summary>
        /// Minimal ObjectiveSO subclass that exposes CompleteObjective for testing.
        /// </summary>
        private class TestObjectiveSO : ObjectiveSO
        {
            public void Complete() => CompleteObjective();
        }

        private TestObjectiveSO CreateTestObjective(VoidEventChannelSO completedChannel)
        {
            var objective = CreateSO<TestObjectiveSO>();

            var so = new UnityEditor.SerializedObject(objective);
            so.FindProperty("m_ObjectiveCompleted").objectReferenceValue = completedChannel;
            so.ApplyModifiedPropertiesWithoutUndo();

            return objective;
        }

        private ObjectiveManager CreateObjectiveManager(
            List<ObjectiveSO> objectives,
            VoidEventChannelSO allCompletedChannel,
            VoidEventChannelSO gameStartedChannel,
            VoidEventChannelSO objectiveCompletedChannel)
        {
            var go = CreateGameObject("ObjectiveManager");
            var manager = go.AddComponent<ObjectiveManager>();

            var so = new UnityEditor.SerializedObject(manager);
            so.FindProperty("m_AllObjectivesCompleted").objectReferenceValue = allCompletedChannel;
            so.FindProperty("m_GameStarted").objectReferenceValue = gameStartedChannel;
            so.FindProperty("m_ObjectiveCompleted").objectReferenceValue = objectiveCompletedChannel;

            var objectivesProp = so.FindProperty("m_Objectives");
            objectivesProp.arraySize = objectives.Count;
            for (int i = 0; i < objectives.Count; i++)
            {
                objectivesProp.GetArrayElementAtIndex(i).objectReferenceValue = objectives[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            // Re-invoke OnEnable now that fields are set
            MethodInfo onEnable = typeof(ObjectiveManager).GetMethod("OnEnable",
                BindingFlags.Instance | BindingFlags.NonPublic);
            onEnable.Invoke(manager, null);

            return manager;
        }

        #endregion

        #region ObjectiveManager — IsObjectiveListComplete

        [Test]
        public void ObjectiveManager_NoObjectives_IsComplete()
        {
            var allCompleted = CreateSO<VoidEventChannelSO>();
            var gameStarted = CreateSO<VoidEventChannelSO>();
            var objCompleted = CreateSO<VoidEventChannelSO>();

            var manager = CreateObjectiveManager(
                new List<ObjectiveSO>(), allCompleted, gameStarted, objCompleted);

            Assert.IsTrue(manager.IsObjectiveListComplete());
        }

        [Test]
        public void ObjectiveManager_SingleObjectiveIncomplete_IsNotComplete()
        {
            var allCompleted = CreateSO<VoidEventChannelSO>();
            var gameStarted = CreateSO<VoidEventChannelSO>();
            var objCompleted = CreateSO<VoidEventChannelSO>();

            var objective = CreateTestObjective(objCompleted);

            var manager = CreateObjectiveManager(
                new List<ObjectiveSO> { objective }, allCompleted, gameStarted, objCompleted);

            Assert.IsFalse(manager.IsObjectiveListComplete());
        }

        [Test]
        public void ObjectiveManager_SingleObjectiveComplete_IsComplete()
        {
            var allCompleted = CreateSO<VoidEventChannelSO>();
            var gameStarted = CreateSO<VoidEventChannelSO>();
            var objCompleted = CreateSO<VoidEventChannelSO>();

            var objective = CreateTestObjective(objCompleted);

            var manager = CreateObjectiveManager(
                new List<ObjectiveSO> { objective }, allCompleted, gameStarted, objCompleted);

            objective.Complete();

            Assert.IsTrue(manager.IsObjectiveListComplete());
        }

        [Test]
        public void ObjectiveManager_MultipleObjectivesPartialComplete_IsNotComplete()
        {
            var allCompleted = CreateSO<VoidEventChannelSO>();
            var gameStarted = CreateSO<VoidEventChannelSO>();
            var objCompleted = CreateSO<VoidEventChannelSO>();

            var obj1 = CreateTestObjective(objCompleted);
            var obj2 = CreateTestObjective(objCompleted);
            var obj3 = CreateTestObjective(objCompleted);

            var manager = CreateObjectiveManager(
                new List<ObjectiveSO> { obj1, obj2, obj3 }, allCompleted, gameStarted, objCompleted);

            obj1.Complete();
            obj3.Complete();
            // obj2 still incomplete

            Assert.IsFalse(manager.IsObjectiveListComplete());
        }

        [Test]
        public void ObjectiveManager_AllObjectivesComplete_IsComplete()
        {
            var allCompleted = CreateSO<VoidEventChannelSO>();
            var gameStarted = CreateSO<VoidEventChannelSO>();
            var objCompleted = CreateSO<VoidEventChannelSO>();

            var obj1 = CreateTestObjective(objCompleted);
            var obj2 = CreateTestObjective(objCompleted);

            var manager = CreateObjectiveManager(
                new List<ObjectiveSO> { obj1, obj2 }, allCompleted, gameStarted, objCompleted);

            obj1.Complete();
            obj2.Complete();

            Assert.IsTrue(manager.IsObjectiveListComplete());
        }

        #endregion

        #region ObjectiveManager — Event Broadcasting

        [Test]
        public void ObjectiveManager_AllComplete_BroadcastsAllObjectivesCompleted()
        {
            var allCompleted = CreateSO<VoidEventChannelSO>();
            var gameStarted = CreateSO<VoidEventChannelSO>();
            var objCompleted = CreateSO<VoidEventChannelSO>();

            var objective = CreateTestObjective(objCompleted);

            var manager = CreateObjectiveManager(
                new List<ObjectiveSO> { objective }, allCompleted, gameStarted, objCompleted);

            bool eventFired = false;
            allCompleted.OnEventRaised += () => eventFired = true;

            // CompleteObjective raises m_ObjectiveCompleted, which ObjectiveManager listens to
            objective.Complete();

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void ObjectiveManager_PartialComplete_DoesNotBroadcast()
        {
            var allCompleted = CreateSO<VoidEventChannelSO>();
            var gameStarted = CreateSO<VoidEventChannelSO>();
            var objCompleted = CreateSO<VoidEventChannelSO>();

            var obj1 = CreateTestObjective(objCompleted);
            var obj2 = CreateTestObjective(objCompleted);

            var manager = CreateObjectiveManager(
                new List<ObjectiveSO> { obj1, obj2 }, allCompleted, gameStarted, objCompleted);

            bool eventFired = false;
            allCompleted.OnEventRaised += () => eventFired = true;

            obj1.Complete(); // only 1 of 2

            Assert.IsFalse(eventFired);
        }

        [Test]
        public void ObjectiveManager_GameStarted_ResetsAllObjectives()
        {
            var allCompleted = CreateSO<VoidEventChannelSO>();
            var gameStarted = CreateSO<VoidEventChannelSO>();
            var objCompleted = CreateSO<VoidEventChannelSO>();

            var obj1 = CreateTestObjective(objCompleted);
            var obj2 = CreateTestObjective(objCompleted);

            var manager = CreateObjectiveManager(
                new List<ObjectiveSO> { obj1, obj2 }, allCompleted, gameStarted, objCompleted);

            obj1.Complete();
            obj2.Complete();
            Assert.IsTrue(manager.IsObjectiveListComplete());

            // Simulate new game
            gameStarted.RaiseEvent();

            Assert.IsFalse(obj1.IsCompleted);
            Assert.IsFalse(obj2.IsCompleted);
            Assert.IsFalse(manager.IsObjectiveListComplete());
        }

        #endregion

        #region Integrated — Score to Victory

        [Test]
        public void Integrated_ScoreReachesTarget_TriggersVictory()
        {
            // Set up event channels
            var scoreUpdatedChannel = CreateSO<ScoreListEventChannelSO>();
            var targetScoreReachedChannel = CreateSO<PlayerScoreEventChannelSO>();
            var objectiveCompletedChannel = CreateSO<VoidEventChannelSO>();
            var allObjectivesCompletedChannel = CreateSO<VoidEventChannelSO>();
            var gameStartedChannel = CreateSO<VoidEventChannelSO>();

            // Create ScoreObjectiveSO with target of 3
            LogAssert.ignoreFailingMessages = true;
            var scoreObjective = CreateSO<ScoreObjectiveSO>();
            LogAssert.ignoreFailingMessages = false;

            var soObj = new UnityEditor.SerializedObject(scoreObjective);
            soObj.FindProperty("m_TargetScore").intValue = 3;
            soObj.FindProperty("m_ScoreManagerUpdated").objectReferenceValue = scoreUpdatedChannel;
            soObj.FindProperty("m_TargetScoreReached").objectReferenceValue = targetScoreReachedChannel;
            soObj.FindProperty("m_ObjectiveCompleted").objectReferenceValue = objectiveCompletedChannel;
            soObj.ApplyModifiedPropertiesWithoutUndo();

            // Manually subscribe after fields are set
            MethodInfo onEnable = typeof(ScoreObjectiveSO).GetMethod("OnEnable",
                BindingFlags.Instance | BindingFlags.NonPublic);
            onEnable.Invoke(scoreObjective, null);

            // Create ObjectiveManager tracking the score objective
            var manager = CreateObjectiveManager(
                new List<ObjectiveSO> { scoreObjective },
                allObjectivesCompletedChannel,
                gameStartedChannel,
                objectiveCompletedChannel);

            // Track victory
            bool victoryDetected = false;
            allObjectivesCompletedChannel.OnEventRaised += () => victoryDetected = true;

            string winnerMessage = null;
            var winnerChannel = CreateSO<StringEventChannelSO>();
            targetScoreReachedChannel.OnEventRaised += ps =>
            {
                winnerMessage = ps.playerID != null
                    ? ps.playerID.name.Replace("_SO", "") + " wins"
                    : "Player wins";
            };

            // Simulate scoring: 3 goals for the winning player
            var winningScore = new Score();
            for (int i = 0; i < 3; i++)
                winningScore.IncrementScore();

            var losingScore = new Score();
            losingScore.IncrementScore();

            var playerScores = new List<PlayerScore>
            {
                new PlayerScore { score = losingScore },
                new PlayerScore { score = winningScore }
            };

            // This triggers the full chain: score update → objective complete → all objectives complete
            scoreUpdatedChannel.RaiseEvent(playerScores);

            Assert.IsTrue(scoreObjective.IsCompleted, "Score objective should be completed");
            Assert.IsTrue(manager.IsObjectiveListComplete(), "All objectives should be complete");
            Assert.IsTrue(victoryDetected, "Victory event should have fired");
        }

        [Test]
        public void Integrated_ScoreBelowTarget_NoVictory()
        {
            var scoreUpdatedChannel = CreateSO<ScoreListEventChannelSO>();
            var targetScoreReachedChannel = CreateSO<PlayerScoreEventChannelSO>();
            var objectiveCompletedChannel = CreateSO<VoidEventChannelSO>();
            var allObjectivesCompletedChannel = CreateSO<VoidEventChannelSO>();
            var gameStartedChannel = CreateSO<VoidEventChannelSO>();

            LogAssert.ignoreFailingMessages = true;
            var scoreObjective = CreateSO<ScoreObjectiveSO>();
            LogAssert.ignoreFailingMessages = false;

            var soObj = new UnityEditor.SerializedObject(scoreObjective);
            soObj.FindProperty("m_TargetScore").intValue = 5;
            soObj.FindProperty("m_ScoreManagerUpdated").objectReferenceValue = scoreUpdatedChannel;
            soObj.FindProperty("m_TargetScoreReached").objectReferenceValue = targetScoreReachedChannel;
            soObj.FindProperty("m_ObjectiveCompleted").objectReferenceValue = objectiveCompletedChannel;
            soObj.ApplyModifiedPropertiesWithoutUndo();

            MethodInfo onEnable = typeof(ScoreObjectiveSO).GetMethod("OnEnable",
                BindingFlags.Instance | BindingFlags.NonPublic);
            onEnable.Invoke(scoreObjective, null);

            var manager = CreateObjectiveManager(
                new List<ObjectiveSO> { scoreObjective },
                allObjectivesCompletedChannel,
                gameStartedChannel,
                objectiveCompletedChannel);

            bool victoryDetected = false;
            allObjectivesCompletedChannel.OnEventRaised += () => victoryDetected = true;

            // Score is 2, target is 5
            var score = new Score();
            score.IncrementScore();
            score.IncrementScore();

            var playerScores = new List<PlayerScore>
            {
                new PlayerScore { score = score }
            };

            scoreUpdatedChannel.RaiseEvent(playerScores);

            Assert.IsFalse(scoreObjective.IsCompleted);
            Assert.IsFalse(victoryDetected);
        }

        #endregion
    }
}
