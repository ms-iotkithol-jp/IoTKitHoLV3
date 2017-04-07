// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <stdio.h>
#include <stdlib.h>

#include <sys/signalfd.h>
#include <unistd.h>
#include <signal.h>
#include <time.h>

#include <glib.h>

#ifdef ARDUINO
#include "AzureIoT.h"
#else
#include "iothub_client.h"
#include "iothub_message.h"
#include "azure_c_shared_utility/threadapi.h"
#include "azure_c_shared_utility/crt_abstractions.h"
#include "iothubtransportamqp.h"
#include "azure_c_shared_utility/platform.h"
#endif

#ifdef MBED_BUILD_TIMESTAMP
#include "certs.h"
#endif // MBED_BUILD_TIMESTAMP

/*String containing Hostname, Device Id & Device Key in the format:                         */
/*  "HostName=<host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"                */
static const char* connectionString = "[device connection string]";
static const char* deviceId = "[device id]";

static int callbackCounter;
static bool g_continueRunning;
static char msgText[1024];
static char propText[1024];
#define MESSAGE_COUNT       5
#define DOWORK_LOOP_NUM     3

//DEFINE_ENUM_STRINGS(IOTHUB_CLIENT_CONFIRMATION_RESULT, IOTHUB_CLIENT_CONFIRMATION_RESULT_VALUES);

guint g_event_source_id = 0;
static gboolean signal_handler(
	GIOChannel *channel,
	GIOCondition condition,
	gpointer user_data
);
static void handle_control_c(GMainLoop* loop);

typedef struct EVENT_INSTANCE_TAG
{
	IOTHUB_MESSAGE_HANDLE messageHandle;
	int messageTrackingId;  // For tracking the messages within the user callback.
} EVENT_INSTANCE;

typedef struct IoTKitHoLContext_tag {
	IOTHUB_CLIENT_HANDLE iotHubClientHandle;
	void* messageLoop;
} IoTKitHoLContext;

double lastAccelX = 0.0;
double lastAccelY = 0.0;
double lastAccelZ = 0.0;
double lastTemperature = 0.0;

static IOTHUBMESSAGE_DISPOSITION_RESULT ReceiveMessageCallback(IOTHUB_MESSAGE_HANDLE message, void* userContextCallback)
{
	int* counter = (int*)userContextCallback;
	const char* buffer;
	size_t size;
	MAP_HANDLE mapProperties;

	if (IoTHubMessage_GetByteArray(message, (const unsigned char**)&buffer, &size) != IOTHUB_MESSAGE_OK)
	{
		printf("unable to retrieve the message data\r\n");
	}
	else
	{
		(void)printf("Received Message [%d] with Data: <<<%.*s>>> & Size=%d\r\n", *counter, (int)size, buffer, (int)size);
		if (memcmp(buffer, "quit", size) == 0)
		{
			g_continueRunning = false;
		}
	}

	// Retrieve properties from the message
	mapProperties = IoTHubMessage_Properties(message);
	if (mapProperties != NULL)
	{
		const char*const* keys;
		const char*const* values;
		size_t propertyCount = 0;
		if (Map_GetInternals(mapProperties, &keys, &values, &propertyCount) == MAP_OK)
		{
			if (propertyCount > 0)
			{
				size_t index;

				printf("Message Properties:\r\n");
				for (index = 0; index < propertyCount; index++)
				{
					printf("\tKey: %s Value: %s\r\n", keys[index], values[index]);
				}
				printf("\r\n");
			}
		}
	}

	/* Some device specific action code goes here... */
	(*counter)++;
	return IOTHUBMESSAGE_ACCEPTED;
}

static void SendConfirmationCallback(IOTHUB_CLIENT_CONFIRMATION_RESULT result, void* userContextCallback)
{
	EVENT_INSTANCE* eventInstance = (EVENT_INSTANCE*)userContextCallback;
	(void)printf("Confirmation[%d] received for message tracking id = %d with result = %s\r\n", callbackCounter, eventInstance->messageTrackingId, ENUM_TO_STRING(IOTHUB_CLIENT_CONFIRMATION_RESULT, result));
	/* Some device specific action code goes here... */
	callbackCounter++;
	IoTHubMessage_Destroy(eventInstance->messageHandle);
	free(eventInstance);
}

static size_t iterator = 0;
void SendMessageToIoTHub(IOTHUB_CLIENT_HANDLE iotHubClientHandle, const unsigned char* msg, int msgLen)
{
	EVENT_INSTANCE* eventinstance = (EVENT_INSTANCE*)malloc(sizeof(EVENT_INSTANCE));
	if ((eventinstance->messageHandle = IoTHubMessage_CreateFromByteArray(msg, msgLen)) == NULL)
	{
		(void)printf("ERROR: iotHubMessageHandle is NULL!\r\n");
	}
	else
	{
		MAP_HANDLE propMap;

		eventinstance->messageTrackingId = iterator;

		propMap = IoTHubMessage_Properties(eventinstance->messageHandle);
		sprintf_s(propText, sizeof(propText), "PropMsg_%d", iterator);
		if (Map_AddOrUpdate(propMap, "PropName", propText) != MAP_OK)
		{
			(void)printf("ERROR: Map_AddOrUpdate Failed!\r\n");
		}

		if (IoTHubClient_SendEventAsync(iotHubClientHandle, eventinstance->messageHandle, SendConfirmationCallback, eventinstance) != IOTHUB_CLIENT_OK)
		{
			(void)printf("ERROR: IoTHubClient_SendEventAsync..........FAILED!\r\n");
		}
		else
		{
			(void)printf("IoTHubClient_SendEventAsync accepted message [%zu] for transmission to IoT Hub.\r\n", iterator);
		}
		iterator++;
	}
}

