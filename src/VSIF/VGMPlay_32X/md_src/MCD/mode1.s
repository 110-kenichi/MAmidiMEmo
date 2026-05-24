| https://github.com/DevsArchive/mcd-mode-1-library
| -------------------------------------------------------------------------
| Sega CD Mode 1 Library
| Ralakimus 2022
| -------------------------------------------------------------------------

| -------------------------------------------------------------------------
| Initialize the Sub CPU
| -------------------------------------------------------------------------
| NOTES:
|	* This assumes that the Sega CD is present and that you have
|	  the Sub CPU BIOS code ready to go. Call FindMCDBIOS before this
|
|	* Sub CPU boot requires that we send it level 2 interrupt requests.
|	  After calling this, make sure you enable vertical interrupts
|	  and have your handler call SendMCDInt2. Then you can properly
|	  wait for the Sub CPU to boot and initialize.
| -------------------------------------------------------------------------
| PARAMETERS:
|	a0.l - Pointer to compressed Sub CPU BIOS code
|	a1.l - Pointer to user Sub CPU program
|	d0.l - Size of user Sub CPU program
| RETURNS:
|	d0.b - Error codes
|	       Bit 0 - MCD took too long to respond
|	       Bit 1 - Failed to load user Sub CPU
| -------------------------------------------------------------------------

	.global	InitSubCPU
	.global	FindMCDBIOS
	.global	SendMCDInt2
	.global	CopyNewUserSP
	.global	ResetSubCPU
	.global CopyToPRGRAM
	.global ReqSubCPUBus
	.global ReturnSubCPUBus

InitSubCPU:
	bsr.s	ResetSubCPU				| Reset the Sub CPU

	bsr.w	ReqSubCPUBus			| Request Sub CPU bus
	move.b	%d2,%d3
	bne.s	ReturnBus				| If it failed to do that, branch

	move.b	#0,0xA12002				| Disable write protect on Sub CPU memory

	movem.l	%d0/%d3/%a1,-(%sp)		| Decompress Sub CPU BIOS into PRG RAM
	lea		0x420000,%a1
	jsr		KosDec
	movem.l	(%sp)+,%d0/%d3/%a1

	movea.l	%a1,%a0					| Copy user Sub CPU program into PRG RAM
	move.l	#0x6000,%d1
	bsr.w	CopyPRGRAMData
	or.b	%d2,%d3

ReturnBus:
	move.b	#0x2A,0xA12002			| Enable write protect on Sub CPU memory
	bsr.w	ReturnSubCPUBus			| Return Sub CPU bus
	or.b	%d2,%d3					| Set error code

	move.b	%d3,%d0					| Get return code
	rts

| -------------------------------------------------------------------------
| Copy new user Sub CPU program into PRG RAM and reset the Sub CPU
| -------------------------------------------------------------------------
| PARAMETERS:
|	a0.l - Pointer to Sub CPU program to copy
|	d0.l - Size of Sub CPU program to copy
| RETURNS:
|	d0.b - Error codes
|	       Bit 0 - MCD took too long to respond
|	       Bit 1 - Failed to load user Sub CPU
| -------------------------------------------------------------------------

CopyNewUserSP:
	bsr.s	ResetSubCPU			| Reset the Sub CPU
	move.l	#0x6000,%d1			| Copy to user Sub CPU program area

| -------------------------------------------------------------------------
| Copy data into PRG RAM
| -------------------------------------------------------------------------
| PARAMETERS:
|	a0.l - Pointer to data to copy
|	d0.l - Size of data to copy
|	d1.l - Destination offset in PRG RAM
| RETURNS:
|	d0.b - Error codes
|	       Bit 0 - MCD took too long to respond
|	       Bit 1 - Failed to load user Sub CPU
| -------------------------------------------------------------------------

CopyToPRGRAM:
	bsr.s	ReqSubCPUBus			| Request Sub CPU bus
	move.b	%d2,%d3
	bne.s	ReturnBus2			| If it failed to do that, branch

	move.b	0xA12002,%d3			| Save write protect settings on Sub CPU memory
	move.b	#0,0xA12002			| Disable write protect on Sub CPU memory

	bsr.w	CopyPRGRAMData			| Copy data to PRG-RAM
	or.b	%d2,%d3

	move.b	%d3,0xA12002			| Restore write protect on Sub CPU memory

