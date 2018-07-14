/*
 * Msg.cpp
 *
 *  Created on: 2017Äê2ÔÂ19ÈÕ
 *      Author: jerry
 */

#include "Msg.h"

Msg::Msg() {

}

Msg::Msg(CMDType CT)
{
	contentLen = 1;
	len = contentLen + 5;
	cmdType = CT;
	content = new uint8_t[contentLen];
	content[0] = 0;
}

Msg::Msg(int Content, CMDType CT) {
	contentLen = sizeof(Content);
	len = contentLen+5;
	cmdType = CT;
	content = new uint8_t[contentLen];
	memcpy(content, &Content, contentLen);
}

Msg::Msg(unsigned long Content, CMDType CT)
{
	contentLen = sizeof(Content);
	len = contentLen + 5;
	cmdType = CT;
	content = new uint8_t[contentLen];
	memcpy(content, &Content, contentLen);
}


Msg::Msg(float Content, CMDType CT)
{
	contentLen = sizeof(float);
	len = contentLen + 5;
	cmdType = CT;
	content = new uint8_t[contentLen];
	memcpy(content,&Content, contentLen);
}

Msg::Msg(double Content, CMDType CT)
{
	contentLen = sizeof(double);
	len = contentLen + 5;
	cmdType = CT;
	content = new uint8_t[contentLen];
	memcpy(content, &Content, contentLen);
}

Msg::Msg(String Content, CMDType CT = DebugMessage)
{
	contentLen = Content.length();
	len = contentLen + 5;
	cmdType = CT;
	content = new byte[contentLen];
	Content.toCharArray((char*)content, contentLen);
}

Msg::~Msg() {
	if (content != 0) {
		delete[] content;
	}
	if (buffer != 0) {
		delete[] buffer;
	}
}

byte* Msg::ToByteArray() {
	//Warning: allocate buffer with keyword new could cause memory issue even with delete in destructor
	buffer = new uint8_t[contentLen + 5];
	int Ender_Idx = content_Idx + contentLen;
	buffer[Header1_Idx] = HEADER1;
	buffer[Header2_Idx] = HEADER2;
	buffer[CMDType_Idx] = cmdType;
	buffer[contentLen_Idx] = contentLen;
	for (int i = 0; i < contentLen; i++) {
		//assemble content
		buffer[i + content_Idx] = content[i];
	}
	buffer[Ender_Idx] = ENDER;
	return buffer;
}

void Msg::ToByteArray(byte* buffer) {
	//Warning: allocate buffer with keyword new could cause memory issue even with delete in destructor
	int Ender_Idx = content_Idx + contentLen;
	buffer[Header1_Idx] = HEADER1;
	buffer[Header2_Idx] = HEADER2;
	buffer[CMDType_Idx] = cmdType;
	buffer[contentLen_Idx] = contentLen;
	for (int i = 0; i < contentLen; i++) {
		//assemble content
		buffer[i + content_Idx] = content[i];
	}
	buffer[Ender_Idx] = ENDER;
	return;
}

byte* Msg::GetContent()
{
	return content;
}


