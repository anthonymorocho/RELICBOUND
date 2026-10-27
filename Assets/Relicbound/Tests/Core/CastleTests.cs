using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Relicbound.Core.Castle;
using Relicbound.Core.Economy;
using Relicbound.Core.Progression;

namespace Relicbound.Core.Tests
{
    public class CastleTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);
        private static string _json;

        /// <summary>Lee el catálogo real del juego (funciona en Unity y con dotnet test).</summary>
        private static string CatalogJson()
        {
            if (_json != null) return _json;
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Assets")))
            {
                directory = directory.Parent;
            }
            Assert.That(directory, Is.Not.Null, "No se encontró la carpeta Assets.");
            _json = File.ReadAllText(Path.Combine(directory.FullName, "Assets", "Relicbound", "Resources", "catalog.json"));
            return _json;
        }

        private static (CastleLayout layout, CastleEconomy economy, ResourceWallet wallet, PlayerProgress progress) Start(int playerLevel = 1, long gold = 5000, long wood = 5000, long stone = 5000)
        {
            var catalog = CatalogLoader.FromJson(CatalogJson());
            var layout = new CastleLayout(catalog, CatalogLoader.DefaultLayout(CatalogJson(), Now));
            var wallet = new ResourceWallet();
            var progress = new PlayerProgress(LevelCurve.CreateDefault(), playerLevel);
            var economy = new CastleEconomy(layout, wallet, progress, 5000);
            wallet.Restore(new Dictionary<ResourceType, long> { { ResourceType.Gold, gold }, { ResourceType.Wood, wood }, { ResourceType.Stone, stone } });
            return (layout, economy, wallet, progress);
        }

        [Test]
        public void StartingCastle_IsValidAndReachable()
        {
            var (layout, _, _, _) = Start();
            Assert.That(layout.Keep, Is.Not.Null);
            Assert.That(layout.Count("tower"), Is.EqualTo(4));
            Assert.That(layout.Count("gate"), Is.EqualTo(1));
            // Cada pieza del castillo inicial debe poder recolocarse en su sitio (sin solapes ni bloqueos).
            foreach (var building in layout.Buildings)
            {
                if (!layout.Catalog.Get(building.TypeId).Movable) continue;
                Assert.That(layout.CanMove(building.InstanceId, building.X, building.Z, building.Rotation), Is.EqualTo(PlacementError.None), building.TypeId + " " + building.X + "," + building.Z);
            }
        }

        [Test]
        public void Placement_RejectsOverlapOutOfBoundsAndLocked()
        {
            var (layout, _, _, _) = Start(playerLevel: 3);
            Assert.That(layout.CanPlace("storehouse", 16, 19, 0, 3), Is.EqualTo(PlacementError.Overlap), "Encima del torreón.");
            Assert.That(layout.CanPlace("storehouse", 38, 38, 0, 3), Is.EqualTo(PlacementError.OutOfBounds));
            Assert.That(layout.CanPlace("storehouse", 1, 33, 0, 3), Is.EqualTo(PlacementError.None));
            Assert.That(layout.CanPlace("storehouse", 1, 33, 0, 1), Is.EqualTo(PlacementError.Locked), "El almacén se desbloquea en el nivel 2.");
        }

        [Test]
        public void ClosingTheGate_IsRejectedBecauseItBlocksThePath()
        {
            var (layout, _, _, _) = Start(playerLevel: 5);
            // La puerta ocupa x 16..24, z 6..12. Se cierran su salida al patio y los dos huecos laterales.
            var gate = layout.Buildings[layout.Buildings.Count - 1];
            Assert.That(gate.TypeId, Is.EqualTo("gate"));
            var walls = new[] { (12, 10), (14, 10), (24, 10), (26, 10), (16, 12), (18, 12), (20, 12), (22, 12) };
            var placed = 0;
            var last = PlacementError.None;
            foreach (var (x, z) in walls)
            {
                last = layout.CanPlace("wall", x, z, 0, 5);
                if (last != PlacementError.None) break;
                layout.Place("wall", x, z, 0, 5, Now);
                placed++;
            }
            Assert.That(last, Is.EqualTo(PlacementError.BlocksPath), "El último tramo dejaría el torreón sin acceso.");
            Assert.That(placed, Is.EqualTo(walls.Length - 1));
        }

        [Test]
        public void Build_ChargesCostAndGivesXp()
        {
            var (layout, economy, wallet, progress) = Start(playerLevel: 2);
            var stone = wallet.Get(ResourceType.Stone);
            var built = economy.Build("storehouse", 1, 33, 0, Now);
            Assert.That(built, Is.Not.Null);
            Assert.That(wallet.Get(ResourceType.Stone), Is.EqualTo(stone - 200));
            Assert.That(progress.XpIntoLevel, Is.GreaterThan(0));
            Assert.That(wallet.GetCapacity(ResourceType.Gold), Is.EqualTo(5000 + 2000), "El almacén amplía la capacidad.");
            Assert.That(layout.Count("storehouse"), Is.EqualTo(1));
        }

        [Test]
        public void Build_FailsWithoutResources_AndChargesNothing()
        {
            var (_, economy, wallet, _) = Start(playerLevel: 3, gold: 10, wood: 10, stone: 10);
            Assert.That(economy.Build("sawmill", 1, 33, 0, Now), Is.Null);
            Assert.That(wallet.Get(ResourceType.Gold), Is.EqualTo(10));
        }

        [Test]
        public void Upgrade_RequiresKeepLevel()
        {
            var (layout, economy, _, _) = Start(playerLevel: 5);
            var wall = System.Linq.Enumerable.First(layout.Buildings, b => b.TypeId == "wall");
            Assert.That(economy.CanUpgrade(wall), Is.EqualTo(UpgradeError.KeepTooLow));
            Assert.That(economy.Upgrade(layout.Keep, Now), Is.True);
            Assert.That(layout.KeepLevel, Is.EqualTo(2));
            Assert.That(economy.Upgrade(wall, Now), Is.True);
            Assert.That(wall.Level, Is.EqualTo(2));
        }

        [Test]
        public void Keep_RequiresPlayerLevelToUpgrade()
        {
            var (layout, economy, _, _) = Start(playerLevel: 1);
            Assert.That(economy.CanUpgrade(layout.Keep), Is.EqualTo(UpgradeError.PlayerTooLow));
        }

        [Test]
        public void Production_AccumulatesUpToEightHoursAndCapacity()
        {
            var (_, economy, wallet, _) = Start(playerLevel: 3, wood: 1000);
            var sawmill = economy.Build("sawmill", 1, 33, 0, Now);
            Assert.That(economy.Pending(sawmill, Now.AddHours(2)), Is.EqualTo(120));
            Assert.That(economy.Pending(sawmill, Now.AddHours(30)), Is.EqualTo(480), "Tope de capacidad / 8 h.");
            var before = wallet.Get(ResourceType.Wood);
            Assert.That(economy.Collect(sawmill, Now.AddHours(1)), Is.EqualTo(60));
            Assert.That(wallet.Get(ResourceType.Wood), Is.EqualTo(before + 60));
            Assert.That(economy.Pending(sawmill, Now.AddHours(1)), Is.EqualTo(0));
        }

        [Test]
        public void Collect_LeavesWhatDoesNotFitInStorage()
        {
            var (layout, economy, wallet, _) = Start(playerLevel: 1, gold: 4990);
            var keep = layout.Keep;
            Assert.That(economy.Collect(keep, Now.AddHours(2)), Is.EqualTo(10), "Solo caben 10 de oro.");
            Assert.That(wallet.Get(ResourceType.Gold), Is.EqualTo(5000));
            Assert.That(economy.Pending(keep, Now.AddHours(2)), Is.EqualTo(50), "Lo demás sigue en el torreón.");
        }
    }
}
