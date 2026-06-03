
    .equ    PERIPHERAL_PORT_P2_B, 0xA10004  | P2 PORT(Byte access)

//https://github.com/matiaszanolli/sega-vr-disasm/blob/master/disasm/sh2/COMM_REGISTER_REFERENCE.md
    .equ     MARS_COMM0,		0xA15120    | Communication Register 0 (68 -> SH2) 
    .equ     MARS_COMM2,		0xA15122    | Communication Register 2 (68 -> SH2) 
    .equ     MARS_COMM6,		0xA15126
    .equ     MARS_COMM7,		0xA15127

    .equ     MARS_COMM8,		0xA15128
    .equ     MARS_COMM10,		0xA1512A

    .equ     MARS_COMM14,		0xA1512E
    .equ     MARS_COMM15,		0xA1512F

|163840bps 46.82clk @ 7.670454 MHz (NTSC)
|115200bps 66.58clk @ 7.670454 MHz (NTSC)

    .globl VGMPlay_32X

    .text

VGMPlay_32X:
    | Z80バスを取得
    move.b  #1,0xA11200
    | Z80リセット
    move.b  #1,0xA11100
_Reset_Z80:
    btst.b  #0,0xA11100
    bne.b   _Reset_Z80

    move.l  #PERIPHERAL_PORT_P2_B, %a0 | PORT2 Address
    move.b  #6,%d2                     | for Check Bit 6
    
    |https://segaretro.org/Sega_Mega_Drive/Control_pad_inputs
    move.b  #0x00,0xA1000B  | Set all read

    move.l  #MARS_COMM0, %a1           | Send Data A (68 -> SH2) 
    move.w  #0x0000,(%a1)
    move.l  #MARS_COMM2,%a2            | Get Addr A (SH2 -> 68) Long 
    move.l  #0xFF1000,(%a2)
                                       | Get Data A (SH2 -> 68)
    move.b  #0x00,0x2(%a2)

    move.l  #MARS_COMM8, %a4           | Send Data B (68 -> SH2) 
    move.w  #0x0000,(%a4)
    move.l  #MARS_COMM10,%a3           | Get Addr B (SH2 -> 68) Long 
    move.l  #0xFF1000,(%a3)
    move.l  #MARS_COMM7, %a5           | Get Data B (SH2 -> 68) 
    move.b  #0x00,(%a5)

    move.l  #0xFF1000, %a6             | Dummy Address for SH2 to write
    move.b  #0x00, (%a6) 

    move.l  #_VGM_ADDRESS_32X, %a7     | Jmp Address

    move.w  #0x0000,%d1                     | VSIF Data A
    move.w  #0x0000,%d3                     | VSIF Data B

|move.l  #0xFF1002, %a0 | PORT2 Address
|move.b  #0x40,(%a0) | Set Bit 6 to indicate ready to SH2
|move.b  #0x00,(%a0) | Clear Bit 6 to indicate ready to SH2

|move.l  #0xC00011,a6 | PSG Port Address for SH2 to write

|move.b #0x80,0xC00011
|move.b #0x0f,0xC00011
|move.b #0x90,0xC00011

| ======================================================================
| Macro: VGM_32X_BLOCK suf
| ======================================================================
    .macro  VGM_32X_BLOCK suf
_VGM_ADDRESS_32X_LOOP_A\suf:
    btst.b  %d2,(%a0)                   | +8 8  Check CLK
    beq.b   _VGM_ADDRESS_32X_LOOP_A\suf | +8 16 Wait pullup
    move.w  (%a0),%d1                   | +8 24 (A) VSIF Addr & Hi Data to SH2 (0CDDAAAA -> DDxxxxxx & AAAA)

    move.w  %d3, (%a4)                      | +8 32 (B) Write VSIF 16bit data B to SH2

    move.l  (%a2),%a6                           |+12 44 (A) Get FM/PSG address

_VGM_DATA_32X_LOOP_A\suf:
    btst.b  %d2,(%a0)                   | +8 8  Check CLK
    bne.b   _VGM_DATA_32X_LOOP_A\suf    | +8 16 Wait pulldown
    move.b  (%a0),%d1                   | +8 24 (A) Lo Data to SH2 (0CDDDDDD -> xxDDDDDD)

    move.b  4(%a2),(%a6)                        |+16 40 (A) Write FM/PSG data

_VGM_ADDRESS_32X_LOOP_B\suf:
    btst.b  %d2,(%a0)                   | +8 8  Check CLK
    beq.b   _VGM_ADDRESS_32X_LOOP_B\suf | +8 16 Wait pullup
    move.w  (%a0),%d3                   | +8 24 (B) VSIF Addr & Hi Data to SH2 (0CDDAAAA -> DDxxxxxx & AAAA)

    move.w  %d1, (%a1)                      | +8 32 (A) Write VSIF 16bit data A to SH2

    move.l  (%a3),%a6                           |+12 44 (B) Get FM/PSG address

_VGM_DATA_32X_LOOP_B\suf:
    btst.b  %d2,(%a0)                   | +8 8  Check CLK
    bne.b   _VGM_DATA_32X_LOOP_B\suf    | +8 16 Wait pulldown
    move.b  (%a0),%d3                   | +8 24 (B) VSIF Lo Data to SH2 (0CDDDDDD -> xxDDDDDD)

    move.b  (%a5),(%a6)                         |+12 36 (B) Write FM/PSG data

                                        |+ 8 44 Loop
    .endm

_VGM_ADDRESS_32X:

    VGM_32X_BLOCK 1

|■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

    jmp     (%a7)                       |+ 8 42 Loop

|■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

//PWM Registers
//https://github.com/matiaszanolli/sega-vr-disasm/blob/master/docs/32x-hardware-manual.md#34-pwm

    .equ YMPORT0, 0xA04000 |; YM2612 port 0
    .equ YMPORT1, 0xA04001 |; YM2612 port 1
    .equ YMPORT2, 0xA04002 |; YM2612 port 2
    .equ YMPORT3, 0xA04003 |; YM2612 port 3
    .equ PSGPORT, 0xC00011 |; PSG port
    .equ DUMMY,   0xFF1000 |; dummy memory
    .equ COMM_CMD_ADRS_HI, 0xA12010 |; COMM CMD ADRS HI(68 -> CD)
    .equ COMM_CMD_ADRS_LO, 0xA12011 |; COMM CMD ADRS LO(68 -> CD)
    .equ COMM_CMD_DATA,    0xA12012 |; COMM CMD DATA (68 -> CD)

| ADRESS_TABLE:
|     dc.l DUMMY   		|;00
|     dc.l YMPORT0 		|;04
|     dc.l YMPORT1 		|;08
|     dc.l YMPORT2 		|;0C
|     dc.l YMPORT3 		|;10
|     dc.l PSGPORT 		|;14
|     dc.l DUMMY 		|;18 PWM Address & Data Hi
|     dc.l DUMMY 		|;1C PWM Data Lo
