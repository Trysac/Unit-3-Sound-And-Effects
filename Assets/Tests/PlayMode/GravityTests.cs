using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BalloonDash.Tests
{
    /// <summary>Gravity across the course scenes: each scene sets its own, however often it loads.</summary>
    public class GravityTests
    {
        const float ProjectGravity = -9.81f;
        const float ChallengeGravityModifier = 1.5f;

        [SetUp]
        public void ResetGravity() => Physics.gravity = new Vector3(0f, ProjectGravity, 0f);

        static IEnumerator Load(string scene)
        {
            SceneManager.LoadScene(scene);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReloadingTheChallengeKeepsItsGravity()
        {
            yield return Load("Challenge 3");
            Assert.That(Physics.gravity.y, Is.EqualTo(ProjectGravity * ChallengeGravityModifier).Within(1e-3f));

            yield return Load("Challenge 3");
            Assert.That(Physics.gravity.y, Is.EqualTo(ProjectGravity * ChallengeGravityModifier).Within(1e-3f));
        }

        [UnityTest]
        public IEnumerator TheRunnerSceneDoesNotInheritTheChallengeGravity()
        {
            yield return Load("Challenge 3");
            yield return Load("Game");

            Assert.That(Physics.gravity.y, Is.EqualTo(ProjectGravity).Within(1e-3f));
        }
    }
}
