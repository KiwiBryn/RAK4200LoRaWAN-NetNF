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

namespace devMobile.IoT.LoRaWAN.nanoFramework.RAK4200
{
	using System.Diagnostics;

	public class Program
	{
		public static void Main()
		{
		}
	}
}
