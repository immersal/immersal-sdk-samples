/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System;
using System.Collections;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;

namespace Immersal.REST
{
    public class MLCoroutineJobLocalizerServer : CoroutineJobLocalizeServer
    {
        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobLocalize On-Server ***************************");
            this.OnStart?.Invoke();

            string encodedImage = Convert.ToBase64String(pixels, 0, pixels.Length);

            SDKLocalizeRequest imageRequest = this.useGPS ? new SDKGeoLocalizeRequest() : new SDKLocalizeRequest();
            imageRequest.token = host.token;
            imageRequest.fx = intrinsics.x;
            imageRequest.fy = intrinsics.y;
            imageRequest.ox = intrinsics.z;
            imageRequest.oy = intrinsics.w;
            imageRequest.param1 = 0;
            imageRequest.param2 = 12;
            imageRequest.param3 = 0.0;
            imageRequest.param4 = 2.0;

            imageRequest.b64 = encodedImage;

            if (this.useGPS)
            {
                SDKGeoLocalizeRequest gr = imageRequest as SDKGeoLocalizeRequest;
                gr.latitude = this.latitude;
                gr.longitude = this.longitude;
                gr.radius = this.radius;
            }
            else
            {
                imageRequest.mapIds = this.mapIds;
            }
            
            string jsonString = JsonUtility.ToJson(imageRequest);
            string endpoint = this.useGPS ? Endpoint.SERVER_GEOLOCALIZE : Endpoint.SERVER_LOCALIZE;

            using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, host.server, endpoint), jsonString))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
                request.useHttpContinue = false;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                var operation = request.SendWebRequest();
                
                while (!operation.isDone)
                {
                    this.OnProgress?.Invoke(request.uploadHandler.progress);
                    yield return null;
                }

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError(request.error);
                    this.OnError?.Invoke(request);
                }
                else if (request.responseCode == (long)HttpStatusCode.OK)
                {
                    Debug.Log(request.downloadHandler.text);
                    SDKLocalizeResult result = JsonUtility.FromJson<SDKLocalizeResult>(request.downloadHandler.text);
                    this.OnResult?.Invoke(result);
                }
            }
        }
    }
}
