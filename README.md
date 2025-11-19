# Erenshor Quest Helper (Enhanced)

A Quality of Life mod for **Erenshor** that adds visual quest markers over NPCs' heads, featuring color coding, an intelligent priority system, and smooth animations.

## 🌟 What's New in Version 2.0

This version is a complete rewrite of the original mod, bringing significant visual improvements, stability fixes, and a much easier installation process.

### 🎨 New Visual Indicators
The system now distinguishes quest types through color coding:
*   **Yellow:** Normal Quests (Story/One-time).
*   **Blue:** Repeatable Quests (Grind Quests).
*   **Gray:** Quest in progress (accepted but missing required items).

### 🧠 Intelligent Priority System
To prevent visual clutter, markers follow a strict display hierarchy. The mod will always prioritize and show only the most important icon:
1.  **Turn In Normal Quest (Yellow)** (Highest Priority)
2.  **Turn In Repeatable Quest (Blue)**
3.  **New Normal Quest (Yellow)**
4.  **New Repeatable Quest (Blue)**
5.  **Quest In Progress (Gray)**

*Example: If an NPC has a quest ready to turn in and a new one to pick up, only the turn-in question mark will appear.*

### ✨ Quality of Life Improvements
*   **Single File Installation:** No need to move image folders manually. All assets are now **embedded resources** within the DLL.
*   **Floating Animation:** Icons now feature a smooth bobbing animation to feel more integrated into the game world.
*   **Scale & Position:** Markers have been resized and repositioned to sit closer to the NPC's head for a cleaner look.
*   **Stability Fixes:** Added comprehensive **Null Checks** to prevent console errors or crashes when encountering NPCs with empty quest lists.

---

## 📖 Icon Legend

| Icon | Color | Meaning |
| :---: | :---: | :--- |
| **!** | 🟡 **Yellow** | **New Quest Available** (One-time/Story). |
| **!** | 🔵 **Blue** | **New Repeatable Quest** available. |
| **?** | 🟡 **Yellow** | **Quest Complete** (Ready to turn in). |
| **?** | 🔵 **Blue** | **Repeatable Quest Complete** (Ready to turn in). |
| **?** | ⚪ **Gray** | **In Progress** (Quest accepted, but missing items). |

---

## 📥 Installation

1. Download and install [BepInEx](https://github.com/BepInEx/BepInEx) for Erenshor (if you haven't already).
2. Download the `quest-helper.dll` file from the [Releases] tab.
3. Drop the file into your plugins folder: `\BepInEx\plugins\`.
4. Done!

*(Note: If you were using the previous version of this mod, please delete the old `drizzlx-ErenshorQuestHelper` folder before installing this one).*

---

## 🛠️ Credits

*   Based on the original work by **drizzlx**.
*   Logic, visuals, and v2.0 implementation by **LucasXK**.
