/********************************
* COPYRIGHT Kirk and Paul little shop 2015
*********************************/

#include "stdhdr.h"
#include "fpga.h"
#include "serial.h"
#include "sys/alt_flash.h"   // for flash access
#include <sys/alt_irq.h>     // for interrupt disable
#include <string.h>          // for memset

#define NIOS_VERSION 0x00000002

static void ExecuteCmd(const char const *input, const u32 base)
{
    SendStr("\r\n", base);
    
    // Tokenize the command
    #define MAX_CMD_WORDS 4
    char *token[MAX_CMD_WORDS];
    char *cmd = (char *)input;
    u8 numTokens = 0;
    while (1)
    {
        // Skip leading whitespace.
        while ((*cmd) && isspace(*cmd))
            cmd++;

        // If we get here and we are at the end of the string, then the last
        // token must have had trailing white spaces. Let's ignore them
        if (!(*cmd))
            break;

        // If we have exceeded the maximum number of allowable tokens, then
        // return as error
        if (numTokens >= MAX_CMD_WORDS)
        {
            SendStr(NO_ANSWER, base);
            return;
        }

        // Store the token.
        token[numTokens] = cmd;
        numTokens++;

        // Everything that isn't a whitespace is part of the token. Let's make
        // sure it is in UPPER CASE
        while ((*cmd) && (!isspace(*cmd)))
        {
            *cmd = toupper(*cmd);
            cmd++;
        }

        // When we get here, we are just past the current token, either because
        // it ended on a whitespace or because it is the end of the user input.
        // If the former, then let's force a null termination for that token. If
        // the latter, then we are done tokenizing.
        if (!(*cmd))
            break;
        *cmd = '\0';
        cmd++;
    }
    
    if (0 == numTokens)
    {
        SendStr(NO_ANSWER, base);
        return;
    }
    
    // Process the command
    switch (token[0][0])
    {
        case 'R':
        {
            if (2 != numTokens)
                SendStr(NO_ANSWER, base);
            else
            {
                u32 regAddr;
                u32 regValue;
                if (StrToU32(token[1], &regAddr) && RegRead(regAddr, &regValue))
                {
                    SendStr("Y ", base);
                    char regValStr[9];
                    U32ToStr(regValue, regValStr);
                    SendStr(regValStr, base);
                    SendStr("\r\n", base);
                }
                else
                    SendStr(NO_ANSWER, base);
            }
            break;
        }
            
        case 'W':
        {
            if (3 != numTokens)
                SendStr(NO_ANSWER, base);
            else
            {
                u32 regAddr;
                u32 regValue;
                if (StrToU32(token[1], &regAddr) && StrToU32(token[2], &regValue) && RegWrite(regAddr, regValue))
                    SendStr(YES_ANSWER, base);
                else
                    SendStr(NO_ANSWER, base);
            }
            break;
        }
        
        case 'V':
        {
            SendStr("FPGA=0x", base);
            char versionStr[9];
            FpgaRegisters * FPGARegs = (FpgaRegisters *)(REGISTER_BASE | BYPASS_DCACHE_MASK);
            U32ToStr(FPGARegs->fpgaVersion, versionStr);
            SendStr(versionStr, base);
            SendStr(" NIOS=0x", base);
            U32ToStr(NIOS_VERSION, versionStr);
            SendStr(versionStr, base);
            SendStr("\r\n", base);
            break;
        }

        case 'F':
        {
            if (4 != numTokens)
                SendStr(NO_ANSWER, base);
            else
            {
                u32 startAddr;
                u32 length;
                u32 checksum;
                StrToU32(token[1], &startAddr);
                StrToU32(token[2], &length);
                StrToU32(token[3], &checksum);

                // Transfer two chunks to get a full sector worth
				#define FLASH_SECTOR_SIZE (64*1024)
				#define TRANSFER_SIZE     (4*1024)
                u8  buffer[FLASH_SECTOR_SIZE];

                // Validate the requested transfer size
                if (length != TRANSFER_SIZE)
                    SendStr(NO_ANSWER, base);
                else
                {
                    u32 bufferIndex = startAddr % FLASH_SECTOR_SIZE;
                    u32 runningSum = 0;
                    u32 numBytesReceived = 0;

                	// Clear the input buffer
                	FlushRx(base);

                    // Acknowledge that the command is good. This will tell the
                    // sender to actually send the specified number of bytes
                    SendStr(YES_ANSWER, base);

                    // We must receive the correct number of bytes
                    while (true)
                    {
                    	while (IORD_FIFOED_AVALON_UART_STATUS(base) & FIFOED_AVALON_UART_CONTROL_RRDY_MSK)
                    	{
							// Read the Uart
							u8 rx = IORD_FIFOED_AVALON_UART_RXDATA(base);
							runningSum += rx;
							buffer[bufferIndex++] = rx;
							numBytesReceived++;
	                        if (numBytesReceived >= length)
	                            break;
                    	}
                        if (numBytesReceived >= length)
                            break;
                    }

                    // check the checksum
                    if (runningSum != checksum)
                        SendStr(NO_ANSWER, base);
                    else
                    {
                    	// If we don't have a full sector worth of data, then ACK and wait for more
                    	if (bufferIndex != FLASH_SECTOR_SIZE)
                    		SendStr(YES_ANSWER, base);
                    	else
                    	{
                    		u32 totalWriteBufferChecksum = 0;
                    		int i;
                    		for (i=0; i<sizeof(buffer); i++)
                    		{
                    			totalWriteBufferChecksum += buffer[i];
                    		}

							alt_flash_fd* fd = alt_flash_open_dev(SERIAL_FLASH_NAME);
							if (NULL == fd)
								SendStr(NO_ANSWER, base);
							else
							{
								u32 sectorStartAddr = (startAddr / FLASH_SECTOR_SIZE) * FLASH_SECTOR_SIZE;
								if (0 == alt_write_flash(fd, sectorStartAddr, buffer, length))
								{
									memset(buffer, 0x99, sizeof(buffer));
									if (0 == alt_read_flash(fd, sectorStartAddr, buffer, sizeof(buffer)))
									{
			                    		u32 totalReadBufferChecksum = 0;
			                    		for (i=0; i<sizeof(buffer); i++)
			                    		{
			                    			totalReadBufferChecksum += buffer[i];
			                    		}
			                    		if (totalReadBufferChecksum == totalWriteBufferChecksum)
											SendStr(YES_ANSWER, base);
			                    		else
											SendStr(NO_ANSWER, base);
									}
		                    		else
										SendStr(NO_ANSWER, base);
								}
								else
									SendStr(NO_ANSWER, base);

								alt_flash_close_dev(fd);
							}
                    	}
                    }
                }
            }

            break;
        }
            
        default:
            SendStr(NO_ANSWER, base);
            break;
    }
    
    return;
}


