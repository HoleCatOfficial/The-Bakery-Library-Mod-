namespace BreadLibrary.Common.IK
{
    public sealed class IKSkeletonJacobian
    {
        public Vector2 Root;

        public float[] Lengths;
        public float[] Angles;

        public Vector2[] JointPositions;
        public float[] MinAngles;
        public float[] MaxAngles;
        public float[] RestAngles;
        public int JointCount => Lengths.Length;
        public IKSkeletonJacobian(Vector2 root, float[] lengths)
        {
            Root = root;
            Lengths = lengths;
            Angles = new float[lengths.Length];
            JointPositions = new Vector2[lengths.Length + 1];


            MinAngles = new float[lengths.Length];
            MaxAngles = new float[lengths.Length];
            RestAngles = new float[lengths.Length];
        }

        public IKSkeletonJacobian(Vector2 root, float Length, int amount)
        {
            Root = root;
            float[] lengths = new float[amount];
            for(int i = 0; i< lengths.Length; i++)
            {
                lengths[i] = Length;
            }

            Lengths = lengths;
            Angles = new float[lengths.Length];
            JointPositions = new Vector2[lengths.Length + 1];


            MinAngles = new float[lengths.Length];
            MaxAngles = new float[lengths.Length];
            RestAngles = new float[lengths.Length];
        }

        private void ForwardKinematics()
        {
            JointPositions[0] = Root;

            for (int i = 0; i < JointCount; i++)
            {
                Vector2 dir = new Vector2(
                    MathF.Cos(Angles[i]),
                    MathF.Sin(Angles[i])
                );

                JointPositions[i + 1] =
                    JointPositions[i] +
                    dir * Lengths[i];
            }
        }
        public void Solve(Vector2 target, int iterations = 10, float alpha = 0.001f)
        {
            //GO FUCK YOURSELF ALPHAAA
            alpha *= 0.0001f;
            for (int iter = 0; iter < iterations; iter++)
            {
                for (int i = 0; i < JointCount; i++)
                {
                    ForwardKinematics();
                    float constraintStrength = 0.15f;
                    Vector2 joint = JointPositions[i];
                    Vector2 end = JointPositions[^1];
                    Vector2 error = target - end;

                    if (error.LengthSquared() < 0.01f)
                        return;

                    Vector2 toEnd = end - joint;

                    Vector2 jacobianCol = new Vector2(-toEnd.Y, toEnd.X);
                    //wydm lucille, WHAT DO YOU MEANN
                    float gradient = Vector2.Dot(jacobianCol, error);

                    float reachTerm = gradient;

                    float constraintTerm =
                        RestAngles[i] - Angles[i];

                    float total =
                        reachTerm +
                        constraintTerm * constraintStrength;

                    Angles[i] += alpha * total;
                }
            }
            //not sure quite why but this just makes it slightly smoother. go figure.
            ForwardKinematics();


        }
    }

}
