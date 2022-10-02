using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyOverlay.X11Keyboard
{
    public class EasyKeyboardConfig
    {
        public string name;
        public int row_size;
        public float[][] sizes;
        public string[][] main_layout;
        public string[][] alt_layout;
        public Dictionary<string, string[]> exec_commands;
        public Dictionary<string, string> display;
        public Dictionary<string, int> keycodes;

        public string NameOfKey(string key, bool shift = false)
        {
            if (display.TryGetValue(key, out name))
                return name;
            
            if (key.Length == 1 && Char.IsLetter(key[0]))
                return shift ? key.ToUpperInvariant() : key.ToLowerInvariant();

            if (key.StartsWith("N") && Char.IsDigit(key[1]))
                return key[1..];

            if (key.StartsWith("KP_"))
                key = key[3..];

            return Char.ToUpperInvariant(key[0]) + key[1..].ToLowerInvariant();
        }

        public bool CheckConfig()
        {
            for (var i = 0; i < sizes.Length; i++)
            {
                var row = sizes[i];
                var rowWidth = row.Sum();
                if (rowWidth - row_size > Single.Epsilon)
                {
                    Debug.Log($"Sizes, row {i}: Want {row_size} units of width, got {rowWidth}!");
                    return false;
                }
            }
            
            foreach (var layout in new [] { main_layout, alt_layout })
            {
                var layoutName = layout == main_layout ? "main" : "alt";

                for (var i = 0; i < sizes.Length; i++)
                {
                    if (sizes[i].Length != layout[i].Length)
                    {
                        Debug.Log(
                            $"{layoutName} layout, row {i}: Want {sizes[i].Length} buttons, got {layout[i].Length}!");
                        return false;
                    }

                    foreach (var s in layout[i])
                    {
                        if (s == null)
                            continue;
                        if (s.StartsWith("EXEC"))
                        {
                            if ( !exec_commands.TryGetValue(s, out _)){
                                Debug.Log($"{layoutName} layout, row {i}: Exec command is not known for {s}! ");
                                return false;
                            }
                        }
                        else if (!keycodes.TryGetValue(s, out _))
                        {
                            Debug.Log($"{layoutName} layout, row {i}: Keycode is not known for {s}! ");
                            return false;
                        }
                    }
                }
            }
            return true;
        }

    }
}