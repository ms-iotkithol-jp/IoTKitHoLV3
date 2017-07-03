#Copyright (c) Microsoft. All rights reserved.
#Licensed under the MIT license. See LICENSE file in the project root for full license information.

#this is CMakeLists.txt for iotkithol_sample_http

compileAsC99()

## IF(LINUX)
	find_package(PkgConfig REQUIRED)
	pkg_search_module(GWGIOUNIX REQUIRED gio-unix-2.0)
	include_directories(${GWGIOUNIX_INCLUDE_DIRS})
	set(LIBS ${GWGIOUNIX_LIBRARIES})
	add_definitions(-D_POSIX_C_SOURCE=1)
## ENDIF(LINUX)



if(NOT ${use_http})
	message(FATAL_ERROR "iotkithol_sample_http being generated without HTTP support")
endif()

set(iotkithol_sample_http_c_files
iotkithol_sample_http.c
)

set(iotkithol_sample_http_h_files
iotkithol_sample_http.h
)

IF(WIN32)
	#windows needs this define
	add_definitions(-D_CRT_SECURE_NO_WARNINGS)
	add_definitions(-DGB_MEASURE_MEMORY_FOR_THIS -DGB_DEBUG_ALLOC)
ENDIF(WIN32)

include_directories(.)

add_executable(iotkithol_sample_http ${iotkithol_sample_http_c_files} ${iotkithol_sample_http_h_files})

target_link_libraries(iotkithol_sample_http 
#iothubclient is here only because locking... in gballoc no less.
	iothub_client
	iothub_client_http_transport
	${LIBS}
)

linkSharedUtil(iotkithol_sample_http)
linkHttp(iotkithol_sample_http)