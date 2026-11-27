namespace ShouldIRestock
{
    public sealed class ModEntry : IMod
    {
        public void Init(ModManifest manifest)
        {
            Log.Bind(manifest);
            Settings.Init();
            Log.Msg(Text.L("loaded", "Should I Restock is loaded. The note sticks under the phone once you are in the shop."));
        }

        public void OnEnable()
        {
            ModHook.OnGameLoadedNormal += OnStoreReady;
            ModHook.OnReturningStoreLate += OnStoreReady;
            ModHook.OnLeavingStoreEarly += OnLeaveStore;
            ModHook.OnGoingSleepEarly += OnSleep;
        }

        public void OnDisable()
        {
            ModHook.OnGameLoadedNormal -= OnStoreReady;
            ModHook.OnReturningStoreLate -= OnStoreReady;
            ModHook.OnLeavingStoreEarly -= OnLeaveStore;
            ModHook.OnGoingSleepEarly -= OnSleep;
            TagHighlighter.Release();
            StickyNote.Destroy();
            PaperBoard.Close();
        }

        private static void OnStoreReady()
        {
            StoreDriver.Wake();
            SceneProbe.DumpOnce();
            StickyNote.Follow();
        }

        private static void OnLeaveStore()
        {
            TagHighlighter.ClearHover();
            OutlinePainter.Clear();
            TabPlatePainter.Clear();
            StickyNote.Destroy();
        }

        private static void OnSleep()
        {
            PaperBoard.Close();
        }
    }
}
