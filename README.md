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
  IpcServer server = new IpcServer(channelFactory);
  server.Listen("MyChannelName", new IpcServerRequestHandlerDelegate((reqestStream) => {
    // handle the request here
  
    // Return Result
    return new IpcDataEmpty(); 
  }));
  
```

## Connect to the Server

```csharp
  // Create a new ChannelFactory. The Settings of this Factory should be same on Client and Server 
  IpcChannelFactory channelFactory = new IpcChannelFactory();
  
  // Create the Client
  IpcClient client = new IpcClient(channelFactory, "MyChannelName")
  using (Stream responseStream = client.Send(new IpcDataBytes(Encoding.UTF8.GetBytes("Hello World")), TimeSpan.FromSeconds(2)))
  {
      // Process the response
  }
  
```


# Performance

The system is designed for high performance. This means the comunication has very low overhead and high bandwidth.

## Many Request Performance





