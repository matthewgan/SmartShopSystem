#pragma once
#include "config.h"

void initialWeightSensor() {
	pinMode(WeightSensor, INPUT);
}

int readWeight() {
	int load = analogRead(WeightSensor);
	load = map(load, 0, 255, 0, 300);
	return load;
}

bool CheckCustomerOnPlatform()
{
	int weight = readWeight();
	return (weight > weightThreshold) ? true : false;
}
