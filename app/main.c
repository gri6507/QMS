/********************************
* COPYRIGHT Kirk and Paul little shop 2015
*********************************/

#include "stdhdr.h"
#include "fpga.h"
#include "serial.h"
#include "epcs_driver.h"
#include <sys/alt_irq.h>     // for interrupt disable

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
                    SendStr("Y\r\n", base);
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

                // Define the maximum transfer size as the subsector size for
                // the EPCS part. This will get the most efficiency
                #define MAX_TRANSFER_SIZE (SERIAL_FLASH_WORDS_PER_SUBSECTOR)
                u8  buffer[MAX_TRANSFER_SIZE];
                u32 bufferIndex = 0;
                u32 runningSum = 0;

                // Validate the requested transfer size against our buffer size
                if (length > MAX_TRANSFER_SIZE)
                    SendStr(NO_ANSWER, base);
                else
                {
                	// Clear the input buffer
                    while (IORD_FIFOED_AVALON_UART_STATUS(base) & FIFOED_AVALON_UART_CONTROL_RRDY_MSK)
                        IORD_FIFOED_AVALON_UART_RXDATA(base);

                    // Acknowledge that the command is good. This will tell the
                    // sender to actually send the specified number of bytes
                    SendStr("Y\r\n", base);

                    // TODO - it would be nice to add a timeout over here
                    while (true)
                    {
                    	while (IORD_FIFOED_AVALON_UART_STATUS(UART_BASE) & FIFOED_AVALON_UART_CONTROL_RRDY_MSK)
                    	{
							// Read the Uart
							u8 rx = IORD_FIFOED_AVALON_UART_RXDATA(UART_BASE);
							runningSum += rx;
							buffer[bufferIndex++] = rx;
                    	}
                        if (bufferIndex >= length)
                            break;
                    }

                    // check the checksum
                    if (runningSum != checksum)
                        SendStr(NO_ANSWER, base);
                    else
                    {
                        alt_flash_fd* fd = EPCS_OpenFlash(SERIAL_FLASH_NAME);
                        if (0 == EPCS_WriteFlash(fd, startAddr, (u32)buffer, length))
                            SendStr("Y\r\n", base);
                        else
                            SendStr(NO_ANSWER, base);
                    }
                }
            }
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
    while (IORD_FIFOED_AVALON_UART_STATUS(UART_BASE) & FIFOED_AVALON_UART_CONTROL_RRDY_MSK)
        IORD_FIFOED_AVALON_UART_RXDATA(UART_BASE);
    while (IORD_FIFOED_AVALON_UART_TX_FIFO_USED(UART_BASE) > 0);
    
    #define MAX_CMD_LEN 16
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
