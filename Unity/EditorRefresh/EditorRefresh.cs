using System.Runtime.InteropServices;

namespace BlenderBridge.EditorRefresh
{
    public static class EditorRefresh
    {
        public static void SetEnabled(bool enabled)
        {
            Native.SetEnabled(enabled);
        }

        static class Native
        {
            [DllImport("EditorRefresh.dll")]
            public static extern bool SetDebugLog(bool enabled);

            [DllImport("EditorRefresh.dll")]
            public static extern bool SetEnabled(bool enabled);
        }
    }
}
