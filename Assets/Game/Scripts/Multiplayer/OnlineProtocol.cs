using System;
using UnityEngine;
using Unity.Netcode;

namespace Bellator.Multiplayer
{
    public enum OnlinePhase { Lobby, Loading, Countdown, Fighting, Result }
    [Serializable] public sealed class OnlineSeat
    {
        public ulong client;
        public string name;
        public int gladiator;
        public bool ready;
    }
    [Serializable] public sealed class OnlineRoom
    {
        public OnlineSeat[] seats = Array.Empty<OnlineSeat>();
        public OnlinePhase phase;
        public string result = "";
        public ulong winner = ulong.MaxValue;
        public double startsAt;
        public int round;
    }
    [Serializable] public struct OnlineInput
    {
        public uint sequence;
        public Vector2 axis;
        public float yaw, pitch;
        public bool sprint, guard, locked;
        // Monotonic action counters survive packet loss without replaying a press.
        public uint light, heavy, dodge, step, ability, reload;
        public bool IsValid => Finite(axis.x) && Finite(axis.y) && Finite(yaw) && Finite(pitch) && axis.sqrMagnitude <= 1.01f && pitch >= -80 && pitch <= 80;
        static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }
    [Serializable] public sealed class OnlineSnapshot : INetworkSerializable
    {
        public int round;
        public uint tick;
        public Vector3 position;
        public Quaternion rotation;
        public float health, stamina, cooldown, speed, x, y, progress;
        public string animation;
        public bool running, locked, blocking, aura;
        public int impact;
        public bool blocked, staggered, broken;
        public float damage;
        public Vector3 hitPoint, dodgeDirection;
        public Vector3[] projectiles = Array.Empty<Vector3>();
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref round); serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref position); serializer.SerializeValue(ref rotation);
            serializer.SerializeValue(ref health); serializer.SerializeValue(ref stamina); serializer.SerializeValue(ref cooldown);
            serializer.SerializeValue(ref speed); serializer.SerializeValue(ref x); serializer.SerializeValue(ref y); serializer.SerializeValue(ref progress);
            serializer.SerializeValue(ref animation); serializer.SerializeValue(ref running); serializer.SerializeValue(ref locked);
            serializer.SerializeValue(ref blocking); serializer.SerializeValue(ref aura); serializer.SerializeValue(ref impact);
            serializer.SerializeValue(ref blocked); serializer.SerializeValue(ref staggered); serializer.SerializeValue(ref broken);
            serializer.SerializeValue(ref damage); serializer.SerializeValue(ref hitPoint); serializer.SerializeValue(ref dodgeDirection);
            serializer.SerializeValue(ref projectiles);
        }
    }
}
