/******************************************************************************************************************************************************
* MIT License																																		  *
*																																					  *
* Copyright (c) 2020																																  *
* Emmanuel Badier <emmanuel.badier@gmail.com>																										  *
* 																																					  *
* Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),  *
* to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,  *
* and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:		  *
* 																																					  *
* The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.					  *
* 																																					  *
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, *
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 																							  *
* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 		  *
* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.							  *
******************************************************************************************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace HeatMap2D
{
	/// <summary>
	/// A test script to show the use of HeatMap2D to render unlimited number of points.
	/// </summary>
	public sealed class HeatMap2D_InfiniteTrajectoryTest : MonoBehaviour
	{
		[Header("HeatMap2D Settings")]
		public HeatMap2D heatmap;
		[Range(0.001f, 1.0f)]
		[Tooltip("Points' radius")]
		public float radius = 0.005f;
		[Range(0.001f, 1.0f)]
		[Tooltip("Color intensity")]
		public float intensity = 0.1f;
		[Range(0, 100000)]
		[Tooltip("The number of points in the trajectory")]
		public int pointsCount = HeatMap2D.MAX_POINTS_COUNT;
		private int _pointsCount = HeatMap2D.MAX_POINTS_COUNT;

		private List<Vector4> _points = new List<Vector4>(); // raw points.
		private List<Vector4> _meaningPoints = new List<Vector4>(); // points obtained after raw points reduction.

		void Start()
		{
			_GeneratePoints();
		}

		private void Update()
		{
			if (heatmap.Radius != radius)
			{
				heatmap.Radius = radius;
			}

			if (heatmap.Intensity != intensity)
			{
				heatmap.Intensity = intensity;
			}

			if(pointsCount != _pointsCount)
			{
				_pointsCount = pointsCount;
				_GeneratePoints();
			}
		}

		private void _GeneratePoints()
		{
			// Generate points.
			_points.Clear();
			float scaleFactor = (float)HeatMap2D.MAX_POINTS_COUNT / (float)_pointsCount;
			float length = 0.0f, lengthStep = 0.0005f * scaleFactor;
			float angleRad = 0.0f, angleRadStep = Mathf.Deg2Rad * 0.5f;
			Vector4 point = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
			Vector3 dir = new Vector3(0.0f, 0.0f, 1.0f);
			while (_points.Count < _pointsCount)
			{
				// Add point.
				_points.Add(point);
				// Compute next point.
				angleRad += angleRadStep;
				length += lengthStep;
				dir.Set(Mathf.Cos(angleRad), 0.0f, Mathf.Sin(angleRad));
				point = dir.normalized * length;
				point.w = 1.0f;
			}

			// Reduce points if needed.
			if (_points.Count > HeatMap2D.MAX_POINTS_COUNT)
			{
				// Points reduction needed.
				_meaningPoints = HeatMap2D.Decimation(_points, HeatMap2D.MAX_POINTS_COUNT);
				heatmap.SetPoints(_meaningPoints);
				Debug.Log("[HeatMap2D_InfiniteTrajectoryTest] " + _points.Count + " points rendered using " + _meaningPoints.Count + " meaningful points.");
			}
			else
			{
				// Points reduction not needed.
				heatmap.SetPoints(_points);
				Debug.Log("[HeatMap2D_InfiniteTrajectoryTest] " + _points.Count + " points rendered.");
			}
		}
	}
}