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
        private readonly GTSHelper _gtsHelper;

        public Program()
        {
            _gtsHelper = new GTSHelper(this);

            //Echo("Testing GTSHelper.GetBlocks()");
            //var allBlocks = new List<IMyTerminalBlock>();
            //_gtsHelper.GetBlocks(allBlocks);
            //Echo($"Is the result null? - {allBlocks == null}");
            //Echo($"Num of blocks returned - {allBlocks.Count}");

            //Echo("Testing GTSHelper.GetBlockGroups()");
            //var allBlockGroups = new List<IMyBlockGroup>();
            //_gtsHelper.GetBlockGroups(allBlockGroups);
            //Echo($"Is the result null? - {allBlockGroups == null}");
            //Echo($"Num of block groups returned - {allBlockGroups.Count}");

            //Echo("Testing GTSHelper.GetBlocksOfType()");
            //var blocksOfType = new List<IMyProgrammableBlock>();
            //_gtsHelper.GetBlocksOfType(blocksOfType, mustBeSameConstruct: true);
            //Echo($"Is the result null? - {blocksOfType == null}");
            //Echo($"Num of blocks returned - {blocksOfType.Count}");
            //foreach (var block in blocksOfType)
            //{
            //    Echo(block.CustomName);
            //}

            //Echo("Testing GTSHelper.SearchBlocksOfName()");
            //var blocksOfName = new List<IMyAirtightSlideDoor>();
            //_gtsHelper.SearchBlocksOfName("asdfasfd", blocksOfName, mustBeSameConstruct: true);
            //Echo($"Is the result null? - {blocksOfName == null}");
            //Echo($"Num of blocks returned - {blocksOfName.Count}");
            //foreach (var block in blocksOfName)
            //{
            //    Echo(block.CustomName);
            //}

            //Echo("Testing GTSHelper.GetBlockWithName()");
            //var blockByName = _gtsHelper.GetBlockWithName<IMyProgrammableBlock>("CF2 Status Report Program", mustBeSameConstruct: false);
            //Echo($"Is the result null? - {blockByName == null}");
            //Echo($"Block name: {blockByName?.CustomName}");

            //Echo("Testing GTSHelper.GetBlockGroupWithName()");
            //var blocksOfGroup = new List<IMyGravityGenerator>();
            //_gtsHelper.GetBlockGroupWithName("Player-activated internals", blocksOfGroup, mustBeSameConstruct: true);
            //Echo($"Is the result null? - {blocksOfGroup == null}");
            //Echo($"Num of blocks returned - {blocksOfGroup.Count}");
            //foreach (var block in blocksOfGroup)
            //{
            //    Echo(block.CustomName);
            //}

            //Echo("Testing GTSHelper.GetBlockWithId()");
            //var blockId = _gtsHelper.GetBlockWithName<IMyProgrammableBlock>("MSG Oxygen Management Program", mustBeSameConstruct: false).EntityId;
            //Echo($"EntityId: {blockId}");
            //var blockById = _gtsHelper.GetBlockWithId<IMyProgrammableBlock>(blockId, mustBeSameConstruct: false);
            //Echo($"Is the result null? - {blockById == null}");
            //Echo($"Block name: {blockById?.CustomName}");
        }

        public void Main(string argument, UpdateType updateSource)
        {
        }
    }
}
