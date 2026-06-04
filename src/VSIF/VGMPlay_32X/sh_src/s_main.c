#include "mars.h"

#define MARS_SYS_INT_CTRL  (*(volatile uint16_t *)0x05000000)
#define MARS_PWM_FIFO_FULL 0x8000
#define MARS_UNCACHED_OFFSET 0x20000000

extern volatile uint8_t g_pwmWriteHead[5];
extern volatile uint8_t g_pwmWriteTail[5];
extern volatile uint16_t g_pwmWriteEntries[5][256];

#define PWM_WRITE_HEAD_N(n) (*(volatile uint8_t *)((uint32_t)&g_pwmWriteHead[n] + MARS_UNCACHED_OFFSET))
#define PWM_WRITE_TAIL_N(n) (*(volatile uint8_t *)((uint32_t)&g_pwmWriteTail[n] + MARS_UNCACHED_OFFSET))
#define PWM_WRITE_ENTRIES_N(n) ((volatile uint16_t *)((uint32_t)&g_pwmWriteEntries[n][0] + MARS_UNCACHED_OFFSET))

static volatile uint16_t *const pwm_regs[5] = {
	(volatile uint16_t *)0x20004030,
	(volatile uint16_t *)0x20004032,
	(volatile uint16_t *)0x20004034,    // PWM L
	(volatile uint16_t *)0x20004036,    // PWM R
	(volatile uint16_t *)0x20004038,    // PWM Mono
};


void Mars_Play_Beep_Short_Slave(void) {
    // 1. 【marsdev必須】68k側がバスを握っている(FM=1)間は、解放されるまで待つ
    // これを怠ると、直下のPWMレジスタ書き込みがすべてハードウェア的に無視されます
    while (MARS_SYS_INT_CTRL & 0x8000) {}

    // 2. PWM回路の初期化と、エミュレータエンジンのキック
    MARS_PWM_CTRL  = 0x0000; 
    MARS_PWM_CYCLE = 1045;   // 22.05kHz 周期
    MARS_PWM_CTRL  = 0x0005; // L/R 再生有効

    // ダミーの無音（センター値）を4回書き込んでバッファを押し出す
    for (int i = 0; i < 4; i++) {
        MARS_PWM_MONO = 522;
    }

    // 3. 矩形波（BEEP音）の流し込み（約50ms〜100ms）
    // marsdevの最適化(-O2等)でループが消去されないよう volatile を付与
    for (volatile int loop = 0; loop < 50; loop++) {
        
        // ハイレベル (11サンプル)
        for (volatile int i = 0; i < 1000; i++) {
            // FIFOがフルなら1ステップ待つ
            while (MARS_PWM_CTRL & MARS_PWM_FIFO_FULL) {}
            MARS_PWM_MONO = 700;
        }
        
        // ローレベル (11サンプル)
        for (volatile int i = 0; i < 1000; i++) {
            while (MARS_PWM_CTRL & MARS_PWM_FIFO_FULL) {}
            MARS_PWM_MONO = 300;
        }
    }

    while (MARS_PWM_CTRL & MARS_PWM_FIFO_FULL) {}

    // 4. 再生後の余韻待機（音が途切れて消滅するのを防ぐ）
    for (volatile int delay = 0; delay < 8000; delay++) {
    }

    // 5. 出力停止
    MARS_PWM_CTRL = 0x0000;
}


void s_main(void) {
	uint8_t pwmTail[5] = {
		PWM_WRITE_TAIL_N(0), PWM_WRITE_TAIL_N(1), PWM_WRITE_TAIL_N(2),
		PWM_WRITE_TAIL_N(3), PWM_WRITE_TAIL_N(4)
	};

	while (MARS_SYS_INT_CTRL & 0x8000) {}
	
	MARS_PWM_CTRL = 0x0000;
	MARS_PWM_CYCLE = 1045;
	MARS_PWM_CTRL = 0x0005;
	MARS_PWM_MONO = 522;
	MARS_PWM_MONO = 522;
	MARS_PWM_MONO = 522;
	MARS_PWM_MONO = 522;

	//Mars_Play_Beep_Short_Slave();

	for(;;) {
		for (uint8_t reg = 0; reg < 5; reg++) {
			uint8_t pwmHead = PWM_WRITE_HEAD_N(reg);
			if (pwmTail[reg] != pwmHead) {
				uint16_t pwmData = PWM_WRITE_ENTRIES_N(reg)[pwmTail[reg]];
				pwmTail[reg] = (uint8_t)(pwmTail[reg] + 1);
				PWM_WRITE_TAIL_N(reg) = pwmTail[reg];
				switch (reg) {
					case 0:
						MARS_PWM_CTRL = pwmData;
						break;
					case 1:
						MARS_PWM_CYCLE = pwmData;
						break;
					default:
						while (*pwm_regs[reg] & MARS_PWM_FIFO_FULL) {}
						*pwm_regs[reg] = pwmData;
						break;
				}
			}
		}
	}
}
