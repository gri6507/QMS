/********************************
* COPYRIGHT Kirk and Paul little shop 2015
*********************************/

#include <stdbool.h>         // use bool for Booleans
#include <sys/alt_irq.h>     // for interrupt disable
#include <io.h>              // for serial IO
#include <ctype.h>           // for isspace()
#include "system.h"          // for QSYS definitions

// Define the standard data types which will be used in the rest of the application
typedef __INT8_TYPE__      s8;
typedef __UINT8_TYPE__     u8;
typedef __INT16_TYPE__     s16;
typedef __UINT16_TYPE__    u16;
typedef __INT32_TYPE__     s32;
typedef __UINT32_TYPE__    u32;
typedef __INT64_TYPE__     s64;
typedef __UINT64_TYPE__    u64;
typedef float              f32;

// Define a macro to calculate the baud rate settings for a UART given the
// desired rate (as a float)
#define BAUD_RATE(rate) (s32)(((f32)FIFOED_UART_FREQ / rate) + 0.5f)

// Definitions for the Fifoed Avalon Uart Registers
#define IOWR_FIFOED_AVALON_UART_DIVISOR(base, data)  IOWR(base, 4, data)
#define IORD_FIFOED_AVALON_UART_STATUS(base)         IORD(base, 2)
#define IORD_FIFOED_AVALON_UART_RXDATA(base)         IORD(base, 0)
#define IORD_FIFOED_AVALON_UART_TX_FIFO_USED(base)   IORD(base, 7)
#define IOWR_FIFOED_AVALON_UART_TXDATA(base, data)   IOWR(base, 1, data)
#define FIFOED_AVALON_UART_STATUS_TRDY_MSK           0x40
#define FIFOED_AVALON_UART_CONTROL_RRDY_MSK          0x80

// Function to Send a character over the UART
static ALT_INLINE void ALT_ALWAYS_INLINE SendChar(const u16 c, const u32 base)
{
    u32 status;

    do
    {
        // Wait (rarely) until we can write
        status = IORD_FIFOED_AVALON_UART_STATUS(base);
    }
    while (!(status & FIFOED_AVALON_UART_STATUS_TRDY_MSK));

    // Write the character to the UART
    IOWR_FIFOED_AVALON_UART_TXDATA(base, c);
}

// Function to send an entire string over the UART
static void SendStr(const char *str, const u32 base)
{
    while (*str)
        SendChar(*str++, base);
}

// Treat an ASCII character as a hex nibble and return its numeric value
static bool HexCharToInt(const char c, u8 *v)
{
    if (isdigit (c))
    {
        *v = c - '0';
        return true;
    }
    else
    {
        if (isupper(c))
        {
            if (c > 'F')
                return false;
            else
            {
                *v = c - 'A' + 10;
                return true;
            }
        }
        else
        {
            if (islower(c))
            {
                if (c > 'f')
                    return false;
                else
                {
                    *v = c - 'a' + 10;
                    return true;
                }
            }
            else
                return false;
        }
    }
}

// Function to convert a string representation of a hex number into a u32
static bool StrToU32(const char const *s, u32 *v)
{
    char *strptr = (char *)s;  // Local pointer.
    char hexBuffer[9];         // Buffer for 8 hex digits plus a NULL terminator.
    u8 i = 0;

    // Find first non-space character.
    while (isspace(*strptr))
        strptr++;

    // Ignore the optional '0x' at the front of the hex parameter
    if ((*strptr == '0') && (*(strptr +1) == 'X'))
        strptr += 2;

    // Copy all consecutive hex characters to a buffer.
    while (isxdigit(*strptr))
    {
        hexBuffer[i++] = *strptr++;
        if (i > 8)
            return false;
    }

    // There are zero hex digits, flag failure.
    if (i == 0)
        return false;

    // Null terminate hex string.
    hexBuffer[i] = '\0';

    // Calculate the hex value
    *v = 0;
    strptr = hexBuffer;
    while (*strptr)
    {
        *v <<= 4;
        
        u8 nibbleValue;
        if (false == HexCharToInt(*strptr, &nibbleValue))
            return false;
        *v += nibbleValue;
        strptr++;
    }
    return true;
}

