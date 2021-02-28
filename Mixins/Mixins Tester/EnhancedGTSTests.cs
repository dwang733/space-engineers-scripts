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
        public class EnhancedGTSTests
        {
            public static void Test(Program program)
            {
                var gtsHelper = new EnhancedGTS(program);

                program.Echo("Testing GTSHelper.GetBlocks()");
                var allBlocks = new List<IMyTerminalBlock>();
                gtsHelper.GetBlocks(allBlocks);
                program.Echo($"Is the result null? - {allBlocks == null}");
                program.Echo($"Num of blocks returned - {allBlocks.Count}");

                program.Echo("Testing GTSHelper.GetBlockGroups()");
                var allBlockGroups = new List<IMyBlockGroup>();
                gtsHelper.GetBlockGroups(allBlockGroups);
                program.Echo($"Is the result null? - {allBlockGroups == null}");
                program.Echo($"Num of block groups returned - {allBlockGroups.Count}");

                program.Echo("Testing GTSHelper.GetBlocksOfType()");
                var blocksOfType = new List<IMyProgrammableBlock>();
                gtsHelper.GetBlocksOfType(blocksOfType, mustBeSameConstruct: true);
                program.Echo($"Is the result null? - {blocksOfType == null}");
                program.Echo($"Num of blocks returned - {blocksOfType.Count}");
                foreach (var block in blocksOfType)
                {
                    program.Echo(block.CustomName);
                }

                program.Echo("Testing GTSHelper.SearchBlocksOfName()");
                var blocksOfName = new List<IMyProgrammableBlock>();
                gtsHelper.SearchBlocksOfName("Programmable block", blocksOfName, mustBeSameConstruct: true);
                program.Echo($"Is the result null? - {blocksOfName == null}");
                program.Echo($"Num of blocks returned - {blocksOfName.Count}");
                foreach (var block in blocksOfName)
                {
                    program.Echo(block.CustomName);
                }

                program.Echo("Testing GTSHelper.SearchBlocksWithKeywords()");
                var blocksByKeywords = new List<IMyProgrammableBlock>();
                gtsHelper.SearchBlocksWithKeywords(new[] { "Programmable block" }, blocksByKeywords, mustBeSameConstruct: true);
                program.Echo($"Is the result null? - {blocksOfName == null}");
                program.Echo($"Num of blocks returned - {blocksOfName.Count}");
                foreach (var block in blocksOfName)
                {
                    program.Echo(block.CustomName);
                }

                program.Echo("Testing GTSHelper.GetBlockWithName()");
                var blockByName = gtsHelper.GetBlockWithName<IMyProgrammableBlock>("Programmable block", mustBeSameConstruct: true);
                program.Echo($"Is the result null? - {blockByName == null}");
                program.Echo($"Block name: {blockByName?.CustomName}");

                program.Echo("Testing GTSHelper.GetBlockGroupWithName()");
                var blocksOfGroup = new List<IMyProgrammableBlock>();
                gtsHelper.GetBlockGroupWithName("Programmable block", blocksOfGroup, mustBeSameConstruct: true);
                program.Echo($"Is the result null? - {blocksOfGroup == null}");
                program.Echo($"Num of blocks returned - {blocksOfGroup.Count}");
                foreach (var block in blocksOfGroup)
                {
                    program.Echo(block.CustomName);
                }

                program.Echo("Testing GTSHelper.GetBlockWithId()");
                var blockId = gtsHelper.GetBlockWithName<IMyProgrammableBlock>("Programmable block", mustBeSameConstruct: true).EntityId;
                program.Echo($"EntityId: {blockId}");
                var blockById = gtsHelper.GetBlockWithId<IMyProgrammableBlock>(blockId, mustBeSameConstruct: true);
                program.Echo($"Is the result null? - {blockById == null}");
                program.Echo($"Block name: {blockById?.CustomName}");
            }
        }
    }
}
