using UnityEngine;
using System;
using Cysharp.Threading.Tasks;


namespace toio.VisualScript
{
	/// <summary>
	/// Visual Scripting 用 Cube ラッパー
	/// </summary>
	public static class CubeUtils
	{
		// --- プロパティ (get only) ---
		public static string GetVersion(Cube cube) => cube.version;
		public static string GetId(Cube cube) => cube.id;
		public static string GetAddr(Cube cube) => cube.addr;
		public static string GetLocalName(Cube cube) => cube.localName;
		public static bool GetIsConnected(Cube cube) => cube.isConnected;
		public static int GetBattery(Cube cube) => cube.battery;
		public static int GetX(Cube cube) => cube.x;
		public static int GetY(Cube cube) => cube.y;
		public static Vector2 GetPos(Cube cube) => cube.pos;
		public static int GetAngle(Cube cube) => cube.angle;
		public static Vector2 GetSensorPos(Cube cube) => cube.sensorPos;
		public static int GetSensorAngle(Cube cube) => cube.sensorAngle;
		public static uint GetStandardId(Cube cube) => cube.standardId;
		public static bool GetIsPressed(Cube cube) => cube.isPressed;
		public static bool GetIsSloped(Cube cube) => cube.isSloped;
		public static bool GetIsCollisionDetected(Cube cube) => cube.isCollisionDetected;
		public static bool GetIsGrounded(Cube cube) => cube.isGrounded;
		public static int GetMaxSpd(Cube cube) => cube.maxSpd;
		public static int GetDeadzone(Cube cube) => cube.deadzone;
		public static bool GetIsDoubleTap(Cube cube) => cube.isDoubleTap;
		public static Cube.PoseType GetPose(Cube cube) => cube.pose;
		public static int GetShakeLevel(Cube cube) => cube.shakeLevel;
		public static int GetLeftSpeed(Cube cube) => cube.leftSpeed;
		public static int GetRightSpeed(Cube cube) => cube.rightSpeed;
		public static Cube.MagnetState GetMagnetState(Cube cube) => cube.magnetState;
		public static Vector3 GetMagneticForce(Cube cube) => cube.magneticForce;
		public static Vector3 GetEulers(Cube cube) => cube.eulers;
		public static Quaternion GetQuaternion(Cube cube) => cube.quaternion;
		public static int GetConnectionIntervalMin(Cube cube) => cube.connectionIntervalMin;
		public static int GetConnectionIntervalMax(Cube cube) => cube.connectionIntervalMax;
		public static int GetConnectionInterval(Cube cube) => cube.connectionInterval;

