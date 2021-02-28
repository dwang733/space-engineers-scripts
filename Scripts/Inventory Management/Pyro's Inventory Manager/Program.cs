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

        /// <summary>
        /// The list of refineries.
        /// </summary>
        private readonly List<IMyRefinery> _refineries = new List<IMyRefinery>();

        public Program()
        {
            GridTerminalSystem = new EnhancedGTS(this);
            _settings = new Settings(Me.CustomData);

            GridTerminalSystem.SearchBlocksWithKeywords(_settings.IngotContainerKeywords, _ingotContainers);
            GridTerminalSystem.SearchBlocksWithKeywords(_settings.RefineryKeywords, _refineries);

            Echo($"{_ingotContainers.Count()} | {_refineries.Count()}");

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            try
            {
                var ingotContainersEnumerator = _ingotContainers.GetEnumerator();
                ingotContainersEnumerator.MoveNext();

                // Transfer output inventory of each refinery to ingot containers.
                // We assume that if an ingot container cannot be transferred to, it will remain so for the rest of this loop.
                var refineryItems = new List<MyInventoryItem>();
                foreach (var refinery in _refineries)
                {
                    var refineryInventory = refinery.OutputInventory;
                    // GetItems() does not clear the list, we need to do this manually.
                    refineryItems.Clear();
                    refineryInventory.GetItems(refineryItems);

                    // Try to transfer each item in refinery output to an ingot container.
                    foreach (var item in refineryItems)
                    {
                        var success = refineryInventory.TransferItemTo(ingotContainersEnumerator.Current.GetInventory(), item);
                        while (!success)
                        {
                            ingotContainersEnumerator.MoveNext();
                            success = refineryInventory.TransferItemTo(ingotContainersEnumerator.Current.GetInventory(), item);
                        }
                    }
                }
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
