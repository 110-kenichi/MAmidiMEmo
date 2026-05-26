
    .equ    PERIPHERAL_PORT_P2_B, 0xA10004  | P2 PORT(Byte access)

//https://github.com/matiaszanolli/sega-vr-disasm/blob/master/disasm/sh2/COMM_REGISTER_REFERENCE.md
    .equ     MARS_COMM0,		0xA15120    | Communication Register 0 (68 -> SH2) 
    .equ     MARS_COMM2,		0xA15122    | Communication Register 2 (68 -> SH2) 
    .equ     MARS_COMM6,		0xA15126

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
    move.w  #0x40,(%a1)
    move.l  #MARS_COMM2, %a2           | Get Addr A (SH2 -> 68) Long 
    move.l  #0xFF1000,(%a2)
    move.l  #MARS_COMM6, %a3           | Get Data A,B (SH2 -> 68)
    move.w  #0,(%a3)

    move.l  #MARS_COMM8, %a4           | Send Data B (68 -> SH2) 
    move.w  #0x40,(%a4)
    move.l  #MARS_COMM10, %a5          | Get Addr B (SH2 -> 68) Long 
    move.l  #0xFF1000,(%a5)

    move.l  #0xFF1000, %a6             | Dummy Address for SH2 to write
    move.b  #0x00, (%a6) 
    move.l  #_VGM_ADDRESS_32X, %a7     | Jmp Address

    move.b  #0x00,%d3                     | Counter A
    move.b  #0,4(%a5)                 |+12 36 Write Counter A
    move.b  #0x00,%d4                     | Counter B
    move.b  #0,5(%a5)                 |+12 36 Write Counter B

|move.l  #0xFF1002, %a0 | PORT2 Address
|move.b  #0x40,(%a0) | Set Bit 6 to indicate ready to SH2
|move.b  #0x00,(%a0) | Clear Bit 6 to indicate ready to SH2

|move.l  #0xC00011,a6 | PSG Port Address for SH2 to write

|move.b #0x80,0xC00011
|move.b #0x0f,0xC00011
|move.b #0x90,0xC00011

_VGM_ADDRESS_32X:
|jmp     (%a7)                       |+ 8 48 Loop

_VGM_ADDRESS_32X_LOOP_A:
    btst.b  %d2,(%a0)                   | +8 8  Check CLK
    beq.b   _VGM_ADDRESS_32X_LOOP_A     | +8 16 Wait pullup
    move.w  (%a0),%d1                   | +8 24 Addr & Hi Data to SH2 (0CDDAAAA -> DDxxxxxx & AAAA)

    addq.b  #1,%d3                          | +4 28 Increment Counter A

    move.l  (%a2),%a6                           |+12 40 Retrieve address A to FM/PSG
    move.b  (%a3),(%a6)                         |+12 52 Write data A to FM/PSG

_VGM_DATA_32X_LOOP_A:
    btst.b  %d2,(%a0)                   | +8 8  Check CLK
    bne.b   _VGM_DATA_32X_LOOP_A        | +8 16 Wait pulldown
    move.b  (%a0),%d1                   | +8 24 Pass Lo Data to SH2 (0CDDDDDD -> xxDDDDDD)
    move.w  %d1, (%a1)                  | +8 32 Write data to address A to SH2
    move.b  %d3,4(%a5)                      |+12 44 Write Counter A

    addq.b  #1,%d4                          | +4 48 Increment Counter B

_VGM_ADDRESS_32X_LOOP_B:
    btst.b  %d2,(%a0)                   | +8 8  Check CLK
    beq.b   _VGM_ADDRESS_32X_LOOP_B     | +8 16 Wait pullup
    move.w  (%a0),%d1                   | +8 24 Addr & Hi Data to SH2 (0CDDAAAA -> DDxxxxxx & AAAA)

    move.l  (%a5),%a6                           |+12 36 Retrieve address B to FM/PSG
    move.b  1(%a3),(%a6)                        |+16 52 Write data B to FM/PSG

_VGM_DATA_32X_LOOP_B:
    btst.b  %d2,(%a0)                   | +8 8  Check CLK
    bne.b   _VGM_DATA_32X_LOOP_B        | +8 16 Wait pulldown
    move.b  (%a0),%d1                   | +8 24 Pass Lo Data to SH2 (0CDDDDDD -> xxDDDDDD)
    move.w  %d1, (%a4)                  | +8 32 Write data to address B to SH2
    move.b  %d4,5(%a5)                      |+12 44 Write Counter B

    jmp     (%a7)                           |+ 8 52 Loop

|move.l #0xC00011, %a6
|move.b #0x80,(%a6)
|move.b #0x0f,(%a6)
|move.b #0x90,(%a6)

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
