using System;
using System.Collections.Generic;
using UnityEngine;

namespace circleXsquares
{
    /* Public Utility Enums */

    public enum GravityDirection
    {
        Down = 0,
        Left,
        Up,
        Right,
    }

    public enum TileType
    {
        Tri = 0,
        Dia,
        Trap,
        Hex,
        Sqr,
        Wed,
    }

    public enum TileColor
    {
        Black = 0,
        Blue,
        Brown,
        Green,
        Orange,
        Purple,
        Red,
        White,
    }

    /* Primary Struct Definitions */

    public struct Constants
    {
        public const int DEFAULT_NUM_LAYERS = 10;
        public const int NUM_COLORS = 8;
        public const int NUM_SHAPES = 6;
    }

    // HexLocus describes a location in a hexagonal coodinate system
    public struct HexLocus
    {
        // turns out this bugger is pretty dang useful
        private static readonly float _sqrt3 = (float)Math.Sqrt(3f);

        // primary components are coordinates on overlayed ACE & DEF trigonal axes
        public int a { get; private set; }
        public int b { get; private set; }
        public int c { get; private set; }
        public int d { get; private set; }
        public int e { get; private set; }
        public int f { get; private set; }

        // comparison operators just compares internal coordinates
        public static bool operator ==(HexLocus h1, HexLocus h2)
        {
            return (
                (h1.a == h2.a)
                && (h1.b == h2.b)
                && (h1.c == h2.c)
                && (h1.d == h2.d)
                && (h1.e == h2.e)
                && (h1.f == h2.f)
            );
        }

        public static bool operator !=(HexLocus h1, HexLocus h2)
        {
            return !(h1 == h2);
        }

        // additional operators allow HexLocus to be used like vectors
        // coordinate simplification is ensured by constructor
        public static HexLocus operator +(HexLocus h1, HexLocus h2)
        {
            int rA = h1.a + h2.a;
            int rB = h1.b + h2.b;
            int rC = h1.c + h2.c;
            int rD = h1.d + h2.d;
            int rE = h1.e + h2.e;
            int rF = h1.f + h2.f;
            return new HexLocus(rA, rB, rC, rD, rE, rF);
        }

        public static HexLocus operator -(HexLocus h1, HexLocus h2)
        {
            int rA = h1.a - h2.a;
            int rB = h1.b - h2.b;
            int rC = h1.c - h2.c;
            int rD = h1.d - h2.d;
            int rE = h1.e - h2.e;
            int rF = h1.f - h2.f;
            return new HexLocus(rA, rB, rC, rD, rE, rF);
        }

        public static HexLocus operator *(HexLocus h1, HexLocus h2)
        {
            int rA = h1.a * h2.a;
            int rB = h1.b * h2.b;
            int rC = h1.c * h2.c;
            int rD = h1.d * h2.d;
            int rE = h1.e * h2.e;
            int rF = h1.f * h2.f;
            return new HexLocus(rA, rB, rC, rD, rE, rF);
        }

        public static HexLocus operator /(HexLocus h1, HexLocus h2)
        {
            int rA = h1.a / h2.a;
            int rB = h1.b / h2.b;
            int rC = h1.c / h2.c;
            int rD = h1.d / h2.d;
            int rE = h1.e / h2.e;
            int rF = h1.f / h2.f;
            return new HexLocus(rA, rB, rC, rD, rE, rF);
        }

