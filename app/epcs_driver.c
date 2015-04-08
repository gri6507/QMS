/********************************
* COPYRIGHT Kirk and Paul little shop 2015
*********************************/

#include "epcs_driver.h"
#include <string.h>  // for memcpy and memset

// Pull in the Altera HAL headers for EPCS
#include "altera_avalon_epcs_flash_controller.h"
#include "epcs_commands.h"
#include "altera_avalon_spi.h"

// An invalid sector address is one that is outside of memory range and/or
// not sector aligned
#define INVALID_SECTOR_ADDR 0xFFFFFFFF

// Variables that keep track of the current sector data
static flashDatum sectorBuffer[SERIAL_FLASH_WORDS_PER_SECTOR];
static u32 sectorBufferAddrInFlash = INVALID_SECTOR_ADDR;

// Enable this to get a profiler of the EPCS flash driver.
#define EPCS_DRIVER_PROFILER 0

/******************************************************************************
 * EPCS_GetMergeChunkLength
 ******************************************************************************/
/**
 * \par Description:
 * This function determines how much of the current sector needs to be
 * processed.
 * \par Notes:
 * \a None
 * \par Controlling Requirements:
 * \a None
 * \param dataStartAddr The address in flash of the complete buffer in question
 * \param dataLen The size of the buffer in question
 * \param sectorAddr The address of the current sector being processed
 * \param chunkStartOffset The offset into the buffer being processed
 * \param chunkLen The size of the chunk we can process for the current sector
 * \return \a Zero on success, non-zero otherwise
 * \par Worst Case Timing:
 * \a N/A
 * \par Calling Sequence:
 * \code
 * \endcode
 */
static void EPCS_GetMergeChunkLength(
	const u32 dataStartAddr,
	const u32 dataLength,
	const u32 sectorAddr,
	const u32 chunkStartOffset,
	u32 * const chunkLen)
{
	/* Figure out the chunk of data that we can process for this sector.
	 * The chunk could be one of the following scenarios:
	 *
	 * Scenario 1:
	 *
	 *            +-------------------------------+
	 *            |      Data To Read/Write       |             <-- RAM resident buffer with new Data
	 *            +-------------------------------+
	 *  +-----------------+-----------------+-----------------+
	 *  |     sector N    |    sector N+1   |    sector N+2   |  <-- flash memory sectors
	 *  +-----------------+-----------------+-----------------+
	 *            ^^^^^^^^
	 *            | chunk|                                       <-- chunk of data we can process
	 *            +------+
	 *
	 * Scenario 2:
	 *
	 *            +-------------------------------+
	 *            |      Data To Read/Write       |             <-- RAM resident buffer with new Data
	 *            +-------------------------------+
	 *  +-----------------+-----------------+-----------------+
	 *  |     sector N    |    sector N+1   |    sector N+2   |  <-- flash memory sectors
	 *  +-----------------+-----------------+-----------------+
	 *                    ^^^^^^^^^^^^^^^^^^
	 *                    |      chunk     |                     <-- chunk of data we can process
	 *                    +----------------+
	 *
	 * Scenario 3
	 *
	 *            +-------------------------------+
	 *            |      Data To Read/Write       |             <-- RAM resident buffer with new Data
	 *            +-------------------------------+
	 *  +-----------------+-----------------+-----------------+
	 *  |     sector N    |    sector N+1   |    sector N+2   |  <-- flash memory sectors
	 *  +-----------------+-----------------+-----------------+
	 *                                      ^^^^^^
	 *                                      |chnk|               <-- chunk of data we can process
	 *                                      +----+
	 *
	 * Scenario 4
	 *
	 *            +----+
	 *            |Data|        <-- RAM resident buffer with new Data
	 *            +----+
	 *  +-----------------+
	 *  |     sector N    |     <-- flash memory sector
	 *  +-----------------+
	 *            ^^^^^^
	 *            |chnk|        <-- chunk of data we can process
	 *            +----+
	 *
	 */

	// If the data goes past the end of the current sector ...
	if ( (dataStartAddr + dataLength) > (sectorAddr + SERIAL_FLASH_WORDS_PER_SECTOR*sizeof(flashDatum)) )
	{
		// ... then the chunk we are about to write out is the delta between
		// the end of this sector and the current offset into the data.
		*chunkLen = (sectorAddr + SERIAL_FLASH_WORDS_PER_SECTOR*sizeof(flashDatum)) -
				(dataStartAddr + chunkStartOffset);
	}
	else
	{
		// ... otherwise, the chunk we are about to write out is the delta
		// between the end of the data and the current offset into the data.
		// Observe that the two "dataStartAddr" cancel each other out.
		*chunkLen = (/* dataStartAddr + */ dataLength) - (/* dataStartAddr + */ chunkStartOffset);
	}
}

