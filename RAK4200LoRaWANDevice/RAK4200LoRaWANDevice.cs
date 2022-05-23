﻿//---------------------------------------------------------------------------------
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
#define DIAGNOSTICS
namespace devMobile.IoT.LoRaWAN
{
	using System;
#if DIAGNOSTICS
	using System.Diagnostics;
#endif
	using System.IO.Ports;
	using System.Text;
	using System.Threading;

	/// <summary>
	/// The LoRaWAN device classes. From The Things Network definitions
	/// </summary>
	public enum LoRaWANDeviceClass
	{
		Undefined = 0,
		/// <summary>
		/// Class A devices support bi-directional communication between a device and a gateway. Uplink messages (from 
		/// the device to the server) can be sent at any time. The device then opens two receive windows at specified 
		/// times (RX1 Delay and RX2 Delay) after an uplink transmission. If the server does not respond in either of 
		/// these receive windows, the next opportunity will be after the next uplink transmission from the device. 
		A,
		/// <summary>
		/// Class B devices extend Class A by adding scheduled receive windows for downlink messages from the server. 
		/// Using time-synchronized beacons transmitted by the gateway, the devices periodically open receive windows. 
		/// The time between beacons is known as the beacon period, and the time during which the device is available 
		/// to receive downlinks is a “ping slot.”
		/// </summary>
		B,
		/// <summary>
		/// Class C devices extend Class A by keeping the receive windows open unless they are transmitting, as shown 
		/// in the figure below. This allows for low-latency communication but is many times more energy consuming than 
		/// Class A devices.
		/// </summary>
		C
	}

	public enum LoRaConfirmType
	{
		Undefined = 0,
		Unconfirmed,
		Confirmed,
	}

	/// <summary>
	/// Possible results of library methods (combination of RAK4200 AT command and state machine errors)
	/// </summary>
	public enum Result
	{
		Undefined = 0,
		/// <summary>
		/// Command executed without error.
		/// </summary>
		Success,
		/// <summary>
		/// Command failed to complete in configured duration.
		/// </summary>
		Timeout,
		/// <summary>
		/// The error code returned by the modem is unknown
		/// </summary>
		ErrorCodeUnknown,
		/// <summary>
		/// The last command received is an unsupported AT command.
		/// </summary>
		ATCommandUnsupported,
		/// <summary>
		/// Invalid parameter in the AT command.
		/// </summary>
		ATCommandInvalidParameter,
		/// <summary>
		/// Reading or writing the flash memory failed
		/// </summary>
		ReadingOrWritingFlashError,
		/// <summary>
		/// There is an error when sending data through the UART port. Check if you exceed 256 bytes UART buffer.
		/// </summary>
		UartError,
		/// <summary>
		/// The LoRa transceiver is busy, could not process a new command.
		/// </summary>
		LoRaTransceiverBusy,
		/// <summary>
		/// LoRa service is unknown.Unknown MAC command received by node.Execute commands that are not supported in the current state, such as sending at+join command in P2P mode.
		/// </summary>
		LoRaServiceIsUnknown,
		/// <summary>
		/// The LoRa parameters are invalid.
		/// </summary>
		LoRaParameterInvalid,
		/// <summary>
		/// The LoRa frequency is invalid.
		/// </summary>
		LoRaFrequencyInvalid,
		/// <summary>
		/// The LoRa data rate (DR) is invalid.
		/// </summary>
		LoRaDataRateInvalid,
		/// <summary>
		/// The LoRa frequency and data rate are invalid.
		/// </summary>
		LoRaFrequencyAndDataRateInvalid,
		/// <summary>
		/// The device hasn’t joined into a LoRa network.
		/// </summary>
		NetworkNotJoined,
		/// <summary>
		/// The length of the packet exceeded that maximum allowed by the LoRa protocol.
		/// </summary>
		PacketToLong,
		/// <summary>
		/// Service is closed by the server. Due to the limitation of duty cycle, the server will send "SRV_MAC_DUTY_CYCLE_REQ" MAC command to close the service.
		/// </summary>
		ServiceIsClosedByServer,
		/// <summary>
		/// This is an unsupported region code.
		/// </summary>
		RegionCodeUnsupported,
		/// <summary>
		/// Duty cycle is restricted.Due to duty cycle, data cannot be sent at this time until the time limit is removed.
		/// </summary>
		DutyCycleRestricted,
		/// <summary>
		/// No valid LoRa channel could be found.
		/// </summary>
		ChannelNotFound,
		/// <summary>
		/// No available LoRa channel could be found.
		/// </summary>
		ChannelNotAvailable,
		/// <summary>
		/// Status is error.Generally, the internal state of the protocol stack is wrong.
		/// </summary>
		StatusInvalid,
		/// <summary>
		/// Timeout reached while sending the packet through the LoRa transceiver.
		/// </summary>
		TransmitTimeout,
		/// <summary>
		/// Timeout reached while waiting for a packet in the LoRa RX1 window.
		/// </summary>
		PacketReceiveRX1Timeout,
		/// <summary>
		/// Time out reached while waiting for a packet in the LoRa RX2 window.
		/// </summary>
		PacketReceiveRX2Timeout,
		/// <summary>
		/// There is an error while receiving a packet during the LoRa RX1 window.
		/// </summary>
		PacketReceiveRX1Error,
		/// <summary>
		/// There is an error while receiving a packet during the LoRa RX2 window.
		/// </summary>
		PacketReceiveRX2Error,
		/// <summary>
		/// Failed to join into a LoRa network.
		/// </summary>
		NetworkJoinFailed,
		/// <summary>
		/// Duplicate downlink message is detected.A message with an invalid downlink count is received.
		/// </summary>
		DuplicateDownlink,
		/// <summary>
		/// Payload size is not valid for the current data rate (DR).
		/// </summary>
		PayloadSizeInvalidforDataRate,
		/// <summary>
		/// Many downlink packets are lost.
		/// </summary>
		DownlinkPacketsLost,
		/// <summary>
		/// Address fail. The address of the received packet does not match the address of the current node.
		/// </summary>
		AddressInvalid,
		/// <summary>
		/// Invalid MIC is detected in the LoRa message.
		/// </summary>
		MicInvalid,
	}

