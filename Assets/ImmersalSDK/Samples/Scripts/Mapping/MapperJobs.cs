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
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Immersal.AR;
using Immersal.REST;
using Immersal.Samples.Util;

namespace Immersal.Samples.Mapping
{
    public class CoroutineJob
    {
        public IJobHost host;

        public virtual IEnumerator RunJob()
        {
            yield return null;
        }
    }

    public class CoroutineJobClear : CoroutineJob
    {
        public bool anchor;

        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobClear ***************************");

            SDKClearRequest r = new SDKClearRequest();
            r.token = host.token;
            r.bank = (host as BaseMapper).currentBank;
            r.anchor = this.anchor;
            string jsonString = JsonUtility.ToJson(r);
            using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, host.server, Endpoint.CLEAR_JOB), jsonString))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
                request.useHttpContinue = false;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                Debug.Log("Response code: " + request.responseCode);

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError(request.error);
                }
                else if (request.responseCode == (long)HttpStatusCode.OK)
                {
                    Debug.Log(request.downloadHandler.text);
                }
            }
        }
    }

    public class CoroutineJobConstruct : CoroutineJob
    {
        public string name;

        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobConstruct ***************************");

            SDKConstructRequest r = new SDKConstructRequest();
            r.token = host.token;
            r.bank = (host as BaseMapper).currentBank;
            r.name = this.name;

            string jsonString = JsonUtility.ToJson(r);
            using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, host.server, Endpoint.CONSTRUCT_MAP), jsonString))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
                request.useHttpContinue = false;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                Debug.Log("Response code: " + request.responseCode);

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError(request.error);
                }
                else if (request.responseCode == (long)HttpStatusCode.OK)
                {
                    SDKConstructResult result = JsonUtility.FromJson<SDKConstructResult>(request.downloadHandler.text);
                    if (result.error == "none")
                    {
                        Debug.Log(string.Format("Started constructing a map width ID {0}, containing {1} images", result.id, result.size));
                    }
                }
            }
        }
    }

    public class CoroutineJobRestoreMapImages : CoroutineJob
    {
        public int mapId;

        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobRestoreMapImages ***************************");

            SDKRestoreMapImagesRequest r = new SDKRestoreMapImagesRequest();
            r.token = host.token;
            r.id = this.mapId;
            string jsonString = JsonUtility.ToJson(r);
            using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, host.server, Endpoint.RESTORE_MAP_IMAGES), jsonString))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
                request.useHttpContinue = false;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError(request.error);
                }
                else if (request.responseCode == (long)HttpStatusCode.OK)
                {
                    Debug.Log(request.downloadHandler.text);
                }
            }
        }
    }

    public class CoroutineJobDeleteMap : CoroutineJob
    {
        public int mapId;

        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobDeleteMap ***************************");

            SDKDeleteMapRequest r = new SDKDeleteMapRequest();
            r.token = host.token;
            r.id = this.mapId;
            string jsonString = JsonUtility.ToJson(r);
            using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, host.server, Endpoint.DELETE_MAP), jsonString))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
                request.useHttpContinue = false;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError(request.error);
                }
                else if (request.responseCode == (long)HttpStatusCode.OK)
                {
                    Debug.Log(request.downloadHandler.text);
                }
            }
        }
    }

    public class CoroutineJobStatus : CoroutineJob
    {
        public override IEnumerator RunJob()
        {
//            Debug.Log("*************************** CoroutineJobStatus ***************************");

            BaseMapper mapper = host as BaseMapper;
            SDKStatusRequest r = new SDKStatusRequest();
            r.token = mapper.token;
            r.bank = mapper.currentBank;
            string jsonString = JsonUtility.ToJson(r);
            using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, mapper.server, Endpoint.STATUS), jsonString))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
                request.useHttpContinue = false;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError(request.error);
                }
                else if (request.responseCode == (long)HttpStatusCode.OK)
                {
                    SDKStatusResult result = JsonUtility.FromJson<SDKStatusResult>(request.downloadHandler.text);
                    mapper.stats.imageCount = result.imageCount;
                }
            }
        }
    }

    public class CoroutineJobCapture : CoroutineJob
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

        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobCapture ***************************");
            float startTime = Time.realtimeSinceStartup;

            BaseMapper mapper = host as BaseMapper;

            SDKImageRequest imageRequest = new SDKImageRequest();
            imageRequest.token = mapper.token;
            imageRequest.run = this.run;
            imageRequest.bank = mapper.currentBank;
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
            imageRequest.b64 = encodedImage;

            string jsonString = JsonUtility.ToJson(imageRequest);

            using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, mapper.server, Endpoint.CAPTURE_IMAGE), jsonString))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
                request.useHttpContinue = false;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                Debug.Log("Response code: " + request.responseCode);

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError(request.error);
                }
                else if (request.responseCode == (long)HttpStatusCode.OK)
                {
                    SDKImageResult result = JsonUtility.FromJson<SDKImageResult>(request.downloadHandler.text);
                    if (result.error == "none")
                    {
                        float elapsedTime = Time.realtimeSinceStartup - startTime;
                        Debug.Log(string.Format("Image uploaded successfully in {0} seconds", elapsedTime));
                    }
                }
            }
        }
    }

    public class CoroutineJobLocalize : CoroutineJob
    {
        public Vector4 intrinsics;
        public Quaternion rotation;
        public Vector3 position;
        public byte[] pixels;
        public int width;
        public int height;

        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobLocalize ***************************");

            BaseMapper mapper = host as BaseMapper;
            Vector3 pos = new Vector3();
            Quaternion rot = new Quaternion();

            Task<int> t = Task.Run(() =>
            {
                return Immersal.Core.LocalizeImage(out pos, out rot, width, height, ref intrinsics, pixels);
            });

            while (!t.IsCompleted)
            {
                yield return null;
            }

            int mapId = t.Result;

            if (mapId >= 0)
            {
                mapper.stats.locSucc++;

                Debug.Log("*************************** Localization Succeeded ***************************");
                Matrix4x4 cloudSpace = Matrix4x4.TRS(pos, rot, Vector3.one);
                Matrix4x4 trackerSpace = Matrix4x4.TRS(position, rotation, Vector3.one);
                Debug.Log("id " + mapId + "\n" +
                          "fc 4x4\n" + cloudSpace + "\n" +
                          "ft 4x4\n" + trackerSpace);

                Matrix4x4 m = trackerSpace*(cloudSpace.inverse);

                LocalizerPose lastLocalizedPose;
                BaseLocalizer.GetLocalizerPose(out lastLocalizedPose, mapId, pos, rot, m.inverse);
                mapper.lastLocalizedPose = lastLocalizedPose;

                foreach (PointCloudRenderer p in mapper.pcr.Values)
                {
                    if (p.mapId == mapId)
                    {
                        p.go.transform.position = m.GetColumn(3);
                        p.go.transform.rotation = m.rotation;
                        break;
                    }
                }
            }
            else
            {
                mapper.stats.locFail++;
                Debug.Log("*************************** Localization Failed ***************************");
            }
        }
    }

    public class CoroutineJobLocalizeServer : CoroutineJob
    {
        public Vector4 intrinsics;
        public Quaternion rotation;
        public Vector3 position;
        public byte[] pixels;
        public int width;
        public int height;
        public int channels;
        public double latitude = 0.0;
        public double longitude = 0.0;
        public double radius = 0.0;
        public bool useGPS = false;

        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobLocalize On-Server ***************************");

            BaseMapper mapper = host as BaseMapper;
            byte[] capture = new byte[channels * width * height + 1024];
            Task<(string, icvCaptureInfo)> t = Task.Run(() =>
            {
                icvCaptureInfo info = Immersal.Core.CaptureImage(capture, capture.Length, pixels, width, height, channels);
                return (Convert.ToBase64String(capture, 0, info.captureSize), info);
            });

            while (!t.IsCompleted)
            {
                yield return null;
            }

            string encodedImage = t.Result.Item1;
            icvCaptureInfo captureInfo = t.Result.Item2;

            SDKLocalizeRequest imageRequest = this.useGPS ? new SDKGeoLocalizeRequest() : new SDKLocalizeRequest();
            imageRequest.token = mapper.token;
            imageRequest.fx = intrinsics.x;
            imageRequest.fy = intrinsics.y;
            imageRequest.ox = intrinsics.z;
            imageRequest.oy = intrinsics.w;
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
                int n = mapper.pcr.Count;

                imageRequest.mapIds = new SDKMapId[n];

                int count = 0;
                foreach (int id in mapper.pcr.Keys)
                {
                    imageRequest.mapIds[count] = new SDKMapId();
                    imageRequest.mapIds[count++].id = id;
                }
            }

            string jsonString = JsonUtility.ToJson(imageRequest);
            string endpoint = this.useGPS ? Endpoint.SERVER_GEOLOCALIZE : Endpoint.SERVER_LOCALIZE;

            SDKLocalizeResult locResult = new SDKLocalizeResult();
            locResult.success = false;
            Matrix4x4 m = new Matrix4x4();
            Matrix4x4 cloudSpace = new Matrix4x4();

            using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, mapper.server, endpoint), jsonString))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
                request.useHttpContinue = false;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError(request.error);
                }
                else if (request.responseCode == (long)HttpStatusCode.OK)
                {
                    locResult = JsonUtility.FromJson<SDKLocalizeResult>(request.downloadHandler.text);

                    if (locResult.success)
                    {
                        cloudSpace = Matrix4x4.identity;
                        cloudSpace.m00 = locResult.r00; cloudSpace.m01 = locResult.r01; cloudSpace.m02 = locResult.r02; cloudSpace.m03 = locResult.px;
                        cloudSpace.m10 = locResult.r10; cloudSpace.m11 = locResult.r11; cloudSpace.m12 = locResult.r12; cloudSpace.m13 = locResult.py;
                        cloudSpace.m20 = locResult.r20; cloudSpace.m21 = locResult.r21; cloudSpace.m22 = locResult.r22; cloudSpace.m23 = locResult.pz;
                        Matrix4x4 trackerSpace = Matrix4x4.TRS(position, rotation, Vector3.one);
                        mapper.stats.locSucc++;

                        Debug.Log("*************************** On-Server Localization Succeeded ***************************");
                        Debug.Log("fc 4x4\n" + cloudSpace + "\n" +
                                  "ft 4x4\n" + trackerSpace);

                        m = trackerSpace * (cloudSpace.inverse);

                        foreach (KeyValuePair<int, PointCloudRenderer> p in mapper.pcr)
                        {
                            if (p.Key == locResult.map)
                            {
                                p.Value.go.transform.position = m.GetColumn(3);
                                p.Value.go.transform.rotation = m.rotation;
                                break;
                            }
                        }

                        Debug.Log(locResult.error);
                    }
                    else
                    {
                        mapper.stats.locFail++;
                        Debug.Log("*************************** On-Server Localization Failed ***************************");
                    }
                }
            }

            if (locResult.success)
            {
                SDKEcefRequest ecefRequest = new SDKEcefRequest();
                ecefRequest.token = mapper.token;
                ecefRequest.id = locResult.map;

                using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, mapper.server, Endpoint.SERVER_ECEF), JsonUtility.ToJson(ecefRequest)))
                {
                    request.method = UnityWebRequest.kHttpVerbPOST;
                    request.useHttpContinue = false;
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Accept", "application/json");
                    yield return request.SendWebRequest();

                    if (request.isNetworkError || request.isHttpError)
                    {
                        Debug.LogError(request.error);
                    }
                    else if (request.responseCode == (long)HttpStatusCode.OK)
                    {
                        SDKEcefResult result = JsonUtility.FromJson<SDKEcefResult>(request.downloadHandler.text);

                        Debug.Log(request.downloadHandler.text);

                        LocalizerPose lastLocalizedPose;
                        BaseLocalizer.GetLocalizerPose(out lastLocalizedPose, locResult.map, cloudSpace.GetColumn(3), cloudSpace.rotation, m.inverse, result.ecef);
                        mapper.lastLocalizedPose = lastLocalizedPose;
                    }
                }
            }
        }
    }

    public class CoroutineJobListJobs : CoroutineJob
    {
        public double latitude = 0.0;
        public double longitude = 0.0;
        public double radius = 0.0;
        public bool useGPS = false;
        public List<int> activeMaps;

        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobListJobs ***************************");

            BaseMapper mapper = host as BaseMapper;
            SDKJobsRequest r = this.useGPS ? new SDKGeoJobsRequest() : new SDKJobsRequest();
            r.token = mapper.token;
            r.bank = mapper.currentBank;

            if (this.useGPS)
            {
                SDKGeoJobsRequest gr = r as SDKGeoJobsRequest;
                gr.latitude = this.latitude;
                gr.longitude = this.longitude;
                gr.radius = this.radius;
            }

            string jsonString = JsonUtility.ToJson(r);
            string endpoint = this.useGPS ? Endpoint.LIST_GEOJOBS : Endpoint.LIST_JOBS;

            using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, mapper.server, endpoint), jsonString))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
                request.useHttpContinue = false;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError(request.error);
                }
                else if (request.responseCode == (long)HttpStatusCode.OK)
                {
                    SDKJobsResult result = JsonUtility.FromJson<SDKJobsResult>(request.downloadHandler.text);
                    if (result.error == "none")
                    {
                        mapper.visualizeManager.SetSelectSlotData(result.jobs, activeMaps);
                    }
                }
            }
        }
    }

    public class CoroutineJobLoadMap : CoroutineJob
    {
        public int id;
        public GameObject go;

        private string MD5(byte[] bytes)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] hashBytes = md5.ComputeHash(bytes);

            string hashString = "";

            for (int i = 0; i < hashBytes.Length; i++)
                hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');

            return hashString.PadLeft(32, '0');
        }

        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobLoadMap ***************************");

            BaseMapper mapper = host as BaseMapper;
            SDKMapRequest r = new SDKMapRequest();
            r.token = mapper.token;
            r.id = this.id;

            string jsonString2 = JsonUtility.ToJson(r);
            using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, mapper.server, Endpoint.LOAD_MAP), jsonString2))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
                request.useHttpContinue = false;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                //Debug.Log("Response code: " + request.responseCode);

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError(request.error);
                }
                else if (request.responseCode == (long)HttpStatusCode.OK)
                {
                    SDKMapResult result = JsonUtility.FromJson<SDKMapResult>(request.downloadHandler.text);
                    if (result.error == "none")
                    {
                        byte[] mapData = Convert.FromBase64String(result.b64);
                        Debug.Log("Load map " + this.id + " (" + mapData.Length + " bytes) (" + MD5(mapData) + "/" + result.md5_al + ")");

                        uint countMax = 16*1024;
                        Vector3[] vector3Array = new Vector3[countMax];

                        Task<int> t0 = Task.Run(() =>
                        {
                            return Immersal.Core.LoadMap(mapData);
                        });
                         
                        while (!t0.IsCompleted)
                        {
                            yield return null;
                        }

                        int mapId = t0.Result;

                        Debug.Log("mapId " + mapId);

                        Task<int> t1 = Task.Run(() =>
                        {
                            return Immersal.Core.GetPointCloud(mapId, vector3Array);
                        });

                        while (!t1.IsCompleted)
                        {
                            yield return null;
                        }

                        int num = t1.Result;

                        Debug.Log("map points: " + num);

                        PointCloudRenderer renderer = go.AddComponent<PointCloudRenderer>();
                        renderer.CreateCloud(vector3Array, num);
                        renderer.mapId = mapId;
                        if (!mapper.pcr.ContainsKey(id)) {
                            mapper.pcr.Add(id, renderer);
                        }

                        mapper.stats.locFail = 0;
                        mapper.stats.locSucc = 0;

                        VisualizeManager.loadJobs.Remove(mapId);
                    }
                }
            }
        }
    }

    public class CoroutineJobFreeMap : CoroutineJob
    {
        public int id;

        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobFreeMap ***************************");

            BaseMapper mapper = host as BaseMapper;

            if (mapper.pcr.ContainsKey(id))
            {
                int mapId = mapper.pcr[id].mapId;

                Task<int> t0 = Task.Run(() =>
                {
                    return Immersal.Core.FreeMap(mapId);
                });

                while (!t0.IsCompleted)
                {
                    yield return null;
                }

                PointCloudRenderer p = mapper.pcr[id];
                p.ClearCloud();
                mapper.pcr.Remove(id);
            }
        }
    }

    public class MapperStats
    {
        public int queueLen;
        public int imageCount;
        public int locFail;
        public int locSucc;
    }
}
