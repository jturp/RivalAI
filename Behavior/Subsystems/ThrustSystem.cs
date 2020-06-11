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
using RivalAI.Behavior.Subsystems;
using RivalAI.Helpers;
using RivalAI.Behavior.Subsystems.Profiles;

namespace RivalAI.Behavior.Subsystems {

	public enum ThrustMode {

		None,
		Strafe,
		ConstantForward

	}

	public partial class AutoPilotSystem {

		public double AngleAllowedForForwardThrust;
		public double MaxVelocityAngleForSpeedControl;
		public bool AllowStrafing;
		public int StrafeMinDurationMs;
		public int StrafeMaxDurationMs;
		public int StrafeMinCooldownMs;
		public int StrafeMaxCooldownMs;
		public double StrafeSpeedCutOff;
		public double StrafeDistanceCutOff;

		public double StrafeMinimumTargetDistance;
		public double StrafeMinimumSafeAngleFromTarget;

		public Vector3I AllowedStrafingDirectionsSpace;
		public Vector3I AllowedStrafingDirectionsPlanet;

		public ThrustMode Mode;
		public List<ThrustProfile> ThrustProfiles;
		public Random Rnd;

		private bool _orientationCalculated;
		private MyBlockOrientation _referenceOrientation;

		public Vector3I PreviousAllowedThrust;
		public Vector3I PreviousRequiredThrust;
		public Vector3I CurrentAllowedThrust;
		public Vector3I CurrentRequiredThrust;

		public bool Strafing;
		public Vector3I CurrentStrafeDirections;
		public Vector3I CurrentAllowedStrafeDirections;
		public bool InvertStrafingActivated;
		public int ThisStrafeDuration;
		public int ThisStrafeCooldown;
		public DateTime LastStrafeStartTime;
		public DateTime LastStrafeEndTime;

		private bool _collisionStrafeAdjusted;
		private bool _minAngleDistanceStrafeAdjusted;
		private Vector3D _collisionStrafeDirection;
		
		public void ChangeMode(ThrustMode newMode) {

			if(newMode == this.Mode) {

				return;

			}

			this.Mode = newMode;
			this.Strafing = false;
			this.SetThrust(new Vector3I(0, 0, 0), new Vector3I(0, 0, 0));

		}

