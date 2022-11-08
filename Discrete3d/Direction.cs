using System.Collections.Generic;
using UnityEngine;


namespace RikusGameDevToolbox.Discrete3d
{
	/// <summary>
	/// This class represents a direction in 3D space.
	/// It has 26 possible values (North, Back & left, Up, North & up etc.)
	/// North is along positive Z axis
	/// East is along positive X axis.
	/// Up is along positive Y axis.
	/// </summary>
	
	[System.Serializable]
    public struct Direction
    {
        static DirectionData[] directions =
        {
        new DirectionData( "Forward",             new Vec(0,   0,  1), new Direction(0),  Quaternion.Euler(0f, 0f, 0f),   4 ),
        new DirectionData( "Forward and right",         new Vec(1,   0,  1), new Direction(1),  Quaternion.Euler(0f, 45f, 0f),  5 ),
        new DirectionData( "Right",              new Vec(1,   0,  0), new Direction(2),  Quaternion.Euler(0f, 90f, 0f),  6 ),
        new DirectionData( "Back and right",         new Vec(1,   0, -1), new Direction(3),  Quaternion.Euler(0f, 135f, 0f), 7 ),
        new DirectionData( "Back",             new Vec(0,   0, -1), new Direction(4),  Quaternion.Euler(0f, 180f, 0f), 0 ),
        new DirectionData( "Back and left",         new Vec(-1,  0, -1), new Direction(5),  Quaternion.Euler(0f, 225f, 0f), 1 ),
        new DirectionData( "Left",              new Vec(-1,  0,  0), new Direction(6),  Quaternion.Euler(0f, 270f, 0f), 2 ),
        new DirectionData( "Forward and left",         new Vec(-1,  0,  1), new Direction(7),  Quaternion.Euler(0f, 315f, 0f), 3 ),

        new DirectionData( "Forward and up",      new Vec(0,   1,  1), new Direction(8),  Quaternion.Euler(0f, 0f, 0f),   20 ),
        new DirectionData( "Forward, right and up",  new Vec(1,   1,  1), new Direction(9),  Quaternion.Euler(0f, 45f, 0f),  21 ),
        new DirectionData( "Right and up",       new Vec(1,   1,  0), new Direction(10), Quaternion.Euler(0f, 90f, 0f),  22 ),
        new DirectionData( "Back, right and up",  new Vec(1,   1, -1), new Direction(11), Quaternion.Euler(0f, 135f, 0f), 23 ),
        new DirectionData( "Back and up",      new Vec(0,   1, -1), new Direction(12), Quaternion.Euler(0f, 180f, 0f), 16 ),
        new DirectionData( "Back, left and up",  new Vec(-1,  1, -1), new Direction(13), Quaternion.Euler(0f, 225f, 0f), 17 ),
        new DirectionData( "Left and up" ,      new Vec(-1,  1,  0), new Direction(14), Quaternion.Euler(0f, 270f, 0f), 18 ),
        new DirectionData( "Forward, left and up",  new Vec(-1,  1,  1), new Direction(15), Quaternion.Euler(0f, 315f, 0f), 19 ),

        new DirectionData( "Forward and down",    new Vec(0,  -1,  1), new Direction(16), Quaternion.Euler(0f, 0f, 0f),   12 ),
        new DirectionData( "Forward, right and down",new Vec(1,  -1,  1), new Direction(17), Quaternion.Euler(0f, 45f, 0f),  13 ),
        new DirectionData( "Right and down",     new Vec(1,  -1,  0), new Direction(18), Quaternion.Euler(0f, 90f, 0f),  14 ),
        new DirectionData( "Back, right and down",new Vec(1,  -1, -1), new Direction(19), Quaternion.Euler(0f, 135f, 0f), 15 ),
        new DirectionData( "Back and down",    new Vec(0,  -1, -1), new Direction(20), Quaternion.Euler(0f, 180f, 0f), 8 ),
        new DirectionData( "Back, left and down",new Vec(-1, -1, -1), new Direction(21), Quaternion.Euler(0f, 225f, 0f), 9 ),
        new DirectionData( "Left and down",     new Vec(-1, -1,  0), new Direction(22), Quaternion.Euler(0f, 270f, 0f), 10 ),
        new DirectionData( "Forward, left and down",new Vec(-1, -1,  1), new Direction(23), Quaternion.Euler(0f, 315f, 0f), 11 ),

        new DirectionData( "Up",                new Vec(0,   1,  0), new Direction(24), Quaternion.Euler(0f, 0f, 0f), 25 ),
        new DirectionData( "Down",              new Vec(0,  -1,  0), new Direction(25), Quaternion.Euler(0f, 0f, 0f), 24 )
		};

