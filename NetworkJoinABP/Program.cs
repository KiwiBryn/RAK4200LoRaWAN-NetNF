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
#define ST_STM32F769I_DISCOVERY      // nanoff --target ST_STM32F769I_DISCOVERY --update 
//#define ESP32_WROOM   //nanoff --target ESP32_WROOM_32 --serialport COM4 --update
// May 2022 Still experiencing issues with ComPort assignments
//#define NETDUINO3_WIFI   // nanoff --target NETDUINO3_WIFI --update
namespace devMobile.IoT.LoRaWAN.nanoFramework.RAK4200
{
   using System;
   using System.Diagnostics;
   using System.IO.Ports;
   using System.Threading;
#if ESP32_WROOM
   using global::nanoFramework.Hardware.Esp32;  ///need NuGet nanoFramework.Hardware.Esp32
#endif

   public class Program
   {
#if ST_STM32F769I_DISCOVERY
      private const string SerialPortId = "COM6";
#endif
#if ESP32_WROOM
      private const string SerialPortId = "COM2";
#endif
      private const string DevEui = "...";
      private const string DevAddress = "...";
      private const string NwksKey = "...";
      private const string AppsKey = "...";
      private const byte MessagePort = 1;
      private const string Payload = "01020304"; // Is AQIDBA==

      public static void Main()
      {
         string response;

         Debug.WriteLine("devMobile.IoT.Rak4200.NetworkJoinABP starting");

         try
         {
#if ESP32_WROOM
			Configuration.SetPinFunction(Gpio.IO17, DeviceFunction.COM2_TX);
			Configuration.SetPinFunction(Gpio.IO16, DeviceFunction.COM2_RX);
#endif

            Debug.Write("Ports:");
            foreach (string port in SerialPort.GetPortNames())
            {
               Debug.Write($" {port}");
            }
            Debug.WriteLine("");

            using (SerialPort serialDevice = new SerialPort(SerialPortId))
            {
               // set parameters
               serialDevice.BaudRate = 9600;
               //_SerialPort.BaudRate = 115200;
               serialDevice.Parity = Parity.None;
               serialDevice.StopBits = StopBits.One;
               serialDevice.Handshake = Handshake.None;
               serialDevice.DataBits = 8;

               serialDevice.ReadTimeout = 10000;
               //serialDevice.ReadBufferSize = 128; 
               //serialDevice.ReadBufferSize = 256; 
               serialDevice.ReadBufferSize = 512;
               //serialDevice.ReadBufferSize = 1024;

               serialDevice.NewLine = "\r\n";

               serialDevice.DataReceived += SerialDevice_DataReceived;

               serialDevice.Open();

               serialDevice.WatchChar = '\n';

               // clear out the RX buffer
               serialDevice.ReadExisting();
               response = serialDevice.ReadExisting();
               Debug.WriteLine($"Response :{response.Trim()} bytes:{response.Length}");
               Thread.Sleep(500);

               // Set the Working mode to LoRaWAN
               Debug.WriteLine("lora:work_mode:0");
               serialDevice.WriteLine("at+set_config=lora:work_mode:0");
               Thread.Sleep(500);

               // Set the JoinMode
               Debug.WriteLine("lora:join_mode");
               serialDevice.WriteLine("at+set_config=lora:join_mode:1");
               Thread.Sleep(500);

               // Set the Class
               Debug.WriteLine("lora:class");
               serialDevice.WriteLine("at+set_config=lora:class:0");
               Thread.Sleep(500);

               // Set the Region to AS923
               Debug.WriteLine("lora:region:AS923");
               serialDevice.WriteLine("at+set_config=lora:region:AS923");
               Thread.Sleep(500);

               // Set the devEUI
               Debug.WriteLine("lora:dev_eui:{DevEui}");
               serialDevice.WriteLine($"at+set_config=lora:dev_eui:{DevEui}");
               Thread.Sleep(500);

               // Set the dev_addr
               Debug.WriteLine("lora:dev_addr: {DevAddress}");
               serialDevice.WriteLine($"at+set_config=lora:dev_addr:{DevAddress}");
               Thread.Sleep(500);

               // Set the Network session key
               Debug.WriteLine("lora:nwks_key:{NwksKey}");
               serialDevice.WriteLine($"at+set_config=lora:nwks_key:{NwksKey}");
               Thread.Sleep(500);

               // Set the appKey
               Debug.WriteLine("lora:apps_key:{AppsKey}");
               serialDevice.WriteLine($"at+set_config=lora:apps_key:{AppsKey}");
               Thread.Sleep(500);

               // Set the Confirm flag
               Debug.WriteLine("lora:confirm:0");
               serialDevice.WriteLine("at+set_config=lora:confirm:0");
               Thread.Sleep(500);

               // Join the network
               Debug.WriteLine("at+join");
               serialDevice.WriteLine("at+join");
               Thread.Sleep(10000);

               while (true)
               {
                  // Send the BCD messages
                  Debug.WriteLine("lora:{MessagePort}:{Payload}");
                  serialDevice.WriteLine($"at+send=lora:{MessagePort}:{Payload}");

                  Thread.Sleep(20000);
               }
            }
         }
         catch (Exception ex)
         {
            Debug.WriteLine(ex.Message);
         }
      }

      private static void SerialDevice_DataReceived(object sender, SerialDataReceivedEventArgs e)
      {
         SerialPort serialPort = (SerialPort)sender;
         string response;

         switch (e.EventType)
         {
            case SerialData.Chars:
               break;

            case SerialData.WatchChar:
               response = serialPort.ReadExisting();
               Debug.Write(response);
               break;
            default:
               Debug.Assert(false, $"e.EventType {e.EventType} unknown");
               break;
         }
      }
   }
}
