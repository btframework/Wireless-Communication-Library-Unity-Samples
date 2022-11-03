# GATT Client Unity Demo
 This demo shows how to use wclGattClient in Unity to communicate with GATT enabled Bluetooth LE devices.
 
 The demo searches for nearby Bluetooth LE devices and looks for device with name set to 'test'. If you need to communication with other device change the name in code.
 
 Once device with such name (test) found the demo connects to it, read its services and characteristics. If device has readable characteristics the demo tries to read its values. If the device has inidicatable or notifiable characteristics the demo tries to subscribed to value change notifications.
 
 When stopping the demo it disconnects from the device.
