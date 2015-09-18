using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
namespace ModelRender
{   
    class FollowingCamera : Camera
    {
        public FollowingCamera()
        {
            World = Matrix.Identity;
        }

        public void SetOffset(float cameraOffsetY, float cameraOffsetZ)
        {
            OffsetY = cameraOffsetY;
            OffsetZ = cameraOffsetZ;
        }

        public void SetHero(Matrix heroWorld)
        {
            var HeroPosition = heroWorld.TranslationVector * heroWorld.ScaleVector;
            var HeroForward = Vector3.Normalize(heroWorld.Forward);
            var HeroUp = Vector3.Normalize(heroWorld.Up);
            base.LookAt(HeroPosition - OffsetZ * HeroForward, HeroPosition, HeroUp);
        }
        public Matrix MatrixCameraToWorld()
        {
            var result = new Matrix();
            result.Row1 = new Vector4(Right, 0);
            result.Row2 = new Vector4(Up, 0);
            result.Row3 = new Vector4(Look, 0);
            result.Row4 = new Vector4(Position, 1);
            return result;

        }
        public new void Update()
        {
            base.Update();
            //update World
            World = Matrix.Translation(0, OffsetY, -OffsetZ);
            World *= MatrixCameraToWorld();
        }

        public Matrix World { get; private set; }

        private float OffsetY = -1f;
        private float OffsetZ = -2f;

    }
}
