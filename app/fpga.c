/********************************
* COPYRIGHT Kirk and Paul little shop 2015
*********************************/

#include "fpga.h"

// Read a single FPGA register
bool RegRead(u32 addr, u32 *value)
{
    if ((addr % 4) != 0)
        return false;
    if (addr >= CONTROL_STATUS_REGISTERS_SPAN)
        return false;
    
    u32 *pReg = (u32 *)((CONTROL_STATUS_REGISTERS_BASE | BYPASS_DCACHE_MASK) + addr);
    *value = *pReg;
    return true;
}

// Write a single FPGA register
bool RegWrite(u32 addr, u32 value)
{
    if ((addr % 4) != 0)
        return false;
    if (addr >= CONTROL_STATUS_REGISTERS_SPAN)
        return false;
    
    u32 *pReg = (u32 *)((CONTROL_STATUS_REGISTERS_BASE | BYPASS_DCACHE_MASK) + addr);
    *pReg = value; 
    return true;
}
