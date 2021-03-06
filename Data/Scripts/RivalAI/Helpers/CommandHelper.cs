using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Weapons;
using SpaceEngineers.Game.ModAPI;
using ProtoBuf;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Utils;
using VRageMath;
using RivalAI;
using RivalAI.Behavior;
using RivalAI.Behavior.Subsystems;
using RivalAI.Helpers;

namespace RivalAI.Helpers {

    public enum CommandType {
    
        DroneAntenna,
        PlayerChat,
        
    }

    public class Command {

        public string CommandCode;

        public CommandType Type;

        public IMyEntity RemoteControl;

        public IMyEntity Character;

        public IMyEntity SenderEntity { get { return (RemoteControl != null ? RemoteControl : Character); } }

        public double Radius;

        public bool IgnoreAntennaRequirement;

        public long TargetEntityId;

        public IMyEntity TargetEntity;

        public float DamageAmount;

        public Vector3D Position;

        public long PlayerIdentity;

        public bool UseTriggerTargetDistance;


        public Command() {

            CommandCode = "";
            Type = CommandType.DroneAntenna;
            RemoteControl = null;
            Character = null;
            Radius = 0;
            IgnoreAntennaRequirement = false;
            TargetEntityId = 0;
            TargetEntity = null;
            Position = Vector3D.Zero;
            PlayerIdentity = 0;
            UseTriggerTargetDistance = false;

        }
        
    }

    public static class CommandHelper {

        public static Action<Command> CommandTrigger;
    
    }
    
}
