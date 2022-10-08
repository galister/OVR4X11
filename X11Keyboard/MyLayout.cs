using System.Collections.Generic;

namespace EasyOverlay.X11Keyboard
{
    public class MyLayout : EasyKeyboardConfig
    {
        public MyLayout()
        {
            name = "en-us_iso";
            row_size = 15;

            sizes = new[]
            {
                new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2f },
                new[] { 1.5f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1.5f },
                new[] { 1.75f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2.25f },
                new[] { 2.25f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2.75f },
                new[] { 1.25f, 1.25f, 1.25f, 6.25f, 1.25f, 1.25f, 1.25f, 1.25f }
            };
            
            main_layout = new[]
            {
                new [] {"grave", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "minus", "equal", "BackSpace"},
                new [] {"Tab", "q", "w", "e", "r", "t", "y", "u", "i", "o", "p", "bracketleft", "bracketright", "backslash"},
                new [] {"XF86Favorites", "a", "s", "d", "f", "g", "h", "j", "k", "l", "semicolon", "apostrophe", "Return"},
                new [] {"Shift_L", "z", "x", "c", "v", "b", "n", "m", "comma", "period", "slash", "Shift_R"},
                new [] {"Control_L", "Super_L", "Alt_L", "space", "Meta_R", "Menu", "Control_R", "EXEC1"}
            };
            
            alt_layout = new []
            {
                new [] {"Escape", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12", "Delete"},
                new [] {"Tab", "Home", "Up", "End", "Prior", null, null, null, "KP_7", "KP_8", "KP_9", "Num_Lock", null, "Insert"},
                new [] {"KILL", "Left", "Down", "Right", "Next", null, null, null, "KP_4", "KP_5", "KP_6", "KP_Subtract", "KP_Enter"},
                new [] {"Shift_L", "Print", "Scroll_Lock", "Pause", null, null, null, null, "KP_1", "KP_2", "KP_3", "KP_Add"},
                new [] {"Control_L", "Super_L", "Alt_L", null, "KP_0", "KP_Divide", "KP_Multiply", "KP_Decimal"}
            };

            exec_commands = new Dictionary<string, string[]>
            {
                ["EXEC1"] = new [] {"whisper_stt", "--lang", "en"},
            };

            macros = new Dictionary<string, string>
            {
                ["KILL"] = "Super_L DOWN; Control_L DOWN; Escape; Control_L UP; Super_L UP"
            };

            labels = new Dictionary<string, string>
            {
                ["1"] = "1\n    !",
                ["2"] = "2\n    @",
                ["3"] = "3\n    #",
                ["4"] = "4\n    $",
                ["5"] = "5\n    %",
                ["6"] = "6\n    ^",
                ["7"] = "7\n    &",
                ["8"] = "8\n    *",
                ["9"] = "9\n    (",
                ["0"] = "0\n    )",
                ["space"] = "",
                ["Delete"] = "Del",
                ["EXEC1"] = "STT",
                ["grave"] = "`\n    ~",
                ["minus"] = "-\n    _",
                ["equal"] = "=\n    +",
                ["Left"] = "←",
                ["Right"] = "→",
                ["Up"] = "↑",
                ["Down"] = "↓",
                ["BackSpace"] = "←",
                ["Control_L"] = "Ctrl ",
                ["Control_R"] = "Ctrl ",
                ["semicolon"] = " ;\n     :  ",
                ["apostrophe"]  = " '\n     \"  ",
                ["comma"] = " ,\n     <  ",
                ["period"]   = " .\n     >  ",
                ["slash"] = " /\n     ?  ",
                ["backslash"] = " \\\n     |  ",
                ["bracketleft"] = " [\n     {  ",
                ["bracketright"] = " ]\n     }  ",
                ["KP_Divide"] = " /",
                ["KP_Add"] = " +",
                ["KP_Multiply"] = " *",
                ["KP_Decimal"] = " .",
                ["KP_Subtract"] = " -",
                ["XF86Favorites"] = "Rofi"
            };
        }
    }
}