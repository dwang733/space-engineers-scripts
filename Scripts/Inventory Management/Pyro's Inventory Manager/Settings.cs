using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
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
    partial class Program
    {
        /// <summary>
        /// Represents user-customizable settings.
        /// </summary>
        public class Settings
        {
            private readonly MyIni _myIni = new MyIni();

            public List<string> IngotContainerKeywords => _myIni.GetRequiredAsList<string>("Settings", nameof(IngotContainerKeywords));

            public List<string> ComponentContainerKeywords => _myIni.GetRequiredAsList<string>("Settings", nameof(ComponentContainerKeywords));

            public List<string> RefineryKeywords => _myIni.GetRequiredAsList<string>("Settings", nameof(RefineryKeywords));

            public List<string> AssemblerKeywords => _myIni.GetRequiredAsList<string>("Settings", nameof(AssemblerKeywords));

            public Settings(string configuration)
            {
                MyIniParseResult result;
                var success = _myIni.TryParse(configuration, out result);
                if (!success)
                {
                    throw new ArgumentException($"Could not parse INI configuration while getting settings: {result.Error}");
                }
            }
        }
    }
}
