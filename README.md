# DurableMocks
Anti-mocking framework for Durable Functions
Uses FakeItEasy under the hood to mock TaskOrchestrationContext, and routes the implementation to the requested Activity or Orchestration
Does not verify serialization and deserialization of inputs and outsputs

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