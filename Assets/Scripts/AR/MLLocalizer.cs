/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.XR.ARFoundation;
using Unity.Collections.LowLevel.Unsafe;
using Immersal.REST;
using UnityEngine.Experimental.Rendering;
using UnityEngine.XR.MagicLeap;

namespace Immersal.AR.MagicLeap
{
	[RequireComponent(typeof(ICameraDataProvider))]
    public class MLLocalizer : LocalizerBase
    {
	    private ICameraDataProvider m_cameraDataProvider;

	    [SerializeField] private Renderer imageRenderer;
	    [SerializeField] private bool saveLocalizationImageOnDevice = false;

	    private Texture2D textureBuffer;
	    
		private static MLLocalizer instance = null;
	    	    
        public event MapChanged OnMapChanged = null;
        public event PoseFound OnPoseFound = null;
        public delegate void MapChanged(int newMapHandle);
        public delegate void PoseFound(LocalizerPose newPose);

        private void ARSessionStateChanged(ARSessionStateChangedEventArgs args)
        {
            CheckTrackingState(args.state);
        }

        private void CheckTrackingState(ARSessionState newState)
        {
            isTracking = newState == ARSessionState.SessionTracking;

            if (!isTracking)
            {
                foreach (KeyValuePair<Transform, SpaceContainer> item in ARSpace.transformToSpace)
                    item.Value.filter.InvalidateHistory();
            }
        }

		public static MLLocalizer Instance
		{
			get
			{
#if UNITY_EDITOR
				if (instance == null && !Application.isPlaying)
				{
					instance = UnityEngine.Object.FindObjectOfType<MLLocalizer>();
				}
#endif
				if (instance == null)
				{
					Debug.LogError("No MLLocalizer instance found. Ensure one exists in the scene.");
				}
				return instance;
			}
		}

		void Awake()
		{
			if (instance == null)
			{
				instance = this;
			}
			if (instance != this)
			{
				Debug.LogError("There must be only one MLLocalizer object in a scene.");
				UnityEngine.Object.DestroyImmediate(this);
				return;
			}
		}

        public override void OnEnable()
        {
			base.OnEnable();
#if !UNITY_EDITOR
			CheckTrackingState(ARSession.state);
			ARSession.stateChanged += ARSessionStateChanged;
#endif
        }

        public override void OnDisable()
        {
#if !UNITY_EDITOR
			ARSession.stateChanged -= ARSessionStateChanged;
#endif
            base.OnDisable();
        }

        public override void Start()
        {
	        base.Start();

			m_Sdk.Localizer = instance;
	        
	        if (m_cameraDataProvider == null)
	        {
		        m_cameraDataProvider = GetComponent<ICameraDataProvider>();
		        if (m_cameraDataProvider == null)
		        {
			        Debug.LogError("Could not find Camera Data Provider.");
			        enabled = false;
		        }
	        }
        }

