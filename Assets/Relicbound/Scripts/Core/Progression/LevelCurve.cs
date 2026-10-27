using System;

namespace Relicbound.Core.Progression
{
    /// <summary>
    /// Experiencia necesaria para pasar de cada nivel al siguiente. Los valores vienen de
    /// datos (ScriptableObject en Unity, docs/12); esta clase solo valida y consulta.
    /// </summary>
    public sealed class LevelCurve
    {
        private readonly long[] _xpToNext;

        /// <param name="xpToNext">Índice 0 = XP para pasar de nivel 1 a 2. Longitud = nivel máximo - 1.</param>
        public LevelCurve(long[] xpToNext)
        {
            if (xpToNext == null || xpToNext.Length == 0) throw new ArgumentException("La curva necesita al menos un tramo.", nameof(xpToNext));
            foreach (var value in xpToNext)
            {
                if (value <= 0) throw new ArgumentException("Cada tramo debe requerir XP positiva.", nameof(xpToNext));
            }
            _xpToNext = (long[])xpToNext.Clone();
        }

        public int MaxLevel => _xpToNext.Length + 1;

        /// <summary>XP necesaria para pasar de <paramref name="level"/> al siguiente; 0 en el nivel máximo.</summary>
        public long XpToNext(int level)
        {
            if (level < 1 || level > MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            return level == MaxLevel ? 0 : _xpToNext[level - 1];
        }

        /// <summary>Curva inicial de docs/12 (niveles 1-30). Hipótesis de balance pendiente de pruebas.</summary>
        public static LevelCurve CreateDefault()
        {
            var table = new long[29];
            for (var i = 0; i < table.Length; i++)
            {
                var level = i + 1;
                // 100 · nivel^1.6 redondeado a decenas: 100, 300, 580, 920, ...
                table[i] = (long)Math.Round(100 * Math.Pow(level, 1.6) / 10.0) * 10;
            }
            return new LevelCurve(table);
        }
    }
}
