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
    partial class Program : MyGridProgram
    {
        /// <summary>
        /// The grid terminal system replacement.
        /// </summary>
        private new readonly EnhancedGTS GridTerminalSystem;

        /// <summary>
        /// The user-customizable settings.
        /// </summary>
        private readonly Settings _settings;

        /// <summary>
        /// The list of ingot containers.
        /// </summary>
        private readonly List<IMyCargoContainer> _ingotContainers = new List<IMyCargoContainer>();

        private readonly List<IMyRefinery> _refineries = new List<IMyRefinery>();

        public Program()
        {
            GridTerminalSystem = new EnhancedGTS(this);
            _settings = new Settings(Me.CustomData);

            //GridTerminalSystem.GetBlocksOfType(_ingotContainers, container => _settings.IngotContainerKeywords.Any(keyword => container.CustomName.Contains(keyword)));
            //GridTerminalSystem.GetBlocksOfType(_refineries, refinery => _settings.RefineryKeywords.Contains(refinery.CustomName));

            //Echo($"{_ingotContainers.Count()} | {_refineries.Count()}");

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            try
            {
                //var ingotContainersEnumerator = _ingotContainers.GetEnumerator();
                //var refineryOutput = new List<MyInventoryItem>();
                //// Transfer output inventory of each refinery to ingot containers.
                //// We assume that if an ingot container cannot be transferred to, it will remain so for the rest of this loop.
                //foreach (var refinery in _refineries)
                //{
                //    var refineryInventory = refinery.OutputInventory;
                //    refineryInventory.GetItems(refineryOutput);

                //    // Try to transfer each item in refinery output to an ingot container.
                //    foreach (var item in refineryOutput)
                //    {
                //        var success = refineryInventory.TransferItemTo(ingotContainersEnumerator.Current.GetInventory(), item);
                //        while (!success)
                //        {
                //            ingotContainersEnumerator.MoveNext();
                //            success = refineryInventory.TransferItemTo(ingotContainersEnumerator.Current.GetInventory(), item);
                //        }
                //    }
                //}

                Echo($"{Runtime.LastRunTimeMs}");
                //GridTerminalSystem.GetBlocksOfType(_refineries, refinery => _settings.RefineryKeywords.Any(keyword => refinery.CustomName.Contains(keyword)));
                GridTerminalSystem.GetBlocksOfType(_refineries);
                _refineries.Select(refinery => _settings.RefineryKeywords.Any(keyword => refinery.CustomName.Contains(keyword)));
                //foreach (var refinery in _refineries)
                //{
                //    if (_settings.RefineryKeywords.Any(keyword => refinery.CustomName.Contains(keyword)))
                //    {
                //        Echo("Refinery matches keywords");
                //    }
                //}
            }
            catch (Exception e)
            {
                Echo("An error occurred during script execution.");
                Echo($"Exception: {e}\n---");

                throw;
            }
        }
    }
}
