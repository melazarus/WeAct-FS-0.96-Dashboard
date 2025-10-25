//https://github.com/WeActStudio/WeActStudio.SystemMonitor/blob/main/library/lcd/lcd_comm_weact_b.py

namespace WeActLCD.Driver
{
    public enum CommandCode : byte
    {
        CMD_WHO_AM_I = 0x81,
        CMD_SET_ORIENTATION = 0x02,
        CMD_SET_BRIGHTNESS = 0x03,
        CMD_FULL = 0x04,
        CMD_SET_BITMAP = 0x05,
        CMD_SET_BITMAP_WITH_FASTLZ = 0x15,
        CMD_FREE = 0x07,
        CMD_SYSTEM_VERSION = 0x42,
        CMD_END = 0x0A,
        CMD_READ = 0x80,
    }
}

