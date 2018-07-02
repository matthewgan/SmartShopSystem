/*
 Name:		GateController.ino
 Created:	2018/7/1 21:39:37
 Author:	matth
*/

#include "LedControl.h"
#include "communication.h"

#define DoorRelayPin  12
#define LedLastTime 3000

unsigned long timestamp;

void setup() {
	// put your setup code here, to run once:
	Serial.begin(9600);
	delay(500);

	LedInit();

	initialReceiver();

	pinMode(DoorRelayPin, OUTPUT);
	digitalWrite(DoorRelayPin, HIGH);

	LED_keepGreen();
}

void openDoor()
{
	LED_keepRed();
	timestamp = millis();
	digitalWrite(DoorRelayPin, LOW);
	delay(300);
	digitalWrite(DoorRelayPin, HIGH);
}

void loop() {
	if ((millis() - timestamp)>LedLastTime)
	{
		LED_keepGreen();
		timestamp = millis();
	}

	//Receive commands from serial
	if (Serial.available() > 0)
	{
		int len = Serial.available();
		byte *buffer = new byte[len];
		for (int i = 0; i < len; i++)
		{
			buffer[i] = Serial.read();
			//protocol parse and validation
			if (receiveFrame(buffer[i]))
			{
				memcpy(frame, comBuffer, ContentLength + FRAME_NONCONTENT_LENGTH);
				ContentLength = 0;
				//run the command to actions
				ParseFrameToCommand(frame);
			}
		}
		free(buffer);
	}
}

void ParseFrameToCommand(uint8_t *frame)
{
	//get cmd type and also record last cmdtype
	CMDTYPE cmdtype = getCmdTypeOfFrame(frame);

	switch (cmdtype)
	{
	case OpenDoor:
	{
		openDoor();
		break;
	}
	default:
		break;
	}
}
