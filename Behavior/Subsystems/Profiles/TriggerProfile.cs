﻿using System;
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
using RivalAI.Behavior.Settings;
using RivalAI.Helpers;

namespace RivalAI.Behavior.Subsystems.Profiles {

	[ProtoContract]
	public class TriggerProfile {

		[ProtoMember(1)]
		public string Type;

		[ProtoMember(2)]
		public bool UseTrigger;

		[ProtoMember(3)]
		public double TargetDistance;

		[ProtoMember(4)]
		public bool InsideAntenna;

		[ProtoMember(5)]
		public float MinCooldownMs;

		[ProtoMember(6)]
		public float MaxCooldownMs;

		[ProtoMember(7)]
		public bool StartsReady;

		[ProtoMember(8)]
		public int MaxActions;

		[ProtoMember(9)]
		public ActionProfile Actions;

		[ProtoMember(10)]
		public List<string> DamageTypes;

		[ProtoMember(11)]
		public bool Triggered;

		[ProtoMember(12)]
		public int CooldownTime;

		[ProtoMember(13)]
		public int TriggerCount;

		[ProtoMember(14)]
		public DateTime LastTriggerTime;

		[ProtoMember(15)]
		public int MinPlayerReputation;

		[ProtoMember(16)]
		public int MaxPlayerReputation;

		[ProtoMember(17)]
		public ConditionProfile Conditions;

		[ProtoMember(18)]
		public bool ConditionCheckResetsTimer;

		[ProtoMember(19)]
		public long DetectedEntityId;

		[ProtoMember(20)]
		public string CommandReceiveCode;

		[ProtoMember(21)]
		public string ProfileSubtypeId;

		[ProtoMember(22)]
		public Vector3D PlayerNearPositionOffset;

		[ProtoMember(23)]
		public bool AllPlayersMustMatchReputation;

		[ProtoMember(24)]
		public double CustomReputationRangeCheck;

		[ProtoMember(25)]
		public string InventoryBlockName;

		[ProtoMember(26)]
		public string InventoryItemDefinitionId;

		[ProtoMember(27)]
		public float InventoryItemMin;

		[ProtoMember(28)]
		public float InventoryItemMax;

		[ProtoMember(29)]
		public string InsideAntennaName;

		[ProtoMember(30)]
		public List<string> ExcludedDamageTypes;

		[ProtoIgnore]
		public Random Rnd;

		public TriggerProfile() {

			Type = "";

			UseTrigger = false;
			TargetDistance = 3000;
			InsideAntenna = false;
			InsideAntennaName = "";
			PlayerNearPositionOffset = Vector3D.Zero;
			MinCooldownMs = 0;
			MaxCooldownMs = 1;
			StartsReady = false;
			MaxActions = -1;
			Actions = new ActionProfile();
			DamageTypes = new List<string>();
			Conditions = new ConditionProfile();

			Triggered = false;
			CooldownTime = 0;
			TriggerCount = 0;
			LastTriggerTime = MyAPIGateway.Session.GameDateTime;
			DetectedEntityId = 0;

			Conditions = new ConditionProfile();
			ConditionCheckResetsTimer = false;

			MinPlayerReputation = -1501;
			MaxPlayerReputation = 1501;
			AllPlayersMustMatchReputation = false;
			CustomReputationRangeCheck = 5000;

			InventoryBlockName = "";
			InventoryItemDefinitionId = "";
			InventoryItemMin = -1;
			InventoryItemMax = -1;

			CommandReceiveCode = "";

			ProfileSubtypeId = "";


			Rnd = new Random();

		}

		public void ActivateTrigger(Func<bool> mainTriggerCheck = null) {

			if(MaxActions >= 0 && TriggerCount >= MaxActions) {

				Logger.MsgDebug(this.ProfileSubtypeId + ": Max Successful Actions Reached. Trigger Disabled", DebugTypeEnum.Trigger);
				UseTrigger = false;
				return;

			}

			if(CooldownTime > 0) {

				TimeSpan duration = MyAPIGateway.Session.GameDateTime - this.LastTriggerTime;

				if(duration.TotalMilliseconds >= CooldownTime) {

					if (mainTriggerCheck != null) {
					
						
					
					}

					if (Conditions.UseConditions == true) {

						if (Conditions.AreConditionsMets()) {

							Logger.MsgDebug(this.ProfileSubtypeId + ": Trigger Cooldown & Conditions Satisfied", DebugTypeEnum.Trigger);
							Triggered = true;

						} else if(this.ConditionCheckResetsTimer) {

							this.LastTriggerTime = MyAPIGateway.Session.GameDateTime;
							CooldownTime = Rnd.Next((int)MinCooldownMs, (int)MaxCooldownMs);

						}

					} else {

						Logger.MsgDebug(this.ProfileSubtypeId + ": Trigger Cooldown Satisfied", DebugTypeEnum.Trigger);
						Triggered = true;

					}

				}

			} else {

				if (mainTriggerCheck != null) {



				}

				if (Conditions.UseConditions == true) {

					if (Conditions.AreConditionsMets()) {

						Logger.MsgDebug(this.ProfileSubtypeId + ": Trigger Cooldown & Conditions Satisfied", DebugTypeEnum.Trigger);
						Triggered = true;

					}

				} else {

					Logger.MsgDebug(this.ProfileSubtypeId + ": No Trigger Cooldown Needed", DebugTypeEnum.Trigger);
					Triggered = true;

				}
				
			}

		}
		
