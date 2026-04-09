using System.Collections.Generic;
using Terraria;

namespace BreadLibrary.Core.Graphics.PixelationShit
{

    // IT JUST WORKS

    public interface IPixelDrawableSource
    {
        void CollectPixelDraws(List<IDrawPixellated> results);
    }

    /// <summary>
    /// Global/stateless collector. Good for systems, managers, cached world visuals, etc.
    /// </summary>
    public interface IPixelDrawProvider
    {
        void CollectPixelDraws(List<IDrawPixellated> results);
    }

    /// <summary>
    /// Player-bound collector. Good for player auras, overlays, held effects, etc.
    /// </summary>
    public interface IPlayerPixelDrawProvider
    {
        void CollectPixelDraws(Player player, List<IDrawPixellated> results);
    }
}