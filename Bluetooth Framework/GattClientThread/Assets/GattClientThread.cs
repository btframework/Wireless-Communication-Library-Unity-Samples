using System;
using System.Threading;
using UnityEngine;

using wclCommon;
using wclBluetooth;

public class GattClientThread : MonoBehaviour
{
    private wclBluetoothManager Manager;
    private wclGattClient Client;
    private wclBluetoothLeBeaconWatcher Watcher;
    private Thread CommThread;
    private ManualResetEvent TerminateEvent;

    private static Guid SERIAL_SERVICE_UUID = new Guid("{6E40FEC1-B5A3-F393-E0A9-E50E24DCCA9E}");
    private static Guid RX_CHARACTERISTIC_UUID = new Guid("{6E40FEC2-B5A3-F393-E0A9-E50E24DCCA9E}");
    private static Guid TX_CHARACTERISTIC_UUID = new Guid("{6E40FEC3-B5A3-F393-E0A9-E50E24DCCA9E}");

    private void CommunicationThread()
    {
        wclGattUuid Uuid = new wclGattUuid();
        Uuid.IsShortUuid = false;

        Debug.Log("Find service");
        Uuid.LongUuid = SERIAL_SERVICE_UUID;
        wclGattService? Service = null;
        Int32 Res = Client.FindService(Uuid, out Service);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Debug.Log("Unable to find service" + Res.ToString("X8"));
        else
        {
            Debug.Log("Find TX characteristic");
            Uuid.LongUuid = TX_CHARACTERISTIC_UUID;
            wclGattCharacteristic? TxChar = null;
            Res = Client.FindCharacteristic(Service.Value, Uuid, out TxChar);
            if (Res != wclErrors.WCL_E_SUCCESS)
                Debug.Log("TX characteristic not found" + Res.ToString("X8"));
            else
            {
                Debug.Log("Find RX characteristic");
                Uuid.LongUuid = RX_CHARACTERISTIC_UUID;
                wclGattCharacteristic? RxChar = null;
                Res = Client.FindCharacteristic(Service.Value, Uuid, out RxChar);
                if (Res != wclErrors.WCL_E_SUCCESS)
                    Debug.Log("RX charaterisitc not found" + Res.ToString("X8"));
                else
                {
                    Debug.Log("Subscribe to notifications");
                    Res = Client.SubscribeForNotifications(RxChar.Value);
                    if (Res != wclErrors.WCL_E_SUCCESS)
                        Debug.Log("Subscribe failed" + Res.ToString("X8"));
                    else
                    {
                        Debug.Log("Connection completed");

                        UInt32 Counter = 0;
                        while (!TerminateEvent.WaitOne(1000))
                        {
                            if (Client.State == wclCommunication.wclClientState.csConnected)
                            {
                                Byte[] Val = BitConverter.GetBytes(Counter);
                                Res = Client.WriteCharacteristicValue(TxChar.Value, Val);
                                if (Res != wclErrors.WCL_E_SUCCESS)
                                    Debug.Log("Write failed: 0x" + Res.ToString("X8"));
                                Counter++;
                            }
                        }
                    }
                }
            }
        }

        if (Res != wclErrors.WCL_E_SUCCESS)
            Debug.Log("Wrong device");

        Client.Disconnect();
    }

    // Start is called before the first frame update
    void Start()
    {
        wclMessageBroadcaster.SetSyncMethod(wclMessageSynchronizationKind.skThread);

        CommThread = null;
        TerminateEvent = new ManualResetEvent(false);

        Manager = new wclBluetoothManager();
        Manager.AfterOpen += Manager_AfterOpen;
        Manager.OnClosed += Manager_OnClosed;

        Watcher = new wclBluetoothLeBeaconWatcher();
        Watcher.OnStarted += Watcher_OnStarted;
        Watcher.OnStopped += Watcher_OnStopped;
        Watcher.OnAdvertisementUuidFrame += Watcher_OnAdvertisementUuidFrame;

        Client = new wclGattClient();
        Client.OnDisconnect += Client_OnDisconnect;
        Client.OnConnect += Client_OnConnect;
        Client.OnCharacteristicChanged += Client_OnCharacteristicChanged;

        Debug.Log("Open Bluetooth Manager");
        Int32 Res = Manager.Open();
        if (Res != wclErrors.WCL_E_SUCCESS)
            Debug.Log("Bluetooth Manager open failed: 0x" + Res.ToString("X8"));
        else
        {
            Debug.Log("Try to get Bluetooth LE radio");
            wclBluetoothRadio Radio;
            Res = Manager.GetLeRadio(out Radio);
            if (Res != wclErrors.WCL_E_SUCCESS)
                Debug.Log("Get LE radio failed: 0x" + Res.ToString("X8"));
            else
            {
                Debug.Log("Start Beacon Watcher");
                Res = Watcher.Start(Radio);
                if (Res != wclErrors.WCL_E_SUCCESS)
                    Debug.Log("Beacon Watcher start failed: 0x" + Res.ToString("X8"));
            }
        }
    }

    private void Client_OnCharacteristicChanged(object Sender, ushort Handle, byte[] Value)
    {
        UInt32 Counter = BitConverter.ToUInt32(Value, 0);
        Debug.Log("Received: " + Counter.ToString());
    }

    private void Client_OnConnect(object Sender, int Error)
    {
        if (Error != wclErrors.WCL_E_SUCCESS)
            Debug.Log("Connect failed" + Error.ToString("X8"));
        else
        {
            Debug.Log("Connected");

            CommThread = new Thread(CommunicationThread);
            CommThread.Start();
        }
    }

    private void Client_OnDisconnect(object Sender, int Reason)
    {
        Debug.Log("Client disconnected" + Reason.ToString("X8"));
    }

    private void Watcher_OnAdvertisementUuidFrame(object Sender, long Address,
        long Timestamp, sbyte Rssi, Guid Uuid)
    {
        // Additionally you can filter device by MAC or somehow else.
        if (Uuid == SERIAL_SERVICE_UUID)
        {
            Debug.Log("Device found: " + Address.ToString("X12"));

            // Get radio here! After stop it will not be available!
            wclBluetoothRadio Radio = Watcher.Radio;
            Watcher.Stop();

            Debug.Log("Try to connect");
            Client.Address = Address;
            Int32 Res = Client.Connect(Radio);
            if (Res != wclErrors.WCL_E_SUCCESS)
                Debug.Log("Start connecting failed" + Res.ToString("X8"));
        }
    }

    private void Watcher_OnStopped(object sender, EventArgs e)
    {
        Debug.Log("Beacon Watcher stopped");
    }

    private void Watcher_OnStarted(object sender, EventArgs e)
    {
        Debug.Log("Beacon Watcher started");
    }

    private void Manager_OnClosed(object sender, EventArgs e)
    {
        Debug.Log("Bluetooth Manager closed");
    }

    private void Manager_AfterOpen(object sender, EventArgs e)
    {
        Debug.Log("Bluetooth Manager opened");
    }

    void Update()
    {

    }

    private void OnDestroy()
    {
        Debug.Log("Closing application");

        if (CommThread != null)
        {
            TerminateEvent.Set();
            CommThread.Join();
            CommThread = null;
        }
        TerminateEvent.Close();

        Manager.Close();

        Debug.Log("Finished");
    }
}
