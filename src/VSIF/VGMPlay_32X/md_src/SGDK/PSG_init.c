// PSG_init for Mega Drive (68K)
// ダミー実装例（実際の初期化内容は要調整）
#include <stdint.h>

#define PSG_PORT 0xC00011

void PSG_init(void) {
    volatile uint8_t* psg = (uint8_t*)PSG_PORT;
    // 全チャンネル音量最大（ミュート）に設定（簡易例）
    for (uint8_t ch = 0; ch < 4; ++ch) {
        *psg = 0x9F | (ch << 5); // ボリューム15 (mute)
    }
}
