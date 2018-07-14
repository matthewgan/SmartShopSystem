// config.h

#ifndef _CONFIG_h
#define _CONFIG_h

#if defined(ARDUINO) && ARDUINO >= 100
	#include "arduino.h"
#else
	#include "WProgram.h"
#endif

#define ActiveLowTriggered LOW
#define ActiveHighTriggered HIGH

// pin definitions
#define IRDoorSensor_Inlet 34 //Active low
#define IRDoorSensor_Outlet 36 //Active low
#define EmgBtn 30 //Active high
#define PosSensorInletOpen 24 // Active low
#define PosSensorClosed 26 // Active low
#define PosSensorOutletOpen 28 // Active low

#define PIRSensor 32 //Pir sensor pin, Active high
#define WeightSensor A0 //weight sensor input pin A0

//SSI Encoder pin
#define enc_DO  6
#define enc_CLK 5
#define enc_CS  4


// global settings
#define Success true
#define NumOfRetry 10 //number of retries if any door operating cmd return false
#define DoorHoldingTime 5000 // hold the door by x ms to let customer out
#define weightThreshold 30 //weight in kg, the value to ctrl weight sensor detect is there any ppl standing on the platform
#define MsgRespondTimeOut 3000



#endif

