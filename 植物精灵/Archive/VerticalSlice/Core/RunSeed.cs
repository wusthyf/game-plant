using System;

namespace PlantSpirit.VerticalSlice
{
    public sealed class RunRandom
    {
        private readonly Random random;
        public int Seed { get; }

        public RunRandom(int seed)
        {
            Seed = seed;
            random = new Random(seed);
        }

        public int Range(int minInclusive, int maxExclusive) => random.Next(minInclusive, maxExclusive);
        public float Value() => (float)random.NextDouble();
        public bool Chance(float probability) => Value() <= probability;
        public RunRandom Fork(string key) => new RunRandom(StableHash(Seed + ":" + key));

        private static int StableHash(string value)
        {
            unchecked
            {
                int hash = 17;
                foreach (char character in value) hash = hash * 31 + character;
                return hash;
            }
        }
    }
}