        // constructor generates a set of coordinates from a Vector2
        public HexLocus(Vector2 inV2)
        {
            HexLocus h1,
                h2,
                h3,
                h4;

            float delta = inV2.y / _sqrt3;
            h1 = new HexLocus(
                (int)Math.Round(inV2.x + delta),
                0,
                (int)Math.Round(delta * 2.0f),
                0,
                0,
                0
            ); // <1>
            h2 = new HexLocus(
                (int)Math.Round(inV2.x - delta),
                0,
                0,
                0,
                (int)Math.Round(delta * -2.0f),
                0
            ); // <2>

            delta = inV2.x / _sqrt3;
            h3 = new HexLocus(
                0,
                (int)Math.Round(delta * 2.0f),
                0,
                0,
                0,
                (int)Math.Round(delta - inV2.y)
            ); // <3>
            h4 = new HexLocus(
                0,
                0,
                0,
                (int)Math.Round(delta * -2.0f),
                0,
                (int)Math.Round(-(delta + inV2.y))
            ); // <4>

            float m1,
                m2,
                m3,
                m4; // <5>
            m1 = (h1.ToUnitySpace() - inV2).magnitude;
            m2 = (h2.ToUnitySpace() - inV2).magnitude;
            m3 = (h3.ToUnitySpace() - inV2).magnitude;
            m4 = (h4.ToUnitySpace() - inV2).magnitude;

            if ((m1 < m2) && (m1 < m3) && (m1 < m4))
                this = h1;
            else if ((m2 < m3) && (m2 < m4))
                this = h2;
            else if (m3 < 4)
                this = h3;
            else
                this = h4; // <6>

            /*
            <1> h1 finds an (a, c) point
            <2> h2 finds an (a, e) point
            <3> h3 finds a (b, f) point
            <4> h4 finds a (d, f) point
            <5> magnitude of each is computed
            <6> the HexLocus of least magnitude is chosen
            */
        }

        // simple constructor using Simplify
        public HexLocus(int inA, int inB, int inC, int inD, int inE, int inF)
        {
            a = inA;
            b = inB;
            c = inC;
            d = inD;
            e = inE;
            f = inF;

            Simplify();
        }

        // .NET expects this behavior to be overridden when overriding ==/!= operators
        public override bool Equals(System.Object obj)
        {
            HexLocus? inHL = obj as HexLocus?;
            if (!inHL.HasValue)
                return false;
            else
                return this == inHL.Value;
        }

        // .NET expects this behavior to be overridden when overriding ==/!= operators
        public override int GetHashCode()
        {
            int hash = 0;
            int[] coordinates = { this.a, this.b, this.c, this.d, this.e, this.f };
            for (int i = 0; i < 6; i++)
                hash += coordinates[i] << i;
            return hash;
        }

        // translates a discrete HexLocus into a Vector2
        public Vector2 ToUnitySpace()
        {
            float x,
                y; // <1>

            x = a;
            x += ((_sqrt3 / 2f) * b);
            x -= (0.5f * c);
            x -= ((_sqrt3 / 2f) * d);
            x -= (0.5f * e); // <2>

            y = (-1f * f);
            y += (0.5f * b);
            y += ((_sqrt3 / 2f) * c);
            y += (0.5f * d);
            y -= ((_sqrt3 / 2f) * e); // <3>

            return new Vector3(x, y);

            /*
            <1> ye olde 30-60-90 triangle trigonometry
            <2> x-axis aligns with a; increases with b; and decreases with c, d, and e
            <3> y-axis aligns with f; increases with b, c, and d; and decreases with e
            */
        }

        // Serialize merely string-separates each component value on a single line
        public string Serialize()
        {
            string s = a.ToString();
            s += " " + b.ToString();
            s += " " + c.ToString();
            s += " " + d.ToString();
            s += " " + e.ToString();
            s += " " + f.ToString();
            return s;
        }

        // pretty-printing of HexLocus coordinates for display
        public string PrettyPrint()
        {
            string s = "(";
            int[] vals = new int[] { a, c, e, b, d, f }; // <1>
            string[] s_vals = new string[vals.Length * 2]; // <2>
            for (int i = 0; i < 6; i++)
                s_vals[i * 2] = vals[i].ToString(); // <3>
            foreach (int i in new int[] { 1, 3, 7, 9 })
                s_vals[i] = ", "; // <4>
            s_vals[5] = "),\n(";
            s_vals[11] = ")";
            s += String.Join("", s_vals); // <5>
            return s;

            /*
            <1> coordinates are arranged into ACE & BDF triples for human-readability
            <2> s_vals is twice the size of s to hold interspersing strings as well
            <3> every even s_vals index is filled with the corresponding int string
            <4> selective odd s_vals indices are filled with interspersing filler
            <5> the concatenation of s_vals is appended to s and returned
            */
        }

