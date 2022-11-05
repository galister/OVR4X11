# DEPRECATION NOTICE

This project is no longer being worked on and is in a broken state.

If you need an overlay for SteamVR Linux, see [galister/X11Overlay](https://github.com/galister/X11Overlay) instead.






# OVR4X11
My personal attempt at a OpenVR desktop overlay for X11 desktops.

It's implemented using Unity and Xshm -- allows showing screens only, no individual windows.

Keyboard included with 2-hand typing support.

The source for the native library can be found here: https://github.com/galister/xshm-cap

# Requirements

The following libraries are needed:
- libX11.so
- libXtst.so
- libxcb.so
- libxcb-xfixes.so
- libxcb-randr.so
- libxcb-shm.so
- libxcb-xinerama.so

# Setup

- Install Unity 2021.03.x with Linux Build support.
- Create a 3D project.
- Edit -> Project settings -> XR Plug-in management:
  - Enable XR on startup
  - Enable OpenVR loader
  - OpenVR tab: App type: Overlay
  
- Import https://github.com/ValveSoftware/steamvr_unity_plugin/releases/tag/2.7.3
- Due to a bug, the Unity XR plugin loader will not work on Linux, until you manually link the libraries. In the Unity project's main folder (where Assets is):
  - ```
    mkdir -p lib/x64
    cd lib/x64
    n -s ../../Library/PackageCache/com.valvesoftware.unity.openvr@3ee6c452bc34/Runtime/x64/*.so .
    ```
    (The hash after openvr@... might be different for you.)

- cd into Assets and clone this repo.
- open the `Overlay` scene from inside the cloned folder
- launch SteamVR & press play

# SteamVR bindings:
- `Click`: keyboard typing and clicking on the screen. set this to your triggers.
- `Grip`: for moving overlays. Recommended: `Grip` input with pressure mode, pressure 70%. Release pressure 50%
- `AltClick`: not used right now
- `Pose`: set this to the controller tip.
- `Scroll`: set this to your joystick, and choose non-discrete mode.

# Pointer

The pointer changes mode depending on the orientation:
- Blue - left click - thumb upwards
- Yellow - right click - palm upwards
- Purple - middle click - backhand upwards

Up is relative to HMD up.

# Grabbing

Simply grab to move screens and the keyboard. Scroll and grab for extra effect, depends on the pointer mode:

- Blue pointer: move on the forward axis (close / far)
- Yellow pointer: change size

# Keyboard

The default layout is my personal 60% layout, reflecting my real life setup. Feel free to change it.

The keyboard also has 3 modes. The keys will change color to indicate the active mode. 

- Blue - regular keyboard
- Yellow - shift
- Purple - alternative layout

The color of the pointer that has remained on the keyboard the longest will determine the color of the keyboard.

# Customization

You can play around in the scene by changing GameObject positions and parents. The size of an overlay comes from its `width` property.

## Watch

The left side clock is your local time, and there are 2 more clocks on the right that are configurable at build time.

You can set the timezones in the scene, on the `/Playspace/ControllerL/Watch` gameobject.

Supported timezones can be found using `timedatectl list-timezones`

## Custom Keymap

Customize your keyboard layout in `MyLayout.cs`. The keycodes come from `xmodmap -pke`.

`main_layout` is for blue / yellow, while `alt_layout` is used for purple.

`exec_commands` can be used to map arbitrary shell commands to keys.

`macros` can be used to imitate a chain of key events with one keypress.

Supported macro commands:
- `Keyname DOWN`
- `Keyname UP`
- `Keyname` (short for DOWN and then UP)

Inside macros, separate individual commands using `;`

# Known Issues

- Grabbing windows that are overlapping and have the same Z distance is wonky. Just release and grab in a spot where they don't overlap.
- Battery indicator randomly shows 0%.
- Will crash the Unity editor if stopping from playmode. Disable the OverlayManager gameobject before pressing stop.
