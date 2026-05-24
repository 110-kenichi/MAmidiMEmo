// Z80_upload for Mega Drive (68K)
// Z80 RAMにデータを転送する関数（SGDK風シグネチャ）
#include <stdint.h>

#define Z80_RAM 0xA00000
#define Z80_BUS_REQ 0xA11100
#define Z80_RESET   0xA11200

void Z80_upload(const void* src, uint32_t dst, uint32_t size) {
    volatile uint8_t* z80_ram = (uint8_t*)Z80_RAM;
    volatile uint8_t* busreq = (uint8_t*)Z80_BUS_REQ;
    volatile uint8_t* reset  = (uint8_t*)Z80_RESET;
    const uint8_t* s = (const uint8_t*)src;
    uint32_t i;

    // Z80バスを取得
    *busreq = 0x00;
    // 32X環境ではBUSREQ待ちループを省略
    //while ((*busreq & 0x01) != 0) ;
    // Z80リセット
    *reset = 0x00;
    // 転送
    for (i = 0; i < size; ++i) {
        z80_ram[(dst + i) & 0x1FFF] = s[i];
    }
    // Z80リセット解除
    *reset = 0x01;
    // バスリリース
    *busreq = 0x01;
}
