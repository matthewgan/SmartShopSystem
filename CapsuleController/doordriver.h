#pragma once
#include "config.h"

class Door
{
public:
	enum Direction
	{
		forward, backwards
	};
	enum DoorState
	{
		OpenInwards, RotatingOutwards, Closed, OpenOutwards, RotatingInwards,
	};

	unsigned long doorRotatingTimeout = 30000; //30s before each door rotating action timeout
	DoorState currentDoorState = OpenInwards;

	void init(Stream& port);
	void SetMotorPower(Direction dir, int spd, int AccTime);
	void motorStop();
	void MotorDrive();
	void MotorDriveLadder();
	bool OpenLetIn();
	bool OpenLetOut();
	bool Close();
	bool SafeToMove();
	bool EmgTriggered();
	void ReleaseInwards();
	void ReleaseOutwards();
	int ReadWeight();
	bool CheckCustomerOnPlatform();

private:
	byte motorSlowSpd = 20;
	byte motorFastSpd = 50;
	byte motorStopCMD = 127;
	float  currentPower_CMD = 0;
	float startPower = 0;
	float targetPower = 0;
	float powerDiff = 0;
	unsigned long startTime = 0;
	unsigned long accTime = 0;
	Stream* MotorPort;
	Direction currentDir;
	//void SendDebugMsg(String);

};