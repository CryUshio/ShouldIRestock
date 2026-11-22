using UnityEngine;

namespace ShouldIRestock
{
    public static class Log
    {
        private static ModLog sink;

        public static void Bind(ModManifest manifest)
        {
            try
            {
                if (manifest != null)
                {
                    sink = new ModLog(manifest);
                }
            }
            catch
            {
            }
        }

        public static void Msg(string text)
        {
            if (sink != null)
            {
                sink.Log(text);
            }

            Debug.Log("[要不要进货] " + text);
        }

        public static void Warn(string text)
        {
            if (sink != null)
            {
                sink.Log(text);
            }

            Debug.LogWarning("[要不要进货] " + text);
        }
    }
}
