# WIP


# Ipc.Connect
A cross-platform shared memory Client/Server Implementation

The main idea of this library is to have an easy to use IPC (Interprocess Communication) library which you can use as other client / server library.

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

For more example you can also check the Tests

# Performance

The system is designed for high performance. This means the comunication has very low overhead and high bandwidth.

|                                          Method | Time (ms) | Comment                 |
|------------------------------------------------ |----------:|------------------------:|
| Execute 100000 Small Request                    |    `2400` | For one Request (24 ns) |
| Send 10 GB Data                                 |    `1700` | Bandwidth 47 Gbit/s     |
| Retrieve 10 GB Data                             |    `1700` | Bandwidth 47 Gbit/s     |

Times are measured on an Intel i7-9750H (2.6 GHz), 16 GB Memory and Windows 11


# Dependencies

* [Dotnet 6](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
* [Cloudtoid.Interprocess](https://www.nuget.org/packages/Cloudtoid.Interprocess/)





