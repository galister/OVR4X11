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
                new [] {"GRAVE", "N1", "N2", "N3", "N4", "N5", "N6", "N7", "N8", "N9", "N0", "MINUS", "EQUAL", "BSPC"},
                new [] {"TAB", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "LBKT", "RBKT", "BSLH"},
                new [] {"EXEC1", "A", "S", "D", "F", "G", "H", "J", "K", "L", "SEMI", "SQT", "RET"},
                new [] {"LSHFT", "Z", "X", "C", "V", "B", "N", "M", "COMMA", "DOT", "FSLH", "RSHFT"},
                new [] {"LCTRL", "LGUI", "LALT", "SPACE", "RALT", "MENU", "RCTRL", "F16"}
            };
            
            alt_layout = new []
            {
                new [] {"ESC", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12", "DEL"},
                new [] {"TAB", "HOME", "UP", "END", "PG_UP", null, null, null, "KP_N7", "KP_N8", "KP_N9", "KP_NUMLOCK", null, "INS"},
                new [] {"EXEC2", "LEFT", "DOWN", "RIGHT", "PG_DN", null, null, null, "KP_N4", "KP_N5", "KP_N6", "KP_MINUS", "KP_ENTER"},
                new [] {"LSHFT", "PSCRN", "SCLK", "PAUSE", null, null, null, null, "KP_N1", "KP_N2", "KP_N3", "KP_PLUS"},
                new [] {"LCTRL", "LGUI", "LALT", null, "KP_N0", "KP_SLASH", "KP_MULTIPLY", "F17"}
            };

            exec_commands = new Dictionary<string, string[]>
            {
                ["EXEC1"] = new [] {"whisper_stt", "--lang", "en"},
                ["EXEC2"] = new [] {"whisper_stt", "--lang", "jp"}
            };

            display = new Dictionary<string, string>
            {
                ["N1"] = "1\n    !",
                ["N2"] = "2\n    @",
                ["N3"] = "3\n    #",
                ["N4"] = "4\n    $",
                ["N5"] = "5\n    %",
                ["N6"] = "6\n    ^",
                ["N7"] = "7\n    &",
                ["N8"] = "8\n    *",
                ["N9"] = "9\n    (",
                ["N0"] = "0\n    )",
                ["SPACE"] = "",
                ["RET"] = "Return",
                ["EXEC1"] = "STT EN  ",
                ["EXEC2"] = "STT JP  ",
                ["GRAVE"] = "`\n    ~",
                ["MINUS"] = "-\n    _",
                ["EQUAL"] = "=\n    +",
                ["LEFT"] = "←",
                ["RIGHT"] = "→",
                ["UP"] = "↑",
                ["DOWN"] = "↓",
                ["BSPC"] = "←",
                ["LSHFT"] = "Shift",
                ["RSHFT"] = "Shift",
                ["LCTRL"] = "Ctrl ",
                ["RCTRL"] = "Ctrl ",
                ["LGUI"] = "Super",
                ["LALT"] = "Alt  ",
                ["RALT"] = "Meta ",
                ["SEMI"] = " ;\n     :  ",
                ["SQT"]  = " '\n     \"  ",
                ["COMMA"] = " ,\n     <  ",
                ["DOT"]   = " .\n     >  ",
                ["FSLH"] = " /\n     ?  ",
                ["BSLH"] = " \\\n     |  ",
                ["LBKT"] = " [\n     {  ",
                ["RBKT"] = " ]\n     }  ",
                ["KP_SLASH"] = " /",
                ["KP_PLUS"] = " +",
                ["KP_MULTIPLY"] = " *",
                ["KP_DOT"] = " .",
                ["KP_MINUS"] = " -",
                ["KP_NUMLOCK"] = "NumLk",
                ["SCLK"]       = "ScrLk",
                ["PSCRN"]      = "PrtScn",
                ["PG_UP"]      = "Pg   Up",
                ["PG_DN"]      = "Pg   Dn",
                ["F16"]        = "PTT   ",
                ["F17"]        = "PTT2  "
            };

            // get yours from `xmodmap -pke`
            keycodes = new Dictionary<string, int>
            {
                ["ESC"] = 9,
                ["N1"] = 10,
                ["N2"] = 11,
                ["N3"] = 12,
                ["N4"] = 13,
                ["N5"] = 14,
                ["N6"] = 15,
                ["N7"] = 16,
                ["N8"] = 17,
                ["N9"] = 18,
                ["N0"] = 19,
                ["MINUS"] = 20,
                ["EQUAL"] = 21,
                ["BSPC"] = 22,
                ["TAB"] = 23,
                
                ["Q"] = 24,
                ["W"] = 25,
                ["E"] = 26,
                ["R"] = 27,
                ["T"] = 28,
                ["Y"] = 29,
                ["U"] = 30,
                ["I"] = 31,
                ["O"] = 32,
                ["P"] = 33,
                ["LBKT"] = 34,
                ["RBKT"] = 35,
                ["RET"] = 36,
                
                ["LCTRL"] = 37,
                ["A"] = 38,
                ["S"] = 39,
                ["D"] = 40,
                ["F"] = 41,
                ["G"] = 42,
                ["H"] = 43,
                ["J"] = 44,
                ["K"] = 45,
                ["L"] = 46,
                ["SEMI"] = 47,
                ["SQT"] = 48,
                ["GRAVE"] = 49,
                
                ["LSHFT"] = 50,
                ["BSLH"] = 51,
                ["Z"] = 52,
                ["X"] = 53,
                ["C"] = 54,
                ["V"] = 55,
                ["B"] = 56,
                ["N"] = 57,
                ["M"] = 58,
                ["COMMA"] = 59,
                ["DOT"] = 60,
                ["FSLH"] = 61,
                ["RSHFT"] = 62,
                ["KP_MULTIPLY"] = 63,
                ["LALT"] = 64,
                ["SPACE"] = 65,
                ["CAPS"] = 66,
                
                ["F1"] = 67,
                ["F2"] = 68,
                ["F3"] = 69,
                ["F4"] = 70,
                ["F5"] = 71,
                ["F6"] = 72,
                ["F7"] = 73,
                ["F8"] = 74,
                ["F9"] = 75,
                ["F10"] = 76,
                ["KP_NUMLOCK"] = 77,
                ["SCLK"] = 78,

                ["KP_N7"] = 79,
                ["KP_N8"] = 80,
                ["KP_N9"] = 81,
                ["KP_MINUS"] = 82,
                ["KP_N4"] = 83,
                ["KP_N5"] = 84,
                ["KP_N6"] = 85,
                ["KP_PLUS"] = 86,
                ["KP_N1"] = 87,
                ["KP_N2"] = 88,
                ["KP_N3"] = 89,
                ["KP_N0"] = 90,
                ["KP_DOT"] = 91,

                ["F11"] = 95,
                ["F12"] = 96,
                
                ["KP_ENTER"] = 104,
                ["RCTRL"] = 105,
                ["KP_SLASH"] = 106,
                ["PSCRN"] = 107,
                ["RALT"] = 108,
                ["HOME"] = 110,
                ["UP"] = 111,
                ["PG_UP"] = 112,
                ["LEFT"] = 113,
                ["RIGHT"] = 114,
                ["END"] = 115,
                ["DOWN"] = 116,
                ["PG_DN"] = 117,
                
                ["INS"] = 118,
                ["DEL"] = 119,
                
                ["PAUSE"] = 127,
                ["MENU"]  = 135,
                
                ["F15"] = 192,
                ["F16"] = 193,
                ["F17"] = 194,
                ["F18"] = 195,
                ["F19"] = 196,
                ["LGUI"] = 206,
                
                ["F13"] = 210,
                ["F14"] = 211,
            };
        }
    }
}