/********************************
* COPYRIGHT Kirk and Paul little shop 2015
*********************************/

#ifndef __STDHDR_H__
#define __STDHDR_H__

#include <alt_types.h>      // used for Altera data types
#include <stdbool.h>         // use bool for Booleans
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

#define REGISTER_BASE  CONTROL_STATUS_REGISTERS_BASE
#define REGISTER_SPAN  CONTROL_STATUS_REGISTERS_SPAN
#define UART_BASE      FIFOED_UART_BASE
#define UART_FREQ      FIFOED_UART_FREQ
#define UART_IRQ       FIFOED_UART_IRQ
#define UART_IRQ_INTERRUPT_CONTROLLER_ID  FIFOED_UART_IRQ_INTERRUPT_CONTROLLER_ID

#endif // __STDHDR_H__
