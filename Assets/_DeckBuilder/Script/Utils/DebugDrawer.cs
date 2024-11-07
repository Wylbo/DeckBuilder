using UnityEngine;

namespace MG.Extend
{
	public static class DebugDrawer
	{
		private static readonly Vector4[] s_UnitSphere = MakeUnitSphere(16);

		private const float dTime = 0.016667f;
		private static Vector4[] MakeUnitSphere(int len)
		{
			Debug.Assert(len > 2);
			var v = new Vector4[len * 3];
			for (int i = 0; i < len; i++)
			{
				var f = i / (float)len;
				float c = Mathf.Cos(f * (float)(Mathf.PI * 2.0));
				float s = Mathf.Sin(f * (float)(Mathf.PI * 2.0));
				v[0 * len + i] = new Vector4(c, s, 0, 1);
				v[1 * len + i] = new Vector4(0, c, s, 1);
				v[2 * len + i] = new Vector4(s, 0, c, 1);
			}
			return v;
		}

		private static readonly Vector4[] s_UnitCube =
		{
			new Vector4(-0.5f,  0.5f, -0.5f, 1),
			new Vector4(0.5f,  0.5f, -0.5f, 1),
			new Vector4(0.5f, -0.5f, -0.5f, 1),
			new Vector4(-0.5f, -0.5f, -0.5f, 1),

			new Vector4(-0.5f,  0.5f,  0.5f, 1),
			new Vector4(0.5f,  0.5f,  0.5f, 1),
			new Vector4(0.5f, -0.5f,  0.5f, 1),
			new Vector4(-0.5f, -0.5f,  0.5f, 1)
		};

		public static void DrawSphere(Vector4 pos, float radius, Color color, float duration = dTime)
		{
			Vector4[] v = s_UnitSphere;
			int len = s_UnitSphere.Length / 3;
			for (int i = 0; i < len; i++)
			{
				var sX = pos + radius * v[0 * len + i];
				var eX = pos + radius * v[0 * len + (i + 1) % len];
				var sY = pos + radius * v[1 * len + i];
				var eY = pos + radius * v[1 * len + (i + 1) % len];
				var sZ = pos + radius * v[2 * len + i];
				var eZ = pos + radius * v[2 * len + (i + 1) % len];
				Debug.DrawLine(sX, eX, color, duration);
				Debug.DrawLine(sY, eY, color, duration);
				Debug.DrawLine(sZ, eZ, color, duration);
			}
		}

		public static void DrawCapusle(Vector4 pos, float radius, float height, Color color, float duration = dTime)
		{
			DrawSphere(pos - (Vector4)Vector3.up * height / 4, radius, color, duration);
			DrawSphere(pos + (Vector4)Vector3.up * height / 4, radius, color, duration);
		}

		public static void DrawBox(Vector4 pos, Vector3 size, Color color, float duration = dTime)
		{
			Vector4[] v = s_UnitCube;
			Vector4 sz = new Vector4(size.x, size.y, size.z, 1);
			for (int i = 0; i < 4; i++)
			{
				var s = pos + Vector4.Scale(v[i], sz);
				var e = pos + Vector4.Scale(v[(i + 1) % 4], sz);
				Debug.DrawLine(s, e, color, duration);
			}
			for (int i = 0; i < 4; i++)
			{
				var s = pos + Vector4.Scale(v[4 + i], sz);
				var e = pos + Vector4.Scale(v[4 + ((i + 1) % 4)], sz);
				Debug.DrawLine(s, e, color, duration);
			}
			for (int i = 0; i < 4; i++)
			{
				var s = pos + Vector4.Scale(v[i], sz);
				var e = pos + Vector4.Scale(v[i + 4], sz);
				Debug.DrawLine(s, e, color, duration);
			}
		}

		public static void DrawBox(Matrix4x4 transform, Color color, float duration = dTime)
		{
			Vector4[] v = s_UnitCube;
			Matrix4x4 m = transform;
			for (int i = 0; i < 4; i++)
			{
				var s = m * v[i];
				var e = m * v[(i + 1) % 4];
				Debug.DrawLine(s, e, color, duration);
			}
			for (int i = 0; i < 4; i++)
			{
				var s = m * v[4 + i];
				var e = m * v[4 + ((i + 1) % 4)];
				Debug.DrawLine(s, e, color, duration);
			}
			for (int i = 0; i < 4; i++)
			{
				var s = m * v[i];
				var e = m * v[i + 4];
				Debug.DrawLine(s, e, color, duration);
			}
		}
	}
}