		public void ProcessThrustStrafing() {

			if (!this.AllowStrafing)
				return;

			if (this.Strafing == false) {

				TimeSpan duration = MyAPIGateway.Session.GameDateTime - this.LastStrafeEndTime;
				if (duration.TotalMilliseconds >= this.ThisStrafeCooldown) {

					//Logger.MsgDebug("Begin Strafe", DebugTypeEnum.AutoPilot);
					this.LastStrafeStartTime = MyAPIGateway.Session.GameDateTime;
					this.ThisStrafeDuration = Rnd.Next(StrafeMinDurationMs, StrafeMaxDurationMs);
					this.Strafing = true;

					MyAPIGateway.Parallel.Start(() => {

						Collision.RunSecondaryCollisionChecks();
						this.CurrentAllowedStrafeDirections = Vector3I.Zero;
						this.CurrentStrafeDirections = new Vector3I(Rnd.Next(-1, 2), Rnd.Next(-1, 2), Rnd.Next(-1, 2));

						if (this.CurrentStrafeDirections.X != 0) {

							this.CurrentAllowedStrafeDirections.X = 1;
							var rAngle = VectorHelper.GetAngleBetweenDirections(Collision.Matrix.Right, Vector3D.Normalize(GetCurrentWaypoint() - Collision.Matrix.Translation));
							var lAngle = VectorHelper.GetAngleBetweenDirections(Collision.Matrix.Left, Vector3D.Normalize(GetCurrentWaypoint() - Collision.Matrix.Translation));
							bool rTargetIntersect = (rAngle < this.StrafeMinimumSafeAngleFromTarget && DistanceToCurrentWaypoint < this.StrafeMinimumTargetDistance);
							bool lTargetIntersect = (lAngle < this.StrafeMinimumSafeAngleFromTarget && DistanceToCurrentWaypoint < this.StrafeMinimumTargetDistance);

							if (this.CurrentStrafeDirections.X == 1) {

								if (rTargetIntersect || (Collision.RightResult.HasTarget(StrafeMinimumTargetDistance) && !Collision.LeftResult.HasTarget(StrafeMinimumTargetDistance))) {

									Logger.MsgDebug("Strafe: X Reverse", DebugTypeEnum.AutoPilot);
									this.CurrentStrafeDirections.X *= -1;

								} else if (lTargetIntersect || (Collision.RightResult.HasTarget(StrafeMinimumTargetDistance) && Collision.LeftResult.HasTarget(StrafeMinimumTargetDistance))) {

									Logger.MsgDebug("Strafe: X Negate", DebugTypeEnum.AutoPilot);
									this.CurrentStrafeDirections.X = 0;

								}

							} else {

								if (lTargetIntersect || (Collision.LeftResult.HasTarget(StrafeMinimumTargetDistance) && !Collision.RightResult.HasTarget(StrafeMinimumTargetDistance))) {

									Logger.MsgDebug("Strafe: X Reverse", DebugTypeEnum.AutoPilot);
									this.CurrentStrafeDirections.X *= -1;

								} else if (rTargetIntersect || (Collision.LeftResult.HasTarget(StrafeMinimumTargetDistance) && Collision.RightResult.HasTarget(StrafeMinimumTargetDistance))) {

									Logger.MsgDebug("Strafe: X Negate", DebugTypeEnum.AutoPilot);
									this.CurrentStrafeDirections.X = 0;

								}

							}

						}

						if (this.CurrentStrafeDirections.Y != 0) {

							this.CurrentAllowedStrafeDirections.Y = 1;
							var uAngle = VectorHelper.GetAngleBetweenDirections(Collision.Matrix.Up, Vector3D.Normalize(GetCurrentWaypoint() - Collision.Matrix.Translation));
							var dAngle = VectorHelper.GetAngleBetweenDirections(Collision.Matrix.Down, Vector3D.Normalize(GetCurrentWaypoint() - Collision.Matrix.Translation));
							bool uTargetIntersect = (uAngle < this.StrafeMinimumSafeAngleFromTarget && DistanceToCurrentWaypoint < this.StrafeMinimumTargetDistance);
							bool dTargetIntersect = (dAngle < this.StrafeMinimumSafeAngleFromTarget && DistanceToCurrentWaypoint < this.StrafeMinimumTargetDistance);

							if (this.CurrentStrafeDirections.Y == 1) {

								if (uTargetIntersect || (Collision.UpResult.HasTarget(StrafeMinimumTargetDistance) && !Collision.DownResult.HasTarget(StrafeMinimumTargetDistance))) {

									Logger.MsgDebug("Strafe: Y Reverse", DebugTypeEnum.AutoPilot);
									this.CurrentStrafeDirections.Y *= -1;

								} else if (dTargetIntersect || (Collision.UpResult.HasTarget(StrafeMinimumTargetDistance) && Collision.DownResult.HasTarget(StrafeMinimumTargetDistance))) {

									Logger.MsgDebug("Strafe: Y Negate", DebugTypeEnum.AutoPilot);
									this.CurrentStrafeDirections.Y = 0;

								}

							} else {

								if (dTargetIntersect || (Collision.DownResult.HasTarget(StrafeMinimumTargetDistance) && !Collision.UpResult.HasTarget(StrafeMinimumTargetDistance))) {

									Logger.MsgDebug("Strafe: Y Reverse", DebugTypeEnum.AutoPilot);
									this.CurrentStrafeDirections.Y *= -1;

								} else if (uTargetIntersect || (Collision.DownResult.HasTarget(StrafeMinimumTargetDistance) && Collision.UpResult.HasTarget(StrafeMinimumTargetDistance))) {

									Logger.MsgDebug("Strafe: Y Negate", DebugTypeEnum.AutoPilot);
									this.CurrentStrafeDirections.Y = 0;

								}

							}

						}

						if (this.CurrentStrafeDirections.Z != 0) {

							this.CurrentAllowedStrafeDirections.Z = 1;
							var fAngle = VectorHelper.GetAngleBetweenDirections(Collision.Matrix.Forward, Vector3D.Normalize(GetCurrentWaypoint() - Collision.Matrix.Translation));
							var bAngle = VectorHelper.GetAngleBetweenDirections(Collision.Matrix.Backward, Vector3D.Normalize(GetCurrentWaypoint() - Collision.Matrix.Translation));
							bool fTargetIntersect = (fAngle < this.StrafeMinimumSafeAngleFromTarget && DistanceToCurrentWaypoint < this.StrafeMinimumTargetDistance);
							bool bTargetIntersect = (bAngle < this.StrafeMinimumSafeAngleFromTarget && DistanceToCurrentWaypoint < this.StrafeMinimumTargetDistance);

							if (this.CurrentStrafeDirections.Z == 1) {

								if (fTargetIntersect || (Collision.ForwardResult.HasTarget(StrafeMinimumTargetDistance) && !Collision.BackwardResult.HasTarget(StrafeMinimumTargetDistance))) {

									Logger.MsgDebug("Strafe: Z Reverse", DebugTypeEnum.AutoPilot);
									this.CurrentStrafeDirections.Z *= -1;

								} else if (bTargetIntersect || (Collision.ForwardResult.HasTarget(StrafeMinimumTargetDistance) && Collision.BackwardResult.HasTarget(StrafeMinimumTargetDistance))) {

									Logger.MsgDebug("Strafe: Z Negate", DebugTypeEnum.AutoPilot);
									this.CurrentStrafeDirections.Z = 0;

								}

							} else {

								if (bTargetIntersect || (Collision.BackwardResult.HasTarget(StrafeMinimumTargetDistance) && !Collision.ForwardResult.HasTarget(StrafeMinimumTargetDistance))) {

									Logger.MsgDebug("Strafe: Z Reverse", DebugTypeEnum.AutoPilot);
									this.CurrentStrafeDirections.Z *= -1;

								} else if (fTargetIntersect || (Collision.BackwardResult.HasTarget(StrafeMinimumTargetDistance) && Collision.ForwardResult.HasTarget(StrafeMinimumTargetDistance))) {

									Logger.MsgDebug("Strafe: Z Negate", DebugTypeEnum.AutoPilot);
									this.CurrentStrafeDirections.Z = 0;

								}

							}

						}

						if (UpDirectionFromPlanet != Vector3D.Zero && MyAltitude < MinimumPlanetAltitude) {

							var thrustDir = VectorHelper.GetThrustDirectionsAwayFromDirection(Collision.Matrix, -UpDirectionFromPlanet);

							if (thrustDir.X != 0) {

								this.CurrentAllowedStrafeDirections.X = 1;
								this.CurrentStrafeDirections.X = thrustDir.X;

							}

							if (thrustDir.Y != 0) {

								this.CurrentAllowedStrafeDirections.Y = 1;
								this.CurrentStrafeDirections.Y = thrustDir.Y;

							}

							if (thrustDir.Z != 0) {

								this.CurrentAllowedStrafeDirections.Z = 1;
								this.CurrentStrafeDirections.Z = thrustDir.Z;

							}

						}


					}, () => {

						Logger.MsgDebug(string.Format("Allowed Strafe Axis: {0}", this.CurrentAllowedStrafeDirections), DebugTypeEnum.Strafe);
						Logger.MsgDebug(string.Format("Applied Strafe Axis: {0}", this.CurrentStrafeDirections), DebugTypeEnum.Strafe);
						SetThrust(this.CurrentAllowedStrafeDirections, this.CurrentStrafeDirections);

					});

				}

			} else {

				TimeSpan duration = MyAPIGateway.Session.GameDateTime - this.LastStrafeStartTime;

				if (duration.TotalMilliseconds >= this.ThisStrafeDuration) {

					//Logger.MsgDebug("End Strafe", DebugTypeEnum.General);
					this.InvertStrafingActivated = false;
					this.LastStrafeEndTime = MyAPIGateway.Session.GameDateTime;
					this.ThisStrafeCooldown = Rnd.Next(StrafeMinCooldownMs, StrafeMaxCooldownMs);
					this.Strafing = false;
					_collisionStrafeAdjusted = false;
					_minAngleDistanceStrafeAdjusted = false;
					_collisionStrafeDirection = Vector3D.Zero;
					SetThrust(new Vector3I(0, 0, 0), new Vector3I(0, 0, 0));
					//Logger.AddMsg("Cooldown: " + this.ThisStrafeCooldown.ToString(), true);

				} else {

					//Logger.MsgDebug("Strafe Collision: " + Collision.VelocityResult.CollisionImminent.ToString() + " - " + Collision.VelocityResult.Time.ToString(), DebugTypeEnum.Collision);

					if (!_collisionStrafeAdjusted && Collision.VelocityResult.CollisionImminent()) {

						Logger.MsgDebug("Strafe Velocity Collision Detect: " + Collision.VelocityResult.Type.ToString() + ", " + Collision.VelocityResult.GetCollisionDistance(), DebugTypeEnum.Collision);
						_collisionStrafeAdjusted = true;
						StopStrafeDirectionNearestPosition(Collision.VelocityResult.GetCollisionCoords());
						_collisionStrafeDirection = Vector3D.Normalize(Collision.VelocityResult.GetCollisionCoords() - _remoteControl.WorldMatrix.Translation);

					} else if(_collisionStrafeAdjusted && VectorHelper.GetAngleBetweenDirections(_collisionStrafeDirection, Vector3D.Normalize(Collision.Velocity - _remoteControl.WorldMatrix.Translation)) > 15) {

						Logger.MsgDebug("Strafe Collision Detect", DebugTypeEnum.General);
						StopStrafeDirectionNearestPosition(Collision.VelocityResult.GetCollisionCoords());
						_collisionStrafeDirection = Vector3D.Normalize(Collision.VelocityResult.GetCollisionCoords() - _remoteControl.WorldMatrix.Translation);

					}

					if (_minAngleDistanceStrafeAdjusted && AngleToCurrentWaypoint < this.StrafeMinimumSafeAngleFromTarget && DistanceToCurrentWaypoint < this.StrafeMinimumTargetDistance) {

						Logger.MsgDebug("Strafe Min Dist/Angle Detect", DebugTypeEnum.General);
						_minAngleDistanceStrafeAdjusted = false;
						StopStrafeDirectionNearestPosition(Collision.VelocityResult.GetCollisionCoords());
						_collisionStrafeDirection = Vector3D.Normalize(Collision.VelocityResult.GetCollisionCoords() - _remoteControl.WorldMatrix.Translation);

					}
				
				}

			}

		}

