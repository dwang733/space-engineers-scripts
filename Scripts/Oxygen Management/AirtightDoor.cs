﻿using Sandbox.Game.EntityComponents;
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
        /// A wrapper class around a door that tracks and handles oxygen emergencies.
        /// </summary>
        public class AirtightDoor
        {
            /// <summary>
            /// The door this class is wrapping around.
            /// </summary>
            private IMyDoor _door;

            /// <summary>
            /// Dictionary that maps room name to whether it is in an oxygen emergency.
            /// </summary>
            private Dictionary<string, bool> _emergencyStatus;

            /// <summary>
            /// True if the door has closed since the start of the emergency.
            /// </summary>
            private bool _initClosed = false;

            /// <summary>
            /// True if the door was opened after the room entered an emergency.
            /// </summary>
            private bool _opened = false;

            /// <summary>
            /// Initializes a new instance of <see cref="AirtightDoor"/>.
            /// </summary>
            /// <param name="door">The door to wrap around.</param>
            /// <param name="roomName">The name of the room that connects via this door.</param>
            /// <param name="secondRoomName">The optional name of the second room that connects via this door.</param>
            public AirtightDoor(IMyDoor door, string roomName, string secondRoomName = null)
            {
                _door = door;
                _emergencyStatus = new Dictionary<string, bool>()
                {
                    { roomName, false }
                };
                if (secondRoomName != null)
                {
                    _emergencyStatus.Add(secondRoomName, false);
                }
            }

            /// <summary>
            /// Enables this door if the rooms attached to it are not in an oxygen emergency.
            /// </summary>
            public void Enable()
            {
                if (!InEmergency())
                {
                    _door.Enabled = true;
                }
            }

            /// <summary>
            /// Disables this door if the rooms attached to it are not in an oxygen emergency.
            /// </summary>
            public void Disable()
            {
                if (!InEmergency())
                {
                    _door.Enabled = false;
                }
            }

            /// <summary>
            /// Checks if door is open at all.
            /// </summary>
            /// <returns>True if door is not fully closed, false otherwise.</returns>
            public bool IsOpen()
            {
                return _door.Status != DoorStatus.Closed;
            }

            /// <summary>
            /// Updates the state of the door to enable behavior during oxygen emergency.
            /// The door will be disabled by default, but can be enabled, opened, then closed before disabling again.
            /// </summary>
            /// <param name="roomName">The name of the room that updating the state.</param>
            /// <param name="inEmergency">True if the room is in an oxygen emergency, false otherwise.</param>
            public void UpdateEmergencyState(string roomName, bool inEmergency)
            {
                var prevInEmergency = InEmergency();
                _emergencyStatus[roomName] = inEmergency;

                if (!inEmergency)
                {
                    // If all rooms are no longer in emergency, re-enable door and reset state variables
                    if (!InEmergency())
                    {

                        _door.Enabled = true;
                        ResetStateVariables();
                    }

                    return;
                }

                // If in emergency for first time, close door
                if (!prevInEmergency)
                {
                    _door.Enabled = true;
                    _door.CloseDoor();
                    ResetStateVariables();
                    return;
                }

                // Check if the door has closed immediately after the emergency has activated
                if (!_initClosed)
                {
                    if (_door.Status == DoorStatus.Closed)
                    {
                        _door.Enabled = false;
                        _initClosed = true;
                    }

                    return;
                }

                // Check if the door has been opened
                if (!_opened)
                {
                    _opened = IsOpen();
                    return;
                }

                // Check if door has been closed afterwards
                // Cycle has completed, disable door again and reset state variables
                if (_door.Status == DoorStatus.Closed)
                {
                    _door.Enabled = false;
                    ResetStateVariables();
                }
            }

            /// <summary>
            /// Resets the state variables of the door.
            /// </summary>
            private void ResetStateVariables()
            {
                _initClosed = false;
                _opened = false;
            }

            /// <summary>
            /// Checks if the door is dealing with an oxygen emergency.
            /// </summary>
            /// <returns>True if either room that connects via this door is experiencing an oxygen emergency.</returns>
            private bool InEmergency()
            {
                return _emergencyStatus.Values.Any(e => e);
            }
        }
    }
}