        // Simplify simplifies current coordinates to simplest possible terms
        // this method should be called every time internal values are changed
        private void Simplify()
        {
            int inA = a,
                inB = b,
                inC = c,
                inD = d,
                inE = e,
                inF = f; // <1>
            int dS = 0,
                i = 0; // <2> <3>
            bool[] p1 = { inA > 0, inC > 0, inE > 0 };
            bool[] n1 = { inA < 0, inC < 0, inE < 0 };
            bool[] p2 = { inB > 0, inD > 0, inF > 0 };
            bool[] n2 = { inB < 0, inD < 0, inF < 0 }; // <4>

            foreach (bool bN in p1)
                if (bN)
                    i++; // <5>
            if (i > 1)
            {
                if (p1[0] && p1[1] && p1[2])
                { // <6>
                    dS = inA;
                    dS = ((dS >= inC) ^ (dS >= inE)) ? dS : inC;
                    dS = ((dS >= inA) ^ (dS >= inE)) ? dS : inE;
                }
                else
                { // <7>
                    int t1,
                        t2;
                    t1 = p1[0] ? inA : inC;
                    t2 = p1[2] ? inE : inC;
                    dS = (t1 < t2) ? t1 : t2;
                }
            }

            i = 0;
            foreach (bool bN in n1)
                if (bN)
                    i++; // <8>
            if (i > 1)
            {
                if (n1[0] && n1[1] && n1[2])
                { // <9>
                    dS = inA;
                    dS = ((dS <= inC) ^ (dS <= inE)) ? dS : inC;
                    dS = ((dS <= inA) ^ (dS <= inE)) ? dS : inE;
                }
                else
                { // <10>
                    int t1,
                        t2;
                    t1 = n1[0] ? inA : inC;
                    t2 = n1[2] ? inE : inC;
                    dS = (t1 > t2) ? t1 : t2;
                }
            }

            a = inA - dS;
            c = inC - dS;
            e = inE - dS; // <11>

            dS = 0;
            i = 0;
            foreach (bool bN in p2)
                if (bN)
                    i++; // <12>
            if (i > 1)
            {
                if (p2[0] && p2[1] && p2[2])
                { // <13>
                    dS = inB;
                    dS = ((dS >= inD) ^ (dS >= inF)) ? dS : inD;
                    dS = ((dS >= inB) ^ (dS >= inF)) ? dS : inF;
                }
                else
                { // <14>
                    int t1,
                        t2;
                    t1 = p2[0] ? inB : inD;
                    t2 = p2[2] ? inF : inD;
                    dS = (t1 < t2) ? t1 : t2;
                }
            }

            i = 0;
            foreach (bool bN in n2)
                if (bN)
                    i++; // <15>
            if (i > 1)
            {
                if (n2[0] && n2[1] && n2[2])
                { // <16>
                    dS = inB;
                    dS = ((dS <= inD) ^ (dS <= inF)) ? dS : inD;
                    dS = ((dS <= inB) ^ (dS <= inF)) ? dS : inF;
                }
                else
                { // <17>
                    int t1,
                        t2;
                    t1 = n2[0] ? inB : inD;
                    t2 = n2[2] ? inF : inD;
                    dS = (t1 > t2) ? t1 : t2;
                }
            }

            b = inB - dS;
            d = inD - dS;
            f = inF - dS; // <18>

            /*
            <1> first, copy the input variables
            <2> dS is the total delta shift to be added or subtracted from each coordinate within a triad
            <3> i is a temporary value used to store the number of positive/negative coordinates within each triad
            <4> p and n arrays articulate which values are positive and negative for each triad
            <5> the first case addresses multiple positive values in the first triad
            <6> if all values are positive, dS is set to the middle value
            <7> if only two values are positive, dS is set to the smaller positive value
            <8> the second case addresses multiple negative values in the first triad
            <9> if all values are negative, dS is set to the middle value
            <10> if only two values are negative, dS is set to the smaller negative value
            <11> dS is applied to the first triad
            <12> the third case addresses multiple positive values in the second triad
            <13> if all values are positive, dS is set to the middle value
            <14> if only two values are positive, dS is set to the smaller positive value
            <15> the fourth case addresses multiple negative values in the second triad
            <16> if all values are negative, dS is set to the middle value
            <17> if only two values are negative, dS is set to the smaller negative value
            <18> dS is applied to the second triad
            */
        }
    }

