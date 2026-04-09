using System.Collections.Generic;
using BreadLibrary.Core.Graphics;
using Terraria.Graphics.Renderers;

namespace BreadLibrary.Core.Graphics.Particles
{
    public class ParticleEngine : ILoadable
    {
        public static ParticleRenderer Particles = new();
        public static ParticleRenderer ShaderParticles = new();
        public static ParticleRenderer BehindProjectiles = new();
        public IEnumerable<IPooledParticle> ActiveParticles
        {
            get
            {
                for (int i = 0; i < Particles.Particles.Count; i++)
                {
                    IPooledParticle particle = (IPooledParticle)Particles.Particles[i];

                    if (particle is null)
                        continue;

                    yield return particle;
                }
            }
        }
        public void Load(Mod mod)
        {
            On_Main.UpdateParticleSystems += UpdateParticles;
            On_Main.DrawDust += DrawParticles;
            On_Main.DrawProjectiles += DrawBehindProjectiles;
        }

        public void Unload() { }

        public static void Clear()
        {
            Particles.Clear();
            ShaderParticles.Clear();
            BehindProjectiles.Clear();
        }

        public static void CollectPixelatedParticles(List<IDrawPixellated> results)
        {
            CollectFromRenderer(Particles, results);
            CollectFromRenderer(ShaderParticles, results);
            CollectFromRenderer(BehindProjectiles, results);
        }

        private static void CollectFromRenderer(ParticleRenderer renderer, List<IDrawPixellated> results)
        {
            if (renderer is null)
                return;

            foreach (IPooledParticle particle in renderer.Particles)
            {
                if (particle is IDrawPixellated pixel && pixel.ShouldDrawPixelated)
                    results.Add(pixel);
            }
        }

        private void UpdateParticles(On_Main.orig_UpdateParticleSystems orig, Main self)
        {
            orig(self);
            BehindProjectiles.Update();
            ShaderParticles.Update();
            Particles.Update();
        }

        private void DrawBehindProjectiles(On_Main.orig_DrawProjectiles orig, Main self)
        {
            Main.spriteBatch.Begin
            (
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.Transform
            );

            BehindProjectiles.Settings.AnchorPosition = -Main.screenPosition;
            BehindProjectiles.Draw(Main.spriteBatch);

            Main.spriteBatch.End();

            orig(self);
        }

        private void DrawParticles(On_Main.orig_DrawDust orig, Main self)
        {
            orig(self);

            Main.spriteBatch.Begin
            (
                SpriteSortMode.Immediate,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.Transform
            );

            ShaderParticles.Settings.AnchorPosition = -Main.screenPosition;
            ShaderParticles.Draw(Main.spriteBatch);
            Main.spriteBatch.End();

            Main.spriteBatch.Begin
            (
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.Transform
            );

            Particles.Settings.AnchorPosition = -Main.screenPosition;
            Particles.Draw(Main.spriteBatch);
            Main.spriteBatch.End();
        }
    }
}