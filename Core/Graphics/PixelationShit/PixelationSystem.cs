
using BreadLibrary.Core.Graphics.Particles;
using global::BreadLibrary.Core.Graphics.PixelationShit;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using System;
    using System.Collections.Generic;
    using Terraria;
    using Terraria.ModLoader;

namespace BreadLibrary.Core.Graphics
{
    public interface IDrawPixellated
    {
        PixelLayer PixelLayer { get; }
        bool ShouldDrawPixelated => true;
        void DrawPixelated(SpriteBatch spriteBatch);
    }

    /// <summary>
    /// Use this for player-bound visuals instead of trying to force ordinary PlayerDrawLayers into the RT.
    /// </summary>
    public interface IPlayerPixelatedDrawer
    {
        PixelLayer PixelLayer { get; }
        bool IsActive(Player player);
        void DrawPixelated(Player player, SpriteBatch spriteBatch);
    }

    internal sealed class PlayerPixelWrapper : IDrawPixellated
    {
        public readonly Player Player;
        public readonly IPlayerPixelatedDrawer Drawer;

        public PlayerPixelWrapper(Player player, IPlayerPixelatedDrawer drawer)
        {
            Player = player;
            Drawer = drawer;
        }

        public PixelLayer PixelLayer => Drawer.PixelLayer;
        public bool ShouldDrawPixelated => Player.active && !Player.dead && Drawer.IsActive(Player);

        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            Drawer.DrawPixelated(Player, spriteBatch);
        }
    }

    [Autoload(Side = ModSide.Client)]
    public sealed class PixelationSystem : ModSystem
    {
        private static readonly List<IDrawPixellated> BehindTilesDraws = new();
        private static readonly List<IDrawPixellated> AboveTilesDraws = new();
        private static readonly List<IDrawPixellated> AboveNPCsDraws = new();
        private static readonly List<IDrawPixellated> AboveProjectilesDraws = new();
        private static readonly List<IDrawPixellated> AbovePlayersDraws = new();
        private static readonly List<IDrawPixellated> scratchPixelDraws = new();

        private static RenderTarget2D behindTilesTarget;
        private static RenderTarget2D aboveTilesTarget;
        private static RenderTarget2D aboveNPCsTarget;
        private static RenderTarget2D aboveProjectilesTarget;
        private static RenderTarget2D abovePlayersTarget;

        private static int preparedFrame = -1;
        private static int targetWidth = -1;
        private static int targetHeight = -1;

        /// <summary>
        /// Lower = chunkier pixels. 2 means half-resolution targets.
        /// </summary>
        public static int PixelScale => 2;

        /// <summary>
        /// Global registration point for anything that wants to add custom pixelated draws.
        /// </summary>
        public static event Action<List<IDrawPixellated>> CollectPixelDrawsEvent;

        /// <summary>
        /// Registration point for player-linked pixel drawers.
        /// </summary>
        public static event Action<Player, List<IPlayerPixelatedDrawer>> CollectPlayerPixelDrawersEvent;

        public override void Load()
        {
            if (Main.dedServ)
                return;
            On_Main.DoDraw += PrepareTargetsBeforeDoDraw;
            //DrawHooks.DrawBehindWallsEvent += EnsurePrepared;
            DrawHooks.DrawBehindTilesEvent += DrawBehindTilesTarget;
            DrawHooks.DrawAboveTilesEvent += DrawAboveTilesTarget;
            DrawHooks.DrawAboveNPCsEvent += DrawAboveNPCsTarget;
            DrawHooks.DrawAboveProjectilesEvent += DrawAboveProjectilesTarget;
            DrawHooks.DrawAbovePlayersEvent += DrawAbovePlayersTarget;
        }

        public override void Unload()
        {
            //DrawHooks.DrawBehindWallsEvent -= EnsurePrepared;
            DrawHooks.DrawBehindTilesEvent -= DrawBehindTilesTarget;
            DrawHooks.DrawAboveTilesEvent -= DrawAboveTilesTarget;
            DrawHooks.DrawAboveNPCsEvent -= DrawAboveNPCsTarget;
            DrawHooks.DrawAboveProjectilesEvent -= DrawAboveProjectilesTarget;
            DrawHooks.DrawAbovePlayersEvent -= DrawAbovePlayersTarget;

            CollectPixelDrawsEvent = null;
            CollectPlayerPixelDrawersEvent = null;

            DisposeTarget(ref behindTilesTarget);
            DisposeTarget(ref aboveTilesTarget);
            DisposeTarget(ref aboveNPCsTarget);
            DisposeTarget(ref aboveProjectilesTarget);
            DisposeTarget(ref abovePlayersTarget);
        }

        public static void Queue(IDrawPixellated draw)
        {
            if (draw is null || !draw.ShouldDrawPixelated)
                return;

            switch (draw.PixelLayer)
            {
                case PixelLayer.BehindTiles:
                    BehindTilesDraws.Add(draw);
                    break;
                case PixelLayer.AboveTiles:
                    AboveTilesDraws.Add(draw);
                    break;
                case PixelLayer.AboveNPCs:
                    AboveNPCsDraws.Add(draw);
                    break;
                case PixelLayer.AboveProjectiles:
                    AboveProjectilesDraws.Add(draw);
                    break;
                case PixelLayer.AbovePlayer:
                    AbovePlayersDraws.Add(draw);
                    break;
            }
        }

        private static void EnsurePrepared()
        {
            if (Main.gameMenu || Main.dedServ)
                return;

            EnsureTargets();
            CollectAllDrawRequests();
            DrawQueuesToTargets();
            Main.graphics.GraphicsDevice.SetRenderTarget(null);
        }

        private static void EnsureTargets()
        {
            int desiredWidth = Math.Max(1, Main.screenWidth / PixelScale);
            int desiredHeight = Math.Max(1, Main.screenHeight / PixelScale);

            if (desiredWidth == targetWidth && desiredHeight == targetHeight &&
                behindTilesTarget is not null &&
                !behindTilesTarget.IsDisposed)
            {
                return;
            }

            targetWidth = desiredWidth;
            targetHeight = desiredHeight;

            DisposeTarget(ref behindTilesTarget);
            DisposeTarget(ref aboveTilesTarget);
            DisposeTarget(ref aboveNPCsTarget);
            DisposeTarget(ref aboveProjectilesTarget);
            DisposeTarget(ref abovePlayersTarget);

            GraphicsDevice gd = Main.instance.GraphicsDevice;

            behindTilesTarget = new RenderTarget2D(gd, targetWidth, targetHeight);
            aboveTilesTarget = new RenderTarget2D(gd, targetWidth, targetHeight);
            aboveNPCsTarget = new RenderTarget2D(gd, targetWidth, targetHeight);
            aboveProjectilesTarget = new RenderTarget2D(gd, targetWidth, targetHeight);
            abovePlayersTarget = new RenderTarget2D(gd, targetWidth, targetHeight);
        }

        private static void CollectAllDrawRequests()
        {
            BehindTilesDraws.Clear();
            AboveTilesDraws.Clear();
            AboveNPCsDraws.Clear();
            AboveProjectilesDraws.Clear();
            AbovePlayersDraws.Clear();

            // Projectiles
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (!proj.active || proj.ModProjectile is null)
                    continue;

                if (proj.ModProjectile is IDrawPixellated pixelProj && pixelProj.ShouldDrawPixelated)
                    Queue(pixelProj);
            }

            // NPCs
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.ModNPC is null)
                    continue;

                if (npc.ModNPC is IDrawPixellated pixelNpc && pixelNpc.ShouldDrawPixelated)
                    Queue(pixelNpc);
            }

            scratchPixelDraws.Clear();
            ParticleEngine.CollectPixelatedParticles(scratchPixelDraws);



            // Custom/player-linked drawers
            if (CollectPlayerPixelDrawersEvent is not null)
            {
                List<IPlayerPixelatedDrawer> drawers = new();

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    if (player is null || !player.active)
                        continue;

                    drawers.Clear();
                    CollectPlayerPixelDrawersEvent.Invoke(player, drawers);

                    for (int j = 0; j < drawers.Count; j++)
                    {
                        IPlayerPixelatedDrawer drawer = drawers[j];
                        if (drawer is null || !drawer.IsActive(player))
                            continue;

                        Queue(new PlayerPixelWrapper(player, drawer));
                    }
                }
            }
            // Anything else a mod wants to push in manually
            if (CollectPixelDrawsEvent is not null)
            {
                List<IDrawPixellated> manualDraws = new();
                CollectPixelDrawsEvent.Invoke(manualDraws);

                for (int i = 0; i < manualDraws.Count; i++)
                {
                    IDrawPixellated draw = manualDraws[i];
                    if (draw is not null && draw.ShouldDrawPixelated)
                        Queue(draw);
                }
            }
        }

        private static void DrawQueuesToTargets()
        {
            DrawQueueToTarget(behindTilesTarget, BehindTilesDraws);
            DrawQueueToTarget(aboveTilesTarget, AboveTilesDraws);
            DrawQueueToTarget(aboveNPCsTarget, AboveNPCsDraws);
            DrawQueueToTarget(aboveProjectilesTarget, AboveProjectilesDraws);
            DrawQueueToTarget(abovePlayersTarget, AbovePlayersDraws);
        }

        private static void DrawQueueToTarget(RenderTarget2D target, List<IDrawPixellated> queue)
        {
            if (target is null)
                return;

            GraphicsDevice gd = Main.instance.GraphicsDevice;
            gd.SetRenderTarget(target);
            gd.Clear(Color.Transparent);

            if (queue.Count > 0)
            {
                Main.spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    SamplerState.PointClamp,
                    DepthStencilState.None,
                    RasterizerState.CullNone,
                    null,
                    Matrix.CreateScale(1f / PixelScale, 1f / PixelScale, 1f));

                for (int i = 0; i < queue.Count; i++)
                    queue[i].DrawPixelated(Main.spriteBatch);

                Main.spriteBatch.End();
            }

            gd.SetRenderTarget(null);
        }
        private static void DrawTargetBack(RenderTarget2D target, List<IDrawPixellated> queue)
        {
            if (target is null || queue.Count == 0)
                return;

            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.ZoomMatrix);

            Main.spriteBatch.Draw(
                target,
                Vector2.Zero - Main.LocalPlayer.velocity,
                null,
                Color.White,
                0f,
                Vector2.Zero,
                PixelScale,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.End();
        }
        private void PrepareTargetsBeforeDoDraw(On_Main.orig_DoDraw orig, Main self, GameTime gameTime)
        {
            if (!Main.dedServ && !Main.gameMenu)
                EnsurePrepared();

            orig(self, gameTime);
        }
        private static void DrawBehindTilesTarget() =>
            DrawTargetBack(behindTilesTarget, BehindTilesDraws);

        private static void DrawAboveTilesTarget() =>
            DrawTargetBack(aboveTilesTarget, AboveTilesDraws);

        private static void DrawAboveNPCsTarget() =>
            DrawTargetBack(aboveNPCsTarget, AboveNPCsDraws);

        private static void DrawAboveProjectilesTarget() =>
            DrawTargetBack(aboveProjectilesTarget, AboveProjectilesDraws);

        private static void DrawAbovePlayersTarget(bool _) =>
            DrawTargetBack(abovePlayersTarget, AbovePlayersDraws);

        private static void DisposeTarget(ref RenderTarget2D target)
        {

            RenderTarget2D targetToDispose = target;
            target = null;

            if (targetToDispose is null || targetToDispose.IsDisposed)
                return;

            Main.QueueMainThreadAction(() =>
            {
                try
                {
                    if (!targetToDispose.IsDisposed)
                        targetToDispose.Dispose();
                }
                catch
                {
                }
            });
        }
    }
}