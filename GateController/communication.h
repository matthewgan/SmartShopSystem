#ifndef COMMUNICATION_H
#define COMMUNICATION_H

enum CMDTYPE
{
	IDLE = 0,               

  OpenDoor = 0x41,
	HeartBeatMsg,
	EndOfCmdtype
};

CMDTYPE lastReceivedCmd;

enum DeviceId
{
	Xs = 0,
	Ys,
	Zs,
	Rs,
	Roller,
	StampPad,
	INVALID
};

#define FRAMEHEADER1		(0xEB)
#define FRAMEHEADER2		(0x90)
#define FRAMEHEADER_LENGTH_IN_BYTES	2
#define FRAMEEND			(0xFE)
#define FRAME_MAX_LENGTH_IN_BYTES	24	
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

void initialReceiver()
{
	SearchState = 0;
	memset(comBuffer, 0, sizeof(comBuffer));
	ContentLength = 0;
	pt = comBuffer;
	lastReceivedCmd = EndOfCmdtype;
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
		if (data <= EndOfCmdtype)
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

