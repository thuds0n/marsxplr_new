using System.Collections.Generic;
using UnityEngine;

public class Messaging : MonoBehaviour
{
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

    private void Update()
    {
        // Temporary parity path while full chat GUI is still being rebuilt.
        if (chatting) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            SubmitInput("/r");
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            SubmitInput("x");
        }
    }

    public void AddMessage(string text, int origin)
    {
        entries.Add(new ChatEntry { text = text, origin = origin });
        if (entries.Count > 50) entries.RemoveAt(0);
        scrollPosition = new Vector2(scrollPosition.x, 1_000_000f);
        Debug.Log("[Messaging] " + text);
    }

    public void SubmitInput(string rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return;
        }

        string input = rawInput.Trim();
        string lower = input.ToLowerInvariant();

        if (lower == "r" || lower == "/r")
        {
            TriggerResetCommand();
            return;
        }

        if (lower == "x" || lower == "/x")
        {
            ToggleXorbCommand();
            return;
        }

        // Local-only fallback chat text while MP/chat UI is still migrating.
        if (Game.Controller != null)
        {
            Game.Controller.msg(input, 0);
        }
    }

    private void TriggerResetCommand()
    {
        if (Game.Settings == null || Game.Player == null)
        {
            AddMessage("Reset unavailable.", 2);
            return;
        }

        Game.Settings.resetTime = Time.time;
        var rb = Game.Player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        if (Game.Controller != null)
        {
            Game.Controller.msg(Game.Player.name + " Resetting in 10 seconds...", 2);
        }
    }

    private void ToggleXorbCommand()
    {
        if (Game.Settings == null)
        {
            AddMessage("XORB unavailable.", 2);
            return;
        }

        if (Game.Settings.zorbSpeed <= 0f)
        {
            if (Game.Controller != null) Game.Controller.msg("XORBs Unavailable", 2);
            return;
        }

        Game.Settings.zorbEnabled = !Game.Settings.zorbEnabled;
        if (Game.Controller != null)
        {
            Game.Controller.msg("XORB " + (Game.Settings.zorbEnabled ? "Activated" : "Deactivated"), 2);
        }
    }
}
