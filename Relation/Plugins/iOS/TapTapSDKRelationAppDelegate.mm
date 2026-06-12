#import <UIKit/UIKit.h>
#import <Foundation/Foundation.h>
#import <TapTapRelationSDK/TapTapRelationSDK-Swift.h>
#import "UnityAppController.h"
#import "AppDelegateListener.h"

@interface TapTapSDKRelationAppDelegateListener : NSObject<AppDelegateListener>

+ (instancetype)sharedInstance;

@end

@implementation TapTapSDKRelationAppDelegateListener

+ (instancetype)sharedInstance {
    static TapTapSDKRelationAppDelegateListener *sharedInstance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        sharedInstance = [[TapTapSDKRelationAppDelegateListener alloc] init];
    });
    return sharedInstance;
}

- (void)onOpenURL:(NSNotification *)notification {
    NSDictionary *userInfo = notification.userInfo;
    NSURL *url = [userInfo valueForKey:@"url"];
    if (url) {
        [TapTapRelation openWithUrl:url];
    }
}

@end

extern "C" void RegisterTapTapSDKRelationAppDelegateListener() {
    TapTapSDKRelationAppDelegateListener *listener = TapTapSDKRelationAppDelegateListener.sharedInstance;
    UnityRegisterAppDelegateListener(listener);
}
