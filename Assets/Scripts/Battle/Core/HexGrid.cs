using System;
using System.Collections.Generic;

namespace AshenOath
{
    // Odd-row offset: нечётные ряды сдвинуты вправо на половину гекса.
    public struct Hex : IEquatable<Hex>
    {
        public int X, Y;
        public Hex(int x, int y) { X = x; Y = y; }
        public bool Equals(Hex other) { return X == other.X && Y == other.Y; }
        public override bool Equals(object obj) { return obj is Hex && Equals((Hex)obj); }
        public override int GetHashCode() { return X * 397 ^ Y; }
        public static bool operator ==(Hex a, Hex b) { return a.Equals(b); }
        public static bool operator !=(Hex a, Hex b) { return !a.Equals(b); }
        public override string ToString() { return X + ", " + Y; }
        public static int Distance(Hex a, Hex b)
        {
            int aq = a.X - (a.Y - (a.Y & 1)) / 2;
            int bq = b.X - (b.Y - (b.Y & 1)) / 2;
            int dq = aq - bq, dr = a.Y - b.Y;
            return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(dq + dr)) / 2;
        }
        public IEnumerable<Hex> Neighbors()
        {
            int offset = Y & 1;
            yield return new Hex(X - 1, Y); yield return new Hex(X + 1, Y);
            yield return new Hex(X - 1 + offset, Y + 1); yield return new Hex(X + offset, Y + 1);
            yield return new Hex(X - 1 + offset, Y - 1); yield return new Hex(X + offset, Y - 1);
        }
    }
}
