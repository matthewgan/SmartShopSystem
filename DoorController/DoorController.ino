/*
 Name:		DoorController.ino
 Created:	2018/7/3 15:44:03
 Author:	matth
*/
// includes
#include "config.h"
#include "DoorDriver.h"
#include "COM.h"
#include "Msg.h"


// global variables
DoorDriver door;
COM PCCom(Serial);
Msg* msgIn = new Msg();

byte DirCmd = 0;
int retryTimes = 0;
int msgResponseRetry = 0;
unsigned long MsgRespondTimer = 0;

bool EmgButtonPressed = false;
bool DetectCustomerInReceived = true;
bool DetectCustomerOutReceived = true;
bool OpenDoorCmdReceived = true;
bool CloseDoorMsgReceived = true;



void setup() {
	Serial.begin(9600);
	Serial1.begin(19200);
	delay(2000);
	//use Serial1 to control the motor
	door.init(Serial1);
	
	pinMode(PIRSensor, INPUT);
	SendDebugMsg("system ready");
}

void loop() {
	//recieve message from serialport 0 connect to PC
	PCCom.ListenPortData(msgIn);
	if (msgIn->dataReady) {
		ProcessCmd(msgIn);
		delete msgIn;
		msgIn = new Msg();
	}

	//Emergence Handler
	if (door.EmgTriggered() == true)
	{
		Msg* EmgMsg = new Msg(Msg::EmgMsg);
		PCCom.Write(EmgMsg);
		delete EmgMsg;
		EmgButtonPressed = true;
	}
	//Emergence Release Handler
	else if (EmgButtonPressed == true)
	{
		Msg* EmgReleaseMsg = new Msg(Msg::EmgRelease);
		PCCom.Write(EmgReleaseMsg);
		delete EmgReleaseMsg;
		EmgButtonPressed = false;
	}

	//Door State Machine
	switch (door.currentDoorState)
	{
	case DoorDriver::OpenInwards:
		CustomerInEventHandler();
		CheckMsgReceive();
		break;

	case DoorDriver::Closed:
		break;

	case DoorDriver::OpenOutwards:
		CustomerOutEventHandler();
		CheckMsgReceive();
		break;

	default:
		break;
	}

}

void CheckMsgReceive()
{
	if (DetectCustomerInReceived == false || DetectCustomerOutReceived == false)
	{
		//resend status msg if PC didn't respond to previous msg for upto 10 times with 500ms between each msg
		if (millis() - MsgRespondTimer > MsgRespondTimeOut 
			&& msgResponseRetry < NumOfRetry)
		{
			SendDebugMsg("Resending Msgs \n");
			if (DetectCustomerInReceived == false)
			{
				CustomerInEventHandler();
			}
			else if (DetectCustomerOutReceived == false)
			{
				Msg* msgOut = new Msg(Msg::DetectCustomerOut);
				PCCom.Write(msgOut);
				delete msgOut;
			}
			MsgRespondTimer = millis();
			msgResponseRetry++;
		}
		else if (msgResponseRetry >= NumOfRetry)
		{
			//open the door inwards if there's no respond after 10 times retry
			SendDebugMsg("Msg response retry times out \n");
			door.OpenLetIn();
			door.motorStop();
			msgResponseRetry = 0;
			delay(5000); //hold the door open let customer out 
		}
	}
}


bool CheckCustomerOnPlatform()
{
	int weightRawVal = analogRead(WeightSensor);
	float weight = map(weightRawVal, 0, 1023, 0, 300);
	return (weight > weightThreshold) ? true : false;
}

void CustomerInEventHandler()
{
	if (digitalRead(PIRSensor) == HIGH 
		&& CheckCustomerOnPlatform()
		&& millis() - MsgRespondTimer > MsgRespondTimeOut)
	{
		Msg* msgOut = new Msg(Msg::DetectCustomerIn);
		PCCom.Write(msgOut);
		delete msgOut;
		DetectCustomerInReceived = false;
		MsgRespondTimer = millis();
	}
}

void CustomerOutEventHandler()
{
	if ((CheckCustomerOnPlatform() == false && digitalRead(PIRSensor) == HIGH) 
		||(CheckCustomerOnPlatform() == false && digitalRead(IRDoorSensor_Outlet) == ActiveLowTriggered))
	{
		Msg* msgOut = new Msg(Msg::DetectCustomerOut);
		PCCom.Write(msgOut);
		delete msgOut;
		DetectCustomerOutReceived = false;
		MsgRespondTimer = millis();
	}
}

void CloseDoor()
{
	while (retryTimes < NumOfRetry && door.Close() != Success)
	{
		//try close the door up to limit times
		retryTimes++;
	}
	if (retryTimes >= NumOfRetry)//error handeling
	{
		door.motorStop();
	}
	else
	{
		Msg* doorClosedMsg = new Msg(Msg::CloseDoorCmdRespond);
		PCCom.Write(doorClosedMsg);
		delete doorClosedMsg;
		MsgRespondTimer = millis();
		retryTimes = 0;
	}
}

void ProcessCmd(Msg* msg)
{
	if (msg->dataReady)
	{
		switch (msg->cmdType)
		{
		case Msg::DetectCustomerInRespond:
			DetectCustomerInReceived = true;
			msgResponseRetry = 0;
			SendDebugMsg("DetectCustomerInRespond Received \n");
			break;

		case Msg::CloseDoorCmd:
			CloseDoorMsgReceived = true;
			CloseDoor();
			SendDebugMsg("CloseDoorCmd Received \n");
			break;

		case Msg::OpenDoorCmd:
			OpenDoorCmdReceived = true;
			switch (DirCmd)
			{
			case 0:
				while (door.OpenLetIn() != Success 
					&& retryTimes < NumOfRetry)
				{
					retryTimes++;
				}
				if (retryTimes >= NumOfRetry)//error handeling
				{
					Msg* errorMsg = new Msg(Msg::ErrorMsg);
					PCCom.Write(errorMsg);
					delete errorMsg;
				}
				retryTimes = 0;
				break;

			case 1:
				while (door.OpenLetOut() != Success 
					&& retryTimes < NumOfRetry)
				{
					retryTimes++;
				}
				if (retryTimes >= NumOfRetry)//error handeling
				{
					Msg* errorMsg = new Msg(Msg::ErrorMsg);
					PCCom.Write(errorMsg);
					delete errorMsg;
				}
				retryTimes = 0;
				break;
			}
			if (true)
			{
				Msg* respondMsg = new Msg(Msg::OpenDoorCmdRespond);//for some reason, can't put it directly under case:
				PCCom.Write(respondMsg);
				delete respondMsg;
				DirCmd = msg->GetContent<byte>();
			}
			break;

		case Msg::DetectCustomerOutRespond:
			SendDebugMsg("DetectCustomerOutRespond Received \n");
			DetectCustomerInReceived = true;
			break;
		}
	}
}


void SendDebugMsg()
{
	Msg* debugMsg = new Msg("debug", Msg::DebugMessage);
	PCCom.Write(debugMsg);
	delete debugMsg;
}

void SendDebugMsg(String msg)
{
	msg = "dbgMsg: " + msg;
	Msg* debugMsg = new Msg(msg, Msg::DebugMessage);
	PCCom.Write(debugMsg);
	delete debugMsg;
}