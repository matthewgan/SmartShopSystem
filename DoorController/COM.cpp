/*
 * COM.cpp
 *
 *  Created on: 2017Äê2ÔÂ17ÈÕ
 *      Author: jerry
 */

#include "COM.h"
#include <Arduino.h>

COM::COM() {
	// TODO Auto-generated constructor stub
	ComPort = &Serial;
}

COM::COM(Stream& port) {
	ComPort = &port;
}

COM::~COM() {
	// TODO Auto-generated destructor stub
}

void COM::Write(byte* buffer, int BufferLen) {
	for (int i = 0; i < BufferLen; i++) {
		ComPort->write(buffer[i]);
	}
}

void COM::Write(Msg* msg) {
	byte* bufferOut = msg->ToByteArray();
	for (int i = 0; i < msg->len; i++) {
		ComPort->write(bufferOut[i]);
	}
}

void COM::Write(Msg msg) {
	byte* bufferOut = msg.ToByteArray();
	for (int i = 0; i < msg.len; i++) {
		ComPort->write(bufferOut[i]);
	}
}

void COM::ListenPortData(Msg* msgIn) {
	while (ComPort->available()) {
		byte dataIn = (byte)ComPort->read();
		ProcessData(dataIn, msgIn);
		if (msgIn->dataReady) {
			msgIn->len = msgIn->contentLen + msgIn->structLen;
		}
	}
}

void COM::ProcessData(byte data, Msg* msg) {
	switch (currentDecodeState) {
	case SearchForHeader1:
		if (data == 0xeb) {
			//SendDebugMsg("SearchForHeader1");
			currentDecodeState = SearchForHeader2;
		}
		break;

	case SearchForHeader2:
		if (data == 0x90) {
			//SendDebugMsg("SearchForHeader2");
			currentDecodeState = SearchForCmdType;
		}
		break;

	case SearchForCmdType:
		//SendDebugMsg("SearchForCmdType");
		msg->cmdType = (Msg::CMDType)data;
		currentDecodeState = SearchForLen;
		break;

	case SearchForLen:
		//SendDebugMsg("SearchForLen");
		msg->contentLen = data;
		currentDecodeState = GetContent;
		break;

	case GetContent:
		//SendDebugMsg("GetContent");
		if (msg->contentLen != 0) {
			msg->content = new byte[msg->contentLen];
			msg->content[0] = data;
			for (int i = 1; i < msg->contentLen; i++) {
				while (!ComPort->available()) {
				} //block the thread if there's no incoming data
				msg->content[i] = (byte)ComPort->read();
			}
			currentDecodeState = CheckEnder;
		} else {
			//SendDebugMsg("GetContent in");
			if (data == 0xfe) {
				msg->dataReady = true;
				currentDecodeState = SearchForHeader1;
			}
			else {
				delete msg;
				msg = new Msg();
				currentDecodeState = SearchForHeader1;
			}
		}
		break;

	case CheckEnder:
		//SendDebugMsg("CheckEnder");
		if (data == 0xfe) {
			msg->dataReady = true;
			currentDecodeState = SearchForHeader1;
		} else {
			delete msg;
			msg = new Msg();
			currentDecodeState = SearchForHeader1;
		}
		break;
	}
}

void COM::SendDebugMsg()
{
	Msg* debugMsg = new Msg("debug", Msg::DebugMessage);
	Write(debugMsg);
	delete debugMsg;
}

void COM::SendDebugMsg(String msg)
{
	msg = "dbgMsg: " + msg;
	Msg* debugMsg = new Msg(msg, Msg::DebugMessage);
	Write(debugMsg);
	delete debugMsg;
}

