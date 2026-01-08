using NUnit.Framework;
using AshNCircuit.Core.States;

namespace AshNCircuit.Tests.Core
{
    public class WorldRngStateTests
    {
        [Test]
        public void SameRunSeed_ProducesSameSequences_ForAllStreams()
        {
            var a = new WorldRngState(runSeed: 12345);
            var b = new WorldRngState(runSeed: 12345);

            for (var i = 0; i < 10; i++)
            {
                Assert.That(a.GenRng.NextUInt64(), Is.EqualTo(b.GenRng.NextUInt64()), "GenRng sequence should match for same seed.");
                Assert.That(a.LootRng.NextUInt64(), Is.EqualTo(b.LootRng.NextUInt64()), "LootRng sequence should match for same seed.");
                Assert.That(a.AiRng.NextUInt64(), Is.EqualTo(b.AiRng.NextUInt64()), "AiRng sequence should match for same seed.");
            }
        }

        [Test]
        public void Streams_HaveDifferentInitialState()
        {
            var state = new WorldRngState(runSeed: 42);

            Assert.That(state.GenRng.State, Is.Not.EqualTo(state.LootRng.State));
            Assert.That(state.GenRng.State, Is.Not.EqualTo(state.AiRng.State));
            Assert.That(state.LootRng.State, Is.Not.EqualTo(state.AiRng.State));
        }
    }
}

