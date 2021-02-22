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
        /// Defines an airtight room with outer doors (to vacuum/space), inner doors (to pressurized area), and air vents.
        /// </summary>
        public class AirtightRoom
        {
            private readonly List<IMyDoor> _innerDoors = new List<IMyDoor>();
            private readonly List<IMyDoor> _outerDoors = new List<IMyDoor>();
            private readonly List<IMyAirVent> _airVents = new List<IMyAirVent>();

            private bool checkOxygenLevel = true;
            private bool monitorOxygenLevel = false;
            private float prevOxygenLevel = 0;
            private bool? prevPressurized = null;

            /// <summary>
            /// Checks the room to ensure that no oxygen is lost.
            /// </summary>
            public void CheckRoomSafety()
            {
                var innerDoorOpen = _innerDoors.Exists(door => door.Status != DoorStatus.Closed);
                var outerDoorOpen = _outerDoors.Exists(door => door.Status != DoorStatus.Closed);
                var pressurized = _airVents.Any(vent => !vent.Depressurize);
                var cannotPressurize = _airVents.Any(vent => !vent.CanPressurize);

                // If any doors are open while trying to change air vent depressurize status, revert back for safety.
                if (!prevPressurized.HasValue)
                {
                    prevPressurized = pressurized;
                }
                else if (prevPressurized.Value != pressurized && (innerDoorOpen || outerDoorOpen))
                {
                    _airVents.ForEach(vent => vent.Depressurize = !prevPressurized.Value);
                    return;
                }
                
                if (pressurized)
                {
                    CheckPressurizedRoom(outerDoorOpen);
                }
                else
                {
                    CheckDepressurizedRoom(innerDoorOpen, outerDoorOpen);
                }

                prevPressurized = pressurized;
            }

            public int InnerDoorsCount()
            {
                return _innerDoors.Count;
            }

            public int OuterDoorsCount()
            {
                return _outerDoors.Count;
            }

            public int AirVentsCount()
            {
                return _airVents.Count;
            }

            public bool IsInnerRoom()
            {
                return _outerDoors.Count == 0;
            }

            public bool IsValidRoom()
            {
                return _innerDoors.Count > 0 && _airVents.Count > 0;
            }

            public static void AddInnerDoor(AirtightRoom room, IMyDoor door)
            {
                room._innerDoors.Add(door);
            }

            public static void AddOuterDoor(AirtightRoom room, IMyDoor door)
            {
                room._outerDoors.Add(door);
            }

            public static void AddAirVent(AirtightRoom room, IMyAirVent vent)
            {
                room._airVents.Add(vent);
            }

            /// <summary>
            /// Checks the status of doors in a pressurized room.
            /// </summary>
            private void CheckPressurizedRoom(bool outerDoorOpen)
            {
                // Handle emergency scenario where outer doors are open
                if (outerDoorOpen)
                {
                    _outerDoors.ForEach(door =>
                    {
                        door.Enabled = true;
                        door.CloseDoor();
                    });
                    _innerDoors.ForEach(door =>
                    {
                        door.Enabled = true;
                        door.CloseDoor();
                    });
                    return;
                }

                // Make sure outer doors are disabled
                _outerDoors.ForEach(door => door.Enabled = false);

                // Make sure inner doors are enabled
                _innerDoors.ForEach(door => door.Enabled = true);

                // Make sure all vents are set to pressurized
                _airVents.ForEach(vent => vent.Depressurize = false);

                // Ensure oxygen level is checked if air vents set to depressurize
                checkOxygenLevel = true;
            }

            /// <summary>
            /// Checks the status of doors in a depressurized room.
            /// </summary>
            private void CheckDepressurizedRoom(bool innerDoorOpen, bool outerDoorOpen)
            {
                var oxygenLevel = _airVents.Sum(vent => vent.GetOxygenLevel()) / _airVents.Count;

                // Handle emergency scenario where both sets of doors are open
                if (innerDoorOpen && outerDoorOpen)
                {
                    _outerDoors.ForEach(door =>
                    {
                        door.Enabled = true;
                        door.CloseDoor();
                    });
                    _innerDoors.ForEach(door =>
                    {
                        door.Enabled = true;
                        door.CloseDoor();
                    });
                    checkOxygenLevel = true;
                    return;
                }

                // Disable outer doors if inner doors are open
                if (innerDoorOpen)
                {
                    _outerDoors.ForEach(door => door.Enabled = false);
                    checkOxygenLevel = true;
                    return;
                }

                // Disable inner doors if outer doors are open
                // Also ensure that air vents cannot be set to pressurized in this state
                if (outerDoorOpen)
                {
                    _innerDoors.ForEach(door => door.Enabled = false);
                    _airVents.ForEach(vent => vent.Depressurize = true);
                    checkOxygenLevel = true;
                    return;
                }

                // Checks if both sets of doors are closed
                if (!innerDoorOpen && !outerDoorOpen)
                {
                    // Check oxygen level in room and force to depressurize if one set of doors was just closed
                    if (checkOxygenLevel)
                    {
                        checkOxygenLevel = false;

                        if (oxygenLevel > 0)
                        {
                            _innerDoors.ForEach(door => door.Enabled = false);
                            _outerDoors.ForEach(door => door.Enabled = false);
                            monitorOxygenLevel = true;
                        }
                        else
                        {
                            _innerDoors.ForEach(door => door.Enabled = true);
                            _outerDoors.ForEach(door => door.Enabled = true);
                            monitorOxygenLevel = false;
                        }
                    }
                    // Re-enable doors once room is depressurized (or cannot depressurize further)
                    else if (monitorOxygenLevel && (oxygenLevel == 0 || Math.Abs(oxygenLevel - prevOxygenLevel) < 0.0005))
                    {
                        _innerDoors.ForEach(door => door.Enabled = true);
                        _outerDoors.ForEach(door => door.Enabled = true);
                        monitorOxygenLevel = false;
                    }
                }

                prevOxygenLevel = oxygenLevel;
            }
        }
    }
}
