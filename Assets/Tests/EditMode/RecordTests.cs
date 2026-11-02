using NUnit.Framework;

namespace BalloonDash.Core.Tests
{
    /// <summary>High scores and settings kept on the device.</summary>
    public class RecordTests
    {
        [Test]
        public void ARunBeatingTheBestIsANewBest()
        {
            var store = new MemoryRecordStore();
            var records = new HighScores(store);

            RecordUpdate first = records.Submit(420, 300, 12);
            Assert.That(first.NewBest, Is.True);
            Assert.That(first.NewBestDistance, Is.True);
            Assert.That(first.PreviousBest, Is.EqualTo(0));

            RecordUpdate second = records.Submit(380, 310, 7);
            Assert.That(second.NewBest, Is.False);
            Assert.That(second.NewBestDistance, Is.True);
            Assert.That(second.PreviousBest, Is.EqualTo(420));

            Assert.That(records.Best, Is.EqualTo(420));
            Assert.That(records.BestDistance, Is.EqualTo(310));
            Assert.That(records.MostCoins, Is.EqualTo(12));
            Assert.That(records.TotalCoins, Is.EqualTo(19));
            Assert.That(records.Runs, Is.EqualTo(2));
            Assert.That(store.SaveCount, Is.EqualTo(2), "one save per run");
        }

        [Test]
        public void RecordsSurviveANewSessionOnTheSameStore()
        {
            var store = new MemoryRecordStore();
            new HighScores(store).Submit(999, 640, 30);

            var later = new HighScores(store);
            Assert.That(later.Best, Is.EqualTo(999));
            Assert.That(later.BestDistance, Is.EqualTo(640));
            Assert.That(later.Runs, Is.EqualTo(1));
        }

        [Test]
        public void SettingsRoundTripAndClampBrokenValues()
        {
            var store = new MemoryRecordStore();
            var settings = new GameSettings { Volume = 3, Music = 0, Muted = true, ScreenShake = false };
            settings.Save(store);

            GameSettings loaded = GameSettings.Load(store);
            Assert.That(loaded.Volume, Is.EqualTo(3));
            Assert.That(loaded.Music, Is.EqualTo(0));
            Assert.That(loaded.Muted, Is.True);
            Assert.That(loaded.ScreenShake, Is.False);

            store.SetInt("balloondash.settings.volume", 99);
            store.SetInt("balloondash.settings.music", -4);
            loaded = GameSettings.Load(store);
            Assert.That(loaded.Volume, Is.EqualTo(GameSettings.MaxVolume));
            Assert.That(loaded.Music, Is.EqualTo(0));
        }

        [Test]
        public void DefaultsAndStepsStayInRange()
        {
            GameSettings settings = GameSettings.Load(new MemoryRecordStore());
            Assert.That(settings.Volume, Is.EqualTo(8));
            Assert.That(settings.Music, Is.EqualTo(7));
            Assert.That(settings.Muted, Is.False);
            Assert.That(settings.ScreenShake, Is.True);

            settings.StepVolume(5);
            settings.StepMusic(-10);
            Assert.That(settings.Volume, Is.EqualTo(10));
            Assert.That(settings.Music, Is.EqualTo(0));
        }
    }
}
