    .globl VGMPlay
    .globl VGMPlay_Low
    .globl VGMPlay_FTDI2XX

    .equ    PERIPHERAL_PORT_P2, 0xA10004   | P2 PORT(Word access)
    .equ    PERIPHERAL_PORT_EXT, 0xA10006  | EXT PORT(Word access)

    |163840bps 46.32clk @ 7.600489 MHz (PAL)
    |115200bps 65.98clk @ 7.600489 MHz (PAL)

    |163840bps 46.82clk @ 7.670454 MHz (NTSC)
    |115200bps 66.58clk @ 7.670454 MHz (NTSC)

#define T163K
|#define T115K

|■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

    .macro  WAIT12
     move.w  (%sp),(%sp)         | 12    dummy
    .endm
    .macro  WAIT14
     move.w  (0,%pc,%d2),%d5     | 14    dummy
    .endm
    .macro  WAIT16
     move.w  ADRESS_TABLE.L,%d5  | 16    dummy
    .endm
    .macro  WAIT18
     move.l  (0,%pc,%d2),%d5     | 18    dummy
    .endm
    .macro  WAIT20               | 20
     move.l  (%sp),(%sp)         | 20    dummy
    .endm

|■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

    .macro start_bit_wait_163K
    move.l   %d2,%d1        | 4      for Receive Data
    WAIT20                  | 20
    NOP                     | 4
    .endm   | 28

    .macro start_bit_wait_115K
    move.l   %d2,%d1        | 4      for Receive Data
    WAIT20                  | 20
    WAIT20                  | 20
    NOP                     | 4
    .endm   | 48

    .macro start_bit_wait
    #ifdef T163K
        start_bit_wait_163K |28
    #endif
    #ifdef T115K
        start_bit_wait_115K |48
    #endif
    .endm

    .macro stop_bit_wait_163K
    NOP                     | 4
    NOP                     | 4
    NOP                     | 4
    .endm   | 12

    .macro stop_bit_wait_115K
    move.l  (%sp),(%sp)     | 20    dummy
    NOP                     | 4
    NOP                     | 4
    .endm   | 28

    .macro stop_bit_wait
    #ifdef T163K
        stop_bit_wait_163K  | 12
    #endif
    #ifdef T115K
        stop_bit_wait_115K  | 28
    #endif
    .endm

|■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

    .macro sample_bit_core
    roxr.w  (%a0)           |12
    roxr.b  %d3,%d1 		| 8
    .endm   | 20
    
    .macro sample_bit_163K
    sample_bit_core         | 20
    NOP                     | 4
    NOP                     | 4
    WAIT18                  | 18
	.endm   | 46

    .macro sample_bit_163K_2
    sample_bit_core         | 20
    WAIT20                  | 20
    NOP                     | 4
    NOP                     | 4
	.endm   | 48

	.macro sample_bit_115K
    NOP                     | 4
    sample_bit_core         | 20
    WAIT20                  | 20
    WAIT20                  | 20
    NOP                     | 4
	.endm   | 68
    
	.macro sample_bit_115K_2
    NOP                     | 4
    sample_bit_core         | 20
    NOP                     | 4
    WAIT20                  | 20
    WAIT18                  | 18
	.endm   | 66

    .macro sample_bit
	#ifdef T163K
        sample_bit_163K | 46
    #endif
	#ifdef T115K
        sample_bit_115K | 68
    #endif
	.endm

	.macro sample_bit_163K_nowait
    NOP                     | 4
    sample_bit_core         | 20
	.endm   | 24

	.macro sample_bit_115K_nowait
    NOP                     | 4
    sample_bit_core         | 20
	.endm   | 24

    .macro sample_bit_nowait
    #ifdef T163K
        sample_bit_163K_nowait   | 24
    #endif
    #ifdef T115K
        sample_bit_115K_nowait   | 24
    #endif
	.endm

    |163840bps 46.82clk @ 7.670454 MHz (NTSC)
    |163840bps 46.32clk @ 7.600489 MHz (PAL)

    .macro sample_bits_163K |
	sample_bit 		        | 46 +1.0 +1.5
	sample_bit 		        | 46
	sample_bit_163K_2 		| 48
	sample_bit 	        	| 46
	sample_bit 	        	| 46
	sample_bit_163K_2 		| 48
	sample_bit 	        	| 46
    .endm

    |115200bps 66.58clk @ 7.670454 MHz (NTSC)

    .macro sample_bits_115K
	sample_bit_115K 		| 68 +1.5
	sample_bit_115K_2 		| 66
	sample_bit_115K_2 		| 66
	sample_bit_115K 	    | 68
	sample_bit_115K_2 		| 66
	sample_bit_115K_2 		| 66
	sample_bit_115K 	    | 66
    .endm
    
    .macro sample_bits
    #ifdef T163K
        sample_bits_163K
    #endif
    #ifdef T115K
        sample_bits_115K
    #endif
    .endm

