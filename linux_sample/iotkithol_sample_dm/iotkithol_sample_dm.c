// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <stdio.h>
#include <stdlib.h>

#include <sys/signalfd.h>
#include <unistd.h>
#include <signal.h>
#include <time.h>

#include <glib.h>

#include "serializer_devicetwin.h"
#include "iothub_client.h"
#include "iothubtransportmqtt.h"
#include "platform.h"
#include "azure_c_shared_utility/threadapi.h"
#include "parson.h"

/*String containing Hostname, Device Id & Device Key in the format:                         */
/*  "HostName=<host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"                */
static const char* connectionString = "[device connection string]";

static int callbackCounter;
static bool g_continueRunning;
static char msgText[1024];
static char propText[1024];

#define SERVER_ERROR 500
#define NOT_IMPLEMENTED 501
#define NOT_VALID 400
#define SERVER_SUCCESS 200

// Define the Model - it is a guitar.
BEGIN_NAMESPACE(IoTKitHoL);

DECLARE_STRUCT(Maker,
    ascii_char_ptr, makerName, /*Gibson, Martin ... */
    ascii_char_ptr, guitarType, /* Stratocaster, Les Paul ... */
    int, year
);

DECLARE_STRUCT(Geo,
    double, longitude,
    double, latitude
);

DECLARE_MODEL(GuitarState,
    WITH_REPORTED_PROPERTY(int32_t, softwareVersion),
    WITH_REPORTED_PROPERTY(uint8_t, reported_batteryLevel),
    WITH_REPORTED_PROPERTY(uint8_t, reported_volumeLevel),
    WITH_REPORTED_PROPERTY(uint8_t, reported_toneLevel)
);

DECLARE_MODEL(GuitarSettings,
    WITH_DESIRED_PROPERTY(uint8_t, desired_volumeLevel, onDesiredVolumeLevel),
    WITH_DESIRED_PROPERTY(Geo, location)
);

DECLARE_DEVICETWIN_MODEL(Guitar,
    WITH_REPORTED_PROPERTY(ascii_char_ptr, lastStringsChangeDate), /*this is a simple reported property*/
    WITH_DESIRED_PROPERTY(ascii_char_ptr, changeStringsReminder),
    
    WITH_REPORTED_PROPERTY(Maker, maker), /*this is a structured reported property*/
    WITH_REPORTED_PROPERTY(GuitarState, state), /*this is a model in model*/
    WITH_DESIRED_PROPERTY(GuitarSettings, settings), /*this is a model in model*/
    WITH_METHOD(getGuitarVIN)
);

END_NAMESPACE(IoTKitHoL);

DEFINE_ENUM_STRINGS(DEVICE_TWIN_UPDATE_STATE, DEVICE_TWIN_UPDATE_STATE_VALUES);

METHODRETURN_HANDLE getGuitarVIN(Guitar* guitar)
{
    (void)(guitar);
    /*Guitar VINs are JSON strings, for example: 1HGCM82633A004352*/
    METHODRETURN_HANDLE result = MethodReturn_Create(201, "\"1HGCM82633A004352\"");
    return result;
}

void onDesiredVolumeLevel(void* argument)
{
    /*by convention 'argument' is of the type of the MODEL encompassing the desired property*/
    /*in this case, it is 'GuitarSettings'*/

    GuitarSettings* guitar = argument;
    printf("received a new desired_volumeLevel = %" PRIu8 "\n", guitar->desired_volumeLevel);

}



//DEFINE_ENUM_STRINGS(IOTHUB_CLIENT_CONFIRMATION_RESULT, IOTHUB_CLIENT_CONFIRMATION_RESULT_VALUES);

guint g_event_source_id = 0;
static gboolean signal_handler(
	GIOChannel *channel,
	GIOCondition condition,
	gpointer user_data
);
static void handle_control_c(GMainLoop* loop);