		public void StopStrafeDirectionNearestPosition(Vector3D coords) {

			double minAngle = 50; //Move this to fields later
			var targetDir = Vector3D.Normalize(coords - _remoteControl.WorldMatrix.Translation);
			var leftAngle = VectorHelper.GetAngleBetweenDirections(_remoteControl.WorldMatrix.Left, targetDir);
			var rightAngle = VectorHelper.GetAngleBetweenDirections(_remoteControl.WorldMatrix.Right, targetDir);
			var upAngle = VectorHelper.GetAngleBetweenDirections(_remoteControl.WorldMatrix.Up, targetDir);
			var downAngle = VectorHelper.GetAngleBetweenDirections(_remoteControl.WorldMatrix.Down, targetDir);
			var forwardAngle = VectorHelper.GetAngleBetweenDirections(_remoteControl.WorldMatrix.Forward, targetDir);
			var backAngle = VectorHelper.GetAngleBetweenDirections(_remoteControl.WorldMatrix.Backward, targetDir);

			if (this.CurrentStrafeDirections.X == 1 && rightAngle < minAngle) {

				//Logger.MsgDebug("Strafe Stop X Movement", DebugTypeEnum.Collision);
				this.CurrentAllowedStrafeDirections.X = 0;
				this.CurrentStrafeDirections.X = 0;
				SetThrust(this.CurrentAllowedStrafeDirections, this.CurrentStrafeDirections);

			}

			if (this.CurrentStrafeDirections.X == -1 && leftAngle < minAngle) {

				//Logger.MsgDebug("Strafe Stop X Movement", DebugTypeEnum.Collision);
				this.CurrentAllowedStrafeDirections.X = 0;
				this.CurrentStrafeDirections.X = 0;
				SetThrust(this.CurrentAllowedStrafeDirections, this.CurrentStrafeDirections);

			}

			if (this.CurrentStrafeDirections.Y == 1 && upAngle < minAngle) {

				//Logger.MsgDebug("Strafe Stop Y Movement", DebugTypeEnum.Collision);
				this.CurrentAllowedStrafeDirections.Y = 0;
				this.CurrentStrafeDirections.Y = 0;
				SetThrust(this.CurrentAllowedStrafeDirections, this.CurrentStrafeDirections);

			}

			if (this.CurrentStrafeDirections.Y == -1 && downAngle < minAngle) {

				//Logger.MsgDebug("Strafe Stop Y Movement", DebugTypeEnum.Collision);
				this.CurrentAllowedStrafeDirections.Y = 0;
				this.CurrentStrafeDirections.Y = 0;
				SetThrust(this.CurrentAllowedStrafeDirections, this.CurrentStrafeDirections);

			}

			if (this.CurrentStrafeDirections.Z == 1 && forwardAngle < minAngle) {

				//Logger.MsgDebug("Strafe Stop Z Movement", DebugTypeEnum.Collision);
				this.CurrentAllowedStrafeDirections.Z = 0;
				this.CurrentStrafeDirections.Z = 0;
				SetThrust(this.CurrentAllowedStrafeDirections, this.CurrentStrafeDirections);

			}

			if (this.CurrentStrafeDirections.Z == -1 && backAngle < minAngle) {

				//Logger.MsgDebug("Strafe Stop Z Movement", DebugTypeEnum.Collision);
				this.CurrentAllowedStrafeDirections.Z = 0;
				this.CurrentStrafeDirections.Z = 0;
				SetThrust(this.CurrentAllowedStrafeDirections, this.CurrentStrafeDirections);

			}

		}

