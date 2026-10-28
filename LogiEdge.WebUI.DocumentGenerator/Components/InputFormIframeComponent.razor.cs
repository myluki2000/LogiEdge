using LogiEdge.CustomerService.Dto;
using LogiEdge.CustomerService.Services;
using LogiEdge.Service.DocumentGenerator.DocumentGeneration;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Reflection;
using System.Text.Json;

namespace LogiEdge.WebUI.DocumentGenerator.Components
{
    /// <summary>
    /// Renders a template input form (custom HTML/JS) in a sandboxed iframe and bridges it to the parent
    /// via postMessage. See .notes/document-input-forms.md for the protocol.
    /// </summary>
    public partial class InputFormIframeComponent : ComponentBase, IAsyncDisposable
    {
        private const string BridgeResourceSuffix = "documentInputFormBridge.js";
        private const string ParentModuleUrl = "/_content/LogiEdge.WebUI.DocumentGenerator/js/documentInputForm.js";
        private const string CollectMessageJson = """{"v":1,"type":"collect"}""";
        private static readonly TimeSpan CollectTimeout = TimeSpan.FromSeconds(10);
        private static readonly JsonSerializerOptions RpcResultJsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private static string? _cachedBridgeJs;

        [Parameter]
        public string? FormHtml { get; set; }
        /// <summary>Already-serialized context JSON (escaped for script injection before embedding).</summary>
        [Parameter]
        public string? ContextJson { get; set; }
        [Parameter]
        public EventCallback OnReady { get; set; }
        [Parameter]
        public EventCallback<string> OnError { get; set; }

        private readonly string _iframeId = $"doc-input-form-{Guid.NewGuid():N}";
        private readonly object _collectLock = new();
        private string? _srcDoc;
        private string? _bridgeJs;
        private string? _lastFormHtml;
        private string? _lastContextJson;
        private DotNetObjectReference<InputFormIframeComponent>? _dotNetRef;
        private IJSObjectReference? _jsModule;
        private TaskCompletionSource<(string? ValuesJson, string? Error)>? _pendingCollect;

        public bool IsReady { get; private set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            _bridgeJs = LoadBridgeJs();
            _dotNetRef = DotNetObjectReference.Create(this);
            _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", ParentModuleUrl);
            await _jsModule.InvokeVoidAsync("initBridge", _dotNetRef, _iframeId);
        }

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            if (FormHtml == _lastFormHtml && ContextJson == _lastContextJson)
                return;

            _lastFormHtml = FormHtml;
            _lastContextJson = ContextJson;

            if (string.IsNullOrWhiteSpace(FormHtml))
            {
                _srcDoc = null;
                IsReady = false;
                return;
            }

            // A new srcdoc reloads the iframe document — wait for a fresh 'ready' message.
            IsReady = false;
            _srcDoc = BuildSrcDoc();
        }

        /// <summary>Posts a collect request to the iframe and awaits its values (or error) up to 10 seconds.</summary>
        public async Task<(string? ValuesJson, string? Error)> CollectAsync()
        {
            TaskCompletionSource<(string?, string?)> tcs;
            bool shouldPost;
            lock (_collectLock)
            {
                if (_pendingCollect is { Task.IsCompleted: false } pending)
                {
                    tcs = pending;
                    shouldPost = false;
                }
                else
                {
                    tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    _pendingCollect = tcs;
                    shouldPost = true;
                }
            }

            if (shouldPost)
            {
                try
                {
                    await PostToIframeAsync(CollectMessageJson);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    CompletePendingCollect(null, "Failed to contact the input form.");
                    return (null, "Failed to contact the input form.");
                }
            }

            if (await Task.WhenAny(tcs.Task, Task.Delay(CollectTimeout)) != tcs.Task)
            {
                CompletePendingCollect(null, "Input form did not respond.");
                return (null, "Input form did not respond.");
            }

            return await tcs.Task;
        }

