#include "mars.h"
#include "string.h"
#include "images.h"

uint32_t lastTick = 0;
uint16_t currentFB = 0;

char joypadStateText[2][16];

uint16_t joypadState[2] = {0};
uint16_t joypadPrevState[2] = {0};

// Swap backbuffer and wait for next tick from MD
void swapBuffers() {
	while(lastTick == MARS_SYS_COMM12);
	MARS_VDP_FBCTL = currentFB ^ 1;
	while((MARS_VDP_FBCTL & MARS_VDP_FS) == currentFB);
	currentFB ^= 1;	
	lastTick = MARS_SYS_COMM12;
}

// Update joypad states
void joypad_update(uint8_t player) {
	joypadPrevState[player] = joypadState[player];
	HwMdReadPad(player);
	joypadState[player] = MARS_SYS_COMM8;
	// Print controller state to the screen
	const char btn[12] = "UDLRBCASZYXM";
	for(int i = 0; i < ((joypadState[player] & 0x1000) ? 12 : 8); i++) {
		if(joypadState[player] & (1 << i)) {
			joypadStateText[player][i] = btn[i];
		} else {
			joypadStateText[player][i] = '.';
		}
	}
	joypadStateText[player][12] = 0;
}

void StartVGMPlayer() {
	while(MARS_SYS_COMM0) ; // wait until 68000 has responded to any earlier requests
	MARS_SYS_COMM0 = 0x0400;
	//while(MARS_SYS_COMM0) ;
}

// marsdev環境のベースアドレスに準拠した定義
#define MARS_SYS_INT_CTRL  (*(volatile uint16_t *)0x05000000)
#define MARS_PWM_FIFO_FULL 0x8000

void Mars_Play_Beep_Short(void) {
    // 1. 【marsdev必須】68k側がバスを握っている(FM=1)間は、解放されるまで待つ
    // これを怠ると、直下のPWMレジスタ書き込みがすべてハードウェア的に無視されます
    while (MARS_SYS_INT_CTRL & 0x8000) {
        __asm__ __volatile__("nop");
    }

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
            while (MARS_PWM_CTRL & MARS_PWM_FIFO_FULL) { __asm__ __volatile__("nop"); }
            MARS_PWM_MONO = 850;
        }
        
        // ローレベル (11サンプル)
        for (volatile int i = 0; i < 1000; i++) {
            while (MARS_PWM_CTRL & MARS_PWM_FIFO_FULL) { __asm__ __volatile__("nop"); }
            MARS_PWM_MONO = 200;
        }
    }

    while (MARS_PWM_CTRL & MARS_PWM_FIFO_FULL) { __asm__ __volatile__("nop"); }

    // 4. 再生後の余韻待機（音が途切れて消滅するのを防ぐ）
    // for (volatile int delay = 0; delay < 8000; delay++) {
    //     __asm__ __volatile__("nop");
    // }

    // 5. 出力停止
    MARS_PWM_CTRL = 0x0000;
}

extern void	VGMPlay_32X();

// Primary CPU main loop
int m_main(void) {
	Hw32xInit(MARS_VDP_MODE_256, 0);
	Hw32xSetBGColor(0,0,0,0);
	Hw32xDelay(1); // Wait for MD's first VInt to complete

	HwMdPuts("MAMI VGM Player for 32X", 0x2000, 0, 0);
	StartVGMPlayer();	//Kick VGMPlayer

	Mars_Play_Beep_Short();

	VGMPlay_32X();

	//game loop
	for(;;) {
		// joypad_update(0);
		// joypad_update(1);
		// HwMdPuts(joypadStateText[0], 0x2000, 10, 16);
		// HwMdPuts(joypadStateText[1], 0x2000, 10, 18);

		//swapBuffers();
	}
	return 0;
}
