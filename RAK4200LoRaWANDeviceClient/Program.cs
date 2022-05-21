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
//    CONFIRMED For confirmed messages
//		DEVICE_DEVEUI_SET
//		DEVICE_FACTORY_SETTINGS
//
//---------------------------------------------------------------------------------
namespace devMobile.IoT.LoRaWAN.NanoFramework.RAK4200
{
	using System.Diagnostics;
	using System.Threading;

	public class Program
	{
		public static void Main()
		{
			Debug.WriteLine("Hello from nanoFramework!");

			Thread.Sleep(Timeout.Infinite);
		}
	}
}