    // HexOrient describes an orientation within the game space
    public struct HexOrient
    {
        // HexOrient consists of a locus, rotation, and layer
        public HexLocus locus;
        public int rotation;
        public int layer;

        // simple constructor
        public HexOrient(HexLocus inLocus, int inRotation, int inLayer)
        {
            locus = inLocus;
            rotation = (inRotation + 12) % 12;
            layer = inLayer;
            if (layer < 0)
                layer = 0;
        }

        // Serialize turns this HexOrient into strings separated by spaces
        public string Serialize()
        {
            string s = locus.Serialize();
            s += " " + rotation.ToString();
            s += " " + layer.ToString();
            return s;
        }

        // translates a discrete HexOrient into a Vector3 and outputs rotation
        public Vector3 ToUnitySpace(out Quaternion outRotation)
        {
            Vector2 v2 = locus.ToUnitySpace();
            outRotation = Quaternion.Euler(0, 0, 30 * rotation);
            return new Vector3(v2.x, v2.y, 2f * layer);
        }

        public static bool operator ==(HexOrient ho1, HexOrient ho2)
        {
            return (
                (ho1.locus == ho2.locus)
                && (ho1.rotation == ho2.rotation)
                && (ho1.layer == ho2.layer)
            );
        }

        public static bool operator !=(HexOrient ho1, HexOrient ho2)
        {
            return !(ho1 == ho2);
        }

        // .NET expects this behavior to be overridden when overriding ==/!= operators
        public override bool Equals(System.Object obj)
        {
            HexOrient? inHO = obj as HexOrient?;
            if (!inHO.HasValue)
                return false;
            else
                return this == inHO.Value;
        }

        // .NET expects this behavior to be overridden when overriding ==/!= operators
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    // TileData describes a tile by attributes
    public struct TileData
    {
        // TileData consists of a type, color, and orientation
        public int type;
        public int color;
        public int special;
        public HexOrient orient;
        public int doorId;

        // simple constructor
        public TileData(int inType, int inColor, int inSpec, HexOrient inOrient, int inDoorId = 0)
        {
            type = inType;
            color = inColor;
            special = inSpec;
            doorId = inDoorId;
            orient = inOrient;
        }

        // Serialize turns this TileData into strings separated by spaces
        public string Serialize()
        {
            string s = type.ToString();
            s += " " + color.ToString();
            s += " " + special.ToString();
            s += " " + orient.Serialize();
            s += " " + doorId.ToString();
            return s;
        }

        public static bool operator ==(TileData td1, TileData td2)
        {
            return (
                (td1.type == td2.type)
                && (td1.color == td2.color)
                && (td1.special == td2.special)
                && (td1.orient == td2.orient)
                && (td1.doorId == td2.doorId)
            );
        }

        public static bool operator !=(TileData td1, TileData td2)
        {
            return !(td1 == td2);
        }