	/// <summary>
	/// RAK4200 client implementation (LoRaWAN only).
	/// </summary>
	public class Rak4200LoRaWanDevice : IDisposable
	{
		/// <summary>
		/// The DevEUI is a 64-bit globally-unique Extended Unique Identifier (EUI-64) assigned by the manufacturer, or
		/// the owner, of the end-device. This is represented by a 16 character long string
		/// </summary>
		public const byte DevEuiLength = 16;
		/// <summary>
		/// The JoinEUI(formerly known as AppEUI) is a 64-bit globally-unique Extended Unique Identifier (EUI-64).Each 
		/// Join Server, which is used for authenticating the end-devices, is identified by a 64-bit globally unique 
		/// identifier, JoinEUI, that is assigned by either the owner or the operator of that server. This is 
		/// represented by a 16 character long string.
		/// </summary>
		public const byte JoinEuiLength = 16;
		/// <summary>
		/// The AppKey is the encryption key between the source of the message (based on the DevEUI) and the destination 
		/// of the message (based on the AppEUI). This key must be unique for each device. This is represented by a 32 
		/// character long string
		/// </summary>
		public const byte AppKeyLength = 32;
		/// <summary>
		/// The DevAddr is composed of two parts: the address prefix and the network address. The address prefix is 
		/// allocated by the LoRa Alliance® and is unique to each network that has been granted a NetID. This is 
		/// represented by an 8 character long string.
		/// </summary>
		public const byte DevAddrLength = 8;
		/// <summary>
		/// After activation, the Network Session Key(NwkSKey) is used to secure messages which do not carry a payload.
		/// </summary>
		public const byte NwsKeyLength = 32;
		/// <summary>
		/// The AppSKey is an application session key specific for the end-device. It is used by both the application 
		/// server and the end-device to encrypt and decrypt the payload field of application-specific data messages.
		/// This is represented by an 32 character long string
		/// </summary>
		public const byte AppsKeyLength = 32;
		/// <summary>
		/// The minimum supported port number. Port 0 is used for FRMPayload which contains MAC commands only.
		/// </summary>
		public const byte MessagePortMinimumValue = 1;
		/// <summary>
		/// The maximum supported port number. Port 224 is used for the LoRaWAN Mac layer test protocol. Ports 
		/// 223…255 are reserved for future application extensions.
		/// </summary>
		public const byte MessagePortMaximumValue = 223;