        static public readonly Direction forward = directions[0].direction;
		static public readonly Direction forward_right = directions[1].direction;
		static public readonly Direction right = directions[2].direction;
		static public readonly Direction back_right = directions[3].direction;
		static public readonly Direction back = directions[4].direction;
		static public readonly Direction back_left = directions[5].direction;
		static public readonly Direction left = directions[6].direction;
		static public readonly Direction forward_left = directions[7].direction;

		static public readonly Direction forward_up = directions[8].direction;
		static public readonly Direction forward_right_up = directions[9].direction;
		static public readonly Direction right_up = directions[10].direction;
		static public readonly Direction back_right_up = directions[11].direction;
		static public readonly Direction back_up = directions[12].direction;
		static public readonly Direction back_left_up = directions[13].direction;
		static public readonly Direction left_up = directions[14].direction;
		static public readonly Direction forward_left_up = directions[15].direction;

		static public readonly Direction forward_down = directions[16].direction;
		static public readonly Direction forward_right_down = directions[17].direction;
		static public readonly Direction right_down = directions[18].direction;
		static public readonly Direction back_right_down = directions[19].direction;
		static public readonly Direction back_down = directions[20].direction;
		static public readonly Direction back_left_down = directions[21].direction;
		static public readonly Direction left_down = directions[22].direction;
		static public readonly Direction forward_left_down = directions[23].direction;

		static public readonly Direction up = directions[24].direction;
		static public readonly Direction down = directions[25].direction;

		struct DirectionData
		{
			public readonly string name;
			public readonly Vec vec;
			public readonly Direction direction;
			public readonly Quaternion rotation;
			public readonly int oppositeIndex;

			public DirectionData(string name, Vec vec, Direction direction, Quaternion rotation, int oppositeIndex)
			{ this.name = name; this.vec = vec; this.direction = direction; this.rotation = rotation; this.oppositeIndex = oppositeIndex; }
		}




		// Lists that user can use to iterate directions
		static public IReadOnlyList<Direction> cardinal = new List<Direction>() { forward, right, left, back };
		static public IReadOnlyList<Direction> compass = new Direction[] { forward, forward_right, right, back_right, back, back_left, left, forward_left };
		static public IReadOnlyList<Direction> compass_up = new Direction[] { forward_up, forward_right_up, right_up, back_right_up, back_up, back_left_up, left_up, forward_left_up };
		static public IReadOnlyList<Direction> compass_down = new Direction[] { forward_down, forward_right_down, right_down, back_right_down, back_down, back_left_down, left_down, forward_left_down };
		static public IReadOnlyList<Direction> vertical = new Direction[] { up, down };
		static public IReadOnlyList<Direction> cardinalAndVertical = new Direction[] { forward, right, left, back, up, down };
		static public IReadOnlyList<Direction> all = new Direction[] { forward, forward_right, right, back_right, back, back_left, left, forward_left,
												   forward_up, forward_right_up, right_up, back_right_up, back_up, back_left_up, left_up, forward_left_up,
												   forward_down, forward_right_down, right_down, back_right_down, back_down, back_left_down, left_down, forward_left_down,
												   up, down };

		/// <summary>
		/// Returns the direction as Vec
		/// </summary>
		public Vec asVec => directions[directionIdx].vec; 


		// Index of the direction this object represents
		private int directionIdx;

