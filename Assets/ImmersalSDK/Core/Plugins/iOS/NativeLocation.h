//
//  NativeLocation.h
//  Immersal SDK
//
//  Created by Mikko on 29/05/2020.
//
//

#import <Foundation/Foundation.h>
#import <CoreLocation/CoreLocation.h>
#import <UIKit/UIKit.h>

@interface NativeLocation : NSObject <CLLocationManagerDelegate>

- (NativeLocation *)init;
- (void)locationManager:(CLLocationManager *)manager didUpdateLocations:(NSArray *)locations;

@end