ReturnBus2:
	bsr.s	ReturnSubCPUBus			| Return Sub CPU bus
	or.b	%d2,%d3				| Set error code

	move.b	%d3,%d0				| Get return code
	rts

| -------------------------------------------------------------------------
| Reset the Sub CPU
| -------------------------------------------------------------------------

ResetSubCPU:
	move.w	#0xFF00,0xA12002		| Reset the Sub CPU
	move.b	#3,0xA12001
	move.b	#2,0xA12001
	move.b	#0,0xA12001

	moveq	#0x80-1,%d2			| Wait
SubCpuResetWait:
	dbf		%d2,SubCpuResetWait
	rts

| -------------------------------------------------------------------------
| Request the Sub CPU bus
| -------------------------------------------------------------------------
| RETURNS:
|	d2.b - Return code
|	       0 - Success
|	       1 - MCD took too long to respond
| -------------------------------------------------------------------------

ReqSubCPUBus:
	move.w	#0x100-1,%d2			| Max time to wait for MCD response

ResetSub:
	bclr	#0,0xA12001			| Set the Sub CPU to be reset
	dbeq	%d2,ResetSub			| Loop until we've waited too long or until the MCD has responded
	bne.s	WaitedTooLong			| If we've waited too long, branch

	move.w	#0x100-1,%d2			| Max time to wait for MCD response

ReqSubBus:
	bset	#1,0xA12001			| Request Sub CPU bus
	dbne	%d2,ReqSubBus			| Loop until we've waited too long or until the MCD has responded
	beq.s	WaitedTooLong			| If we've waited too long, branch

	moveq	#0,%d2				| Success
	rts

WaitedTooLong:
	moveq	#1,%d2				| Waited too long
	rts

| -------------------------------------------------------------------------
| Return the Sub CPU bus
| -------------------------------------------------------------------------
| RETURNS:
|	d2.b - Return code
|	       0 - Success
|	       1 - MCD took too long to respond
| -------------------------------------------------------------------------

ReturnSubCPUBus:
	move.w	#0x100-1,%d2			| Max time to wait for MCD response

RunSub:
	bset	#0,0xA12001				| Set the Sub CPU to run again
	dbne	%d2,RunSub				| Loop until we've waited too long or until the MCD has responded
	beq.s	WaitedTooLong2			| If we've waited too long, branch

	move.w	#0x100-1,%d2			| Max time to wait for MCD response

GiveSubBus:
	bclr	#1,0xA12001			| Give back Sub CPU bus
	dbeq	%d2,GiveSubBus			| Loop until we've waited too long or until the MCD has responded
	bne.s	WaitedTooLong2			| If we've waited too long, branch

	moveq	#0,%d2				| Success
	rts

WaitedTooLong2:
	moveq	#1,%d2				| Waited too long
	rts

| -------------------------------------------------------------------------
| Copy PRG-RAM data
| -------------------------------------------------------------------------
| NOTE: Requires that Sub CPU bus access must be granted
| -------------------------------------------------------------------------
| PARAMETERS:
|	a0.l - Pointer to data to copy
|	d0.l - Size of data to copy
|	d1.l - PRG RAM offset
| RETURNS:
|	d2.b - Return code
|	       0 - Success
|	       2 - Failed to copy data
| -------------------------------------------------------------------------

CopyPRGRAMData:
	lea		0x420000,%a1			| Get destination address
	move.l	%d1,%d2
	andi.l	#0x1FFFF,%d2
	add.l	%d2,%a1
	
	move.b	0xA12003,%d2			| Set bank ID
	andi.b	#0x3F,%d2
	swap	%d1
	ror.b	#3,%d1
	andi.b	#0xC0,%d1
	or.b	%d2,%d1
	move.b	%d1,0xA12003

CopyData:
	move.b	(%a0),(%a1)			| Copy byte
	cmpm.b	(%a0)+,(%a1)+			| Did it copy correctly?
	beq.s	CopyDataLoop			| If so, branch
	moveq	#2,%d2				| Failed to copy data
	rts

CopyDataLoop:
	subq.l	#1,%d0				| Decrement size
	beq.s	End				| If there's no more data left copy, branch
	cmpa.l	#0x43FFFF,%a1			| Have we reached the end of the bank?
	bls.s	CopyData			| If not, branch

	lea		0x420000,%a1			| Go to top of bank
	move.b	0xA12003,%d1			| Increment bank ID
	addi.b	#0x40,%d1
	move.b	%d1,0xA12003
	bra.s	CopyData			| Copy more data

