using System;
using System.Collections.Generic;
using UnityEngine;

using wclCommon;
using wclBluetooth;
using System.Security.Cryptography;
using UnityEditor.PackageManager;
using System.Text;

public class RfCommClient : MonoBehaviour
{
    private wclBluetoothManager FManager;
    private wclRfCommClient FClient;
    private List<Int64> FDevices;

    // Start is called before the first frame update
    void Start()
    {
        FManager = new wclBluetoothManager();
        FManager.AfterOpen += FManager_AfterOpen;
        FManager.BeforeClose += FManager_BeforeClose;
        FManager.OnClosed += FManager_OnClosed;
        FManager.OnDiscoveringStarted += FManager_OnDiscoveringStarted;
        FManager.OnDeviceFound += FManager_OnDeviceFound;
        FManager.OnDiscoveringCompleted += FManager_OnDiscoveringCompleted;
        FManager.OnAuthenticationCompleted += FManager_OnAuthenticationCompleted;
        FManager.OnConfirm += FManager_OnConfirm;
        FManager.OnNumericComparison += FManager_OnNumericComparison;
        FManager.OnPasskeyNotification += FManager_OnPasskeyNotification;
        FManager.OnPasskeyRequest += FManager_OnPasskeyRequest;
        FManager.OnPinRequest += FManager_OnPinRequest;

        FClient = new wclRfCommClient();
        FClient.OnDisconnect += FClient_OnDisconnect;
        FClient.OnConnect += FClient_OnConnect;
        FClient.OnData += FClient_OnData;

        FDevices = new List<Int64>();

        Debug.Log("Try to open Bluetooth Manager");
        Int32 Res = FManager.Open();
        if (Res != wclErrors.WCL_E_SUCCESS)
            Debug.Log("Open Bluetooth Manager failed: 0x" + Res.ToString("X8"));
    }

    private void FClient_OnData(object Sender, Byte[] Data)
    {
        if (Data != null && Data.Length > 0)
            Debug.Log("Received: " + Encoding.ASCII.GetString(Data));
    }

    private void FClient_OnConnect(object Sender, Int32 Error)
    {
        if (Error != wclErrors.WCL_E_SUCCESS)
            Debug.Log("Connect failed: 0x" + Error.ToString("X8"));
        else
            Debug.Log("Connected");
    }

    private void FClient_OnDisconnect(object Sender, Int32 Reason)
    {
        Debug.Log("Client disconnected by reason: 0x" + Reason.ToString("X8"));
    }

    private void FManager_OnPinRequest(object Sender, wclBluetoothRadio Radio, Int64 Address,
        out String Pin)
    {
        Debug.Log("Enter 0000 as PIN on the device");
        Pin = "0000";
    }

    private void FManager_OnPasskeyRequest(object Sender, wclBluetoothRadio Radio, Int64 Address,
        out UInt32 Passkey)
    {
        Debug.Log("Passkey requested. Default 0000 is used. Change it here is device requires other.");
        Passkey = 0000;
    }

    private void FManager_OnPasskeyNotification(object Sender, wclBluetoothRadio Radio, Int64 Address,
        UInt32 Passkey)
    {
        Debug.Log("Type the passkey on device: " + Passkey.ToString());
    }

    private void FManager_OnNumericComparison(object Sender, wclBluetoothRadio Radio, Int64 Address,
        UInt32 Number, out Boolean Confirm)
    {
        Debug.Log("Numeric comparison pairing. Accept");
        Confirm = true;
    }

    private void FManager_OnConfirm(object Sender, wclBluetoothRadio Radio, Int64 Address,
        out Boolean Confirm)
    {
        Debug.Log("Just Works pairing. Accept");
        Confirm = true;
    }

    private void FManager_OnAuthenticationCompleted(object Sender, wclBluetoothRadio Radio,
        Int64 Address, Int32 Error)
    {
        if (Error != wclErrors.WCL_E_SUCCESS)
            Debug.Log("Pair with device " + Address.ToString("X12") + " failed: 0x" + Error.ToString("X12"));
        else
            Debug.Log("Device " + Address.ToString("X12") + " was paired");
    }

    private void FManager_OnDiscoveringCompleted(object Sender, wclBluetoothRadio Radio, Int32 Error)
    {
        Debug.Log("Discovering completed with result: 0x" + Error.ToString("X8"));

        if (FDevices.Count == 0)
            Debug.Log("No devices were found");
        else
        {
            Int64 Device = 0;
            Debug.Log("Try to resolve devices name");
            foreach (Int64 Address in FDevices)
            {
                String Name;
                Int32 Res = Radio.GetRemoteName(Address, out Name);
                if (Res != wclErrors.WCL_E_SUCCESS)
                    Debug.Log("Device [" + Address.ToString("X12") + "]: get name failed: 0x" + Res.ToString("X8"));
                else
                {
                    Debug.Log("Device [" + Address.ToString("X12") + "]: " + Name);
                    Debug.Log("Looking for HUAWEI P smart");
                    if (Name == "HUAWEI P smart")
                    {
                        Device = Address;
                        break;
                    }
                }
            }

            if (Device == 0)
                Debug.Log("Required device not found");
            else
            {
                Debug.Log("Required device found");

                Debug.Log("Try to connect");
                FClient.Address = Device;
                FClient.Service = wclUUIDs.SerialPortServiceClass_UUID;
                Int32 Res = FClient.Connect(Radio);
                if (Res != wclErrors.WCL_E_SUCCESS)
                    Debug.Log("Start connect failed: 0x" + Res.ToString("X8"));
            }
        }
    }

    private void FManager_OnDeviceFound(object Sender, wclBluetoothRadio Radio, Int64 Address)
    {
        Debug.Log("Device found: " + Address.ToString("X12"));
        FDevices.Add(Address);
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

        Debug.Log("Try to get working Bluetooth Radio");
        wclBluetoothRadio Radio;
        Int32 Res = FManager.GetClassicRadio(out Radio);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Debug.Log("Get working radio failed: 0x" + Res.ToString("X8"));
        else
        {
            Debug.Log("Try to start discovering");
            Res = Radio.Discover(10, wclBluetoothDiscoverKind.dkClassic);
            if (Res != wclErrors.WCL_E_SUCCESS)
                Debug.Log("Start discovering failed: 0x" + Res.ToString("X8"));
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDestroy()
    {
        Debug.Log("Application is closing");

        if (FClient != null)
        {
            Debug.Log("Try to disconnect");
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