        public override async void Localize()
		{
	        byte[] pixels = null;
			MLCamera.YUVBuffer yBuffer = default;
	        Transform cameraTransform = null;
	        Vector4 intrinsics = Vector4.zero;
	        
	        bool cameraDataIsOk = false;
	        
	        if (m_cameraDataProvider != null)
	        {
		        // Get bytes and camera transform from camera data provider
		        Task<bool> t = Task.Run(() =>
		        {
			        return m_cameraDataProvider.TryAcquireLatestData(out pixels, out yBuffer, out cameraTransform);
		        });

				await t;
	        
		        // Get intrinsics
		        bool gotIntrinsics = m_cameraDataProvider.TryAcquireIntrinsics(out intrinsics);

		        cameraDataIsOk = t.Result && gotIntrinsics;
	        }
	        else
	        {
		        Debug.LogError("Cannot find camera data provider.");
	        }

	        if (cameraDataIsOk)
			{
				stats.localizationAttemptCount++;

				Vector3 camPos = cameraTransform.position;
				Quaternion camRot = cameraTransform.rotation;
				int width = (int)yBuffer.Width;
				int height = (int)yBuffer.Height;

				unsafe
				{
					ulong handle;
					byte* ptr = (byte*)UnsafeUtility.PinGCArrayAndGetDataAddress(pixels, out handle);
					m_PixelBuffer = (IntPtr)ptr;
					UnsafeUtility.ReleaseGCObject(handle);
				}

				if (m_PixelBuffer == IntPtr.Zero) return;

				Vector3 pos = Vector3.zero;
				Quaternion rot = Quaternion.identity;

				float startTime = Time.realtimeSinceStartup;

				Task<int> t = Task.Run(() =>
				{
					return Immersal.Core.LocalizeImage(out pos, out rot, width, height, ref intrinsics, m_PixelBuffer);
				});

				await t;

				int mapHandle = t.Result;
				float elapsedTime = Time.realtimeSinceStartup - startTime;

				if (mapHandle >= 0 && ARSpace.mapHandleToOffset.ContainsKey(mapHandle))
				{
					Debug.Log(string.Format("Relocalized in {0} seconds", elapsedTime));
					stats.localizationSuccessCount++;

					if (mapHandle != lastLocalizedMapHandle)
					{
						if (resetOnMapChange)
						{
							Reset();
						}
						
						lastLocalizedMapHandle = mapHandle;

						OnMapChanged?.Invoke(mapHandle);
					}

					rot *= Quaternion.Euler(0f, 0f, -90f);
					MapOffset mo = ARSpace.mapHandleToOffset[mapHandle];

					Matrix4x4 offsetNoScale = Matrix4x4.TRS(mo.position, mo.rotation, Vector3.one);
					Vector3 scaledPos = Vector3.Scale(pos, mo.scale);
					Matrix4x4 cloudSpace = offsetNoScale * Matrix4x4.TRS(scaledPos, rot, Vector3.one);
					Matrix4x4 trackerSpace = Matrix4x4.TRS(camPos, camRot, Vector3.one);
					Matrix4x4 m = trackerSpace * (cloudSpace.inverse);

					if (useFiltering)
						mo.space.filter.RefinePose(m);
					else
						ARSpace.UpdateSpace(mo.space, m.GetColumn(3), m.rotation);

					LocalizerPose localizerPose;
					GetLocalizerPose(out localizerPose, mapHandle, pos, rot, m.inverse);
					OnPoseFound?.Invoke(localizerPose);

					if (ARSpace.mapHandleToMap.ContainsKey(mapHandle))
					{
						ARMap map = ARSpace.mapHandleToMap[mapHandle];
						map.NotifySuccessfulLocalization(mapHandle);
					}
				}
				else
				{
					Debug.Log(string.Format("Localization attempt failed after {0} seconds", elapsedTime));
				}
			}

			base.Localize();
		}