		public void ProcessForwardThrust(double currentAngle = 180) {

			if (this.Strafing || _remoteControl?.SlimBlock?.CubeGrid?.Physics == null)
				return;

			if (currentAngle > this.AngleAllowedForForwardThrust) {

				//Logger.MsgDebug(string.Format("thrust target angle not matched: {0} / {1}", currentAngle, this.AngleAllowedForForwardThrust), DebugTypeEnum.Thrust);
				SetThrust(new Vector3I(0, 0, 0), new Vector3I(0, 0, 0));
				return;

			}

			var velocityToTargetAngle = VectorHelper.GetAngleBetweenDirections(Vector3D.Normalize(GetCurrentWaypoint() - _remoteControl.WorldMatrix.Translation), Vector3D.Normalize(_remoteControl.SlimBlock.CubeGrid.Physics.LinearVelocity));
			var velocityAmount = _remoteControl.SlimBlock.CubeGrid.Physics.LinearVelocity.Length();

			//Logger.MsgDebug(string.Format("Velocity Angle and Speed: {0} / {1}", velocityToTargetAngle, velocityAmount), DebugTypeEnum.Thrust);

			if (velocityToTargetAngle > this.MaxVelocityAngleForSpeedControl) {

				SetThrust(new Vector3I(0, 0, 1), new Vector3I(0, 0, 1));
				return;

			}
			
			//Logger.MsgDebug(string.Format("Forward Thrust Check: {0} / {1}", velocityAmount, IdealMaxSpeed - MaxSpeedTolerance), DebugTypeEnum.Thrust);
			if (velocityAmount < IdealMaxSpeed - MaxSpeedTolerance) {

				SetThrust(new Vector3I(0, 0, 1), new Vector3I(0, 0, 1));
				return;

			}

			//Logger.MsgDebug(string.Format("Reverse Thrust Check: {0} / {1}", velocityAmount, IdealMaxSpeed + MaxSpeedTolerance), DebugTypeEnum.Thrust);
			if (velocityAmount > IdealMaxSpeed + MaxSpeedTolerance) {

				Logger.MsgDebug("thrust reverse", DebugTypeEnum.Thrust);
				SetThrust(new Vector3I(0, 0, 1), new Vector3I(0, 0, -1));
				return;

			}

			//Logger.MsgDebug("thrust drift", DebugTypeEnum.Thrust);
			SetThrust(new Vector3I(0, 0, 1), new Vector3I(0, 0, 0));

		}

