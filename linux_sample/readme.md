Copy iotkithol_sample_http and raspbberrypi_photoupload into Azure IoT SDK's iothub_client/samples directory  
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

