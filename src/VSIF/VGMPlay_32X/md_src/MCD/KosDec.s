| https://segaretro.org/Kosinski_compression#Decompression_code
| ---------------------------------------------------------------------------
| Kosinski decompression subroutine
| Inputs:
| a0 = compressed data location
| a1 = destination
| ---------------------------------------------------------------------------

| =============== S U B R O U T I N E =======================================

	.global KosDec

.macro _Kos_RunBitStream 
    dbf 	%d2,skip\@
    moveq   #7,%d2
    move.b  %d1,%d0
    swap    %d3
    bpl.s   skip\@
    move.b  (%a0)+,%d0            | get desc. bitfield
    move.b  (%a0)+,%d1            |
    move.b  (%a4,%d0.w),%d0            | reload converted desc. bitfield from a LUT
    move.b  (%a4,%d1.w),%d1            |
skip\@:
    .endm
| ---------------------------------------------------------------------------
 
KosDec:
    moveq   #7,%d7
    moveq   #0,%d0
    moveq   #0,%d1
    lea KosDec_ByteMap(%pc),%a4
    move.b  (%a0)+,%d0            | get desc field low-byte
    move.b  (%a0)+,%d1            | get desc field hi-byte
    move.b  (%a4,%d0.w),%d0            | reload converted desc. bitfield from a LUT
    move.b  (%a4,%d1.w),%d1            |
    moveq   #7,%d2               | set repeat count to 8
    moveq   #-1,%d3              | d3 will be desc field switcher
    clr.w   %d3              |
    bra.s   KosDec_FetchNewCode
 
KosDec_FetchCodeLoop:
    | code 1 (Uncompressed byte)
    _Kos_RunBitStream
    move.b  (%a0)+,(%a1)+
 
KosDec_FetchNewCode:
    add.b   %d0,%d0               | get a bit from the bitstream
    bcs.s   KosDec_FetchCodeLoop        | if code = 0, branch
 
    | codes 00 and 01
    _Kos_RunBitStream
    moveq   #0,%d4               | d4 will contain copy count
    add.b   %d0,%d0               | get a bit from the bitstream
    bcs.s   KosDec_Code_01
 
    | code 00 (Dictionary ref. short)
    _Kos_RunBitStream
    add.b   %d0,%d0               | get a bit from the bitstream
    addx.w  %d4,%d4
    _Kos_RunBitStream
    add.b   %d0,%d0               | get a bit from the bitstream
    addx.w  %d4,%d4
    _Kos_RunBitStream
    moveq   #-1,%d5
    move.b  (%a0)+,%d5            | d5 = displacement
 
KosDec_StreamCopy:
    lea (%a1,%d5),%a3
    move.b  (%a3)+,(%a1)+         | do 1 extra copy (to compensate for +1 to copy counter)
 
KosDec_copy:
    move.b  (%a3)+,(%a1)+
    dbf %d4,KosDec_copy
    bra.w   KosDec_FetchNewCode
| ---------------------------------------------------------------------------
KosDec_Code_01:
    | code 01 (Dictionary ref. long / special)
    _Kos_RunBitStream
    move.b  (%a0)+,%d6            | d6 = %LLLLLLLL
    move.b  (%a0)+,%d4            | d4 = %HHHHHCCC
    moveq   #-1,%d5
    move.b  %d4,%d5               | d5 = %11111111 HHHHHCCC
    lsl.w   #5,%d5               | d5 = %111HHHHH CCC00000
    move.b  %d6,%d5               | d5 = %111HHHHH LLLLLLLL
    and.w   %d7,%d4               | d4 = %00000CCC
    bne.s   KosDec_StreamCopy       | if CCC=0, branch
 
    | special mode (extended counter)
    move.b  (%a0)+,%d4            | read cnt
    beq.s   KosDec_Quit         | if cnt=0, quit decompression
    subq.b  #1,%d4
    beq.w   KosDec_FetchNewCode     | if cnt=1, fetch a new code
 
    lea (%a1,%d5),%a3
    move.b  (%a3)+,(%a1)+         | do 1 extra copy (to compensate for +1 to copy counter)
    move.w  %d4,%d6
    not.w   %d6
    and.w   %d7,%d6
    add.w   %d6,%d6
    lsr.w   #3,%d4
    jmp KosDec_largecopy(%pc,%d6.w)
 
