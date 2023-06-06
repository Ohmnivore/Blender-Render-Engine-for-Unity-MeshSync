using UnityEngine;
using System;

namespace BlenderBridge.ViewLink
{
    [Serializable]
    struct ViewState
    {
        public string ID;
        public int Width;
        public int Height;
        public bool IsPerspective;
        public Pose ViewPose;
        public Matrix4x4 WindowMatrix;
    }
}
