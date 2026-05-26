#include "common.h"
#include "VGMPlayZ80.z80.h"

// 32X COMM
static volatile uint16_t* const mars_comm0  = (uint16_t*) MARS_COMM0;
static volatile uint16_t* const mars_comm2  = (uint16_t*) MARS_COMM2;
static volatile uint16_t* const mars_comm8  = (uint16_t*) MARS_COMM8;
static volatile uint32_t* const mars_comm12 = (uint32_t*) MARS_COMM12;

// VDP
static volatile uint16_t* const vdp_data_port = (uint16_t*) VDP_DATA_PORT;
static volatile uint16_t* const vdp_ctrl_port = (uint16_t*) VDP_CTRL_PORT;
static volatile uint32_t* const vdp_ctrl_wide = (uint32_t*) VDP_CTRL_PORT;

extern void VGMPlay_32X();


extern int VGMPlay_InitMCD();
extern void VGMPlay_FTDI2XX_MDCD();


// External functions
extern uint16_t read_joypad(uint8_t player);

uint32_t timer = 0;
uint16_t vramOffset = 0;

// It is recommended to put functions that run 1+ times every frame into RAM
// by specifying this attribute before the signature. This keeps the M68K off
// the ROM so the SH-2s can access it without slowdown.
// It should be safe to add or remove it from any function and experiment with
// the speed vs space differences

__attribute__((section(".data")))
void vdp_color(uint16_t index, uint16_t color) {
	index <<= 1;
	*vdp_ctrl_wide = ((0xC000 + (((uint32_t)index) & 0x3FFF)) << 16) + (((uint32_t)index) >> 14);
	*vdp_data_port = color;
}

__attribute__((section(".data")))
void do_commands(void) {
	uint16_t cmd = *mars_comm0;
	switch(cmd >> 8) {
	default: break; // Unknown command
	case 0: return; // No command
	case 3:
		*mars_comm8 = read_joypad(cmd);
		break;
	case 4:
		VGMPlay_32X();	// Infinite loop that runs the VGM player until the next VInt, where it will return control to the main loop and allow the next command to be processed. This is necessary to keep the player in sync with the music and allow for real-time commands like changing colors or stopping playback.
		break;
	case 5: // Set VRAM or Plane offset
		vramOffset = *mars_comm2;
		break;
	case 6: // Write tile to Plane B
		*vdp_ctrl_wide = (((uint32_t)0x6000 + ((vramOffset) & 0x3FFF)) << 16) + (((vramOffset) >> 14) | 0x03);
		*vdp_data_port = *mars_comm2;
		vramOffset += 2;
		break;
	case 7: // Write word to VRAM address
		*vdp_ctrl_wide = (((uint32_t)0x4000 + ((vramOffset) & 0x3FFF)) << 16) + (((vramOffset) >> 14) | 0x00);
		*vdp_data_port = *mars_comm2;
		vramOffset += 2;
		break;
	}
	*mars_comm0 = 0;
}

const uint16_t color_cycle[10] = { 0xEEE, 0xCCC, 0xAAA, 0x888, 0x666, 0x444, 0x666, 0x888, 0xAAA, 0xCCC };

void HwMdSetOffset(unsigned short offset) {
	vramOffset = offset;
}

void HwMdSetNTable(unsigned short word) {
		*vdp_ctrl_wide = (((uint32_t)0x6000 + ((vramOffset) & 0x3FFF)) << 16) + (((vramOffset) >> 14) | 0x03);
		*vdp_data_port = word;
		vramOffset += 2;
}

static void NextChr(char c, uint16_t color) {
	if(c >= '0' && c <= '9') {
		c = c - '0' + 2;
	} else if(c >= 'A' && c <= 'Z') {
		c = c - 'A' + 12;
	} else if(c >= 'a' && c <= 'z') {
		c = c - 'a' + 12;
	} else if(c == ' ') {
		c = 0;
	} else {
		c = 1;
	}
	// タイル番号は下位10ビット、属性は上位ビットのみ合成
	HwMdSetNTable(c | color);
}

void HwMdPuts(char *str, uint16_t color, int x, int y) {
	HwMdSetOffset(((y<<6) | x) << 1);
	while(*str) NextChr(*str++, color);
}

void HwMdPutc(char chr, uint16_t color, int x, int y) {
	HwMdSetOffset(((y<<6) | x) << 1);
	NextChr(chr, color);
}

extern uint16_t SYS_setInterruptMaskLevel(uint16_t level);
extern uint16_t SYS_disableInts(void);
extern void YM2612_reset(void);
extern void PSG_init(void);
extern void Z80_upload(const void* src, uint32_t dst, uint32_t size);


__attribute__((section(".data")))
void main(void) {
	uint16_t ticks = 0, col = 0;

	SYS_setInterruptMaskLevel(7); /* disable ints */
  	YM2612_reset();
  	PSG_init();
	Z80_upload(md_src_VGMPlayZ80_z80_bin, 0, md_src_VGMPlayZ80_z80_bin_len);

	SYS_disableInts();

    HwMdPuts("MAMI VGM SOUND DRIVER BY ITOKEN", 0x8000, 0, 0);
	if(VGMPlay_InitMCD() != 0)
	{
	    HwMdPuts("SUPER 32X MODE",0x2000, 0, 2);
	}else
	{
	    HwMdPuts("SUPER 32X AND MDCD MODE",0x2000, 0, 2);
	}

	//HwMdPuts("MAMI VGM Player for 32X", 0x0000, 0, 0);
	
	while(1) {
		// Cycle background/border color
		if(++ticks >= 8) {
			ticks = 0;
			if(++col >= 10) col = 0;
		}
		vdp_color(0, color_cycle[col]);
		// TODO: Remove this after fixing _vblank
		while(*vdp_ctrl_port & 8) do_commands();
		while(!(*vdp_ctrl_port & 8)) do_commands();
		*mars_comm12 = ++timer;
	}
}