		public void SafeStrafeDirections(Vector3D upDirection, Vector3D destination, double minAltitude, double minTargetDist){

			if(upDirection != new Vector3D()) {

				double elevation = 0;

				if(this._remoteControl.TryGetPlanetElevation(Sandbox.ModAPI.Ingame.MyPlanetElevation.Surface, out elevation) == true) {

					if(elevation < minAltitude) {

						var newThrust = VectorHelper.GetThrustDirectionsAwayFromSurface(this._remoteControl.WorldMatrix, upDirection, this.CurrentStrafeDirections);

						if(newThrust != this.CurrentStrafeDirections) {

							this.CurrentStrafeDirections = newThrust;
							SetThrust(this.CurrentAllowedStrafeDirections, this.CurrentStrafeDirections);

						}

					}

				}

			}

			if(Vector3D.Distance(this._remoteControl.GetPosition(), destination) < minTargetDist) {

				var dirToCollision = Vector3D.Normalize(destination - this._remoteControl.GetPosition());
				var newThrust = VectorHelper.GetThrustDirectionsAwayFromSurface(this._remoteControl.WorldMatrix, -dirToCollision, this.CurrentStrafeDirections);

				if(newThrust != this.CurrentStrafeDirections) {

					this.CurrentStrafeDirections = newThrust;
					SetThrust(this.CurrentAllowedStrafeDirections, this.CurrentStrafeDirections);

				}

			}

		}

