/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System;

namespace Immersal.REST
{
    public static class Endpoint
    {
        public const string URL_FORMAT             = "{0}/{1}";
        public const string CAPTURE_IMAGE          = "captureb64";
        public const string CONSTRUCT_MAP          = "construct";
        public const string LOAD_MAP               = "mapb64";
        public const string DOWNLOAD_SPARSE        = "sparse";
        public const string LOGIN                  = "login";
        public const string LIST_JOBS              = "list";
        public const string CLEAR_JOB              = "clear";
        public const string STATUS                 = "status";
        public const string DOWNLOAD_DENSE         = "dense";
        public const string DELETE_MAP             = "delete";
        public const string SERVER_LOCALIZE        = "localizeb64";
        public const string RESTORE_MAP_IMAGES     = "restore";
        public const string LIST_GEOJOBS           = "geolist";
        public const string SERVER_GEOLOCALIZE     = "geolocalizeb64";
        public const string SERVER_ECEF            = "ecef";
        public const string DOWNLOAD_TEXTURED      = "tex";
        public const string CAPTURE_IMAGE_BIN      = "capture";
        public const string SERVER_LOCALIZE_BIN    = "localize";
        public const string SERVER_GEOLOCALIZE_BIN = "geolocalize";
    }

    [Serializable]
    public class SDKRequestBase
    {
        public string token;
        public int bank = 0;
    }

    [Serializable]
    public class SDKResultBase
    {
        public string error;
    }

    [Serializable]
    public class SDKLoginRequest
    {
        public string login;
        public string password;
    }

    [Serializable]
    public class SDKLoginResult : SDKResultBase
    {
        public string token;
        public int banks;
    }

    [Serializable]
    public class SDKClearRequest : SDKRequestBase
    {
        public bool anchor;
    }

    [Serializable]
    public class SDKClearResult : SDKResultBase
    {

    }

    [Serializable]
    public class SDKConstructRequest : SDKRequestBase
    {
        public string name;
        public int featureCount;
        public bool preservePoses;
        public int windowSize;
    }

    [Serializable]
    public class SDKConstructResult : SDKResultBase
    {
        public int id;
        public int size;
    }

    [Serializable]
    public class SDKJob
    {
        public int id;
        public int size;
        public int bank;
        public string work;
        public string status;
        public string server;
        public string name;
        public double latitude;
        public double longitude;
        public double altitude;
        public string created;
        public string modified;
    }

    [Serializable]
    public class SDKStatusRequest : SDKRequestBase
    {
        
    }

    [Serializable]
    public class SDKStatusResult : SDKResultBase
    {
        public int imageCount;
        public int bankMax;
        public int imageMax;
        public bool eulaAccepted;
    }

    [Serializable]
    public class SDKJobsRequest : SDKRequestBase
    {

    }

    [Serializable]
    public class SDKGeoJobsRequest : SDKJobsRequest
    {
        public double latitude;
        public double longitude;
        public double radius;
    }

    [Serializable]
    public class SDKJobsResult : SDKResultBase
    {
        public int count;
        public SDKJob[] jobs;
    }

    [Serializable]
    public class SDKImageRequest : SDKRequestBase
    {
        public int run;
        public int index;
        public bool anchor;
        public double px;
        public double py;
        public double pz;
        public double r00;
        public double r01;
        public double r02;
        public double r10;
        public double r11;
        public double r12;
        public double r20;
        public double r21;
        public double r22;
        public double fx;
        public double fy;
        public double ox;
        public double oy;
        public double latitude;
        public double longitude;
        public double altitude;
    }

    [Serializable]
    public class SDKImageResult : SDKResultBase
    {
        public string path;
    }
     
    [Serializable]
    public class SDKMapId
    {
        public int id;
    }

    [Serializable]
    public class SDKGeoLocalizeRequest : SDKLocalizeRequest
    {
        public double latitude;
        public double longitude;
        public double radius;
    }

    [Serializable]
    public class SDKLocalizeRequest : SDKRequestBase
    {
        public double fx;
        public double fy;
        public double ox;
        public double oy;
        public int param1;
        public int param2;
        public double param3;
        public double param4;
        public SDKMapId[] mapIds;
    }

    [Serializable]
    public class SDKLocalizeResult : SDKResultBase
    {
        public bool success;
        public int map;
        public float px;
        public float py;
        public float pz;
        public float r00;
        public float r01;
        public float r02;
        public float r10;
        public float r11;
        public float r12;
        public float r20;
        public float r21;
        public float r22;
    }

    [Serializable]
    public class SDKEcefRequest : SDKRequestBase
    {
        public int id;
    }

    [Serializable]
    public class SDKEcefResult : SDKResultBase
    {
        public double[] ecef;
    }

    [Serializable]
    public class SDKMapRequest : SDKRequestBase
    {
        public int id;
    }

    [Serializable]
    public class SDKMapResult : SDKResultBase
    {
        public string sha256_al;
        public string b64;
    }

    [Serializable]
    public class SDKDeleteMapRequest : SDKRequestBase
    {
        public int id;
    }

    [Serializable]
    public class SDKDeleteMapResult : SDKResultBase
    {

    }

    [Serializable]
    public class SDKRestoreMapImagesRequest : SDKRequestBase
    {
        public int id;
    }

    [Serializable]
    public class SDKRestoreMapImagesResult : SDKResultBase
    {

    }
}