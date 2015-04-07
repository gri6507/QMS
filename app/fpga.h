/********************************
* COPYRIGHT Kirk and Paul little shop 2015
*********************************/

#ifndef __FPGA_H__
#define __FPGA_H__


#include "stdhdr.h"

#ifdef NIOS2_MMU_PRESENT
    /* Convert KERNEL region address to IO region address */
    #define BYPASS_DCACHE_MASK   (0x1 << 29)
#else
    /* Set bit 31 of address to bypass D-cache */
    #define BYPASS_DCACHE_MASK   (0x1 << 31)
#endif

typedef struct {
    /* 000 */ u32 fpgaVersion;
    /* 004 */ u32 modeCtrl;
    /* 008 */ u32 dac1;
    /* 00C */ u32 dac2;
    /* 010 */ u32 dac3;
    /* 014 */ u32 dac4;
    /* 018 */ u32 adc1;
    /* 01C */ u32 adc2;
    /* 020 */ u32 adc3;
    /* 024 */ u32 adc4;
} FpgaRegisters;


// Write a single FPGA register
bool RegWrite(u32 addr, u32 value);

// Read a single FPGA register
bool RegRead(u32 addr, u32 *value);

#endif // __FPGA_H__
