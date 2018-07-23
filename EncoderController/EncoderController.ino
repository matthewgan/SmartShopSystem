/*
 Name:		EncoderController.ino
 Created:	2018/7/8 16:47:48
 Author:	matth
*/
#include "communication.h"
#include "encoder.h"

float lastValue;

float absValue;

STRU_IntMsg msg;

// the setup function runs once when you press reset or power the board
void setup() {
	Serial.begin(9600);
	delay(500);

	//initial the receiver
	initialReceiver();

	//initial encoder
	enc_init();
	lastValue = enc_read();
	absValue = 0;
}

// the loop function runs over and over again until power down or reset
void loop() {
	//keep counting encoder continuously
	float value = enc_read();
	absValue = ReadValueToAbsoluteValue(absValue, value, lastValue);
	
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

// received commands to actions
void ParseFrameToCommand(uint8_t *frame)
{
	//get cmd type and also record last cmdtype
	CMDTYPE cmdtype = getCmdTypeOfFrame(frame);

	switch (cmdtype)
	{
		case ReadEncoderRequest:
		{
			FillIntMsg(&msg, ReadEncoderResponse, absValue);
			Serial.write((uint8_t *)&msg, sizeof(STRU_IntMsg));
			Serial.flush();
			break;
		}
		case ResetEncoder:
		{
			lastValue = enc_read();
			absValue = 0;
			break;
		}
		default:break;
	}
}

//calculate absolute encoder value from last read value and current value
float ReadValueToAbsoluteValue(float abs, float val, float lastval)
{
	float last = lastval;
	float cur = val;
	float ret = abs;

	if ((last > 260) && (val < 100))
	{
		cur += 360;
	}
	if ((last < 100) && (val > 260))
	{
		last += 360;
	}

	//direction
	if (cur >= last)
	{
		//rotate to inward
		ret += cur - last;
	}
	else
	{
		//rotate to outward
		ret -= last - cur;
	}

	return ret;
}
