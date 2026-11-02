using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BalloonDash.Tests
{
    /// <summary>The 2021 course scenes as they shipped: the runner and the Challenge 3 balloon.</summary>
    public class CourseSceneTests
    {
        const float ProjectGravity = -9.81f;

        [SetUp]
        public void ResetGravity() => Physics.gravity = new Vector3(0f, ProjectGravity, 0f);

        static IEnumerator Load(string scene)
        {
            SceneManager.LoadScene(scene);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheChallengeBackdropScrollsLeft()
        {
            yield return Load("Challenge 3");
            Transform backdrop = GameObject.FindWithTag("Background").transform;
            float start = backdrop.position.x;

            yield return new WaitForSeconds(1f);

            Assert.That(backdrop.position.x, Is.LessThan(start - 3f), "the backdrop moves at 10 units a second");
        }
    }
}