        public override async void LocalizeServer(SDKMapId[] mapIds)
        {
	        byte[] pngBytes = null;
	        Transform cameraTransform = null;
	        Vector4 intrinsics = Vector4.zero;
	        
	        bool cameraDataIsOk = false;
	        
	        if (m_cameraDataProvider != null)
	        {
		        // Get png bytes and camera transform from camera data provider
		        Task<bool> t = Task.Run(() =>
		        {
			        return m_cameraDataProvider.TryAcquirePngBytes(out pngBytes, out cameraTransform);
		        });

				await t;
	        
		        // Get intrinsics
		        bool gotIntrinsics = m_cameraDataProvider.TryAcquireIntrinsics(out intrinsics);

		        cameraDataIsOk = t.Result && gotIntrinsics;
	        }
	        else
	        {
		        Debug.LogError("Cannot find camera data provider.");
	        }

	        if (cameraDataIsOk)
			{
				stats.localizationAttemptCount++;
				
				JobLocalizeServerAsync j = new JobLocalizeServerAsync();
				
				Vector3 camPos = cameraTransform.position;
				Quaternion camRot = cameraTransform.rotation;

				j.mapIds = mapIds;
				j.intrinsics = intrinsics;
				j.image = pngBytes;
				
				if (imageRenderer)
				{
					Task t = Task.Run(() =>
					{
						if (!textureBuffer)
						{
							textureBuffer = new Texture2D(8,8);
							textureBuffer.filterMode = FilterMode.Point;
						}

						bool loadSuccessful = textureBuffer.LoadImage(j.image);
						if (loadSuccessful && textureBuffer.width != 8 && textureBuffer.height != 8)
							imageRenderer.material.mainTexture = textureBuffer;
					});
				}
				
				if (saveLocalizationImageOnDevice)
					File.WriteAllBytes(Path.Combine(Application.persistentDataPath, "latestImage.png"), j.image);

				float startTime = 0f;
				
				j.OnStart += () =>
				{
					startTime = Time.realtimeSinceStartup;
				};

				j.OnResult += (SDKResultBase r) =>
				{
					if (r is SDKLocalizeResult result)
					{
						float elapsedTime = Time.realtimeSinceStartup - startTime;

						if (result.success)
						{
							Debug.Log("*************************** On-Server Localization Success ***************************");
							Debug.Log(string.Format("Relocalized in {0} seconds", elapsedTime));
							
							int mapServerId = result.map;
							
							if (mapServerId > 0 && ARSpace.mapHandleToOffset.ContainsKey(mapServerId))
							{
								if (mapServerId != lastLocalizedMapHandle)
								{
									if (m_ResetOnMapChange)
									{
										Reset();
									}
									lastLocalizedMapHandle = mapServerId;
									OnMapChanged?.Invoke(mapServerId);
								}
								
								MapOffset mo = ARSpace.mapHandleToOffset[mapServerId];
								stats.localizationSuccessCount++;
								
								// Response matrix from server
								Matrix4x4 responseMatrix = Matrix4x4.identity;
								responseMatrix.m00 = result.r00; responseMatrix.m01 = result.r01; responseMatrix.m02 = result.r02; responseMatrix.m03 = result.px;
								responseMatrix.m10 = result.r10; responseMatrix.m11 = result.r11; responseMatrix.m12 = result.r12; responseMatrix.m13 = result.py;
								responseMatrix.m20 = result.r20; responseMatrix.m21 = result.r21; responseMatrix.m22 = result.r22; responseMatrix.m23 = result.pz;
								
								Vector3 pos = responseMatrix.GetColumn(3);
								Quaternion rot = responseMatrix.rotation;
								rot *= Quaternion.Euler(0f, 0f, -90f);
								
								Matrix4x4 offsetNoScale = Matrix4x4.TRS(mo.position, mo.rotation, Vector3.one);
								Vector3 scaledPos = Vector3.Scale(pos, mo.scale);
								Matrix4x4 cloudSpace = offsetNoScale * Matrix4x4.TRS(scaledPos, rot, Vector3.one);
								Matrix4x4 trackerSpace = Matrix4x4.TRS(camPos, camRot, Vector3.one);
								Matrix4x4 m = trackerSpace * (cloudSpace.inverse);

								if (useFiltering)
									mo.space.filter.RefinePose(m);
								else
									ARSpace.UpdateSpace(mo.space, m.GetColumn(3), m.rotation);
								
								LocalizerPose localizerPose;
								GetLocalizerPose(out localizerPose, mapServerId, pos, rot, m.inverse);
								OnPoseFound?.Invoke(localizerPose);

								if (ARSpace.mapHandleToMap.ContainsKey(mapServerId))
								{
									ARMap map = ARSpace.mapHandleToMap[mapServerId];
									map.NotifySuccessfulLocalization(mapServerId);
								}
							}
						}
						else
						{
							Debug.Log("*************************** On-Server Localization Failed ***************************");
							Debug.Log(string.Format("Localization attempt failed after {0} seconds", elapsedTime));
						}
					}
				};

				await j.RunJobAsync();
			}

			base.LocalizeServer(mapIds);
		}
    }
}