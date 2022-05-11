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
// May 2022 Still experiencing issues with ComPort assignments
//#define ESP32_WROOM   //nanoff --target ESP32_WROOM_32 --serialport COM4 --update
//#define NETDUINO3_WIFI   // nanoff --target NETDUINO3_WIFI --update
namespace devMobile.IoT.LoRaWAN.nanoFramework.RAK4200
{
   using System;
	using System.Diagnostics;
   using System.IO.Ports;
   using System.Threading;

   public class Program
	{
      private const string SerialPortId = "COM6";
      private const string DevEui = "...";
      private const string AppEui = "...";
      private const string AppKey = "...";
      private const byte MessagePort = 1;
      private const string Payload = "48656c6c6f204c6f526157414e"; // Hello LoRaWAN

      public static void Main()
      {
         string response;

         Debug.WriteLine("devMobile.IoT.Rak4200.NetworkJoinOTAA starting");

         Debug.Write("Ports:");
         foreach (string port in SerialPort.GetPortNames())
         {
            Debug.Write($" {port}");
         }
         Debug.WriteLine("");

         try
         {
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
               Console.WriteLine("lora:work_mode:0");
               serialDevice.WriteLine("at+set_config=lora:work_mode:0");

               // Set the JoinMode
               Console.WriteLine("lora:join_mode");
               serialDevice.WriteLine("at+set_config=lora:join_mode:0");
               Thread.Sleep(500);

               // Set the Class
               Console.WriteLine("lora:class");
               serialDevice.WriteLine("at+set_config=lora:class:0");
               Thread.Sleep(500);

               // Set the Region to AS923
               Console.WriteLine("lora:region:AS923");
               serialDevice.WriteLine("at+set_config=lora:region:AS923");
               Thread.Sleep(500);

               // Set the devEUI
               Console.WriteLine("lora:dev_eui:{DevEui}");
               serialDevice.WriteLine($"at+set_config=lora:dev_eui:{DevEui}");
               Thread.Sleep(500);

               // Set the appEUI
               Console.WriteLine("lora:app_eui:{AppEui}");
               serialDevice.WriteLine($"at+set_config=lora:app_eui:{AppEui}");
               Thread.Sleep(500);

               // Set the appKey
               Console.WriteLine("lora:app_key:{AppKey}");
               serialDevice.WriteLine($"at+set_config=lora:app_key:{AppKey}");
               Thread.Sleep(500);

               // Set the Confirm flag
               Console.WriteLine("lora:confirm:0");
               serialDevice.WriteLine("at+set_config=lora:confirm:0");
               Thread.Sleep(500);

               // Reset the device
               Console.WriteLine("device:restart");
               serialDevice.WriteLine($"at+set_config=device:restart");
               Thread.Sleep(10000);

               // Join the network
               Console.WriteLine("at+join");
               serialDevice.WriteLine("at+join");
               Thread.Sleep(10000);

               while (true)
               {
                  // Send the BCD messages
                  Console.WriteLine("lora:{MessagePort}:{Payload}");
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