        // .NET expects this behavior to be overridden when overriding ==/!= operators
        public override bool Equals(System.Object obj)
        {
            TileData? inTD = obj as TileData?;
            if (!inTD.HasValue)
                return false;
            else
                return this == inTD.Value;
        }

        // .NET expects this behavior to be overridden when overriding ==/!= operators
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    // CheckpointData describes the player progression tracking for a level
    public struct CheckpointData
    {
        // a checkpoint simply consists of a location and layer (no rotation)
        public HexLocus locus;
        public int layer;

        // simple constructor
        public CheckpointData(HexLocus inLocus, int inLayer)
        {
            locus = inLocus;
            layer = inLayer;
            if (layer < 0)
                layer = 0;
        }

        // Serialize turns this CheckpointData into strings separated by spaces
        public string Serialize()
        {
            string s = locus.Serialize();
            s += " " + layer.ToString();
            return s;
        }

        public static bool operator ==(CheckpointData a, CheckpointData b)
        {
            return (a.locus == b.locus) && (a.layer == b.layer);
        }

        public static bool operator !=(CheckpointData a, CheckpointData b)
        {
            return !(a == b);
        }

        // .NET expects this behavior to be overridden when overriding ==/!= operators
        public override bool Equals(System.Object obj)
        {
            CheckpointData? inCD = obj as CheckpointData?;
            if (!inCD.HasValue)
                return false;
            else
                return this == inCD.Value;
        }

        // .NET expects this behavior to be overridden when overriding ==/!= operators
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    // VictoryData describes the win condition(s) of a level
    public struct VictoryData
    {
        public HexLocus locus;
        public int layer;

        // simple constructor
        public VictoryData(HexLocus inLocus, int inLayer)
        {
            locus = inLocus;
            layer = inLayer;
            if (layer < 0)
                layer = 0;
        }

        // Serialize turns this VictoryData into strings separated by spaces
        public string Serialize()
        {
            string s = locus.Serialize();
            s += " " + layer.ToString();
            return s;
        }

        public static bool operator ==(VictoryData a, VictoryData b)
        {
            return (a.locus == b.locus) && (a.layer == b.layer);
        }

        public static bool operator !=(VictoryData a, VictoryData b)
        {
            return !(a == b);
        }

        // .NET expects this behavior to be overridden when overriding ==/!= operators
        public override bool Equals(System.Object obj)
        {
            VictoryData? inVD = obj as VictoryData?;
            if (!inVD.HasValue)
                return false;
            else
                return this == inVD.Value;
        }

        // .NET expects this behavior to be overridden when overriding ==/!= operators
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    // WarpData describes the mechanism by which players move between level layers
    public struct WarpData
    {
        public HexLocus locus;
        public int layer;
        public int targetLayer
        {
            get { return layer + 1; }
            set { }
        }

        // simple constructor
        public WarpData(HexLocus inLocus, int inLayer)
        {
            locus = inLocus;
            layer = inLayer;
        }

        // Serialize turns this WarpData into strings separated by spaces
        public string Serialize()
        {
            string s = locus.Serialize();
            s += " " + layer.ToString();
            return s;
        }

        public static bool operator ==(WarpData wd1, WarpData wd2)
        {
            return wd1.locus == wd2.locus && wd1.layer == wd2.layer;
        }

        public static bool operator !=(WarpData wd1, WarpData wd2)
        {
            return !(wd1 == wd2);
        }

        // .NET expects this behavior to be overridden when overriding ==/!= operators
        public override bool Equals(System.Object obj)
        {
            WarpData? inWD = obj as WarpData?;
            if (!inWD.HasValue)
                return false;
            else
                return this == inWD.Value;
        }

        // .NET expects this behavior to be overridden when overriding ==/!= operators
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    // LevelData is the main aggregate unit
    public struct LevelData
    {
        // a level consists of a set of tiles, checkpoints, and warps
        public List<TileData> tileSet;
        public List<CheckpointData> chkpntSet;
        public List<VictoryData> victorySet;
        public List<WarpData> warpSet;

