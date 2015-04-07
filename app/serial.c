/********************************
* COPYRIGHT Kirk and Paul little shop 2015
*********************************/

#include "serial.h"

// Function to Send a character over the UART
void SendChar(const u16 c, const u32 base)
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
void SendStr(const char *str, const u32 base)
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
bool StrToU32(const char const *s, u32 *v)
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

void U32ToStr(u32 v, char *ans)
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

