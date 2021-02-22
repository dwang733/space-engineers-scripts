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
        public class ItemTypeHelper
        {
            // Maps DefinitionId.SubtypeId to MyItemType.SubtypeId
            // Note that datapad, and missiles/ammo are not in this list
            private static Dictionary<string, string> AssemblerToInventoryDict = new Dictionary<string, string>()
            {
                ["BulletproofGlass"] = "BulletproofGlass",
                ["Canvas"] = "Canvas",
                ["ComputerComponent"] = "Computer",
                ["ConstructionComponent"] = "Construction",
                ["Datapad"] = "Datapad",
                ["DetectorComponent"] = "Detector",
                ["Display"] = "Display",
                ["ExplosivesComponent"] = "Explosives",
                ["GirderComponent"] = "Girder",
                ["GravityGeneratorComponent"] = "GravityGenerator",
                ["InteriorPlate"] = "InteriorPlate",
                ["LargeTube"] = "LargeTube",
                ["MedicalComponent"] = "Medical",
                ["MetalGrid"] = "MetalGrid",
                ["MotorComponent"] = "Motor",
                ["PowerCell"] = "PowerCell",
                ["RadioCommunicationComponent"] = "RadioCommunication",
                ["ReactorComponent"] = "Reactor",
                ["SmallTube"] = "SmallTube",
                ["SolarCell"] = "SolarCell",
                ["SteelPlate"] = "SteelPlate",
                ["Superconductor"] = "Superconductor",
                ["ThrustComponent"] = "Thrust"
            };

            /// <summary>
            /// Attempts to convert from assembler's <see cref="MyDefinitionId"/> to inventory's <see cref="MyItemType"/>.
            /// </summary>
            /// <param name="definitionId">A <see cref="MyProductionItem"/> from an assembler's blueprint</param>
            /// <returns>
            /// A <see cref="MyItemType"/> for use in inventories, or null if conversion fails.
            /// </returns>
            public static MyItemType? AssemblerToInventory(MyDefinitionId definitionId)
            {
                var productionItemSubtype = definitionId.SubtypeName;
                var inventoryItemSubtype = AssemblerToInventoryDict[productionItemSubtype];
                if (inventoryItemSubtype == null)
                {
                    return null;
                }

                var typeId = productionItemSubtype.Equals("Datapad") ? "MyObjectBuilder_Datapad" : "MyObjectBuilder_Component";
                return new MyItemType(typeId, inventoryItemSubtype);
            }
        }
    }
}
