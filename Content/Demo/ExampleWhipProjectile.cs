using BreadLibrary.Common.Whip;
using Terraria;

namespace BreadLibrary.Content.Demo
{
	public class ExampleWhipProjectile : BaseWhipProjectile
	{
        #region IwhipMOtion
        protected override IWhipMotion CreateMotion()
        {
            return new WhipMotions.CowboyWindupMotion();
        }

        protected override void SetupModifiers(ModularWhipController controller)
        {
            //controller.AddModifier(new WhipModifiers.TwirlModifier(4, 12, 1f* Projectile.spriteDirection));
            //controller.AddModifier(new WhipModifiers.SmoothSineModifier(6, 30, 8f, 4f, 1f, Direction: Projectile.spriteDirection));
        }
        #endregion
        public override void OnSpawn(IEntitySource source)
        {
            Projectile.aiStyle = -1;
        }

        public override SoundStyle? WhipCrack_SFX => null;
        public override void Prepare()
        {
            AddHitEffects(BuffID.Electrified);

            WhipController = new ModularWhipController(CreateMotion());

            SetupModifiers(WhipController);

        }
        public override void AI2()
        {

        }


        #region Drawing
        public override float GetWhipWidth(float baseWidth, float t)
        {
            _HeadOffset = new Vector2(0, -_HeadRectangle.Height/2f);
            _DebugMode = false;
            _ShouldDrawNormal = false;
            _Head_VerticalFrames = 5;;
            return baseWidth + Math.Clamp(MathF.Sin(t * 10f) * 10f  * MathF.Tan(t * 14f + Main.GlobalTimeWrappedHourly*20f + Main.rand.NextFloat(4038f)) * MathHelper.SmoothStep(0, 1f, t), 1, 4);
        }
        protected override float RenderSpacing => 10f;
        public override float _PrimitiveScrollRate() => -1f;
        public override Color GetWhipColor(float t, float w)
        {
            return Color.Lerp(Color.White, Color.Blue, MathF.Sin(Main.GlobalTimeWrappedHourly)*MathF.Cos(t*10f));
        }
        public float Saturate(float x)
        {
            if (x > 1f)
                return 1f;
            if (x < 0f)
                return 0f;
            return x;
        }

        protected override void DrawOverPrimitive(List<Vector2> points)
        {
            _Head_y = (int)(5 * Math.Abs(MathF.Sin(Main.GlobalTimeWrappedHourly)));
            Texture2D tex = ModContent.Request<Texture2D>("BreadLibrary/Content/Demo/ExampleWhip_MidChain").Value;




            float whipLength = Projectile.WhipSettings.Segments;

            float spacingPixels = tex.Width;

            int count = Math.Max(1, (int)(whipLength / spacingPixels));

            // shared sliding parameter
            float slide = (MathF.Sin(Main.GlobalTimeWrappedHourly*1f))%1f;

            for (int i = 0; i < count; i++)
            {
                // fixed offset per element
                float offset = i / (float)count;

                // sliding + offset
                float t = (slide + offset) % 1f;

                Vector2 point = GetPointAlongWhip(points, t);

                float rot = GetRotationAlongWhip(points, t);

                Color color = Color.White.MultiplyRGB(
                    Lighting.GetColor(point.ToTileCoordinates()));

                Main.EntitySpriteDraw(
                    tex,
                    point - Main.screenPosition,
                    null,
                    color,
                    rot,
                    tex.Size() / 2,
                    1f,
                    0);
            }

        }
        public override bool _PrimitiveIsScrollingTexture => true;
        protected override Texture2D PrimitiveTex => ModContent.Request<Texture2D>("BreadLibrary/Content/Demo/DoubleTrail").Value;

        protected override Texture2D WhipHandle => ModContent.Request<Texture2D>("BreadLibrary/Content/Demo/ExampleWhipHilt").Value;
        protected override Texture2D WhipHead => ModContent.Request<Texture2D>("BreadLibrary/Content/Demo/ExampleWhipHead").Value;

        #endregion
    }
}