		private const string ErrorMarker = "ERROR:";
		private const string ReplyMarker = "at+recv=";
		private readonly TimeSpan CommandTimeoutDefault = new TimeSpan(0, 0, 5);

		private SerialPort serialDevice = null;

		private string ATCommandExpectedResponse;
		private readonly AutoResetEvent ATCommandResponseExpectedEvent;
		private StringBuilder Response;
		private Result Result;

		/// <summary>
		/// Event handler called when network join process completed.
		/// </summary>
		/// <param name="joinSuccessful">Was the network join attempt successful</param>
		public delegate void JoinCompletionHandler(bool joinSuccessful);
		public JoinCompletionHandler OnJoinCompletion;
		/// <summary>
		/// Event handler called when uplink message delivery to network confirmed
		/// </summary>
		public delegate void MessageConfirmationHandler(int rssi, int snr);
		public MessageConfirmationHandler OnMessageConfirmation;
		/// <summary>
		/// Event handler called when downlink message received.
		/// </summary>
		/// <param name="port">LoRaWAN Port number.</param>
		/// <param name="rssi">Received Signal Strength Indicator(RSSI).</param>
		/// <param name="snr">Signal to Noise Ratio(SNR).</param>
		/// <param name="payload">Hexadecimal representation of payload.</param>
		public delegate void ReceiveMessageHandler(byte port, int rssi, int snr, string payload);
		public ReceiveMessageHandler OnReceiveMessage;

		public Rak4200LoRaWanDevice()
		{
			this.Response = new StringBuilder(512);
			this.ATCommandResponseExpectedEvent = new AutoResetEvent(false);
		}

