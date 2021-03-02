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
        /// The list of component containers.
        /// </summary>
        private readonly List<IMyCargoContainer> _componentContainers = new List<IMyCargoContainer>();

        /// <summary>
        /// The list of refineries.
        /// </summary>
        private readonly List<IMyRefinery> _refineries = new List<IMyRefinery>();

        /// <summary>
        /// The list of assemblers.
        /// </summary>
        private readonly List<IMyAssembler> _assemblers = new List<IMyAssembler>();

        public Program()
        {
            GridTerminalSystem = new GridTerminalSystemV2(this);
            _settings = new Settings(Me.CustomData);

            GridTerminalSystem.SearchBlocksWithKeywords(_settings.IngotContainerKeywords, _ingotContainers);
            GridTerminalSystem.SearchBlocksWithKeywords(_settings.ComponentContainerKeywords, _componentContainers);
            GridTerminalSystem.SearchBlocksWithKeywords(_settings.RefineryKeywords, _refineries);
            GridTerminalSystem.SearchBlocksWithKeywords(_settings.AssemblerKeywords, _assemblers);

            Echo($"{_ingotContainers.Count()} | {_refineries.Count()}");
            Echo($"{_componentContainers.Count()} | {_assemblers.Count()}");

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            try
            {
                TransferOutOfProductionBlocks(_refineries, _ingotContainers);
                TransferOutOfProductionBlocks(_assemblers, _componentContainers);
            }
            catch (Exception e)
            {
                Echo("An error occurred during script execution.");
                Echo($"Exception: {e}\n---");

                throw;
            }
        }
        
        /// <summary>
        /// Transfer output of the specified production blocks into the specified containers.
        /// </summary>
        /// <param name="prodBlocks">The production blocks to transfer out of.</param>
        /// <param name="containers">The containers to transfer into.</param>
        private void TransferOutOfProductionBlocks(IEnumerable<IMyProductionBlock> prodBlocks, List<IMyCargoContainer> containers)
        {
            // We assume that if a container cannot be transferred to, it will remain so for the rest of the loop.
            var containersEnumerator = containers.GetEnumerator();
            containersEnumerator.MoveNext();
            var containerInventory = containersEnumerator.Current.GetInventory();

            // Transfer output inventory of each production block to containers.
            var prodItems = new List<MyInventoryItem>();
            foreach (var prodBlock in prodBlocks)
            {
                //Echo($"Checking {prodBlock.CustomName}");
                var prodInventory = prodBlock.OutputInventory;
                // GetItems() does not clear the list, we need to do this manually. 
                prodItems.Clear();
                prodInventory.GetItems(prodItems);

                // Try to transfer each item in refinery output to an ingot container.
                Echo($"{prodBlock.CustomName} has {prodItems.Count} items");
                foreach (var item in prodItems)
                {
                    var currentItemAmount = item.Amount;
                    Echo($"Trying to transfer {item.Type.SubtypeId} from {prodBlock.CustomName} to {containersEnumerator.Current.CustomName}");
                    while (true)
                    {
                        // TransferItemTo() returning true does not mean that the item actually transferred.
                        // So, we have to check the amounts manually.
                        var prevItemAmount = containerInventory.GetItemAmount(item.Type);
                        prodInventory.TransferItemTo(containerInventory, item);
                        var newItemAmount = containerInventory.GetItemAmount(item.Type);

                        if (newItemAmount - prevItemAmount >= currentItemAmount)
                        {
                            break;
                        }

                        var moved = containersEnumerator.MoveNext();
                        if (!moved)
                        {
                            break;
                        }

                        currentItemAmount -= newItemAmount - prevItemAmount;
                        containerInventory = containersEnumerator.Current.GetInventory();
                    }
                }
            }
        }
    }
}
