# DurableMocks
Anti-mocking framework for Durable Functions


# How to:

1.      
    ```c
    using DurableMocks;
    ```
2.  
    ```c
    var host = hostBuilder.Build();
    _taskOrchestrationContext = host.Services.CreateDurableMock(typeof(OrchestratorOutput).Assembly);
    ```
3.
    ```c
    var input = new OrchestratorInput("test", 9);
    var res = await _taskOrchestrationContext.CallTestOrchestratorAsync(input);
    ```