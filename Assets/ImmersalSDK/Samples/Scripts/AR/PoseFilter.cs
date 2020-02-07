/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of Immersal SDK v1.3.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;

namespace Immersal.AR
{
	public class PoseFilter
	{
		public Vector3 position = Vector3.zero;
		public Quaternion rotation = Quaternion.identity;

		private static uint m_HistorySize = 8;
		private Vector3[] m_P = new Vector3[m_HistorySize];
		private Vector3[] m_X = new Vector3[m_HistorySize];
		private Vector3[] m_Z = new Vector3[m_HistorySize];
		private uint m_Samples = 0;

		public uint SampleCount()
		{
			return m_Samples;
		}

		public void InvalidateHistory()
		{
			m_Samples = 0;
		}

		public void ResetFiltering()
		{
			position = Vector3.zero;
			rotation = Quaternion.identity;
			InvalidateHistory();
		}

		public void RefinePose(Matrix4x4 r)
		{
			uint idx = m_Samples% m_HistorySize;
			m_P[idx] = r.GetColumn(3);
			m_X[idx] = r.GetColumn(0);
			m_Z[idx] = r.GetColumn(2);
			m_Samples++;
			uint n = m_Samples > m_HistorySize ? m_HistorySize : m_Samples;
			position = FilterAVT(m_P, n);
			Vector3 x = Vector3.Normalize(FilterAVT(m_X, n));
			Vector3 z = Vector3.Normalize(FilterAVT(m_Z, n));
			Vector3 up = Vector3.Normalize(Vector3.Cross(z, x));
			rotation = Quaternion.LookRotation(z, up);
		}

		private Vector3 FilterAVT(Vector3[] buf, uint n)
		{
			Vector3 mean = Vector3.zero;
			for (uint i = 0; i < n; i++)
				mean += buf[i];
			mean /= (float)n;
			if (n <= 2)
				return mean;
			float s = 0;
			for (uint i = 0; i < n; i++)
			{
				s += Vector3.SqrMagnitude(buf[i] - mean);
			}
			s /= (float)n;
			Vector3 avg = Vector3.zero;
			int ib = 0;
			for (uint i = 0; i < n; i++)
			{
				float d = Vector3.SqrMagnitude(buf[i] - mean);
				if (d <= s)
				{
					avg += buf[i];
					ib++;
				}
			}
			if (ib > 0)
			{
				avg /= (float)ib;
				return avg;
			}
			return mean;
		}
	}

}
