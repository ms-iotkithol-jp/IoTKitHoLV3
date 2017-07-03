// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifdef DONT_USE_UPLOADTOBLOB
#error "trying to compile iothub_client_sample_upload_to_blob.c while DONT_USE_UPLOADTOBLOB is #define'd"
#else

#include <stdio.h>
#include <stdlib.h>
#include <time.h>
#include <sys/time.h>
#include <unistd.h>

#ifdef ARDUINO
#include "AzureIoT.h"
#else
#include "iothub_client.h"
#include "iothub_message.h"
#include "azure_c_shared_utility/crt_abstractions.h"
#include "iothubtransporthttp.h"
#include "azure_c_shared_utility/platform.h"
#endif

#ifdef MBED_BUILD_TIMESTAMP
#include "certs.h"
#endif // MBED_BUILD_TIMESTAMP

typedef struct IoTKitHoLContext_tag {
	IOTHUB_CLIENT_HANDLE iotHubClientHandle;
	void* messageLoop;
} IoTKitHoLContext;

/*String containing Hostname, Device Id & Device Key in the format:                         */
/*  "HostName=<host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"                */
//static const char* connectionString = "<<device connection string>>";
//static const char* deviceId = "<<device_id>>";
static const char* connectionString = "<< IoT Hub Connection String >>";
static const char* deviceId = "<< Device Id >>";
int duration = 60;

const unsigned char* take_photo(long* contentLength)
{
	int status;
	FILE *fp;

	unsigned char* buffer = NULL;
	unsigned char* fileName = "tmpphoto.jpg";
	char* command = "/usr/bin/fswebcam -r 1280x720 tmpphoto.jpg";
	system(command);

	while (true) {
		if ((fp = fopen(fileName, "rb")) != NULL) {
			break;
		}
		sleep(1);
	}
	fseek(fp, 0, SEEK_END);
	long fileSize = ftell(fp);
	fseek(fp, 0, SEEK_SET);
	if (fileSize > 0) {
		buffer = (unsigned char*)malloc(fileSize);
		fread(buffer, fileSize, 1, fp);
	}
	*contentLength = fileSize;
	fclose(fp);
	remove(fileName);

	return buffer;
}

void UploadCallback(IOTHUB_CLIENT_FILE_UPLOAD_RESULT result, void* userContext)
{
	printf("Upload status - %d\r\n", result);
}

void raspberrypi_photoupload_run(void* messageLoop)
{
	IoTKitHoLContext iotKitHoLContext;
	IOTHUB_CLIENT_HANDLE iotHubClientHandle;

	iotKitHoLContext.messageLoop = messageLoop;

	time_t now;
	struct tm *local;
	char* photoFileName;
	const unsigned char* photoContent;
	long contentLength;

	if (platform_init() != 0)
	{
		printf("Failed to initialize the platform.\r\n");
	}
	else
	{
		(void)printf("Starting the IoTHub client sample upload to blob...\r\n");

		if ((iotHubClientHandle = IoTHubClient_CreateFromConnectionString(connectionString, HTTP_Protocol)) == NULL)
		{
			(void)printf("ERROR: iotHubClientHandle is NULL!\r\n");
		}
		else
		{
			iotKitHoLContext.iotHubClientHandle = iotHubClientHandle;
			printf("Connected to IoT Hub\r\n");
			while (true) {
				photoContent = take_photo(&contentLength);
				if (photoContent != NULL) {
					now = time(NULL);
					local = localtime(&now);

					photoFileName = (char*)malloc(strlen(deviceId) + 32);
					sprintf(photoFileName, "%s_%04d_%02d_%02d_%02d_%02d_%02d_Pro.jpg", deviceId, local->tm_year+1900, local->tm_mon+1, local->tm_mday, local->tm_hour, local->tm_min, local->tm_sec);
					printf("Try to upload as %s size is %d\r\n", photoFileName, contentLength);
					if (IoTHubClient_UploadToBlobAsync(iotHubClientHandle, photoFileName, photoContent, contentLength, UploadCallback, &iotKitHoLContext) != IOTHUB_CLIENT_OK)
					{
						printf("Failed to upload picture\r\n");
					}
					else
					{
						printf("Uploaded %s %d bytes.\r\n", photoFileName, contentLength);
					}
					free((void*)photoFileName);
					free((void*)photoContent);
				}
				else {
					printf("Failed to take a picture\r\n");
				}
				sleep(duration);
			}
			IoTHubClient_Destroy(iotHubClientHandle);
		}
		platform_deinit();
	}
}

#endif /*DONT_USE_UPLOADTOBLOB*/
