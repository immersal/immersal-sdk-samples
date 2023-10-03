using UnityEngine;
using System;
using System.Text;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Threading.Tasks;
using Immersal.AR;
using Unity.XR.CoreUtils;

namespace Immersal.Samples
{
	public class ARLocalizerMoveCamera : MonoBehaviour
	{
		[SerializeField] private float m_simulatedLatency = 0f;
		
		private ImmersalSDK m_Sdk = null;
		private IntPtr m_PixelBuffer = IntPtr.Zero;

		private Transform m_trackerXf = null;
		private XROrigin m_Origin = null;

		//
		// filtering variables
		//
		
		private Vector3 filteredPosition = Vector3.zero;
		private Quaternion filteredRotation = Quaternion.identity;
		
		private Vector3 targetPosition = Vector3.zero;
		private Quaternion targetRotation = Quaternion.identity;
		
		private static uint m_HistorySize = 8;
		private Vector3[] m_P = new Vector3[m_HistorySize];
		private Vector3[] m_X = new Vector3[m_HistorySize];
		private Vector3[] m_Z = new Vector3[m_HistorySize];
		private uint m_Samples = 0;
		
		private float m_WarpThresholdDistSq = 5.0f * 5.0f;
		private float m_WarpThresholdCosAngle = Mathf.Cos(20.0f * Mathf.PI / 180.0f);

		[SerializeField] private bool m_useFiltering = false;
		private Matrix4x4 trackerLastPose = Matrix4x4.identity;
		private Matrix4x4 locLastPose = Matrix4x4.identity;
		
		void Start()
		{
			m_Sdk = ImmersalSDK.Instance;
			m_trackerXf = Camera.main.transform;

			m_Origin = FindObjectOfType<XROrigin>();
		}
		
		void Update()
		{
			// if filtering is used, XR Session Origin transform is updated incrementally instead of at once
			if (m_useFiltering)
			{
				float distSq = (filteredPosition - targetPosition).sqrMagnitude;
				float cosAngle = Quaternion.Dot(filteredRotation, targetRotation);
				if (m_Samples == 1 || distSq > m_WarpThresholdDistSq || cosAngle < m_WarpThresholdCosAngle)
				{
					targetPosition = filteredPosition;
					targetRotation = filteredRotation;
				}
				else
				{
					float smoothing = 0.025f;
					float steps = Time.deltaTime / (1.0f / 60.0f);
					if (steps < 1.0f)
						steps = 1.0f;
					else if (steps > 6.0f)
						steps = 6.0f;
					float alpha = 1.0f - Mathf.Pow(1.0f - smoothing, steps);

					targetRotation = Quaternion.Slerp(targetRotation, filteredRotation, alpha);
					targetPosition = Vector3.Lerp(targetPosition, filteredPosition, alpha);
				}

				m_Origin.transform.position = targetPosition;
				m_Origin.transform.rotation = targetRotation;
			}
		}
		
		public virtual void OnDestroy()
		{
			m_PixelBuffer = IntPtr.Zero;
		}