double averageAccelX = 0.0;
double averageAccelY = 0.0;
double averageAccelZ = -1.0;
double averageTemperature = 25.0;
char currentTime[32];

static gboolean on_timer_measure(gpointer user_data)
{
	IoTKitHoLContext* context = (IoTKitHoLContext*)user_data;

	// please impelement sensor measure implementation here.

	lastAccelX = averageAccelX + (50.0 - (double)(rand() % 100)) / 500.0;
	lastAccelY = averageAccelY + (50.0 - (double)(rand() % 100)) / 500.0;
	lastAccelZ = averageAccelZ + (50.0 - (double)(rand() % 100)) / 500.0;
	lastTemperature = averageTemperature + (50.0 - (double)(rand() % 100)) / 25.0;

	time_t now;
	struct tm *local;
	//	struct timespec mlocal;
	//	clock_gettime(CLOCK_REALTIME, &mlocal);

	now = time(NULL);
	local = localtime(&now);
	//	local = localtime(&mlocal.tv_sec);

	sprintf(currentTime, "%04d-%02d-%02dT%02d:%02d:%02dZ", local->tm_year + 1900, local->tm_mon + 1, local->tm_mday, local->tm_hour, local->tm_min, local->tm_sec);

	printf("measured!\r\n");

	return TRUE;
}

static gboolean on_timer_upload(gpointer user_data)
{
	IoTKitHoLContext* context = (IoTKitHoLContext*)user_data;
	time_t now;
	struct tm *local;
	now = time(NULL);
	local = localtime(&now);

	// modifiy here when you want to send more sensors
	sprintf(msgText, "{\"time\":\"%s\",\"accelx\":%f,\"accely\":%f,\"accelz\":%f,\"temp\":%f}",
		currentTime, lastAccelX, lastAccelY, lastAccelZ, lastTemperature);
	SendMessageToIoTHub(context->iotHubClientHandle, msgText, strlen(msgText));

	return TRUE;
}

int upload_duration_msec = 5000;	// upload data each 5 sec;
int measure_duration_msec = 1000;	// measure data each 1 sec
guint upload_timer_id;
guint measure_timer_id;

void start_timers(IoTKitHoLContext* context)
{
	measure_timer_id = g_timeout_add(
		measure_duration_msec,
		on_timer_measure,
		context
	);
	if (measure_timer_id == 0) {
		// Failed
	}
	else {
		upload_timer_id = g_timeout_add(
			upload_duration_msec,
			on_timer_upload,
			context
		);
	}
}

void iotkithol_sample_amqp_run(void* messageLoop)
{
	IoTKitHoLContext iotKitHoLContext;
	IOTHUB_CLIENT_HANDLE iotHubClientHandle;

	iotKitHoLContext.messageLoop = messageLoop;

	EVENT_INSTANCE messages[MESSAGE_COUNT];
	double avgWindSpeed = 10.0;
	int receiveContext = 0;
	g_continueRunning = true;

	srand((unsigned int)time(NULL));

	callbackCounter = 0;

	if (platform_init() != 0)
	{
		printf("Failed to initialize the platform.\r\n");
	}
	else
	{
		(void)printf("Starting the IoTHub client sample AMQP - %s\r\n", connectionString);

		if ((iotHubClientHandle = IoTHubClient_CreateFromConnectionString(connectionString, AMQP_Protocol)) == NULL)
		{
			(void)printf("ERROR: iotHubClientHandle is NULL!\r\n");
		}
		else
		{
			iotKitHoLContext.iotHubClientHandle = iotHubClientHandle;

			unsigned int timeout = 241000;
			// Because it can poll "after 9 seconds" polls will happen effectively // at ~10 seconds.
			// Note that for scalabilty, the default value of minimumPollingTime
			// is 25 minutes. For more information, see:
			// https://azure.microsoft.com/documentation/articles/iot-hub-devguide/#messaging
			unsigned int minimumPollingTime = 9;
			if (IoTHubClient_SetOption(iotHubClientHandle, "timeout", &timeout) != IOTHUB_CLIENT_OK)
			{
				printf("failure to set option \"timeout\"\r\n");
			}

			if (IoTHubClient_SetOption(iotHubClientHandle, "MinimumPollingTime", &minimumPollingTime) != IOTHUB_CLIENT_OK)
			{
				printf("failure to set option \"MinimumPollingTime\"\r\n");
			}

#ifdef MBED_BUILD_TIMESTAMP
			// For mbed add the certificate information
			if (IoTHubClient_SetOption(iotHubClientHandle, "TrustedCerts", certificates) != IOTHUB_CLIENT_OK)
			{
				printf("failure to set option \"TrustedCerts\"\r\n");
			}
#endif // MBED_BUILD_TIMESTAMP

			/* Setting Message call back, so we can receive Commands. */
			if (IoTHubClient_SetMessageCallback(iotHubClientHandle, ReceiveMessageCallback, &receiveContext) != IOTHUB_CLIENT_OK)
			{
				(void)printf("ERROR: IoTHubClient_SetMessageCallback..........FAILED!\r\n");
			}
			else
			{
				(void)printf("IoTHubClient_SetMessageCallback...successful.\r\n");

				start_timers(&iotKitHoLContext);
			}

			// run the glib loop
			g_main_loop_run(messageLoop);

			IoTHubClient_Destroy(iotHubClientHandle);
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
	iotkithol_sample_amqp_run(start_message_loop());
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