KosDec_largecopy:
    .rept 8
    move.b  (%a3)+,(%a1)+
    .endr
    dbf %d4,KosDec_largecopy
    bra.w   KosDec_FetchNewCode
 
KosDec_Quit:
    rts
 
| ---------------------------------------------------------------------------
| A look-up table to invert bits order in desc. field bytes
| ---------------------------------------------------------------------------
 
KosDec_ByteMap:
    dc.b    0x00,0x80,0x40,0xC0,0x20,0xA0,0x60,0xE0,0x10,0x90,0x50,0xD0,0x30,0xB0,0x70,0xF0
    dc.b    0x08,0x88,0x48,0xC8,0x28,0xA8,0x68,0xE8,0x18,0x98,0x58,0xD8,0x38,0xB8,0x78,0xF8
    dc.b    0x04,0x84,0x44,0xC4,0x24,0xA4,0x64,0xE4,0x14,0x94,0x54,0xD4,0x34,0xB4,0x74,0xF4
    dc.b    0x0C,0x8C,0x4C,0xCC,0x2C,0xAC,0x6C,0xEC,0x1C,0x9C,0x5C,0xDC,0x3C,0xBC,0x7C,0xFC
    dc.b    0x02,0x82,0x42,0xC2,0x22,0xA2,0x62,0xE2,0x12,0x92,0x52,0xD2,0x32,0xB2,0x72,0xF2
    dc.b    0x0A,0x8A,0x4A,0xCA,0x2A,0xAA,0x6A,0xEA,0x1A,0x9A,0x5A,0xDA,0x3A,0xBA,0x7A,0xFA
    dc.b    0x06,0x86,0x46,0xC6,0x26,0xA6,0x66,0xE6,0x16,0x96,0x56,0xD6,0x36,0xB6,0x76,0xF6
    dc.b    0x0E,0x8E,0x4E,0xCE,0x2E,0xAE,0x6E,0xEE,0x1E,0x9E,0x5E,0xDE,0x3E,0xBE,0x7E,0xFE
    dc.b    0x01,0x81,0x41,0xC1,0x21,0xA1,0x61,0xE1,0x11,0x91,0x51,0xD1,0x31,0xB1,0x71,0xF1
    dc.b    0x09,0x89,0x49,0xC9,0x29,0xA9,0x69,0xE9,0x19,0x99,0x59,0xD9,0x39,0xB9,0x79,0xF9
    dc.b    0x05,0x85,0x45,0xC5,0x25,0xA5,0x65,0xE5,0x15,0x95,0x55,0xD5,0x35,0xB5,0x75,0xF5
    dc.b    0x0D,0x8D,0x4D,0xCD,0x2D,0xAD,0x6D,0xED,0x1D,0x9D,0x5D,0xDD,0x3D,0xBD,0x7D,0xFD
    dc.b    0x03,0x83,0x43,0xC3,0x23,0xA3,0x63,0xE3,0x13,0x93,0x53,0xD3,0x33,0xB3,0x73,0xF3
    dc.b    0x0B,0x8B,0x4B,0xCB,0x2B,0xAB,0x6B,0xEB,0x1B,0x9B,0x5B,0xDB,0x3B,0xBB,0x7B,0xFB
    dc.b    0x07,0x87,0x47,0xC7,0x27,0xA7,0x67,0xE7,0x17,0x97,0x57,0xD7,0x37,0xB7,0x77,0xF7
    dc.b    0x0F,0x8F,0x4F,0xCF,0x2F,0xAF,0x6F,0xEF,0x1F,0x9F,0x5F,0xDF,0x3F,0xBF,0x7F,0xFF
