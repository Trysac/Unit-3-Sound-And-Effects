using System.Collections.Generic;

namespace BalloonDash.Core
{
    /// <summary>Where records and settings are kept (PlayerPrefs in the game, memory in tests).</summary>
    public interface IRecordStore
    {
        int GetInt(string key, int fallback);
        void SetInt(string key, int value);
        void Save();
    }

    public sealed class MemoryRecordStore : IRecordStore
    {
        readonly Dictionary<string, int> values = new Dictionary<string, int>();

        public int GetInt(string key, int fallback) => values.TryGetValue(key, out int value) ? value : fallback;
        public void SetInt(string key, int value) => values[key] = value;
        public void Save() => SaveCount++;

        /// <summary>How many times the records were flushed.</summary>
        public int SaveCount { get; private set; }
    }

    /// <summary>What a finished run changed in the records.</summary>
    public readonly struct RecordUpdate
    {
        public readonly bool NewBest;
        public readonly bool NewBestDistance;
        public readonly int PreviousBest;

        public RecordUpdate(bool newBest, bool newBestDistance, int previousBest)
        {
            NewBest = newBest;
            NewBestDistance = newBestDistance;
            PreviousBest = previousBest;
        }
    }

    /// <summary>The best score and distance, the most coins in one run, every coin ever collected and the runs played, on the device.</summary>
    public sealed class HighScores
    {
        const string Prefix = "balloondash.records.";

        readonly IRecordStore store;

        public HighScores(IRecordStore store) => this.store = store;

        public int Best => store.GetInt(Prefix + "best", 0);

        /// <summary>Longest run in whole metres.</summary>
        public int BestDistance => store.GetInt(Prefix + "distance", 0);

        public int MostCoins => store.GetInt(Prefix + "coins", 0);

        public int TotalCoins => store.GetInt(Prefix + "bank", 0);

        public int Runs => store.GetInt(Prefix + "runs", 0);

        /// <summary>Records a finished run and saves the store once.</summary>
        public RecordUpdate Submit(int points, int metres, int coins)
        {
            int previous = Best;
            bool newBest = points > previous;
            if (newBest)
            {
                store.SetInt(Prefix + "best", points);
            }
            bool newDistance = metres > BestDistance;
            if (newDistance)
            {
                store.SetInt(Prefix + "distance", metres);
            }
            if (coins > MostCoins)
            {
                store.SetInt(Prefix + "coins", coins);
            }
            store.SetInt(Prefix + "bank", TotalCoins + coins);
            store.SetInt(Prefix + "runs", Runs + 1);
            store.Save();
            return new RecordUpdate(newBest, newDistance, previous);
        }
    }

    /// <summary>The settings menu, kept on the device between sessions.</summary>
    public sealed class GameSettings
    {
        const string Prefix = "balloondash.settings.";
        public const int MaxVolume = 10;

        /// <summary>Effects volume in tenths, 0..10.</summary>
        public int Volume = 8;
        /// <summary>Music volume in tenths, 0..10.</summary>
        public int Music = 7;
        public bool Muted;
        public bool ScreenShake = true;

        public void StepVolume(int step) => Volume = Clamp(Volume + step, 0, MaxVolume);

        public void StepMusic(int step) => Music = Clamp(Music + step, 0, MaxVolume);

        static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;

        /// <summary>Reads the settings, falling back to the defaults for missing or broken values.</summary>
        public static GameSettings Load(IRecordStore store)
        {
            var defaults = new GameSettings();
            return new GameSettings
            {
                Volume = Clamp(store.GetInt(Prefix + "volume", defaults.Volume), 0, MaxVolume),
                Music = Clamp(store.GetInt(Prefix + "music", defaults.Music), 0, MaxVolume),
                Muted = store.GetInt(Prefix + "muted", 0) == 1,
                ScreenShake = store.GetInt(Prefix + "shake", 1) == 1,
            };
        }

        public void Save(IRecordStore store)
        {
            store.SetInt(Prefix + "volume", Volume);
            store.SetInt(Prefix + "music", Music);
            store.SetInt(Prefix + "muted", Muted ? 1 : 0);
            store.SetInt(Prefix + "shake", ScreenShake ? 1 : 0);
            store.Save();
        }
    }
}
