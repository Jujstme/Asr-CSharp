What you need in order to build the project:

- .NET 9.0 preview sdk https://dotnet.microsoft.com/en-us/download/dotnet
- WASI SDK https://github.com/WebAssembly/wasi-sdk

Remember to set the WASI_SDK_PATH enviroment variable accordingly

build the project in a commend prompt by running: dotnet publish -r wasi-wasm -c Release