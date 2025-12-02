    .globl VGMPlay_FTDI2XX_MDCD

    .equ    PERIPHERAL_PORT_P2_B, 0xA10005   | P2 PORT(Byte access)

|163840bps 46.82clk @ 7.670454 MHz (NTSC)
|115200bps 66.58clk @ 7.670454 MHz (NTSC)

VGMPlay_FTDI2XX_MDCD:
    move.b  #1,0xA11200
    move.b  #1,0xA11100
Reset_FTDI2XX:
    btst.b  #0,0xA11100
    bne.b   Reset_FTDI2XX

    move.l  #PERIPHERAL_PORT_P2_B, %a0 | PORT2 Address
    move.l  #ADRESS_TABLE, %a1         | Sound Register TABLE(OPNA2(Idx No 1,2,3,5), PSG(Idx No 6))
                                       | a2 is write address
    move.l  #_VGM_ADDRESS_FTDI2XX, %a3 | Jmp Address
    move.l  #0xFF0000,%a4              | PCM AREA ADRS
    move.l  #0xA1200E,%a5              | COMM CMD ADRS

    clr.l   %d0                        | for Recv
    move.b  #0xC0,%d1                  | for And Data(Hi 2bit)
    move.b  #6,%d2                     | for Check Bit 6
    move.w  #0,%d5                     | for PCM ADRS 
    move.l  #0xA00001,%d6              | for PCM ADRS MAGIC KEY

    move.b  #0x00, (%a0) 
    move.b  #0x00, (%a5) 

_VGM_ADDRESS_FTDI2XX:

_VGM_ADDRESS_FTDI2XX_LOOP:
    btst.b  %d2,(%a0)                         | +8 8    Check CLK
    beq.b   _VGM_ADDRESS_FTDI2XX_LOOP         | +8 16   Wait pullup
    move.b  (%a0),%d0                         | +8 24   Get Address Idx(Lo 4bit) and Data(Hi 2bit)
    | Get Write Address
    | 0CDDAAAA -> DDAAAA00
    add.b   %d0,%d0                           | +4 28   Shift Left
    add.b   %d0,%d0                           | +4 32   Shift Left
    move.l  (%d0.w, %a1), %a2                 |+16 48   Get Register Address

_VGM_DATA_FTDI2XX_LOOP:
    btst.b  %d2,(%a0)                         | +8 8    Check CLK
    bne.b   _VGM_DATA_FTDI2XX_LOOP            | +8 16   Wait pulldown
    and.b   %d1,%d0                           | +4 20   DDAAAA00 -> DD000000
    or.b    (%a0),%d0                         |  8 28   Get Data(Hi 2bit | Lo 6bit)
    | Write Data to Address
    move.b  %d0,(%a2)                         |+ 8 36   Write DATA to register

    cmpa.l  %d6,%a2                           |  6 42         
    bne     _VGM_ADDRESS_FTDI2XX              | 10/8    jump/not jump

||||||||||||||||||||||||||||

_PCM_PROC_ADDRESS_HI_LO:
    btst.b  %d2,(%a0)                         | +8 8    Check CLK
    beq.b   _PCM_PROC_ADDRESS_HI_LO           | +8 16   Wait pullup
    move.b  (%a0),(%a5)                       |+12 28
_PCM_PROC_ADDRESS_HI_HI:
    btst.b  %d2,(%a0)                         | +8 8    Check CLK
    bne.b   _PCM_PROC_ADDRESS_HI_HI           | +8 16   Wait pulldown
    move.b  (%a0),(%a5)                       |+12 28

_PCM_PROC_ADDRESS_LO_LO:
    btst.b  %d2,(%a0)                         | +8 8    Check CLK
    beq.b   _PCM_PROC_ADDRESS_LO_LO           | +8 16   Wait pullup
    move.b  (%a0),(%a5)                       |+12 28
_PCM_PROC_ADDRESS_LO_HI:
    btst.b  %d2,(%a0)                         | +8 8    Check CLK
    bne.b   _PCM_PROC_ADDRESS_LO_HI           | +8 16   Wait pulldown
    move.b  (%a0),(%a5)                       |+12 28

_PCM_PROC_DATA_HI:
    btst.b  %d2,(%a0)                         | +8 8    Check CLK
    beq.b   _PCM_PROC_DATA_HI                 | +8 16   Wait pullup
    move.b  (%a0),%d0                         |
    move.b  %d0,(%a5)                         |
_PCM_PROC_DATA_LO:
    btst.b  %d2,(%a0)                         | +8 8    Check Burst Flag
    bne.b   _PCM_PROC_DATA_LO                 | +8 16   Wait pulldown
    move.b  (%a0),(%a5)                       |+12 28

    jmp     (%a3)                             |+ 8 52   Loop

|■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

    .equ YMPORT0, 0xA04000 |; YM2612 port 0
    .equ YMPORT1, 0xA04001 |; YM2612 port 1
    .equ YMPORT2, 0xA04002 |; YM2612 port 2
    .equ YMPORT3, 0xA04003 |; YM2612 port 3
    .equ PSGPORT, 0xC00011 |; PSG port
    .equ DUMMY,   0xA00000 |; dummy memory
    .equ PCMKEY,  0xA00001 |; PCM MAGIC WORD(dummy memory)

ADRESS_TABLE:
    .rept 8
    dc.l DUMMY   		|;00
    dc.l YMPORT0 		|;04
    dc.l YMPORT1 		|;08
    dc.l YMPORT2 		|;0C
    dc.l YMPORT3 		|;10
    dc.l PSGPORT 		|;14
    dc.l PCMKEY 		|;18
    dc.l DUMMY 		    |;1c
    .endr

|■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

