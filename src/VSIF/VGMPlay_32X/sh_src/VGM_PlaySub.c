#include "mars.h"

#define A_MARS_SYS_IN_DATA	    (*(volatile uint16_t *)0x20004020) // COMM0
#define B_MARS_SYS_IN_DATA      (*(volatile uint16_t *)0x20004028)	// COMM8

#define A_MARS_SYS_OUT_ADDR     (*(volatile uint32_t *)0x20004022)	//COMM2
#define B_MARS_SYS_OUT_ADDR     (*(volatile uint32_t *)0x2000402A) 	//COMM10

#define A_MARS_SYS_OUT_DATA     (*(volatile uint8_t *)0x20004026)	//COMM6
#define B_MARS_SYS_OUT_DATA     (*(volatile uint8_t *)0x20004027) 	//COMM7

#define A_MARS_SYS_COUNTER      (*(volatile uint8_t *)0x2000402E) 	//COMM14
#define B_MARS_SYS_COUNTER      (*(volatile uint8_t *)0x2000402F) 	//COMM15

uint32 address_table[16] = {
		0xFF1000, // Dummy
		0xA04000, // YM2612 port 0
		0xA04001, // YM2612 port 1
		0xA04002, // YM2612 port 2
		0xA04003, // YM2612 port 3
		0xC00011, // PSG
		0xFF1000, // PWM Address & Data Hi
		0xFF1000, // PWM Data Lo

		0xFF1000, // Dummy
		0xFF1000, // Dummy
		0xFF1000, // Dummy
		0xFF1000, // Dummy
		0xFF1000, // Dummy
		0xFF1000, // Dummy
		0xFF1000, // Dummy
		0xFF1000, // Dummy
	};
	
void VGMPlay_32X() {
	uint8_t a_counter = 0;
	uint8_t a_currentData = 0;
	volatile uint16_t *a_pwmAddr = (volatile uint16_t *)0x20004030;

	uint8_t b_counter = 0;
	uint8_t b_currentData = 0;
	volatile uint16_t *b_pwmAddr = (volatile uint16_t *)0x20004030;

	for(;;)
	{
		uint8_t acnt = A_MARS_SYS_COUNTER;
		if(a_counter != acnt){
			a_counter = acnt;

			uint16 in_data = A_MARS_SYS_IN_DATA;

			uint16_t idx = (in_data >> 8) & 0x7;
			//idx = 5;
			A_MARS_SYS_OUT_ADDR = address_table[idx];
			a_currentData = ((in_data >> 6) & 0xc0) | (in_data & 0x3f);
			switch(idx)
			{
				case 6:
					// PWM Address
					a_pwmAddr = (volatile uint16_t *)(0x20004030 + (a_currentData >> 4));
					break;
				case 7:
					// PWM Data
					*a_pwmAddr = (((uint16_t)b_currentData & 0xf) << 8) | (uint16_t)a_currentData;
					break;
				default:
					// No PWM command
					A_MARS_SYS_OUT_DATA = (uint8)a_currentData;
					break;
			}
		}
		uint8_t bcnt = B_MARS_SYS_COUNTER;
		if(b_counter != bcnt){
			b_counter = bcnt;

			uint16 in_data = B_MARS_SYS_IN_DATA;

			uint16_t idx = (in_data >> 8) & 0x7;
			//idx = 5;
			B_MARS_SYS_OUT_ADDR = address_table[idx];
			b_currentData = ((in_data >> 6) & 0xc0) | (in_data & 0x3f);
			switch(idx)
			{
				case 6:
					// PWM Address
					b_pwmAddr = (volatile uint16_t *)(0x20004030 + (b_currentData >> 4));
					break;
				case 7:
					// PWM Data
					*b_pwmAddr = (((uint16_t)a_currentData & 0xf) << 8) | (uint16_t)b_currentData;
					break;
				default:
					// No PWM command
					B_MARS_SYS_OUT_DATA = (uint8)b_currentData;
					break;
			}
		}
	}
}

