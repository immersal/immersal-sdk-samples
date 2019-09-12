/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of Immersal AR Cloud SDK v1.1.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;

namespace Immersal.Samples.Multiplayer
{
	public class Billboard : MonoBehaviour {

		void Update()
		{
			transform.LookAt(Camera.main.transform);
		}
	}
}