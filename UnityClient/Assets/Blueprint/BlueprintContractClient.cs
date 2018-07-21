using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Loom.Nethereum.ABI.FunctionEncoding.Attributes;
using Loom.Client;
using UnityEngine;

/// <summary>
/// Abstracts interaction with the Blueprint contract.
/// </summary>
public class BlueprintContractClient : IDisposable {
    public delegate void ValueChangedEventHandler(string key, string value);
    public delegate void ValueRemovedEventHandler(string key);

    public event ValueChangedEventHandler ValueChanged;
    public event ValueRemovedEventHandler ValueRemoved;

    private readonly string backendHost;
    private readonly string abi;
    private readonly byte[] privateKey;
    private readonly byte[] publicKey;
    private readonly ILogger logger;
    private readonly Address address;
    private readonly ConcurrentQueue<Action> eventActionsQueue = new ConcurrentQueue<Action>();

    private DAppChainClient client;
    private EvmContract contract;
    private IRpcClient reader;
    private IRpcClient writer;

    public BlueprintContractClient(string backendHost, string abi, byte[] privateKey, byte[] publicKey, ILogger logger)
    {
        this.backendHost = backendHost;
        this.abi = abi;
        this.privateKey = privateKey;
        this.publicKey = publicKey;
        this.logger = logger;
        this.address = Address.FromPublicKey(this.publicKey);
    }

    /// <summary>
    /// Dispatches queued events. Clears the queue afterwards.
    /// </summary>
    public void DispatchQueuedEvents() {
        if (this.eventActionsQueue.IsEmpty)
            return;

        Action eventAction;
        while (this.eventActionsQueue.TryDequeue(out eventAction)) {
            eventAction();
        }
    }

    /// <summary>
    /// Establishes initial connection with the contract.
    /// </summary>
    public async Task ConnectToContract()
    {
        if (this.contract == null)
        {
            this.contract = await GetContract();
            this.contract.EventReceived += EventReceivedHandler;
        }
    }

    #region Contract public functions

    /* Those methods mirror the functions of the Solidity contract,
       and are made for convenience. */

    public async Task Store(string key, string value) {
        await ConnectToContract();
        await this.contract.CallAsync("store", key, value);
    }

    public async Task<string> Load(string key) {
        await ConnectToContract();
        return await this.contract.StaticCallSimpleTypeOutputAsync<string>("load", key);
    }

    public async Task Remove(string key) {
        await ConnectToContract();
        await this.contract.CallAsync("remove", key);
    }

    #endregion

    public void Dispose() {
        if (this.contract != null) {
            this.contract.EventReceived -= EventReceivedHandler;
        }

        this.client?.Dispose();
        this.reader?.Dispose();
        this.writer?.Dispose();
    }

    /// <summary>
    /// Connects to the DAppChain and returns an instance of a contract.
    /// </summary>
    /// <returns></returns>
    private async Task<EvmContract> GetContract()
    {
        this.writer = RpcClientFactory.Configure()
            .WithLogger(this.logger)
            .WithWebSocket("ws://" + this.backendHost + ":46657/websocket")
            .Create();

        this.reader = RpcClientFactory.Configure()
            .WithLogger(this.logger)
            .WithWebSocket("ws://" + this.backendHost + ":9999/queryws")
            .Create();

        this.client = new DAppChainClient(this.writer, this.reader)
            { Logger = this.logger };

        // required middleware
        this.client.TxMiddleware = new TxMiddleware(new ITxMiddlewareHandler[]
        {
            new NonceTxMiddleware(this.publicKey, this.client),
            new SignedTxMiddleware(this.privateKey)
        });

        // If 'truffle deploy' was used to deploy the contract,
        // you will have to use the contract address directly
        // instead of resolving it from contract name
        Address contractAddr = await this.client.ResolveContractAddressAsync("Blueprint");
        EvmContract evmContract = new EvmContract(this.client, contractAddr, this.address, this.abi);

        return evmContract;
    }

    /// <summary>
    /// This method receives raw EVM events from the DAppChain.
    /// Add decoding of your own events here.
    /// </summary>
    /// <remarks>
    /// Events are not dispatched immediately.
    /// Instead, they are queued to allow dispatching them when it is appropriate.
    /// </remarks>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void EventReceivedHandler(object sender, EvmChainEventArgs e) {
        switch (e.EventName) {
            case "ValueChanged": {
                ValueChangedEventData eventDto = e.DecodeEventDto<ValueChangedEventData>();
                this.eventActionsQueue.Enqueue(() => this.ValueChanged?.Invoke(eventDto.Key, eventDto.Value));
                break;
            }
            case "ValueRemoved": {
                ValueRemovedEventData eventDto = e.DecodeEventDto<ValueRemovedEventData>();
                this.eventActionsQueue.Enqueue(() => this.ValueRemoved?.Invoke(eventDto.Key));
                break;
            }
            default:
                throw new ArgumentOutOfRangeException($"Unknown event {e.EventName}");
        }
    }

    #region Event Data Transfer Objects

    private class ValueChangedEventData
    {
        [Parameter("string")]
        public string Key { get; set; }

        [Parameter("string")]
        public string Value { get; set; }
    }

    private class ValueRemovedEventData
    {
        [Parameter("string")]
        public string Key { get; set; }
    }

    #endregion
}