typedef struct IoTKitHoLContext_tag {
	IOTHUB_CLIENT_HANDLE iotHubClientHandle;
	GMainLoop* messageLoop;
    Guitar* things[1];
} IoTKitHoLContext;

static int DeviceMethodCallback(const char* method_name, const unsigned char* payload, size_t size, unsigned char** response, size_t* resp_size, void* userContextCallback)
{
	IoTKitHoLContext* context = (IoTKitHoLContext*)userContextCallback;
	Guitar* guitar =     context->things[0];
    int retValue;
    
    if (method_name == NULL)
    {
        LogError("invalid method name");
        retValue = NOT_VALID;
    }
    else if ((response == NULL) || (resp_size == NULL))
    {
        LogError("invalid response parameters");
        retValue = NOT_VALID;
    }
    else if (guitar == NULL)
    {
        LogError("invalid user context callback data");
        retValue = NOT_VALID;
    }
    else {
        LogInfo("DeviceTwin Method CallBack: Method_name=%s, Payload=%s, size=%u", method_name, payload, size);
        retValue=SERVER_SUCCESS;
    }
    return retValue;
}

static void DeviceTwinPropertyCallback(int status_code, void* userContextCallback)
{
    (void)(userContextCallback);
    LogInfo("DeviceTwin CallBack: Status_code = %u", status_code);
}

static void DeviceTwinCallback(DEVICE_TWIN_UPDATE_STATE update_state, const unsigned char* payLoad, size_t size, void* userContextCallback)
{
	IoTKitHoLContext* context = (IoTKitHoLContext*)userContextCallback;

}

void iotkithol_sample_mqtt_dm_run(GMainLoop* messageLoop)
{
	IoTKitHoLContext iotKitHoLContext;
	IOTHUB_CLIENT_HANDLE iotHubClientHandle;

	iotKitHoLContext.messageLoop = messageLoop;

	g_continueRunning = true;

	srand((unsigned int)time(NULL));

	callbackCounter = 0;

	if (platform_init() != 0)
	{
		printf("Failed to initialize the platform.\r\n");
	}
	else
	{
        if (SERIALIZER_REGISTER_NAMESPACE(IoTKitHoL) == NULL)
        {
            LogError("unable to SERIALIZER_REGISTER_NAMESPACE");
        }
        else
        {
			(void)printf("Starting the IoTHub dm client sample MqTT - %s\r\n", connectionString);

			if ((iotHubClientHandle = IoTHubClient_CreateFromConnectionString(connectionString, MQTT_Protocol)) == NULL)
			{
				(void)printf("ERROR: iotHubClientHandle is NULL!\r\n");
			}
			else
			{
				iotKitHoLContext.iotHubClientHandle = iotHubClientHandle;
	
                // Turn on Log 
	            bool trace = true;
    	        (void)IoTHubClient_SetOption(iotHubClientHandle, "logtrace", &trace);

        	    Guitar* guitar = IoTHubDeviceTwin_CreateGuitar(iotHubClientHandle);
				if (guitar == NULL)
            	{
                	printf("Failure in IoTHubDeviceTwin_CreateGuitar");
            	}
            	else
            	{
                	iotKitHoLContext.things[0] = guitar;
                	if (IoTHubClient_SetDeviceMethodCallback(iotHubClientHandle, DeviceMethodCallback, &iotKitHoLContext) != IOTHUB_CLIENT_OK)
                	{
                    	LogError("failed to associate a callback for device methods");
                    	printf("Failure in associate a callback for device methods");
                	}
                	else{
                    	/*setting values for reported properties*/
                    	guitar->lastStringsChangeDate="2016/11/14";
	                    guitar->maker.makerName = "Martin";
    	                guitar->maker.guitarType = "Electric Acoustic";
        	            guitar->maker.year = 2014;
            	        guitar->state.reported_batteryLevel = 6;
                	    guitar->state.softwareVersion = 1;

						if(IoTHubClient_SetDeviceTwinCallback(iotHubClientHandle, DeviceTwinCallback, (void*)(&iotKitHoLContext))!=IOTHUB_CLIENT_OK)
						{
							printf("Failed set DeviceTwin callback");
						}

                    	/*sending the values to IoTHub*/
	                    if (IoTHubDeviceTwin_SendReportedStateGuitar(guitar, DeviceTwinPropertyCallback, NULL) != IOTHUB_CLIENT_OK)
            	        {
                	        (void)printf("Failed sending serialized reported state\n");
                    	}
                    	else
                    	{
                        	printf("Reported state will be send to IoTHub\n");
                    	}
	                
            
//				start_timers(&iotKitHoLContext);

						// run the glib loop
						g_main_loop_run(messageLoop);

					}
					IoTHubDeviceTwin_DestroyGuitar(iotKitHoLContext.things[0]);
				}
				IoTHubClient_Destroy(iotHubClientHandle);
			}
		}
		platform_deinit();
    }
}

