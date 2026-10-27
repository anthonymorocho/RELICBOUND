using NUnit.Framework;
using Relicbound.Core.Progression;

namespace Relicbound.Core.Tests
{
    public class ProgressionTests
    {
        [Test]
        public void DefaultCurve_Has30LevelsAndGrows()
        {
            var curve = LevelCurve.CreateDefault();
            Assert.That(curve.MaxLevel, Is.EqualTo(30));
            for (var level = 2; level < curve.MaxLevel; level++)
            {
                Assert.That(curve.XpToNext(level), Is.GreaterThan(curve.XpToNext(level - 1)));
            }
        }

        [Test]
        public void AddXp_CanSkipSeveralLevels_AndKeepsRemainder()
        {
            var progress = new PlayerProgress(new LevelCurve(new long[] { 100, 200, 300 }));
            var gained = progress.AddXp(350);
            Assert.That(gained, Is.EqualTo(2));
            Assert.That(progress.Level, Is.EqualTo(3));
            Assert.That(progress.XpIntoLevel, Is.EqualTo(50));
        }

        [Test]
        public void MaxLevel_StopsAccumulating()
        {
            var progress = new PlayerProgress(new LevelCurve(new long[] { 100 }));
            progress.AddXp(10_000);
            Assert.That(progress.IsMaxLevel, Is.True);
            Assert.That(progress.XpIntoLevel, Is.EqualTo(0));
            Assert.That(progress.LevelFraction, Is.EqualTo(1f));
        }

        [Test]
        public void LoadedValuesOutOfRange_AreNormalized()
        {
            var progress = new PlayerProgress(new LevelCurve(new long[] { 100, 200 }), level: 1, xpIntoLevel: 150);
            Assert.That(progress.Level, Is.EqualTo(2));
            Assert.That(progress.XpIntoLevel, Is.EqualTo(50));
        }
    }
}
