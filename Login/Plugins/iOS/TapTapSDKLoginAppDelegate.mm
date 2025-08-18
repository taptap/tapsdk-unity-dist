
#import <UIKit/UIKit.h>
#import <Foundation/Foundation.h>
#import <TapTapBasicToolsSDK/TapTapSDKVerifyProtocol.h>
#import <TapTapLoginSDK/TapTapLoginSDK-Swift.h>
#import "UnityAppController.h"
#import <UIKit/UIKit.h>
#import "AppDelegateListener.h"

@interface TapTapSDKLoginAppDelegateListener : NSObject<AppDelegateListener>

+ (instancetype)sharedInstance;
@end

@implementation TapTapSDKLoginAppDelegateListener

+ (instancetype)sharedInstance {
    static TapTapSDKLoginAppDelegateListener *sharedInstance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        sharedInstance = [[TapTapSDKLoginAppDelegateListener alloc] init];
    });
    return sharedInstance;
}

- (void)onOpenURL:(NSNotification *)notification {
    NSDictionary *userInfo = notification.userInfo;
    NSURL *url = [userInfo valueForKey:@"url"];
    if (url) {
        [TapTapLogin openWithUrl:url];
    }
    
}

@end

extern "C" void RegisterTapTapSDKLoginAppDelegateListener() {
    TapTapSDKLoginAppDelegateListener *listener = TapTapSDKLoginAppDelegateListener.sharedInstance;
    UnityRegisterAppDelegateListener(listener);
}

