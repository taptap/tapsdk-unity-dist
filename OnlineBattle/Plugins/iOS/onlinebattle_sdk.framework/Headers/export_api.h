#pragma once

#include <stdint.h>

#if defined(_WIN32) || defined(__CYGWIN__)
#ifdef EXPORT_TDK_CPP
#define TAPSDK_EXPORT_API __declspec(dllexport)
#elif defined(TAPSDK_SHARED_LIB)
#define TAPSDK_EXPORT_API __declspec(dllimport)
#else
#define TAPSDK_EXPORT_API
#endif
#else
#ifdef EXPORT_TDK_CPP
#define TAPSDK_EXPORT_API __attribute__((visibility("default")))
#else
#define TAPSDK_EXPORT_API
#endif
#endif

#ifdef __cplusplus
extern "C" {
#endif

// logLevel - 日志等级：1 trace、2 debug、3 info、4 warn、5 error
// codeLocation - 输出日志的代码位置，如：filename:line_number
// logTag - 日志标签，如：模块名、功能名等
// logMessage - 日志内容
typedef void (*TapSdkCppLogWriter)(int32_t logLevel, const char* codeLocation, const char* logTag, const char* logMessage);

#ifdef __cplusplus
}
#endif