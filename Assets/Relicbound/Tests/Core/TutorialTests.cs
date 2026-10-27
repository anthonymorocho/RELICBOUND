using NUnit.Framework;
using Relicbound.Core.Boot;
using Relicbound.Core.Save;

namespace Relicbound.Core.Tests
{
    public class TutorialTests
    {
        [Test]
        public void Steps_AdvanceOnlyWithTheExpectedSignal()
        {
            var flow = new TutorialFlow(new TutorialData());
            Assert.That(flow.Current, Is.EqualTo(TutorialStep.Pan));
            Assert.That(flow.Notify(TutorialSignal.KeepSelected), Is.False);
            Assert.That(flow.Current, Is.EqualTo(TutorialStep.Pan));
            flow.Notify(TutorialSignal.CameraPanned);
            flow.Notify(TutorialSignal.CameraZoomed);
            Assert.That(flow.Notify(TutorialSignal.KeepSelected), Is.True);
            Assert.That(flow.IsDone, Is.True);
        }

        [Test]
        public void Progress_SurvivesReload()
        {
            var data = new TutorialData();
            new TutorialFlow(data).Notify(TutorialSignal.CameraPanned);
            var reloaded = new TutorialFlow(data);
            Assert.That(reloaded.Current, Is.EqualTo(TutorialStep.Zoom));
            Assert.That(reloaded.CompletedCount, Is.EqualTo(1));
        }
    }
}
