#pragma once
//SSI Encoder pin
#define enc_DO  6
#define enc_CLK 5
#define enc_CS  4
const int BIT_COUNT = 10; // this's the percision of rotary encoder.

void enc_init() {
	pinMode(enc_DO, INPUT);
	pinMode(enc_CLK, OUTPUT);
	pinMode(enc_CS, OUTPUT);


	digitalWrite(enc_CLK, HIGH);
	digitalWrite(enc_CS, HIGH);
}

float enc_read() {
	digitalWrite(enc_CS, LOW);
	delayMicroseconds(3);
	unsigned long sample1 = shiftIn(enc_DO, enc_CLK, BIT_COUNT);
	delayMicroseconds(3);  // Clock must be high for 20 microseconds before a new sample can be taken

						   //return sample1;
	return ((sample1 & 0x0FFF) * 360UL) / 1024.0; // ouptut value from 0 to 360 with two point percision
}

//read in a byte of data from the digital input of the board.
unsigned long shiftIn(const int data_pin, const int clock_pin, const int bit_count) {
	unsigned long data = 0;

	for (int i = 0; i<bit_count; i++) {
		data <<= 1; // shift all read data left one bit.
					/*
					// speed up I/O In order to meet the communication speed of this encoder.
					The correct form is:
					PORTD &= ~(1 << n); // Pin n goes low
					PORTD |= (1 << n); // Pin n goes high
					So:
					PORTD &= ~(1 << PD0); // PD0 goes low
					PORTD |= (1 << PD0); // PD0 goes high

					PORTD &= ~(1 << PD1); // PD1 goes low
					PORTD |= (1 << PD1); // PD1 goes high
					*/

					//digitalWrite(clock_pin,LOW);
		PORTD &= ~(1 << 5); // clock pin goes low
		delayMicroseconds(3);
		//digitalWrite(clock_pin,HIGH);
		PORTD |= (1 << 5); // lock pin goes high
		delayMicroseconds(3);

		data |= digitalRead(data_pin); // cat the new read bit to the whole read data.
	}
	digitalWrite(enc_CS, HIGH);
	delayMicroseconds(3);
	return data;
}