/******************************************************************************
 * EPCS_ReadSector
 ******************************************************************************/
/**
 * \par Description:
 * This function reads a sector worth of flash data into a local buffer
 * \par Notes:
 * \a If the local buffer already has the desired sector, then do not read it
 * again.
 * \par Controlling Requirements:
 * \a None
 * \param flash_info The FD from Altera HAL for this serial flash device
 * \param sectorAddr the Absolute address in flash that is the start of the
 * sector in question.
 * \return \a Zero on success, non-zero otherwise
 * \par Worst Case Timing:
 * \a N/A
 * \par Calling Sequence:
 * \code
 * \endcode
 */
static int EPCS_ReadSector(alt_flash_dev* flash_info, const u32 sectorAddr)
{
	// Assume that the requested sector is the same that is currently in the
	// local buffer. Set the return value to indicate success
	int result = 0;

	// Only if our local sector buffer is not what is being requested, go
	// read the flash.
	if (sectorAddr != sectorBufferAddrInFlash)
	{
		// Read a sector into a local buffer
		result = alt_read_flash(flash_info, sectorAddr, sectorBuffer, sizeof(sectorBuffer));

		// If the result was good, then update the notion of the current sector
		// in our local buffer to the address of the sector we just read.
		if (0 == result)
		{
			sectorBufferAddrInFlash = sectorAddr;
		}

		// Otherwise, invalidate the local buffer by saying that the address of the
		// sector in our local buffer is some garbage
		else
		{
			sectorBufferAddrInFlash = INVALID_SECTOR_ADDR;
		}
	}

	// Return the result
	return result;
}

/******************************************************************************
 * EPCS_EraseSector
 ******************************************************************************/
/**
 * \par Description:
 * This function erases a sector worth of flash data, and if needed, invalidates
 * the local buffer.
 * \par Notes:
 * \a The local buffer is only invalidated if we are erasing the same sector that
 * was duplicated in the local buffer.
 * \par Controlling Requirements:
 * \a None
 * \param flash_info The FD from Altera HAL for this serial flash device
 * \param sectorAddr the Absolute address in flash that is the start of the
 * sector in question.
 * \return \a Zero on success, non-zero otherwise
 * \par Worst Case Timing:
 * \a N/A
 * \par Calling Sequence:
 * \code
 * \endcode
 */
static int EPCS_EraseSector(alt_flash_dev* flash_info, const u32 sectorAddr)
{
    // If we are erasing a sector that is in our local buffer, then invalidate
    // our local buffer.
    if (sectorAddr == sectorBufferAddrInFlash)
	{
        sectorBufferAddrInFlash = INVALID_SECTOR_ADDR;
    }
    
    // TODO - optimize me to not do an address validation. That's just unneeded.
    return alt_epcs_flash_erase_block(flash_info, sectorAddr);
}

/******************************************************************************
 * EPCS_DoReadFlash
 ******************************************************************************/
