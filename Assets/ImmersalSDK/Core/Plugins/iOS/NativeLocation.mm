//
//  NativeLocation.m
//  Immersal SDK
//
//  Created by Mikko on 29/05/2020.
//
//

#import "NativeLocation.h"

double latitude;
double longitude;
double altitude;
double haccuracy;
double vaccuracy;

@implementation NativeLocation

CLLocationManager *locationManager;
static bool isEnabled = NO;

- (NativeLocation *)init
{
    locationManager = [[CLLocationManager alloc] init];
    locationManager.delegate = self;
    locationManager.distanceFilter = kCLDistanceFilterNone;
    locationManager.desiredAccuracy = kCLLocationAccuracyBest;
    
    if ([[[UIDevice currentDevice] systemVersion] floatValue] >= 8.0)
        [locationManager requestWhenInUseAuthorization];
        
    return self;
}

- (void)locationManager:(CLLocationManager *)manager didChangeAuthorizationStatus:(CLAuthorizationStatus)status;
{
/*    switch (status) {
        case kCLAuthorizationStatusAuthorizedWhenInUse:
        case kCLAuthorizationStatusAuthorizedAlways:
            isEnabled = YES; break;
        default:
            isEnabled = NO; break;
    }*/
}

- (void)locationManager:(CLLocationManager*)manager didFailWithError:(NSError*)error;
{
    isEnabled = NO;
}

- (void)locationManager:(CLLocationManager *)manager didUpdateLocations:(NSArray *)locations;
{
    CLLocation *location = [locations lastObject];
    latitude = location.coordinate.latitude;
    longitude = location.coordinate.longitude;
    altitude = location.altitude;
    haccuracy = location.horizontalAccuracy;
    vaccuracy = location.verticalAccuracy;
    
    isEnabled = YES;
    
    //NSLog(@"lat: %f long: %f alt: %f", latitude, longitude, altitude);
}

- (void)start
{
    if (locationManager != NULL) {
        [locationManager startUpdatingLocation];
    }
}

- (void)stop
{
    if (locationManager != NULL) {
        [locationManager stopUpdatingLocation];
    }
    
    isEnabled = NO;
}

@end

static NativeLocation* locationDelegate = NULL;

extern "C"
{
    void startLocation()
    {
        if (locationDelegate == NULL) {
            locationDelegate = [[NativeLocation alloc] init];
        }
        
        [locationDelegate start];
    }

    void stopLocation()
    {
        if (locationDelegate != NULL) {
            [locationDelegate stop];
        }
    }
        
    double getLatitude()
    {
        return latitude;
    }

    double getLongitude()
    {
        return longitude;
    }

    double getAltitude()
    {
        return altitude;
    }

    double getHorizontalAccuracy()
    {
        return haccuracy;
    }

    double getVerticalAccuracy()
    {
        return vaccuracy;
    }

    bool locationServicesEnabled()
    {
        return isEnabled;
    }
}
