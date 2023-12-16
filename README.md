# RedSharpNano - A Tiny Redis Client

## Introduction

RedSharpNano is a minimalistic Redis client for the RESP2 protocol, showcasing that a fully functional Redis client can be achieved in approximately 100 lines of code C# code. It's designed to emphasize simplicity, not best practices or optimal implementations.

## Features

- **Lightweight**: With only about 100 lines of code, RedSharpNano is extremely lightweight and easy to understand.
- **RESP2 Protocol Support**: It should be capable of supporting almost all RESP2 protocol functionalities.
- **Pipeline Support**: Offers a simple pipeline mechanism to batch Redis commands for efficiency.
- **Versatile Command Input**: Offers two ways to input commands:
  - Use the structured command format: `Call("cmd", "arg", "arg")`.
  - Or use a single string command: `Call("CMD arg arg")`. This format allows setting keys and values with spaces by enclosing them in double quotes, e.g., `Call("SET \"my key\" \"some value\"")`.
- **Simple and Easy-to-Use API**: The API is straightforward, making it easy to integrate into projects for learning or prototyping purposes.
- **Open Source**: Distributed under the MIT License, free to use, modify, and distribute.

## Installation

RedSharpNano can be integrated into your project in two ways:

1. **Direct File Inclusion**: Include the Resp2Client.cs file in your project.
2. **Library Project**: Use the library project provided in the solution for a more structured approach.

## Usage

To run the demo project or execute the tests, you need .Net 8 SDK installed and a local Redis server, you can use Docker to run a Redis instance.

- This command pulls the latest Redis image and runs it in a detached mode with the default Redis port (6379) mapped to your local machine.

```bash
docker run -d --name redis-stack-server -p 6379:6379 redis/redis-stack-server:latest
```

- To run the demo project use your IDE or execute the following command

```bash
dotnet run --project .\src\RedSharpNano.Demo\
```

- To run the tests use your IDE or execute the following command

```bash
dotnet test
```

To use RedSharpNano, follow these steps:

1. **Initialization**: Create an instance of the `Resp2Client` class.

   ```csharp
   var client = new Resp2Client("localhost", 6379);
   ```

2. **Executing Commands**: Use the Call method to execute Redis commands.

   ```csharp
   var result = client.Call("SET", "key", "value");
   ```

3. **Using Pipelines**: Batch commands using the Pipeline method for efficient execution.

   ```csharp
    var results = client.Pipeline(c =>
    {
        c.Call("SET", "batchkey1", "value1");
        c.Call("SET", "batchkey2", "value2");
        c.Call("GET", "batchkey1");
        c.Call("GET", "batchkey2");
    });

    foreach (var result in results)
    {
        Console.WriteLine(result);
    }
   ```

4. **Handling Null or Non-existent Keys**

   ```csharp
    var client = new Resp2Client();
    var value = client.Call("GET", "nonexistentkey");
    Console.WriteLine(value ?? "Key does not exist");
   ```

## Design Philosophy

- **Simplicity over Typing**: The project is intentionally untyped to keep it tiny and straightforward, aligning with the goal of demonstrating simplicity over adherence to best practices.
- **Educational Purpose**: Ideal for simple applications or those looking to understand the basics of a Redis client implementation without the complexity of a full-featured client.

## Acknowledgments

This project is inspired by [TinyRedisClient](https://github.com/ptrofimov/tinyredisclient). Special thanks to the original author for providing a great starting point.

## Disclaimer

- RedSharpNano is not designed following the best practices for production-grade Redis clients.
- The client is intended for educational purposes or as a starting point for more complex implementations.
- May not handle all edge cases or advanced features of Redis.

## Contributing

Contributions are welcome, especially for educational purposes or to extend the project's scope in line with its core philosophy of simplicity.
