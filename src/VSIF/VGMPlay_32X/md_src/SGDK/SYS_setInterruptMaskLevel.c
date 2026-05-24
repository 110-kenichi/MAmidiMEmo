// SYS_setInterruptMaskLevel for Mega Drive (68K)
// SGDK compatible signature
#include <stdint.h>

// Set the interrupt mask level (0~7)
// Returns previous SR value
uint16_t SYS_setInterruptMaskLevel(uint16_t level) {
    uint16_t old_sr, new_sr;
    // Read current SR
    __asm__ volatile ("move %/sr,%0" : "=d"(old_sr));
    // Set new SR with desired interrupt mask (bits 8-10)
    new_sr = (old_sr & ~0x0700) | ((level & 0x7) << 8);
    __asm__ volatile ("move %0,%/sr" :: "d"(new_sr));
    return old_sr;
}