        // simple constructor
        public LevelData(
            List<TileData> inTiles,
            List<CheckpointData> inChkpnts,
            List<VictoryData> inVictories,
            List<WarpData> inWarps
        )
        {
            tileSet = inTiles;
            chkpntSet = inChkpnts;
            victorySet = inVictories;
            warpSet = inWarps;
        }

        // Serialize turns this LevelData into a list of layer chunks
        public string[] Serialize()
        {
            List<string> returnStrings = new List<string>();
            // first line of the file is reserved for human-readable comments
            returnStrings.Add("-- level comments goes here --");
            // second line will get used for player start information
            returnStrings.AddRange(
                new string[] { "-- player start info goes here --", " ", "-- Tiles --" }
            );
            foreach (TileData td in tileSet)
                returnStrings.Add(td.Serialize());

            returnStrings.AddRange(new string[] { "-- End Tiles --", " ", "-- Checkpoints --" });
            foreach (CheckpointData cd in chkpntSet)
                returnStrings.Add(cd.Serialize());

            returnStrings.AddRange(
                new string[] { "-- End Checkpoints --", " ", "-- Victories --" }
            );
            foreach (VictoryData vd in victorySet)
                returnStrings.Add(vd.Serialize());

            returnStrings.AddRange(new string[] { "-- End Victories --", " ", "-- Warps --" });
            foreach (WarpData wd in warpSet)
                returnStrings.Add(wd.Serialize());
            returnStrings.Add("-- End Warps --");

            return returnStrings.ToArray();
        }
    }

    /* Utility Definitions */

    // a class of static file parsing methods useful throughout the ecosystem
    public static class LevelLoader
    {
        // splitChar is just a useful delimiter for general parsing behavior
        public static Char[] splitChar = { ' ' };

        // reads an array of strings to parse out level data
        public static LevelData LoadLevel(string[] lines)
        {
            // check to see that we have enough data to work with
            if (lines.Length < 10)
            {
                Debug.LogError("File could not be read correctly.");
                return new LevelData();
            }

            List<TileData> tileList = new List<TileData>();
            List<CheckpointData> chkpntList = new List<CheckpointData>();
            List<VictoryData> victoryList = new List<VictoryData>();
            List<WarpData> warpList = new List<WarpData>();
            bool canReadTile = false,
                canReadChkpnt = false,
                canReadVictory = false,
                canReadWarp = false;

            for (int i = 3; i < lines.Length; i++)
            {
                // skip over empty lines
                if (lines[i] == "" || lines[i] == " ")
                    continue;

                // a tiles comment precedes a list of tiles
                if (lines[i] == ("-- Tiles --"))
                {
                    canReadTile = true;
                    continue;
                }
                // a checkpoints comment precedes a list of checkpoints
                if (lines[i] == "-- Checkpoints --")
                {
                    canReadChkpnt = true;
                    continue;
                }
                // a victories comment precedes a list of victories
                if (lines[i] == "-- Victories --")
                {
                    canReadVictory = true;
                    continue;
                }
                // a warps comment precedes a list of warps
                if (lines[i] == "-- Warps --")
                {
                    canReadWarp = true;
                    continue;
                }
                // an end tiles comment follows a list of tiles
                if (lines[i] == ("-- End Tiles --"))
                {
                    canReadTile = false;
                    continue;
                }
                // an end checkpoints comment follows a list of checkpoints
                if (lines[i] == "-- End Checkpoints --")
                {
                    canReadChkpnt = false;
                    continue;
                }
                // an end victories comment follows a list of victories
                if (lines[i] == "-- End Victories --")
                {
                    canReadVictory = false;
                    continue;
                }
                // and warps comment follows a list of warps
                if (lines[i] == "-- End Warps --")
                {
                    canReadWarp = false;
                    continue;
                }

                // if no comment has been triggered, we should be reading one (and only one) of these three
                if (canReadTile)
                    tileList.Add(ReadTile(lines[i]));
                if (canReadChkpnt)
                    chkpntList.Add(ReadChkpnt(lines[i]));
                if (canReadVictory)
                    victoryList.Add(ReadVictory(lines[i]));
                if (canReadWarp)
                    warpList.Add(ReadWarp(lines[i]));
            }

            return new LevelData(tileList, chkpntList, victoryList, warpList);
        }