		public async void Localize()
		{
			if (m_Sdk.cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
			{
				using (image)
				{
					// tracker (camera) pose at the time of localization start
					// note it's the local transform instead of the global one. We don't want to include transform from AR Session Origin 
					Vector3 trackerPosBefore = m_trackerXf.localPosition;
					Quaternion trackerRotBefore = m_trackerXf.localRotation;
			
					// get tracker's camera intrinsics and a pointer to the image from AR Foundation
					Vector4 intrinsics;
					ARHelper.GetIntrinsics(out intrinsics);
					ARHelper.GetPlaneDataFast(ref m_PixelBuffer, image);

					if (m_PixelBuffer != IntPtr.Zero)
					{
						// localization done in an async task. Pose of camera (image) is in relation to the Spatial Map.
						// note that currently the on-device plugin does return the usual mapId, but instead a runtime generated internal mapHandle of the map in device memory
						float startTime = Time.realtimeSinceStartup;
						Task<LocalizeInfo> t = Task.Run(() =>
						{
							return Immersal.Core.LocalizeImage(image.width, image.height, ref intrinsics, m_PixelBuffer);
						});

						await t;

						LocalizeInfo locInfo = t.Result;

						// store Immersal localization results
						Matrix4x4 resultMatrix = Matrix4x4.identity;
						resultMatrix.m00 = locInfo.r00; resultMatrix.m01 = locInfo.r01; resultMatrix.m02 = locInfo.r02; resultMatrix.m03 = locInfo.px;
						resultMatrix.m10 = locInfo.r10; resultMatrix.m11 = locInfo.r11; resultMatrix.m12 = locInfo.r12; resultMatrix.m13 = locInfo.py;
						resultMatrix.m20 = locInfo.r20; resultMatrix.m21 = locInfo.r21; resultMatrix.m22 = locInfo.r22; resultMatrix.m23 = locInfo.pz;

						Vector3 locPos = resultMatrix.GetColumn(3);
						Quaternion locRot = resultMatrix.rotation;

						// WARNING: added just to simulate high latency in localization as the camera continues moving during computation
						await Task.Delay(Mathf.RoundToInt(m_simulatedLatency * 1000f));
						
						float elapsedTime = Time.realtimeSinceStartup - startTime;

						// tracker (camera) pose after some time when localization is done
						Vector3 trackerPosAfter = m_trackerXf.localPosition;
						Quaternion trackerRotAfter = m_trackerXf.localRotation;
						
						// delta between before and after tracker (camera) poses = before.inverse * after
						Matrix4x4 trackerPoseBefore =  Matrix4x4.TRS(trackerPosBefore, trackerRotBefore, UnityEngine.Vector3.one);
						Matrix4x4 trackerPoseAfter =  Matrix4x4.TRS(trackerPosAfter, trackerRotAfter, UnityEngine.Vector3.one);
						Matrix4x4 trackerPoseDelta = trackerPoseBefore.inverse * trackerPoseAfter;

						// get the user-friendly mapId from the mapHandle returned from localization
						int mapHandle = locInfo.handle;
						int mapId = ARMap.MapHandleToId(mapHandle);
						
						// mapIds start at 0, -1 means localization failed. ARMap with matching mapId needs to be found in the scene 
						if (mapId > 0 && ARSpace.mapIdToOffset.ContainsKey(mapId))
						{
							// apply the landscape/portrait orientation of the device to localization result
							ARHelper.GetRotation(ref locRot);

							// convert localization result from Immersal's internal right-handed coordinate system to Unity's left-handed
							locPos = ARHelper.SwitchHandedness(locPos);
							locRot = ARHelper.SwitchHandedness(locRot);
							Matrix4x4 locResult = Matrix4x4.TRS(locPos, locRot, Vector3.one);
							
							// LogMatrix4x4("camBefore", trackerPoseBefore);
							// LogMatrix4x4("locResult", locResult);
							
							// get any offset transform for the ARMap in scene with matching mapId (pos, rot, scale in Unity Editor for the ARMap game object)
							// TODO: there's a bug with scaling the ARMap :) Don't do it
							MapOffset mapOffset = ARSpace.mapIdToOffset[mapId];
							Matrix4x4 mapOffsetMatrix = Matrix4x4.TRS(mapOffset.position, mapOffset.rotation, Vector3.one);

							// first apply delta between before and after localization tracker (camera) poses to localization result
							// then apply the ARMap's mapOffsetMatrix to the result in case it had been transformed in the Unity scene
							// this corrected localization result is the localized camera image's pose in relation to the map = mapSpace
							Matrix4x4 mapSpace = mapOffsetMatrix * (locResult * trackerPoseDelta);

							// tracker (camera) pose in Unity scene after localization was computed = trackerSpace
							Matrix4x4 trackerSpace = Matrix4x4.TRS(trackerPosAfter, trackerRotAfter, Vector3.one);
							
							// final offset transform for AR Camera's parent AR Session Origin game object
							// essentially the AR Session Origin is moved to the map in a way that the AR Camera view will match the real world
							Matrix4x4 m = mapSpace * (trackerSpace.inverse);
							
							
							//
							// optional filtering setup starts here
							//
							
							// deltas (last and current localization) for localization results and tracker poses
							Matrix4x4 trackerDelta = trackerLastPose.inverse * trackerPoseBefore;
							Matrix4x4 locDelta = locLastPose.inverse * locResult;
							
							// difference between deltas
							// Should be no difference but just an identity matrix if localization and tracker agree how the device moved since last localization
							Matrix4x4 difference = trackerDelta.inverse * locDelta;

							// LogMatrix4x4("trackerDeltaLastLoc", trackerDelta);
							// LogMatrix4x4("locDeltaLastLoc", locDelta);
							// LogMatrix4x4("difference", difference);
							
							// update variables for next localization
							trackerLastPose = trackerPoseBefore;
							locLastPose = locResult;
							
							
							// finally update XR Session Origin transform. If filtering is on, updating happens incrementally in Update() loop and visually content "swims" in place over time
							// otherwise we just transform the AR Session Origin instantly which visually "snaps" the 3d content in place
							if (m_useFiltering)
							{
								RefinePose(m);
							}
							else
							{
								m_Origin.transform.position = m.GetColumn(3);
								m_Origin.transform.rotation = m.rotation;
							}
							

							Debug.Log(string.Format("Localized to mapId {0} after {1} seconds", mapId, elapsedTime));
						}
						else
						{
							Debug.Log(string.Format("Failed after {0} seconds", elapsedTime));
						}
					}
				}
			}
		}

		private void LogMatrix4x4(string matrixName, Matrix4x4 m)
		{
			StringBuilder sb = new StringBuilder();
			string f = " 0.000;-0.000";

			sb.Append(string.Format("{0}:\n", matrixName));
			sb.Append(string.Format("{0}  {1}  {2}  {3}\n", m.m00.ToString(f), m.m01.ToString(f), m.m02.ToString(f), m.m03.ToString(f)));
			sb.Append(string.Format("{0}  {1}  {2}  {3}\n", m.m10.ToString(f), m.m11.ToString(f), m.m12.ToString(f), m.m13.ToString(f)));
			sb.Append(string.Format("{0}  {1}  {2}  {3}\n", m.m20.ToString(f), m.m21.ToString(f), m.m22.ToString(f), m.m23.ToString(f)));
			sb.Append(string.Format("{0}  {1}  {2}  {3}", m.m30.ToString(f), m.m31.ToString(f), m.m32.ToString(f), m.m33.ToString(f)));

			Debug.Log(sb.ToString());
		}
		
		//
		// filtering functions
		//
		
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
			filteredPosition = Vector3.zero;
			filteredRotation = Quaternion.identity;
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
			filteredPosition = FilterAVT(m_P, n);
			Vector3 x = Vector3.Normalize(FilterAVT(m_X, n));
			Vector3 z = Vector3.Normalize(FilterAVT(m_Z, n));
			Vector3 up = Vector3.Normalize(Vector3.Cross(z, x));
			filteredRotation = Quaternion.LookRotation(z, up);
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
