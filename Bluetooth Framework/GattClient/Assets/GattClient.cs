using System;
using System.Collections.Generic;
using UnityEngine;

using wclCommon;
using wclBluetooth;

public class GattClient : MonoBehaviour
{
    private wclBluetoothManager FManager;
    private wclGattClient FClient;
    private List<Int64> FDevices;

    void Start()
    {
        FManager = new wclBluetoothManager();
        FManager.AfterOpen += FManager_AfterOpen;
        FManager.BeforeClose += FManager_BeforeClose;
        FManager.OnClosed += FManager_OnClosed;
        FManager.OnDiscoveringStarted += FManager_OnDiscoveringStarted;
        FManager.OnDiscoveringCompleted += FManager_OnDiscoveringCompleted;
        FManager.OnDeviceFound += FManager_OnDeviceFound;

        FClient = new wclGattClient();
        FClient.OnConnect += FClient_OnConnect;
        FClient.OnDisconnect += FClient_OnDisconnect;
        FClient.OnCharacteristicChanged += FClient_OnCharacteristicChanged;

        FDevices = new List<Int64>();

        Debug.Log("Try to open Bluetooth Manager");
        Int32 Res = FManager.Open();
        if (Res != wclErrors.WCL_E_SUCCESS)
            Debug.Log("Bluetooth Manager open failed: 0x" + Res.ToString("X8"));
    }

    private void FClient_OnCharacteristicChanged(object Sender, UInt16 Handle, Byte[] Value)
    {
        Debug.Log("Characteristic [" + Handle.ToString("X4") + "]: value changed");
        DumpValue(Value);
    }

    private void FClient_OnDisconnect(object Sender, Int32 Reason)
    {
        Debug.Log("Disconnected from device with disconnec reason: 0x" + Reason.ToString("X8"));
    }

    private void DumpValue(Byte[] Value)
    {
        if (Value == null || Value.Length == 0)
            Debug.Log("      Value is empty");
        else
        {
            String s = "";
            foreach (Byte b in Value)
                s = s + b.ToString("X2");
            Debug.Log("      Value: " + s);
        }
    }

