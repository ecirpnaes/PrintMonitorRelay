# PrintMonitorRelay
Windows Service project that listens to low level print sppoler events for specific print jobs. When a matching print job name is found (job sent to a specific printer), 
the service initiates a call to an Internet relay switch. The switch sounds an alert at a warehouse to notify staff. 
