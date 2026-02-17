using System;

[Serializable]
public sealed class GameWorldDescModern
{
    public string name = "";
    public string url = "";
    public bool featured;

    public GameWorldDescModern() { }

    public GameWorldDescModern(string n, string u, bool feat)
    {
        name = n;
        url = u;
        featured = feat;
    }
}

public static class GameDataModern
{
    public static float gameVersion = 2.22f;
    public static float serverVersion = 0.2f;
    public static string gameName = "marsxplr";

    public static string userName = "";
    public static string userCode = "";
    public static string errorMessage = "";
    public static string masterBlacklist = "";
    public static System.Array gameWorlds;
    public static int networkMode;
}
