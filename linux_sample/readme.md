# Azure IoT SDK additional sample 
Sample apps run on Raspbian(it may run other Linux OS) 
- iotkithol_sample_http   - This app sends emulated acceleration and temperature at regular interval and can receive command from IoT Hub 
- iotkithol_sample_dm     - This app is device side sample of IoT Hub DM capability 
- raspberrypi_photoupLoad - Upload photo taken by Web Cam to storage blob via IoT Hub 

## Setup 
Follow this [link](https://github.com/Azure/azure-iot-gateway-sdk/blob/master/doc/devbox_setup.md#set-up-a-linux-development-environment) to set up the development environment and [Azure IoT SDK](http://github.com/Azure/azure-iot-sdk-c) 
After apt-get install and before github repository clone, please execute following additional library. 

sudo apt-get install libglib1.0-dev 

※ In the case of 

Copy iotkithol_sample_http, iotkithol_sample_dm and raspbberrypi_photoupload into Azure IoT SDK's iothub_client/samples directory  
Add add_CMakeList.txt descriptions at bottom of iothub_client/samples/CMakeList.txt  

Edit  
open iothub_client/samples/iotkithol_sample_http/iotkithol_sample_http.c  
set IoT Hub connection information, IoT Hub endpoint, deviceId and deviceKey and save it.

Build & Run  
at the top of Azure IoT SDK directory.  

> cd azure-iot-sdk-c/  
> build_all/linux/build.sh --no-mqtt  
> cd cmake/iotsdk_linux/iothub_client/samples/iotkithol_sample_http  
> ./iotkithol_sample_http

or 

> cd cmake/iotsdk_linux/iothub_client/samples/raspbberrypi_photoupload  
> ./raspbberrypi_photoupload

Please see <https://doc.co/mtf3bT/NsXXfD> and <http://aka.ms/IoTKitHoLV3On> 

About iotkithol_iothub_dm please refere iotkithol_iothub_dm/readme.md 

