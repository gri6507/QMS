/********************************
* COPYRIGHT Kirk and Paul little shop 2015
*********************************/


#ifndef __SERIAL_H__
#define __SERIAL_H__

#include "stdhdr.h"
#include <ctype.h>           // for isspace()
#include <io.h>              // for serial IO


// Define a macro to calculate the baud rate settings for a UART given the
// desired rate (as a float)
#define BAUD_RATE(rate) (s32)(((f32)UART_FREQ / rate) + 0.5f)

// Definitions for the Fifoed Avalon Uart Registers
#define IOWR_FIFOED_AVALON_UART_DIVISOR(base, data)  IOWR(base, 4, data)
#define IORD_FIFOED_AVALON_UART_STATUS(base)         IORD(base, 2)
#define IORD_FIFOED_AVALON_UART_RXDATA(base)         IORD(base, 0)
#define IORD_FIFOED_AVALON_UART_TX_FIFO_USED(base)   IORD(base, 7)
#define IOWR_FIFOED_AVALON_UART_TXDATA(base, data)   IOWR(base, 1, data)
#define FIFOED_AVALON_UART_STATUS_TRDY_MSK           0x40
#define FIFOED_AVALON_UART_CONTROL_RRDY_MSK          0x80

#define NO_ANSWER "N\r\n"

// Function to Send a character over the UART
void SendChar(const u16 c, const u32 base);

// Function to send an entire string over the UART
void SendStr(const char *str, const u32 base);

// Function to convert a string representation of a hex number into a u32
bool StrToU32(const char const *s, u32 *v);

// Function to convert a u32 into a string representation of the hex value
void U32ToStr(u32 v, char *ans);




#endif // __SERIAL_H__
