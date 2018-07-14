/*
 Name:		EncoderController.ino
 Created:	2018/7/8 16:47:48
 Author:	matth
*/
#include "communication.h"
#include "encoder.h"

// the setup function runs once when you press reset or power the board
void setup() {
	Serial.begin(9600);
	delay(500);

	enc_init();
}

// the loop function runs over and over again until power down or reset
void loop() {
	
}
