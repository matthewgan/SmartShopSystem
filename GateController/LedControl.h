#ifndef LED_CONTROL_H
#define LED_CONTROL_H

//use WS2812 LED chipsets
#include <Adafruit_NeoPixel.h>
//#include "Pins.h"

#define LEDpin 10

Adafruit_NeoPixel LED = Adafruit_NeoPixel(60, LEDpin, NEO_GRB + NEO_KHZ400);

void LedInit()
{
	pinMode(LEDpin, OUTPUT);
	LED.begin();
	LED.show();
}

void LED_keepGreen()
{
	uint32_t col = LED.Color(0, 255, 0);//green
	for (uint16_t i = 0; i<LED.numPixels(); i++) {
		LED.setPixelColor(i, col);
		LED.show();		
	}
}

void LED_keepRed()
{
	uint32_t col = LED.Color(255, 0, 0);//red
	for (uint16_t i = 0; i<LED.numPixels(); i++) {
		LED.setPixelColor(i, col);
		LED.show();
	}
}


#endif
