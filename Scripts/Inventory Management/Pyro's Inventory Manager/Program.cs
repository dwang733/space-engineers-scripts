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
        private new readonly GridTerminalSystemV2 GridTerminalSystem;

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
            GridTerminalSystem = new GridTerminalSystemV2(this);
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
                    //Echo($"Checking refinery {refinery.CustomName}");
                    var refineryInventory = refinery.OutputInventory;
                    // GetItems() does not clear the list, we need to do this manually.
                    refineryItems.Clear();
                    refineryInventory.GetItems(refineryItems);

                    // Try to transfer each item in refinery output to an ingot container.
                    //Echo($"{refinery.CustomName} has {refineryItems.Count} items");
                    foreach (var item in refineryItems)
                    {
                        var currentItemAmount = item.Amount;
                        //Echo($"Trying to transfer {item.Type.SubtypeId} from {refinery.CustomName} to {ingotContainersEnumerator.Current.CustomName}");
                        foreach (var container in _ingotContainers)
                        {
                            // TransferItemTo() returning true does not mean that the item actually transferred.
                            // So, we have to check the amounts manually.
                            var containerInventory = container.GetInventory();
                            var prevItemAmount = containerInventory.GetItemAmount(item.Type);
                            refineryInventory.TransferItemTo(container.GetInventory(), item);
                            var newItemAmount = containerInventory.GetItemAmount(item.Type);

                            if (newItemAmount - prevItemAmount >= currentItemAmount)
                            {
                                break;
                            }
                            
                            currentItemAmount -= newItemAmount - prevItemAmount;
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