        private async Task OnFormMessage(string rawJson)
        {
            JsonElement message;
            try
            {
                using JsonDocument document = JsonDocument.Parse(rawJson);
                message = document.RootElement.Clone();
            }
            catch (JsonException)
            {
                return; // ignore malformed messages
            }

            string? type = message.TryGetProperty("type", out JsonElement typeElement) ? typeElement.GetString() : null;
            switch (type)
            {
                case "ready":
                    IsReady = true;
                    StateHasChanged();
                    await OnReady.InvokeAsync();
                    break;

                case "error":
                    string? errorMessage = message.TryGetProperty("message", out JsonElement messageElement) && messageElement.ValueKind == JsonValueKind.String
                        ? messageElement.GetString()!
                        : "Unknown input form error.";
                    StateHasChanged();
                    await OnError.InvokeAsync(errorMessage);
                    CompletePendingCollect(null, errorMessage);
                    break;

                case "values":
                    if (message.TryGetProperty("values", out JsonElement values) && values.ValueKind != JsonValueKind.Null)
                        CompletePendingCollect(values.GetRawText(), null);
                    else
                        CompletePendingCollect(null, "The input form did not return any values.");
                    break;

                case "rpc":
                    await HandleRpcMessageAsync(message);
                    break;
            }
        }

        private async Task HandleRpcMessageAsync(JsonElement message)
        {
            if (!message.TryGetProperty("id", out JsonElement idElement) || idElement.ValueKind != JsonValueKind.Number)
                return;
            long id = idElement.GetInt64();
            string? method = message.TryGetProperty("method", out JsonElement methodElement) ? methodElement.GetString() : null;

            (string? ResultJson, string? Error) outcome;
            if (method == "getCustomers") // explicit allowlist — the only RPC exposed to templates
            {
                try
                {
                    List<CustomerDto> customers = await CustomerManagementService.GetCustomersAsync();
                    JsonElement result = JsonSerializer.SerializeToElement(customers, RpcResultJsonSerializerOptions);
                    outcome = (result.GetRawText(), null);
                }
                catch (Exception exception)
                {
                    outcome = (null, exception.Message);
                }
            }
            else
            {
                outcome = (null, $"Unknown RPC method: '{method}'.");
            }

            string replyJson = outcome.Error is null
                ? $"{{\"v\":1,\"type\":\"rpcResult\",\"id\":{id},\"result\":{outcome.ResultJson}}}"
                : $"{{\"v\":1,\"type\":\"rpcResult\",\"id\":{id},\"error\":{JsonSerializer.Serialize(outcome.Error)}}}";
            await PostToIframeAsync(replyJson);
        }

        private async Task PostToIframeAsync(string messageJson)
        {
            if (_jsModule is not { } module)
                return;
            await module.InvokeVoidAsync("postToIframe", _iframeId, messageJson);
        }

        private void CompletePendingCollect(string? valuesJson, string? error)
        {
            lock (_collectLock)
            {
                _pendingCollect?.TrySetResult((valuesJson, error));
            }
        }

        private string BuildSrcDoc()
        {
            // Plain concatenation on purpose: FormHtml and the bridge JS are untrusted content and must
            // never pass through string interpolation (braces in template code would be misparsed).
            string escapedContext = UserInputJson.EscapeForScriptInjection(ContextJson ?? "null");
            return "<!DOCTYPE html><html><head><meta charset=\"utf-8\"><style>" + FormStyles + "</style></head><body>"
                + "<script>const context = " + escapedContext + ";</script>"
                + "<script>" + _bridgeJs + "</script>"
                + FormHtml!
                + "</body></html>";
        }

        private static string LoadBridgeJs()
        {
            if (_cachedBridgeJs != null)
                return _cachedBridgeJs;

            Assembly assembly = typeof(InputFormIframeComponent).Assembly;
            string resourceName = assembly.GetManifestResourceNames().Single(name => name.EndsWith(BridgeResourceSuffix, StringComparison.OrdinalIgnoreCase));
            using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
            using StreamReader reader = new(stream);
            _cachedBridgeJs = reader.ReadToEnd();
            return _cachedBridgeJs;
        }

        public async ValueTask DisposeAsync()
        {
            if (_jsModule is { } module)
            {
                try
                {
                    await module.InvokeVoidAsync("disposeBridge"); // remove the listener first — no stale callbacks
                }
                catch (Exception)
                {
                    // The circuit may already be gone; keep disposing the rest.
                }
                try
                {
                    await module.DisposeAsync();
                }
                catch (Exception)
                {
                    // Ignore — best effort.
                }
            }
            _dotNetRef?.Dispose();
        }

        private const string FormStyles = """
            body { font-family: system-ui, -apple-system, 'Segoe UI', sans-serif; font-size: 14px; color: #212529; margin: 12px; }
            label { display: block; margin-bottom: 10px; }
            input, select, textarea { width: 100%; box-sizing: border-box; margin-top: 4px; padding: 4px 8px; border: 1px solid #ced4da; border-radius: 4px; font: inherit; }
            """;
    }
}