		/// <summary>
		/// Initializes a new instance of the devMobile.IoT.LoRaWAN.NetCore.RAK3172.Rak3172LoRaWanDevice class using the
		/// specified port name, baud rate, parity bit, data bits, and stop bit.
		/// </summary>
		/// <param name="serialPortId">The port to use (for example, COM1).</param>
		/// <param name="baudRate">The baud rate, 600 to 115K2.</param>
		/// <param name="serialParity">One of the System.IO.Ports.SerialPort.Parity values, defaults to None.</param>
		/// <param name="dataBits">The data bits value, defaults to 8.</param>
		/// <param name="stopBits">One of the System.IO.Ports.SerialPort.StopBits values, defaults to One.</param>
		/// <exception cref="System.IO.IOException">The serial port could not be found or opened.</exception>
		/// <exception cref="UnauthorizedAccessException">The application does not have the required permissions to open the serial port.</exception>
		/// <exception cref="ArgumentNullException">The serialPortId is null.</exception>
		/// <exception cref="ArgumentException">The specified serialPortId, baudRate, serialParity, dataBits, or stopBits is invalid.</exception>
		/// <exception cref="InvalidOperationException">The attempted operation was invalid e.g. the port was already open.</exception>
		/// <returns><see cref="Result"/> of the operation.</returns>
		public Result Initialise(string serialPortId, int baudRate, Parity serialParity = Parity.None, ushort dataBits = 8, StopBits stopBits = StopBits.One)
		{
			if (serialPortId == null) 
			{
				throw new ArgumentNullException(nameof(serialPortId));
			}

			if (serialPortId == string.Empty)
			{
				throw new ArgumentException(nameof(serialPortId));
			}

			serialDevice = new SerialPort(serialPortId)
			{
				BaudRate = baudRate,
				Parity = serialParity,
				StopBits = stopBits,
				Handshake = Handshake.None,
				DataBits = dataBits,

				// BHL necessary?
				ReadTimeout = 10000,
				ReadBufferSize = 512,

				NewLine = "\r\n"
			};

			serialDevice.DataReceived += SerialDevice_DataReceived;

			serialDevice.Open();

			serialDevice.WatchChar = '\n';

			// clear out the input buffer.
			serialDevice.ReadExisting();

			// Set the Working mode to LoRaWAN, not/never going todo P2P with this library.
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:work_mode:0");
#endif
			Result result = SendCommand("Initialization OK", "at+set_config=lora:work_mode:0", CommandTimeoutDefault);
			if (result != Result.Success)
			{
#if DIAGNOSTICS
            Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:work_mode:0 failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		/// <summary>
		/// Sets the DeviceEUI
		/// </summary>
		/// <param name="deviceEui">The device EUI.</param>
		/// <exception cref="ArgumentNullException">The device EUI value is null.</exception>
		/// <exception cref="System.IO.ArgumentException">The deviceEui length is incorrect.</exception>
		/// <returns><see cref="Result"/> of the operation.</returns>
		public Result DeviceEui(string deviceEui)
		{
			if (deviceEui == null)
			{
				throw new ArgumentNullException(nameof(deviceEui), $"DeviceEUI is invalid");
			}

			if (deviceEui.Length != DevEuiLength)
			{
				throw new ArgumentException($"DevEUI invalid length must be {DevEuiLength} characters", nameof(deviceEui));
			}

#if DIAGNOSTICS
			Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} AT+DEVEUI={deviceEui}");
#endif
			Result result = SendCommand("OK",$"at+set_config=lora:dev_eui:{deviceEui}", CommandTimeoutDefault);
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} AT+DEVEUI failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		/// <summary>
		/// Sets the LoRaWAN device class.
		/// </summary>
		/// <param name="loRaClass" cref="LoRaWANDeviceClass">The LoRaWAN device class</param>
		/// <exception cref="System.IO.ArgumentException">The loRaClass is invalid.</exception>
		/// <returns><see cref="Result"/> of the operation.</returns>

		public Result Class(LoRaWANDeviceClass loRaClass)
		{
			string command;

			switch (loRaClass)
			{
				case LoRaWANDeviceClass.A:
					command = "at+set_config=lora:class:0";
					break;
				case LoRaWANDeviceClass.B:
					command = "at+set_config=lora:class:1";
					break;
				case LoRaWANDeviceClass.C:
					command = "at+set_config=lora:class:2";
					break;
				default:
					throw new ArgumentException($"LoRa class value {loRaClass} invalid", nameof(loRaClass));
			}

			// Set the class
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} {command}");
#endif
			Result result = SendCommand("OK",command, CommandTimeoutDefault);
			if (result != Result.Success)
			{
#if DIAGNOSTICS
            Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} {command} failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		/// <summary>
		/// Disables uplink message confirmations.
		/// </summary>
		/// <returns><see cref="Result"/> of the operation.</returns>
		public Result UplinkMessageConfirmationOff()
		{
			// Set the confirmation type
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:confirm:0");
#endif
			Result result = SendCommand("OK", "at+set_config=lora:confirm:0",CommandTimeoutDefault);
			if (result != Result.Success)
			{
#if DIAGNOSTICS
            Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} AT+CFM=0 failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		/// <summary>
		/// Enables uplink message confirmations.
		/// </summary>
		/// <returns><see cref="Result"/> of the operation.</returns>
		public Result UplinkMessageConfirmationOn()
		{
			// Set the confirmation type
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:confirm:1");
#endif
			Result result = SendCommand("OK", "at+set_config=lora:confirm:1", CommandTimeoutDefault);
			if (result != Result.Success)
			{
#if DIAGNOSTICS
            Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} AT+CFM=1 failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		/// <summary>
		/// Sets the band/region.
		/// </summary>
		/// <param name="Region">The LoRaWAN region code plus optional regional configuration settings.</param>
		/// <exception cref="ArgumentNullException">The region value is null.</exception>
		/// <returns><see cref="Result"/> of the operation.</returns>
		public Result Region(string region)
		{
			if (region == null)
			{
				throw new ArgumentNullException(nameof(region), $"Region is invalid");
			}

#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:region:{region}");
#endif
			Result result = SendCommand("OK", $"at+set_config=lora:region:{region}", CommandTimeoutDefault);
			if (result != Result.Success)
			{
#if DIAGNOSTICS
            Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:region failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		/// <summary>
		/// Disables Adaptive Data Rate(ADR) support.
		/// </summary>
		/// <returns><see cref="Result"/> of the operation.</returns>
		public Result AdrOff()
		{
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:adr:0");
#endif
			Result result = SendCommand("OK", "at+set_config=lora:adr:0", CommandTimeoutDefault);
			if (result != Result.Success)
			{
#if DIAGNOSTICS
            Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:adr:1 failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		/// <summary>
		/// Enables Adaptive Data Rate(ADR) support
		/// </summary>
		/// <returns><see cref="Result"/> of the operation.</returns>
		public Result AdrOn()
		{
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:adr:1");
#endif
			Result result = SendCommand("OK", "at+set_config=lora:adr:1", CommandTimeoutDefault);
			if (result != Result.Success)
			{
#if DIAGNOSTICS
            Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:adr:1failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		/// <summary>
		/// Resets device back to factory settings.
		/// </summary>
		/// <returns><see cref="Result"/> of the operation.</returns>
		public Result FactoryReset()
		{
#if DIAGNOSTICS
			Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:adr:1");
#endif
			Result result = SendCommand("OK", "at+set_config=lora:default_parameters", CommandTimeoutDefault);
			if (result != Result.Success)
			{
#if DIAGNOSTICS
				Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:adr:1failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}


		/// <summary>
		/// Configures the device to use Activation By Personalisation(ABP) to connect to the LoRaWAN network
		/// </summary>
		/// <param name="devAddr">The device address<see cref="DevAddrLength"></param>
		/// <param name="nwksKey">The network sessions key<see cref="NwsKeyLength"> </param>
		/// <param name="appsKey">The application session key <see cref="AppsKeyLength"/></param>
		/// <exception cref="System.IO.ArgumentNullException">The devAddr, nwksKey or appsKey is null.</exception>
		/// <exception cref="System.IO.ArgumentException">The devAddr, nwksKey or appsKey length is incorrect.</exception>
		/// <returns><see cref="Result"/> of the operation.</returns>
		public Result AbpInitialise(string devAddr, string nwksKey, string appsKey)
		{
			Result result;

			if (devAddr == null)
			{
				throw new ArgumentNullException(nameof(devAddr));
			}

			if (devAddr.Length != DevAddrLength)
			{
				throw new ArgumentException($"devAddr invalid length must be {DevAddrLength} characters", nameof(devAddr));
			}

			if (nwksKey == null)
			{
				throw new ArgumentNullException(nameof(nwksKey));
			}

			if (nwksKey.Length != NwsKeyLength)
			{
				throw new ArgumentException($"nwksKey invalid length must be {NwsKeyLength} characters", nameof(nwksKey));
			}

			if (appsKey == null)
			{
				throw new ArgumentNullException(nameof(appsKey));
			}

			if (appsKey.Length != AppsKeyLength)
			{
				throw new ArgumentException($"appsKey invalid length must be {AppsKeyLength} characters", nameof(appsKey));
			}

			// Set the network join mode to ABP
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:join_mode:1");
#endif
			result = SendCommand("OK", "at+set_config=lora:join_mode:1", CommandTimeoutDefault);
			if (result != Result.Success)
			{
#if DIAGNOSTICS
            Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:join_mode:1 failed {result}" );
#endif
				return result;
			}

			// set the devAddr
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:dev_addr:{devAddr}");
#endif
			result = SendCommand("OK", $"at+set_config=lora:dev_addr:{devAddr}", CommandTimeoutDefault);
			if (result != Result.Success)
			{
#if DIAGNOSTICS
            Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:dev_addr failed {result}");
#endif
				return result;
			}

			// Set the nwsKey
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} AT+NWKSKEY={nwksKey}");
#endif
			result = SendCommand("OK", $"at+set_config=lora:nwks_key:{nwksKey}", CommandTimeoutDefault);
			if (result != Result.Success)
			{
#if DIAGNOSTICS
            Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} AT+NWKSKEY failed {result}");
#endif
				return result;
			}

			// Set the appsKey
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} AT+APPSKEY={appsKey}");
#endif
			result = SendCommand("OK", $"at+set_config=lora:apps_key:{appsKey}", CommandTimeoutDefault);
			if (result != Result.Success)
			{
#if DIAGNOSTICS
            Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} AT+APPSKEY failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		/// <summary>
		/// Configures the device to use Over The Air Activation(OTAA) to connect to the LoRaWAN network
		/// </summary>
		/// <param name="joinEui">The join server unique identifier <see cref="JoinEuiLength"/></param>
		/// <param name="appKey">The application key<see cref="AppKeyLength"/> </param>
		/// <exception cref="System.IO.ArgumentNullException">The joinEui or appKey is null.</exception>
		/// <exception cref="System.IO.ArgumentException">The joinEui or appKey length is incorrect.</exception>
		/// <returns><see cref="Result"/> of the operation.</returns>
		public Result OtaaInitialise(string joinEui, string appKey)
		{
			Result result;

			if (joinEui == null)
			{
				throw new ArgumentNullException(nameof(joinEui));
			}

			if (joinEui.Length != JoinEuiLength)
			{
				throw new ArgumentException($"appEui invalid length must be {JoinEuiLength} characters", nameof(joinEui));
			}

			if (appKey == null)
			{
				throw new ArgumentNullException(nameof(appKey));
			}

			if (appKey.Length != AppKeyLength)
			{
				throw new ArgumentException($"appKey invalid length must be {AppKeyLength} characters", nameof(appKey));
			}

			// Set the Network Join Mode to OTAA
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:join_mode:0");
#endif
			result = SendCommand("OK", "at+set_config=lora:join_mode:0", CommandTimeoutDefault);
			if (result != Result.Success)
			{
#if DIAGNOSTICS
            Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:join_mode:0 failed {result}");
#endif
				return result;
			}

			// Set the appEUI
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:app_eui:{joinEui}");
#endif
			result = SendCommand("OK", $"at+set_config=lora:app_eui:{joinEui}", CommandTimeoutDefault);
			if (result != Result.Success)
			{
#if DIAGNOSTICS
            Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:app_eui:{joinEui}");
#endif
				return result;
			}

			// Set the appKey
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:app_key:{appKey}");
#endif
			result = SendCommand("OK", $"at+set_config=lora:app_key:{appKey}", CommandTimeoutDefault);
			if (result != Result.Success)
			{
#if DIAGNOSTICS
            Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} at+set_config=lora:app_key:{appKey}");
#endif
				return result;
			}

			return Result.Success;
		}

		/// <summary>
		/// Starts the process which Joins device to the network
		/// </summary>
		/// <param name="JoinAttempts">Number of attempts made to join the network</param>
		/// <param name="retryIntervalSeconds">Delay between attempts to join the network</param>
		/// <returns><see cref="Result"/> of the operation.</returns>
		public Result Join(TimeSpan timeout)
		{
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} AT+JOIN");
#endif
			Result result = SendCommand("OK Join Success", "at+join", timeout);
			if (result != Result.Success)
			{
#if DIAGNOSTICS
            Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} AT+JOIN failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		/// <summary>
		/// Sends an uplink message in Hexadecimal format
		/// </summary>
		/// <param name="port">LoRaWAN Port number.</param>
		/// <param name="payload">Hexadecimal encoded bytes to send</param>
		/// <exception cref="ArgumentNullException">The payload string is null.</exception>
		/// <exception cref="ArgumentException">The payload string must be a multiple of 2 characters long.</exception>
		/// <exception cref="ArgumentException">The port is number is out of range must be <see cref="MessagePortMinimumValue"/> to <see cref="MessagePortMaximumValue"/>.</exception>
		/// <returns><see cref="Result"/> of the operation.</returns>
		public Result Send(byte port, string payload, TimeSpan timeout)
		{
			if ((port < MessagePortMinimumValue) || (port > MessagePortMaximumValue))
			{
				throw new ArgumentException($"Port invalid must be {MessagePortMinimumValue} to {MessagePortMaximumValue}", nameof(port));
			}

			if (payload == null)
			{
				throw new ArgumentNullException(nameof(payload));
			}

			if ((payload.Length % 2) != 0)
			{
				throw new ArgumentException("Payload length invalid must be a multiple of 2", nameof(payload));
			}

			// Send message the network
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} AT+SEND={port}:payload {payload}");
#endif
			Result result = SendCommand("OK", $"at+send=lora:{port}:{payload}", timeout);
			if (result != Result.Success)
			{
#if DIAGNOSTICS
            Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} AT+SEND failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		/// <summary>
		/// Sends an uplink message of array of bytes with a sepcified port number.
		/// </summary>
		/// <param name="port">LoRaWAN Port number.</param>
		/// <param name="payload">Array of bytes to send</param>
		/// <exception cref="ArgumentNullException">The payload array is null.</exception>
		/// <returns><see cref="Result"/> of the operation.</returns>
		public Result Send(byte port, byte[] payload, TimeSpan timeout)
		{
			if ((port < MessagePortMinimumValue) || (port > MessagePortMaximumValue))
			{
				throw new ArgumentException($"Port invalid must be greater than or equal to {MessagePortMinimumValue} and less than or equal to {MessagePortMaximumValue}", nameof(port));
			}

			if (payload == null)
			{
				throw new ArgumentNullException(nameof(payload));
			}

			string payloadHex = BytesToHex(payload);

			// Send message the network
#if DIAGNOSTICS
         Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} AT+SEND=:{port} payload {payloadHex}");
#endif
			Result result = SendCommand("OK", $"at+send=lora:{port}:{payloadHex}", timeout);
			if (result != Result.Success)
			{
#if DIAGNOSTICS
            Debug.WriteLine($" {DateTime.UtcNow:hh:mm:ss} AT+SEND failed {result}");
#endif
				return result;
			}

			return Result.Success;
		}

		private Result SendCommand(string expectedResponse, string command, TimeSpan timeout)
		{
			this.ATCommandExpectedResponse = expectedResponse;

			serialDevice.WriteLine(command);

			this.ATCommandResponseExpectedEvent.Reset();

			if (!this.ATCommandResponseExpectedEvent.WaitOne((int)timeout.TotalMilliseconds, false))
			{
				return Result.Timeout;
			}

			this.ATCommandExpectedResponse = string.Empty;

			return Result;
		}

		private Result ModemErrorParser(string errorText)
		{
			Result result;
			ushort errorNumber;

			try
			{
				errorNumber = ushort.Parse(errorText);
			}
			catch (Exception)
			{
				return Result.ErrorCodeUnknown;
			}

			switch (errorNumber)
			{
				case 1:
					result = Result.ATCommandUnsupported;
					break;
				case 2:
					result = Result.ATCommandInvalidParameter;
					break;
				case 3: //There is an error when reading or writing flash.
					result = Result.ReadingOrWritingFlashError;
					break;
				case 5: //There is an error when sending through UART
					result = Result.UartError;
					break;
				case 80:
					result = Result.LoRaTransceiverBusy;
					break;
				case 81:
					result = Result.LoRaServiceIsUnknown;
					break;
				case 82:
					result = Result.LoRaParameterInvalid;
					break;
				case 83:
					result = Result.LoRaFrequencyInvalid;
					break;
				case 84:
					result = Result.LoRaDataRateInvalid;
					break;
				case 85:
					result = Result.LoRaFrequencyAndDataRateInvalid;
					break;
				case 86:
					result = Result.NetworkNotJoined;
					break;
				case 87:
					result = Result.PacketToLong;
					break;
				case 88:
					result = Result.ServiceIsClosedByServer;
					break;
				case 89:
					result = Result.RegionCodeUnsupported;
					break;
				case 90:
					result = Result.DutyCycleRestricted;
					break;
				case 91:
					result = Result.ChannelNotFound;
					break;
				case 92:
					result = Result.ChannelNotAvailable;
					break;
				case 93:
					result = Result.StatusInvalid;
					break;
				case 94:
					result = Result.Timeout;
					break;
				case 95:
					result = Result.PacketReceiveRX1Timeout;
					break;
				case 96:
					result = Result.PacketReceiveRX2Timeout;
					break;
				case 97:
					result = Result.PacketReceiveRX1Error;
					break;
				case 98:
					result = Result.PacketReceiveRX2Error;
					break;
				case 99:
					result = Result.NetworkJoinFailed;
					break;
				case 100:
					result = Result.DuplicateDownlink;
					break;
				case 101:
					result = Result.PayloadSizeInvalidforDataRate;
					break;
				case 102:
					result = Result.DownlinkPacketsLost;
					break;
				case 103:
					result = Result.AddressInvalid;
					break;
				case 104:
					result = Result.MicInvalid;
					break;
				default:
					result = Result.ErrorCodeUnknown;
					break;
			}

			return result;
		}

		private void SerialDevice_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			// we only care if got EoL character
			if (e.EventType != SerialData.WatchChar)
			{
				return;
			}

			SerialPort serialDevice = (SerialPort)sender;

			Response.Append(serialDevice.ReadExisting());

			int eolPosition;
			do
			{
				// extract a line
				eolPosition = Response.ToString().IndexOf(serialDevice.NewLine);

				if (eolPosition != -1)
				{
					string line = Response.ToString(0, eolPosition);
					Response = Response.Remove(0, eolPosition + serialDevice.NewLine.Length);
#if DIAGNOSTICS
               Debug.WriteLine($" Line :{line} ResponseExpected:{ATCommandExpectedResponse} Response:{Response}");
#endif
					int errorIndex = line.IndexOf(ErrorMarker);
					if (errorIndex != -1)
					{
						string errorNumber = line.Substring(errorIndex + ErrorMarker.Length);

						Result = ModemErrorParser(errorNumber.Trim());
						ATCommandResponseExpectedEvent.Set();
					}

					if (ATCommandExpectedResponse != string.Empty)
					{
						int successIndex = line.IndexOf(ATCommandExpectedResponse);
						if (successIndex != -1)
						{
							Result = Result.Success;
							ATCommandResponseExpectedEvent.Set();
						}
					}

					int receivedMessageIndex = line.IndexOf(ReplyMarker);
					if (receivedMessageIndex != -1)
					{
						string[] fields = line.Split("=,:".ToCharArray());

						byte port = byte.Parse(fields[1]);
						int rssi = int.Parse(fields[2]);
						int snr = int.Parse(fields[3]);
						int length = int.Parse(fields[4]);

						if (this.OnMessageConfirmation != null)
						{
							OnMessageConfirmation(rssi, snr);
						}
						if (length > 0)
						{
							string payload = fields[5];

							if (this.OnReceiveMessage != null)
							{
								OnReceiveMessage(port, rssi, snr, payload);
							}
						}
					}
				}
			}
			while (eolPosition != -1);

		}

		// Utility functions for clients for processing messages payloads to be send, ands messages payloads received.

		/// <summary>
		/// Converts an array of byes to a hexadecimal string.
		/// </summary>
		/// <param name="payloadBytes"></param>
		/// <exception cref="ArgumentNullException">The array of bytes is null.</exception>
		/// <returns>String containing hex encoded bytes</returns>
		public static string BytesToHex(byte[] payloadBytes)
		{
			if (payloadBytes == null)
			{
				throw new ArgumentNullException(nameof(payloadBytes));
			}

			StringBuilder payloadBcd = new StringBuilder(BitConverter.ToString(payloadBytes));

			payloadBcd = payloadBcd.Replace("-", "");

			return payloadBcd.ToString();
		}

		/// <summary>
		/// Converts a hexadecimal string to an array of bytes.
		/// </summary>
		/// <param name="payload">array of bytes encoded as hex</param>
		/// <exception cref="ArgumentNullException">The Hexadecimal string is null.</exception>
		/// <exception cref="ArgumentException">The Hexadecimal string is not at even number of characters.</exception>
		/// <exception cref="System.FormatException">The Hexadecimal string contains some invalid characters.</exception>
		/// <returns>Array of bytes parsed from Hexadecimal string.</returns>
		public static byte[] HexToByes(string payload)
		{
			if (payload == null)
			{
				throw new ArgumentNullException(nameof(payload));
			}
			if (payload.Length % 2 != 0)
			{
				throw new ArgumentException($"Payload invalid length must be an even number", nameof(payload));
			}

			Byte[] payloadBytes = new byte[payload.Length / 2];

			char[] chars = payload.ToCharArray();

			for (int index = 0; index < payloadBytes.Length; index++)
			{
				byte byteHigh = Convert.ToByte(chars[index * 2].ToString(), 16);
				byte byteLow = Convert.ToByte(chars[(index * 2) + 1].ToString(), 16);

				payloadBytes[index] += (byte)(byteHigh * 16);
				payloadBytes[index] += byteLow;
			}

			return payloadBytes;
		}

		/// <summary>
		/// Ensures unmanaged serial port and thread resources are released in a "responsible" manner.
		/// </summary>
		public void Dispose()
		{
			if (serialDevice != null)
			{
				serialDevice.Dispose();
				serialDevice = null;
			}
		}
	}
}
