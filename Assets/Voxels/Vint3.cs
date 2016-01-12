using UnityEngine;
using System;

public struct Vint3 : IEquatable<Vint3>
{
    public static readonly Vint3 Zero = new Vint3(0, 0, 0);

    public int x;
    public int y;
    public int z;

    public Vint3(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Vint3(Vector3 vec)
    {
        x = Mathf.RoundToInt(vec.x);
        y = Mathf.RoundToInt(vec.y);
        z = Mathf.RoundToInt(vec.z);
    }

    public static Vint3 operator +(Vint3 a, Vint3 b)
    {
        return new Vint3(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    public static Vint3 operator -(Vint3 a, Vint3 b)
    {
        return new Vint3(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    public static bool operator ==(Vint3 a, Vint3 b)
    {
        return a.x == b.x && a.y == b.y && a.z == b.z;
    }

    public static bool operator !=(Vint3 a, Vint3 b)
    {
        return !(a == b);
    }

    public static Vint3 operator *(Vint3 a, float s)
    {
        return new Vint3(Mathf.FloorToInt(a.x * s), Mathf.FloorToInt(a.y * s), Mathf.FloorToInt(a.z * s));
    }

    // to behave near the x/y/z planes, I manually floor down (c# default is to go towards zero)
    public static Vint3 operator /(Vint3 a, float s)
    {
        return new Vint3(Mathf.FloorToInt(a.x / s), Mathf.FloorToInt(a.y / s), Mathf.FloorToInt(a.z / s));
    }

    public Vector3 Vector
    {
        get { return new Vector3(x, y, z); }
    }

    public override bool Equals(object obj)
    {
        if (obj is Vint3)
        {
            return (Vint3)obj == this;
        }
        return false;
    }

    public bool Equals(Vint3 v)
    {
        return v == this;
    }

    public override int GetHashCode()
    {
        return x * 2 + y * 9941 + z * 900001;
    }

    public override string ToString()
    {
        return "(" + x + "," + y + "," + z + ")";
    }
}

