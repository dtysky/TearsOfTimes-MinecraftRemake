using System;

namespace Render.Core
{
    using SharpDX;
    class Camera
    {
        public Camera()
        {
            SetLens(0.25f * MathUtil.Pi, 1.0f, 1.0f, 1000.0f);
        }
        public void SetPosition(float x, float y, float z)
        {
            Position.X = x;
            Position.Y = y;
            Position.Z = z;
        }
        public void SetPosition(Vector3 position)
        {
            Position = position;
        }
        public void SetLens(float fovY, float aspect, float zn, float zf)
        {
            FovY = fovY;
            Aspect = aspect;
            NearZ = zn;
            FarZ = zf;

            NearWindowHeight = 2.0f * NearZ * Convert.ToSingle(Math.Tan(0.5f * FovY));
            FarWindowHeight = 2.0f * FarZ * Convert.ToSingle(Math.Tan(0.5f * FovY));

            Project = Matrix.PerspectiveFovLH(FovY, Aspect, NearZ, FarZ);
        }
        public void LookAt(Vector3 position, Vector3 target, Vector3 worldUp)
        {
            Position = position;
            Look = Vector3.Normalize(Vector3.Subtract(target, position));
            Right = Vector3.Normalize(Vector3.Cross(worldUp, Look));
            Up = Vector3.Cross(Look, Right);
        }
        public void Strafe(float d)
        {
            Position = Vector3.Add(Vector3.Multiply(new Vector3(d, d, d), Right), Position);
        }
        public void Walk(float d)
        {
            Position = Vector3.Add(Vector3.Multiply(new Vector3(d, d, d), Look), Position);
        }
        public void Pitch(float angle)
        {
            Matrix Rotate = Matrix.RotationAxis(Right, angle);
            Up = Vector3.TransformNormal(Up, Rotate);
            Look = Vector3.TransformNormal(Look, Rotate);
        }
        public void RotateY(float angle)
        {
            Matrix Rotate = Matrix.RotationY(angle);

            Right = Vector3.TransformNormal(Right, Rotate);
            Up = Vector3.TransformNormal(Up, Rotate);
            Look = Vector3.TransformNormal(Look, Rotate);
        }
        public void Update()
        {
            Vector3 R = Right;
            Vector3 U = Up;
            Vector3 L = Look;
            Vector3 P = Position;

            // Keep camera's axes orthogonal to each other and of unit length.
            L = Vector3.Normalize(L);
            U = Vector3.Normalize(Vector3.Cross(L, R));

            // U, L already ortho-normal, so no need to normalize cross product.
            R = Vector3.Cross(U, L);

            // Fill in the view matrix entries.
            float x = -Vector3.Dot(P, R);
            float y = -Vector3.Dot(P, U);
            float z = -Vector3.Dot(P, L);

            Right = R;
            Up = U;
            Look = L;

            View = new Matrix()
            {
                M11 = Right.X,
                M21 = Right.Y,
                M31 = Right.Z,
                M41 = x,

                M12 = Up.X,
                M22 = Up.Y,
                M32 = Up.Z,
                M42 = y,

                M13 = Look.X,
                M23 = Look.Y,
                M33 = Look.Z,
                M43 = z,

                M14 = 0.0f,
                M24 = 0.0f,
                M34 = 0.0f,
                M44 = 1.0f,
            };

        }

        protected Vector3 Position = new Vector3(0, 0, 0);
        protected Vector3 Right = new Vector3(1.0f, 0, 0);
        protected Vector3 Up = new Vector3(0, 1.0f, 0);
        protected Vector3 Look = new Vector3(0, 0, 1.0f);

        float NearZ;
        float FarZ;
        float Aspect;
        float FovY;
        float NearWindowHeight;
        float FarWindowHeight;

        public Matrix View = new Matrix();
        public Matrix Project = new Matrix();
    }
}
