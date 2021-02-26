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
        private const string ContainerName = "B Cargo I/O";
        private const string DisplaysBlockGroupName = "B I/O Amounts Displays";

        private string[] _ingotSubtypes = new[] { "Stone", "Iron", "Silicon", "Nickel", "Cobalt", "Silver", "Gold", "Platinum", "Magnesium" };
        private int[] _ingotThresholds = new[] { 1000, 5000, 1000, 1000, 500, 500, 0, -1, 100 };
        private string[] _oreSubtypes = new[] { "Ice" };
        private int[] _oreThresholds = new[] { -1 };

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

            var container = GridTerminalSystem.GetBlockWithName(ContainerName) as IMyCargoContainer;
            if (container == null)
            {
                Echo($"Could not find ingot/ore container with name {ContainerName}");
                return;
            }
            var inventory = container.GetInventory();

            var displaysBlockGroup = GridTerminalSystem.GetBlockGroupWithName(DisplaysBlockGroupName);
            if (displaysBlockGroup == null)
            {
                Echo($"Could not find block group for displays with name {DisplaysBlockGroupName}");
                return;
            }
            var displays = new List<IMyTerminalBlock>();
            displaysBlockGroup.GetBlocks(displays, block => true);

            var allAmounts = CalculateAllAmounts(inventory);
            var belowThreshold = allAmounts.Item1;
            var displayString = allAmounts.Item2;

            foreach (var displayBlock in displays)
            {
                var display = displayBlock as IMyTextPanel;
                if (display == null)
                {
                    continue;
                }

                display.ContentType = ContentType.TEXT_AND_IMAGE;
                display.FontSize = 1.4F;
                display.Alignment = TextAlignment.CENTER;

                // Add newline at beginning to center text vertically and add padding
                display.WriteText("\n", false);
                display.WriteText(displayString, true);

                if (belowThreshold)
                {
                    display.FontColor = Color.DarkRed;
                }
                else
                {
                    display.FontColor = Color.White;
                }
            }
        }

        // Calculate amounts of each type of ingot/ore specified
        // Returns a tuple, where the 1st item says whether any ingot/ore type is under their respective threshold,
        // while the 2nd item is the string to be displayed.
        private MyTuple<bool, string> CalculateAllAmounts(IMyInventory inventory)
        {
            var oreAmounts = CalculateAmounts(inventory, _oreSubtypes, _oreThresholds, MyItemType.MakeOre);
            var oresBelowThreshold = oreAmounts.Item1;
            var oresDisplayString = oreAmounts.Item2;

            var ingotAmounts = CalculateAmounts(inventory, _ingotSubtypes, _ingotThresholds, MyItemType.MakeIngot);
            var ingotsBelowThreshold = ingotAmounts.Item1;
            var ingotsDisplayString = ingotAmounts.Item2;

            var belowThreshold = oresBelowThreshold || ingotsBelowThreshold;
            var displayString = oresDisplayString + ingotsDisplayString;

            // Remove trailing newline
            displayString = displayString.TrimEnd();
            return new MyTuple<bool, string>(belowThreshold, displayString);
        }

        // Calculate amounts of each type of ingot/ore specified
        // Returns a tuple, where the 1st item says whether any ingot/ore type is under their respective threshold,
        // while the 2nd item is the string to be displayed.
        private MyTuple<bool, string> CalculateAmounts(
                IMyInventory inventory, string[] subtypes, int[] thresholds, Func<string, MyItemType> subtypeFunc)
        {
            var belowThreshold = false;
            var displayString = "";

            for (var i = 0; i < subtypes.Length; i++)
            {
                var subtype = subtypes[i];
                var threshold = thresholds[i];

                var itemType = subtypeFunc(subtype);
                var amount = inventory.GetItemAmount(itemType);
                var itemBelowThreshold = amount <= threshold;
                belowThreshold = belowThreshold || itemBelowThreshold;

                // Display in thousands
                var displayAmount = MyFixedPoint.MultiplySafe(MyFixedPoint.Floor(amount), 1.0f / 1000).ToString();
                displayAmount += "k";
                if (itemBelowThreshold)
                {
                    displayAmount += " (!!!)";
                }
                displayString += $"{subtype}: {displayAmount}\n";
            }

            return new MyTuple<bool, string>(belowThreshold, displayString);
        }
    }
}
