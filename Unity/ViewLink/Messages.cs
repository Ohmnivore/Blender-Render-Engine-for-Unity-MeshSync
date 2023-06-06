using System.Collections.Generic;
using UnityEngine;

namespace BlenderBridge.ViewLink
{
    struct ViewUpdated : Comms.IMessage
    {
        public byte Type => (byte)ClientMessageType.ViewUpdated;

        public string ID;
        public int Width;
        public int Height;
        public bool IsPerspective;
        public List<float> ViewMatrix;
        public List<float> WindowMatrix;

        public Matrix4x4 ConvertedViewMatrix => ConvertListToMatrix(ViewMatrix);
        public Matrix4x4 ConvertedWindowMatrix => ConvertListToMatrix(WindowMatrix);

        Matrix4x4 ConvertListToMatrix(IList<float> data)
        {
            if (data.Count != 16)
            {
                Debug.LogError($"Invalid matrix received for View with ID {ID}");

                return Matrix4x4.identity;
            }

            return new Matrix4x4(
                new Vector4(data[0], data[4], data[8], data[12]),
                new Vector4(data[1], data[5], data[9], data[13]),
                new Vector4(data[2], data[6], data[10], data[14]),
                new Vector4(data[3], data[7], data[11], data[15])
            );
        }
    }

    struct ViewDestroyed : Comms.IMessage
    {
        public byte Type => (byte)ClientMessageType.ViewDestroyed;

        public string ID;
    }
    
    struct ObjectVisibility : Comms.IMessage
    {
        public byte Type => (byte)ClientMessageType.ObjectVisibility;

        public string Name;
        public bool Visible;
        public bool Obsolete;
    }
}