/**
 * \par Description:
 * This function reads flash one sector at a time, relinquishing the calling
 * task each time, and stores the result in a RAM buffer
 * \par Notes:
 * \a None
 * \par Controlling Requirements:
 * \a None
 * \param flash_info The FD from Altera HAL for this serial flash device
 * \param srcAddr The starting address within flash. This address is absolute
 * to the start of flash.
 * \param destAddr The pointer to the RAM based data buffer to store the data
 * read from flash.
 * \param length The number of bytes to read from flash.
 * \return \a Zero on success, non-zero otherwise
 * \par Worst Case Timing:
 * \a N/A
 * \par Calling Sequence:
 * \code
 * \endcode
 */
static int EPCS_DoReadFlash(alt_flash_dev* flash_info, const u32 srcAddr, const u32 destAddr, const u32 length)
{
	int result;
	u32 sectorAddr;
	u32 sectorMergeAddr;
    u32 chunkStartOffset = 0;
    u32 chunkLen;

	// Iterate through every sector that contains the data that is being
	// requested to be read.
	do
	{
		// Read the entire sector into a local buffer
		sectorAddr = GetSectorAddr(srcAddr + chunkStartOffset);
        result = EPCS_ReadSector(flash_info, sectorAddr);
        if (0 != result)
            break;

        // Merge in the chunk
        sectorMergeAddr = srcAddr + chunkStartOffset - sectorAddr;
        EPCS_GetMergeChunkLength(srcAddr, length, sectorAddr, chunkStartOffset, &chunkLen);
        memcpy((void *)(destAddr + chunkStartOffset), &sectorBuffer[sectorMergeAddr], chunkLen);
        chunkStartOffset += chunkLen;

        // Advance to the next sector
		sectorAddr += SERIAL_FLASH_WORDS_PER_SECTOR*sizeof(flashDatum);
    } while (sectorAddr < (srcAddr + length));

	return result;
}

/******************************************************************************
 * EPCS_DoWriteFlash
 ******************************************************************************/
/**
 * \par Description:
 * This function writes to flash, making sure to not modify flash contents
 * outside of what is being requested to be written.
 * \par Notes:
 * \a This function can also be used to erase flash
 * \par Controlling Requirements:
 * \a None
 * \param flash_info The FD from Altera HAL for this serial flash device
 * \param destAddr The starting address within flash. This address is absolute
 * to the start of flash.
 * \param srcAddr The pointer to the data buffer to write to flash. If this is
 * NULL, then erase flash
 * \param length The number of bytes to write into flash.
 * \return \a Zero on success, non-zero otherwise
 * \par Worst Case Timing:
 * \a N/A
 * \par Calling Sequence:
 * \code
 * \endcode
 */
