namespace BreadLibrary.Core.SoftBodySim
{
    public class SoftbodyInstance
    {
        public SoftbodySim Sim;

        public int[,] NodeGrid;
        public int GridWidth;
        public int GridHeight;

        private VertexPositionColor[] _verts;
        private short[] _indices;


        public Entity AttachedEntity;
        public struct Anchor
        {
            public int Node;
            public Vector2 LocalOffset;
            public float LocalAngle;
        }

        public List<Anchor> Anchors = new();

        public Vector2 LocalOffset;
      
        public SoftbodyInstance(SoftbodySim sim, Material material = default)
        {
            Sim = sim;
            Sim.Mat = material;

        }

        public void AttachToNPC(Entity npc, Vector2 Offset, List<int> nodes)
        {
            AttachedEntity = npc;
            Anchors.Clear();

            for(int i = 0; i< Sim.Nodes.Count; i++)
{
                Vector2 local = Sim.Nodes[i].Pos - npc.Center;

                Sim.Attachments.Add(new SoftbodySim.AttachmentConstraint
                {
                    Node = i,
                    Target = () => npc.Center + Offset+ local.RotatedBy(0),
                    Stiffness = 0.05f
                });
            }
        }
        public void AttachCenterCrossToNPC(int[,] grid, NPC npc)
        {
            AttachedEntity = npc;
            Sim.Attachments.Clear();

            int w = grid.GetLength(0);
            int h = grid.GetLength(1);

            int cx = w / 2;
            int cy = h / 2;

            int[] xs = { cx, cx + 1, cx - 1, cx, cx };
            int[] ys = { cy, cy, cy, cy + 1, cy - 1 };

            for (int i = 0; i < 5; i++)
            {
                int id = grid[xs[i], ys[i]];
                if (id == -1)
                    continue;

                Vector2 local = Sim.Nodes[id].Pos - npc.Center;

                Sim.Attachments.Add(new SoftbodySim.AttachmentConstraint
                {
                    Node = id,
                    Target = () => npc.Center + local.RotatedBy(npc.rotation),
                    Stiffness = Sim.Mat.AttachmentStiffness
                });
            }
        }
        public void Update()
        {
           

            Sim.Step();
        }
        VertexPositionColor[] vertices;
        short[] indices;
        private BasicEffect effect;
        public void Draw()
        {
            if (Main.dedServ)
                return;
            Main.spriteBatch.Begin(
          SpriteSortMode.Deferred,
          BlendState.AlphaBlend,
          Main.DefaultSamplerState,
          DepthStencilState.None,
          RasterizerState.CullNone,
          null,
          Main.GameViewMatrix.TransformationMatrix);
            if (indices is null)
            BuildMesh();
            UpdateVertexBuffer();



            GraphicsDevice gd = Main.graphics.GraphicsDevice;

            effect ??= new BasicEffect(gd)
            {
                VertexColorEnabled = true,
                Projection = Main.GameViewMatrix.TransformationMatrix,
                View = Matrix.Identity,
                World = Matrix.Identity
            };
            effect.Projection = Matrix.CreateOrthographicOffCenter(
               0, Main.screenWidth,
               Main.screenHeight, 0,
               -1f,  1f);

            effect.View = Main.GameViewMatrix.ZoomMatrix;
            Vector2 pivot = AttachedEntity.Center - Main.screenPosition;

            effect.World = Matrix.Identity;
                //Matrix.CreateTranslation(-pivot.X, -pivot.Y, 0f) *
                //Matrix.CreateScale(1f) *
                //Matrix.CreateTranslation(pivot.X, pivot.Y, 0f);


            gd.BlendState = BlendState.AlphaBlend;
            gd.DepthStencilState = DepthStencilState.None;
            gd.RasterizerState = RasterizerState.CullNone;
            gd.SamplerStates[0] = SamplerState.PointClamp;
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                gd.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    vertices,
                    0,
                    vertices.Length,
                    indices,
                    0,
                    indices.Length / 3
                );
            }

            for (int i = 0; i < Sim.Nodes.Count - 1; i++) { Utilities.Utilities.DrawLineBetter(Main.spriteBatch, Sim.Nodes[i].Pos, Sim.Nodes[i + 1].Pos, Color.Aqua, 2f); }


            Main.spriteBatch.End();
        }

        void UpdateVertexBuffer()
        {
            for (int i = 0; i < Sim.Nodes.Count; i++)
            {
                var n = Sim.Nodes[i];

                vertices[i].Position =
                    new Vector3(n.Pos.X - Main.screenPosition.X, n.Pos.Y - Main.screenPosition.Y, 0);

                vertices[i].Color = Color.White;
            }
        }

        public void BuildMesh()
        {
            int quadCount = (GridWidth - 1) * (GridHeight - 1);

            indices = new short[quadCount * 6];

            int k = 0;

            for (int x = 0; x < GridWidth - 1; x++)
                for (int y = 0; y < GridHeight - 1; y++)
                {
                    int a = NodeGrid[x, y];
                    int b = NodeGrid[x + 1, y];
                    int c = NodeGrid[x, y + 1];
                    int d = NodeGrid[x + 1, y + 1];

                    indices[k++] = (short)a;
                    indices[k++] = (short)b;
                    indices[k++] = (short)c;

                    indices[k++] = (short)b;
                    indices[k++] = (short)d;
                    indices[k++] = (short)c;
                }

            vertices = new VertexPositionColor[Sim.Nodes.Count];
        }
    }
}
