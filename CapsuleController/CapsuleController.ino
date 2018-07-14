/*
 Name:		CapsuleController.ino
 Created:	2018/7/8 16:48:34
 Author:	matth
*/
#include "communication.h"
#include "config.h"
#include "doordriver.h"

Door door;

bool EmgButtonPressed = false;
bool DetectCustomerInReceived = true;
bool DetectCustomerOutReceived = true;

STRU_Msg msg;
STRU_ErrorMsg errmsg;

int retryTimes = 0;
int msgResponseRetry = 0;
unsigned long MsgRespondTimer = 0;

// the setup function runs once when you press reset or power the board
void setup() {
	Serial.begin(57600);
	Serial1.begin(19200);
	delay(500);

	initialReceiver();
	door.init(Serial1);

}

// the loop function runs over and over again until power down or reset
void loop() {
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

	//Emergence Button Handler
	if (door.EmgTriggered() == true)
	{
		//Emergence Handler
		SendCmdOnlyMsg(EmgMsg);
		EmgButtonPressed = true;
	}
	else if (EmgButtonPressed == true)
	{
		//Emergence Release Handler
		SendCmdOnlyMsg(EmgRelease);
		EmgButtonPressed = false;
	}

	//Door State Machine, only need to handler two states
	switch (door.currentDoorState)
	{
	case Door::OpenInwards:
	{
		CustomerInEventHandler();
		CheckDetectInResponseReceiveWithRetry();
		break;
	}

	case Door::OpenOutwards:
	{
		CustomerOutEventHandler();
		CheckDetectOutResponseReceiveWithRetry();
		break;
	}

	default:break;
	}
}

// received commands to actions
void ParseFrameToCommand(uint8_t *frame)
{
	//get cmd type and also record last cmdtype
	CMDTYPE cmdtype = getCmdTypeOfFrame(frame);

	switch (cmdtype)
	{
	case DetectCustomerInRespond:
	{
		DetectCustomerInReceived = true;
		msgResponseRetry = 0;
		break;
	}

	case CloseDoorCmd:
	{
		retryTimes = 0;
		CloseDoorWithRetry();
		break;
	}

	case OpenDoorCmd:
	{
		retryTimes = 0;
		STRU_OpenDoorMsg *pMsg = (STRU_OpenDoorMsg*)frame;
		//uint8_t pdir = pMsg->dir;
		if (pMsg->dir == (uint8_t)(0))
		{
			OpenDoorInwardWithRetry();
		}
		else if (pMsg->dir == (uint8_t)(1))
		{
			OpenDoorOutwardWithRetry();
		}
		break;
	}

	case DetectCustomerOutRespond:
	{
		DetectCustomerOutReceived = true;
		msgResponseRetry = 0;
		break;
	}

	case Reset:
	{
		door.OpenLetIn();
		break;
	}

	default:break;
	}
}

void SendCmdOnlyMsg(CMDTYPE ct)
{
	FillCmdTypeOnlyMsg(&msg, ct);
	Serial.write((uint8_t *)&msg, sizeof(STRU_Msg));
	Serial.flush();
}

void SendErrorMsg(ERROR_CODE error_code)
{
	FillErrorMsg(&errmsg, error_code);
	Serial.write((uint8_t *)&errmsg, sizeof(STRU_ErrorMsg));
	Serial.flush();
}

void CloseDoorWithRetry()
{
	while ((retryTimes < NumOfRetry) && (door.Close() != Success))
	{
		retryTimes++;
	}
	if (retryTimes >= NumOfRetry)//error handeling
	{
		SendErrorMsg(CloseDoorRetryTooManyTimes);
		door.motorStop();
	}
	else
	{
		SendCmdOnlyMsg(CloseDoorCmdRespond);
retryTimes = 0;
	}
}

void OpenDoorInwardWithRetry()
{
	while ((retryTimes < NumOfRetry) && (door.OpenLetIn() != Success))
	{
		retryTimes++;
	}
	if (retryTimes >= NumOfRetry)//error handeling
	{
		SendErrorMsg(OpenDoorInwardRetryTooManyTimes);
		door.motorStop();
		retryTimes = 0;
	}
	else
	{
		SendCmdOnlyMsg(OpenDoorCmdRespond);
		retryTimes = 0;
	}
}

