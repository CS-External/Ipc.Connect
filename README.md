# WIP


# Ipc.Connect
A cross-platform shared memory Client/Server Implementation

The main idea of this library is to have an easy to use IPC (Inter Process Communication) library which you can use as other client / server library.

# How to use

## Create a Server

```csharp
  // Create a new ChannelFactory. The Settings of this Factory should be same on Client and Server 
  IpcChannelFactory channelFactory = new IpcChannelFactory();
  
  // Create the Server
  IpcServer server = new IpcServer(m_ChannelFactory);
  server.listen("MyChannelName", new IpcServerRequestHandlerDelegate((reqestStream) => {
    // handle the request here
  
    // Return Result
    return new IpcDataEmpty(); 
  }));
  
```
