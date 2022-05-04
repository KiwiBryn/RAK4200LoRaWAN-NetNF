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
//---------------------------------------------------------------------------------
//#define SERIAL_SYNC_READ
#define SERIAL_ASYNC_READ
//#define SERIAL_THREADED_READ
#define ST_STM32F769I_DISCOVERY      // nanoff --target ST_STM32F769I_DISCOVERY --update 
// May 2022 Still experiencing issues with ComPort assignments
//#define ESP32_WROOM   //nanoff --target ESP32_WROOM_32 --serialport COM4 --update
//#define NETDUINO3_WIFI   // nanoff --target NETDUINO3_WIFI --update
//#define ST_NUCLEO64_F091RC // nanoff --target ST_NUCLEO64_F091RC --update 
//#define ST_NUCLEO144_F746ZG //nanoff --target ST_NUCLEO144_F746ZG --update

namespace devMobile.IoT.LoRaWAN.NetCore.RAK4200
{
	using System;
	using System.Diagnostics;
	using System.IO.Ports;
	using System.Threading;

	public class Program
	{
		private static SerialPort _SerialPort;
#if SERIAL_THREADED_READ
		private static Boolean _Continue = true;
#endif
#if ESP32_WROOM
      private const string SerialPortId = "";
#endif
#if NETDUINO3_WIFI
      private const string SerialPortId = "COM3";
#endif
#if MBN_QUAIL
      private const string SpiBusId = "";
#endif
#if ST_NUCLEO64_F091RC
      private const string SerialPortId = "";
#endif
#if ST_NUCLEO144_F746ZG
      private const string SerialPortId = "";
#endif
#if ST_STM32F429I_DISCOVERY
      private const string SerialPortId = "";
#endif
#if ST_STM32F769I_DISCOVERY
		private const string SerialPortId = "COM6";
#endif

		public static void Main()
		{
#if SERIAL_THREADED_READ
			Thread readThread = new Thread(SerialPortProcessor);
#endif

			Debug.WriteLine("devMobile.IoT.LoRaWAN.NetNF.RAK4200 BreakoutSerial starting");

			Debug.Write("Ports:");
			foreach (string port in SerialPort.GetPortNames())
			{
				Debug.Write($" {port}");
			}
			Debug.WriteLine("");

			try
			{
				// set GPIO functions for COM2 (this is UART1 on ESP32)
#if ESP32_WROOM
				Configuration.SetPinFunction(Gpio.IO04, DeviceFunction.COM2_TX);
            Configuration.SetPinFunction(Gpio.IO05, DeviceFunction.COM2_RX);
#endif

				_SerialPort = new SerialPort(SerialPortId);

				// set parameters
				_SerialPort.BaudRate = 115200;
				_SerialPort.Parity = Parity.None;
				_SerialPort.DataBits = 8;
				_SerialPort.StopBits = StopBits.One;
				_SerialPort.Handshake = Handshake.None;

				_SerialPort.ReadTimeout = 1000;
				_SerialPort.WatchChar = '\n';
				_SerialPort.NewLine = "\r\n";

				_SerialPort.Open();

#if SERIAL_THREADED_READ
				readThread.Start();
#endif

#if SERIAL_ASYNC_READ
				_SerialPort.DataReceived += SerialDevice_DataReceived;
#endif

				while (true)
				{
					string atCommand = "at+version";
					Debug.WriteLine($"TX:{atCommand} bytes:{atCommand.Length}");
					_SerialPort.WriteLine(atCommand);

#if SERIAL_SYNC_READ
					// Read the response
					string response = _SerialPort.ReadLine();
					Debug.WriteLine($"RX:{response.Trim()} bytes:{response.Length}");
#endif
					Thread.Sleep(20000);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
		}

#if SERIAL_ASYNC_READ
		private static void SerialDevice_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			SerialPort serialPort = (SerialPort)sender;

			switch (e.EventType)
			{
				case SerialData.Chars:
					string response = serialPort.ReadExisting();

					if ( response.Length>0)
					{ 
						Debug.WriteLine($"RX:{response.Trim()} bytes:{response.Length}");
					}
					break;
				case SerialData.WatchChar:
					Debug.WriteLine($"RX:WatchChar");
					break;
				default:
					Debug.Assert(false, $"e.EventType {e.EventType} unknown");
					break;
			}
		}
#endif

#if SERIAL_THREADED_READ
		public static void SerialPortProcessor()
		{
			string message;

			while (_Continue)
			{
				try
				{
					message = _SerialPort.ReadLine();
					Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss} 1:{message}");
				}
				catch (TimeoutException) 
				{
					message = _SerialPort.ReadExisting();
					Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss} Timeout:{message}");
				}
			}
		}
#endif
	}
}