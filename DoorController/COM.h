/*
 * COM.h
 *
 *  Created on: 2017Äê2ÔÂ17ÈÕ
 *      Author: jerry
 */

#ifndef COM_H_
#define COM_H_

#include "Msg.h"

using namespace std;

class COM {
	enum DecodeStates {
		SearchForHeader1,
		SearchForHeader2,
		SearchForCmdType,
		SearchForLen,
		CheckEnder,
		GetContent,
	};

public:
	Stream* ComPort;
	//Msg* msgIn = new Msg();
	COM();
	COM(Stream& port);
	virtual ~COM();
	void Write(byte* buffer, int bufferLen);
	void Write(Msg* msg);
	void Write(Msg msg);
	void ListenPortData(Msg* msgIn);
	void ProcessData(byte data,  Msg* msg);
	void SendDebugMsg(String msg);
	void SendDebugMsg();

private:
DecodeStates currentDecodeState = SearchForHeader1;
Msg* debugMsg;
};

#endif /* COM_H_ */

