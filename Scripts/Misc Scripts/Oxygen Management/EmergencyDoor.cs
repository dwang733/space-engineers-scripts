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
        /// A wrapper class around a door to share state information between rooms in case of emergency.
        /// </summary>
        public class EmergencyDoor
        {
            /// <summary>
            /// The door this class is wrapping around.
            /// </summary>
            public IMyDoor Door { get; private set; }

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

            public EmergencyDoor(IMyDoor door, string roomName, string secondRoomName = null)
            {
                Door = door;

                _emergencyStatus = new Dictionary<string, bool>()
                {
                    { roomName, false }
                };
                if (secondRoomName != null)
                {
                    _emergencyStatus.Add(secondRoomName, false);
                }
            }

            public void Enable()
            {
                if (!InEmergency())
                {
                    Door.Enabled = true;
                }
            }

            public void Disable()
            {
                if (!InEmergency())
                {
                    Door.Enabled = false;
                }
            }

            public void UpdateEmergencyStatus(string roomName, bool inEmergency)
            {
                var prevInEmergency = InEmergency();
                _emergencyStatus[roomName] = inEmergency;

                if (!inEmergency)
                {
                    // If emergency is over, re-enable door and reset state variables
                    if (!InEmergency())
                    {

                        Door.Enabled = true;
                        ResetStateVariables();
                    }

                    return;
                }

                // If in emergency for first time, close door
                if (!prevInEmergency)
                {
                    Door.Enabled = true;
                    Door.CloseDoor();
                    ResetStateVariables();
                    return;
                }

                // Check if the door has closed immediately after the emergency has activated
                if (!_initClosed)
                {
                    if (Door.Status == DoorStatus.Closed)
                    {
                        Door.Enabled = false;
                        _initClosed = true;
                    }

                    return;
                }

                if (!_opened)
                {
                    if (Door.Status != DoorStatus.Closed)
                    {
                        _opened = true;
                    }

                    return;
                }

                // Cycle has completed, disable door again and reset state variables
                if (Door.Status == DoorStatus.Closed)
                {
                    Door.Enabled = false;
                    ResetStateVariables();
                }
            }

            private void ResetStateVariables()
            {
                _initClosed = false;
                _opened = false;
            }

            private bool InEmergency()
            {
                return _emergencyStatus.Values.Any(e => e);
            }
        }
    }
}
