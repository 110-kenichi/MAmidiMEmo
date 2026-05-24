| https://segaretro.org/images/2/2d/MCDHardware_Manual_PCM_Sound_Source.pdf
| https://www.retrodev.com/RF5C68A.pdf
| https://github.com/DevsArchive/mcd-mode-1-library
| https://segaretro.org/images/archive/6/6f/20190509144929%21Mega-CD_Software_Development_Manual.pdf
| https://github.com/viciious/SegaCDMode1PCM
| https://github.com/DarkMorford/scd-bios-disassembly/tree/master

    .globl VGMPlay_InitMCD

VGMPlay_InitMCD:
   	movem.l	%d1-%d7/%a0-%a6,-(%sp)

    jsr     FindMCDBIOS
    bcs     McdBiosNotFound

	jsr 	ResetSubCPU
    
    bsr.w	ReqSubCPUBus			| Request Sub CPU bus
	move.b	%d2,%d3
	bne.s	McdBiosNotFound			| If it failed to do that, branch

	move.b	#0,0xA12002				| Disable write protect on Sub CPU memory

    | Copy User program
    move.l  #CD_PCM_PROC,%a0
    move.l  #CD_PCM_PROC_END-CD_PCM_PROC,%d0
    move.l  #0,%d1
    jsr     CopyToPRGRAM
	bne.s	McdBiosNotFound

 WAIT_CD_INIT:
    cmpi.b  #0x55,0xa1200f
    bne     WAIT_CD_INIT

	move.b	#0x2A,0xA12002			| Enable write protect on Sub CPU memory
	bsr.w	ReturnSubCPUBus			| Return Sub CPU bus
   	movem.l	(%sp)+,%d1-%d7/%a0-%a6
    rts

McdBiosNotFound:
	move.b	#0x2A,0xA12002			| Enable write protect on Sub CPU memory
	bsr.w	ReturnSubCPUBus			| Return Sub CPU bus
   	movem.l	(%sp)+,%d1-%d7/%a0-%a6
    move.l  #1,%d0
    rts

|■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

.macro  Vectors
vector\@:
    dc.l    CD_Init - vector\@ + 4
.endm

CD_PCM_PROC:

_Vecteurs_68K:
    dc.l    CD_PCM_PROC_END - _Vecteurs_68K + 4             /* Stack address */
    .rept   255
Vectors
    .endr

    .equ GA_LED_STATUS, 0xFFFF8000
    .equ GA_LEDR, 2
    .equ GA_LEDG, 1
    .equ GA_RESET, 0xFFFF8001
    .equ GA_RES0, 0
    .equ GA_INT_MASK, 0xFFFF8033
    .equ GA_CDD_CONTROL, 0xFFFF8037
    .equ GA_MEMORY_MODE, 0xFFFF8003
	.equ GA_PM1, 4
	.equ GA_PM0, 3
	.equ GA_MODE, 2
	.equ GA_DMNA, 1
	.equ GA_RET, 0
	.equ GA_CDC_ADDRESS      , 0xFFFF8005
	.equ GA_CDC_REGISTER     , 0xFFFF8007
	.equ GA_CDC_DATA         , 0xFFFF8008   | Read-only
	.equ GA_CDC_DMA_ADDRESS  , 0xFFFF800A
	.equ GA_STOPWATCH        , 0xFFFF800C

    .equ GA_COMM_SUBFLAGS    , 0xFFFF800F
    .equ GA_COMM_SUBDATA     , 0xFFFF8020

    .equ CDC_WRITE_RESET, 0xF

    .equ GA_CDD_FADER        , 0xFFFF8034

CD_Init:
	clr.b   (GA_INT_MASK).w
    move.w  #0x2700, %sr               | DI

	| Trigger peripheral reset
	bclr #GA_RES0, (GA_RESET).w

	| Red LED on
	move.b #1, (GA_LED_STATUS).w

	| Set Word RAM to 2M mode
	lea (GA_MEMORY_MODE).w, %a0
loc_27C:
    btst  #GA_MODE, (%a0)
    beq.s loc_288
    bclr  #GA_MODE, (%a0)
    bra.s loc_27C
loc_288:
	| Give Word RAM to main CPU
	btst  #GA_DMNA, (%a0)
	beq.s loc_292
	bset  #GA_RET, (%a0)
loc_292:
	| Reset CDC (Sanyo LC89510)
	move.b #CDC_WRITE_RESET, (GA_CDC_ADDRESS).w
	move.b #0, (GA_CDC_REGISTER).w
	move.w #0, (GA_STOPWATCH).w

	| Clear communication registers
	moveq  #0, %d0
	move.b %d0, (GA_COMM_SUBFLAGS).w
	lea    (GA_COMM_SUBDATA).w, %a0
	move.l %d0, (%a0)+
	move.l %d0, (%a0)+
	move.l %d0, (%a0)+
	move.l %d0, (%a0)

	| Mute CD audio
	move.w #0, (GA_CDD_FADER).w

