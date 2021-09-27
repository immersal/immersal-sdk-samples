using UnityEngine;
using System;

namespace Immersal.Samples.Util
{
	public static class LocationUtil
    {
		private static double toRadians(double angle)
		{
			return (Math.PI / 180) * angle;
		}

		// Haversine distance
		public static double DistanceBetweenPoints(Vector2 p1, Vector2 p2)
		{
			double R = 6371e3;	// Earth's radius in metres
			double radLat1 = toRadians(p1.x);
			double radLat2 = toRadians(p2.x);
			double radLon1 = toRadians(p1.y);
			double radLon2 = toRadians(p2.y);
			double deltaLat = radLat2 - radLat1;
			double deltaLon = radLon2 - radLon1;

			// the square of half the chord length between the points
			double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) + Math.Cos(radLat1) * Math.Cos(radLat2) * Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
			// angular distance in radians
			double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a));
			// distance in metres
			double d = R * c;

			return d;
		}
	}
}
