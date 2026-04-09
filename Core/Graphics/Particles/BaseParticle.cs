using BreadLibrary.Core.Graphics.PixelationShit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria.Graphics.Renderers;

namespace BreadLibrary.Core.Graphics.Particles
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class PoolCapacityAttribute(int capacity) : Attribute
    {
        public int Capacity { get; } = capacity;
    }

    public abstract class BaseParticle<T> : IDrawPixellated, IPooledParticle where T : IPooledParticle, new()
    {
        public const int DEFAULT_POOL_CAPACITY = 100;

        public static ParticlePool<T> Pool { get; } = new ParticlePool<T>(typeof(T).GetCustomAttribute<PoolCapacityAttribute>()?.Capacity ?? DEFAULT_POOL_CAPACITY, GetNewParticle);

        protected static T GetNewParticle() => new T();

        public bool IsRestingInPool { get; private set; }

        public bool ShouldBeRemovedFromRenderer { get; protected set; }
        public abstract PixelLayer PixelLayer { get; }

        public virtual void FetchFromPool()
        {
            IsRestingInPool = false;
            ShouldBeRemovedFromRenderer = false;
        }

        public virtual void RestInPool()
        {
            IsRestingInPool = true;
        }

        public virtual void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
        {
        }

        public virtual void Update(ref ParticleRendererSettings settings)
        {
        }

        public abstract void DrawPixelated(SpriteBatch spriteBatch);
    }
}
