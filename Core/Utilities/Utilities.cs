using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreadLibrary.Core.Utilities
{
    public static partial class Utilities
    {
        /// <summary>
        ///     Returns the namespace path to the provided object, including the object itself.
        /// </summary>
        public static string GetPath(this object obj) => obj.GetType().Namespace.Replace('.', '/') + "/" + obj.GetType().Name;



        /// <summary>
        ///     Excludes a given <see cref="NPC"/> from the bestiary completely.
        /// </summary>
        /// <param name="npc">The NPC to apply the bestiary deletion to.</param>
        public static void ExcludeFromBestiary(this ModNPC npc)
        {
            NPCID.Sets.NPCBestiaryDrawModifiers value = new()
            {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(npc.Type, value);
        }

        /// <summary>
        ///     A simple utility that gracefully gets a <see cref="NPC"/>'s <see cref="NPC.ModNPC"/> instance as a specific type without having to do clunky casting.
        /// </summary>
        /// <remarks>
        ///     In the case of casting errors, this will create a log message that informs the user of the failed cast and fall back on a dummy instance.
        /// </remarks>
        /// <typeparam name="TNPC">The ModNPC type to convert to.</typeparam>
        /// <param name="n">The NPC to access the ModNPC from.</param>
        public static TNPC As<TNPC>(this NPC n) where TNPC : ModNPC
        {
            if (n.ModNPC is TNPC castedNPC)
                return castedNPC;

            bool vanillaNPC = n.ModNPC is null;
            Mod mod = ModContent.GetInstance<BreadLibrary>();
            if (vanillaNPC)
                mod.Logger.Warn($"A vanilla NPC of ID {n.type} was erroneously casted to a mod NPC of type {nameof(TNPC)}.");
            else
                mod.Logger.Warn($"A NPC of type {n.ModNPC.Name} was erroneously casted to a mod NPC of type {nameof(TNPC)}.");

            return ModContent.GetInstance<TNPC>();
        }

        /// <summary>
        ///     A simple utility that gracefully gets a <see cref="Projectile"/>'s <see cref="Projectile.ModProjectile"/> instance as a specific type without having to do clunky casting.
        /// </summary>
        /// <remarks>
        ///     In the case of casting errors, this will create a log message that informs the user of the failed cast and fall back on a dummy instance.
        /// </remarks>
        /// <typeparam name="TProjectile">The ModProjectile type to convert to.</typeparam>
        /// <param name="p">The Projectile to access the ModProjectile from.</param>
        public static TProjectile As<TProjectile>(this Projectile p) where TProjectile : ModProjectile
        {
            if (p.ModProjectile is TProjectile castedProjectile)
                return castedProjectile;

            bool vanillaProjectile = p.ModProjectile is null;
            Mod mod = ModContent.GetInstance<BreadLibrary>();
            if (vanillaProjectile)
                mod.Logger.Warn($"A vanilla projectile of ID {p.type} was erroneously casted to a mod projectile of type {nameof(TProjectile)}.");
            else
                mod.Logger.Warn($"A projectile of type {p.ModProjectile.Name} was erroneously casted to a mod projectile of type {nameof(TProjectile)}.");

            return ModContent.GetInstance<TProjectile>();
        }
        /// <summary>
        /// Draws a line significantly more efficiently than <see cref="Utils.DrawLine(SpriteBatch, Vector2, Vector2, Color, Color, float)"/> using just one scaled line texture. Positions are automatically converted to screen coordinates.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch by which the line should be drawn.</param>
        /// <param name="start">The starting point of the line in world coordinates.</param>
        /// <param name="end">The ending point of the line in world coordinates.</param>
        /// <param name="color">The color of the line.</param>
        /// <param name="width">The width of the line.</param>
        public static void DrawLineBetter(this SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float width)
        {
            // Draw nothing if the start and end are equal, to prevent division by 0 problems.
            if (start == end)
                return;

            start -= Main.screenPosition;
            end -= Main.screenPosition;

            Texture2D line = TextureAssets.FishingLine.Value;
            float rotation = (end - start).ToRotation();
            Vector2 scale = new Vector2(Vector2.Distance(start, end) / line.Width, width / line.Height);

            spriteBatch.Draw(line, start, null, color, rotation, line.Size() * Vector2.UnitY * 0.5f, scale, SpriteEffects.None, 0f);
        }
    }
}
