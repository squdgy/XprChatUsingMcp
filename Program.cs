using McpDotNet.Configuration;
using McpDotNet.Extensions.AI;
using McpDotNet.Protocol.Transport;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

#region ChatClient
var BRAVE_API_KEY = Environment.GetEnvironmentVariable("BRAVE_API_KEY");

// Configure tools for the agent to use
var braveSearchMcpConfig = new McpServerConfig
{
    Id = "brave-search",
    Name = "Brave Search Server",
    TransportType = TransportTypes.StdIo,
    TransportOptions = new Dictionary<string, string>
    {
        // 👇🏼 The command executed to start the MCP server
        ["command"] = "docker",
        ["arguments"] = $"run -i --rm -e BRAVE_API_KEY mcp/brave-search"
    }
};

// Configure tools for the agent to use
var xprMcpConfig = new McpServerConfig
{
    Id = "xpr-mcp",
    Name = "XPR Network MCP Server",
    TransportType = TransportTypes.StdIo,
    TransportOptions = new Dictionary<string, string>
    {
        // 👇🏼 The command executed to start the MCP server
        ["command"] = "node",
        ["arguments"] = "/Users/maurawilder/dev/xpr-mcp/dist/index.js"
    }
};

using var factory =
    LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));

// 👇🏼 Use Ollama as the chat client
var ollamaClient =
    new OllamaChatClient(new Uri("http://localhost:11434/"), "llama3.2:3b");
var client = new ChatClientBuilder(ollamaClient)
    // .UseLogging(factory) // Logging, uncomment if you need to debug
    // 👇🏼 Add function invocation to the chat client, wrapping the Ollama client
    .UseFunctionInvocation()
    .Build();

#endregion

const string ChatRoleDirective = """
                         You are a helpful assistant delivering XPR Network information 
                         relating to cryptocurrency and blockchain. You have the ability
                         to search the web for news and information, including details
                         about XPR Network. Additionally, you can 
                         interact with the XPR Network blockchain. You can retrieve token
                         balances from accounts, transfer tokens between accounts, and buy
                         or sell tokens. You can also retrieve information about which
                         tokens are supported on XPR Network.
                         """;

// 👇🏼 Get an MCP session scope used to get the MCP tools
var sessionScope = await McpSessionScope.CreateAsync([braveSearchMcpConfig, xprMcpConfig]);

var prompt = readPrompt();
while (prompt != "exit")
{
    IList<ChatMessage> messages =
    [
        new(ChatRole.System, ChatRoleDirective),
        new(ChatRole.User, prompt)
    ];

    // 👇🏼 Pass the messages and the MCP tools to the chat client
    var response =
        await client.GetResponseAsync(
            messages,
            new ChatOptions { Tools = sessionScope.Tools });

    Console.WriteLine($"===\n{response}\n===");
    prompt = readPrompt();
    if (prompt == "new")
    {
        sessionScope = await McpSessionScope.CreateAsync([braveSearchMcpConfig, xprMcpConfig]);
    }
}

static string? readPrompt()
{
    Console.Write("Ask me about XPR accounts or to act on behalf of your XPR account. Type `exit` to exit > ");
    return Console.ReadLine();
}