static int EPCS_DoWriteFlash(alt_flash_dev* flash_info, const u32 destAddr, const u32 srcAddr, const u32 length)
{
    u32 sectorAddr;
    u32 result;
    u16 sectorMergeAddr;
    u32 chunkStartOffset = 0;
	u32 chunkLen;
    void *memPtr;
	bool mustEraseAndWrite;

#if EPCS_DRIVER_PROFILER
#define NUM_PROFILER_ENTRIES 16
	enum profilerTimes
	{
		startTime = 0,
		afterRead,
		afterMerge,
		afterLogic,
		afterEraseSector,
		afterWrite,
		numProfilerTimes
	};
	u32 epcsDriverProfiler[NUM_PROFILER_ENTRIES][numProfilerTimes];
	u8 epcsProfilerIndex;

    memset(epcsDriverProfiler, 0, sizeof(epcsDriverProfiler));
    epcsProfilerIndex = 0;
#endif // EPCS_DRIVER_PROFILER

    // The writes happen in pages, and partial page writes are allowed.
    // However, duplicate writes to the same address within a page are not
    // allowed. Similarly, writing to a page that has not been erased in not
    // allowed. Therefore, this function has to make sure that the sector(s)
    // containing the page(s) to be written have been erased.

    // Iterate through every affected sector
    do 
    {
#if EPCS_DRIVER_PROFILER
		epcsDriverProfiler[epcsProfilerIndex][startTime] = OSTimeGet();
#endif // EPCS_DRIVER_PROFILER

        // Get the base address of the sector we are working with next
        sectorAddr = GetSectorAddr(destAddr + chunkStartOffset);
        
        // Figure out the chunk length that we want to overwrite in this sector
        EPCS_GetMergeChunkLength(destAddr, length, sectorAddr, chunkStartOffset, &chunkLen);

        // Read the current sector from flash.
        result = EPCS_ReadSector(flash_info, sectorAddr);
        if (0 != result)
            break;

#if EPCS_DRIVER_PROFILER
        epcsDriverProfiler[epcsProfilerIndex][afterRead] = OSTimeGet();
#endif // EPCS_DRIVER_PROFILER

        // Assume that we will not have to actually erase/write the sector. This
        // can happen when the same data is being written to flash as before.
        mustEraseAndWrite = false;

        // If we want to overwrite only a part of the sector, then we need
        // to merge in the new data.
        if (SERIAL_FLASH_WORDS_PER_SECTOR*sizeof(flashDatum) != chunkLen)
        {
            // Merge in the chunk of new data into this sector
            sectorMergeAddr = destAddr + chunkStartOffset - sectorAddr;
            if (0 == srcAddr)
            {
                memset(&sectorBuffer[sectorMergeAddr], EPCS_FLASH_ERASE_VALUE, chunkLen);

                // Even though this chunk of the sector may have already been
                // erased in flash, since this code should be called very seldom
                // it is not a huge penalty to pay to force another erasure of
                // this sector.
                mustEraseAndWrite = true;
            }
            else
            {
            	// Test to see if the flash contents is already the same as the
            	// new chunk data. Only if it's different, indicate that we have
            	// to do a full sector erase/write operation
            	if (0 != memcmp(&sectorBuffer[sectorMergeAddr], (void *)(srcAddr + chunkStartOffset), chunkLen))
            	{
            		memcpy(&sectorBuffer[sectorMergeAddr], (void *)(srcAddr + chunkStartOffset), chunkLen);
                    mustEraseAndWrite = true;
            	}
            }

#if EPCS_DRIVER_PROFILER
            epcsDriverProfiler[epcsProfilerIndex][afterMerge] = OSTimeGet();
#endif // EPCS_DRIVER_PROFILER

            // Set the source data pointer to be from the merged sector buffer
            memPtr = sectorBuffer;
        }
        else
        {
            if (0 == srcAddr)
            {
                // If we are erasing the flash, then create the erased sector
                // worth of data and keep the source data pointer to be the local
                // sector buffer.
                memset(sectorBuffer, EPCS_FLASH_ERASE_VALUE, chunkLen);
                memPtr = sectorBuffer;

                // Even though this chunk of the sector may have already been
                // erased in flash, since this code should be called very seldom
                // it is not a huge penalty to pay to force another erasure of
                // this sector.
                mustEraseAndWrite = true;
            }
            else
            {
            	// Test to see if the flash contents is already the same as the
            	// new sector data. Only if it's different, indicate that we have
            	// to do a full sector erase/write operation
            	if (0 != memcmp(sectorBuffer, (void *)(srcAddr + chunkStartOffset), chunkLen))
            	{
					// No merging is necessary. We can write out an entire sector
					// worth of data, using the original source buffer
					memPtr = (void *)(srcAddr + chunkStartOffset);
	                mustEraseAndWrite = true;
            	}
            }
        }

        if (true == mustEraseAndWrite)
		{
#if EPCS_DRIVER_PROFILER
        	epcsDriverProfiler[epcsProfilerIndex][afterLogic] = OSTimeGet();
#endif // EPCS_DRIVER_PROFILER

			// Erase the sector so that it can be written again.
			EPCS_EraseSector(flash_info, sectorAddr);

#if EPCS_DRIVER_PROFILER
        	epcsDriverProfiler[epcsProfilerIndex][afterEraseSector] = OSTimeGet();
#endif // EPCS_DRIVER_PROFILER

			// Write the new sector to flash
			result = (*flash_info->write_block)(flash_info, 0, sectorAddr, memPtr, SERIAL_FLASH_WORDS_PER_SECTOR*sizeof(flashDatum));
			if (0 != result)
				break;

#if EPCS_DRIVER_PROFILER
        	epcsDriverProfiler[epcsProfilerIndex][afterWrite] = OSTimeGet();
#endif // EPCS_DRIVER_PROFILER
		}

        // Advance to the next sector
        sectorAddr += SERIAL_FLASH_WORDS_PER_SECTOR*sizeof(flashDatum);
        chunkStartOffset += chunkLen;

#if EPCS_DRIVER_PROFILER
		epcsProfilerIndex++;
		if (epcsProfilerIndex >= NUM_PROFILER_ENTRIES )
		{
			epcsProfilerIndex = 0;
		}
#endif // EPCS_DRIVER_PROFILER

    } while (sectorAddr < (destAddr + length));

    return result;
}