|    TESTSOUND
    move.b  #0xFF,0xFF0011  | KOFF

CD_Main:
    move.l  #0xFF800e,%a0              | COMM CMD Address
    move.l  #0xFF0000,%a4              | PCM AREA ADRS

    clr.l   %d0                        | for Recv 
    move.b  #0xC0,%d1                  | for And Data(Hi 2bit)
    move.b  #6,%d2                     | for Check Bit 6
                                       | d4 for PCM ADRS
    move.w  #2,%d5                     | for PCM burst flag 
    clr.l   %d4                        | for Recv 

    move.b  #0x55,0xFF800F             | Set OK

CD_PCM_LOOP:
CD_PCM_PROC_ADDRESS_HI_HI:
    btst.b  %d2,(%a0)                         | +8 8    Check CLK
    beq.b   CD_PCM_PROC_ADDRESS_HI_HI         | +8 16   Wait pullup
    | 0CAA0000
    move.b  (%a0),%d4                         | +8 24   
    | 0CAA0000 -> AA000000
    lsl.b   #2,%d4                            |+10 34   Shift Left
CD_PCM_PROC_ADDRESS_HI_LO:
    btst.b  %d2,(%a0)                         | +8 8    Check CLK
    bne.b   CD_PCM_PROC_ADDRESS_HI_LO         | +8 16   Wait pulldown
    | 00AAAAAA | AA000000
    or.b    (%a0),%d4                         |  8 24   

    | AAAAAAAA -> AAAAAAAA_00000000
    lsl.w   #8,%d4                            |+22 46   

CD_PCM_PROC_ADDRESS_LO_HI:
    btst.b  %d2,(%a0)                         | +8 8    Check CLK
    beq.b   CD_PCM_PROC_ADDRESS_LO_HI         | +8 16   Wait pullup
    | 0CAA0000
    move.b  (%a0),%d0                         | +8 24   
    | 0CAA0000 -> AA000000
    lsl.b   #2,%d0                            |+10 34   Shift Left
CD_PCM_PROC_ADDRESS_LO_LO:
    btst.b  %d2,(%a0)                         | +8 8    Check CLK
    bne.b   CD_PCM_PROC_ADDRESS_LO_LO         | +8 16   Wait pulldown
    | 00AAAAAA | AA000000
    or.b    (%a0),%d0                         |  8 24   

    | AAAAAAAA_00000000 | 00000000_AAAAAAAA
    or.b    %d0,%d4                           | +4 50  PCM Address(Hi+Lo)

CD_PCM_PROC_DATA_HI:
    btst.b  %d2,(%a0)                         | +8 8    Check CLK
    beq.b   CD_PCM_PROC_DATA_HI                 | +8 16   Wait pullup
    | 0CDD0000
    move.b  (%a0),%d0                         | +8 24

    | 0CDD0000 -> DD000000
    lsl.b   #2,%d0                            |+10 34   Shift Left

CD_PCM_PROC_DATA_LO:
    btst.b  %d2,(%a0)                         | +8 8    Check Burst Flag
    bne.b   CD_PCM_PROC_DATA_LO               | +8 16   Wait pulldown
    | 00DDDDDD | DD000000
    or.b    (%a0),%d0                         |  8 24   Get Data(Lo 2bit)
    | Write Data to Address
    move.b  %d0,(%a4, %d4.w)                  |+14 38   Write DATA to register

    bra     CD_PCM_LOOP                       |+10 48   Loop
CD_PCM_PROC_END:

|||||||||||||||||||||||| Debug

.macro  Waits
    move    #40,%d7
1:
    dbf     %d7,1b
.endm

.macro TESTSOUND
    move.b  #0x80,0xFF000F

    move.l  #0xFF2000,%a1              | PCM AREA ADRS
    move.l  #0xFFE/4,%d0
SET_PCM:
    move.w  #0xFDFD,(%a1)+
    Waits
    move.w  #0xFDFD,(%a1)+
    Waits
    move.w  #0x7F7F,(%a1)+
    Waits
    move.w  #0x7F7F,(%a1)+
    Waits
    dbf     %d0,SET_PCM
    move.w  #0xFFFF,(%a1)+
    Waits
    move.w  #0xFFFF,(%a1)+
    Waits
    move.w  #0xFFFF,(%a1)+
    Waits
    move.w  #0xFFFF,(%a1)+
    Waits

    move.b  #0xc0,0xFF000F  | SON ch0
    Waits
    move.b  #0x00,0xFF000D  | PCM ADR
    Waits
    move.b  #0x00,0xFF000B  | LOOP
    Waits
    move.b  #0x00,0xFF0009  | LOOP
    Waits
    move.b  #0x08,0xFF0007  | PITCH HI
    Waits
    move.b  #0x08,0xFF0005  | PITCH LO
    Waits
    move.b  #0xFF,0xFF0003  | PAN
    Waits
    move.b  #0xFF,0xFF0001  | VOL
    Waits
    move.b  #0xFE,0xFF0011  | KON
    Waits

||||||||||||||||||||||||
    .endm
