/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

#if (UNITY_IOS || PLATFORM_ANDROID) && !UNITY_EDITOR
using System.Runtime.InteropServices;
using UnityEngine;

namespace Immersal
{
	public class NativeBindings
	{
		#if UNITY_IOS
		[DllImport("__Internal")]
		public static extern void startLocation();

		[DllImport("__Internal")]
		public static extern void stopLocation();

		[DllImport("__Internal")]
		public static extern double getLatitude();

		[DllImport("__Internal")]
		public static extern double getLongitude();

		[DllImport("__Internal")]
		public static extern double getAltitude();

		[DllImport("__Internal")]
		public static extern double getHorizontalAccuracy();

		[DllImport("__Internal")]
		public static extern double getVerticalAccuracy();

		[DllImport("__Internal")]
		public static extern bool locationServicesEnabled();

		#elif PLATFORM_ANDROID
		static AndroidJavaClass obj = new AndroidJavaClass("com.immersal.nativebindings.Main");
		#endif

		public static bool StartLocation()
		{
			if (!Input.location.isEnabledByUser)
			{
				return false;
			}

			#if UNITY_IOS
			startLocation();
			#elif PLATFORM_ANDROID
			obj.CallStatic("startLocation");
			#endif

			return true;
		}

		public static void StopLocation()
		{
			#if UNITY_IOS
			stopLocation();
			#elif PLATFORM_ANDROID
			obj.CallStatic("stopLocation");
			#endif
		}

		public static double GetLatitude()
		{
			#if UNITY_IOS
			return getLatitude();
			#elif PLATFORM_ANDROID
			return obj.CallStatic<double>("getLatitude");
			#endif
		}

		public static double GetLongitude()
		{
			#if UNITY_IOS
			return getLongitude();
			#elif PLATFORM_ANDROID
			return obj.CallStatic<double>("getLongitude");
			#endif
		}

		public static double GetAltitude()
		{
			#if UNITY_IOS
			return getAltitude();
			#elif PLATFORM_ANDROID
			return obj.CallStatic<double>("getAltitude");
			#endif
		}

		public static double GetHorizontalAccuracy()
		{
			#if UNITY_IOS
			return getHorizontalAccuracy();
			#elif PLATFORM_ANDROID
			return obj.CallStatic<double>("getHorizontalAccuracy");
			#endif
		}

		public static double GetVerticalAccuracy()
		{
			#if UNITY_IOS
			return getVerticalAccuracy();
			#elif PLATFORM_ANDROID
			return obj.CallStatic<double>("getVerticalAccuracy");
			#endif
		}

		public static bool LocationServicesEnabled()
		{
			#if UNITY_IOS
			return locationServicesEnabled();
			#elif PLATFORM_ANDROID
			return obj.CallStatic<bool>("locationServicesEnabled");
			#endif
		}
	}
}
#endif