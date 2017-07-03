#Copyright (c) Microsoft. All rights reserved.
#Licensed under the MIT license. See LICENSE file in the project root for full license information.

#this is CMakeLists.txt for raspberrypi_photoupload

compileAsC99()

if(NOT ${use_http})
	message(FATAL_ERROR "raspberrypi_photoupload being generated without HTTP support")
endif()

set(raspberrypi_photoupload_c_files
raspberrypi_photoupload.c
)

if(WIN32)
else()
	set(raspberrypi_photoupload_c_files ${raspberrypi_photoupload_c_files} ./linux/main.c)
endif()


set(raspberrypi_photoupload_h_files
raspberrypi_photoupload.h
)

include_directories(.)

add_executable(raspberrypi_photoupload ${raspberrypi_photoupload_c_files} ${raspberrypi_photoupload_h_files})

target_link_libraries(raspberrypi_photoupload 
#iothubclient is here only because locking... in gballoc no less.
	iothub_client
	iothub_client_http_transport
)

linkSharedUtil(raspberrypi_photoupload)
linkHttp(raspberrypi_photoupload)