        // parses a string to construct a TileData
        public static TileData ReadTile(string lineIn)
        {
            // split the line into individual items first
            string[] s = lineIn.Split(splitChar);

            // checks to see if there's enough items to be read
            if (s.Length < 10)
            {
                Debug.LogError("Line for tile data is formatted incorrectly.");
                return new TileData();
            }

            // proceeds to read the line items
            int type = Int32.Parse(s[0]);
            int color = Int32.Parse(s[1]);
            int extra = Int32.Parse(s[2]);
            HexLocus hexLocus = new HexLocus(
                Int32.Parse(s[3]),
                Int32.Parse(s[4]),
                Int32.Parse(s[5]),
                Int32.Parse(s[6]),
                Int32.Parse(s[7]),
                Int32.Parse(s[8])
            );
            int roation = Int32.Parse(s[9]);
            int layer = Int32.Parse(s[10]);

            int doorId = 0;
            if (s.Length > 11)
            {
                doorId = Int32.Parse(s[11]);
            }

            return new TileData(
                type,
                color,
                extra,
                new HexOrient(hexLocus, roation, layer),
                doorId
            );
        }

        // parses a string to construct a CheckpointData
        public static CheckpointData ReadChkpnt(string lineIn)
        {
            // split the line into individual items first
            string[] s = lineIn.Split(splitChar);

            // checks to see if there's enough items to be read
            if (s.Length < 7)
            {
                Debug.LogError("Line for checkpoint data is formatted incorrectly.");
                return new CheckpointData();
            }

            // proceeds to read the line items
            HexLocus hl = new HexLocus(
                Int32.Parse(s[0]),
                Int32.Parse(s[1]),
                Int32.Parse(s[2]),
                Int32.Parse(s[3]),
                Int32.Parse(s[4]),
                Int32.Parse(s[5])
            );
            int y = Int32.Parse(s[6]);

            return new CheckpointData(hl, y);
        }

        // parses a string to construct a VictoryData
        public static VictoryData ReadVictory(string lineIn)
        {
            // split the line into individual items first
            string[] s = lineIn.Split(splitChar);

            // checks to see if there's enough items to be read
            if (s.Length < 7)
            {
                Debug.LogError("Line for victory data is formatted incorrectly.");
                return new VictoryData();
            }

            // proceeds to read the line items
            HexLocus hl = new HexLocus(
                Int32.Parse(s[0]),
                Int32.Parse(s[1]),
                Int32.Parse(s[2]),
                Int32.Parse(s[3]),
                Int32.Parse(s[4]),
                Int32.Parse(s[5])
            );
            int y = Int32.Parse(s[6]);

            return new VictoryData(hl, y);
        }

        // parses a string to construct a WarpData
        public static WarpData ReadWarp(string lineIn)
        {
            // split the line into individual items first
            string[] s = lineIn.Split(splitChar);

            // checks to see if there's enough items to be read
            if (s.Length < 7)
            {
                Debug.LogError("Line for checkpoint data is formatted incorrectly.");
                return new WarpData();
            }

            // proceeds to read the line items
            HexLocus hl = new HexLocus(
                Int32.Parse(s[0]),
                Int32.Parse(s[1]),
                Int32.Parse(s[2]),
                Int32.Parse(s[3]),
                Int32.Parse(s[4]),
                Int32.Parse(s[5])
            );
            int y = Int32.Parse(s[6]);

            return new WarpData(hl, y);
        }
    }
}