|■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

|163840bps 46.82clk @ 7.670454 MHz (NTSC)
|115200bps 66.58clk @ 7.670454 MHz (NTSC)

VGMPlay:
    move.b  #1,0xA11200
    move.b  #1,0xA11100
Reset:
    btst.b  #0,0xA11100
    bne.b   Reset

    move.l  #PERIPHERAL_PORT_P2, %a0   | 12
    move.l  #ADRESS_TABLE, %a1         | 12
    clr.l   %d2                        | for Test Bit 0
    move.l  #1,%d3                     | for Rotate Bit 
    move.b  #0xfc,%d4                  | for Address Table

_VGM_ADDRESS:
    btst.b  %d2,(%a0)       | +8
    bne.b   _VGM_ADDRESS    | +8  16
    NOP                     | +4  20   230K     163K     273K
    start_bit_wait          | +48 68 | +14 34 | +28 48 | +6 28

    sample_bits

  	sample_bit_nowait       |     32 |     24 |     24 |     24 |
    and.b  %d4,%d1          | + 4 36 | +4  28 | +4  28 |     28
    move.l (%d1, %a1), %a2  | +16 52 | +16    | +16 44 | +16   
    
    stop_bit_wait           | +28 28 | +4  20 | +12 12 | +0  16
    
_VGM_DATA:
    btst.b  %d2,(%a0)       | +8
    bne.b   _VGM_DATA       | +8  16
    NOP                     | +4  20
    start_bit_wait          | +48 68 | +12 34 | +24 46 | +6 28

    sample_bits

  	sample_bit_nowait       |     32 |     24 |     24 |     24 |
    move.b %d1, (%a2)       | +12 44 | +12    | +12 36 | +12

    nop
    nop
    nop                     | +12 12 |        | +12 12 |
    stop_bit_wait           | +28 40 |  +4    | +12 24 | +0
    bra.w _VGM_ADDRESS      | +10 50 | +10 26 | +10 33 | +10


|■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

    .macro sample_bits_lo
    sample_bits_115K
    .endm

    .macro sample_bit_nowait_lo
    sample_bit_115K_nowait
	.endm   | 24

    .macro start_bit_wait_lo
    start_bit_wait_115K
    .endm   | 48

    .macro stop_bit_wait_lo
    stop_bit_wait_115K
    .endm   | 28

|163840bps 46.82clk @ 7.670454 MHz (NTSC)
|115200bps 66.58clk @ 7.670454 MHz (NTSC)

VGMPlay_Low:
    move.b  #1,0xA11200
    move.b  #1,0xA11100
Reset_LO:
    btst.b  #0,0xA11100
    bne.b   Reset_LO

    move.l  #PERIPHERAL_PORT_P2, %a0   | 12
    move.l  #ADRESS_TABLE, %a1         | 12
    clr.l   %d2                        | for Test Bit 0
    move.l  #1,%d3                     | for Rotate Bit 
    move.b  #0xfc,%d4                  | for Address Table

_VGM_ADDRESS_LO:
    btst.b  %d2,(%a0)       | +8
    bne.b   _VGM_ADDRESS_LO | +8  16
    NOP                     | +4  20   230K     163K     273K
    start_bit_wait_lo       | +48 68 | +14 34 | +28 48 | +6 28

    sample_bits_lo

  	sample_bit_nowait_lo    |     24 |     24 |     24 |     24 |
    and.b  %d4,%d1          | + 4 28 | +4  28 | +4  28 |     28
    move.l (%d1, %a1), %a2  | +16 44 | +16    | +16 44 | +16   
    
    stop_bit_wait_lo        | +28 28 | +4  20 | +12 12 | +0  16
    