		// --- メソッド ---
		public static Cube Move(Cube cube, int left, int right, int durationMs, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Weak)
		{
			cube.Move(left, right, durationMs, order);
			return cube;
		}
		public static Cube TurnLedOn(Cube cube, int red, int green, int blue, int durationMs, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
		{
			cube.TurnLedOn(red, green, blue, durationMs, order);
			return cube;
		}
		public static Cube TurnOnLightWithScenario(Cube cube, int repeatCount, Cube.LightOperation[] operations, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
		{
			cube.TurnOnLightWithScenario(repeatCount, operations, order);
			return cube;
		}
		public static Cube TurnLedOff(Cube cube, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
		{
			cube.TurnLedOff(order);
			return cube;
		}
		public static Cube PlayPresetSound(Cube cube, int soundId, int volume = 255, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
		{
			cube.PlayPresetSound(soundId, volume, order);
			return cube;
		}
		public static Cube PlaySound(Cube cube, int repeatCount, Cube.SoundOperation[] operations, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
		{
			cube.PlaySound(repeatCount, operations, order);
			return cube;
		}
		public static Cube PlaySound(Cube cube, byte[] buff, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
		{
			cube.PlaySound(buff, order);
			return cube;
		}
		public static Cube StopSound(Cube cube, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
		{
			cube.StopSound(order);
			return cube;
		}
		public static Cube ConfigSlopeThreshold(Cube cube, int angle, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
		{
			cube.ConfigSlopeThreshold(angle, order);
			return cube;
		}
		public static Cube ConfigCollisionThreshold(Cube cube, int level, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
		{
			cube.ConfigCollisionThreshold(level, order);
			return cube;
		}
		public static Cube ConfigDoubleTapInterval(Cube cube, int interval, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
		{
			cube.ConfigDoubleTapInterval(interval, order);
			return cube;
		}
		public static Cube TargetMove(Cube cube, int targetX, int targetY, int targetAngle, int configID = 0, int timeOut = 0, Cube.TargetMoveType targetMoveType = Cube.TargetMoveType.RotatingMove, int maxSpd = 80, Cube.TargetSpeedType targetSpeedType = Cube.TargetSpeedType.UniformSpeed, Cube.TargetRotationType targetRotationType = Cube.TargetRotationType.AbsoluteLeastAngle, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
		{
			cube.TargetMove(targetX, targetY, targetAngle, configID, timeOut, targetMoveType, maxSpd, targetSpeedType, targetRotationType, order);
			return cube;
		}
		public static Cube AccelerationMove(Cube cube, int targetSpeed, int acceleration, int rotationSpeed = 0, Cube.AccPriorityType accPriorityType = Cube.AccPriorityType.Translation, int controlTime = 0, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
		{
			cube.AccelerationMove(targetSpeed, acceleration, rotationSpeed, accPriorityType, controlTime, order);
			return cube;
		}
		public static UniTask ConfigMotorRead(Cube cube, bool valid, float timeOutSec = 0.5f, Action<bool, Cube> callback = null, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
			=> cube.ConfigMotorRead(valid, timeOutSec, callback, order);
		public static UniTask ConfigIDNotification(Cube cube, int intervalMs, Cube.IDNotificationType notificationType = Cube.IDNotificationType.Balanced, float timeOutSec = 0.5f, Action<bool, Cube> callback = null, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
			=> cube.ConfigIDNotification(intervalMs, notificationType, timeOutSec, callback, order);
		public static UniTask ConfigIDMissedNotification(Cube cube, int sensitivityMs, float timeOutSec = 0.5f, Action<bool, Cube> callback = null, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
			=> cube.ConfigIDMissedNotification(sensitivityMs, timeOutSec, callback, order);
		public static UniTask ConfigMagneticSensor(Cube cube, Cube.MagneticMode mode, float timeOutSec = 0.5f, Action<bool, Cube> callback = null, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
			=> cube.ConfigMagneticSensor(mode, timeOutSec, callback, order);
		public static UniTask ConfigMagneticSensor(Cube cube, Cube.MagneticMode mode, int intervalMs, Cube.MagneticNotificationType notificationType, float timeOutSec = 0.5f, Action<bool, Cube> callback = null, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
			=> cube.ConfigMagneticSensor(mode, intervalMs, notificationType, timeOutSec, callback, order);
		public static UniTask ConfigAttitudeSensor(Cube cube, Cube.AttitudeFormat format, int intervalMs, Cube.AttitudeNotificationType notificationType, float timeOutSec = 0.5f, Action<bool, Cube> callback = null, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
			=> cube.ConfigAttitudeSensor(format, intervalMs, notificationType, timeOutSec, callback, order);
		public static Cube RequestMotionSensor(Cube cube, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
		{
			cube.RequestMotionSensor(order);
			return cube;
		}
		public static Cube RequestMagneticSensor(Cube cube, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
		{
			cube.RequestMagneticSensor(order);
			return cube;
		}
		public static Cube RequestAttitudeSensor(Cube cube, Cube.AttitudeFormat format, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
		{
			cube.RequestAttitudeSensor(format, order);
			return cube;
		}
		public static UniTask ConfigConnectionInterval(Cube cube, int min, int max, float timeOutSec = 0.5f, Action<bool, Cube> callback = null, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
			=> cube.ConfigConnectionInterval(min, max, timeOutSec, callback, order);
		public static Cube ObtainConnectionIntervalConfig(Cube cube, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
		{
			cube.ObtainConnectionIntervalConfig(order);
			return cube;
		}
		public static Cube ObtainConnectionInterval(Cube cube, Cube.ORDER_TYPE order = Cube.ORDER_TYPE.Strong)
		{
			cube.ObtainConnectionInterval(order);
			return cube;
		}

		public static byte NoteNumberToByte(Cube.NOTE_NUMBER input_enum) => (byte)input_enum;
		public static byte FloatToByte(float input_float) => (byte)input_float;

		// --- Struct ヘルパー (Visual Scripting) ---
		public static Cube.SoundOperation CreateSoundOperation(int durationMs, byte volume, byte noteNumber)
			=> new(durationMs, volume, noteNumber);

		public static Cube.SoundOperation CreateSoundOperation(int durationMs, byte volume, Cube.NOTE_NUMBER noteNumber)
			=> new(durationMs, volume, noteNumber);

		public static int GetSoundOperationDurationMs(Cube.SoundOperation operation) => operation.durationMs;
		public static byte GetSoundOperationVolume(Cube.SoundOperation operation) => operation.volume;
		public static byte GetSoundOperationNoteNumber(Cube.SoundOperation operation) => operation.note_number;

		public static Cube.LightOperation CreateLightOperation(int durationMs, byte red, byte green, byte blue)
			=> new(durationMs, red, green, blue);

		public static int GetLightOperationDurationMs(Cube.LightOperation operation) => operation.durationMs;
		public static byte GetLightOperationRed(Cube.LightOperation operation) => operation.red;
		public static byte GetLightOperationGreen(Cube.LightOperation operation) => operation.green;
		public static byte GetLightOperationBlue(Cube.LightOperation operation) => operation.blue;
	}
}
