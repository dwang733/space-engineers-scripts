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
        /// <summary>
        /// Represents an assembler that is used as input for a quota system.
        /// </summary>
        public class QuotaAssembler
        {
            /// <summary>
            /// The program that called this class, used to access things like Echo().
            /// </summary>
            private readonly Program _p;

            /// <summary>
            /// The assembler that this class is wrapping around.
            /// </summary>
            private readonly IMyAssembler _assembler;

            /// <summary>
            /// Boolean for whether to check the disassembly queue.
            /// This means the assembler won't make annoying sounds by constantly switching between the two queues.
            /// </summary>
            private bool _checkDisassemblyQueue;

            /// <summary>
            /// Initializes a new instance of <see cref="QuotaAssembler"/> wrapping a <see cref="IMyAssembler"/>.
            /// </summary>
            /// <param name="program">The calling program, used to access things like Echo()</param>
            /// <param name="assembler">The assembler to use for the quota system.</param>
            public QuotaAssembler(Program program, IMyAssembler assembler)
            {
                _p = program;
                if (assembler == null)
                {
                    throw new ArgumentNullException(nameof(assembler));
                }
                _assembler = assembler;
                _checkDisassemblyQueue = false;
            }

            /// <summary>
            /// Sorts the assembly queue and removes from it based on the disassembly queue.
            /// </summary>
            /// <param name="forceCheckDisassembly">Whether to forcibly check the disassembly queue, false by default</param>
            /// <returns>A sorted dictionary of the quota represented by the assembler, or null if queue can't be read.</returns>
            public SortedDictionary<MyDefinitionId, int> Update(bool forceCheckDisassembly = false)
            {
                if (_assembler.Mode == MyAssemblerMode.Disassembly)
                {
                    _p.Echo($"Assembler is in disassembly mode. Will not read until set back to assembly mode.");
                    _checkDisassemblyQueue = true;
                    return null;
                }

                var quotaDict = new SortedDictionary<MyDefinitionId, int>(
                    Comparer<MyDefinitionId>.Create((i1, i2) => i1.SubtypeName.CompareTo(i2.SubtypeName)));

                // Fetch assembly queue.
                var queue = new List<MyProductionItem>();
                _assembler.GetQueue(queue);
                if (queue.Count == 0)
                {
                    return quotaDict;
                }

                // Put assembly queue into dictionary and combine item amounts if there are multiple stacks.
                string prevKey = null;
                var updateQueue = false;
                foreach (var assemblyItem in queue)
                {
                    var key = assemblyItem.BlueprintId;
                    var prevAmount = quotaDict.ContainsKey(key) ? quotaDict[key] : 0;
                    quotaDict[key] = assemblyItem.Amount.ToIntSafe() + prevAmount;

                    // Update queue if it is not in alphabetical order.
                    updateQueue = updateQueue || String.Compare(prevKey, key.SubtypeName) >= 0;
                    prevKey = key.SubtypeName;
                }

                // Check disassembly queue for items to remove from production queue.
                if (_checkDisassemblyQueue || forceCheckDisassembly)
                {
                    _assembler.Mode = MyAssemblerMode.Disassembly;
                    _assembler.GetQueue(queue);
                    if (queue.Count > 0)
                    {
                        updateQueue = true;
                    }

                    foreach (var disassemblyItem in queue)
                    {
                        var key = disassemblyItem.BlueprintId;
                        if (quotaDict.ContainsKey(key))
                        {
                            var newAmount = quotaDict[key] - disassemblyItem.Amount.ToIntSafe();
                            // Remove item if amount is zero/negative for simplicity reasons.
                            if (newAmount > 0)
                            {
                                quotaDict[key] = newAmount;
                            }
                            else
                            {
                                quotaDict.Remove(key);
                            }
                        }
                    }

                    // Clear disassembly queue and set back to assembly mode.
                    _assembler.ClearQueue();
                    _assembler.Mode = MyAssemblerMode.Assembly;
                    _checkDisassemblyQueue = false;
                }

                // Overwrite current production queue with updated amounts.
                if (updateQueue)
                {
                    _assembler.ClearQueue();
                    foreach (var quotaPair in quotaDict)
                    {
                        _assembler.AddQueueItem(quotaPair.Key, (double)quotaPair.Value);
                    }
                }

                return quotaDict;
            }
        }
    }
}