		public void ResetTime(){
		
			this.LastTriggerTime = MyAPIGateway.Session.GameDateTime;

			if (this.Actions?.Spawner != null) {

				this.Actions.Spawner.LastSpawnTime = MyAPIGateway.Session.GameDateTime;

			}
				

			if (this.Actions?.ChatData != null) {

				this.Actions.ChatData.LastChatTime = MyAPIGateway.Session.GameDateTime;

			}

		}

		public void InitTags(string customData) {

			if(string.IsNullOrWhiteSpace(customData) == false) {

				var descSplit = customData.Split('\n');

				foreach(var tag in descSplit) {

					//Type
					if(tag.Contains("[Type:") == true) {

						Type = TagHelper.TagStringCheck(tag);

					}

					//UseTrigger
					if(tag.Contains("[UseTrigger:") == true) {

						UseTrigger = TagHelper.TagBoolCheck(tag);

					}

					//InsideAntenna
					if(tag.Contains("[InsideAntenna:") == true) {

						InsideAntenna = TagHelper.TagBoolCheck(tag);

					}

					//InsideAntennaName
					if (tag.Contains("[InsideAntennaName:") == true) {

						InsideAntennaName = TagHelper.TagStringCheck(tag);

					}

					//TargetDistance
					if (tag.Contains("[TargetDistance:") == true) {

						TargetDistance = TagHelper.TagDoubleCheck(tag, TargetDistance);

					}

					//MinCooldown
					if(tag.Contains("[MinCooldownMs:") == true) {

						MinCooldownMs = TagHelper.TagFloatCheck(tag, MinCooldownMs);

					}

					//MaxCooldown
					if(tag.Contains("[MaxCooldownMs:") == true) {

						MaxCooldownMs = TagHelper.TagFloatCheck(tag, MaxCooldownMs);

					}

					//StartsReady
					if(tag.Contains("[StartsReady:") == true) {

						StartsReady = TagHelper.TagBoolCheck(tag);

					}

					//MaxActions
					if (tag.Contains("[MaxActions:") == true) {

						MaxActions = TagHelper.TagIntCheck(tag, MaxActions);

					}

					//Actions
					if (tag.Contains("[Actions:") == true) {

						var tempValue = TagHelper.TagStringCheck(tag);
						bool gotAction = false;

						if (string.IsNullOrWhiteSpace(tempValue) == false) {

							byte[] byteData = { };

							if(TagHelper.ActionObjectTemplates.TryGetValue(tempValue, out byteData) == true) {

								try {

									var profile = MyAPIGateway.Utilities.SerializeFromBinary<ActionProfile>(byteData);

									if(profile != null) {

										Actions = profile;
										gotAction = true;

									}

								} catch(Exception) {



								}

							}

						}

						if (!gotAction)
							Logger.WriteLog("Could Not Find Action Profile Associated To Tag: " + tag);


					}

					//DamageTypes
					if (tag.Contains("[DamageTypes:") == true) {

						var tempValue = TagHelper.TagStringCheck(tag);

						if(!string.IsNullOrWhiteSpace(tempValue) && DamageTypes.Contains(tempValue) == false) {

							DamageTypes.Add(tempValue);

						}

					}

					//MinPlayerReputation
					if (tag.Contains("[MinPlayerReputation:") == true) {

						MinPlayerReputation = TagHelper.TagIntCheck(tag, MinPlayerReputation);

					}

					//MaxPlayerReputation
					if (tag.Contains("[MaxPlayerReputation:") == true) {

						MaxPlayerReputation = TagHelper.TagIntCheck(tag, MaxPlayerReputation);

					}

					//AllPlayersMustMatchReputation
					if (tag.Contains("[AllPlayersMustMatchReputation:") == true) {

						AllPlayersMustMatchReputation = TagHelper.TagBoolCheck(tag);

					}

					//CustomReputationRangeCheck
					if (tag.Contains("[CustomReputationRangeCheck:") == true) {

						CustomReputationRangeCheck = TagHelper.TagDoubleCheck(tag, CustomReputationRangeCheck);

					}

					//Conditions
					if (tag.Contains("[Conditions:") == true) {

						var tempValue = TagHelper.TagStringCheck(tag);
						bool gotCondition = false;

						if (string.IsNullOrWhiteSpace(tempValue) == false) {

							byte[] byteData = { };

							if(TagHelper.ConditionObjectTemplates.TryGetValue(tempValue, out byteData) == true) {

								try {

									var profile = MyAPIGateway.Utilities.SerializeFromBinary<ConditionProfile>(byteData);

									if(profile != null) {

										this.Conditions = profile;
										gotCondition = true;

									}

								} catch(Exception) {



								}

							}

						}

						if (!gotCondition)
							Logger.WriteLog("Could Not Find Condition Profile Associated To Tag: " + tag);


					}

					//ConditionCheckResetsTimer
					if (tag.Contains("[ConditionCheckResetsTimer:") == true) {

						ConditionCheckResetsTimer = TagHelper.TagBoolCheck(tag);

					}

					//PlayerNearPositionOffset
					if (tag.Contains("[PlayerNearPositionOffset:") == true) {

						PlayerNearPositionOffset = TagHelper.TagVector3DCheck(tag);

					}

				}

			}

			if(MinCooldownMs > MaxCooldownMs) {

				MinCooldownMs = MaxCooldownMs;

			}

			if(StartsReady == true) {

				CooldownTime = 0;

			} else {

				CooldownTime = Rnd.Next((int)MinCooldownMs, (int)MaxCooldownMs);

			}


		}

	}
}
