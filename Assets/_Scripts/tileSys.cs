using UnityEngine;
using System.Collections;

namespace tileSys {

//	public enum R3shift : byte {
//
//		plusX = 0x01,
//		minusX = 0x02,
//		plusY = 0x04,
//		minusY = 0x08
//	}
//
//	struct tileCoordinate {
//
//		public int X;
//		public int Y;
//		public R3shift delta;
//
//		public const float deltaShift = (Mathf.Sqrt(3f) * 2) / 3;
//
//		public tileCoordinate(int inX, int inY, R3shift inShift) {
//			X = inX;
//			Y = inY;
//
//			if ((byte)inShift >= 0x10) {
//				delta = R3shift.None;
//				return;
//			}
//
//			R3shift returnShift = inShift;
//			// bitwise 0011
//			R3shift splitX = R3shift.plusX | R3shift.minusX;
//			// bitwise 1100
//			R3shift splitY = R3shift.plusY | R3shift.minusY;
//
//			// bitwise reduction of xx11 to xx00
//			if ((inShift & splitX) == splitX) returnShift = (returnShift & splitY);
//			// bitwise reduction of 11xx to 00xx
//			if ((inShift & splitY) == splitY) returnShift = (returnShift & splitX);
//
//			delta = rtrnShift;
//		}
//
//		public tileCoordinate(Vector2 inVec2) {
//			int inX = Mathf.FloorToInt(inVec2.x);
//			int inY = Mathf.FloorToInt(inVec2.y);
//
//			float checkLow = (float)inX;
//			float checkHigh = inX + (deltaShift - 1);
//			
//		}
//
//		public Vector2 toUnitySpace() {
//			Vector2 rtrnVec2 = new Vector2();
//
//			rtrnVec2.x += X;
//			rtrnVec2.y += Y;
//
//			if ((R3shift.plusX & delta) == R3shift.plusX) rtrnVec2.x += deltaShift;
//			if ((R3shift.minusX & delta) == R3shift.minusX) rtrnVec2.x -= deltaShift;
//			if ((R3shift.plusY & delta) == R3shift.plusY) rtrnVec2.y += deltaShift;
//			if ((R3shift.minusY & delta) == R3shift.minusY) rtrnVec2.y -= deltaShift;
//		}
//	}
}
