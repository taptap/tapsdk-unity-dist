#pragma once

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