/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of Immersal SDK v1.3.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;

namespace Immersal.Samples
{
	public class Rotate : MonoBehaviour
	{
		[SerializeField]
		private float speed = 10f;

		void Start () {
			
		}
		
		void Update () {
			transform.Rotate(Vector3.forward, speed * Time.deltaTime, Space.Self);
			//transform.Rotate(Vector3.up, speed * Time.deltaTime, Space.Self);
			//transform.Rotate(Vector3.right, speed * 2f * Time.deltaTime, Space.Self);
		}
	}
}
