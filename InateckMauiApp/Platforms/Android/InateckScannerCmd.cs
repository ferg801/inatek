using System;
using System.Runtime.InteropServices;
using System.Text;

namespace InateckMauiApp.Platforms.Android
{
    /// <summary>
    /// DeviceType enum matching Inateck SDK device types
    /// </summary>
    public enum InateckDeviceType
    {
        None = 0,
        Pro8 = 1,
        ST45 = 2,
        ST23 = 3,
        ST91 = 4,
        ST42 = 5,
        ST54 = 6,
        ST55 = 7,
        ST73 = 8,
        ST75 = 9,
        ST43 = 10,
        P7 = 11,
        ST21 = 12,
        ST60 = 13,
        ST70 = 14,
        P6 = 15,
        ST35 = 16,
        ST75S = 41,   // BCST-75S
        ST75SAI = 42  // BCST-75S AI version
    }

    /// <summary>
    /// Wrapper for Inateck Scanner CMD library (Parsing Library)
    /// This library parses scanner data - BLE communication is handled separately
    /// </summary>
    public static class InateckScannerCmd
    {
        private const string LibName = "libinateck_scanner_cmd.so";

        #region Native P/Invoke declarations

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_notify_data_result(byte[] data, UIntPtr length);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_auth();

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int inateck_scanner_cmd_check_auth_result(byte[] data, UIntPtr length);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_software_version();

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_software_result(byte[] data, UIntPtr length);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_set_bee(byte voiceTime, byte silentTime, byte count);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte inateck_scanner_cmd_check_result(byte[] data, UIntPtr length);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_set_led(byte color, byte lightTime, byte darkTime, byte count);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_set_name([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_set_time([MarshalAs(UnmanagedType.LPUTF8Str)] string time);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_get_settings();

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_get_settings_result(int deviceType, byte[] data, UIntPtr dataLength);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_set_settings(int deviceType, byte[] readData, UIntPtr readDataLength, [MarshalAs(UnmanagedType.LPUTF8Str)] string cmd);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_open_all_code();

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_close_all_code();

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_reset_all_code();

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_get_prefix();

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_set_prefix(byte[] data, UIntPtr length);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_get_suffix();

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_set_suffix(byte[] data, UIntPtr length);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_get_affix_result(byte[] data, UIntPtr length);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_get_reset();

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_get_restart();

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_get_inventory_upload_cache();

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_get_inventory_upload_count();

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_get_inventory_clear_cache();

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_get_mac();

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_get_mac_result(byte[] data, UIntPtr dataLength);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_get_version();

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr inateck_scanner_cmd_get_hid_output(byte outputType);

        #endregion

        #region Helper methods

        private static string PtrToString(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return null;
            return Marshal.PtrToStringUTF8(ptr);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Parse notification data received from scanner via GATT notification
        /// Returns JSON with parsed barcode data
        /// </summary>
        public static string ParseNotifyData(byte[] data)
        {
            if (data == null || data.Length == 0) return null;
            var ptr = inateck_scanner_cmd_notify_data_result(data, (UIntPtr)data.Length);
            return PtrToString(ptr);
        }

        /// <summary>
        /// Get the authentication command bytes to send to scanner
        /// </summary>
        public static string GetAuthCommand()
        {
            var ptr = inateck_scanner_cmd_auth();
            return PtrToString(ptr);
        }

        /// <summary>
        /// Check if authentication response is successful
        /// Returns 1 if success, 0 if failed
        /// </summary>
        public static int CheckAuthResult(byte[] responseData)
        {
            if (responseData == null || responseData.Length == 0) return 0;
            return inateck_scanner_cmd_check_auth_result(responseData, (UIntPtr)responseData.Length);
        }

        /// <summary>
        /// Get command to request software version
        /// </summary>
        public static string GetSoftwareVersionCommand()
        {
            var ptr = inateck_scanner_cmd_software_version();
            return PtrToString(ptr);
        }

        /// <summary>
        /// Parse software version response
        /// </summary>
        public static string ParseSoftwareVersion(byte[] responseData)
        {
            if (responseData == null || responseData.Length == 0) return null;
            var ptr = inateck_scanner_cmd_software_result(responseData, (UIntPtr)responseData.Length);
            return PtrToString(ptr);
        }

        /// <summary>
        /// Get command to make scanner beep
        /// </summary>
        public static string GetBeepCommand(byte voiceTime = 5, byte silentTime = 0, byte count = 1)
        {
            var ptr = inateck_scanner_cmd_set_bee(voiceTime, silentTime, count);
            return PtrToString(ptr);
        }

        /// <summary>
        /// Check generic command result
        /// </summary>
        public static byte CheckResult(byte[] responseData)
        {
            if (responseData == null || responseData.Length == 0) return 0;
            return inateck_scanner_cmd_check_result(responseData, (UIntPtr)responseData.Length);
        }

        /// <summary>
        /// Get command to control LED
        /// </summary>
        public static string GetLedCommand(byte color, byte lightTime, byte darkTime, byte count)
        {
            var ptr = inateck_scanner_cmd_set_led(color, lightTime, darkTime, count);
            return PtrToString(ptr);
        }

        /// <summary>
        /// Get command to set scanner name
        /// </summary>
        public static string GetSetNameCommand(string name)
        {
            var ptr = inateck_scanner_cmd_set_name(name);
            return PtrToString(ptr);
        }

        /// <summary>
        /// Get command to set scanner time
        /// </summary>
        public static string GetSetTimeCommand(string time)
        {
            var ptr = inateck_scanner_cmd_set_time(time);
            return PtrToString(ptr);
        }

        /// <summary>
        /// Get command to request scanner settings
        /// </summary>
        public static string GetSettingsCommand()
        {
            var ptr = inateck_scanner_cmd_get_settings();
            return PtrToString(ptr);
        }

        /// <summary>
        /// Parse settings response
        /// </summary>
        public static string ParseSettingsResult(InateckDeviceType deviceType, byte[] responseData)
        {
            if (responseData == null || responseData.Length == 0) return null;
            var ptr = inateck_scanner_cmd_get_settings_result((int)deviceType, responseData, (UIntPtr)responseData.Length);
            return PtrToString(ptr);
        }

        /// <summary>
        /// Get command to change scanner settings
        /// cmd format: JSON array like [{"area":"3","value":"4","name":"volume"}]
        /// </summary>
        public static string GetSetSettingsCommand(InateckDeviceType deviceType, byte[] currentSettings, string settingsJson)
        {
            if (currentSettings == null) currentSettings = new byte[0];
            var ptr = inateck_scanner_cmd_set_settings((int)deviceType, currentSettings, (UIntPtr)currentSettings.Length, settingsJson);
            return PtrToString(ptr);
        }

        /// <summary>
        /// Get command to enable all barcode types
        /// </summary>
        public static string GetOpenAllCodeCommand()
        {
            var ptr = inateck_scanner_cmd_open_all_code();
            return PtrToString(ptr);
        }

        /// <summary>
        /// Get command to disable all barcode types
        /// </summary>
        public static string GetCloseAllCodeCommand()
        {
            var ptr = inateck_scanner_cmd_close_all_code();
            return PtrToString(ptr);
        }

        /// <summary>
        /// Get command to reset barcode settings to defaults
        /// </summary>
        public static string GetResetAllCodeCommand()
        {
            var ptr = inateck_scanner_cmd_reset_all_code();
            return PtrToString(ptr);
        }

        /// <summary>
        /// Get command to request prefix settings
        /// </summary>
        public static string GetPrefixCommand()
        {
            var ptr = inateck_scanner_cmd_get_prefix();
            return PtrToString(ptr);
        }

        /// <summary>
        /// Get command to set prefix
        /// </summary>
        public static string GetSetPrefixCommand(byte[] prefixData)
        {
            if (prefixData == null) prefixData = new byte[0];
            var ptr = inateck_scanner_cmd_set_prefix(prefixData, (UIntPtr)prefixData.Length);
            return PtrToString(ptr);
        }

        /// <summary>
        /// Get command to request suffix settings
        /// </summary>
        public static string GetSuffixCommand()
        {
            var ptr = inateck_scanner_cmd_get_suffix();
            return PtrToString(ptr);
        }

        /// <summary>
        /// Get command to set suffix
        /// </summary>
        public static string GetSetSuffixCommand(byte[] suffixData)
        {
            if (suffixData == null) suffixData = new byte[0];
            var ptr = inateck_scanner_cmd_set_suffix(suffixData, (UIntPtr)suffixData.Length);
            return PtrToString(ptr);
        }

        /// <summary>
        /// Parse prefix/suffix response
        /// </summary>
        public static string ParseAffixResult(byte[] responseData)
        {
            if (responseData == null || responseData.Length == 0) return null;
            var ptr = inateck_scanner_cmd_get_affix_result(responseData, (UIntPtr)responseData.Length);
            return PtrToString(ptr);
        }

        /// <summary>
        /// Get command to factory reset scanner
        /// </summary>
        public static string GetResetCommand()
        {
            var ptr = inateck_scanner_cmd_get_reset();
            return PtrToString(ptr);
        }

        /// <summary>
        /// Get command to restart scanner
        /// </summary>
        public static string GetRestartCommand()
        {
            var ptr = inateck_scanner_cmd_get_restart();
            return PtrToString(ptr);
        }

        /// <summary>
        /// Get command to upload inventory cache (for USB connected scanners)
        /// </summary>
        public static string GetInventoryUploadCacheCommand()
        {
            var ptr = inateck_scanner_cmd_get_inventory_upload_cache();
            return PtrToString(ptr);
        }

        /// <summary>
        /// Get command to get inventory cache count
        /// </summary>
        public static string GetInventoryUploadCountCommand()
        {
            var ptr = inateck_scanner_cmd_get_inventory_upload_count();
            return PtrToString(ptr);
        }

        /// <summary>
        /// Get command to clear inventory cache
        /// </summary>
        public static string GetInventoryClearCacheCommand()
        {
            var ptr = inateck_scanner_cmd_get_inventory_clear_cache();
            return PtrToString(ptr);
        }

        /// <summary>
        /// Get command to request MAC address
        /// </summary>
        public static string GetMacCommand()
        {
            var ptr = inateck_scanner_cmd_get_mac();
            return PtrToString(ptr);
        }

        /// <summary>
        /// Parse MAC address response
        /// </summary>
        public static string ParseMacResult(byte[] responseData)
        {
            if (responseData == null || responseData.Length == 0) return null;
            var ptr = inateck_scanner_cmd_get_mac_result(responseData, (UIntPtr)responseData.Length);
            return PtrToString(ptr);
        }

        /// <summary>
        /// Get command to request version info
        /// </summary>
        public static string GetVersionCommand()
        {
            var ptr = inateck_scanner_cmd_get_version();
            return PtrToString(ptr);
        }

        /// <summary>
        /// Get command to set HID output type
        /// outputType: 0 = HID, 1 = SPP, 2 = GATT
        /// </summary>
        public static string GetSetHidOutputCommand(byte outputType)
        {
            var ptr = inateck_scanner_cmd_get_hid_output(outputType);
            return PtrToString(ptr);
        }

        #endregion

        #region Convenience methods for common settings

        /// <summary>
        /// Get JSON command to set volume level
        /// level: 0=mute, 2=low, 4=medium, 8=high
        /// </summary>
        public static string VolumeSettingJson(int level)
        {
            return $"[{{\"area\":\"3\",\"value\":\"{level}\",\"name\":\"volume\"}}]";
        }

        /// <summary>
        /// Get JSON command to enable DataMatrix scanning
        /// </summary>
        public static string DataMatrixOnJson => "[{\"area\":\"27\",\"value\":\"1\",\"name\":\"datamatrix_on\"}]";

        /// <summary>
        /// Get JSON command to disable DataMatrix scanning
        /// </summary>
        public static string DataMatrixOffJson => "[{\"area\":\"27\",\"value\":\"0\",\"name\":\"datamatrix_on\"}]";

        /// <summary>
        /// Get JSON command to enable QR Code scanning
        /// </summary>
        public static string QrCodeOnJson => "[{\"area\":\"28\",\"value\":\"1\",\"name\":\"qrcode_on\"}]";

        /// <summary>
        /// Get JSON command to set Bluetooth mode
        /// mode: 0=HID, 1=SPP, 2=GATT, 3=Receiver
        /// </summary>
        public static string BluetoothModeJson(int mode)
        {
            int btModeLow = (mode == 1 || mode == 3) ? 1 : 0;
            int btModeHigh = (mode == 2 || mode == 3) ? 1 : 0;
            return $"[{{\"area\":\"1\",\"value\":\"{btModeLow}\",\"name\":\"bt_mode_low\"}},{{\"area\":\"31\",\"value\":\"{btModeHigh}\",\"name\":\"bt_mode_high\"}}]";
        }

        #endregion
    }
}
