using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace VildNinja.Voxels.Web
{
    public class Player : IEquatable<Player>
    {
        public readonly int host;
        public readonly int connection;
        public readonly Dictionary<Vint3, int> histroy;
        public Vector3 position;
        public Vint3 area = new Vint3(int.MaxValue, int.MaxValue, int.MaxValue);


        public Player(int connection, int host)
        {
            this.connection = connection;
            this.host = host;
            histroy = new Dictionary<Vint3, int>();
        }

        public bool Equals(Player other)
        {
            return connection == other.connection;
        }

        public override int GetHashCode()
        {
            return connection * 1483;
        }

        public override string ToString()
        {
            return "#" + connection + "@" + host + area;
        }
    }
}