void OpenDoorOutwardWithRetry()
{
	while ((retryTimes < NumOfRetry) && (door.OpenLetOut() != Success))
	{
		retryTimes++;
	}
	if (retryTimes >= NumOfRetry)//error handeling
	{
		SendErrorMsg(OpenDoorOutwardRetryTooManyTimes);
		door.motorStop();
		retryTimes = 0;
	}
	else
	{
		SendCmdOnlyMsg(OpenDoorCmdRespond);
		retryTimes = 0;
	}
}

void CustomerInEventHandler()
{
	if (digitalRead(PIRSensor) == HIGH
		&& door.CheckCustomerOnPlatform()
		&& (millis() - MsgRespondTimer > MsgRespondTimeOut)
		&& (DetectCustomerInReceived == true)
		)
	{
		SendCmdOnlyMsg(DetectCustomerIn);
		DetectCustomerInReceived = false;
		MsgRespondTimer = millis();
	}
}

void CustomerOutEventHandler()
{
	//if (((door.CheckCustomerOnPlatform() == false) && (digitalRead(PIRSensor) == HIGH))
	//	|| ((door.CheckCustomerOnPlatform() == false) && (digitalRead(IRDoorSensor_Outlet) == ActiveLowTriggered)))
	if ((door.CheckCustomerOnPlatform() == false) && (digitalRead(IRDoorSensor_Outlet) == ActiveLowTriggered))
	{
		SendCmdOnlyMsg(DetectCustomerOut);
		DetectCustomerOutReceived = false;
		MsgRespondTimer = millis();
	}
}

#if false
void CheckResponseMsgReceive()
{
	if (DetectCustomerInReceived == false || DetectCustomerOutReceived == false)
	{
		//resend status msg if PC didn't respond to previous msg for upto max times with 500ms between each msg
		if (millis() - MsgRespondTimer > MsgRespondTimeOut
			&& msgResponseRetry < NumOfRetry)
		{
			if (DetectCustomerInReceived == false)
			{
				CustomerInEventHandler();
			}
			else if (DetectCustomerOutReceived == false)
			{
				CustomerOutEventHandler();
			}
			MsgRespondTimer = millis();
			msgResponseRetry++;
		}
		else if (msgResponseRetry >= NumOfRetry)
		{

			door.OpenLetIn();
			door.motorStop();
			msgResponseRetry = 0;
			delay(5000);
		}
	}
}
#endif

#if false
void CheckDetectInResponseReceiveWithRetry()
{
	if (DetectCustomerInReceived == false)
	{
		if (((millis() - MsgRespondTimer) > MsgRespondTimeOut) && (msgResponseRetry < NumOfRetry))
		{
			CustomerInEventHandler();
			msgResponseRetry++;
			MsgRespondTimer = millis();
		}
		else if (msgResponseRetry > NumOfRetry)
		{
			SendErrorMsg(GetDetectCustomerInResponseRetryTooManyTimes);
			//because right now the door is open to inward, no need to move door
			door.motorStop();
			msgResponseRetry = 0;
			delay(5000); //lock 5s after error
		}
	}
}
#endif

void CheckDetectInResponseReceiveWithRetry()
{
	if (DetectCustomerOutReceived == false)
	{
		if (((millis() - MsgRespondTimer) > MsgRespondTimeOut) && (msgResponseRetry < NumOfRetry))
		{
			CustomerOutEventHandler();
			msgResponseRetry++;
			MsgRespondTimer = millis();
		}
		else if (msgResponseRetry > NumOfRetry)
		{
			SendErrorMsg(GetDetectCustomerOutResponseRetryTooManyTimes);
			//because right now the door is open to inward, no need to move door
			door.motorStop();
			msgResponseRetry = 0;
			delay(5000); //lock 5s after error
		}
	}
}

void CheckDetectOutResponseReceiveWithRetry()
{
	if (DetectCustomerOutReceived == false)
	{
		if (((millis() - MsgRespondTimer) > MsgRespondTimeOut) && (msgResponseRetry < NumOfRetry))
		{
			CustomerOutEventHandler();
			msgResponseRetry++;
			MsgRespondTimer = millis();
		}
		else if (msgResponseRetry > NumOfRetry)
		{
			SendErrorMsg(GetDetectCustomerOutResponseRetryTooManyTimes);
			//because right now the door is open to outward, and customer is going outside, need to close the door to prevent inverse coming
			door.Close();
			msgResponseRetry = 0;
			delay(5000); //lock 5s after error
		}
	}
}