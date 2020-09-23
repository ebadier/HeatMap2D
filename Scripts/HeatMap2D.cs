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
using UnityEngine.Assertions;

namespace HeatMap2D
{
	/// <summary>
	/// This script draw a heatmap on a mesh in XZ plane.
	/// This class cannot render more than MAX_POINTS_COUNT points on a mesh because of shader performance limitation.
	/// But you can draw much more points than this limit by first reducing your set of points using the provided reduction methods,
	/// and then sending the reduced set of points to the shader using SetPoints().
	/// </summary>
	public sealed class HeatMap2D : MonoBehaviour
	{
		// Unity maximum allowed array size for shaders.
		// We can use ComputeBuffer to overpass this limit,
		// but anyway shader performance drops too much beyond this limit.
		public const int MAX_POINTS_COUNT = 1023;

		public Material heatmapMaterial;
		[Tooltip("Where to affect heatmap material : to MeshRenderer's sharedMaterial or material")]
		public bool affectSharedMaterial = false;

		private MeshRenderer _meshRenderer;
		// to sample the distance from a hot point.
		private float _radius = 0.1f;
		// to change the amount of coloration.
		private float _intensity = 0.1f;

		// Shader properties.
		private int _invRadiusID = Shader.PropertyToID("_InvRadius");
		private int _intensityID = Shader.PropertyToID("_Intensity");
		private int _countID = Shader.PropertyToID("_Count");
		private int _pointsID = Shader.PropertyToID("_Points");
		//private ComputeBuffer _computeBuffer;

		public float Radius
		{
			get { return _radius; }
			set
			{
				_radius = value;
				heatmapMaterial.SetFloat(_invRadiusID, 1.0f / _radius);
			}
		}

		public float Intensity
		{
			get { return _intensity; }
			set
			{
				_intensity = value;
				heatmapMaterial.SetFloat(_intensityID, _intensity);
			}
		}

		public Bounds WorldBounds
		{
			get { return _meshRenderer.bounds; }
		}

		private void Awake()
		{
			_meshRenderer = GetComponent<MeshRenderer>();

			// Initialize shader variables.
			heatmapMaterial.SetFloat(_invRadiusID, 1.0f / _radius);
			heatmapMaterial.SetFloat(_intensityID, _intensity);
			heatmapMaterial.SetInt(_countID, 0);
			heatmapMaterial.SetVectorArray(_pointsID, new Vector4[MAX_POINTS_COUNT]);
			// ComputeBuffer version.
			//_computeBuffer = new ComputeBuffer(MAX_BUFFER_SIZE, Marshal.SizeOf(Vector4.zero), ComputeBufferType.Default);
			//heatmapMaterial.SetBuffer(_pointsID, _computeBuffer);

			if (affectSharedMaterial)
			{
				_meshRenderer.sharedMaterial = heatmapMaterial;
			}
			else
			{
				_meshRenderer.material = heatmapMaterial;
			}
		}

		// ComputeBuffer version.
		//private void OnDestroy()
		//{
		//	_computeBuffer.Release();
		//}

		/// <summary>
		/// Set the whole list of points contributing to the heatMap.
		/// Setting a null or empty list is allowed to clear the heatmap.
		/// <param name="points">x,y,z components are 3D coordinates. w component is weight (should be > 0).</param>
		/// </summary>
		public void SetPoints(List<Vector4> points)
		{
			if ((points == null) || (points.Count == 0))
			{
				// clear heatmap.
				heatmapMaterial.SetInt(_countID, 0);
				return;
			}

			if (points.Count > MAX_POINTS_COUNT)
			{
				Debug.LogError("[HeatMap2D] #points (" + points.Count + ") exceeds maximum (" + MAX_POINTS_COUNT + ") !");
				return;
			}

			heatmapMaterial.SetInt(_countID, points.Count);
			heatmapMaterial.SetVectorArray(_pointsID, points);
			// ComputeBuffer version
			//_computeBuffer.SetData(points);
		}

