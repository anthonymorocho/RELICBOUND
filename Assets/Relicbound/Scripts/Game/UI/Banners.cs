using UnityEngine;

namespace Relicbound.Game.UI
{
    public enum BannerCharge
    {
        Chevron,
        Tower,
        Cross,
    }

    /// <summary>Estandarte heráldico: campo, metal (borde y figura) y figura.</summary>
    public readonly struct BannerStyle
    {
        public BannerStyle(string id, string name, Color field, Color metal, BannerCharge charge)
        {
            Id = id;
            Name = name;
            Field = field;
            Metal = metal;
            Charge = charge;
        }

        public string Id { get; }
        public string Name { get; }
        public Color Field { get; }
        public Color Metal { get; }
        public BannerCharge Charge { get; }
    }

    /// <summary>Estandartes iniciales (docs/04 §Perfil). Los cosméticos posteriores amplían esta lista desde datos.</summary>
    public static class Banners
    {
        public static readonly BannerStyle[] Starting =
        {
            new BannerStyle("banner_azure", "Azur", Hex("#2F5D9E"), Hex("#E2BE5A"), BannerCharge.Tower),
            new BannerStyle("banner_crimson", "Gules", Hex("#9E2F2F"), Hex("#E6E2D8"), BannerCharge.Chevron),
            new BannerStyle("banner_verdant", "Sinople", Hex("#2F7A45"), Hex("#E2BE5A"), BannerCharge.Cross),
        };

        public static BannerStyle Get(string id)
        {
            foreach (var banner in Starting)
            {
                if (banner.Id == id) return banner;
            }
            return Starting[0];
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString(value, out var color);
            return color;
        }
    }
}
