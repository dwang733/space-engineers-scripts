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
        private const string ItemContainerName = "B Cargo Items";
        private const string CargoHelpersBlockGroupName = "B Cargo Items Helpers";

        public Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.
            // 
            // It's recommended to set RuntimeInfo.UpdateFrequency 
            // here, which will allow your script to run itself without a 
            // timer block.
            Echo("Script started!");
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            // The main entry point of the script, invoked every time
            // one of the programmable block's Run actions are invoked,
            // or the script updates itself. The updateSource argument
            // describes where the update came from.
            // 
            // The method itself is required, but the arguments above
            // can be removed if not needed.

            var itemContainer = GridTerminalSystem.GetBlockWithName(ItemContainerName);
            if (itemContainer == null)
            {
                Echo($"Could not find item container with name {ItemContainerName}");
                return;
            }
            var itemContainerInventory = itemContainer.GetInventory();

            // Get by block group name, so block group can exclude this programming block
            var cargoHelpersBlockGroup = GridTerminalSystem.GetBlockGroupWithName(CargoHelpersBlockGroupName);
            if (cargoHelpersBlockGroup == null)
            {
                Echo($"Could not find block group for cargo helpers with name {CargoHelpersBlockGroupName}");
            }
            var cargoHelpers = new List<IMyCargoContainer>();
            cargoHelpersBlockGroup.GetBlocksOfType(cargoHelpers);

            var helperItems = new List<MyInventoryItem>();
            foreach (var cargoHelper in cargoHelpers)
            {
                var helperInventory = cargoHelper.GetInventory();
                if (helperInventory == null)
                {
                    continue;
                }

                helperInventory.GetItems(helperItems, item => true);
                foreach (var helperItem in helperItems)
                {
                    // Last null parameter should pull maximum amount possible
                    helperInventory.TransferItemTo(itemContainerInventory, helperItem, null);
                }
            }
        }
    }
}
