using System;

namespace Relicbound.Core.Progression
{
    /// <summary>Nivel y experiencia del jugador. La XP solo se gana jugando (docs/12).</summary>
    public sealed class PlayerProgress
    {
        private readonly LevelCurve _curve;

        public PlayerProgress(LevelCurve curve, int level = 1, long xpIntoLevel = 0)
        {
            _curve = curve ?? throw new ArgumentNullException(nameof(curve));
            if (level < 1) level = 1;
            if (level > curve.MaxLevel) level = curve.MaxLevel;
            Level = level;
            XpIntoLevel = Math.Max(0, xpIntoLevel);
            Normalize();
        }

        public int Level { get; private set; }

        public long XpIntoLevel { get; private set; }

        public long XpToNext => _curve.XpToNext(Level);

        public bool IsMaxLevel => Level == _curve.MaxLevel;

        /// <summary>Progreso 0..1 dentro del nivel actual, para la barra del HUD.</summary>
        public float LevelFraction => IsMaxLevel ? 1f : (float)XpIntoLevel / XpToNext;

        public event Action<int> LevelUp;

        /// <summary>Suma XP y devuelve cuántos niveles se han subido.</summary>
        public int AddXp(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (IsMaxLevel || amount == 0) return 0;
            var before = Level;
            XpIntoLevel += amount;
            Normalize();
            return Level - before;
        }

        private void Normalize()
        {
            while (!IsMaxLevel && XpIntoLevel >= XpToNext)
            {
                XpIntoLevel -= XpToNext;
                Level++;
                LevelUp?.Invoke(Level);
            }
            if (IsMaxLevel) XpIntoLevel = 0;
        }
    }
}
