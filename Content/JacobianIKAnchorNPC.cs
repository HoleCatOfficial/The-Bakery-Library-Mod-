using BreadLibrary.Common.IK;
using BreadLibrary.Core;
using BreadLibrary.Core.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace BreadLibrary.Content
{

    public class JacobianIKAnchorNPC : ModNPC
    {
        private IKSkeletonJacobian Limb;

        public override void SetDefaults()
        {
            NPC.width = 40;
            NPC.height = 40;
            NPC.lifeMax = 999999;
            NPC.dontTakeDamage = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.knockBackResist = 0f;
            NPC.aiStyle = -1;
        }

        public override void OnSpawn(IEntitySource source)
        {
            float[] lengths = new float[]
            {
            30f,50f,30f,
            30f,30f,20f,
            30f,50f,30f,
            30f,30f,20f
            };

            Limb = new IKSkeletonJacobian(NPC.Center, lengths);
            
            /*Limb.MaxAngles = new float[]
            {
                1.2f,
                1.5f,
                0.2f,
                0.5f
            };
            Limb.MinAngles =new float[]
            {
               -1.2f,
               -0.2f,
               -1.4f,
               -0.5f
            };
            Limb.RestAngles = new float[]
            {
                0f,
                0.7f,
                -0.4f,
                0.1f
            };
            */



        }

        public override void AI()
        {
            NPC.velocity = Vector2.Zero;
            NPC.rotation = 0f;

            Player player = Main.LocalPlayer;

            Vector2 target =
                Main.MouseWorld;

            Limb.Root = NPC.Center;

            Limb.Solve(
                target,
        alpha: 0.005f,
            iterations: 36
            );
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if(!NPC.IsABestiaryIconDummy)
            DrawChain(spriteBatch);
            return true;
        }

        private void DrawChain(SpriteBatch spriteBatch)
        {

            for (int i = 0; i < Limb.JointPositions.Length - 1; i++)
            {
                Vector2 start = Limb.JointPositions[i];
                Vector2 end = Limb.JointPositions[i + 1];

                float dist = start.Distance(end);
                float rot = (start.AngleTo(end));
                Utils.DrawBorderString(spriteBatch, i.ToString(), start - Main.screenPosition, Color.White, 0.6f);
                end = Limb.JointPositions[i] + new Vector2(dist, 0).RotatedBy(rot);
                Color color = Color.Lerp(Color.Green, Color.Red, i / (float)(Limb.JointPositions.Length - 2));
                Utilities.DrawLineBetter(spriteBatch, start, end, color, 4f);

            }
        }
    }
}