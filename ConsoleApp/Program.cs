using ConsoleApp;

class Program
{
    static void Main(string[] args)
    {
        Reader reader = new Reader();

        Console.WriteLine("=== RFID Reader Demo ===");
        Console.WriteLine("Connecting to reader at 192.168.1.190:6000...");

        if (reader.ConnectToReader())
        {
            try
            {
                while (true)
                {
                    Console.WriteLine("\n**************************");
                    Console.WriteLine("Options:");
                    Console.WriteLine("1. Show reader information");
                    Console.WriteLine("2. Trigger-based reading (Answer Mode)");
                    Console.WriteLine("3. Real-time tag reading (continuous)");
                    Console.WriteLine("4. Exit");
                    Console.Write("Select option: ");

                    string? input = Console.ReadLine();

                    switch (input)
                    {
                        case "1":
                            reader.DisplayReaderInformation();
                            break;
                        case "2":
                            reader.ReadOnTrigger();
                            break;
                        case "3":
                            reader.StartRealtimeTagReading();
                            break;
                        case "4":
                            goto exit;
                        default:
                            Console.WriteLine("Invalid option");
                            break;
                    }
                }

            exit:
                reader.DisconnectFromReader();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Failed to connect to RFID reader");
        }

        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    }
}