End:
	moveq	#0,%d2				| Success
	rts

| -------------------------------------------------------------------------
| Send a level 2 interrupt request to the Sub CPU
| -------------------------------------------------------------------------

SendMCDInt2:
	bset	#0,0xA12000			| Send interrupt request
	rts

| -------------------------------------------------------------------------
| Check if there's a known MCD BIOS available
| -------------------------------------------------------------------------
| RETURNS:
|	cc/cs - Found, not found 
|	a0.l  - Pointer to Sub CPU BIOS
| -------------------------------------------------------------------------

FindMCDBIOS:
	|cmpi.l	#0x'S''E''G''A',0x400100		| Is the "SEGA" signature present?
	cmpi.l	#0x53454741,0x400100			| Is the "SEGA" signature present?
	bne.s	NotFound						| If not, branch
	|cmpi.w	#0x'B''R',0x400180				| Is the "Boot ROM" software type present?
	cmpi.w	#0x4252,0x400180				| Is the "Boot ROM" software type present?
	bne.s	NotFound						| If not, branch

	lea		MCDBIOSList(%pc),%a2			| Get list of known BIOSes
	moveq	#(MCDBIOSListEnd-MCDBIOSList)/2-1,%d0

FindLoop:
	lea		MCDBIOSList(%pc),%a1		| Get pointer to BIOS data
	adda.w	(%a2)+,%a1

	movea.l	(%a1)+,%a0				| Get Sub CPU BIOS address
	lea		0x400120,%a3			| Get BIOS name

CheckName:
	move.b	(%a1)+,%d1			| Get character
	beq.s	NameMatch			| If we are done checking, branch
	cmp.b	(%a3)+,%d1			| Does the BIOS name match so far?
	bne.s	NextBIOS			| If not, go check the next BIOS
	bra.s	CheckName			| Loop until name is fully checked

NameMatch:
	move.b	(%a1)+,%d1			| Is this Sub CPU BIOS address region specific?
	beq.s	Found				| If not, branch
	cmp.b	0x4001F0,%d1		| Does the BIOS region match?
	bne.s	NextBIOS			| If not, branch

Found:
	andi	#0xFE,%ccr			| BIOS found
	rts

NextBIOS:
	dbf		%d0,FindLoop		| Loop until all BIOSes are checked

NotFound:
	ori		#1,%ccr				| BIOS not found
	rts

| -------------------------------------------------------------------------
| MCD BIOSes to find
| -------------------------------------------------------------------------

MCDBIOSList:
	dc.w	MCDBIOS_JP1-MCDBIOSList
	dc.w	MCDBIOS_US1-MCDBIOSList
	dc.w	MCDBIOS_EU1-MCDBIOSList
	dc.w	MCDBIOS_CD2-MCDBIOSList
	dc.w	MCDBIOS_CDX-MCDBIOSList
	dc.w	MCDBIOS_LaserActive-MCDBIOSList
	dc.w	MCDBIOS_Wondermega1-MCDBIOSList
	dc.w	MCDBIOS_Wondermega2-MCDBIOSList
MCDBIOSListEnd:

MCDBIOS_JP1:
	dc.l	0x416000
	.asciz 	"MEGA-CD BOOT ROM"
	.ascii	"J"
	.even

MCDBIOS_US1:
	dc.l	0x415800
	.asciz	"SEGA-CD BOOT ROM"
	dc.b	0
	.even

MCDBIOS_EU1:
	dc.l	0x415800
	.asciz	"MEGA-CD BOOT ROM"
	.ascii	"E"
	.even

MCDBIOS_CD2:
	dc.l	0x416000
	.asciz	"CD2 BOOT ROM    "
	dc.b	0
	.even

MCDBIOS_CDX:
	dc.l	0x416000
	.asciz	"CDX BOOT ROM    "
	dc.b	0
	.even

MCDBIOS_LaserActive:
	dc.l	0x41AD00
	.asciz	"MEGA-LD BOOT ROM"
	dc.b	0
	.even

MCDBIOS_Wondermega1:
	dc.l	0x416000
	.asciz	"WONDER-MEGA BOOTROM"
	dc.b	0
	.even

MCDBIOS_Wondermega2:
	dc.l	0x416000
	.asciz	"WONDERMEGA2 BOOTROM"
	dc.b	0
	.even

| -------------------------------------------------------------------------
