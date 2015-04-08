#ifndef __EPCS__DRIVER_H__
#define __EPCS__DRIVER_H__

#include "stdhdr.h"
#include "sys/alt_flash.h"

// EPCQ32SI8N
#define SERIAL_FLASH_NUM_SECTORS         64
#define SERIAL_FLASH_NUM_SUBSECTORS      1024
#define SERIAL_FLASH_NUM_PAGES           16384
#define SERIAL_FLASH_WORDS_PER_PAGE      256
#define SERIAL_FLASH_WORDS_PER_SUBSECTOR 4 * 1024
#define SERIAL_FLASH_WORDS_PER_SECTOR    64 * 1024

// Flash memory organization description
typedef u8 flashDatum;

// A macro to convert bytes into number of Sectors of the serial flash.
// N.B. this macro only works for the main MIR serial flash. Do not use it for
// the VLCM serial flash.
#define ConvertBytesToSectors(bytes) ((u16)((bytes)/(SERIAL_FLASH_WORDS_PER_SECTOR*sizeof(flashDatum)))+1)

// A macro to convert a byte address into a sector or subsector base address
#define GetSubsectorAddr(addr) (((u32)((addr)/(SERIAL_FLASH_WORDS_PER_SUBSECTOR*sizeof(flashDatum))))*SERIAL_FLASH_WORDS_PER_SUBSECTOR*sizeof(flashDatum))
#define GetSectorAddr(addr) (((u32)((addr)/(SERIAL_FLASH_WORDS_PER_SECTOR*sizeof(flashDatum))))*SERIAL_FLASH_WORDS_PER_SECTOR*sizeof(flashDatum))


// Erasing flash is equivalent to writing this value
#define EPCS_FLASH_ERASE_VALUE           0xFF

// The command to erase a subsector
#define FLASH_CMD_SUBSECTOR_ERASE        0x20

/* This is a custom driver that is adapted from Altera HAL to write to serial
 * flash based on subsectors, rather than sectors as is done in the HAL. This
 * driver still uses most of the HAL. However, to have a common API naming
 * scheme, even functions that are not modified have been renamed below.
 */

#define EPCS_OpenFlash(name) alt_flash_open_dev(name)
#define EPCS_CloseFlash(fd) alt_flash_close_dev(fd)

int EPCS_WriteFlash(alt_flash_dev* flash_info, const u32 destAddr, const u32 srcAddr, const u32 length);
int EPCS_ReadFlash(alt_flash_dev* flash_info, const u32 srcAddr, const u32 destAddr, const u32 length);
int EPCS_EraseFlash(alt_flash_dev* flash_info, const u32 startAddr, const u32 numBytes);

#endif //  __EPCS__DRIVER_H__

