#ifndef _MOTORTIMER_h
#define _MOTORTIMER_h

#if defined(ARDUINO) && ARDUINO >= 100
#include "arduino.h"
#else
#include "WProgram.h"
#endif

//preload timer 65535-16MHz/256/20Hz
static uint16_t preload = 62411;

//initialize timer1 for motor loop
void init_timer1()
{
	noInterrupts();
	TCCR1A = 0;
	TCCR1B = 0;
	TCNT1 = 0;

	//preload timer 65535-16MHz/256/20Hz
	OCR1A = 
}


#endif
