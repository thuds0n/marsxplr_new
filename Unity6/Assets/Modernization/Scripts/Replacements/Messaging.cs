using System.Collections.Generic;
using UnityEngine;

// Port of Messaging.js: the "Messaging Console" window at the top left, its
// chat field and the chat commands. Offline there is nobody to send to, so a
// chat line is only added locally; the language filter is not ported.
public class Messaging : MonoBehaviour
{
    // Messaging.js chatOrigins.
    public const int Local = 0;
    public const int Remote = 1;
    public const int Server = 2;

    public bool chatting;
    public int showChat = -1;
    public Vector2 scrollPosition;

    [System.Serializable]
    public class ChatEntry
    {
        public string text = "";
        public int origin;
    }

    public readonly List<ChatEntry> entries = new List<ChatEntry>();

    private string _inputField = "";
    private Rect _windowRect;

    // Game.js msg().
    public void AddMessage(string text, int origin)
    {
        entries.Add(new ChatEntry { text = text, origin = origin });
        if (entries.Count > 50) entries.RemoveAt(0);
        scrollPosition = new Vector2(scrollPosition.x, 1_000_000f);
    }

    private void OnGUI()
    {
        if (Game.Settings == null || Game.Controller == null) return;
        if (Game.Settings.simplified) return;
        GUI.skin = Game.Controller.Skin;
        if (Game.Controller.loadingWorld) return;

        float width = Mathf.Min(Mathf.Max(170, Screen.width / 4), 250);
        if (showChat == 1 || showChat < 0)
        {
            _windowRect = new Rect(10, 40, width, Game.Settings.minimapAllowed && Game.Settings.useMinimap ? Screen.height * 0.75f - 60 : Screen.height - 55);
            GUI.Window(11, _windowRect, ChatWindow, "Messaging Console");
        }
        else if (GUI.Button(new Rect(10, 40, width, 25), "Messaging Console"))
        {
            showChat = 1;
            GUI.FocusControl("Chat input field");
        }
    }

    private void ChatWindow(int id)
    {
        GUIStyle closeButtonStyle = GUI.skin.GetStyle("close_button");
        if (GUI.Button(new Rect(closeButtonStyle.padding.left, closeButtonStyle.padding.top, closeButtonStyle.normal.background.width, closeButtonStyle.normal.background.height), "", "close_button"))
        {
            showChat = 0;
        }

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        foreach (ChatEntry entry in entries)
        {
            GUILayout.BeginHorizontal();
            if (entry.origin == Remote)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(entry.text, "chatRemote");
            }
            else if (entry.origin == Local)
            {
                GUILayout.Label(entry.text, "chatLocal");
                GUILayout.FlexibleSpace();
            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(entry.text, "chatServer");
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(3);
        }
        GUILayout.EndScrollView();

        GUILayout.FlexibleSpace();

        if (Event.current.type == EventType.KeyDown && Event.current.character == '\n' && _inputField.Length > 0)
        {
            SubmitInput(_inputField);
            _inputField = "";
            chatting = false;
            GUI.UnfocusWindow();
            // Unity 6 keeps the field's keyboard focus, which hides the next
            // key from Input (see the Tab handling below).
            GUIUtility.keyboardControl = 0;
        }

        GUI.SetNextControlName("Chat input field");
        _inputField = GUILayout.TextField(_inputField, 300);

        if (chatting && _inputField == "") GUILayout.Label("(Press \"Tab\" to Cancel)");
        else if (chatting) GUILayout.Label("(Press \"Enter\" to Send)");
        else GUILayout.Label("(Press \"Tab\" to Message)");

        if (chatting)
        {
            GUI.FocusControl("Chat input field");
            GUI.FocusWindow(id);
        }

        // Engine difference: Unity 6 does not report keys to Input while an
        // IMGUI text field has keyboard focus, so Tab pressed while chatting
        // only arrives as a GUI key event. Unity 2.x reported it to Input too.
        bool tabEvent = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Tab;
        bool tabDown = Input.GetKeyDown(KeyCode.Tab) || tabEvent;
        if (showChat < 0 && showChat > -5) showChat = showChat - 1;
        else if (showChat < 0 || ((tabDown || Input.GetKeyDown(KeyCode.Mouse0)) && Time.time > Game.Controller.kpTime))
        {
            Game.Controller.kpTime = Time.time + Game.Controller.kpDur;
            // The original tests the window rectangle against Input.mousePosition,
            // whose y runs upwards; kept as it was.
            if ((!chatting && !Input.GetKeyDown(KeyCode.Mouse0)) || (_windowRect.Contains(Input.mousePosition) && Input.GetKeyDown(KeyCode.Mouse0)))
            {
                if (showChat == 1) chatting = true;
                showChat = 1;
            }
            else
            {
                chatting = false;
                if (tabDown)
                {
                    GUI.UnfocusWindow();
                    GUIUtility.keyboardControl = 0;
                }
            }
        }
        if (tabEvent) Event.current.Use();
    }

    // Messaging.js ChatWindow(), on Enter: the "x" and "r" commands, else chat.
    public void SubmitInput(string input)
    {
        if (string.IsNullOrEmpty(input) || Game.Controller == null) return;

        if (input == "x" || input == "/x" || input == "/X")
        {
            // XORBs are not ported, so the command always gives the original's
            // answer for a game without them (sZ would otherwise switch one on).
            Game.Controller.msg("XORBs Unavailable", Server);
        }
        else if (input == "r" || input == "/r" || input == "/R")
        {
            if (Game.Settings == null || Game.Player == null) return;
            Game.Settings.resetTime = Time.time;
            var rb = Game.Player.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            Game.Controller.msg(Game.Player.name + " Resetting in 10 seconds...", Server);
        }
        else
        {
            input = Game.LanguageFilter(input);
            Game.Controller.msg(input, Local);
        }
    }
}
