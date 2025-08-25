using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReaderB;

namespace ConsoleApp
{
    internal class Reader
    {
        private int frmHandle = -1;
        private const string IP_ADDRESS = "192.168.1.190";
        private const int PORT = 6000;
        private byte comAddr = 0x00;

        public bool ConnectToReader()
        {
            try
            {
                int result = StaticClassReaderB.OpenNetPort(PORT, IP_ADDRESS, ref comAddr, ref frmHandle);

                if (result == 0)
                {
                    Console.WriteLine($"Successfully connected to RFID reader at {IP_ADDRESS}:{PORT}");
                    Console.WriteLine($"Handle: {frmHandle}, Address: 0x{comAddr:X2}");

                    return true;
                }
                else
                {
                    Console.WriteLine($"Failed to connect to RFID reader. Error code: 0x{result:X2}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred while connecting: {ex.Message}");
                return false;
            }
        }

        public void DisconnectFromReader()
        {
            if (frmHandle != -1)
            {
                try
                {
                    int result = StaticClassReaderB.CloseNetPort(frmHandle);
                    if (result == 0)
                    {
                        Console.WriteLine("Successfully disconnected from RFID reader");
                    }
                    else
                    {
                        Console.WriteLine($"Error disconnecting: 0x{result:X2}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during disconnect: {ex.Message}");
                }
                finally
                {
                    frmHandle = -1;
                }
            }
        }

        public void DisplayReaderInformation()
        {
            if (frmHandle == -1)
            {
                Console.WriteLine("Not connected to reader");
                return;
            }

            try
            {
                byte[] versionInfo = new byte[2];
                byte[] readerType = new byte[1];
                byte[] trType = new byte[2];
                byte[] dmaxfre = new byte[1];
                byte[] dminfre = new byte[1];
                byte[] powerdBm = new byte[1];
                byte[] scanTime = new byte[1];

                int result = StaticClassReaderB.GetReaderInformation(ref comAddr, versionInfo, ref readerType[0], trType,
                                                ref dmaxfre[0], ref dminfre[0], ref powerdBm[0], ref scanTime[0], frmHandle);

                if (result == 0)
                {
                    Console.WriteLine("=== Reader Information ===");
                    Console.WriteLine($"Version: {versionInfo[0]}.{versionInfo[1]}");
                    Console.WriteLine($"Reader Type: 0x{readerType[0]:X2}");
                    Console.WriteLine($"Protocol Type: 0x{trType[0]:X2}{trType[1]:X2}");

                    double minFreq = 902.6 + dminfre[0] * 0.4;
                    double maxFreq = 902.6 + dmaxfre[0] * 0.4;
                    Console.WriteLine($"Frequency Range: {minFreq:F1} - {maxFreq:F1} MHz");

                    Console.WriteLine($"Power: {powerdBm[0]} dBm");
                    Console.WriteLine($"Scan Time: {scanTime[0]} x 100ms");
                    Console.WriteLine("==========================");
                }
                else
                {
                    Console.WriteLine($"Failed to get reader information. Error: 0x{result:X2}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting reader information: {ex.Message}");
            }
        }

        public void InventoryTags()
        {
            if (frmHandle == -1)
            {
                Console.WriteLine("Not connected to reader");
                return;
            }

            try
            {
                byte[] epcData = new byte[1000];
                int totalLen = 0;
                int cardNum = 0;

                Console.WriteLine("Starting tag inventory...");

                int result = StaticClassReaderB.Inventory_G2(ref comAddr, 0, 0, 0, epcData, ref totalLen, ref cardNum, frmHandle);

                if (result == 0 || result == 0x01 || result == 0x02 || result == 0x03)
                {
                    Console.WriteLine($"Found {cardNum} tag(s)");

                    if (cardNum > 0)
                    {
                        int offset = 0;
                        for (int i = 0; i < cardNum; i++)
                        {
                            if (offset < totalLen)
                            {
                                int epcLen = epcData[offset];
                                offset++;

                                if (offset + epcLen <= totalLen)
                                {
                                    StringBuilder epcStr = new StringBuilder();
                                    for (int j = 0; j < epcLen; j++)
                                    {
                                        epcStr.Append($"{epcData[offset + j]:X2} ");
                                    }

                                    Console.WriteLine($"Tag {i + 1}: EPC = {epcStr.ToString().Trim()}");
                                    offset += epcLen;
                                }
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Inventory failed. Error code: 0x{result:X2}");
                    PrintErrorDescription(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during inventory: {ex.Message}");
            }
        }

        public void ReadTagMemory(string epcHex, byte memoryBank = 0x01, byte startAddress = 0x00, byte wordCount = 0x02)
        {
            if (frmHandle == -1)
            {
                Console.WriteLine("Not connected to reader");
                return;
            }

            try
            {
                byte[] epc = HexStringToByteArray(epcHex);
                byte[] password = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                byte[] data = new byte[240];
                byte[] errorcode = new byte[1];
                byte epcLength = (byte)epc.Length;

                Console.WriteLine($"Reading memory from tag EPC: {epcHex}");
                Console.WriteLine($"Memory Bank: 0x{memoryBank:X2}, Start Address: 0x{startAddress:X2}, Word Count: {wordCount}");

                int errorCodeInt = 0;
                int result = StaticClassReaderB.ReadCard_G2(ref comAddr, epc, memoryBank, startAddress, wordCount,
                                       password, 0, 0, 0, data, epcLength, ref errorCodeInt, frmHandle);
                errorcode[0] = (byte)errorCodeInt;

                if (result == 0)
                {
                    Console.WriteLine("Memory read successful:");
                    StringBuilder dataStr = new StringBuilder();
                    for (int i = 0; i < wordCount * 2; i++)
                    {
                        dataStr.Append($"{data[i]:X2} ");
                    }
                    Console.WriteLine($"Data: {dataStr.ToString().Trim()}");
                }
                else
                {
                    Console.WriteLine($"Memory read failed. Error code: 0x{result:X2}");
                    if (result == 0xFC)
                    {
                        Console.WriteLine($"Tag error code: 0x{errorcode[0]:X2}");
                    }
                    PrintErrorDescription(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading tag memory: {ex.Message}");
            }
        }

        private byte[] HexStringToByteArray(string hex)
        {
            hex = hex.Replace(" ", "").Replace("-", "");

            if (hex.Length % 2 != 0)
                hex = "0" + hex;

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        public void SetAnswerMode()
        {
            if (frmHandle == -1)
            {
                Console.WriteLine("Not connected to reader");
                return;
            }

            try
            {
                byte[] parameter = new byte[6];

                // Set work mode parameters for Answer Mode
                parameter[0] = 0x00; // Read_Mode: Bit1Bit0 = 0 means Responses (Answer mode)
                parameter[1] = 0x00; // Mode_State: Bit0 = 0 for 18000-6C protocol, Bit1 = 0 for Wiegand output
                parameter[2] = 0x01; // Mem_Inven: 0x01 = EPC store area
                parameter[3] = 0x00; // First_Adr: Start from first word
                parameter[4] = 0x02; // Word_Num: Read 2 words (4 bytes)
                parameter[5] = 0x01; // Tag_time: 1 second interval (not used in answer mode)

                int result = StaticClassReaderB.SetWorkMode(ref comAddr, parameter, frmHandle);

                if (result == 0)
                {
                    Console.WriteLine("Reader successfully set to Answer Mode");
                    Console.WriteLine("The reader will now only read tags when triggered by external commands");
                }
                else
                {
                    Console.WriteLine($"Failed to set Answer Mode. Error code: 0x{result:X2}");
                    PrintErrorDescription(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting Answer Mode: {ex.Message}");
            }
        }

        public void ReadOnTrigger()
        {
            if (frmHandle == -1)
            {
                Console.WriteLine("Not connected to reader");
                return;
            }

            Console.WriteLine("\n=== Trigger-Based Tag Reading ===");
            Console.WriteLine("Reader is in Answer Mode - tags will only be read when triggered");
            Console.WriteLine("Press SPACE to trigger tag reading, 'q' to quit\n");

            var seenTags = new HashSet<string>();

            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);

                if (keyInfo.Key == ConsoleKey.Q)
                {
                    Console.WriteLine("Exiting trigger-based reading mode");
                    break;
                }
                else if (keyInfo.Key == ConsoleKey.Spacebar)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Trigger activated - scanning for tags...");

                    try
                    {
                        byte[] epcData = new byte[1000];
                        int totalLen = 0;
                        int cardNum = 0;

                        int result = StaticClassReaderB.Inventory_G2(ref comAddr, 0, 0, 0, epcData, ref totalLen, ref cardNum, frmHandle);

                        if (result == 0 || result == 0x01 || result == 0x02 || result == 0x03)
                        {
                            if (cardNum > 0)
                            {
                                Console.WriteLine($"Found {cardNum} tag(s):");

                                int offset = 0;
                                for (int i = 0; i < cardNum; i++)
                                {
                                    if (offset < totalLen)
                                    {
                                        int epcLen = epcData[offset];
                                        offset++;

                                        if (offset + epcLen <= totalLen)
                                        {
                                            StringBuilder epcStr = new StringBuilder();
                                            for (int j = 0; j < epcLen; j++)
                                            {
                                                epcStr.Append($"{epcData[offset + j]:X2}");
                                            }

                                            string tagEpc = epcStr.ToString();

                                            if (!seenTags.Contains(tagEpc))
                                            {
                                                seenTags.Add(tagEpc);
                                                Console.WriteLine($"  NEW TAG: {tagEpc}");
                                            }
                                            else
                                            {
                                                Console.WriteLine($"  EXISTING: {tagEpc}");
                                            }

                                            offset += epcLen;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("No tags detected in range");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Scan failed. Error code: 0x{result:X2}");
                            PrintErrorDescription(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during triggered scan: {ex.Message}");
                    }

                    Console.WriteLine("Press SPACE again to trigger another scan, 'q' to quit\n");
                }
                else
                {
                    Console.WriteLine("Press SPACE to trigger scan or 'q' to quit");
                }
            }
        }

        public void StartRealtimeTagReading()
        {
            if (frmHandle == -1)
            {
                Console.WriteLine("Not connected to reader");
                return;
            }

            // Temporarily increase scan time for better real-time performance
            try
            {
                byte scanTime = 20; // 20 * 100ms = 2 seconds
                StaticClassReaderB.WriteScanTime(ref comAddr, ref scanTime, frmHandle);
                Console.WriteLine("Optimized scan time for real-time reading");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Note: Could not optimize scan time: {ex.Message}");
            }

            Console.WriteLine("\n=== Real-time Tag Reading Started ===");
            Console.WriteLine("Press 'q' and Enter to stop real-time reading...");
            Console.WriteLine("Scanning interval: 300ms\n");

            var seenTags = new HashSet<string>();
            var cancellationToken = new CancellationTokenSource();
            bool stopRequested = false;

            // Start background scanning
            var scanTask = Task.Run(async () =>
            {
                while (!stopRequested)
                {
                    try
                    {
                        byte[] epcData = new byte[1000];
                        int totalLen = 0;
                        int cardNum = 0;

                        int result = StaticClassReaderB.Inventory_G2(ref comAddr, 0, 0, 0, epcData, ref totalLen, ref cardNum, frmHandle);

                        if ((result == 0 || result == 0x01 || result == 0x02 || result == 0x03) && cardNum > 0)
                        {
                            int offset = 0;
                            for (int i = 0; i < cardNum; i++)
                            {
                                if (offset < totalLen)
                                {
                                    int epcLen = epcData[offset];
                                    offset++;

                                    if (offset + epcLen <= totalLen)
                                    {
                                        StringBuilder epcStr = new StringBuilder();
                                        for (int j = 0; j < epcLen; j++)
                                        {
                                            epcStr.Append($"{epcData[offset + j]:X2}");
                                        }

                                        string tagEpc = epcStr.ToString();

                                        if (!seenTags.Contains(tagEpc))
                                        {
                                            seenTags.Add(tagEpc);
                                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] NEW TAG DETECTED: {tagEpc}");
                                        }
                                        else
                                        {
                                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Tag present: {tagEpc}");
                                        }

                                        offset += epcLen;
                                    }
                                }
                            }
                        }
                        else if (result == 0x01)
                        {
                            // Scan returned early but might have partial data - this is often normal
                            // Only show message every 10 scans to avoid spam
                            if (DateTime.Now.Second % 10 == 0)
                            {
                                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Scanning... (quick scan mode)");
                            }
                        }
                        else if (result != 0xFB && result != 0x02 && result != 0x03) // Don't spam common inventory messages
                        {
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Scan error: 0x{result:X2}");
                            PrintErrorDescription(result);
                        }

                        // Clear seen tags every 30 seconds to detect re-entered tags
                        if (DateTime.Now.Second % 30 == 0 && seenTags.Count > 0)
                        {
                            seenTags.Clear();
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Tag cache cleared");
                        }

                        await Task.Delay(300);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Error during scanning: {ex.Message}");
                        await Task.Delay(2000); // Wait longer on error
                    }
                }
            });

            // Wait for user input to stop
            while (true)
            {
                var input = Console.ReadLine();
                if (input?.ToLower() == "q")
                {
                    stopRequested = true;
                    break;
                }
            }

            Console.WriteLine("\nStopping real-time scanning...");
            scanTask.Wait(5000); // Wait up to 5 seconds for clean shutdown

            // Restore original scan time
            try
            {
                byte scanTime = 10; // Restore to 10 * 100ms = 1 second default
                StaticClassReaderB.WriteScanTime(ref comAddr, ref scanTime, frmHandle);
                Console.WriteLine("Restored original scan time");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Note: Could not restore scan time: {ex.Message}");
            }

            Console.WriteLine("Real-time scanning stopped.\n");
        }

        private void PrintErrorDescription(int errorCode)
        {
            switch (errorCode)
            {
                case 0x01:
                    Console.WriteLine("Return before Inventory finished");
                    break;
                case 0x02:
                    Console.WriteLine("Inventory-scan-time overflow");
                    break;
                case 0x03:
                    Console.WriteLine("More Data");
                    break;
                case 0x04:
                    Console.WriteLine("Reader module MCU is Full");
                    break;
                case 0x05:
                    Console.WriteLine("Access password error");
                    break;
                case 0x30:
                    Console.WriteLine("Communication error");
                    break;
                case 0x31:
                    Console.WriteLine("CRC checksum error");
                    break;
                case 0x32:
                    Console.WriteLine("Return data length error");
                    break;
                case 0x33:
                    Console.WriteLine("Communication busy");
                    break;
                case 0x34:
                    Console.WriteLine("Busy, command is being executed");
                    break;
                case 0x35:
                    Console.WriteLine("ComPort Opened");
                    break;
                case 0x36:
                    Console.WriteLine("ComPort Closed");
                    break;
                case 0x37:
                    Console.WriteLine("Invalid Handle");
                    break;
                case 0x38:
                    Console.WriteLine("Invalid Port");
                    break;
                case 0xFB:
                    Console.WriteLine("No Tag Operation");
                    break;
                case 0xFC:
                    Console.WriteLine("Tag Return ErrorCode");
                    break;
                case 0xFD:
                    Console.WriteLine("Command length wrong");
                    break;
                case 0xFE:
                    Console.WriteLine("Illegal command");
                    break;
                case 0xFF:
                    Console.WriteLine("Parameter Error");
                    break;
                default:
                    Console.WriteLine("Unknown error");
                    break;
            }
        }
    }

}
