using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.Entities.Blocks;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    public static class LCDManager
    {
        public static List<IMyTextPanel> panels;
        private static string text = "";
        public static MyGridProgram program;

        public static void InitializePanels(List<IMyTextPanel> panels)
        {
            LCDManager.panels = panels;
            foreach (var panel in panels)
            {
                panel.ContentType = ContentType.TEXT_AND_IMAGE;
            }
        }
        public static void AddText(string text)
        {
            LCDManager.text += "\n" + text;
        }
        public static void WriteText()
        {
            foreach (var panel in panels)
            {
                try
                {
                    panel.WriteText(text);
                }
                catch
                {
                    panels.Remove(panel);
                }
                
            }
            program.Echo(text);
            text = "";
        }
    }
}
