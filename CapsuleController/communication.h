#ifndef COMMUNICATION_H
#define COMMUNICATION_H

enum CMDTYPE
{
	IDLE = 0,                   //IDLE
	DetectCustomerIn = 1,       // Arduino -> PC
	DetectCustomerInRespond,    // PC -> Arduino

	CloseDoorCmd,               // PC -> Arduino
	CloseDoorCmdRespond,        // Arduino -> PC

	OpenDoorCmd,                // PC -> Arduino, dir 0: inwards, 1: outwards
	OpenDoorCmdRespond,         // Arduino -> PC

	DetectCustomerOut,          // Arduino -> PC
	DetectCustomerOutRespond,   // PC -> Arduino

	ErrorMsg,                   // Arduino -> PC, happens when something strage detected, Eg: No one in the room but sensor trigger
	EmgMsg,                     // Arduino -> PC, when emergency button is pushed
	EmgRelease,                 // Arduino -> PC, when emergency button is released
	Reset,						// PC -> Arduino, when the system first boot up, need to return the door to inwards, no need to reply

	OpenGate = 0x41,
	HeartBeatMsg,



	DebugMessage = 0xFF,
};

enum ERROR_CODE
{
	CloseDoorRetryTooManyTimes,
	OpenDoorInwardRetryTooManyTimes,
	OpenDoorOutwardRetryTooManyTimes,
	GetDetectCustomerInResponseRetryTooManyTimes,
	GetDetectCustomerOutResponseRetryTooManyTimes,
};

CMDTYPE lastReceivedCmd;

#define FRAMEHEADER1		(0xEB)
#define FRAMEHEADER2		(0x90)
#define FRAMEHEADER_LENGTH_IN_BYTES	2
#define FRAMEEND			(0xFE)
#define FRAME_MAX_LENGTH_IN_BYTES	100	
#define FRAME_BUFFER_LENGTH			(FRAME_MAX_LENGTH_IN_BYTES*2)
#define FRAME_NONCONTENT_LENGTH		5

uint8_t SearchState;
uint8_t comBuffer[FRAME_BUFFER_LENGTH];
uint8_t frame[FRAME_MAX_LENGTH_IN_BYTES];
uint8_t ContentLength;
uint8_t counter;
uint8_t *pt = comBuffer;

#pragma pack(4)
struct STRU_resetMsg
{
	uint8_t header[2];
	uint8_t cmdtype;
	uint8_t cmdlength;

	uint8_t endmark;
	uint8_t rsv[3];
};

struct STRU_Msg
{
	uint8_t header[2];
	uint8_t cmdtype;
	uint8_t cmdlength;

	uint8_t endmark;
	uint8_t rsv[3];
};

struct STRU_OpenDoorMsg
{
	uint8_t header[2];
	uint8_t cmdtype;
	uint8_t cmdlength;

	uint8_t dir;
	uint8_t endmark;
	uint8_t rsv[2];
};

struct STRU_ErrorMsg
{
	uint8_t header[2];
	uint8_t cmdtype;
	uint8_t cmdlength;

	uint8_t errcode;
	uint8_t endmark;
	uint8_t rsv[2];
};


struct STRU_HeartBeatMsg
{
	uint8_t header[2];
	uint8_t cmdtype;
	uint8_t cmdlength;

	uint32_t beat;

	uint8_t endmark;
	uint8_t rsv[3];
};

struct STRU_LEDcolorMsg
{
	uint8_t header[2];
	uint8_t cmdtype;
	uint8_t cmdlength;

	uint8_t mode;
	uint8_t endmark;
	uint8_t rsv[2];
};

struct STRU_MsgReply
{
	uint8_t header[2];
	uint8_t cmdtype;
	uint8_t cmdlength;
  
	uint8_t result;
	uint8_t endmark;
	uint8_t rsv[2];
};
#pragma pack()

void FillReplyIdMsg(STRU_MsgReply* ret, CMDTYPE CT, uint8_t result)
{
	ret->header[0] = FRAMEHEADER1;
	ret->header[1] = FRAMEHEADER2;
	ret->cmdtype = (uint8_t)CT;
	ret->cmdlength = 2;
	ret->result = result;
	ret->endmark = FRAMEEND;
}

void FillHeartBeatMsg(STRU_HeartBeatMsg* ret, CMDTYPE CT, uint32_t beat)
{
	ret->header[0] = FRAMEHEADER1;
	ret->header[1] = FRAMEHEADER2;
	ret->cmdtype = (uint8_t)CT;
	ret->cmdlength = 4;
	ret->beat = beat;
	ret->endmark = FRAMEEND;
}

void FillCmdTypeOnlyMsg(STRU_Msg* ret, CMDTYPE CT)
{
	ret->header[0] = FRAMEHEADER1;
	ret->header[1] = FRAMEHEADER2;
	ret->cmdtype = (uint8_t)CT;
	ret->cmdlength = 0;
	ret->endmark = FRAMEEND;
}

void FillErrorMsg(STRU_ErrorMsg* ret, ERROR_CODE err)
{
	ret->header[0] = FRAMEHEADER1;
	ret->header[1] = FRAMEHEADER2;
	ret->cmdtype = (uint8_t)ErrorMsg;
	ret->cmdlength = 1;
	ret->errcode = (uint8_t)err;
	ret->endmark = FRAMEEND;
}

void initialReceiver()
{
	SearchState = 0;
	memset(comBuffer, 0, sizeof(comBuffer));
	ContentLength = 0;
	pt = comBuffer;
	lastReceivedCmd = DebugMessage;
}

CMDTYPE getCmdTypeOfFrame(uint8_t *frame)
{
	CMDTYPE CT = (CMDTYPE)(*(frame + FRAMEHEADER_LENGTH_IN_BYTES));
	lastReceivedCmd = CT;
	return CT;
}

bool receiveFrame(uint8_t data)
{
	bool ret = false;
	*pt = data;
	if (SearchState == 0)
	{
		//find 0xeb
		if (data == FRAMEHEADER1)
		{
			pt++;
			SearchState = 1;
		}
	}
	else if (SearchState == 1)
	{
		//find 0x90
		if (data == FRAMEHEADER2)
		{
			pt++;
			SearchState = 2;
		}
		else
		{
			//reset pt to start
			pt = comBuffer;
			SearchState = 0;
		}
	}
	else if (SearchState == 2)
	{
		//cmd type validation
		if (data <= DebugMessage)
		{
			pt++;
			SearchState = 3;
		}
		else
		{
			pt = comBuffer;
			SearchState = 0;
		}
	}
	else if (SearchState == 3)
	{
		//frame length validation, header + cmdtype + length + end = 5bytes
		if (data <= (FRAME_MAX_LENGTH_IN_BYTES - FRAME_NONCONTENT_LENGTH))
		{
			pt++;
			ContentLength = data;
			SearchState = 4;
			counter = 0;
		}
		else
		{
			pt = comBuffer;
			SearchState = 0;
		}
	}
	else if (SearchState == 4)
	{
		//receive data
		if (counter < ContentLength)
		{
			counter++;
			pt++;
		}
		else if (data == FRAMEEND)
		{
			ret = true;
			pt = comBuffer;
			SearchState = 0;
			counter = 0;
		}
	}
	return ret;
}

#endif // !COMMUNICATION_H

