#Copyright (c) Microsoft. All rights reserved.
#Licensed under the MIT license. See LICENSE file in the project root for full license information.
# Modified by H. Ota
#this is CMakeLists.txt for raspberrypi_photoupload

cmake_minimum_required(VERSION 2.8.11)

if(WIN32)
    message(FATAL_ERROR "This CMake file is only support Linux builds!")
endif()

set(AZUREIOT_INC_FOLDER ".." "/usr/include/azureiot")

include_directories(${AZUREIOT_INC_FOLDER})

set(raspberrypi_photoupload_c_files
    ../raspberrypi_photoupload.c
    ./main.c
)

set(raspberrypi_photoupload_h_files
    ../raspberrypi_photoupload.h
)

add_executable(raspberrypi_photoupload ${raspberrypi_photoupload_c_files} ${raspberrypi_photoupload_h_files})

target_link_libraries(raspberrypi_photoupload
    iothub_client
    iothub_client_http_transport
    aziotsharedutil
    pthread
    curl
    ssl
    crypto
)
