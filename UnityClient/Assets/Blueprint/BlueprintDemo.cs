using Loom.Client;
using UnityEngine;
using UnityEngine.UI;

public class BlueprintDemo : MonoBehaviour {
    public string BackendHost = "127.0.0.1";
    public TextAsset ContractAbi;

    public InputField KeyText;
    public InputField ValueText;
    public Text LoadedValueText;

    private BlueprintContractClient client;

    #region Unity messages

    private void Start() {
        // In a real game, the private key should be stored somewhere.
        // Essentially, it represents a users' account/identity.
        // But for this sample's simplicity, just generate a new private key each time.
        byte[] privateKey = CryptoUtils.GeneratePrivateKey();
        byte[] publicKey = CryptoUtils.PublicKeyFromPrivateKey(privateKey);
        this.client = new BlueprintContractClient(
            this.BackendHost,
            this.ContractAbi.text,
            privateKey,
            publicKey,
            NullLogger.Instance // Use Debug.unityLogger for more logs
        );

        // Subscribe to the event emitted by the contract
        this.client.ValueChanged += ClientOnValueChanged;
        this.client.ValueRemoved += ClientOnValueRemoved;
    }

    private void OnDestroy() {
        this.client.ValueChanged -= ClientOnValueChanged;
        this.client.ValueRemoved -= ClientOnValueRemoved;
    }

    private void Update() {
        // Dispatch the events.
        // This is done here to make sure the are dispatched on the main thread.
        this.client.DispatchQueuedEvents();
    }

    #endregion

    #region UI event handlers

    public async void StoreClickHandler() {
        this.LoadedValueText.text = "Storing...";
        await this.client.Store(this.KeyText.text, this.ValueText.text);
        this.LoadedValueText.text = "Stored!";
    }

    public async void LoadClickHandler() {
        this.LoadedValueText.text = "Loading...";
        string value = await this.client.Load(this.KeyText.text);
        this.LoadedValueText.text = $"Value of '{this.KeyText.text}' is '{value}'";
    }

    public async void RemoveClickHandler() {
        this.LoadedValueText.text = "Removing...";
        await this.client.Remove(this.KeyText.text);
        this.LoadedValueText.text = "Removed!";
    }

    #endregion

    #region Contract event handlers

    private void ClientOnValueChanged(string key, string value) {
        Debug.LogFormat("Value changed: key '{0}', value '{1}'", key, value);
    }

    private void ClientOnValueRemoved(string key) {
        Debug.LogFormat("Value removed: key '{0}'", key);
    }

    #endregion
}
