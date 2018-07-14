#ifndef _DOORDRIVER_h
#define _DOORDRIVER_h

#if defined(ARDUINO) && ARDUINO >= 100
#include "arduino.h"
#else
#include "WProgram.h"
#endif

#include "COM.h"
#include "Msg.h"

class DoorDriver
{
public:
	enum Direction
	{
		forward, backwards
	};
	enum DoorState
	{
		OpenInwards,RotatingOutwards, Closed, OpenOutwards,RotatingInwards,
	};

	unsigned long doorRotatingTimeout = 30000; //30s before each door rotating action timeout
	DoorState currentDoorState = OpenInwards;

	void init(Stream& port);
	void SetMotorPower(Direction dir,int spd, int AccTime);
	void motorStop();
	void MotorDrive();
	bool OpenLetIn();
	bool OpenLetOut();
	bool Close();
	bool SafeToMove();
	bool EmgTriggered();
	void ReleaseInwards();
	void ReleaseOutwards();

private:
	byte motorRunningSpd = 70;
	byte motorStopCMD = 127;
	float  currentPower_CMD = 0;
	float startPower = 0;
	float targetPower = 0;
	float powerDiff = 0;
	unsigned long startTime = 0;
	unsigned long accTime = 0;
	Stream* MotorPort;
	Direction currentDir;
	COM dbgCom;
	void SendDebugMsg(String);
	
};

#endif

