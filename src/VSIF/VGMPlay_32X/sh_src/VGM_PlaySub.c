#include "mars.h"

#define A_MARS_SYS_IN_DATA	    (*(volatile uint16_t *)0x20004020) // COMM0
#define B_MARS_SYS_IN_DATA      (*(volatile uint16_t *)0x20004028)	// COMM8

#define A_MARS_SYS_OUT_ADDR     (*(volatile uint32_t *)0x20004022)	//COMM2
#define B_MARS_SYS_OUT_ADDR     (*(volatile uint32_t *)0x2000402A) 	//COMM10

#define A_MARS_SYS_OUT_DATA     (*(volatile uint8_t *)0x20004026)	//COMM6
#define B_MARS_SYS_OUT_DATA     (*(volatile uint8_t *)0x20004027) 	//COMM7

#define MARS_UNCACHED_OFFSET    0x20000000

uint32 address_table[16] = {
		0xFF1000, // Dummy
		0xA04000, // YM2612 port 0
		0xA04001, // YM2612 port 1
		0xA04002, // YM2612 port 2
		0xA04003, // YM2612 port 3
		0xC00011, // PSG
		0xFF1000, // PWM Address & Data Hi
		0xFF1000, // PWM Data Lo

		0xA12010, // COMM CMD ADRS HI(68 -> CD)
		0xA12011, // COMM CMD ADRS LO(68 -> CD)
		0xA12012, // COMM CMD DATA (68 -> CD)
		0xFF1000, // PWM L +Diff
		0xFF1000, // PWM R +Diff
		0xFF1000, // PWM L -Diff
		0xFF1000, // PWM R -Diff
		0xFF1000, // PWM Mono +-Diff
	};

static uint16_t prev_val[5] = {
	0, 0, 0, 0, 0
};
volatile uint8_t g_pwmWriteHead = 0;
volatile uint8_t g_pwmWriteTail = 0;
volatile uint16_t g_pwmWriteEntries[256];

#define PWM_WRITE_HEAD (*(volatile uint8_t *)((uint32_t)&g_pwmWriteHead + MARS_UNCACHED_OFFSET))
#define PWM_WRITE_TAIL (*(volatile uint8_t *)((uint32_t)&g_pwmWriteTail + MARS_UNCACHED_OFFSET))
#define PWM_WRITE_ENTRIES ((volatile uint16_t *)((uint32_t)&g_pwmWriteEntries[0] + MARS_UNCACHED_OFFSET))

#define PWM_QUEUE_ENTRY(reg, data) (uint16_t)((((uint16_t)(reg)) << 12) | ((uint16_t)(data) & 0x0FFF))

void Mars_Play_Beep_Short(void);

#define PWM_ENQUEUE(sample) \
	do { \
		uint8_t next = (uint8_t)(pwmWriteHead + 1); \
		if (next == pwmWriteTail) { \
			pwmWriteTail = PWM_WRITE_TAIL; \
			if (next == pwmWriteTail) \
				break; \
		} \
		PWM_WRITE_ENTRIES[pwmWriteHead] = PWM_QUEUE_ENTRY(pwmReg, (sample)); \
		pwmWriteHead = next; \
		PWM_WRITE_HEAD = pwmWriteHead; \
		prev_val[pwmReg] = (sample); \
	} while (0)

void VGMPlay_32X_Sub() {
	uint8_t pwmReg = 4;
	uint16_t pwmHighData = 0;
	uint8_t pwmWriteHead = PWM_WRITE_HEAD;
	uint8_t pwmWriteTail = PWM_WRITE_TAIL;
	
	for(;;)
	{
        uint16_t in_data = A_MARS_SYS_IN_DATA;
		if(in_data != 0 && in_data == A_MARS_SYS_IN_DATA) // Check if there's new data in A_MARS_SYS_IN_DATA
		{
			A_MARS_SYS_IN_DATA = 0; // Clear the data after reading to prevent reprocessing the same command

			uint8_t idx = (in_data >> 8) & 0xF;
			A_MARS_SYS_OUT_ADDR = address_table[idx];
			uint16_t currentData = ((in_data >> 6) & 0xc0) | (in_data & 0x3f);
			switch(idx)
			{
				case 6:
					pwmReg = (currentData >> 4) & 0x07;
					pwmHighData = ((uint16_t)currentData & 0x0f) << 8;
					break;
				case 7:
					PWM_ENQUEUE(pwmHighData | (uint16_t)currentData);
					break;
				case 11:	// PWM L +Diff
					pwmReg = 2;
					PWM_ENQUEUE(prev_val[pwmReg] + currentData);
					break;
				case 12:	// PWM R +Diff
					pwmReg = 3;
					PWM_ENQUEUE(prev_val[pwmReg] + currentData);
					break;
				case 13:	// PWM L -Diff
					pwmReg = 2;
					PWM_ENQUEUE(prev_val[pwmReg] - currentData);
					break;
				case 14:	// PWM R -Diff
					pwmReg = 3;
					PWM_ENQUEUE(prev_val[pwmReg] - currentData);
					break;
				case 15:	// PWM Mono Diff
					pwmReg = 4;
					PWM_ENQUEUE(prev_val[pwmReg] + (int8_t)currentData);
					break;
				default:
					// No PWM command
					A_MARS_SYS_OUT_DATA = (uint8_t)currentData;
					break;
			}
		}
		
		in_data = B_MARS_SYS_IN_DATA;
		if(in_data != 0 && in_data == B_MARS_SYS_IN_DATA) // Check if there's new data in B_MARS_SYS_IN_DATA
		{
			B_MARS_SYS_IN_DATA = 0; // Clear the data after reading to prevent reprocessing the same command

			uint8_t idx = (in_data >> 8) & 0xF;
			B_MARS_SYS_OUT_ADDR = address_table[idx];
			uint16_t currentData = ((in_data >> 6) & 0xc0) | (in_data & 0x3f);
			switch(idx)
			{
				case 6:
					pwmReg = (currentData >> 4) & 0x07;
					pwmHighData = ((uint16_t)currentData & 0x0f) << 8;
					break;
				case 7:
					PWM_ENQUEUE(pwmHighData | (uint16_t)currentData);
					break;
				case 11:	// PWM L +Diff
					pwmReg = 2;
					PWM_ENQUEUE(prev_val[pwmReg] + currentData);
					break;
				case 12:	// PWM R +Diff
					pwmReg = 3;
					PWM_ENQUEUE(prev_val[pwmReg] + currentData);
					break;
				case 13:	// PWM L -Diff
					pwmReg = 2;
					PWM_ENQUEUE(prev_val[pwmReg] - currentData);
					break;
				case 14:	// PWM R -Diff
					pwmReg = 3;
					PWM_ENQUEUE(prev_val[pwmReg] - currentData);
					break;
				case 15:	// PWM Mono Diff
					pwmReg = 4;
					PWM_ENQUEUE(prev_val[pwmReg] + (int8_t)currentData);
					break;
				default:
					// No PWM command
					B_MARS_SYS_OUT_DATA = (uint8_t)currentData;
					break;
			}
		}
	}
}

#undef PWM_ENQUEUE
#undef PWM_QUEUE_ENTRY

