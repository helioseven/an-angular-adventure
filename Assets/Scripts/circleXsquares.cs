using System;
using System.Collections;
using UnityEngine;

namespace circleXsquares {

	// tileData describes a tile without reference to the GameObject
	public struct tileData
	{

		// tileData consists of a position, rotation, type, and color
		public hexLocus locus;
		public int rotation;
		public int type;
		public int color;

		// constructor simply 
		public tileData (hexLocus inLocus, int inRotation, int inType, int inColor)
		{
			locus = inLocus;
			rotation = inRotation;
			type = inType;
			color = inColor;
		}

		// serialize turns this tileData into strings separated by spaces
		public string serialize ()
		{
			string s = type.ToString();
			s += " " + color.ToString();
			s += " " + locus.a.ToString();
			s += " " + locus.b.ToString();
			s += " " + locus.c.ToString();
			s += " " + locus.d.ToString();
			s += " " + locus.e.ToString();
			s += " " + locus.f.ToString();
			s += " " + rotation.ToString();
			return s;
		}
	}
	
	// hexLocus describes a location in a hexagonal coodinate system
	public struct hexLocus {

		//turns out this bugger is pretty dang useful
		private static readonly float sqrt3 = (float)Math.Sqrt(3f);

		public int a;
		public int b;
		public int c;
		public int d;
		public int e;
		public int f;

		// operators allow hexLocus to be added and subtracted like vectors
		// since they are static, coordinate simplification is ensured by the constructor
		public static hexLocus operator +(hexLocus h1, hexLocus h2)
		{
			// + operator adds each component together
			int rA, rB, rC, rD, rE, rF;
			rA = h1.a + h2.a;
			rB = h1.b + h2.b;
			rC = h1.c + h2.c;
			rD = h1.d + h2.d;
			rE = h1.e + h2.e;
			rF = h1.f + h2.f;
			return new hexLocus(rA, rB, rC, rD, rE, rF);
		}
		public static hexLocus operator -(hexLocus h1, hexLocus h2)
		{
			// - operator subtracts each component in the second locus from the first locus
			int rA, rB, rC, rD, rE, rF;
			rA = h1.a - h2.a;
			rB = h1.b - h2.b;
			rC = h1.c - h2.c;
			rD = h1.d - h2.d;
			rE = h1.e - h2.e;
			rF = h1.f - h2.f;
			return new hexLocus(rA, rB, rC, rD, rE, rF);
		}
		public static hexLocus operator *(hexLocus h1, hexLocus h2)
		{
			// * operator multiplies each component together
			int rA, rB, rC, rD, rE, rF;
			rA = h1.a * h2.a;
			rB = h1.b * h2.b;
			rC = h1.c * h2.c;
			rD = h1.d * h2.d;
			rE = h1.e * h2.e;
			rF = h1.f * h2.f;
			return new hexLocus(rA, rB, rC, rD, rE, rF);
		}
		public static hexLocus operator /(hexLocus h1, hexLocus h2)
		{
			// / operator divides each component in the second locus from the first locus
			int rA, rB, rC, rD, rE, rF;
			rA = h1.a / h2.a;
			rB = h1.b / h2.b;
			rC = h1.c / h2.c;
			rD = h1.d / h2.d;
			rE = h1.e / h2.e;
			rF = h1.f / h2.f;
			return new hexLocus(rA, rB, rC, rD, rE, rF);
		}

		// constructor generates a set of coordinates from a given Vector3
		public hexLocus (Vector3 inVec3)
		{
			inVec3 = new Vector3(inVec3.x, inVec3.y, 0.0f);
			hexLocus h1, h2, h3, h4;

			float delta = inVec3.y / sqrt3;
			// h1 finds an (a, c) point
			h1 = new hexLocus((int)Math.Round(inVec3.x + delta), 0, (int)Math.Round(delta * 2.0f), 0, 0, 0);
			// h2 finds an (a, e) point
			h2 = new hexLocus((int)Math.Round(inVec3.x - delta), 0, 0, 0, (int)Math.Round(delta * -2.0f), 0);

			delta = inVec3.x / sqrt3;
			// h3 finds a (b, f) point
			h3 = new hexLocus(0, (int)Math.Round(delta * 2.0f), 0, 0, 0, (int)Math.Round(delta - inVec3.y));
			// h4 finds a (d, f) point
			h4 = new hexLocus(0, 0, 0, (int)Math.Round(delta * -2.0f), 0, (int)Math.Round(-(delta + inVec3.y)));

			float m1, m2, m3, m4;
			m1 = (h1.toUnitySpace() - inVec3).magnitude;
			m2 = (h2.toUnitySpace() - inVec3).magnitude;
			m3 = (h3.toUnitySpace() - inVec3).magnitude;
			m4 = (h4.toUnitySpace() - inVec3).magnitude;

			if ((m1 < m2) && (m1 < m3) && (m1 < m4)) this = h1;
			else if ((m2 < m3) && (m2 < m4)) this = h2;
			else if (m3 < 4) this = h3;
			else this = h4;
		}

