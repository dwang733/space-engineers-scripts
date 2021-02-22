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
        private const string BroadcastTag = "RAWR Ice Lake #1";
        private const string ItemContainerName = "B Cargo Items";
        private const string RawrContainerName = "zzz Test Container";

        private IMyBroadcastListener _listener;

        public Program()
        {
            Echo("Script started!");
            _listener = IGC.RegisterBroadcastListener(BroadcastTag);
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            var itemContainer = GridTerminalSystem.GetBlockWithName(ItemContainerName) as IMyCargoContainer;
            if (itemContainer == null)
            {
                Echo($"Could not find item container with name {ItemContainerName}");
                return;
            }
            var itemContainerInventory = itemContainer.GetInventory();

            var rawrContainer = GridTerminalSystem.GetBlockWithName(RawrContainerName) as IMyCargoContainer;
            if (rawrContainer == null)
            {
                Echo($"Could not find item container with name {RawrContainerName}");
                return;
            }
            var rawrContainerInventory = rawrContainer.GetInventory();

            while (_listener.HasPendingMessage)
            {
                var igcMessage = _listener.AcceptMessage();
                if (!igcMessage.Tag.Equals(BroadcastTag))
                {
                    continue;
                }

                // Broadcast message has format (MyDefinitionId assemblerType, MyItemType inventoryType, int amount) 
                var message = igcMessage.Data as ImmutableList<MyTuple<string, string, int>>;
                foreach (var tuple in message)
                {
                    var assemblerType = MyDefinitionId.Parse(tuple.Item1);
                    var inventoryType = MyItemType.Parse(tuple.Item2);
                    var quotaAmount = tuple.Item3;

                    var inventoryItem = itemContainerInventory.FindItem(inventoryType);
                    if (!inventoryItem.HasValue)
                    {
                        Echo($"Could not find mapping from assembler to inventory item for {assemblerType.SubtypeName}");
                        continue;
                    }

                    var rawrAmount = rawrContainerInventory.GetItemAmount(inventoryType);
                    var quotaDiff = quotaAmount - rawrAmount;
                    if (quotaDiff <= 0)
                    {
                        continue;
                    }

                    var success = itemContainerInventory.TransferItemTo(rawrContainerInventory, inventoryItem.Value, quotaDiff);
                }
            }
        }
    }
}
