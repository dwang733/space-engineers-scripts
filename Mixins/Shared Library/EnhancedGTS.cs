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
    partial class Program
    {
        /// <summary>
        /// <para>A replacement for the base Grid Terminal System.</para>
        /// <example>
        /// You can hide the base implementation with the following code in Program.cs:
        /// <code>
        /// new EnhancedGTS GridTerminalSystem;
        /// </code>
        /// </example>
        /// </summary>
        public class EnhancedGTS
        {
            private readonly MyGridProgram _program;

            public EnhancedGTS(MyGridProgram program)
            {
                _program = program;
            }

            /// <summary>
            /// Gets all the available blocks the Grid Terminal System has access to.
            /// This may include blocks that are not be on the same construct as the programmable block.
            /// </summary>
            /// <param name="blocks">The list of blocks that will be populated.</param>
            public void GetBlocks(List<IMyTerminalBlock> blocks)
            {
                // TODO: Filter by same construct
                _program.GridTerminalSystem.GetBlocks(blocks);
            }

            /// <summary>
            /// Gets a list of block groups the Grid Terminal System has access to.
            /// This may include blocks that are not on the same construct as the programmable block.
            /// </summary>
            /// <param name="blocks">The list of block groups that will be populated.</param>
            /// <param name="collect">An optional function to filter the block groups.</param>
            public void GetBlockGroups(
                List<IMyBlockGroup> blockGroups,
                Func<IMyBlockGroup, bool> collect = null)
            {
                // TODO: Filter by same construct
                _program.GridTerminalSystem.GetBlockGroups(blockGroups, collect);
            }

            /// <summary>
            /// Gets a list of blocks of the specified type.
            /// </summary>
            /// <typeparam name="T">The type of block to retrieve.</typeparam>
            /// <param name="blocks">The list of blocks that will be populated.</param>
            /// <param name="collect">An optional function to filter blocks of the specified type.</param>
            /// <param name="mustBeSameConstruct">Set to true if blocks must be on the same construct as the programmable block. True by default.</param>
            public void GetBlocksOfType<T>(
                List<T> blocks,
                Func<T, bool> collect = null,
                bool mustBeSameConstruct = true)
                where T : class, IMyTerminalBlock
            {
                if (mustBeSameConstruct)
                {
                    collect = CreateCollectWithSameConstructConstraint(collect);
                }

                _program.GridTerminalSystem.GetBlocksOfType(blocks, collect);
            }

            /// <summary>
            /// Searches all blocks of the specified type and returns those whose name contains the specified name
            /// (just like searching grid blocks via control panel).
            /// E.g. a block named "Mynoch" would be returned if you search for "no".
            /// </summary>
            /// <typeparam name="T">The type of block to search.</typeparam>
            /// <param name="name">The name to use in the search.</param>
            /// <param name="blocks">The list of blocks that will be populated.</param>
            /// <param name="collect">An optional function to filter blocks of the specified type.</param>
            /// <param name="mustBeSameConstruct">Set to true if blocks must be on the same construct as the programmable block. True by default.</param>
            public void SearchBlocksOfName<T>(
                string name,
                List<T> blocks,
                Func<T, bool> collect = null,
                bool mustBeSameConstruct = true)
                where T : class, IMyTerminalBlock
            {
                if (mustBeSameConstruct)
                {
                    collect = CreateCollectWithSameConstructConstraint(collect);
                }

                var allBlocks = new List<IMyTerminalBlock>();
                _program.GridTerminalSystem.SearchBlocksOfName(name, allBlocks);

                blocks.Clear();
                foreach (var genericBlock in allBlocks)
                {
                    var block = genericBlock as T;
                    if (block != null && (collect == null || collect(block)))
                    {
                        blocks.Add(block);
                    }
                }
            }

            /// <summary>
            /// Gets a single block of the specified type by its exact name.
            /// </summary>
            /// <typeparam name="T">The type of the block.</typeparam>
            /// <param name="name">The name of the block, which must be exact, case sensitive, and include any spacing.</param>
            /// <param name="mustBeSameConstruct">Set to true if blocks must be on the same construct as the programmable block. True by default.</param>
            /// <returns>The single block, or null if the block doesn't exist, or is not on the same construct as the programmable block if the check is enabled.</returns>
            public T GetBlockWithName<T>(
                string name,
                bool mustBeSameConstruct = true)
                where T : class, IMyTerminalBlock
            {
                var block = _program.GridTerminalSystem.GetBlockWithName(name) as T;
                if (block == null || (mustBeSameConstruct && !block.IsSameConstructAs(_program.Me)))
                {
                    return null;
                }

                return block;
            }

            /// <summary>
            /// Gets the blocks with the specified type in a block group by its exact name.
            /// </summary>
            /// <typeparam name="T">The type of block to get in the block group.</typeparam>
            /// <param name="name">The name of the block group, which must be exact, case sensitive, and include any spacing.</param>
            /// <param name="blocks">The list of blocks that will be populated.</param>
            /// <param name="collect">An optional function to filter blocks of the specified type in the block group.</param>
            /// <param name="mustBeSameConstruct">Set to true if blocks must be on the same construct as the programmable block. True by default.</param>
            public void GetBlockGroupWithName<T>(
                string name,
                List<T> blocks,
                Func<T, bool> collect = null,
                bool mustBeSameConstruct = true)
                where T : class, IMyTerminalBlock
            {
                var group = _program.GridTerminalSystem.GetBlockGroupWithName(name);
                if (group == null)
                {
                    blocks.Clear();
                    return;
                }

                if (mustBeSameConstruct)
                {
                    collect = CreateCollectWithSameConstructConstraint(collect);
                }

                group.GetBlocksOfType(blocks, collect);
            }

            /// <summary>
            /// Gets a single block of the specified type by its EntityId.
            /// </summary>
            /// <typeparam name="T">The type of the block.</typeparam>
            /// <param name="mustBeSameConstruct">Set to true if blocks must be on the same construct as the programmable block. True by default.</param>
            /// <returns>The single block, or null if the block doesn't exist, or is not on the same construct as the programmable block if the check is enabled.</returns>
            public T GetBlockWithId<T>(
                long id,
                bool mustBeSameConstruct = true)
                where T : class, IMyTerminalBlock
            {
                var block = _program.GridTerminalSystem.GetBlockWithId(id) as T;
                if (block == null || (mustBeSameConstruct && !block.IsSameConstructAs(_program.Me)))
                {
                    return null;
                }

                return block;
            }

            /// <summary>
            /// Creates a collect predicate with an additional constraint where the block must be on the same construct as the programmable block.
            /// </summary>
            /// <typeparam name="T">The type of block.</typeparam>
            /// <param name="collect">The collect predicate function.</param>
            /// <returns>A new collect predicate with a same construct constraint.</returns>
            private Func<T, bool> CreateCollectWithSameConstructConstraint<T>(Func<T, bool> collect) where T : class, IMyTerminalBlock
            {
                if (collect == null)
                {
                    collect = block => block.IsSameConstructAs(_program.Me);
                }
                else
                {
                    collect = block => collect(block) && block.IsSameConstructAs(_program.Me);
                }

                return collect;
            }
        }
    }
}
