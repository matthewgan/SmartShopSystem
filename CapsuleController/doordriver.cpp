#include "doordriver.h"

#define DEBUGGING true

void Door::init(Stream& port)
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

void Door::SetMotorPower(Direction dir, int spd, int AccTime) //Acceleration Time is in ms
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

void Door::motorStop()
{
	MotorPort->write(motorStopCMD);
	currentPower_CMD = motorStopCMD;
	//SendDebugMsg("Motor stop \n");
}

void Door::MotorDrive()
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

void Door::MotorDriveLadder()
{
	//make a ladder of 3 times accTime
	unsigned long timeNow = millis();
	//acc step
	if (timeNow - startTime < accTime)
	{
		float powerRatio = (timeNow - startTime) / (float)accTime;
		currentPower_CMD = (byte)(startPower + (powerDiff * powerRatio));
		currentPower_CMD = constrain(currentPower_CMD, 0, 255);
		byte motorPower = currentPower_CMD;
		MotorPort->write(motorPower);
	}
	//stable step
	else if (timeNow - startTime < accTime * 2)
	{
		byte motorPower = currentPower_CMD;
		MotorPort->write(motorPower);
	}
	//deacc step
	else if (timeNow - startTime < accTime * 3)
	{
		float powerRatio = (timeNow - startTime - 2 * accTime) / (float)accTime;
		currentPower_CMD = (byte)(currentPower_CMD + (-powerDiff * powerRatio));
		currentPower_CMD = constrain(currentPower_CMD, 0, 255);
		byte motorPower = currentPower_CMD;
		MotorPort->write(motorPower);
	}
	else
	{
		motorStop();
	}
}

bool Door::SafeToMove()
{
	bool safeToMove = false;
	if (digitalRead(IRDoorSensor_Inlet) != ActiveLowTriggered
		&& digitalRead(IRDoorSensor_Outlet) != ActiveLowTriggered)
	{
		safeToMove = true;
	}
	return safeToMove;
}

bool Door::EmgTriggered()
{
	bool EmgBtnPushed = false;
	if (digitalRead(EmgBtn) == ActiveHighTriggered)
	{
		EmgBtnPushed = true;
		motorStop();
	}
	return EmgBtnPushed;
}

bool Door::OpenLetIn()
{
	bool successed = false;
	SetMotorPower(backwards, motorFixSpd, 1000);
	currentDoorState = RotatingInwards;
	unsigned long timer = millis();
	//SendDebugMsg("opening door to let customer in \n");
	while (digitalRead(PosSensorInletOpen) != ActiveLowTriggered
		//&& digitalRead(IRDoorSensor_Outlet) != ActiveLowTriggered
		//&& EmgTriggered() == false
		//&& (millis() - timer <= doorRotatingTimeout)
		)
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

bool Door::OpenLetOut()
{
	bool successed = false;
	SetMotorPower(forward, motorFixSpd, 1000);
	currentDoorState = RotatingOutwards;
	unsigned long timer = millis();
	//SendDebugMsg("opening door to let customer out \n");
	while (digitalRead(PosSensorOutletOpen) != ActiveLowTriggered
		//&& (millis() - timer <= doorRotatingTimeout)
		//&& EmgTriggered() == false
		)
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


bool Door::Close()
{
	bool successed = false;
	if (currentDoorState == OpenInwards)
	{
		//SendDebugMsg("Door Closing outwards \n");
		currentDoorState = RotatingOutwards;
		SetMotorPower(forward, motorFixSpd, 1000);
		unsigned long timer = millis();
		while (digitalRead(PosSensorClosed) != ActiveLowTriggered
			&& (CheckCustomerOnPlatform() == true)
			//&& EmgTriggered() == false
			&& digitalRead(IRDoorSensor_Inlet) != ActiveLowTriggered
			//&& (millis() - timer <= doorRotatingTimeout)
			)
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
		//SendDebugMsg("Door Closing inwards \n");
		currentDoorState = RotatingInwards;
		SetMotorPower(backwards, motorFixSpd, 1000);
		unsigned long timer = millis();
		while (digitalRead(PosSensorClosed) != ActiveLowTriggered
			&& (CheckCustomerOnPlatform() == false)
			&& digitalRead(IRDoorSensor_Outlet) != ActiveLowTriggered 
			//&& EmgTriggered() == false 
			//&& (millis() - timer <= doorRotatingTimeout)
			)
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

void Door::ReleaseInwards()
{
	//SendDebugMsg("door Releasing inwards. \n");
	DoorState previousState = currentDoorState;
	currentDoorState = RotatingInwards;
	SetMotorPower(backwards, motorFixSpd, 1000);
	unsigned long timer = millis();
	while (digitalRead(PosSensorInletOpen) != ActiveLowTriggered 
		//&& EmgTriggered() == false 
		&& (millis() - timer <= doorRotatingTimeout))
	{
		MotorDrive();
	}
	motorStop();
	currentDoorState = OpenInwards;
}

void Door::ReleaseOutwards()
{
	//SendDebugMsg("door Releasing outwards. \n");
	DoorState previousState = currentDoorState;
	currentDoorState = RotatingOutwards;
	SetMotorPower(forward, motorFixSpd, 1000);
	unsigned long timer = millis();
	while (digitalRead(PosSensorOutletOpen) != ActiveLowTriggered 
		//&& EmgTriggered() == false 
		&& (millis() - timer <= doorRotatingTimeout))
	{
		MotorDrive();
	}
	motorStop();
	currentDoorState = OpenOutwards;
}

int Door::ReadWeight() {
	int load = analogRead(WeightSensor);
	load = map(load, 0, 255, 0, 300);
	return load;
}

bool Door::CheckCustomerOnPlatform()
{
	int weight = ReadWeight();
	return (weight > weightThreshold) ? true : false;
}
