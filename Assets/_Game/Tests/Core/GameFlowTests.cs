using System;
using BeforeItWasYou.Core.Flow;
using NUnit.Framework;

namespace BeforeItWasYou.Core.Tests
{
    public sealed class GameFlowTests
    {
        [Test]
        public void RecorridoCompleto_DelArranqueALosCreditos()
        {
            var flow = new GameFlow();
            var changes = 0;
            flow.Changed += (_, __) => changes++;
            flow.Enter(GameState.Menu);
            flow.Enter(GameState.Playing);
            Assert.IsTrue(flow.IsSimulating);
            flow.Enter(GameState.Paused);
            Assert.IsFalse(flow.IsSimulating);
            flow.Enter(GameState.Playing);
            flow.Enter(GameState.Failed);
            flow.Enter(GameState.Playing);
            flow.Enter(GameState.Epilogue);
            flow.Enter(GameState.Credits);
            flow.Enter(GameState.Menu);
            Assert.AreEqual(9, changes);
        }

        [Test]
        public void NoSePuedePausarEnElMenuNiMorirEnElEpilogo()
        {
            var flow = new GameFlow();
            flow.Enter(GameState.Menu);
            Assert.IsFalse(flow.TryEnter(GameState.Paused));
            Assert.Throws<InvalidOperationException>(() => flow.Enter(GameState.Failed));
            flow.Enter(GameState.Playing);
            flow.Enter(GameState.Epilogue);
            Assert.IsFalse(flow.CanEnter(GameState.Failed));
        }
    }
}