		public void InvertStrafe(Vector3D collisionCoords) {

			if(this.Strafing == false) {

				return;

			}

			SetThrust(new Vector3I(0, 0, 0), new Vector3I(0, 0, 0));

			/*
			var dirToCollision = Vector3D.Normalize(collisionCoords - this._remoteControl.GetPosition());
			var newThrust = VectorHelper.GetThrustDirectionsAwayFromSurface(this._remoteControl.WorldMatrix, -dirToCollision, this.CurrentStrafeDirections);

			if(newThrust != this.CurrentStrafeDirections) {

				this.CurrentStrafeDirections = newThrust;
				SetThrust(this.CurrentAllowedStrafeDirections, this.CurrentStrafeDirections);

			}
			*/

		}

		public void SetThrust(Vector3I allowedThrust, Vector3I requiredThrust) {

			if(this.PreviousAllowedThrust == allowedThrust && this.PreviousRequiredThrust == requiredThrust)
				return;

			foreach(var thrustProfile in this.ThrustProfiles.ToList()) {

				if(thrustProfile.ThrustBlock == null || MyAPIGateway.Entities.Exist(thrustProfile.ThrustBlock?.SlimBlock?.CubeGrid) == false) {

					this.ThrustProfiles.Remove(thrustProfile);
					continue;

				}

				if(thrustProfile.ThrustBlock.SlimBlock.CubeGrid != this._remoteControl.SlimBlock.CubeGrid) {

					this.ThrustProfiles.Remove(thrustProfile);
					continue;

				}

				thrustProfile.UpdateThrust(allowedThrust, requiredThrust);

			}

			this.PreviousAllowedThrust = allowedThrust;
			this.PreviousRequiredThrust = requiredThrust;

		}

		public void StopAllThrust() {

			this.Strafing = false;
			this.SetThrust(new Vector3I(0, 0, 0), new Vector3I(0, 0, 0));

		}

		public Vector3I TransformThrustData(Vector3I originalData) {

			Vector3I newThrustData = new Vector3I();
			_referenceOrientation = _behavior.Settings.BlockOrientation;

			if (originalData.X != 0) {

				var axisDir = originalData.X == 1 ? _referenceOrientation.TransformDirection(Base6Directions.Direction.Right) : _referenceOrientation.TransformDirection(Base6Directions.Direction.Left);
				DirectionToVector(axisDir, ref newThrustData);

			}

			if (originalData.Y != 0) {

				var axisDir = originalData.Y == 1 ? _referenceOrientation.TransformDirection(Base6Directions.Direction.Up) : _referenceOrientation.TransformDirection(Base6Directions.Direction.Down);
				DirectionToVector(axisDir, ref newThrustData);

			}

			if (originalData.Z != 0) {

				var axisDir = originalData.Z == 1 ? _referenceOrientation.TransformDirection(Base6Directions.Direction.Forward) : _referenceOrientation.TransformDirection(Base6Directions.Direction.Backward);
				DirectionToVector(axisDir, ref newThrustData);

			}

			return newThrustData;

		}

		public void DirectionToVector(Base6Directions.Direction direction, ref Vector3I vectorData) {

			if (direction == Base6Directions.Direction.Forward) {

				vectorData.Z = 1;
				return;

			}

			if (direction == Base6Directions.Direction.Backward) {

				vectorData.Z = -1;
				return;

			}

			if (direction == Base6Directions.Direction.Up) {

				vectorData.Y = 1;
				return;

			}

			if (direction == Base6Directions.Direction.Down) {

				vectorData.Y = -1;
				return;

			}

			if (direction == Base6Directions.Direction.Right) {

				vectorData.X = 1;
				return;

			}

			if (direction == Base6Directions.Direction.Left) {

				vectorData.X = -1;
				return;

			}

		}

	}
	
}