		#region Algorithms
		/// <summary>
		/// Computes the weighted-average of the given 3D-weighted points.
		/// x,y,z components are 3D coordinates. w component is weight (should be > 0).
		/// </summary>
		public static Vector4 WeightedAverage(Vector4 pointA, Vector4 pointB)
		{
			float wSum = pointA.w + pointB.w;
			Assert.IsTrue(wSum > 0.0f, "[HeatMap2D.WeightedAverage] weights should be > 0 !");
			float wInvSum = 1.0f / wSum;
			// Weighted-average of coordinates.
			return new Vector4((pointA.x * pointA.w + pointB.x * pointB.w) * wInvSum,
								(pointA.y * pointA.w + pointB.y * pointB.w) * wInvSum,
								(pointA.z * pointA.w + pointB.z * pointB.w) * wInvSum,
								// Sum the weights.
								wSum);
		}

		/// <summary>
		/// Returns the bounds of the given set of points.
		/// </summary>
		public static Bounds GetBounds(List<Vector4> points)
		{
			Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
			Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
			foreach (Vector4 point in points)
			{
				// X min/max
				if (point.x > max.x)
				{
					max.x = point.x;
				}
				if (point.x < min.x)
				{
					min.x = point.x;
				}
				// Y min/max
				if (point.y > max.y)
				{
					max.y = point.y;
				}
				if (point.y < min.y)
				{
					min.y = point.y;
				}
				// Z min/max
				if (point.z > max.z)
				{
					max.z = point.z;
				}
				if (point.z < min.z)
				{
					min.z = point.z;
				}
			}
			Bounds bounds = new Bounds();
			bounds.SetMinMax(min, max);
			return bounds;
		}

		/// <summary>
		/// Reduces the given set of points using a grid partition in XZ plane.
		/// As a result, you get less points with more weights, which are representative of the original set of points.
		/// This method is fast and gives high-fidelity results with any type of input points (randomly-generated, trajectories, ...).
		/// <param name="points">x,y,z components are 3D coordinates. w component is weight (should be > 0).</param>
		/// <param name="maxPointsCount"/>The new maximum number of points. If maxPointsCount >= points.Count, do nothing and directly returns points.<param/>
		/// </summary>
		public static List<Vector4> GridPartition(List<Vector4> points, int maxPointsCount = MAX_POINTS_COUNT)
		{
			Assert.IsTrue(maxPointsCount >= 0, "[HeatMap2D.GridPartition2D] maxPointsCount should be >= 0 !");

			if (maxPointsCount >= points.Count)
			{
				// Nothing to do.
				return points;
			}

			List<Vector4> rPoints = new List<Vector4>(maxPointsCount);
			if (maxPointsCount == 0)
			{
				// Someone could ask for this.
				return rPoints;
			}

			// Get the cell lengths of the NxN square grid.
			Bounds gridBounds = GetBounds(points);
			Vector3 gridSize = gridBounds.size;
			Vector3 gridMin = gridBounds.min;
			int n = Mathf.FloorToInt(Mathf.Sqrt((float)maxPointsCount));
			float cell_X_Length = gridSize.x / (float)n;
			float cell_Z_Length = gridSize.z / (float)n;
			// Average the points to one in each cell.
			float cell_X_Min, cell_Z_Min, cell_X_Max, cell_Z_Max;
			Vector4 rPoint = Vector4.zero;
			bool emptyCell;
			for (int i = 0; i < n; ++i) // rows
			{
				cell_X_Min = gridMin.x + (i * cell_X_Length);
				cell_X_Max = cell_X_Min + cell_X_Length;
				for (int j = 0; j < n; ++j) // cols
				{
					cell_Z_Min = gridMin.z + (j * cell_Z_Length);
					cell_Z_Max = cell_Z_Min + cell_Z_Length;
					emptyCell = true;
					foreach (Vector4 point in points)
					{
						// Check if the point is in the current cell.
						if ((point.x >= cell_X_Min) && (point.x < cell_X_Max) && (point.z >= cell_Z_Min) && (point.z < cell_Z_Max))
						{
							if (emptyCell)
							{
								rPoint = point;
								emptyCell = false;
							}
							else
							{
								rPoint = WeightedAverage(rPoint, point);
							}
						}
					}

					if (!emptyCell)
					{
						rPoints.Add(rPoint);
					}
				}
			}
			return rPoints;
		}

