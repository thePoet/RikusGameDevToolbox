using UnityEngine;

namespace RikusGameDevToolbox.Discrete3d
{
	/// <summary>
	/// This class represents a rotation around three axis in that is limited
	/// to orthogonal values. It has 24 possible values. (Think of a dice put in a 
	/// tight box.)
	/// </summary>
	
	public class GridOrientation
	{
		public static readonly GridOrientation[] all;

		public static readonly GridOrientation forward;
		public static readonly GridOrientation forward_1;
		public static readonly GridOrientation forward_2;
		public static readonly GridOrientation forward_3;

		public static readonly GridOrientation right;
		public static readonly GridOrientation right_1;
		public static readonly GridOrientation right_2;
		public static readonly GridOrientation right_3;

		public static readonly GridOrientation back;
		public static readonly GridOrientation back_1;
		public static readonly GridOrientation back_2;
		public static readonly GridOrientation back_3;

		public static readonly GridOrientation left;
		public static readonly GridOrientation left_1;
		public static readonly GridOrientation left_2;
		public static readonly GridOrientation left_3;

		public static readonly GridOrientation up;
		public static readonly GridOrientation up_1;
		public static readonly GridOrientation up_2;
		public static readonly GridOrientation up_3;

		public static readonly GridOrientation down;
		public static readonly GridOrientation down_1;
		public static readonly GridOrientation down_2;
		public static readonly GridOrientation down_3;


		static GridOrientation()
		{
			all = new GridOrientation[24];

			Vector3[] directions = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left, Vector3.up, Vector3.down };
			string[] dirString = { "Forward", "Right", "Back", "Left", "Up", "Down" };

			for (int d = 0; d < directions.Length; d++)
			{
				for (int r = 0; r < 4; r++)
				{
					int index = d * 4 + r;

					Quaternion quaternion = Quaternion.LookRotation(directions[d]) * Quaternion.Euler(0f, 0f, 90f * r);
					string description = "Pointing: " + dirString + " rotated " + r + " steps.";

					all[index] = new GridOrientation(index, description, quaternion);
				}
			}


			forward = all[0];
			forward_1 = all[1];
			forward_2 = all[2];
			forward_3 = all[3];

			right = all[4];
			right_1 = all[5];
			right_2 = all[6];
			right_3 = all[7];

			back = all[8];
			back_1 = all[9];
			back_2 = all[10];
			back_3 = all[11];

			left = all[12];
			left_1 = all[13];
			left_2 = all[14];
			left_3 = all[15];

			up = all[16];
			up_1 = all[17];
			up_2 = all[18];
			up_3 = all[19];

			down = all[20];
			down_1 = all[21];
			down_2 = all[22];
			down_3 = all[23];


		}

		public readonly int id;
		public readonly string asString;
		public readonly Quaternion asQuaternion;


		public static GridOrientation GetRandom()
		{
			return all[Random.Range(0, 24)];
		}

		private GridOrientation(int id, string name, Quaternion quaternion)
		{
			this.id = id;
			asString = name;
			asQuaternion = quaternion;
		}
	}
}