using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Debug = UnityEngine.Debug;

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
        public Dictionary<string, string> labels;
        public Dictionary<string, string> macros;
        public Dictionary<string, int> keycodes = new();

        public string NameOfKey(string key, bool shift = false)
        {
            if (labels.TryGetValue(key, out name))
                return name;
            
            if (key.StartsWith("KP_"))
                key = key[3..];
            
            if (key.Contains("_"))
                key = key.Split('_').First();

            return Char.ToUpperInvariant(key[0]) + key[1..].ToLowerInvariant();
        }

        private static readonly Regex macroRx = new(@"([A-Za-z0-1_-]+)(?: (UP|DOWN))?;?", RegexOptions.Compiled);
        public List<(int, bool)> KeyEventsFromMacro(string s)
        {
            var l = new List<(int, bool)>();

            foreach (Match m in macroRx.Matches(s))
            {
                if (m.Success)
                {
                    if (!keycodes.TryGetValue(m.Groups[1].Value, out var keycode))
                    {
                        Debug.Log($"Unknown keycode in macro: '{m.Groups[1].Value}'");
                        return new List<(int, bool)>();
                    }
                    
                    if (!m.Groups[2].Success)
                    {
                        l.Add((keycode, true));
                        l.Add((keycode, false));
                    }
                    else if (m.Groups[2].Value == "DOWN")
                        l.Add((keycode, true));
                    else if (m.Groups[2].Value == "UP")
                        l.Add((keycode, false));
                    else
                    {
                        Debug.Log($"Unknown key state in macro: '{m.Groups[2].Value}', looking for UP or DOWN.");
                        return new List<(int, bool)>();
                    }
                }
            }
            return l;
        }

        public bool LoadAndCheckConfig()
        {
            var regex = new Regex(@"^keycode +(\d+) = (.+)$", RegexOptions.Compiled | RegexOptions.Multiline);
            var output = Process.Start(
                new ProcessStartInfo("xmodmap", "-pke") { RedirectStandardOutput = true, UseShellExecute = false}
            )!.StandardOutput.ReadToEnd();
            
            foreach (Match match in regex.Matches(output))
                if (match.Success)
                    if (Int32.TryParse(match.Groups[1].Value, out var keyCode))
                        foreach (var exp in match.Groups[2].Value.Split(' '))
                            keycodes.TryAdd(exp, keyCode);


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
                        else if (!keycodes.TryGetValue(s, out _) && !macros.TryGetValue(s, out _))
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