#include "mars.h"

#define MARS_SYS_INT_CTRL  (*(volatile uint16_t *)0x05000000)
#define MARS_PWM_FIFO_FULL 0x8000
#define MARS_UNCACHED_OFFSET 0x20000000

extern volatile uint16_t g_pwmWriteHead;
extern volatile uint16_t g_pwmWriteTail;
extern volatile uint16_t g_pwmWriteEntries[1024];

#define PWM_WRITE_HEAD (*(volatile uint16_t *)((uint32_t)&g_pwmWriteHead + MARS_UNCACHED_OFFSET))
#define PWM_WRITE_TAIL (*(volatile uint16_t *)((uint32_t)&g_pwmWriteTail + MARS_UNCACHED_OFFSET))
#define PWM_WRITE_ENTRIES ((volatile uint16_t *)((uint32_t)&g_pwmWriteEntries[0] + MARS_UNCACHED_OFFSET))

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
    uint16_t pwmTail = PWM_WRITE_TAIL;

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
        uint16_t pwmHead = PWM_WRITE_HEAD;
		if (pwmTail != pwmHead) {
            uint16_t pwmEntry = PWM_WRITE_ENTRIES[pwmTail];
            uint8_t pwmReg = (uint8_t)((pwmEntry >> 12) & 0x07);
            uint16_t pwmData = pwmEntry & 0x0FFF;
			pwmTail = (uint16_t)((pwmTail + 1) & 0x03FF);
            PWM_WRITE_TAIL = pwmTail;

			while (*pwm_regs[pwmReg] & MARS_PWM_FIFO_FULL) {}
			*pwm_regs[pwmReg] = pwmData;
		}
	}
}
