//---------------------------------------------------------------------------------
// Copyright (c) May 2022, devMobile Software
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Must have one of following options defined in the project\build definitions
//    PAYLOAD_HEX or PAYLOAD_BYTES
//    OTAA or ABP
//
// Optional definitions
//		DEVICE_DEVEUI_SET
//		DEVICE_FACTORY_SETTINGS
//    CONFIRMED For confirmed messages
//
//---------------------------------------------------------------------------------
//#define ST_STM32F769I_DISCOVERY      // nanoff --target ST_STM32F769I_DISCOVERY --update 
#define ESP32_WROOM   //nanoff --target ESP32_PSRAM_REV0 --serialport COM17 --update
//#define DEVICE_DEVEUI_SET
//#define FACTORY_RESET
//#define PAYLOAD_BCD
#define PAYLOAD_BYTES
//#define OTAA
//#define ABP
//#define CONFIRMED or UNCONFIRMED
//#define REGION_SET
//#define ADR_SET
namespace devMobile.IoT.LoRaWAN
{
	using System;
	using System.Threading;
	using System.Diagnostics;
	using System.IO.Ports;
#if ESP32_WROOM
	using nanoFramework.Hardware.Esp32; //need NuGet nanoFramework.Hardware.Esp32
#endif

	public class Program
	{
#if ST_STM32F769I_DISCOVERY
		private const string SerialPortId = "COM6";
#endif
#if ESP32_WROOM
		private const string SerialPortId = "COM2";
#endif
		private const string Region = "AS923";
		private static readonly TimeSpan JoinTimeOut = new TimeSpan(0, 0, 10);
		private static readonly TimeSpan SendTimeout = new TimeSpan(0, 0, 10);
		private const byte MessagePort = 1;
#if PAYLOAD_BCD
		private const string PayloadBcd = "48656c6c6f204c6f526157414e"; // Hello LoRaWAN in BCD
#endif
#if PAYLOAD_BYTES
      private static readonly byte[] PayloadBytes = { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x20, 0x4c, 0x6f, 0x52, 0x61, 0x57, 0x41, 0x4e}; // Hello LoRaWAN in bytes
#endif

		public static void Main()
		{
			Result result;

			Debug.WriteLine("devMobile.IoT.RAK4200LoRaWANDeviceClient starting");

			try
			{
				// set GPIO functions for COM2 (this is UART1 on ESP32)
#if ESP32_WROOM
				Configuration.SetPinFunction(Gpio.IO16, DeviceFunction.COM2_TX);
				Configuration.SetPinFunction(Gpio.IO17, DeviceFunction.COM2_RX);
#endif

				Debug.Write("Ports:");
				foreach (string port in SerialPort.GetPortNames())
				{
					Debug.Write($" {port}");
				}
				Debug.WriteLine("");

				using (Rak4200LoRaWanDevice device = new Rak4200LoRaWanDevice())
				{
					result = device.Initialise(SerialPortId, 9600, Parity.None, 8, StopBits.One);
					if (result != Result.Success)
					{
						Debug.WriteLine($"Initialise failed {result}");
						return;
					}

#if CONFIRMED
               device.OnMessageConfirmation += OnMessageConfirmationHandler;
#endif
					device.OnReceiveMessage += OnReceiveMessageHandler;

#if FACTORY_RESET
					Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} FactoryReset");
					result = device.FactoryReset();
					if (result != Result.Success)
					{
						Debug.WriteLine($"FactoryReset failed {result}");
						return;
					}
#endif

#if DEVICE_DEVEUI_SET
					Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Device EUI");
					result = device.DeviceEui(Config.devEui);
					if (result != Result.Success)
					{
						Debug.WriteLine($"ADR on failed {result}");
						return;
					}
#endif

#if REGION_SET
					Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Region{Region}");
					result = device.Region(Region);
					if (result != Result.Success)
					{
						Debug.WriteLine($"Region on failed {result}");
						return;
					}
#endif

#if ADR_SET
					Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} ADR On");
					result = device.AdrOn();
					if (result != Result.Success)
					{
						Debug.WriteLine($"ADR on failed {result}");
						return;
					}
#endif
#if CONFIRMED
               Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Confirmed");
               result = device.UplinkMessageConfirmationOn();
               if (result != Result.Success)
               {
                  Debug.WriteLine($"Confirm on failed {result}");
                  return;
               }
#endif
#if UNCONFIRMED
					Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Unconfirmed");
					result = device.UplinkMessageConfirmationOff();
					if (result != Result.Success)
					{
						Debug.WriteLine($"Confirm off failed {result}");
						return;
					}
#endif

#if OTAA
					Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} OTAA");
					result = device.OtaaInitialise(Config.JoinEui, Config.AppKey);
					if (result != Result.Success)
					{
						Debug.WriteLine($"OTAA Initialise failed {result}");
						return;
					}
#endif

#if ABP
               Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} ABP");
               result = device.AbpInitialise(Config.DevAddress, Config.NwksKey, Config.AppsKey);
               if (result != Result.Success)
               {
                  Debug.WriteLine($"ABP Initialise failed {result}");
                  return;
               }
#endif

					Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Join start Timeout:{JoinTimeOut:hh:mm:ss}");
					result = device.Join(JoinTimeOut);
					if (result != Result.Success)
					{
						Debug.WriteLine($"Join failed {result}");
						return;
					}
					Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Join finish");

					while (true)
					{
#if PAYLOAD_BCD
						Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Send Timeout:{SendTimeout:hh:mm:ss} port:{MessagePort} payload BCD:{PayloadBcd}");
						result = device.Send(MessagePort, PayloadBcd, SendTimeout);
#endif
#if PAYLOAD_BYTES
                  Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Send Timeout:{SendTimeout:hh:mm:ss} port:{MessagePort} payload Bytes:{BitConverter.ToString(PayloadBytes)}");
                  result = device.Send(MessagePort, PayloadBytes, SendTimeout);
#endif
						if (result != Result.Success)
						{
							Debug.WriteLine($"Send failed {result}");
						}

						Thread.Sleep(new TimeSpan(0, 5, 0));
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
		}

#if CONFIRMED
      static void OnMessageConfirmationHandler(int rssi, int snr)
      {
         Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Send Confirm RSSI:{rssi} SNR:{snr}");
      }
#endif

		static void OnReceiveMessageHandler(byte port, int rssi, int snr, string payloadBcd)
		{
			byte[] payloadBytes = Rak4200LoRaWanDevice.HexToByes(payloadBcd);

			Debug.WriteLine($"{DateTime.UtcNow:hh:mm:ss} Receive Message RSSI:{rssi} SNR:{snr} Port:{port} Payload:{payloadBcd} PayLoadBytes:{BitConverter.ToString(payloadBytes)}");
		}

	}
}
