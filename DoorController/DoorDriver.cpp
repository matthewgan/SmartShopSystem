// 
// 
// 
#include "DoorDriver.h"

void DoorDriver::init(Stream& port)
{
	MotorPort = &port;
	motorStop();
	pinMode(IRDoorSensor_Inlet, INPUT_PULLUP);
	pinMode(IRDoorSensor_Outlet, INPUT_PULLUP);
	pinMode(EmgBtn, INPUT_PULLUP);
	pinMode(PosSensorInletOpen, INPUT_PULLUP);
	pinMode(PosSensorOutletOpen, INPUT_PULLUP);
	pinMode(PosSensorClosed, INPUT_PULLUP);
	pinMode(WeightSensor, INPUT);
}

void DoorDriver::SetMotorPower(Direction dir, int spd, int AccTime) //Acceleration Time is in ms
{
	spd = constrain(spd, 0, 100);
	spd = map(spd, 0, 100, 0, 127);

	if (dir == forward)
	{
		spd = motorStopCMD + spd;
	}
	else if (dir == backwards)
	{
		spd = motorStopCMD - spd;
	}
	currentDir = dir;
	powerDiff = spd - currentPower_CMD;
	startPower = currentPower_CMD;
	accTime = AccTime;
	startTime = millis();
}

void DoorDriver::motorStop()
{
	MotorPort->write(motorStopCMD);
	currentPower_CMD = motorStopCMD;
	SendDebugMsg("Motor stop \n");
}

void DoorDriver::MotorDrive()
{
	if (millis() - startTime < accTime)
	{
		float powerRatio = (millis() - startTime) / (float)accTime;
		currentPower_CMD = (byte)(startPower + (powerDiff * powerRatio));
		currentPower_CMD = constrain(currentPower_CMD, 0, 255);
		byte motorPower = currentPower_CMD;
		MotorPort->write(motorPower);
	}
}

bool DoorDriver::SafeToMove()
{
	bool safeToMove = false;
	if (digitalRead(IRDoorSensor_Inlet) != ActiveLowTriggered 
		&& digitalRead(IRDoorSensor_Outlet) != ActiveLowTriggered)
	{
		safeToMove = true;
	}
	return safeToMove;
}

bool DoorDriver::EmgTriggered()
{
	bool EmgBtnPushed = false;
	if (digitalRead(EmgBtn) == ActiveHighTriggered)
	{
		EmgBtnPushed = true;
		motorStop();
	}
	return EmgBtnPushed;
}

bool DoorDriver::OpenLetIn()
{
	bool successed = false;
	SetMotorPower(backwards, motorRunningSpd, 1000);
	currentDoorState = RotatingInwards;
	unsigned long timer = millis();
	SendDebugMsg("opening door to let customer in \n");
	while (digitalRead(IRDoorSensor_Outlet) != ActiveLowTriggered 
		&& EmgTriggered() == false 
		&& digitalRead(PosSensorInletOpen) != ActiveLowTriggered 
		&& (millis() - timer <= doorRotatingTimeout))
	{
		MotorDrive();
	}
	if (digitalRead(PosSensorInletOpen) == ActiveLowTriggered)
	{//check if exit correctly
		successed = true;
		currentDoorState = OpenInwards;
		motorStop();
	}
	else {
		ReleaseOutwards();
	}
	return successed;
}

bool DoorDriver::OpenLetOut()
{
	bool successed = false;
	SetMotorPower(forward, motorRunningSpd, 1000);
	currentDoorState = RotatingOutwards;
	unsigned long timer = millis();
	SendDebugMsg("opening door to let customer out \n");
	while (EmgTriggered() == false 
		&& digitalRead(PosSensorOutletOpen) != ActiveLowTriggered 
		&& (millis() - timer <= doorRotatingTimeout))
	{
		MotorDrive();
	}
	if (digitalRead(PosSensorOutletOpen) == ActiveLowTriggered)
	{
		successed = true;
		currentDoorState = OpenOutwards;
		motorStop();
	}
	return successed;
}

bool DoorDriver::Close()
{
	bool successed = false;
	if (currentDoorState == OpenInwards)
	{
		SendDebugMsg("Door Closing outwards \n");
		currentDoorState = RotatingOutwards;
		SetMotorPower(forward, motorRunningSpd, 1000);
		unsigned long timer = millis();
		while (digitalRead(IRDoorSensor_Inlet) != ActiveLowTriggered 
			&& EmgTriggered() == false 
			&& digitalRead(PosSensorClosed) != ActiveLowTriggered 
			&& (millis() - timer <= doorRotatingTimeout))
		{
			MotorDrive();
		}
		if (digitalRead(PosSensorClosed) == ActiveLowTriggered)
		{
			successed = true;
			currentDoorState = Closed;
			motorStop();
		}
		else { ReleaseInwards(); }
	}
	else if (currentDoorState == OpenOutwards)
	{
		SendDebugMsg("Door Closing inwards \n");
		currentDoorState = RotatingInwards;
		SetMotorPower(backwards, motorRunningSpd, 1000);
		unsigned long timer = millis();
		while (digitalRead(IRDoorSensor_Outlet) != ActiveLowTriggered && EmgTriggered() == false && digitalRead(PosSensorClosed) != ActiveLowTriggered && (millis() - timer <= doorRotatingTimeout))
		{
			MotorDrive();
		}
		if (digitalRead(PosSensorClosed) == ActiveLowTriggered)
		{
			successed = true;
			currentDoorState = Closed;
			motorStop();
		}
		else { ReleaseOutwards(); }
	}
	return successed;
}

void DoorDriver::ReleaseInwards()
{
	SendDebugMsg("door Releasing inwards. \n");
	DoorState previousState = currentDoorState;
	currentDoorState = RotatingInwards;
	SetMotorPower(backwards, motorRunningSpd, 1000);
	unsigned long timer = millis();
	while (digitalRead(PosSensorInletOpen) != ActiveLowTriggered && EmgTriggered() == false && (millis() - timer <= doorRotatingTimeout))
	{
		MotorDrive();
	}
	if (digitalRead(PosSensorInletOpen) == ActiveLowTriggered)
	{
		motorStop();
		currentDoorState = OpenInwards;
	}
}

void DoorDriver::ReleaseOutwards()
{
	SendDebugMsg("door Releasing outwards. \n");
	DoorState previousState = currentDoorState;
	currentDoorState = RotatingOutwards;
	SetMotorPower(forward, motorRunningSpd, 1000);
	unsigned long timer = millis();
	while (digitalRead(PosSensorOutletOpen) != ActiveLowTriggered && EmgTriggered() == false && (millis() - timer <= doorRotatingTimeout))
	{
		MotorDrive();
	}
	if (digitalRead(PosSensorOutletOpen) == ActiveLowTriggered)
	{
		motorStop();
		currentDoorState = OpenOutwards;
	}
}

void DoorDriver::SendDebugMsg(String msg)
{
	msg = "dbgMsg: " + msg;
	Msg* debugMsg = new Msg(msg, Msg::DebugMessage);
	dbgCom.Write(debugMsg);
	delete debugMsg;
}