void* start_message_loop()
{
	GMainLoop* loop = g_main_loop_new(NULL, FALSE);
	handle_control_c(loop);
	return loop;
}

int main(void)
{
	iotkithol_sample_mqtt_dm_run(start_message_loop());
	return 0;
}

static void handle_control_c(GMainLoop* loop)
{
	sigset_t mask;
	sigemptyset(&mask);
	sigaddset(&mask, SIGINT);
	sigaddset(&mask, SIGTERM);

	if (sigprocmask(SIG_BLOCK, &mask, NULL) < 0)
	{
		printf("Failed to set signal mask\r\n");
	}
	else
	{
		int fd = signalfd(-1, &mask, 0);
		if (fd < 0)
		{
			printf("Failed to create signal descriptor\r\n");
		}
		else
		{
			GIOChannel *channel = g_io_channel_unix_new(fd);
			if (channel == NULL)
			{
				close(fd);
				printf("Failed to create IO channel\r\n");
			}
			else
			{
				g_io_channel_set_close_on_unref(channel, TRUE);
				g_io_channel_set_encoding(channel, NULL, NULL);
				g_io_channel_set_buffered(channel, FALSE);

				g_event_source_id = g_io_add_watch(
					channel,
					G_IO_IN | G_IO_HUP | G_IO_ERR | G_IO_NVAL,
					signal_handler,
					loop
				);

				if (g_event_source_id == 0)
				{
					printf("g_io_add_watch failed\r\n");
				}

				g_main_loop_ref(loop);
				g_io_channel_unref(channel);
			}
		}
	}
}

static gboolean signal_handler(
	GIOChannel *channel,
	GIOCondition condition,
	gpointer user_data
)
{
	static unsigned int terminated = 0;
	struct signalfd_siginfo si;
	int fd;
	GMainLoop* loop = (GMainLoop*)user_data;
	gboolean result;

	if (condition & (G_IO_NVAL | G_IO_ERR | G_IO_HUP)) {
		printf("Quitting...");
		g_main_loop_unref(loop);
		g_source_remove(g_event_source_id);
		g_main_loop_quit(loop);
		result = FALSE;
	}
	else
	{
		fd = g_io_channel_unix_get_fd(channel);

		if (read(fd, &si, sizeof(si)) != sizeof(si))
		{
			printf("read from fd failed\r\n");
			result = FALSE;
		}
		else
		{
			switch (si.ssi_signo) {
			case SIGINT:
				printf("Caught ctrl+c - quitting...");
				g_main_loop_unref(loop);
				g_source_remove(g_event_source_id);
				g_main_loop_quit(loop);
				break;
			case SIGTERM:
				if (terminated == 0) {
					printf("Caught SIGTERM - quitting...");
					g_main_loop_unref(loop);
					g_source_remove(g_event_source_id);
					g_main_loop_quit(loop);
				}

				terminated = 1;
				break;
			}

			result = TRUE;
		}
	}

	return result;
}

