#ifndef Msg_H_
#define Msg_H_
#include <Arduino.h>

class Msg {

public:
	enum CMDType:byte
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

		OpenGate = 0x41,
		HeartBeatMsg,

		DebugMessage = 0xFF,
	};

	enum MsgIndex {
		Header1_Idx, Header2_Idx, CMDType_Idx, contentLen_Idx, content_Idx
	};

	byte HEADER1 = 0xEB;
	byte HEADER2 = 0x90;
	CMDType cmdType;
	byte contentLen;
	byte* content = 0;
	byte ENDER = 0xFE;
	byte len = 0;
	int structLen = 5;
	bool dataReady = false; //use for data inbound


	Msg();
	Msg(CMDType CT);
	Msg(int Content, CMDType CT);
	Msg(unsigned long Content, CMDType CT);
	Msg(float Content, CMDType CT);
	Msg(double Content, CMDType CT);
	Msg(String Content, CMDType CT = DebugMessage);
	virtual ~Msg();
	byte* GetContent();

	template<typename T>
	static void GetBytes(T var, byte* toArray, int& arrayLen);
	byte* ToByteArray();
	void ToByteArray(byte* bufffer);
	template<typename T>
	T GetContent(void);

private:
	byte* buffer = 0;
};

template<typename T>
void Msg::GetBytes(T var, byte* toArray, int& arrayLen) {
	for (int i = 0; i < arrayLen; i++) {
		toArray[i] = (var >> ((arrayLen - i - 1) * 8)) & 0xFF;
	}
}

template<typename T>
T Msg::GetContent(void)
{
	T var;
	if (contentLen == sizeof(var))
	{
		memcpy(&var, content, contentLen);
	}
	return var;
}

#endif

