using System.Collections.Generic;
using NUnit.Framework;
using Relicbound.Core.Economy;

namespace Relicbound.Core.Tests
{
    public class EconomyTests
    {
        private static ResourceWallet Wallet(long capacity)
        {
            var wallet = new ResourceWallet();
            foreach (var type in ResourceTypes.All) wallet.SetCapacity(type, capacity);
            return wallet;
        }

        [Test]
        public void Add_StopsAtCapacity_AndReportsWhatWasAdded()
        {
            var wallet = Wallet(100);
            Assert.That(wallet.Add(ResourceType.Gold, 80), Is.EqualTo(80));
            Assert.That(wallet.Add(ResourceType.Gold, 50), Is.EqualTo(20));
            Assert.That(wallet.Get(ResourceType.Gold), Is.EqualTo(100));
        }

        [Test]
        public void Gems_AreNotCappedByStorage()
        {
            var wallet = Wallet(100);
            Assert.That(wallet.Add(ResourceType.Gems, 1000), Is.EqualTo(1000));
        }

        [Test]
        public void TrySpend_IsAllOrNothing()
        {
            var wallet = Wallet(1000);
            wallet.Add(ResourceType.Gold, 100);
            wallet.Add(ResourceType.Stone, 10);
            var cost = new ResourceCost(new Dictionary<ResourceType, long> { { ResourceType.Gold, 50 }, { ResourceType.Stone, 20 } });

            Assert.That(wallet.TrySpend(cost), Is.False);
            Assert.That(wallet.Get(ResourceType.Gold), Is.EqualTo(100));
            Assert.That(wallet.Get(ResourceType.Stone), Is.EqualTo(10));
        }

        [Test]
        public void LoweringCapacity_KeepsStoredResources()
        {
            var wallet = Wallet(1000);
            wallet.Add(ResourceType.Wood, 900);
            wallet.SetCapacity(ResourceType.Wood, 500);
            Assert.That(wallet.Get(ResourceType.Wood), Is.EqualTo(900));
            Assert.That(wallet.Add(ResourceType.Wood, 10), Is.EqualTo(0));
        }

        [Test]
        public void Changed_FiresWithNewAmount()
        {
            var wallet = Wallet(1000);
            var events = new List<(ResourceType, long)>();
            wallet.Changed += (type, value) => events.Add((type, value));
            wallet.Add(ResourceType.Iron, 5);
            Assert.That(events, Is.EqualTo(new[] { (ResourceType.Iron, 5L) }));
        }

        [Test]
        public void NegativeCost_IsRejected()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => ResourceCost.Of(ResourceType.Gold, -1));
        }
    }
}