		// constructor simply passes the given coordinates into cSimplify
		public hexLocus (int inA, int inB, int inC, int inD, int inE, int inF)
		{
			a = inA;
			b = inB;
			c = inC;
			d = inD;
			e = inE;
			f = inF;

			cSimplify();
		}

		// method simplifies current coordinates to simplest terms
		// this method should be called every time internal values are changed
		private void cSimplify ()
		{
			// copies the input variables
			int inA = a, inB = b, inC = c, inD = d, inE = e, inF = f;
			// dS is the total delta shift to be added or subtracted from each coordinate within a triad
			// i is a temporary value used to store the number of positive/negative coordinates within each triad
			int dS = 0, i = 0;
			// p and n arrays articulate which values are positive and negative for each triad
			bool[] p1 = new bool[]{inA > 0, inC > 0, inE > 0};
			bool[] n1 = new bool[]{inA < 0, inC < 0, inE < 0};
			bool[] p2 = new bool[]{inB > 0, inD > 0, inF > 0};
			bool[] n2 = new bool[]{inB < 0, inD < 0, inF < 0};

			// the first case addresses multiple positive values in the first triad
			foreach (bool bN in p1) if (bN) i++;
			if (i > 1) {
				if (p1[0] && p1[1] && p1[2]) {
					// if all values are positive, d is set to the middle value
					dS = inA;
					dS = ((dS >= inC) ^ (dS >= inE)) ? dS : inC;
					dS = ((dS >= inA) ^ (dS >= inE)) ? dS : inE;
				} else {
					// if only two values are positive, d is set to the smaller positive value
					int t1, t2;
					t1 = p1[0] ? inA : inC;
					t2 = p1[2] ? inE : inC;

					dS = (t1 < t2) ? t1 : t2;
				}
			}

			// the second case addresses multiple negative values in the first triad
			i = 0;
			foreach (bool bN in n1) if (bN) i++;
			if (i > 1) {
				if (n1[0] && n1[1] && n1[2]) {
					// if all values are negative, d is set to the middle value
					dS = inA;
					dS = ((dS <= inC) ^ (dS <= inE)) ? dS : inC;
					dS = ((dS <= inA) ^ (dS <= inE)) ? dS : inE;
				} else {
					// if only two values are negative, d is set to the smaller negative value
					int t1, t2;
					t1 = n1[0] ? inA : inC;
					t2 = n1[2] ? inE : inC;
					
					dS = (t1 > t2) ? t1 : t2;
				}
			}

			// delta shift is applied to the first triad
			a = inA - dS;
			c = inC - dS;
			e = inE - dS;

			// the third case addresses multiple positive values in the second triad
			dS = 0;
			i = 0;
			foreach (bool bN in p2) if (bN) i++;
			if (i > 1) {
				if (p2[0] && p2[1] && p2[2]) {
					// if all values are positive, d is set to the middle value
					dS = inB;
					dS = ((dS >= inD) ^ (dS >= inF)) ? dS : inD;
					dS = ((dS >= inB) ^ (dS >= inF)) ? dS : inF;
				} else {
					// if only two values are positive, d is set to the smaller positive value
					int t1, t2;
					t1 = p2[0] ? inB : inD;
					t2 = p2[2] ? inF : inD;

					dS = (t1 < t2) ? t1 : t2;
				}
			}

			// the fourth case addresses multiple negative values in the second triad
			i = 0;
			foreach (bool bN in n2) if (bN) i++;
			if (i > 1) {
				if (n2[0] && n2[1] && n2[2]) {
					// if all values are negative, d is set to the middle value
					dS = inB;
					dS = ((dS <= inD) ^ (dS <= inF)) ? dS : inD;
					dS = ((dS <= inB) ^ (dS <= inF)) ? dS : inF;
				} else {
					// if only two values are negative, d is set to the smaller negative value
					int t1, t2;
					t1 = n2[0] ? inB : inD;
					t2 = n2[2] ? inF : inD;
					
					dS = (t1 > t2) ? t1 : t2;
				}
			}

			// delta shift is applied to the second triad
			b = inB - dS;
			d = inD - dS;
			f = inF - dS;
		}

		// translates a discrete hexAxis location into a Unity world space location
		public Vector3 toUnitySpace ()
		{
			// x and y are calculated from basic 30-60-90 trigonometry
			float x, y;

			x = a;
			x += ((sqrt3 / 2f) * b);
			x -= (0.5f * c);
			x -= ((sqrt3 / 2f) * d);
			x -= (0.5f * e);

			y = (-1f * f);
			y += (0.5f * b);
			y += ((sqrt3 / 2f) * c);
			y += (0.5f * d);
			y -= ((sqrt3 / 2f) * e);

			return new Vector3(x, y, 0f);
		}
	}
}