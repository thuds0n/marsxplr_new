// Compatibility facade retained for older migration references. Automatic
// single-player routing is owned solely by SinglePlayerStartupBootstrap.
public static class InitSceneRuntimeBootstrap
{
    public static bool ContinueSinglePlayerFlow()
    {
        return SinglePlayerStartupBootstrap.ContinueFromCurrentScene();
    }
}
