using UnityEngine;

namespace BlenderBridge.ViewLink
{
    public static class CoordinateSystem
    {
        // Taken from MeshSync's msEntityConverter.cpp and muMath.h
        public static Pose BlenderViewMatrixToUnityPose(Matrix4x4 blenderMatrix)
        {
            blenderMatrix = CorrectCameraMatrix(blenderMatrix.inverse);

            var unityPosition = blenderMatrix.GetPosition();
            unityPosition = FlipZ(SwapYZ(FlipX(unityPosition)));

            var unityRotation = blenderMatrix.rotation;
            unityRotation = FlipZ(SwapYZ(FlipX(unityRotation)));
            unityRotation = unityRotation * Quaternion.AngleAxis(-90f, Vector3.right);

            return new Pose(unityPosition, unityRotation);
        }

        static Vector3 FlipX(Vector3 v)
        {
            return new Vector3(-v.x, v.y, v.z);
        }

        static Quaternion FlipX(Quaternion q)
        {
            return new Quaternion(q.x, -q.y, -q.z, q.w);
        }

        static Matrix4x4 FlipX(Matrix4x4 m)
        {
            return new Matrix4x4(
                new Vector4( m.m00, -m.m10, -m.m20, -m.m30),
                new Vector4(-m.m01,  m.m11,  m.m21,  m.m31),
                new Vector4(-m.m02,  m.m12,  m.m22,  m.m32),
                new Vector4(-m.m03,  m.m13,  m.m23,  m.m33));
        }

        static Vector3 FlipY(Vector3 v)
        {
            return new Vector3(v.x, -v.y, v.z);
        }

        static Quaternion FlipY(Quaternion q)
        {
            return new Quaternion(-q.x, q.y, -q.z, q.w);
        }

        static Matrix4x4 FlipY(Matrix4x4 m)
        {
            return new Matrix4x4(
                new Vector4( m.m00, -m.m10,  m.m20,  m.m30),
                new Vector4(-m.m01,  m.m11, -m.m21, -m.m31),
                new Vector4( m.m02, -m.m12,  m.m22,  m.m32),
                new Vector4( m.m03, -m.m13,  m.m23,  m.m33));
        }

        static Vector3 FlipZ(Vector3 v)
        {
            return new Vector3(v.x, v.y, -v.z);
        }

        static Quaternion FlipZ(Quaternion q)
        {
            return new Quaternion(-q.x, -q.y, q.z, q.w);
        }

        static Matrix4x4 FlipZ(Matrix4x4 m)
        {
            return new Matrix4x4(
                new Vector4( m.m00,  m.m10, -m.m20,  m.m30),
                new Vector4( m.m01,  m.m11, -m.m21,  m.m31),
                new Vector4(-m.m02, -m.m12,  m.m22, -m.m32),
                new Vector4( m.m03,  m.m13,  m.m23,  m.m33));
        }

        static Vector3 SwapYZ(Vector3 v)
        {
            return new Vector3(v.x, v.z, v.y);
        }

        static Quaternion SwapYZ(Quaternion q)
        {
            return new Quaternion(-q.x, -q.z, -q.y, q.w);
        }

        static Matrix4x4 SwapYZ(Matrix4x4 m)
        {
            return new Matrix4x4(
                new Vector4(m.m00, m.m10, m.m20, m.m30),
                new Vector4(m.m02, m.m12, m.m22, m.m32),
                new Vector4(m.m01, m.m11, m.m21, m.m31),
                new Vector4(m.m03, m.m13, m.m23, m.m33));
        }

        // Cameras and lights point towards their negative z in blender, in Unity they point in positive Z, correct for this
        static Matrix4x4 CorrectCameraMatrix(Matrix4x4 m)
        {
            return new Matrix4x4(
                new Vector4(-m.m00, -m.m10, -m.m20, -m.m30),
                new Vector4( m.m01,  m.m11,  m.m21,  m.m31),
                new Vector4(-m.m02, -m.m12, -m.m22, -m.m32),
                new Vector4( m.m03,  m.m13,  m.m23,  m.m33));
        }
    }
}