    private void FClient_OnConnect(object Sender, Int32 Error)
    {
        if (Error != wclErrors.WCL_E_SUCCESS)
            Debug.Log("Connect failed: 0x" + Error.ToString("X8"));
        else
        {
            Debug.Log("Connected");

            Debug.Log("Try to read services");
            wclGattService[] Services;
            Int32 Res = FClient.ReadServices(wclGattOperationFlag.goNone, out Services);
            if (Res != wclErrors.WCL_E_SUCCESS)
                Debug.Log("Read services failed: 0x" + Res.ToString("X8"));
            else
            {
                if (Services == null || Services.Length == 0)
                    Debug.Log("No services found");
                else
                {
                    foreach (wclGattService Service in Services)
                    {
                        String Uuid;
                        if (Service.Uuid.IsShortUuid)
                            Uuid = Service.Uuid.ShortUuid.ToString("X4");
                        else
                            Uuid = Service.Uuid.LongUuid.ToString();
                        Debug.Log("Service: " + Uuid);

                        Debug.Log("  Try to read characteristics");
                        wclGattCharacteristic[] Characteristics;
                        Res = FClient.ReadCharacteristics(Service, wclGattOperationFlag.goNone, out Characteristics);
                        if (Res != wclErrors.WCL_E_SUCCESS)
                            Debug.Log("  Read characteristics failed: 0x" + Res.ToString("X8"));
                        else
                        {
                            if (Characteristics == null || Characteristics.Length == 0)
                                Debug.Log("  Characteristics not found");
                            else
                            {
                                foreach (wclGattCharacteristic Characteristic in Characteristics)
                                {
                                    if (Characteristic.Uuid.IsShortUuid)
                                        Uuid = Characteristic.Uuid.ShortUuid.ToString("X4");
                                    else
                                        Uuid = Characteristic.Uuid.LongUuid.ToString();
                                    Debug.Log("  Characteristic: " + Uuid);

                                    if (Characteristic.IsWritableWithoutResponse || Characteristic.IsWritable || Characteristic.IsSignedWritable)
                                        Debug.Log("    Characteristic is writable");
                                    if (Characteristic.IsBroadcastable)
                                        Debug.Log("    Characteristic is broadcastable");

                                    if (Characteristic.IsReadable)
                                    {
                                        Debug.Log("    Characteristic if readable");
                                        Debug.Log("    Try to read its value");
                                        Byte[] Value;
                                        Res = FClient.ReadCharacteristicValue(Characteristic, wclGattOperationFlag.goNone, out Value);
                                        if (Res != wclErrors.WCL_E_SUCCESS)
                                            Debug.Log("      Read value failed: 0x" + Res.ToString("X8"));
                                        else
                                            DumpValue(Value);
                                    }

                                    if (Characteristic.IsNotifiable || Characteristic.IsIndicatable)
                                    {
                                        Debug.Log("    Characteristic is notifiable or indicatable");
                                        wclGattCharacteristic Char = Characteristic;
                                        if (Char.IsNotifiable && Char.IsIndicatable)
                                            Char.IsIndicatable = false;
                                        Debug.Log("      Try to subscribe");
                                        Res = FClient.SubscribeForNotifications(Char);
                                        if (Res != wclErrors.WCL_E_SUCCESS)
                                            Debug.Log("      Subscribe failed: 0x" + Res.ToString("X8"));
                                        else
                                            Debug.Log("      Subsribed");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void FManager_OnDeviceFound(object Sender, wclBluetoothRadio Radio, Int64 Address)
    {
        Debug.Log("Device found: " + Address.ToString("X12"));
        FDevices.Add(Address);
    }

    private void FManager_OnDiscoveringCompleted(object Sender, wclBluetoothRadio Radio, Int32 Error)
    {
        Debug.Log("Discovering completed with result: 0x" + Error.ToString("X8"));
        if (FDevices.Count == 0)
            Debug.Log("No devices found");
        else
        {
            Debug.Log("Found " + FDevices.Count.ToString() + " devices");
            Debug.Log("Try to resolve devices name. Looking for device with 'test' as its name");
            Int64 Device = 0;
            foreach (Int64 Address in FDevices)
            {
                String Name;
                Int32 Res = Radio.GetRemoteName(Address, out Name);
                if (Res != wclErrors.WCL_E_SUCCESS)
                    Debug.Log("  Device [" + Address.ToString("X12") + "]: get name failed: 0x" + Res.ToString("X8"));
                else
                {
                    Debug.Log("  Device [" + Address.ToString("X12") + "]: " + Name);
                    if (Name == "test")
                    {
                        Device = Address;
                        break;
                    }
                }
            }

            if (Device == 0)
                Debug.Log("No required device found");
            else
            {
                Debug.Log("Required device found.");
                Debug.Log("Try to connect");

                FClient.Address = Device;
                Int32 Res = FClient.Connect(Radio);
                if (Res != wclErrors.WCL_E_SUCCESS)
                    Debug.Log("Connect to selected device failed: 0x" + Res.ToString("X8"));
            }
        }
    }

    private void FManager_OnDiscoveringStarted(object Sender, wclBluetoothRadio Radio)
    {
        Debug.Log("Discovering started");
        FDevices.Clear();
    }

    private void FManager_OnClosed(object sender, EventArgs e)
    {
        Debug.Log("Bluetooth Manager closed");
    }

    private void FManager_BeforeClose(object sender, EventArgs e)
    {
        Debug.Log("Bluetooth Manager is closing");
    }

    private void FManager_AfterOpen(object sender, EventArgs e)
    {
        Debug.Log("Bluetooth Manager opened");

        Debug.Log("Try to get working Bluetooth LE radio");
        wclBluetoothRadio Radio;
        Int32 Res = FManager.GetLeRadio(out Radio);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Debug.Log("Unable to get working Bluetooth LE radio: 0x" + Res.ToString("X8"));
        else
        {
            Debug.Log("Try to start discovering");
            Res = Radio.Discover(10, wclBluetoothDiscoverKind.dkBle);
            if (Res != wclErrors.WCL_E_SUCCESS)
                Debug.Log("Unable to start discovering: 0x" + Res.ToString("X8"));
        }
    }

    void Update()
    {
        
    }

    private void OnDestroy()
    {
        Debug.Log("Closing application");

        if (FClient != null)
        {
            Debug.Log("Try to disconnect GATT client.");
            Int32 Res = FClient.Disconnect();
            if (Res != wclErrors.WCL_E_SUCCESS)
                Debug.Log("Disconnect failed: 0x" + Res.ToString("X8"));
            FClient = null;
        }

        if (FManager != null)
        {
            Debug.Log("Try to close Bluetooth Manager");
            Int32 Res = FManager.Close();
            if (Res != wclErrors.WCL_E_SUCCESS)
                Debug.Log("Close Bluetooth Manager failed: 0x" + Res.ToString("X8"));
            FManager = null;
        }
    }
}
