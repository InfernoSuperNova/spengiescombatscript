using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace IngameScript
{
    // this whole class shouldn't exist, but it's a hack to get around my shitty code
    public static class Data
    {
        public static Vector3D prevTargetVelocity = Vector3D.Zero;
        public static Vector3D aimPos = Vector3D.Zero;
    }
}
