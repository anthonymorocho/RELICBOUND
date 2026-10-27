using System;
using System.Collections.Generic;
using NUnit.Framework;
using Relicbound.Core.Boot;
using Relicbound.Core.CameraControl;
using Relicbound.Core.Save;

namespace Relicbound.Core.Tests
{
    public class BootFlowTests
    {
        private static LoadResult Loaded(bool introSeen) => new LoadResult(LoadStatus.Loaded, new SaveGame
        {
            Profile = new ProfileData { ProfileId = "x", PlayerName = "Patricia", CastleName = "Bastión" },
            Tutorial = new TutorialData { IntroSeen = introSeen },
        }, null);

        [Test]
        public void FirstOpen_GoesThroughProfileAndIntroToCastle()
        {
            var flow = new BootFlow();
            var stages = new List<BootStage>();
            flow.StageChanged += stages.Add;

            flow.SplashFinished();
            flow.SaveLoaded(new LoadResult(LoadStatus.NoSave, null, null));
            flow.ProfileCreated();
            flow.IntroFinished();

            Assert.That(stages, Is.EqualTo(new[] { BootStage.LoadingSave, BootStage.ProfileCreation, BootStage.Intro, BootStage.Castle }));
        }

        [Test]
        public void ReturningPlayer_GoesStraightToCastle()
        {
            var flow = new BootFlow();
            flow.SplashFinished();
            flow.SaveLoaded(Loaded(introSeen: true));
            Assert.That(flow.Stage, Is.EqualTo(BootStage.Castle));
        }

        [Test]
        public void NewerSave_BlocksWithUpdateRequired()
        {
            var flow = new BootFlow();
            flow.SplashFinished();
            flow.SaveLoaded(new LoadResult(LoadStatus.NewerVersion, null, "v9"));
            Assert.That(flow.Stage, Is.EqualTo(BootStage.UpdateRequired));
        }

        [Test]
        public void InvalidTransition_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new BootFlow().IntroFinished());
        }
    }

    public class CameraTests
    {
        [Test]
        public void Pivot_IsClampedTighterWhenZoomedOut()
        {
            var limits = CameraLimits.ForGrid(40, 1f, 4f, 10f, 40f); // límite 24 m
            float x = 100, z = -100;
            limits.ClampPivot(ref x, ref z, 10f);
            Assert.That((x, z), Is.EqualTo((24f, -24f)));

            x = 100; z = 0;
            limits.ClampPivot(ref x, ref z, 40f);
            Assert.That(x, Is.EqualTo(12f));
        }

        [Test]
        public void Distance_IsClamped()
        {
            var limits = new CameraLimits(10, 10, 5, 20);
            Assert.That(limits.ClampDistance(1), Is.EqualTo(5));
            Assert.That(limits.ClampDistance(50), Is.EqualTo(20));
        }

        [Test]
        public void SmallFingerJitter_IsStillATap()
        {
            var gesture = new PointerGesture(dpi: 400);
            gesture.Begin(100, 100, 0f);
            gesture.Update(105, 103, 0.05f); // ~0.37 mm
            Assert.That(gesture.End(106, 104, 0.1f), Is.EqualTo(GestureKind.Tap));
        }

        [Test]
        public void MovingBeyondThreshold_IsADragAndNeverBecomesATap()
        {
            var gesture = new PointerGesture(dpi: 400); // umbral ≈ 39 px
            gesture.Begin(100, 100, 0f);
            gesture.Update(160, 100, 0.05f);
            Assert.That(gesture.End(100, 100, 0.1f), Is.EqualTo(GestureKind.Drag), "Volver al punto inicial no lo convierte en toque.");
        }

        [Test]
        public void StillPress_BecomesHold()
        {
            var gesture = new PointerGesture(dpi: 160);
            gesture.Begin(0, 0, 0f);
            Assert.That(gesture.Update(1, 1, 0.5f), Is.EqualTo(GestureKind.Hold));
        }

        [Test]
        public void UnknownDpi_FallsBackTo160()
        {
            Assert.That(new PointerGesture(dpi: 0).ThresholdPixels, Is.EqualTo(2.5f / 25.4f * 160f).Within(0.001f));
        }
    }
}