		/// <summary>
		/// Reduces the given set of points using Canopy clustering in XZ plane.
		/// As a result, you get less points with more weights, which are representative of the original set of points.
		/// This method is fast and gives high-fidelity results with any type of input points (randomly-generated, trajectories, ...).
		/// But this method has one drawback : you can't control the new maximum number of points.
		/// <param name="points">x,y,z components are 3D coordinates. w component is weight (should be > 0).</param>
		/// <param name="maxDistance"/>Two points are averaged together if their XZ distance is less than this value.<param/>
		/// </summary>
		public static List<Vector4> CanopyClustering(List<Vector4> points, float maxDistance = 0.033f)
		{
			Assert.IsTrue(maxDistance > 0.0f, "[HeatMap2D.CanopyClustering] maxDistance should be > 0 !");

			List<Vector4> rPoints = new List<Vector4>(MAX_POINTS_COUNT);
			float sqrMaxDistance = maxDistance * maxDistance;
			Vector2 vecXZ = Vector2.zero;
			Vector4 rPoint = Vector4.zero;
			foreach (Vector4 point in points)
			{
				for (int i = 0; i < rPoints.Count; ++i)
				{
					rPoint = rPoints[i];
					vecXZ.Set(	rPoint.x - point.x,
								rPoint.z - point.z);
					// Average only close points.
					if (vecXZ.sqrMagnitude < sqrMaxDistance)
					{
						// Tag this point as averaged (weight > 0).
						if (rPoint.w < 0.0f)
						{
							rPoint.w = -rPoint.w;
						}
						rPoints[i] = WeightedAverage(rPoint, point);
						break;
					}
				}
				// Always init with negative weight to remove non-averaged points at the end.
				rPoints.Add(new Vector4(point.x, point.y, point.z, -point.w));
			}
			// Remove non-averaged points.
			rPoints.RemoveAll(rP => rP.w < 0.0f);
			return rPoints;
		}

		/// <summary>
		/// Reduces the given set of points using Decimation.
		/// As a result, you get less points with more weights, which are representative of the original set of points.
		/// This method is very fast and gives high-fidelity results with non-randomly generated input points only (e.g Trajectories).
		/// <param name="points">x,y,z components are 3D coordinates. w component is weight (should be > 0).</param>
		/// <param name="maxPointsCount"/>The new maximum number of points. If maxPointsCount >= points.Count, do nothing and directly returns points.<param/>
		/// </summary>
		public static List<Vector4> Decimation(List<Vector4> points, int maxPointsCount = MAX_POINTS_COUNT)
		{
			Assert.IsTrue(maxPointsCount >= 0, "[HeatMap2D.Decimation] maxPointsCount should be >= 0 !");

			if (maxPointsCount >= points.Count)
			{
				// Nothing to do.
				return points;
			}

			List<Vector4> rPoints = new List<Vector4>(maxPointsCount);
			if (maxPointsCount == 0)
			{
				// Someone could ask for this.
				return rPoints;
			}

			// Maximize clustersCount to get the maximum numbers of equally-sized clusters.
			int clustersSize = Mathf.CeilToInt((float)points.Count / (float)maxPointsCount);
			int clustersCount = points.Count / clustersSize;
			Vector4 rPoint;
			for (int i = 0; i < clustersCount; ++i)
			{
				rPoint = points[i * clustersSize];
				rPoint.w *= clustersSize; // Compensate the loss of other points.
				rPoints.Add(rPoint); // Only keep the first point in each cluster
			}
			return rPoints;
		}
		#endregion
	}
}