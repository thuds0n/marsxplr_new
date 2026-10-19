using System;
using System.Collections.Generic;
using UnityEngine;

// Port of WhirldObject.js and WhirldData.js. World scenes such as Fort Boxy
// store per-object world parameters (the welcome message, the "marsxplr"
// server settings, "cc") in this component; the .meta reuses the script GUID
// those scenes reference, so their data deserialises into it.
public class WhirldObjectModern : MonoBehaviour
{
    [Serializable]
    public class WhirldData
    {
        public string n;
        public string v;
        public UnityEngine.Object o;
    }

    public WhirldData[] data = new WhirldData[0];

    public Dictionary<string, object> Params { get; private set; }

    private void Awake()
    {
        Activate();
    }

    public void Activate()
    {
        if (Params != null || data == null || data.Length == 0) return;
        Params = new Dictionary<string, object>();
        foreach (var dat in data)
        {
            if (dat == null || string.IsNullOrEmpty(dat.n)) continue;
            Params[dat.n] = dat.o != null ? (object)dat.o : dat.v;
        }
    }
}
