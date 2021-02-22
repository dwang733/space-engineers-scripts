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
        /// The flag that is set when this requester should broadcast via IGC.
        /// </summary>
        private const string BroadcastFlag = "broadcast";

        /// <summary>
        /// The broadcast tag used to broadcast.
        /// </summary>
        private const string BroadcastTag = "RAWR Ice Lake #1";

        /// <summary>
        /// The name of the assembler that is requesting the items.
        /// </summary>
        private const string RequesterAssemblerName = "zzz Test";
        //private const string RequesterAssemblerName = "IL RAWR Requester";

        /// <summary>
        /// The utility class that can read command line flags and arguments.
        /// </summary>
        private readonly MyCommandLine _commandLine = new MyCommandLine();

        /// <summary>
        /// The assembler that is requesting the items, as a <see cref="QuotaAssembler"/>
        /// </summary>
        private readonly QuotaAssembler _requesterAssembler;

        public Program()
        {
            Echo("Script started!");
            var assembler = GridTerminalSystem.GetBlockWithName(RequesterAssemblerName) as IMyAssembler;
            if (assembler == null)
            {
                Echo($"Could not find requester assembler with name {RequesterAssemblerName}");
                throw new ArgumentNullException(nameof(assembler));
            }

            _requesterAssembler = new QuotaAssembler(this, assembler);
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            try
            {
                // Update quota assembler
                var quotaDict = _requesterAssembler.Update();

                if (!_commandLine.Switch(BroadcastFlag))
                {
                    return;
                }

                // Broadcast message has format (MyDefinitionId assemblerType, MyItemType inventoryType, int amount) 
                var messageTuples = new List<MyTuple<string, string, int>>();
                // Convert production queue items to a type used by inventories
                foreach (var entry in quotaDict)
                {
                    var blueprintId = entry.Key;
                    var amount = entry.Value;

                    var inventoryItemNullable = ItemTypeHelper.AssemblerToInventory(blueprintId);
                    if (inventoryItemNullable == null)
                    {
                        Echo($"Could not find mapping from assembler to inventory item for {blueprintId.SubtypeName}");
                        return;
                    }

                    var inventoryItemType = inventoryItemNullable.Value;
                    var broadcastItem = new MyTuple<string, string, int>(blueprintId.ToString(), inventoryItemType.ToString(), amount);
                    messageTuples.Add(broadcastItem);
                    Echo($"{blueprintId.SubtypeName}, {inventoryItemType.SubtypeId}, {amount}");
                }

                Echo("Sending message via IGC!");
                var broadcastMessage = messageTuples.ToImmutableList();
                IGC.SendBroadcastMessage(BroadcastTag, broadcastMessage);
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
