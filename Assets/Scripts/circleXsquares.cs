using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace circleXsquares {

	/* Primary Definitions */
	
	// HexLocus describes a location in a hexagonal coodinate system
	public struct HexLocus
	{

		//turns out this bugger is pretty dang useful
		private static readonly float sqrt3 = (float)Math.Sqrt(3f);

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
			bool bA, bB, bC, bD, bE, bF;
			bA = h1.a == h2.a;
			bB = h1.b == h2.b;
			bC = h1.c == h2.c;
			bD = h1.d == h2.d;
			bE = h1.e == h2.e;
			bF = h1.f == h2.f;
			return (bA && bB && bC && bD && bE && bF);
		}
		public static bool operator !=(HexLocus h1, HexLocus h2) { return !(h1 == h2); }
		// operators allow HexLocus to be added and subtracted like vectors
		// since they are static, coordinate simplification is ensured by the constructor
		public static HexLocus operator +(HexLocus h1, HexLocus h2)
		{
			int rA, rB, rC, rD, rE, rF;
			rA = h1.a + h2.a;
			rB = h1.b + h2.b;
			rC = h1.c + h2.c;
			rD = h1.d + h2.d;
			rE = h1.e + h2.e;
			rF = h1.f + h2.f;
			return new HexLocus(rA, rB, rC, rD, rE, rF);
		}
		public static HexLocus operator -(HexLocus h1, HexLocus h2)
		{
			int rA, rB, rC, rD, rE, rF;
			rA = h1.a - h2.a;
			rB = h1.b - h2.b;
			rC = h1.c - h2.c;
			rD = h1.d - h2.d;
			rE = h1.e - h2.e;
			rF = h1.f - h2.f;
			return new HexLocus(rA, rB, rC, rD, rE, rF);
		}
		public static HexLocus operator *(HexLocus h1, HexLocus h2)
		{
			int rA, rB, rC, rD, rE, rF;
			rA = h1.a * h2.a;
			rB = h1.b * h2.b;
			rC = h1.c * h2.c;
			rD = h1.d * h2.d;
			rE = h1.e * h2.e;
			rF = h1.f * h2.f;
			return new HexLocus(rA, rB, rC, rD, rE, rF);
		}
		public static HexLocus operator /(HexLocus h1, HexLocus h2)
		{
			int rA, rB, rC, rD, rE, rF;
			rA = h1.a / h2.a;
			rB = h1.b / h2.b;
			rC = h1.c / h2.c;
			rD = h1.d / h2.d;
			rE = h1.e / h2.e;
			rF = h1.f / h2.f;
			return new HexLocus(rA, rB, rC, rD, rE, rF);
		}

		// constructor generates a set of coordinates from a Vector2
		public HexLocus (Vector2 inV2)
		{
			HexLocus h1, h2, h3, h4;

			float delta = inV2.y / sqrt3;
			h1 = new HexLocus((int)Math.Round(inV2.x + delta), 0, (int)Math.Round(delta * 2.0f), 0, 0, 0); // <1>
			h2 = new HexLocus((int)Math.Round(inV2.x - delta), 0, 0, 0, (int)Math.Round(delta * -2.0f), 0); // <2>

			delta = inV2.x / sqrt3;
			h3 = new HexLocus(0, (int)Math.Round(delta * 2.0f), 0, 0, 0, (int)Math.Round(delta - inV2.y)); // <3>
			h4 = new HexLocus(0, 0, 0, (int)Math.Round(delta * -2.0f), 0, (int)Math.Round(-(delta + inV2.y))); // <4>

			float m1, m2, m3, m4; // <5>
			m1 = (h1.ToUnitySpace() - inV2).magnitude;
			m2 = (h2.ToUnitySpace() - inV2).magnitude;
			m3 = (h3.ToUnitySpace() - inV2).magnitude;
			m4 = (h4.ToUnitySpace() - inV2).magnitude;

			if ((m1 < m2) && (m1 < m3) && (m1 < m4)) this = h1;
			else if ((m2 < m3) && (m2 < m4)) this = h2;
			else if (m3 < 4) this = h3;
			else this = h4; // <6>

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
		public HexLocus (int inA, int inB, int inC, int inD, int inE, int inF)
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
			if (!inHL.HasValue) return false;
			else return this == inHL.Value;
		}

		// .NET expects this behavior to be overridden when overriding ==/!= operators
		public override int GetHashCode()
		{
			int hash = 0;
			int[] coordinates = new int[] {this.a, this.b, this.c, this.d, this.e, this.f};
			for (int i = 0; i < 6; i++) hash += coordinates[i] << i;
			return hash;
		}

		// translates a discrete HexLocus into a Unity world space location
		public Vector2 ToUnitySpace ()
		{
			float x, y; // <1>

			x = a;
			x += ((sqrt3 / 2f) * b);
			x -= (0.5f * c);
			x -= ((sqrt3 / 2f) * d);
			x -= (0.5f * e); // <2>

			y = (-1f * f);
			y += (0.5f * b);
			y += ((sqrt3 / 2f) * c);
			y += (0.5f * d);
			y -= ((sqrt3 / 2f) * e); // <3>

			return new Vector3(x, y);

			/*
			<1> ye olde 30-60-90 triangle trigonometry
			<2> x-axis aligns with a; increases with b; and decreases with c, d, and e
			<3> y-axis aligns with f; increases with b, c, and d; and decreases with e
			*/
		}

		// Serialize merely string-separates each component value on a single line
		public string Serialize ()
		{
			string s = a.ToString();
			s += " " + b.ToString();
			s += " " + c.ToString();
			s += " " + d.ToString();
			s += " " + e.ToString();
			s += " " + f.ToString();
			return s;
		}

		// Simplify simplifies current coordinates to simplest possible terms
		// this method should be called every time internal values are changed
		private void Simplify ()
		{
			int inA = a, inB = b, inC = c, inD = d, inE = e, inF = f; // <1>
			int dS = 0, i = 0; // <2> <3>
			bool[] p1 = new bool[]{inA > 0, inC > 0, inE > 0};
			bool[] n1 = new bool[]{inA < 0, inC < 0, inE < 0};
			bool[] p2 = new bool[]{inB > 0, inD > 0, inF > 0};
			bool[] n2 = new bool[]{inB < 0, inD < 0, inF < 0}; // <4>

			foreach (bool bN in p1) if (bN) i++; // <5>
			if (i > 1) {
				if (p1[0] && p1[1] && p1[2]) { // <6>
					dS = inA;
					dS = ((dS >= inC) ^ (dS >= inE)) ? dS : inC;
					dS = ((dS >= inA) ^ (dS >= inE)) ? dS : inE;
				} else { // <7>
					int t1, t2;
					t1 = p1[0] ? inA : inC;
					t2 = p1[2] ? inE : inC;
					dS = (t1 < t2) ? t1 : t2;
				}
			}

			i = 0;
			foreach (bool bN in n1) if (bN) i++; // <8>
			if (i > 1) {
				if (n1[0] && n1[1] && n1[2]) { // <9>
					dS = inA;
					dS = ((dS <= inC) ^ (dS <= inE)) ? dS : inC;
					dS = ((dS <= inA) ^ (dS <= inE)) ? dS : inE;
				} else { // <10>
					int t1, t2;
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
			foreach (bool bN in p2) if (bN) i++; // <12>
			if (i > 1) {
				if (p2[0] && p2[1] && p2[2]) { // <13>
					dS = inB;
					dS = ((dS >= inD) ^ (dS >= inF)) ? dS : inD;
					dS = ((dS >= inB) ^ (dS >= inF)) ? dS : inF;
				} else { // <14>
					int t1, t2;
					t1 = p2[0] ? inB : inD;
					t2 = p2[2] ? inF : inD;
					dS = (t1 < t2) ? t1 : t2;
				}
			}

			i = 0;
			foreach (bool bN in n2) if (bN) i++; // <15>
			if (i > 1) {
				if (n2[0] && n2[1] && n2[2]) { // <16>
					dS = inB;
					dS = ((dS <= inD) ^ (dS <= inF)) ? dS : inD;
					dS = ((dS <= inB) ^ (dS <= inF)) ? dS : inF;
				} else { // <17>
					int t1, t2;
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

	// (??)
	// (!!) currently not in use
	public struct HexOrient
	{

		// (??)
		public HexLocus locus;
		public int rotation;

		// simple constructor
		public HexOrient (HexLocus inLocus, int inRotation)
		{
			locus = inLocus;
			rotation = inRotation;
		}
	}

	// (??)
	// (!!) currently not in use
	public struct TileTrigger
	{

		// (??)
		public bool isActive;
		public HexLocus targetLocus;
		public int targetLayer;

		// simple constructor
		public TileTrigger (bool inActive, HexLocus inLocus, int inLayer)
		{
			isActive = inActive;
			targetLocus = inLocus;
			targetLayer = inLayer;
		}
	}

	// TileData describes a tile by attributes
	public struct TileData
	{

		// TileData consists of a type, color, position, and rotation
		public int type;
		public int color;
		public HexLocus locus;
		public int rotation;

		// simple constructor
		public TileData (int inType, int inColor, HexLocus inLocus, int inRotation)
		{
			type = inType;
			color = inColor;
			locus = inLocus;
			rotation = inRotation;
		}

		// Serialize turns this TileData into strings separated by spaces
		public string Serialize ()
		{
			string s = type.ToString();
			s += " " + color.ToString();
			s += " " + locus.Serialize();
			s += " " + rotation.ToString();
			return s;
		}
	}

	// ChkpntData describes the mechanism by which we track player progress
	public struct ChkpntData
	{

		// a checkpoint simply consists of an activity indicator, and a location
		public bool isActive;
		public HexLocus locus;

		// simple constructor
		public ChkpntData (bool inActive, HexLocus inLocus)
		{
			isActive = inActive;
			locus = inLocus;
		}

		// Serialize turns this ChkpntData into strings separated by spaces
		public string Serialize ()
		{
			string s = (isActive ? 1 : 0).ToString();
			s += " " + locus.Serialize();
			return s;
		}
	}

	// WarpData describes the mechanism by which players win and move between level layers
	public struct WarpData
	{

		// warps are used as win triggers, of which there may be multiple
		public bool isVictory;
		// otherwise, warps consist of two-way flag, origin and target layers, and a position
		public bool isTwoWay;
		public int originLayer;
		public int targetLayer;
		public HexLocus locus;

		// simple constructor
		public WarpData (bool inVictory, bool inTW, int inOrigin, int inTarget, HexLocus inLocus)
		{
			isVictory = inVictory;
			isTwoWay = inTW;
			originLayer = inOrigin;
			targetLayer = inTarget;
			locus = inLocus;
		}

		// Serialize turns this WarpData into strings separated by spaces
		public string Serialize ()
		{
			string s = (isVictory ? 1 : 0).ToString();
			s += " " + (isTwoWay ? 1 : 0).ToString();
			s += " " + originLayer.ToString();
			s += " " + targetLayer.ToString();
			s += " " + locus.Serialize();
			return s;
		}
	}

	// LayerData describes a component of a level by a set of tiles and checkpoints
	public struct LayerData
	{

		// a layer needs to know it's depth within the level using it
		public int layerDepth;
		// the rest of the layer is a set of tiles and a set of checkpoints
		public List<TileData> tileSet;
		public List<ChkpntData> chkpntSet;

		// simple constructor
		public LayerData (int inDepth, List<TileData> inTiles, List<ChkpntData> inChkpnts)
		{
			layerDepth = inDepth;
			tileSet = inTiles;
			chkpntSet = inChkpnts;
		}

		// Serialize turns this LayerData into a "paragraph"-style chunk
		public string[] Serialize ()
		{
			List<string> returnStrings = new List<string>();
			// for each layer, we have a single line comment for layer number
			returnStrings.Add("-- Layer #" + layerDepth.ToString() + " --");
			// then each line below that is a Serialized TileData
			foreach (TileData td in tileSet)
				returnStrings.Add(td.Serialize());

			// next is checkpoints, signaled by a human-readable comment line
			returnStrings.Add("-- Checkpoints --");
			// then each line below lists out the checkpoints for this layer
			foreach (ChkpntData cpd in chkpntSet)
				returnStrings.Add(cpd.Serialize());
			returnStrings.Add("-- End Layer --");

			return returnStrings.ToArray();
		}
	}

	// LevelData is the main aggregate unit of a set of layers and their relationships
	public struct LevelData
	{

		// a level consists of a set of layers and warps between those layers
		public List<LayerData> layerSet;
		public List<WarpData> warpSet;

		// simple constructor
		public LevelData (List<LayerData> inLayers, List<WarpData> inWarps)
		{
			layerSet = inLayers;
			warpSet = inWarps;
		}

		// Serialize turns this LevelData into a list of layer chunks
		public string[] Serialize ()
		{
			List<string> returnStrings = new List<string>();
			// first line of the file is reserved for human-readable comments
			returnStrings.Add("-- Level Comments go here --");

			foreach (LayerData ld in layerSet) {
				// then a chunk is added for each tile in the layer
				returnStrings.Add(" ");
				returnStrings.AddRange(ld.Serialize());
			}

			// single line comment signals warps
			returnStrings.AddRange(new string[]{" ", "-- Warps --"});
			foreach (WarpData wd in warpSet)
				// then each warp for the level is listed out in lines
				returnStrings.Add(wd.Serialize());

			return returnStrings.ToArray();
		}
	}

	/* Utility Definitions */

	// a class of static file parsing methods useful throughout the ecosystem
	public static class FileParsing
	{

		// splitChar is just a useful delimiter for general parsing behavior
		public static Char[] splitChar = new Char[] {' '};

		// reads an array of strings to parse out level data
		public static LevelData ReadLevel (string[] lines)
		{
			// check to see that we have enough data to work with
			if (lines.Length < 3) {
				Debug.LogError("File could not be read correctly.");
				return new LevelData();
			}

			List<TileData> tileList = new List<TileData>();
			List<ChkpntData> chkpntList = new List<ChkpntData>();
			List<LayerData> layerList = new List<LayerData>();
			List<WarpData> warpList = new List<WarpData>();
			bool canReadTile = true;
			bool canReadChkpnt = false;
			bool canReadWarp = false;
			// layerAdd will help us track which layer tiles are put into in the level
			int layerAdd = 0;

			for (int i = 3; i < lines.Length; i++) {
				// (??)
				if (lines[i] == "" || lines[i] == " ") continue;

				// a layer comment precedes a list of tiles
				if (lines[i] == ("-- Layer #" + layerAdd.ToString() + " --")) {
					canReadTile = true;
					canReadChkpnt = false;
					canReadWarp = false;
					continue;
				}
				// a checkpoints comment precedes a list of checkpoints, tied to the last layer comment
				if (lines[i] == "-- Checkpoints --") {
					canReadTile = false;
					canReadChkpnt = true;
					canReadWarp = false;
					continue;
				}
				// an end layer comment packages all tiles and checkpoints into a layer, and then resets
				if (lines[i] == "-- End Layer --") {
					// when we come to the end of a layer, add it to the stack and reset the other lists
					layerList.Add(new LayerData(layerAdd++, tileList, chkpntList));
					tileList = new List<TileData>();
					chkpntList = new List<ChkpntData>();
					continue;
				}
				// a warps comment precedes a list of warps, by convention at the end of a level file
				if (lines[i] == "-- Warps --") {
					canReadTile = false;
					canReadChkpnt = false;
					canReadWarp = true;
					continue;
				}

				// if no comment has been triggered, we should be reading one (and only one) of these three
				if (canReadTile) tileList.Add(ReadTile(lines[i]));
				if (canReadChkpnt) chkpntList.Add(ReadChkpnt(lines[i]));
				if (canReadWarp) warpList.Add(ReadWarp(lines[i]));
			}

			return new LevelData(layerList, warpList);
		}

		// parses a string to construct a TileData
		public static TileData ReadTile (string lineIn)
		{
			// split the line into individual items first
			string[] s = lineIn.Split(splitChar);

			// checks to see if there's enough items to be read
			if (s.Length < 9) {
				Debug.LogError("Line for tile data is formatted incorrectly.");
				return new TileData();
			}

			// proceeds to read the line items
			int i = Int32.Parse(s[0]);
			int j = Int32.Parse(s[1]);
			HexLocus hl = new HexLocus(
				Int32.Parse(s[2]),
				Int32.Parse(s[3]),
				Int32.Parse(s[4]),
				Int32.Parse(s[5]),
				Int32.Parse(s[6]),
				Int32.Parse(s[7]));
			int r = Int32.Parse(s[8]);

			return new TileData(i, j, hl, r);
		}

		// parses a string to construct a ChkpntData
		public static ChkpntData ReadChkpnt (string lineIn)
		{
			// split the line into individual items first
			string[] s = lineIn.Split(splitChar);

			// checks to see if there's enough items to be read
			if (s.Length < 7) {
				Debug.LogError("Line for checkpoint data is formatted incorrectly.");
				return new ChkpntData();
			}

			// proceeds to read the line items
			bool b = Int32.Parse(s[0]) == 0 ? false : true;
			HexLocus hl = new HexLocus(
				Int32.Parse(s[1]),
				Int32.Parse(s[2]),
				Int32.Parse(s[3]),
				Int32.Parse(s[4]),
				Int32.Parse(s[5]),
				Int32.Parse(s[6]));

			return new ChkpntData(b, hl);
		}

		// parses a string to construct a WarpData
		public static WarpData ReadWarp (string lineIn)
		{
			// split the line into individual items first
			string[] s = lineIn.Split(splitChar);

			// checks to see if there's enough items to be read
			if (s.Length < 10) {
				Debug.LogError("Line for checkpoint data is formatted incorrectly.");
				return new WarpData();
			}

			// proceeds to read the line items
			bool b1 = Int32.Parse(s[0]) == 0 ? false : true;
			bool b2 = Int32.Parse(s[1]) == 0 ? false : true;
			int o = Int32.Parse(s[2]);
			int d = Int32.Parse(s[3]);
			HexLocus hl = new HexLocus(
				Int32.Parse(s[4]),
				Int32.Parse(s[5]),
				Int32.Parse(s[6]),
				Int32.Parse(s[7]),
				Int32.Parse(s[8]),
				Int32.Parse(s[9]));

			return new WarpData(b1, b2, o, d, hl);
		}
	}
}
