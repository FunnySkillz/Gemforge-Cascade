using System;

namespace GemforgeCascade.Core
{
    // Small deterministic generator with explicit state for replays and stable-turn saves.
    public sealed class DeterministicRandom
    {
        private const uint NonZeroFallback = 0x6D2B79F5u;
        private uint state;

        public uint State => state;

        public DeterministicRandom(uint seed)
        {
            Restore(seed);
        }

        public void Restore(uint savedState)
        {
            state = savedState == 0 ? NonZeroFallback : savedState;
        }

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));

            return (int)(NextUInt() % (uint)maxExclusive);
        }

        private uint NextUInt()
        {
            uint value = state;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            state = value;
            return value;
        }
    }
}
