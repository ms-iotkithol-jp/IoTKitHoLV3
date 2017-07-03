# Preparation 
Copy iotkithol_iothub_dm into Azure IoT SDK's iothub_client/samples directory  
Add  descriptions at bottom of iothub_client/samples/CMakeList.txt  

Before 
if(${use_mqtt}) 
    add_sample_directory(iothub_client_sample_mqtt) 
    add_sample_directory(iothub_client_sample_device_method) 
	if (${use_wsio}) 
		add_sample_directory(iothub_client_sample_mqtt_websockets) 
	endif() 
endif() 

After 
if(${use_mqtt}) 
    add_sample_directory(iothub_client_sample_mqtt) 
    add_sample_directory(iothub_client_sample_device_method) 
	if (${use_wsio}) 
		add_sample_directory(iothub_client_sample_mqtt_websockets) 
	endif() 
    *add_sample_directory(iotkithol_iothub_dm)* 
endif() 

## Edit file
open iothub_client/samples/iotkithol_iothub_dm/iotkithol_iothub_dm.c  
set IoT Hub connection information, IoT Hub endpoint,  and save it.

static const char* connectionString = "**[device connection string]**";

## Build & Run  
at the top of Azure IoT SDK directory.  

> cd azure-iot-sdk-c/build_all/linux  
> ./build.sh  

Then whole libraries and samples will be build and stored under cmake/iotsdk_linux.
You can run the sample. 
> cd cmake/iotsdk_linux/iothub_client/samples/iotkithol_iothub_dm  
> ./iotkithol_iothub_dm

When you want to build this sample after only iotkithol_iothub_dm.c/h editing

> cd cmake/iotsdk_linux/iothub_client/samples/iotkithol_iothub_dm  
> make

## Debug 
When you use gdb and SIGILL happens. 
Please input magic word. 

(gdb) *handle SIGILL nostop*