		public static bool operator ==(Direction d1, Direction d2)
		{
			return d1.directionIdx == d2.directionIdx;
		}
		public static bool operator !=(Direction d1, Direction d2)
		{
			return d1.directionIdx != d2.directionIdx;
		}

		public static Direction FromVec(Vec vec)
		{
			foreach (DirectionData dd in directions)
			{
				if (dd.vec == vec)
				{
					return dd.direction;
				}
			}

			throw new System.Exception("Trying to generate Direction from invalid Vec.");

		}

		public static Direction FromVector3(Vector3 vector)
		{
			Vec vec = new Vec(
								Mathf.RoundToInt(vector.x),
								Mathf.RoundToInt(vector.y),
								Mathf.RoundToInt(vector.z));

			return FromVec(vec);
		}

		static public Direction RandomCardinalDirection()
		{
			return cardinal[Random.Range(0, 4)];
		}

		static public Direction RandomCardinalAndVerticalDirection()
		{
			return cardinalAndVertical[Random.Range(0, 6)];
		}

		static public Direction RandomCompassDirection()
		{
			return compass[Random.Range(0, 8)];
		}



		public override bool Equals(object obj)
		{
			return obj is Direction && directionIdx == ((Direction)obj).directionIdx;
		}
		public override int GetHashCode()
		{
			return directionIdx;
		}

		/// <summary>
		/// Constructor is private, because user is expected to create a new Direction
		/// by copying from the static members. (example: Direction d = Direction.south;)
		/// </summary>
		private Direction(int directionIdx)
		{
			this.directionIdx = directionIdx;
		}

		override public string ToString()
		{
			return "Direction : " + directions[directionIdx].name;
		}

		public bool IsCardinalDirection()
		{
			return (directionIdx == 0 || directionIdx == 2 || directionIdx == 4 || directionIdx == 6);
		}

		public bool IsCardinalOrVerticalDirection()
		{
			return (directionIdx == 0 || directionIdx == 2 || directionIdx == 4 || directionIdx == 6 || directionIdx == 24 || directionIdx == 25);
		}

		public bool IsCompassDirection()
		{
			if (directionIdx < 8)
				return true;
			return false;
		}

		public Vec VecForward()
		{
			return asVec;
		}

		public Vec VecRight()
		{
			return Rotate(2).asVec;
		}


		public Direction Right90Deg(int numTimes = 1)
		{
			return Rotate(numTimes * 2);
		}

		public Direction Left90Deg(int numTimes = 1)
		{
			return Rotate(-numTimes * 2);
		}

		public Direction Right45Deg(int numTimes = 1)
		{
			return Rotate(numTimes);
		}

		public Direction Left45Deg(int numTimes = 1)
		{
			return Rotate(-numTimes);
		}

		/// <summary>
		/// Rotates direction 180 around vertical axis (keeping the vertical component of the direction same).
		/// </summary>
		public Direction Behind()
		{
			return Rotate(4);
		}

		public Direction Opposite()
		{
			int index = directions[directionIdx].oppositeIndex;
			return directions[index].direction;
		}

		/// <summary>
		/// Returns the direction as rotation around the y-axis (vertical component is disregarded)
		/// </summary>
		public Quaternion ToRotation()
		{
			return directions[directionIdx].rotation;
		}


		/// <summary>
		/// Returns a direction that is rotated given amount of 45 deg steps around the y-axis.
		/// Clockwise is positive.
		/// </summary>
		private Direction Rotate(int steps)
		{
			if (this == up) return up;
			if (this == down) return down;


			int adjustedSteps = steps % 8;
			if (adjustedSteps < 0)
				adjustedSteps += 8;

			int idx = directionIdx + adjustedSteps;


			if (directionIdx <= 7)
			{
				if (idx > 7)
					idx -= 8;
			}
			else if (directionIdx <= 15)
			{
				if (idx > 15)
					idx -= 8;
			}
			else if (directionIdx <= 23)
			{
				if (idx > 23)
					idx -= 8;
			}

			return directions[idx].direction;


		}


	}

}