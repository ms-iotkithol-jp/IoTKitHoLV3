Copy iotkithol_sample_http and raspbberrypi_photoupload into Azure IoT SDK's c/iothub_client/samples directory  
Add add_CMakeList.txt descriptions at bottom of c/iothub_client/samples/CMakeList.txt  

Edit  
open iothub_client/samples/iotkithol_sample_http/iotkithol_sample_http.c  
set IoT Hub connection information, IoT Hub endpoint, deviceId and deviceKey and save it.

Build & Run  
at the top of Azure IoT SDK directory.  

> cd ./c/  
> build_all/linux/build.sh --skip-unittests  
> cd ./c/cmake/iotsdk_linux/iothub_client/samples/iotkithol_sample_http  
> ./iotkithol_sample_http

or 

> cd ./c/cmake/iotsdk_linux/iothub_client/samples/raspbberrypi_photoupload  
> ./raspbberrypi_photoupload

Please see <https://doc.co/mtf3bT/NsXXfD> and <http://aka.ms/IoTKitHoLV3On>
