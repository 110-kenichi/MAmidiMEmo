// SYS_disableInts for Mega Drive (68K)
// SGDK compatible signature
#include <stdint.h>

// Disable all interrupts (set mask level 7)
// Returns previous SR value
uint16_t SYS_disableInts(void) {
    uint16_t old_sr, new_sr;
    // Read current SR
    __asm__ volatile ("move %/sr,%0" : "=d"(old_sr));
    // Set new SR with interrupt mask level 7
    new_sr = (old_sr & ~0x0700) | (7 << 8);
    __asm__ volatile ("move %0,%/sr" :: "d"(new_sr));
    return old_sr;
}