static void U32ToStr(u32 v, char *ans)
{
    int i;
    const char nibbleValues[] = {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'};
    
    ans[8] = '\0';
    for (i=0; i<8; i++)
    {
        ans[7-i] = nibbleValues[v & 0xf];
        v >>= 4;
    }
}

#define NO_ANSWER "N\r\n"

static void ExecuteCmd(const char const *input, const u32 base)
{
    SendStr("\r\n", base);
    
    // Tokenize the command
    #define MAX_CMD_WORDS 3
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
            if (2 != numTokens)
                SendStr(NO_ANSWER, base);
            else
            {
                u32 regAddr;
                u32 regValue;
                if (StrToU32(token[1], &regAddr) && RegRead(regAddr, &regValue))
                {
                    SendStr("Y 0x", base);
                    char regValStr[9];
                    U32ToStr(regValue, regValStr);
                    SendStr(regValStr, base);
                    SendStr("\r\n", base);
                }
                else
                    SendStr(NO_ANSWER, base);
            }
            break;
            
        case 'W':
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
            
        default:
            SendStr(NO_ANSWER, base);
            break;
    }
    
    return;
}

int main(void)
{
    // Prepare for UART communication with external world. The default baud rate
    // is 921,600 bps
    IOWR_FIFOED_AVALON_UART_DIVISOR(FIFOED_UART_BASE, BAUD_RATE(921600.0f));

    // Make sure UART interrupts are disabled
    alt_ic_irq_disable(FIFOED_UART_IRQ_INTERRUPT_CONTROLLER_ID, FIFOED_UART_IRQ);

    // Clear the input and output buffers
    while (IORD_FIFOED_AVALON_UART_STATUS(FIFOED_UART_BASE) & FIFOED_AVALON_UART_CONTROL_RRDY_MSK)
        IORD_FIFOED_AVALON_UART_RXDATA(FIFOED_UART_BASE);
    while (IORD_FIFOED_AVALON_UART_TX_FIFO_USED(FIFOED_UART_BASE) > 0);
    
    #define MAX_CMD_LEN 16
    char cmd[MAX_CMD_LEN];
    s8 cmdIndex = 0;

    // Sit in an infinite loop waiting for serial commands
    while(1)
    {
        while (IORD_FIFOED_AVALON_UART_STATUS(FIFOED_UART_BASE) & FIFOED_AVALON_UART_CONTROL_RRDY_MSK)
        {
            // Read the Uart
            char rx = IORD_FIFOED_AVALON_UART_RXDATA(FIFOED_UART_BASE);
            
            // If this is the end of a command, then try to parse it
            if (('\r' == rx) || ('\n' == rx))
            {
                cmd[cmdIndex] = '\0';
                ExecuteCmd(cmd, FIFOED_UART_BASE);
                cmdIndex = 0;
            }
            
            // If this is a backspace
            else if ('\b' == rx)
            {
                SendStr("\b \b", FIFOED_UART_BASE);
                if (cmdIndex > 0)
                    cmdIndex--;
            }
            
            // This is any other character
            else
            {
                // echo the character
                SendChar(rx, FIFOED_UART_BASE);
                
                // Add it to the buffer, if possible, making sure to save the 
                // space for the null terminator (when completing the command)
                if (cmdIndex < (MAX_CMD_LEN - 1))
                    cmd[cmdIndex++] = rx;
                
                // Otherwise, report the error and reset the buffer
                else
                {
                    cmdIndex = 0;
                    SendStr(NO_ANSWER, FIFOED_UART_BASE);
                }
            }
        }
    }
}