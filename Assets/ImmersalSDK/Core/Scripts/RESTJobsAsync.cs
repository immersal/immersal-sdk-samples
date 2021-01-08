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
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Immersal.REST
{
    public static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> DownloadAsync(this HttpClient client, HttpRequestMessage request, Stream destination, IProgress<float> progress = null, CancellationToken cancellationToken = default) {
            using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
            {
                var contentLength = response.Content.Headers.ContentLength;

                using (var download = await response.Content.ReadAsStreamAsync())
                {
                    var relativeProgress = new Progress<long>(totalBytes => progress.Report((float)totalBytes / contentLength.Value));
                    await download.CopyToAsync(destination, 81920, relativeProgress, cancellationToken);
                }

                request.Dispose();
                return response;
            }
        }
    }

    public static class StreamExtensions
    {
        public static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize, IProgress<long> progress = null, CancellationToken cancellationToken = default) {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (!source.CanRead)
                throw new ArgumentException("Has to be readable", nameof(source));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (!destination.CanWrite)
                throw new ArgumentException("Has to be writable", nameof(destination));
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            var buffer = new byte[bufferSize];
            long totalBytesRead = 0;
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0) {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                totalBytesRead += bytesRead;
                progress?.Report(totalBytesRead);
            }
        }
    }

    public class JobAsync
    {
        public readonly string server = ImmersalSDK.Instance.localizationServer;
        public readonly string token = ImmersalSDK.Instance.developerToken;
        public Action OnStart;
        public Action<HttpResponseMessage> OnError;
        public Action<SDKResultBase> OnResult;
        public Progress<float> Progress = new Progress<float>();

        public virtual async Task RunJobAsync()
        {
            await Task.Yield();
        }

        protected void HandleError(HttpResponseMessage response)
        {
            Debug.LogError("Error: " + response.StatusCode);
            this.OnError?.Invoke(response);
        }
    }

    public class JobClearAsync : JobAsync
    {
        public bool anchor;

        public override async Task RunJobAsync()
        {
            Debug.Log("*************************** JobClearAsync ***************************");
            this.OnStart?.Invoke();

            SDKClearRequest r = new SDKClearRequest();
            r.token = this.token;
            r.anchor = this.anchor;
            string jsonString = JsonUtility.ToJson(r);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, string.Format(Endpoint.URL_FORMAT, this.server, Endpoint.CLEAR_JOB));
            request.Content = new StringContent(jsonString);

            using (MemoryStream stream = new MemoryStream())
            {
                using (var response = await ImmersalSDK.client.DownloadAsync(request, stream, this.Progress, CancellationToken.None))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = Encoding.ASCII.GetString(stream.GetBuffer());
                        SDKClearResult result = JsonUtility.FromJson<SDKClearResult>(responseBody);
                        this.OnResult?.Invoke(result);
                    }
                    else
                    {
                        HandleError(response);
                    }
                }
            }
        }
    }

    public class JobConstructAsync : JobAsync
    {
        public string name;
        public int featureCount = 600;
        public int windowSize = 0;
        public bool preservePoses = false;

        public override async Task RunJobAsync()
        {
            Debug.Log("*************************** JobConstructAsync ***************************");
            this.OnStart?.Invoke();

            SDKConstructRequest r = new SDKConstructRequest();
            r.token = this.token;
            r.name = this.name;
            r.featureCount = this.featureCount;
            r.preservePoses = this.preservePoses;

            string jsonString = JsonUtility.ToJson(r);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, string.Format(Endpoint.URL_FORMAT, this.server, Endpoint.CONSTRUCT_MAP));
            request.Content = new StringContent(jsonString);

            using (MemoryStream stream = new MemoryStream())
            {
                using (var response = await ImmersalSDK.client.DownloadAsync(request, stream, this.Progress, CancellationToken.None))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = Encoding.ASCII.GetString(stream.GetBuffer());
                        SDKConstructResult result = JsonUtility.FromJson<SDKConstructResult>(responseBody);
                        this.OnResult?.Invoke(result);
                    }
                    else
                    {
                        HandleError(response);
                    }
                }
            }
        }
    }

    public class JobRestoreMapImagesAsync : JobAsync
    {
        public int id;

        public override async Task RunJobAsync()
        {
            Debug.Log("*************************** JobRestoreMapImagesAsync ***************************");
            this.OnStart?.Invoke();

            SDKRestoreMapImagesRequest r = new SDKRestoreMapImagesRequest();
            r.token = this.token;
            r.id = this.id;

            string jsonString = JsonUtility.ToJson(r);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, string.Format(Endpoint.URL_FORMAT, this.server, Endpoint.RESTORE_MAP_IMAGES));
            request.Content = new StringContent(jsonString);

            using (MemoryStream stream = new MemoryStream())
            {
                using (var response = await ImmersalSDK.client.DownloadAsync(request, stream, this.Progress, CancellationToken.None))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = Encoding.ASCII.GetString(stream.GetBuffer());
                        SDKRestoreMapImagesResult result = JsonUtility.FromJson<SDKRestoreMapImagesResult>(responseBody);
                        this.OnResult?.Invoke(result);
                    }
                    else
                    {
                        HandleError(response);
                    }
                }
            }
        }
    }

    public class JobDeleteMapAsync : JobAsync
    {
        public int id;

        public override async Task RunJobAsync()
        {
            Debug.Log("*************************** JobDeleteMapAsync ***************************");
            this.OnStart?.Invoke();

            SDKDeleteMapRequest r = new SDKDeleteMapRequest();
            r.token = this.token;
            r.id = this.id;

            string jsonString = JsonUtility.ToJson(r);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, string.Format(Endpoint.URL_FORMAT, this.server, Endpoint.DELETE_MAP));
            request.Content = new StringContent(jsonString);

            using (MemoryStream stream = new MemoryStream())
            {
                using (var response = await ImmersalSDK.client.DownloadAsync(request, stream, this.Progress, CancellationToken.None))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = Encoding.ASCII.GetString(stream.GetBuffer());
                        SDKDeleteMapResult result = JsonUtility.FromJson<SDKDeleteMapResult>(responseBody);
                        this.OnResult?.Invoke(result);
                    }
                    else
                    {
                        HandleError(response);
                    }
                }
            }
        }
    }

    public class JobStatusAsync : JobAsync
    {
        public override async Task RunJobAsync()
        {
//            Debug.Log("*************************** JobStatusAsync ***************************");
            this.OnStart?.Invoke();

            SDKStatusRequest r = new SDKStatusRequest();
            r.token = this.token;
            string jsonString = JsonUtility.ToJson(r);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, string.Format(Endpoint.URL_FORMAT, this.server, Endpoint.STATUS));
            request.Content = new StringContent(jsonString);

            using (MemoryStream stream = new MemoryStream())
            {
                using (var response = await ImmersalSDK.client.DownloadAsync(request, stream, this.Progress, CancellationToken.None))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = Encoding.ASCII.GetString(stream.GetBuffer());
                        SDKStatusResult result = JsonUtility.FromJson<SDKStatusResult>(responseBody);
                        this.OnResult?.Invoke(result);
                    }
                    else
                    {
                        HandleError(response);
                    }
                }
            }
        }
    }

    public class JobCaptureAsync : JobAsync
    {
        public int run;
        public int index;
        public bool anchor;
        public Vector4 intrinsics;
        public Matrix4x4 rotation;
        public Vector3 position;
        public double latitude;
        public double longitude;
        public double altitude;
        public string encodedImage;
        public string imagePath;

        public override async Task RunJobAsync()
        {
            Debug.Log("*************************** JobCaptureAsync ***************************");
            this.OnStart?.Invoke();

            SDKImageRequest imageRequest = new SDKImageRequest();
            imageRequest.token = this.token;
            imageRequest.run = this.run;
            imageRequest.index = this.index;
            imageRequest.anchor = this.anchor;
            imageRequest.px = position.x;
            imageRequest.py = position.y;
            imageRequest.pz = position.z;
            imageRequest.r00 = rotation.m00;
            imageRequest.r01 = rotation.m01;
            imageRequest.r02 = rotation.m02;
            imageRequest.r10 = rotation.m10;
            imageRequest.r11 = rotation.m11;
            imageRequest.r12 = rotation.m12;
            imageRequest.r20 = rotation.m20;
            imageRequest.r21 = rotation.m21;
            imageRequest.r22 = rotation.m22;
            imageRequest.fx = intrinsics.x;
            imageRequest.fy = intrinsics.y;
            imageRequest.ox = intrinsics.z;
            imageRequest.oy = intrinsics.w;
            imageRequest.latitude = latitude;
            imageRequest.longitude = longitude;
            imageRequest.altitude = altitude;

            byte[] image = File.ReadAllBytes(imagePath);

            string jsonString = JsonUtility.ToJson(imageRequest);
            byte[] jsonBytes = Encoding.ASCII.GetBytes(jsonString);
            byte[] body = new byte[jsonBytes.Length + 1 + image.Length];
            Array.Copy(jsonBytes, 0, body, 0, jsonBytes.Length);
            body[jsonBytes.Length] = 0;
            Array.Copy(image, 0, body, jsonBytes.Length + 1, image.Length);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, string.Format(Endpoint.URL_FORMAT, this.server, Endpoint.CAPTURE_IMAGE_BIN));
            request.Content = new ByteArrayContent(body);

            using (MemoryStream stream = new MemoryStream())
            {
                using (var response = await ImmersalSDK.client.DownloadAsync(request, stream, this.Progress, CancellationToken.None))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = Encoding.ASCII.GetString(stream.GetBuffer());
                        SDKImageResult result = JsonUtility.FromJson<SDKImageResult>(responseBody);
                        this.OnResult?.Invoke(result);
                    }
                    else
                    {
                        HandleError(response);
                    }
                }
            }
        }
    }

    public class JobLocalizeServerAsync : JobAsync
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector4 intrinsics;
        public int param1 = 0;
        public int param2 = 12;
        public float param3 = 0.0f;
        public float param4 = 2.0f;
        public double latitude = 0.0;
        public double longitude = 0.0;
        public double radius = 0.0;
        public bool useGPS = false;
        public SDKMapId[] mapIds;
        public byte[] image;

        public override async Task RunJobAsync()
        {
            Debug.Log("*************************** JobLocalizeServerAsync ***************************");
            this.OnStart?.Invoke();

            SDKLocalizeRequest imageRequest = this.useGPS ? new SDKGeoLocalizeRequest() : new SDKLocalizeRequest();
            imageRequest.token = this.token;
            imageRequest.fx = intrinsics.x;
            imageRequest.fy = intrinsics.y;
            imageRequest.ox = intrinsics.z;
            imageRequest.oy = intrinsics.w;
            imageRequest.param1 = param1;
            imageRequest.param2 = param2;
            imageRequest.param3 = param3;
            imageRequest.param4 = param4;

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
            byte[] jsonBytes = Encoding.ASCII.GetBytes(jsonString);
            byte[] body = new byte[jsonBytes.Length + 1 + image.Length];
            Array.Copy(jsonBytes, 0, body, 0, jsonBytes.Length);
            body[jsonBytes.Length] = 0;
            Array.Copy(image, 0, body, jsonBytes.Length + 1, image.Length);

            string endpoint = this.useGPS ? Endpoint.SERVER_GEOLOCALIZE_BIN : Endpoint.SERVER_LOCALIZE_BIN;

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, string.Format(Endpoint.URL_FORMAT, this.server, endpoint));
            request.Content = new ByteArrayContent(body);

            using (MemoryStream stream = new MemoryStream())
            {
                using (var response = await ImmersalSDK.client.DownloadAsync(request, stream, this.Progress, CancellationToken.None))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = Encoding.ASCII.GetString(stream.GetBuffer());
                        SDKLocalizeResult result = JsonUtility.FromJson<SDKLocalizeResult>(responseBody);
                        this.OnResult?.Invoke(result);
                    }
                    else
                    {
                        HandleError(response);
                    }
                }
            }
        }
    }

    public class JobEcefAsync : JobAsync
    {
        public int id;

        public override async Task RunJobAsync()
        {
            Debug.Log("*************************** JobEcefAsync ***************************");
            this.OnStart?.Invoke();

            SDKEcefRequest ecefRequest = new SDKEcefRequest();
            ecefRequest.token = this.token;
            ecefRequest.id = this.id;

            string jsonString = JsonUtility.ToJson(ecefRequest);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, string.Format(Endpoint.URL_FORMAT, this.server, Endpoint.SERVER_ECEF));
            request.Content = new StringContent(jsonString);

            using (MemoryStream stream = new MemoryStream())
            {
                using (var response = await ImmersalSDK.client.DownloadAsync(request, stream, this.Progress, CancellationToken.None))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = Encoding.ASCII.GetString(stream.GetBuffer());
                        Debug.Log("ECEF: " + responseBody);
                        SDKEcefResult result = JsonUtility.FromJson<SDKEcefResult>(responseBody);
                        this.OnResult?.Invoke(result);
                    }
                    else
                    {
                        HandleError(response);
                    }
                }
            }
        }
    }

    public class JobListJobsAsync : JobAsync
    {
        public double latitude = 0.0;
        public double longitude = 0.0;
        public double radius = 0.0;
        public bool useGPS = false;
        public List<int> activeMaps;
        public bool useToken = true;

        public override async Task RunJobAsync()
        {
            Debug.Log("*************************** JobListJobsAsync ***************************");
            this.OnStart?.Invoke();

            SDKJobsRequest r = this.useGPS ? new SDKGeoJobsRequest() : new SDKJobsRequest();
            r.token = useToken ? this.token : "";

            if (this.useGPS)
            {
                SDKGeoJobsRequest gr = r as SDKGeoJobsRequest;
                gr.latitude = this.latitude;
                gr.longitude = this.longitude;
                gr.radius = this.radius;
            }

            string jsonString = JsonUtility.ToJson(r);
            string endpoint = this.useGPS ? Endpoint.LIST_GEOJOBS : Endpoint.LIST_JOBS;

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, string.Format(Endpoint.URL_FORMAT, this.server, endpoint));
            request.Content = new StringContent(jsonString);

            using (MemoryStream stream = new MemoryStream())
            {
                using (var response = await ImmersalSDK.client.DownloadAsync(request, stream, this.Progress, CancellationToken.None))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = Encoding.ASCII.GetString(stream.GetBuffer());
                        SDKJobsResult result = JsonUtility.FromJson<SDKJobsResult>(responseBody);
                        this.OnResult?.Invoke(result);
                    }
                    else
                    {
                        HandleError(response);
                    }
                }
            }
        }
    }

    public class JobLoadMapAsync : JobAsync
    {
        public int id;

        public override async Task RunJobAsync()
        {
            Debug.Log("*************************** JobLoadMapAsync ***************************");
            this.OnStart?.Invoke();

            SDKMapRequest r = new SDKMapRequest();
            r.token = this.token;
            r.id = this.id;

            string jsonString = JsonUtility.ToJson(r);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, string.Format(Endpoint.URL_FORMAT, this.server, Endpoint.LOAD_MAP));
            request.Content = new StringContent(jsonString);

            using (MemoryStream stream = new MemoryStream())
            {
                using (var response = await ImmersalSDK.client.DownloadAsync(request, stream, this.Progress, CancellationToken.None))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = Encoding.ASCII.GetString(stream.GetBuffer());
                        SDKMapResult result = JsonUtility.FromJson<SDKMapResult>(responseBody);
                        this.OnResult?.Invoke(result);
                    }
                    else
                    {
                        HandleError(response);
                    }
                }
            }
        }
    }

    public class JobSetPrivacyAsync : JobAsync
    {
        public int id;
        public int privacy;

        public override async Task RunJobAsync()
        {
            Debug.Log("*************************** JobSetPrivacyAsync ***************************");
            this.OnStart?.Invoke();

            SDKMapPrivacyRequest privacyRequest = new SDKMapPrivacyRequest();
            privacyRequest.token = this.token;
            privacyRequest.id = this.id;
            privacyRequest.privacy = this.privacy;

            string jsonString = JsonUtility.ToJson(privacyRequest);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, string.Format(Endpoint.URL_FORMAT, this.server, Endpoint.PRIVACY));
            request.Content = new StringContent(jsonString);

            using (MemoryStream stream = new MemoryStream())
            {
                using (var response = await ImmersalSDK.client.DownloadAsync(request, stream, this.Progress, CancellationToken.None))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = Encoding.ASCII.GetString(stream.GetBuffer());
                        SDKMapPrivacyResult result = JsonUtility.FromJson<SDKMapPrivacyResult>(responseBody);
                        this.OnResult?.Invoke(result);
                    }
                    else
                    {
                        HandleError(response);
                    }
                }
            }
        }
    }

    public class JobLoginAsync : JobAsync
    {
        public string username;
        public string password;

        public override async Task RunJobAsync()
        {
            Debug.Log("*************************** JobLoginAsync ***************************");
            this.OnStart?.Invoke();

            SDKLoginRequest loginRequest = new SDKLoginRequest();
            loginRequest.login = this.username;
            loginRequest.password = this.password;

            string jsonString = JsonUtility.ToJson(loginRequest);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, string.Format(Endpoint.URL_FORMAT, this.server, Endpoint.LOGIN));
            request.Content = new StringContent(jsonString);

            using (MemoryStream stream = new MemoryStream())
            {
                using (var response = await ImmersalSDK.client.DownloadAsync(request, stream, this.Progress, CancellationToken.None))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = Encoding.ASCII.GetString(stream.GetBuffer());
                        SDKLoginResult result = JsonUtility.FromJson<SDKLoginResult>(responseBody);
                        this.OnResult?.Invoke(result);
                    }
                    else
                    {
                        HandleError(response);
                    }
                }
            }
        }
    }
}
