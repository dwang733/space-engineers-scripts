using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
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
        /// The number of ticks after an item, that is added to production to fulfill a deficit, is finished producing
        /// before it is removed from the set of producing items.
        /// </summary>
        private const int NumTicksAfterProductionFinished = 1000;

        /// <summary>
        /// The name of the container that contains the items/components.
        /// </summary>
        private const string ItemContainerName = "B Cargo Items";

        /// <summary>
        /// The name of the block group containing the production assemblers (master + slaves).
        /// </summary>
        private const string ProductionAssemblersBlockGroupName = "B Assemblers";
        
        /// <summary>
        /// The name of the master production assembler.
        /// </summary>
        private const string ProductionMasterAssemblerName = "B Assembler Master";

        /// <summary>
        /// The name of the assembler that is requesting the items.
        /// </summary>
        private const string RequesterAssemblerName = "B RAL Requester";
        
        /// <summary>
        /// The container that contains the items/components. 
        /// </summary>
        private readonly IMyCargoContainer _itemContainer;

        /// <summary>
        /// The master production assembler.
        /// </summary>
        private readonly IMyAssembler _productionMasterAssembler;

        /// <summary>
        /// The set of items that are being produced because of a deficit in the quota,
        /// mapped to how many ticks it's been since it's been out of production.
        /// </summary>
        private readonly Dictionary<MyDefinitionId, int> _itemsBelowQuota = new Dictionary<MyDefinitionId, int>();

        /// <summary>
        /// The assembler that is requesting the items, as a <see cref="QuotaAssembler"/>
        /// </summary>
        private readonly QuotaAssembler _requesterAssembler;

        public Program()
        {
            Echo("Script started!");

            _itemContainer = GridTerminalSystem.GetBlockWithName(ItemContainerName) as IMyCargoContainer;
            if (_itemContainer == null)
            {
                Echo($"Could not find item container with name {ItemContainerName}");
                throw new ArgumentNullException(nameof(_itemContainer));
            }

            _productionMasterAssembler = GridTerminalSystem.GetBlockWithName(ProductionMasterAssemblerName) as IMyAssembler;
            if (_productionMasterAssembler == null)
            {
                Echo($"Could not find production master assembler with name {ProductionMasterAssemblerName}");
                throw new ArgumentNullException(nameof(_productionMasterAssembler));
            }

            var requesterAssembler = GridTerminalSystem.GetBlockWithName(RequesterAssemblerName) as IMyAssembler;
            if (requesterAssembler == null)
            {
                Echo($"Could not find requester assembler with name {RequesterAssemblerName}");
                throw new ArgumentNullException(nameof(requesterAssembler));
            }
            _requesterAssembler = new QuotaAssembler(this, requesterAssembler);

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            try
            {
                // Update quota assembler
                var quotaDict = _requesterAssembler.Update();
                if (quotaDict == null)
                {
                    return;
                }

                // Get all assemblers in the production assemblers block group.
                var assemblersBlockGroup = GridTerminalSystem.GetBlockGroupWithName(ProductionAssemblersBlockGroupName);
                if (assemblersBlockGroup == null)
                {
                    Echo($"Could not find block group for production assemblers with name {ProductionAssemblersBlockGroupName}");
                    return;
                }
                var assemblers = new List<IMyAssembler>();
                assemblersBlockGroup.GetBlocksOfType(assemblers);

                // Check if any items that were queued up for quota deficits have finished producing, and increment their ticks.
                if (_itemsBelowQuota.Count > 0)
                {
                    var totalAssemblerQueue = new List<MyProductionItem>();
                    var assemblerQueue = new List<MyProductionItem>();
                    // Get all items in production from all production assemblers.
                    foreach (var assembler in assemblers)
                    {
                        assembler.GetQueue(assemblerQueue);
                        totalAssemblerQueue.AddRange(assemblerQueue);
                    }

                    // Get all items that are still being produced and set their ticks to 0.
                    foreach (var productionItem in totalAssemblerQueue)
                    {
                        var blueprintId = productionItem.BlueprintId;
                        if (_itemsBelowQuota.ContainsKey(blueprintId))
                        {
                            _itemsBelowQuota[blueprintId] = 0;
                        }
                    }

                    // Get all items that don't have ticks set to 0, and increment by update frequency.
                    // Any items with a tick value beyond the threshold is eligible for removal from the set.
                    var removedKeys = new List<MyDefinitionId>();
                    foreach (var item in _itemsBelowQuota.Keys.ToList())
                    {
                        if (_itemsBelowQuota[item] >= 0)
                        {
                            _itemsBelowQuota[item] += 100;
                        }

                        if (_itemsBelowQuota[item] >= NumTicksAfterProductionFinished)
                        {
                            removedKeys.Add(item);
                        }
                    }

                    // Remove all the eligible items.
                    foreach (var key in removedKeys)
                    {
                        var success = _itemsBelowQuota.Remove(key);
                    }
                }

                // Check item container inventory for quota deficit
                var itemContainerInventory = _itemContainer.GetInventory();
                foreach (var quotaPair in quotaDict)
                {
                    var assemblerType = quotaPair.Key;
                    var quotaAmount = quotaPair.Value;
                    var inventoryType = ItemTypeHelper.AssemblerToInventory(assemblerType);
                    if (!inventoryType.HasValue)
                    {
                        Echo($"Could not find mapping from assembler to inventory item for {quotaPair.Key.SubtypeName}");
                        continue;
                    }

                    // Do not check inventory amount if we're producing to make up for a deficit.
                    if (_itemsBelowQuota.ContainsKey(assemblerType))
                    {
                        continue;
                    }

                    // Check if inventory amount is below quota amount.
                    var inventoryAmount = itemContainerInventory.GetItemAmount(inventoryType.Value);
                    var quotaDiff = quotaAmount - inventoryAmount;
                    if (quotaDiff <= 0)
                    {
                        continue;
                    }

                    // Add missing amount from quota into production
                    Echo($"Queueing additional {quotaDiff} {assemblerType.SubtypeName} for production");
                    _productionMasterAssembler.AddQueueItem(assemblerType, quotaDiff);
                    _itemsBelowQuota[assemblerType] = 0;
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