_VGM_DATA_LO:
    btst.b  %d2,(%a0)       | +8
    bne.b   _VGM_DATA_LO    | +8  16
    NOP                     | +4  20
    start_bit_wait_lo       | +48 68 | +12 34 | +24 46 | +6 28

    sample_bits_lo

  	sample_bit_nowait_lo    |     24 |     24 |     24 |     24 |
    move.b %d1, (%a2)       | +12 36 | +12    | +12 36 | +12

    nop
    nop
    nop                     | +12 12 |        | +12 12 |
    stop_bit_wait_lo        | +28 40 |  +4    | +12 24 | +0
    bra.w _VGM_ADDRESS_LO   | +10 50 | +10 26 | +10 34 | +10

|■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

    .equ    PERIPHERAL_PORT_P2_B, 0xA10005   | P2 PORT(Byte access)


|163840bps 46.82clk @ 7.670454 MHz (NTSC)
|115200bps 66.58clk @ 7.670454 MHz (NTSC)

VGMPlay_FTDI2XX:
    move.b  #1,0xA11200
    move.b  #1,0xA11100
Reset_FTDI2XX:
    btst.b  #0,0xA11100
    bne.b   Reset_FTDI2XX

    move.l  #PERIPHERAL_PORT_P2_B, %a0 | PORT2 Address
    move.l  #ADRESS_TABLE, %a1         | Sound Register TABLE(OPNA2(Idx No 1,2,3,5), PSG(Idx No 6))
    clr.l   %d0                        | for Recv Addr Idx(Lo 4bit)
    move.b  #0xC0,%d1                  | for And Data(Hi 2bit)
    move.b  #6,%d2                     | for Check Bit 6
    move.l  #_VGM_ADDRESS_FTDI2XX, %a3 | Jmp Address
    move.b  #0x00, (%a0) 

_VGM_ADDRESS_FTDI2XX:

_VGM_ADDRESS_FTDI2XX_LOOP:
    btst.b  %d2,(%a0)                         | +8 8    Check CLK
    beq.b   _VGM_ADDRESS_FTDI2XX_LOOP         | +8 16   Wait pullup
    move.b  (%a0),%d0                         | +8 24   Get Address Idx(Lo 4bit) and Data(Hi 2bit)
    | Get Write Address
    | 0CDDAAAA -> DDAAAA00
    lsl.b   #2,%d0                            |+10 34   Shift Left
    move.l  (%d0, %a1), %a2                   |+16 50   Get Register Address

_VGM_DATA_FTDI2XX_LOOP:
    btst.b  %d2,(%a0)                         | +8 8    Check CLK
    bne.b   _VGM_DATA_FTDI2XX_LOOP            | +8 16   Wait pulldown
    and.b   %d1,%d0                           |+ 4 20   
    or.b    (%a0),%d0                         |  8 28   Get Data(Hi 2bit | Lo 6bit)
    | Write Data to Address
    move.b  %d0,(%a2)                         |+ 8 36   Write DATA to register

    jmp     (%a3)                             |+ 8 44   Loop

|    move.l #0xC00011, %a6
|    move.b #0x80,(%a6)
|    move.b #0x0f,(%a6)
|    move.b #0x90,(%a6)

|■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

    .equ YMPORT0, 0xA04000 |; YM2612 port 0
    .equ YMPORT1, 0xA04001 |; YM2612 port 1
    .equ YMPORT2, 0xA04002 |; YM2612 port 2
    .equ YMPORT3, 0xA04003 |; YM2612 port 3
    .equ PSGPORT, 0xC00011 |; PSG port
    .equ CD_ADDR_HI,   0xA12010 |; COMM CMD Address Hi
    .equ CD_ADDR_LO,   0xA12011 |; COMM CMD Address Lo
    .equ CD_DATA,   0xA12012 |; COMM CMD DATA
    .equ DUMMY,   0xFF1000 |; dummy memory

ADRESS_TABLE:
    .rept 4
    dc.l DUMMY   		|;00
    dc.l YMPORT0 		|;04
    dc.l YMPORT1 		|;08
    dc.l YMPORT2 		|;0C
    dc.l YMPORT3 		|;10
    dc.l PSGPORT 		|;14
    dc.l DUMMY 		    |;18
    dc.l DUMMY 		    |;1C

    dc.l DUMMY 		    |
    dc.l DUMMY 		    |
    dc.l DUMMY 		    |
    dc.l DUMMY 		    |
    dc.l DUMMY 		    |
    dc.l DUMMY 		    |
    dc.l DUMMY 		    |
    dc.l DUMMY 		    |
    .endr

|■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

