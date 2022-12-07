using UnityEngine;

using wclCommon;
using wclBluetooth;
using System;
using UnityEngine.UIElements;

public class BeaconWatcher : MonoBehaviour
{
    private wclBluetoothManager FManager;
    private wclBluetoothLeBeaconWatcher FWatcher;

    // Start is called before the first frame update
    void Start()
    {
        FManager = new wclBluetoothManager();
        FManager.AfterOpen += FManager_AfterOpen;
        FManager.BeforeClose += FManager_BeforeClose;
        FManager.OnClosed += FManager_OnClosed;

        FWatcher = new wclBluetoothLeBeaconWatcher();
        FWatcher.OnStarted += FWatcher_OnStarted;
        FWatcher.OnStopped += FWatcher_OnStopped;
        FWatcher.OnAdvertisementFrameInformation += FWatcher_OnAdvertisementFrameInformation;
        FWatcher.OnAdvertisementUuidFrame += FWatcher_OnAdvertisementUuidFrame;

        Debug.Log("Try to open Bluetooth Manager");
        Int32 Res = FManager.Open();
        if (Res != wclErrors.WCL_E_SUCCESS)
            Debug.Log("Open Bluetooth Manager failed: 0x" + Res.ToString("X8"));
    }

    private void FWatcher_OnStopped(object sender, EventArgs e)
    {
        Debug.Log("Beacon watcher stopped");
    }

    private void FWatcher_OnStarted(object sender, EventArgs e)
    {
        Debug.Log("Beacon watcher started");
    }

    private void FWatcher_OnAdvertisementUuidFrame(object Sender, Int64 Address, Int64 Timestamp,
        SByte Rssi, Guid Uuid)
    {
        Debug.Log("UUID advertisement received: " + Uuid.ToString());
    }

    private void FWatcher_OnAdvertisementFrameInformation(object Sender, Int64 Address, Int64 Timestamp, SByte Rssi,
        String Name, wclBluetoothLeAdvertisementType PacketType, wclBluetoothLeAdvertisementFlag Flags)
    {
        Debug.Log("Advertisement frame recevied from " + Address.ToString("X12"));
        // Uncomment the lines below if you want to stop search when required device found.
        /*const Int64 DEVICE_ADDRESS = 0x30E283AD9928;
        if (Address == DEVICE_ADDRESS)
        {
            Debug.Log("Device found. Stopping");
            FWatcher.Stop();
        }*/
    }

    private void FManager_OnClosed(object sender, EventArgs e)
    {
        Debug.Log("Bluetooth manager is closed");
    }

    private void FManager_BeforeClose(object sender, EventArgs e)
    {
        Debug.Log("Bluetooth Manager is closing");
    }

    private void FManager_AfterOpen(object sender, EventArgs e)
    {
        Debug.Log("Bluetooth Manager opened");

        Debug.Log("Try to get Bluetooth LE working radio");
        wclBluetoothRadio Radio;
        Int32 Res = FManager.GetLeRadio(out Radio);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Debug.Log("Get working LE radio failed: 0x" + Res.ToString("X8"));
        else
        {
            Debug.Log("Try to start Beacon Watcher");
            Res = FWatcher.Start(Radio);
            if (Res != wclErrors.WCL_E_SUCCESS)
                Debug.Log("Start Beacon Watcher failed: " + Res.ToString("X8"));
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDestroy()
    {
        Debug.Log("Closing the application.");

        if (FWatcher != null)
        {
            Debug.Log("Try to stop Beacon Watcher");
            Int32 Res = FWatcher.Stop();
            if (Res != wclErrors.WCL_E_SUCCESS)
                Debug.Log("Stop Beacon Watcher failed: 0x" + Res.ToString("X8"));
            FWatcher = null;
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
