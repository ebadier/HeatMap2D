using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace HeatMap2D
{
	/// <summary>
	/// Put this script on a GameObject having a mesh renderer to draw a heatmap on it.
	/// This class cannot render more than MAX_POINTS_COUNT points on a mesh because of shader performance limitation.
	/// But you can draw much more points than this limit by first reducing your set of points using the provided clustering methods,
	/// and then sending the reduced set of points using SetPoints().
	/// </summary>
	public sealed class HeatMap2D : MonoBehaviour
	{
		// Unity maximum allowed array size for shaders.
		// We can use ComputeBuffer to overpass this limit,
		// but anyway shader performance drops too badly beyond this limit.
		public const int MAX_POINTS_COUNT = 1023;

		public Material heatmapMaterial;
		[Tooltip("Where to affect heatmap material : to MeshRenderer's sharedMaterial or material")]
		public bool affectSharedMaterial = false;

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

		private void Awake()
		{
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
				GetComponent<MeshRenderer>().sharedMaterial = heatmapMaterial;
			}
			else
			{
				GetComponent<MeshRenderer>().material = heatmapMaterial;
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

		#region Clustering
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
		/// Reduces the given set of points using Canopy clustering.
		/// As a result, you get less points with more weights, which are representative of the original set of points.
		/// Use this method to reduce a list of random points (no assumption can be made on the original set of points).
		/// <param name="points">x,y,z components are 3D coordinates. w component is weight (should be > 0).</param>
		/// <param name="maxDistance"/>Two points are averaged together if their distance is less than this value.<param/>
		/// </summary>
		public static List<Vector4> CanopyClustering(List<Vector4> points, float maxDistance = 0.033f)
		{
			Assert.IsTrue(maxDistance > 0.0f, "[HeatMap2D.CanopyClustering] maxDistance should be > 0 !");

			List<Vector4> rPoints = new List<Vector4>();
			float sqrMaxDistance = maxDistance * maxDistance;
			Vector3 dir = Vector3.zero;
			Vector4 rPoint = Vector4.zero;
			foreach (Vector4 point in points)
			{
				for (int i = 0; i < rPoints.Count; ++i)
				{
					rPoint = rPoints[i];
					dir.Set(rPoint.x - point.x,
							rPoint.y - point.y,
							rPoint.z - point.z);
					// Average only close points.
					if (dir.sqrMagnitude < sqrMaxDistance)
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
		/// Reduce the given set of points using Average clustering.
		/// As a result, you get less points with more weights, which are representative of the original set of points.
		/// You can use this method to reduce a list of non-randomly generated points (eg: trajectories).
		/// - It allows you to control the new maximum number of points.
		/// - In any other cases, prefer using Canopy clustering method.
		/// <param name="points">x,y,z components are 3D coordinates. w component is weight (should be > 0).</param>
		/// <param name="maxPointsCount"/>The new maximum number of points. If maxPointsCount >= points.Count, do nothing and directly returns points.<param/>
		/// </summary>
		public static List<Vector4> AverageClustering(List<Vector4> points, int maxPointsCount = MAX_POINTS_COUNT)
		{
			Assert.IsTrue(maxPointsCount >= 0, "[HeatMap2D.AverageClustering] maxPointsCount should be >= 0 !");

			if (maxPointsCount >= points.Count)
			{
				// Nothing to do.
				return points;
			}

			List<Vector4> rPoints = new List<Vector4>();
			if (maxPointsCount == 0)
			{
				// Someone could ask for this.
				return rPoints;
			}

			// Maximize clustersCount to get the maximum numbers of equally-sized clusters.
			int clustersSize = Mathf.CeilToInt((float)points.Count / (float)maxPointsCount);
			int clustersCount = points.Count / clustersSize;
			Vector4 rPoint;
			// Average points in each cluster.
			for (int i = 0; i < clustersCount; ++i)
			{
				rPoint = points[i * clustersSize]; // the first point in each cluster...
				for (int j = 1; j < clustersSize; ++j)
				{
					/// ...is averaged with others points in the same cluster.
					rPoint = WeightedAverage(rPoint, points[i*clustersSize + j]);
				}
				rPoints.Add(rPoint);
			}
			// Last cluster is empty or smaller than the others.
			int lastClusterSize = points.Count - (clustersCount * clustersSize);
			if (lastClusterSize > 0)
			{
				rPoint = points[points.Count - lastClusterSize];
				for (int i = points.Count - lastClusterSize + 1; i < points.Count; ++i)
				{
					rPoint = WeightedAverage(rPoint, points[i]);
				}
				rPoints.Add(rPoint);
			}
			//Debug.Log("[HeatMap2D.AverageClustering] #Clusters (" + clustersCount +") ; Size (" + clustersSize + ") ; LastSize (" + lastClusterSize + ")");
			return rPoints;
		}
		#endregion
	}
}