///////////////////////////////////////////////////
//                MAIN APPLICATION               //
///////////////////////////////////////////////////

int main(void)
{
    // Prepare for UART communication with external world. The default baud rate
    // is 921,600 bps
    IOWR_FIFOED_AVALON_UART_DIVISOR(UART_BASE, BAUD_RATE(921600.0f));

    // Make sure UART interrupts are disabled
    alt_ic_irq_disable(UART_IRQ_INTERRUPT_CONTROLLER_ID, UART_IRQ);

    // Clear the input and output buffers
    FlushRx(UART_BASE);
    FlushTx(UART_BASE);
    
    #define MAX_CMD_LEN 64
    char cmd[MAX_CMD_LEN];
    s8 cmdIndex = 0;

    // Sit in an infinite loop waiting for serial commands
    while(1)
    {
        while (IORD_FIFOED_AVALON_UART_STATUS(UART_BASE) & FIFOED_AVALON_UART_CONTROL_RRDY_MSK)
        {
            // Read the Uart
            char rx = IORD_FIFOED_AVALON_UART_RXDATA(UART_BASE);
            
            // If this is the end of a command, then try to parse it
            if (('\r' == rx) || ('\n' == rx))
            {
                cmd[cmdIndex] = '\0';
                ExecuteCmd(cmd, UART_BASE);
                FlushRx(UART_BASE);
                FlushTx(UART_BASE);
                cmdIndex = 0;
            }
            
            // If this is a backspace
            else if ('\b' == rx)
            {
                SendStr("\b \b", UART_BASE);
                if (cmdIndex > 0)
                    cmdIndex--;
            }
            
            // This is any other character
            else
            {
                // echo the character
                SendChar(rx, UART_BASE);
                
                // Add it to the buffer, if possible, making sure to save the 
                // space for the null terminator (when completing the command)
                if (cmdIndex < (MAX_CMD_LEN - 1))
                    cmd[cmdIndex++] = rx;
                
                // Otherwise, report the error and reset the buffer
                else
                {
                    cmdIndex = 0;
                    SendStr(NO_ANSWER, UART_BASE);
                }
            }
        }
    }
}
