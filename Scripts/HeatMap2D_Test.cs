/******************************************************************************************************************************************************
* MIT License																																		  *
*																																					  *
* Copyright (c) 2020																																  *
* Emmanuel Badier <emmanuel.badier@gmail.com>																										  *
* 																																					  *
* Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),  *
* to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,  *
* and/or sell copies of the Software, and to permit persons to whom the Software isfurnished to do so, subject to the following conditions:			  *
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
	/// A test script to show the use of HeatMap2D functions.
	/// This test doesn't allow you to generate more than HeatMap2D.MAX_POINTS_COUNT points.
	/// This is mandatory to explore all test-cases correctly.
	/// </summary>
	public sealed class HeatMap2D_Test : MonoBehaviour
	{
		public enum GenerationMethod { Random = 0, Trajectory }
		public enum ReductionMethod { None = 0, CanopyClustering, AverageClustering }

		[Header("HeatMap2D Settings")]
		public HeatMap2D heatmap;
		[Range(0.01f, 1.0f)]
		[Tooltip("Points' radius")]
		public float radius = 0.1f;
		[Range(0.01f, 1.0f)]
		[Tooltip("Color intensity")]
		public float intensity = 0.15f;
		[Header("Points Generation")]
		[Tooltip("In Random mode, you can add points using Mouse clicks.")]
		public GenerationMethod generationMethod;
		private GenerationMethod _generationMethod;
		[Range(0, HeatMap2D.MAX_POINTS_COUNT)]
		[Tooltip("The number of points to generate.")]
		public int pointsCount = HeatMap2D.MAX_POINTS_COUNT;
		private int _pointsCount = HeatMap2D.MAX_POINTS_COUNT;
		[Header("Points Reduction")]
		[Tooltip("To reduce points in trajectories, use AverageClustering. To reduce randomly generated points, use CanopyClustering.")]
		public ReductionMethod reductionMethod;
		private ReductionMethod _reductionMethod;
		[Range(0.01f, 1.0f)]
		[Tooltip("The maximum distance between two points to average them using CanopyClustering.")]
		public float canopyClusteringMaxDistance = 0.033f;
		private float _canopyClusteringMaxDistance = 0.033f;
		[Range(0, HeatMap2D.MAX_POINTS_COUNT)]
		[Tooltip("The maximum number of points to produce using AverageClustering.")]
		public int averageClusteringMaxPointsCount = 300;
		private int _averageClusteringMaxPointsCount = 300;

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

			if (generationMethod != _generationMethod)
			{
				_generationMethod = generationMethod;
				_reductionMethod = reductionMethod = ReductionMethod.None;
				_GeneratePoints();
			}

			if(pointsCount != _pointsCount)
			{
				_pointsCount = pointsCount;
				_reductionMethod = reductionMethod = ReductionMethod.None;
				_GeneratePoints();
			}

			if (_reductionMethod != reductionMethod)
			{
				_reductionMethod = reductionMethod;
				_SetPoints();
			}

			if (canopyClusteringMaxDistance != _canopyClusteringMaxDistance)
			{
				_canopyClusteringMaxDistance = canopyClusteringMaxDistance;
				if(_reductionMethod == ReductionMethod.CanopyClustering)
				{
					_SetPoints();
				}
			}

			if (averageClusteringMaxPointsCount != _averageClusteringMaxPointsCount)
			{
				_averageClusteringMaxPointsCount = averageClusteringMaxPointsCount;
				if(_reductionMethod == ReductionMethod.AverageClustering)
				{
					_SetPoints();
				}
			}
		}

		private void OnMouseDown()
		{
			// Can only add random points in this mode.
			if(_generationMethod == GenerationMethod.Random)
			{
				RaycastHit hit;
				if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
				{
					if (_points.Count < HeatMap2D.MAX_POINTS_COUNT)
					{
						//Debug.Log("Hit " + hit.transform.name);
						//GameObject debug = GameObject.CreatePrimitive(PrimitiveType.Cube);
						//debug.transform.position = hit.point;
						Vector4 point = hit.point;
						point.w = 1.0f;
						_points.Add(point);
						Debug.Log("[HeatMap2D_Test] point successfully added, #points : " + _points.Count);
						_SetPoints();
					}
					else
					{
						Debug.LogWarning("[HeatMap2D_Test] cannot add more points, max points count already reached : " + HeatMap2D.MAX_POINTS_COUNT);
					}
				}
			}
		}

		private void _GeneratePoints()
		{
			if (_generationMethod == GenerationMethod.Random)
			{
				// good visual default value for this method.
				heatmap.Radius = radius = 0.1f;
				_GenerateRandomPoints();
			}
			else if (_generationMethod == GenerationMethod.Trajectory)
			{
				// good visual default value for this method.
				heatmap.Radius = radius = 0.02f;
				_GenerateTrajectoryPoints();
			}
		}

		private void _GenerateTrajectoryPoints()
		{
			_points.Clear();
			float length = 0.0f, lengthStep = 0.00025f;
			float angleRad = 0.0f, angleRadStep = Mathf.Deg2Rad * 0.5f;
			Vector4 point = Vector4.zero;
			Vector2 current = Vector2.zero;
			Vector2 dir = new Vector2(1.0f, 0.0f);
			while (_points.Count < pointsCount)
			{
				// Add point.
				point = transform.TransformPoint(current);
				point.w = 1.0f;
				_points.Add(point);
				// Compute next point.
				angleRad += angleRadStep;
				length += lengthStep;
				dir.Set(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
				current = dir.normalized * length;
			}
			Debug.Log("[HeatMap2D_Test] " + _points.Count + " points generated using Trajectory method.");
			_SetPoints();
		}

		private void _GenerateRandomPoints()
		{
			_points.Clear();
			Vector4 point = Vector4.zero;
			Vector3 current = Vector3.zero;
			while (_points.Count < pointsCount)
			{
				current.Set(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0.0f);
				point = transform.TransformPoint(current);
				point.w = 1.0f;
				_points.Add(point);
			}
			Debug.Log("[HeatMap2D_Test] " + _points.Count + " points generated using Random method.");
			_SetPoints();
		}

		private void _SetPoints()
		{
			if (_reductionMethod == ReductionMethod.None)
			{
				heatmap.SetPoints(_points);
			}
			else if (_reductionMethod == ReductionMethod.CanopyClustering)
			{
				_meaningPoints = HeatMap2D.CanopyClustering(_points, _canopyClusteringMaxDistance);
				Debug.Log("[HeatMap2D_Test] " + _points.Count + " points reduced to " + _meaningPoints.Count + " points using CanopyClustering.");
				heatmap.SetPoints(_meaningPoints);
			}
			else if (_reductionMethod == ReductionMethod.AverageClustering)
			{
				_meaningPoints = HeatMap2D.AverageClustering(_points, _averageClusteringMaxPointsCount);
				Debug.Log("[HeatMap2D_Test] " + _points.Count + " points reduced to " + _meaningPoints.Count + " points using AverageClustering.");
				heatmap.SetPoints(_meaningPoints);
			}
		}
	}
}