/******************************************************************************
 * EPCS_EraseFlash
 ******************************************************************************/
/**
 * \par Description:
 * This function erases flash, making sure to not modify flash contents
 * outside of what is being requested to be erased.
 * \par Notes:
 * \a None
 * \par Controlling Requirements:
 * \a None
 * \param flash_info The FD from Altera HAL for this serial flash device
 * \param startAddr The starting address within flash. This address is absolute
 * to the start of flash.
 * \param numBytes The number of bytes to erase in flash.
 * \return \a Zero on success, non-zero otherwise
 * \par Worst Case Timing:
 * \a N/A
 * \par Calling Sequence:
 * \code
 * \endcode
 */
int EPCS_EraseFlash(alt_flash_dev* flash_info, const u32 startAddr, const u32 numBytes)
{
	return EPCS_DoWriteFlash(flash_info, startAddr, 0, numBytes);
}

/******************************************************************************
 * EPCS_WriteFlash
 ******************************************************************************/
/**
 * \par Description:
 * This function writes flash, making sure to not modify flash contents
 * outside of what is being requested to be written.
 * \par Notes:
 * \a None
 * \par Controlling Requirements:
 * \a None
 * \param flash_info The FD from Altera HAL for this serial flash device
 * \param destAddr The starting address within flash. This address is absolute
 * to the start of flash.
 * \param srcAddr The pointer to the data buffer to write to flash. If this is
 * NULL, then erase flash
 * \param length The number of bytes to write into flash.
 * \return \a Zero on success, non-zero otherwise
 * \par Worst Case Timing:
 * \a N/A
 * \par Calling Sequence:
 * \code
 * \endcode
 */
int EPCS_WriteFlash(alt_flash_dev* flash_info, const u32 destAddr, const u32 srcAddr, const u32 length)
{
	return EPCS_DoWriteFlash(flash_info, destAddr, srcAddr, length);
	// return alt_write_flash(flash_info, destAddr, srcAddr, length);
}

/******************************************************************************
 * EPCS_ReadFlash
 ******************************************************************************/
/**
 * \par Description:
 * This function reads flash one sector at a time, relinquishing the calling
 * task each time, and stores the result in a RAM buffer
 * \par Notes:
 * \a None
 * \par Controlling Requirements:
 * \a None
 * \param flash_info The FD from Altera HAL for this serial flash device
 * \param srcAddr The starting address within flash. This address is absolute
 * to the start of flash.
 * \param destAddr The pointer to the RAM based data buffer to store the data
 * read from flash.
 * \param length The number of bytes to read from flash.
 * \return \a Zero on success, non-zero otherwise
 * \par Worst Case Timing:
 * \a N/A
 * \par Calling Sequence:
 * \code
 * \endcode
 */
int EPCS_ReadFlash(alt_flash_dev* flash_info, const u32 srcAddr, const u32 destAddr, const u32 length)
{
	return EPCS_DoReadFlash(flash_info, srcAddr, destAddr, length);
}
